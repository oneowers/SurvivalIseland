// Path: Assets/Project/Scripts/Player/AimTargetingSystem.cs
// Purpose: Selects the best right-stick aim target around the player using generic AimTargetable components.
// Dependencies: MessagePipe, UnityEngine, VContainer.Unity, ProjectResonance.PlayerInput, ProjectResonance.PlayerCombat.

using System;
using MessagePipe;
using ProjectResonance.PlayerInput;
using UnityEngine;
using VContainer.Unity;

namespace ProjectResonance.PlayerCombat
{
    /// <summary>
    /// Scans nearby world objects and keeps the best generic aim target selected.
    /// </summary>
    public sealed class AimTargetingSystem : IStartable, ITickable, IDisposable
    {
        private readonly CharacterController _characterController;
        private readonly Camera _playerCamera;
        private readonly AimTargetingConfig _config;
        private readonly IBufferedSubscriber<AimInput> _aimInputSubscriber;
        private readonly IPlayerAimModeQuery _playerAimModeQuery;

        private IDisposable _aimInputSubscription;
        private Collider[] _overlapResults;
        private AimTargetable[] _candidateBuffer;
        private Vector2 _aimInput;
        private float _scanCooldown;

        /// <summary>
        /// Creates the aim-targeting system.
        /// </summary>
        /// <param name="characterController">Player character controller.</param>
        /// <param name="playerCamera">Runtime player camera.</param>
        /// <param name="config">Aim-targeting authoring config.</param>
        /// <param name="aimInputSubscriber">Buffered aim input subscriber.</param>
        public AimTargetingSystem(
            CharacterController characterController,
            Camera playerCamera,
            AimTargetingConfig config,
            IBufferedSubscriber<AimInput> aimInputSubscriber,
            IPlayerAimModeQuery playerAimModeQuery)
        {
            _characterController = characterController;
            _playerCamera = playerCamera;
            _config = config;
            _aimInputSubscriber = aimInputSubscriber;
            _playerAimModeQuery = playerAimModeQuery;
        }

        /// <summary>
        /// Gets the currently selected target.
        /// </summary>
        public AimTargetable CurrentTarget { get; private set; }

        /// <summary>
        /// Gets whether aim input is currently active.
        /// </summary>
        public bool HasActiveAim => _aimInput.sqrMagnitude > ResolveSelectionDeadZone() * ResolveSelectionDeadZone();

        /// <summary>
        /// Starts listening for buffered aim input and allocates scan buffers.
        /// </summary>
        public void Start()
        {
            var bufferSize = ResolveMaxDetectedColliders();
            _overlapResults = new Collider[bufferSize];
            _candidateBuffer = new AimTargetable[bufferSize];
            _aimInputSubscription = _aimInputSubscriber.Subscribe(OnAimInputChanged);
        }

        /// <summary>
        /// Ticks the nearby-target scan.
        /// </summary>
        public void Tick()
        {
            if (_playerAimModeQuery != null && _playerAimModeQuery.IsPlantingModeActive)
            {
                SetCurrentTarget(null);
                _scanCooldown = 0f;
                return;
            }

            if (!HasActiveAim)
            {
                SetCurrentTarget(null);
                _scanCooldown = 0f;
                return;
            }

            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            _scanCooldown -= deltaTime;
            if (_scanCooldown > 0f && IsCurrentTargetStillValid())
            {
                return;
            }

            _scanCooldown = ResolveScanFrequencySeconds();
            EvaluateCurrentTarget();
        }

        /// <summary>
        /// Releases subscriptions owned by the targeting system.
        /// </summary>
        public void Dispose()
        {
            _aimInputSubscription?.Dispose();
            _aimInputSubscription = null;
            SetCurrentTarget(null);
        }

        private void OnAimInputChanged(AimInput message)
        {
            _aimInput = Vector2.ClampMagnitude(message.Value, 1f);
            _scanCooldown = 0f;
        }

