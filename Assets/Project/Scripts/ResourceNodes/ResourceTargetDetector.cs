// Path: Assets/Project/Scripts/ResourceNodes/ResourceTargetDetector.cs
// Purpose: Detects nearby resource nodes around the player and selects the one closest to the screen center.
// Dependencies: UniTask, UnityEngine, ProjectResonance.ResourceNodes.

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectResonance.ResourceNodes
{
    /// <summary>
    /// Detects nearby resource nodes and keeps the best visible candidate selected.
    /// </summary>
    [AddComponentMenu("Project Resonance/Resource Nodes/Resource Target Detector")]
    [DisallowMultipleComponent]
    public sealed class ResourceTargetDetector : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField]
        private bool _enableDebugLogs = false;

        [Header("References")]
        [SerializeField]
        private ResourceTargetingConfig _targetingConfig;

        [SerializeField]
        private Camera _playerCamera;

        [SerializeField]
        private Transform _playerOrigin;

        private Collider[] _overlapResults;
        private ResourceNodeRuntime[] _candidateNodes;
        private CancellationTokenSource _scanCancellation;
        private ResourceNodeRuntime _currentTarget;
        private bool _isInitialized;

        /// <summary>
        /// Gets the currently selected resource target.
        /// </summary>
        public ResourceNodeRuntime CurrentTarget => _currentTarget;

        /// <summary>
        /// Starts the periodic nearby-resource scan loop.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized || _targetingConfig == null)
            {
                if (_enableDebugLogs && _targetingConfig == null)
                {
                    Debug.LogWarning("[ResourceTargetDetector] Initialize skipped because ResourceTargetingConfig is missing.", this);
                }

                return;
            }

            _isInitialized = true;

            var bufferSize = Mathf.Max(1, _targetingConfig.MaxDetectedColliders);
            _overlapResults = new Collider[bufferSize];
            _candidateNodes = new ResourceNodeRuntime[bufferSize];
            _scanCancellation = new CancellationTokenSource();

            if (_enableDebugLogs)
            {
                Debug.Log($"[ResourceTargetDetector] Initialized. Radius={_targetingConfig.DetectionRadius}, Frequency={_targetingConfig.CheckFrequencySeconds}, LayerMask={_targetingConfig.BroadphaseLayerMask.value}", this);
            }

            RunDetectionLoopAsync(_scanCancellation.Token).Forget();
        }

        /// <summary>
        /// Stops the periodic nearby-resource scan loop.
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
                    EvaluateNearestVisibleResource();

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

        private void EvaluateNearestVisibleResource()
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
                _targetingConfig.BroadphaseLayerMask,
                QueryTriggerInteraction.Ignore);

            if (hitCount <= 0)
            {
                SetCurrentTarget(null);
                return;
            }

            var bestAlignment = float.NegativeInfinity;
            var bestDistanceSqr = float.PositiveInfinity;
            ResourceNodeRuntime bestNode = null;
            var uniqueNodeCount = 0;

            for (var index = 0; index < hitCount; index++)
            {
                var collider = _overlapResults[index];
                if (collider == null)
                {
                    continue;
                }

                var node = collider.GetComponentInParent<ResourceNodeRuntime>();
                if (node == null || node.IsDestroyed || ContainsNode(uniqueNodeCount, node))
                {
                    continue;
                }

                _candidateNodes[uniqueNodeCount] = node;
                uniqueNodeCount++;

                var toNode = node.transform.position - cameraToUse.transform.position;
                var distanceSqr = toNode.sqrMagnitude;
                if (distanceSqr <= Mathf.Epsilon)
                {
                    continue;
                }

                var alignment = Vector3.Dot(cameraToUse.transform.forward, toNode.normalized);
                if (alignment < _targetingConfig.MinimumViewDot)
                {
                    continue;
                }

                if (alignment > bestAlignment || (Mathf.Approximately(alignment, bestAlignment) && distanceSqr < bestDistanceSqr))
                {
                    bestAlignment = alignment;
                    bestDistanceSqr = distanceSqr;
                    bestNode = node;
                }
            }

            SetCurrentTarget(bestNode);
        }

        private bool ContainsNode(int count, ResourceNodeRuntime node)
        {
            for (var index = 0; index < count; index++)
            {
                if (_candidateNodes[index] == node)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetCurrentTarget(ResourceNodeRuntime nextTarget)
        {
            if (_currentTarget == nextTarget)
            {
                return;
            }

            _currentTarget = nextTarget;

            if (_enableDebugLogs)
            {
                Debug.Log($"[ResourceTargetDetector] CurrentTarget={(nextTarget != null ? nextTarget.name : "null")}", this);
            }
        }

    }
}
