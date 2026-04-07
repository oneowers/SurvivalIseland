// Path: Assets/Project/Scpripts/TreeFelling/TreeFallSystem.cs
// Purpose: Animates tree falling and spawns pooled logs and branch fragments after impact.
// Dependencies: UniTask, MessagePipe, UnityEngine.Pool, VContainer, TreeConfig.

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using ProjectResonance.Common.Random;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;

namespace ProjectResonance.TreeFelling
{
    /// <summary>
    /// Owns the fall animation and pooled fragment spawning for chopped trees.
    /// </summary>
    public sealed class TreeFallSystem : IStartable, IDisposable
    {
        private const string DebugPrefix = "[TreeFallSystem]";

        private readonly TreeFallConfig _fallConfig;
        private readonly IRandomProvider _randomProvider;
        private readonly IObjectResolver _resolver;
        private readonly ISubscriber<TreeFallStartEvent> _treeFallSubscriber;
        private readonly IPublisher<SoundEvent> _soundPublisher;
        private readonly IPublisher<ParticleEvent> _particlePublisher;

        private ObjectPool<LogPickup> _logPool;
        private ObjectPool<GameObject> _branchPool;
        private IDisposable _treeFallSubscription;
        private CancellationTokenSource _disposeCancellation;

        /// <summary>
        /// Initializes the tree fall system.
        /// </summary>
        /// <param name="fallConfig">Fall sequence config.</param>
        /// <param name="randomProvider">Injectable random provider.</param>
        /// <param name="resolver">Resolver used to instantiate pooled prefabs.</param>
        /// <param name="treeFallSubscriber">Tree fall event subscriber.</param>
        /// <param name="soundPublisher">One-shot sound publisher.</param>
        /// <param name="particlePublisher">Particle event publisher.</param>
        public TreeFallSystem(
            TreeFallConfig fallConfig,
            IRandomProvider randomProvider,
            IObjectResolver resolver,
            ISubscriber<TreeFallStartEvent> treeFallSubscriber,
            IPublisher<SoundEvent> soundPublisher,
            IPublisher<ParticleEvent> particlePublisher)
        {
            _fallConfig = fallConfig;
            _randomProvider = randomProvider;
            _resolver = resolver;
            _treeFallSubscriber = treeFallSubscriber;
            _soundPublisher = soundPublisher;
            _particlePublisher = particlePublisher;
        }

        /// <summary>
        /// Creates pools and starts listening for tree fall events.
        /// </summary>
        public void Start()
        {
            _disposeCancellation = new CancellationTokenSource();

            Debug.Log($"{DebugPrefix} Start. FallConfigAssigned={_fallConfig != null}, LogPrefabAssigned={(_fallConfig != null && _fallConfig.LogPrefab != null)}, BranchPrefabAssigned={(_fallConfig != null && _fallConfig.BranchFragmentPrefab != null)}");

            if (_fallConfig != null && _fallConfig.LogPrefab != null)
            {
                var logPoolCapacity = Mathf.Max(1, _fallConfig.LogPoolCapacity);
                _logPool = new ObjectPool<LogPickup>(
                    CreateLogPickup,
                    OnTakeLogPickup,
                    OnReturnLogPickup,
                    OnDestroyLogPickup,
                    false,
                    logPoolCapacity,
                    logPoolCapacity * 4);
            }

            if (_fallConfig != null && _fallConfig.BranchFragmentPrefab != null)
            {
                var branchPoolCapacity = Mathf.Max(1, _fallConfig.BranchPoolCapacity);
                _branchPool = new ObjectPool<GameObject>(
                    CreateBranchFragment,
                    OnTakeBranchFragment,
                    OnReturnBranchFragment,
                    OnDestroyBranchFragment,
                    false,
                    branchPoolCapacity,
                    branchPoolCapacity * 4);
            }

            _treeFallSubscription = _treeFallSubscriber.Subscribe(OnTreeFallStarted);
        }

        /// <summary>
        /// Stops the system and disposes runtime resources.
        /// </summary>
        public void Dispose()
        {
            _treeFallSubscription?.Dispose();
            _treeFallSubscription = null;

            if (_disposeCancellation != null)
            {
                _disposeCancellation.Cancel();
                _disposeCancellation.Dispose();
                _disposeCancellation = null;
            }

            _logPool?.Dispose();
            _branchPool?.Dispose();
        }

