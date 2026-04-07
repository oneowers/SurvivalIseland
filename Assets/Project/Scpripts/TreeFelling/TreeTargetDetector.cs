// Path: Assets/Project/Scpripts/TreeFelling/TreeTargetDetector.cs
// Purpose: Detects nearby trees around the player and selects the one closest to the screen center.
// Dependencies: UniTask, UnityEngine, TreeFelling.

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectResonance.TreeFelling
{
    /// <summary>
    /// Detects nearby trees and keeps the best visible candidate selected.
    /// </summary>
    [AddComponentMenu("Project Resonance/Tree Felling/Tree Target Detector")]
    [DisallowMultipleComponent]
    public sealed class TreeTargetDetector : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField]
        private bool _enableDebugLogs = true;

        [SerializeField]
        private bool _drawDebugGizmos = true;

        [Header("References")]
        [SerializeField]
        private TreeTargetingConfig _targetingConfig;

        [SerializeField]
        private Camera _playerCamera;

        [SerializeField]
        private Transform _playerOrigin;

        private Collider[] _overlapResults;
        private ChoppableTree[] _candidateTrees;
        private CancellationTokenSource _scanCancellation;
        private ChoppableTree _currentTarget;
        private bool _isInitialized;

        /// <summary>
        /// Gets the currently selected tree target.
        /// </summary>
        public ChoppableTree CurrentTarget => _currentTarget;

        /// <summary>
        /// Starts the periodic nearby-tree scan loop.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized || _targetingConfig == null)
            {
                if (_enableDebugLogs && _targetingConfig == null)
                {
                    Debug.LogWarning("[TreeTargetDetector] Initialize skipped because TreeTargetingConfig is missing.", this);
                }

                return;
            }

            _isInitialized = true;

            var bufferSize = Mathf.Max(1, _targetingConfig.MaxDetectedColliders);
            _overlapResults = new Collider[bufferSize];
            _candidateTrees = new ChoppableTree[bufferSize];
            _scanCancellation = new CancellationTokenSource();

            if (_enableDebugLogs)
            {
                Debug.Log($"[TreeTargetDetector] Initialized. Radius={_targetingConfig.DetectionRadius}, Frequency={_targetingConfig.CheckFrequencySeconds}, LayerMask={_targetingConfig.TreeLayerMask.value}", this);
            }

            RunDetectionLoopAsync(_scanCancellation.Token).Forget();
        }

        /// <summary>
        /// Stops the periodic nearby-tree scan loop.
        /// </summary>
        public void Shutdown()
        {
            if (_scanCancellation != null)
            {
                _scanCancellation.Cancel();
                _scanCancellation.Dispose();
                _scanCancellation = null;
            }

            _currentTarget = null;
            _isInitialized = false;
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private void Reset()
        {
            _playerCamera = FindFirstObjectByType<Camera>();
            _playerOrigin = transform;
        }

        private async UniTaskVoid RunDetectionLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    EvaluateNearestVisibleTree();

                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_targetingConfig.CheckFrequencySeconds),
                        DelayType.DeltaTime,
                        PlayerLoopTiming.Update,
                        cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Scope shutdown is expected to stop the detector loop.
            }
        }

        private void EvaluateNearestVisibleTree()
        {
            var cameraToUse = _playerCamera != null ? _playerCamera : Camera.main;
            var origin = _playerOrigin != null ? _playerOrigin.position : transform.position;

            if (cameraToUse == null || _targetingConfig == null)
            {
                SetCurrentTarget(null);
                return;
            }

            var hitCount = Physics.OverlapSphereNonAlloc(
                origin,
                _targetingConfig.DetectionRadius,
                _overlapResults,
                _targetingConfig.TreeLayerMask,
                QueryTriggerInteraction.Ignore);

            if (hitCount <= 0)
            {
                SetCurrentTarget(null);
                return;
            }

            var bestAlignment = float.NegativeInfinity;
            var bestDistanceSqr = float.PositiveInfinity;
            ChoppableTree bestTree = null;
            var uniqueTreeCount = 0;

            for (var index = 0; index < hitCount; index++)
            {
                var collider = _overlapResults[index];
                if (collider == null)
                {
                    continue;
                }

                var tree = collider.GetComponentInParent<ChoppableTree>();
                if (tree == null || tree.IsFalling || ContainsTree(uniqueTreeCount, tree))
                {
                    continue;
                }

                _candidateTrees[uniqueTreeCount] = tree;
                uniqueTreeCount++;

                var toTree = tree.transform.position - cameraToUse.transform.position;
                var distanceSqr = toTree.sqrMagnitude;
                if (distanceSqr <= Mathf.Epsilon)
                {
                    continue;
                }

                var alignment = Vector3.Dot(cameraToUse.transform.forward, toTree.normalized);
                if (alignment < _targetingConfig.MinimumViewDot)
                {
                    continue;
                }

                // Prefer the object closest to screen center, then break ties by distance.
                if (alignment > bestAlignment || (Mathf.Approximately(alignment, bestAlignment) && distanceSqr < bestDistanceSqr))
                {
                    bestAlignment = alignment;
                    bestDistanceSqr = distanceSqr;
                    bestTree = tree;
                }
            }

            SetCurrentTarget(bestTree);
        }

        private bool ContainsTree(int count, ChoppableTree tree)
        {
            for (var index = 0; index < count; index++)
            {
                if (_candidateTrees[index] == tree)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetCurrentTarget(ChoppableTree nextTarget)
        {
            if (_currentTarget == nextTarget)
            {
                return;
            }

            _currentTarget = nextTarget;

            if (_enableDebugLogs)
            {
                Debug.Log($"[TreeTargetDetector] CurrentTarget={(nextTarget != null ? nextTarget.name : "null")}", this);
            }
        }

        private void OnDrawGizmos()
        {
            if (!_drawDebugGizmos)
            {
                return;
            }

            var origin = _playerOrigin != null ? _playerOrigin.position : transform.position;
            var radius = _targetingConfig != null ? _targetingConfig.DetectionRadius : 3f;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(origin, radius);

            if (_currentTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(origin, _currentTarget.transform.position);
            }
        }
    }
}