        private void EvaluateCurrentTarget()
        {
            var cameraToUse = _playerCamera != null ? _playerCamera : Camera.main;
            if (cameraToUse == null || _characterController == null)
            {
                SetCurrentTarget(null);
                return;
            }

            var origin = _characterController.transform.position;
            var desiredPoint = ResolveDesiredPoint(cameraToUse, origin);
            var overlapCount = Physics.OverlapSphereNonAlloc(
                origin,
                ResolveMaxAimRadius(),
                _overlapResults,
                ResolveBroadphaseMask(),
                QueryTriggerInteraction.Collide);

            var bestDistanceSqr = float.PositiveInfinity;
            AimTargetable bestTarget = null;
            var uniqueCount = 0;

            for (var index = 0; index < overlapCount; index++)
            {
                var collider = _overlapResults[index];
                if (collider == null)
                {
                    continue;
                }

                var targetable = collider.GetComponentInParent<AimTargetable>();
                if (targetable == null || ContainsTarget(uniqueCount, targetable))
                {
                    continue;
                }

                _candidateBuffer[uniqueCount] = targetable;
                uniqueCount++;

                if (!targetable.isActiveAndEnabled || !targetable.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!targetable.TryGetHitReceiver(out var receiver) || !receiver.CanReceiveHit)
                {
                    continue;
                }

                var anchorPosition = targetable.ResolveAnchorPosition(ResolveTargetAnchorHeightBias());
                var distanceSqr = (anchorPosition - desiredPoint).sqrMagnitude;
                if (distanceSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distanceSqr;
                    bestTarget = targetable;
                }
            }

            SetCurrentTarget(bestTarget);
        }

        private void SetCurrentTarget(AimTargetable nextTarget)
        {
            if (ReferenceEquals(CurrentTarget, nextTarget))
            {
                return;
            }

            CurrentTarget = nextTarget;
            if (CurrentTarget == null)
            {
                return;
            }

            var flashColor = _config != null ? _config.SelectionFlashColor : new Color(1f, 0.78f, 0.28f, 1f);
            var flashDuration = _config != null ? _config.SelectionFlashDuration : 1f;
            var flashStrength = _config != null ? _config.SelectionFlashStrength : 0.7f;
            CurrentTarget.PlaySelectionFeedback(flashColor, flashDuration, flashStrength);
        }

        private bool ContainsTarget(int count, AimTargetable candidate)
        {
            for (var index = 0; index < count; index++)
            {
                if (_candidateBuffer[index] == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsCurrentTargetStillValid()
        {
            if (CurrentTarget == null || _characterController == null)
            {
                return false;
            }

            if (!CurrentTarget.isActiveAndEnabled || !CurrentTarget.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (!CurrentTarget.TryGetHitReceiver(out var receiver) || !receiver.CanReceiveHit)
            {
                return false;
            }

            var origin = _characterController.transform.position;
            var toTarget = CurrentTarget.ResolveAnchorPosition(ResolveTargetAnchorHeightBias()) - origin;
            toTarget.y = 0f;
            return toTarget.sqrMagnitude <= ResolveMaxAimRadius() * ResolveMaxAimRadius();
        }

        private Vector3 ResolveDesiredPoint(Camera cameraToUse, Vector3 origin)
        {
            var planarForward = cameraToUse.transform.forward;
            planarForward.y = 0f;
            if (planarForward.sqrMagnitude <= Mathf.Epsilon)
            {
                planarForward = _characterController != null ? _characterController.transform.forward : Vector3.forward;
                planarForward.y = 0f;
            }

            planarForward.Normalize();

            var planarRight = cameraToUse.transform.right;
            planarRight.y = 0f;
            if (planarRight.sqrMagnitude <= Mathf.Epsilon)
            {
                planarRight = Vector3.Cross(Vector3.up, planarForward);
            }

            planarRight.Normalize();

            var planarDirection = (planarRight * _aimInput.x) + (planarForward * _aimInput.y);
            if (planarDirection.sqrMagnitude > Mathf.Epsilon)
            {
                planarDirection.Normalize();
            }

            var radius = Mathf.Clamp01(_aimInput.magnitude) * ResolveMaxAimRadius();
            return origin + new Vector3(0f, ResolveTargetAnchorHeightBias(), 0f) + (planarDirection * radius);
        }

        private float ResolveMaxAimRadius()
        {
            return _config != null ? _config.MaxAimRadius : 2f;
        }

        private float ResolveScanFrequencySeconds()
        {
            return _config != null ? _config.ScanFrequencySeconds : 0.05f;
        }

        private int ResolveMaxDetectedColliders()
        {
            return _config != null ? _config.MaxDetectedColliders : 32;
        }

        private int ResolveBroadphaseMask()
        {
            return _config != null ? _config.BroadphaseLayerMask.value : ~0;
        }

        private float ResolveTargetAnchorHeightBias()
        {
            return _config != null ? _config.TargetAnchorHeightBias : 0.9f;
        }

        private float ResolveSelectionDeadZone()
        {
            return _config != null ? _config.SelectionDeadZone : 0.08f;
        }
    }
}