        private void OnTreeFallStarted(TreeFallStartEvent message)
        {
            if (message.Tree == null || _fallConfig == null)
            {
                Debug.LogWarning($"{DebugPrefix} TreeFallStartEvent ignored. TreeAssigned={message.Tree != null}, FallConfigAssigned={_fallConfig != null}");
                return;
            }

            Debug.Log($"{DebugPrefix} TreeFallStartEvent received for {message.Tree.name}");
            RunFallSequenceAsync(message.Tree, _disposeCancellation.Token).Forget();
        }

        private async UniTaskVoid RunFallSequenceAsync(ChoppableTree tree, CancellationToken cancellationToken)
        {
            try
            {
                var fallDirection = tree.ResolveFallDirection();
                var fallAxis = Vector3.Cross(Vector3.up, fallDirection);

                if (fallAxis.sqrMagnitude <= Mathf.Epsilon)
                {
                    fallAxis = tree.transform.right;
                }

                fallAxis.Normalize();

                var initialRotation = tree.transform.rotation;
                var swayRotation = Quaternion.AngleAxis(_fallConfig.SwayAngleDegrees, fallAxis) * initialRotation;
                var finalRotation = Quaternion.AngleAxis(_fallConfig.FallAngleDegrees, fallAxis) * initialRotation;

                Debug.Log($"{DebugPrefix} RunFallSequenceAsync started for {tree.name}. FallDirection={fallDirection}");

                await RotateTreeAsync(tree.transform, initialRotation, swayRotation, tree.Config.SwayDuration, cancellationToken);
                await RotateTreeAsync(tree.transform, swayRotation, finalRotation, _fallConfig.FallDuration, cancellationToken);

                SpawnLogFragments(tree, fallDirection);
                SpawnBranchFragments(tree, fallDirection, cancellationToken);

                if (tree.Config.FallSound != null)
                {
                    _soundPublisher.Publish(new SoundEvent("tree_fall", tree.transform.position, tree.Config.FallSound));
                }

                _particlePublisher.Publish(new ParticleEvent("dust_cloud", tree.transform.position));
                Debug.Log($"{DebugPrefix} Fall finished for {tree.name}");
            }
            catch (OperationCanceledException)
            {
                // Container shutdown is expected to interrupt pending fall sequences.
            }
        }

        private void SpawnLogFragments(ChoppableTree tree, Vector3 fallDirection)
        {
            if (_logPool == null || tree.Config == null)
            {
                return;
            }

            var baseLogCount = Mathf.Max(1, tree.Config.LogsOnFall);
            var minLogs = Mathf.Max(1, _fallConfig.MinLogsOnFall);
            var maxLogs = Mathf.Max(minLogs, _fallConfig.MaxLogsOnFall);
            var upperBound = Mathf.Max(baseLogCount, maxLogs);
            var logCount = Mathf.Clamp(Mathf.RoundToInt(_randomProvider.Range(baseLogCount, upperBound + 0.999f)), minLogs, upperBound);

            var startPosition = tree.transform.position + (Vector3.up * _fallConfig.LogSpawnHeight);
            var right = Vector3.Cross(Vector3.up, fallDirection).normalized;

            Debug.Log($"{DebugPrefix} Spawning log fragments. Count={logCount}, Tree={tree.name}");

            for (var index = 0; index < logCount; index++)
            {
                var logPickup = _logPool.Get();
                if (logPickup == null)
                {
                    continue;
                }

                var normalizedIndex = logCount > 1 ? index / (float)(logCount - 1) : 0.5f;
                var alongTrunkOffset = fallDirection * (_fallConfig.LogSpawnLineLength * normalizedIndex);
                var lateralOffset = right * ((index - ((logCount - 1) * 0.5f)) * _fallConfig.LogLateralSpacing);
                var spawnPosition = startPosition + alongTrunkOffset + lateralOffset;
                var spawnRotation = Quaternion.LookRotation(fallDirection, Vector3.up);

                logPickup.Spawn(spawnPosition, spawnRotation);
            }
        }

