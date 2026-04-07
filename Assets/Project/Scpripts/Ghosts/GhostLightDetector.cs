// Path: Assets/Project/Scpripts/Ghosts/GhostLightDetector.cs
// Purpose: Periodically scans nearby colliders for ILightSource emitters and reports weak or fear-inducing light to a ghost.
// Dependencies: UniTask, GhostBase, GhostSpawnConfig, UnityEngine, VContainer.

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace ProjectResonance.Ghosts
{
    /// <summary>
    /// Contract for world objects that can influence ghost light behavior.
    /// </summary>
    public interface ILightSource
    {
        /// <summary>
        /// Gets the world-space source position.
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// Gets the normalized source intensity in the [0..1] range.
        /// </summary>
        float NormalizedIntensity { get; }

        /// <summary>
        /// Gets whether the source is currently emitting usable light.
        /// </summary>
        bool IsEmittingLight { get; }
    }

    /// <summary>
    /// Cached overlap-based light detector used by ghost actors.
    /// </summary>
    [AddComponentMenu("Project Resonance/Ghosts/Ghost Light Detector")]
    [DisallowMultipleComponent]
    public sealed class GhostLightDetector : MonoBehaviour
    {
        [SerializeField]
        [Min(0.1f)]
        private float _scanIntervalSeconds = 0.3f;

        [SerializeField]
        private LayerMask _lightLayerMask = ~0;

        [SerializeField]
        [Min(4)]
        private int _overlapBufferSize = 32;

        private readonly List<Component> _componentBuffer = new List<Component>(8);
        private readonly HashSet<int> _visitedSourceIds = new HashSet<int>();

        private GhostBase _owner;
        private GhostSpawnConfig _config;
        private Collider[] _overlapBuffer;
        private Vector3 _weakLightPosition;
        private float _weakLightIntensity;

        /// <summary>
        /// Gets the last total normalized light intensity detected around the owner ghost.
        /// </summary>
        public float TotalLightIntensity { get; private set; }

        [Inject]
        private void Construct(GhostSpawnConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Tries to get the strongest weak light target currently seen by the detector.
        /// </summary>
        /// <param name="position">Resolved target position.</param>
        /// <param name="intensity">Resolved normalized intensity.</param>
        /// <returns>True when a weak light source is currently available.</returns>
        public bool TryGetWeakLightTarget(out Vector3 position, out float intensity)
        {
            position = _weakLightPosition;
            intensity = _weakLightIntensity;
            return _weakLightIntensity > 0f;
        }

        /// <summary>
        /// Clears the cached detector snapshot.
        /// </summary>
        public void ResetSnapshot()
        {
            TotalLightIntensity = 0f;
            _weakLightPosition = default;
            _weakLightIntensity = 0f;
        }

        private void Awake()
        {
            _owner = GetComponent<GhostBase>();
            _overlapBuffer = new Collider[Mathf.Max(4, _overlapBufferSize)];
        }

        private void Start()
        {
            RunScanLoopAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid RunScanLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_owner != null && _owner.IsActive && gameObject.activeInHierarchy)
                    {
                        ScanNearbyLightSources();
                    }
                    else
                    {
                        ResetSnapshot();
                    }

                    await UniTask.Delay(
                        (int)(_scanIntervalSeconds * 1000f),
                        DelayType.DeltaTime,
                        PlayerLoopTiming.Update,
                        cancellationToken);
                }
            }
            catch (System.OperationCanceledException)
            {
                // Pool disposal or scene unload stops the detector through cancellation.
            }
        }

        private void ScanNearbyLightSources()
        {
            if (_owner == null)
            {
                return;
            }

            ResetSnapshot();
            _visitedSourceIds.Clear();

            var hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                _owner.DetectionRadius,
                _overlapBuffer,
                _lightLayerMask,
                QueryTriggerInteraction.Collide);

            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var hitCollider = _overlapBuffer[hitIndex];
                if (hitCollider == null)
                {
                    continue;
                }

                CollectLightSourcesFromHierarchy(hitCollider.transform);
            }

            if (TotalLightIntensity > (_config != null ? _config.FearLightThreshold : 0.6f))
            {
                _owner.OnLightDetected(TotalLightIntensity);
            }
        }

        private void CollectLightSourcesFromHierarchy(Transform currentTransform)
        {
            while (currentTransform != null)
            {
                _componentBuffer.Clear();
                currentTransform.GetComponents<Component>(_componentBuffer);

                for (var componentIndex = 0; componentIndex < _componentBuffer.Count; componentIndex++)
                {
                    if (!(_componentBuffer[componentIndex] is ILightSource lightSource) || !lightSource.IsEmittingLight)
                    {
                        continue;
                    }

                    var sourceId = _componentBuffer[componentIndex].GetInstanceID();
                    if (!_visitedSourceIds.Add(sourceId))
                    {
                        continue;
                    }

                    var normalizedIntensity = Mathf.Clamp01(lightSource.NormalizedIntensity);
                    TotalLightIntensity += normalizedIntensity;

                    if (normalizedIntensity < (_config != null ? _config.FearLightThreshold : 0.6f) &&
                        normalizedIntensity > _weakLightIntensity)
                    {
                        _weakLightIntensity = normalizedIntensity;
                        _weakLightPosition = lightSource.Position;
                    }
                }

                currentTransform = currentTransform.parent;
            }
        }
    }
}