        private void SpawnBranchFragments(ChoppableTree tree, Vector3 fallDirection, CancellationToken cancellationToken)
        {
            if (_branchPool == null || tree.Config == null)
            {
                return;
            }

            var spawnCenter = tree.transform.position + (Vector3.up * _fallConfig.BranchSpawnHeight) + (fallDirection * (_fallConfig.LogSpawnLineLength * 0.5f));

            Debug.Log($"{DebugPrefix} Spawning branch fragments. Count={tree.Config.BranchesOnFall}, Tree={tree.name}");

            for (var index = 0; index < tree.Config.BranchesOnFall; index++)
            {
                var branch = _branchPool.Get();
                if (branch == null)
                {
                    continue;
                }

                var randomOffset2D = _randomProvider.InsideUnitCircle() * _fallConfig.BranchScatterRadius;
                var spawnPosition = spawnCenter + new Vector3(randomOffset2D.x, 0f, randomOffset2D.y);
                var spawnRotation = Quaternion.Euler(0f, _randomProvider.Range(0f, 360f), 0f);

                branch.transform.SetPositionAndRotation(spawnPosition, spawnRotation);

                if (branch.TryGetComponent<Rigidbody>(out var rigidbody))
                {
                    // Resetting the rigidbody prevents pooled fragments from carrying stale momentum.
                    rigidbody.velocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                    rigidbody.AddForce((fallDirection + Vector3.up).normalized * _fallConfig.BranchImpulse, ForceMode.Impulse);
                }

                ReleaseBranchAfterDelayAsync(branch, cancellationToken).Forget();
            }
        }

        private async UniTaskVoid ReleaseBranchAfterDelayAsync(GameObject branch, CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_fallConfig.BranchLifetimeSeconds),
                    DelayType.DeltaTime,
                    PlayerLoopTiming.Update,
                    cancellationToken);

                if (branch != null && _branchPool != null)
                {
                    _branchPool.Release(branch);
                }
            }
            catch (OperationCanceledException)
            {
                // Container shutdown should silently stop delayed releases.
            }
        }

        private LogPickup CreateLogPickup()
        {
            var logPickup = _resolver.Instantiate(_fallConfig.LogPrefab, (Transform)null);
            logPickup.BindPool(_logPool);
            logPickup.gameObject.SetActive(false);
            return logPickup;
        }

        private void OnTakeLogPickup(LogPickup logPickup)
        {
            if (logPickup != null)
            {
                logPickup.gameObject.SetActive(true);
            }
        }

        private void OnReturnLogPickup(LogPickup logPickup)
        {
            if (logPickup != null)
            {
                logPickup.gameObject.SetActive(false);
            }
        }

        private void OnDestroyLogPickup(LogPickup logPickup)
        {
            if (logPickup != null)
            {
                UnityEngine.Object.Destroy(logPickup.gameObject);
            }
        }

        private GameObject CreateBranchFragment()
        {
            var branchFragment = _resolver.Instantiate(_fallConfig.BranchFragmentPrefab, (Transform)null);
            branchFragment.SetActive(false);
            return branchFragment;
        }

        private void OnTakeBranchFragment(GameObject branchFragment)
        {
            if (branchFragment != null)
            {
                branchFragment.SetActive(true);
            }
        }

        private void OnReturnBranchFragment(GameObject branchFragment)
        {
            if (branchFragment != null)
            {
                branchFragment.SetActive(false);
            }
        }

        private void OnDestroyBranchFragment(GameObject branchFragment)
        {
            if (branchFragment != null)
            {
                UnityEngine.Object.Destroy(branchFragment);
            }
        }

        private static async UniTask RotateTreeAsync(
            Transform target,
            Quaternion from,
            Quaternion to,
            float duration,
            CancellationToken cancellationToken)
        {
            if (target == null)
            {
                return;
            }

            if (duration <= Mathf.Epsilon)
            {
                target.rotation = to;
                return;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);

                // SmoothStep keeps the same tactile anticipation as the original tweened sway/fall motion.
                var easedProgress = progress * progress * (3f - (2f * progress));
                target.rotation = Quaternion.Slerp(from, to, easedProgress);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            target.rotation = to;
        }
    }
}
