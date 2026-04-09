// Path: Assets/Project/Scripts/Player/PlayerAimCombatInteractor.cs
// Purpose: Rotates the player toward the selected aim target and applies hits when the mobile HIT action is pressed.
// Dependencies: UnityEngine, VContainer.Unity, ProjectResonance.PlayerInput, ProjectResonance.PlayerCombat.

using System;
using ProjectResonance.Inventory;
using ProjectResonance.PlayerInput;
using UnityEngine;
using VContainer.Unity;

namespace ProjectResonance.PlayerCombat
{
    /// <summary>
    /// Executes player combat against the target selected by the right aim joystick.
    /// </summary>
    public sealed class PlayerAimCombatInteractor : IStartable, ILateTickable, IDisposable
    {
        private readonly CharacterController _characterController;
        private readonly AimTargetingSystem _aimTargetingSystem;
        private readonly AimTargetingConfig _config;
        private readonly PlayerHitDamageResolver _damageResolver;
        private readonly EquippedToolDurabilityService _equippedToolDurabilityService;
        private readonly IPlayerAimModeQuery _playerAimModeQuery;
        private readonly PlayerInputHandler _playerInputHandler;

        private Vector2 _aimInput;
        private bool _wasAimActive;
        private AimTargetable _lastTrackedTarget;

        /// <summary>
        /// Creates the player aim-combat interactor.
        /// </summary>
        /// <param name="characterController">Player character controller.</param>
        /// <param name="aimTargetingSystem">Runtime aim targeting system.</param>
        /// <param name="config">Aim targeting config.</param>
        /// <param name="damageResolver">Outgoing hit damage resolver.</param>
        public PlayerAimCombatInteractor(
            CharacterController characterController,
            AimTargetingSystem aimTargetingSystem,
            AimTargetingConfig config,
            PlayerHitDamageResolver damageResolver,
            EquippedToolDurabilityService equippedToolDurabilityService,
            IPlayerAimModeQuery playerAimModeQuery,
            PlayerInputHandler playerInputHandler)
        {
            _characterController = characterController;
            _aimTargetingSystem = aimTargetingSystem;
            _config = config;
            _damageResolver = damageResolver;
            _equippedToolDurabilityService = equippedToolDurabilityService;
            _playerAimModeQuery = playerAimModeQuery;
            _playerInputHandler = playerInputHandler;
        }

        /// <summary>
        /// Starts listening for aim and hit input messages.
        /// </summary>
        public void Start()
        {
            if (_playerInputHandler == null)
            {
                return;
            }

            _aimInput = Vector2.ClampMagnitude(_playerInputHandler.CurrentAimInput, 1f);
            _playerInputHandler.AimInputChanged += OnAimInputChanged;
            _playerInputHandler.HeavyInteractPerformed += OnHeavyInteract;
        }

        /// <summary>
        /// Rotates the player toward the currently selected aim target.
        /// </summary>
        public void LateTick()
        {
            if (_characterController == null || _aimTargetingSystem == null || !_aimTargetingSystem.HasActiveAim)
            {
                return;
            }

            if (_aimInput.sqrMagnitude <= ResolveSelectionDeadZone() * ResolveSelectionDeadZone())
            {
                return;
            }

            var target = _aimTargetingSystem.CurrentTarget;
            if (target == null)
            {
                return;
            }

            _lastTrackedTarget = target;

            var targetPosition = target.ResolveAnchorPosition(ResolveTargetAnchorHeightBias());
            var lookDirection = targetPosition - _characterController.transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            _characterController.transform.rotation = Quaternion.Slerp(
                _characterController.transform.rotation,
                targetRotation,
                ResolveFacingRotationSpeed() * Time.deltaTime);
        }

        /// <summary>
        /// Releases input subscriptions owned by the combat interactor.
        /// </summary>
        public void Dispose()
        {
            if (_playerInputHandler == null)
            {
                return;
            }

            _playerInputHandler.AimInputChanged -= OnAimInputChanged;
            _playerInputHandler.HeavyInteractPerformed -= OnHeavyInteract;
        }

        private void OnAimInputChanged(AimInput message)
        {
            var nextAimInput = Vector2.ClampMagnitude(message.Value, 1f);
            var isAimActive = nextAimInput.sqrMagnitude > ResolveSelectionDeadZone() * ResolveSelectionDeadZone();
            var currentTarget = _aimTargetingSystem != null ? _aimTargetingSystem.CurrentTarget : null;

            if (currentTarget != null)
            {
                _lastTrackedTarget = currentTarget;
            }

            if (_wasAimActive && !isAimActive)
            {
                if (!IsPlantingModeActive())
                {
                    TryPerformReleaseHit(currentTarget != null ? currentTarget : _lastTrackedTarget);
                }

                _lastTrackedTarget = null;
            }

            _aimInput = nextAimInput;
            _wasAimActive = isAimActive;
        }

        private void OnHeavyInteract(HeavyInteractInput message)
        {
            if (_characterController == null || _aimTargetingSystem == null)
            {
                return;
            }

            if (IsPlantingModeActive())
            {
                return;
            }

            if (!TryPerformHit(_aimTargetingSystem.CurrentTarget))
            {
                TryPerformHit(_lastTrackedTarget);
            }
        }

        private void TryPerformReleaseHit(AimTargetable releaseTarget)
        {
            if (TryPerformHit(releaseTarget))
            {
                return;
            }

            TryPerformHit(_lastTrackedTarget);
        }

        private bool TryPerformHit(AimTargetable target)
        {
            if (_characterController == null || target == null)
            {
                return false;
            }

            if (!CanHitTarget(target))
            {
                return false;
            }

            if (!target.TryGetHitReceiver(out var hitReceiver))
            {
                return false;
            }

            if (!hitReceiver.CanReceiveHit)
            {
                return false;
            }

            var resolvedHit = _damageResolver.ResolveCurrentHit();
            var origin = _characterController.transform.position;
            var hitDirection = target.ResolveDistanceReferencePosition(ResolveTargetAnchorHeightBias()) - origin;
            hitDirection.y = 0f;

            if (hitDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                hitDirection = _characterController.transform.forward;
            }

            var hitContext = new PlayerHitContext(
                _characterController.transform,
                origin,
                hitDirection.normalized,
                resolvedHit.AxeTier,
                resolvedHit.Damage);

            hitReceiver.ReceiveHit(in hitContext);
            _equippedToolDurabilityService?.TryConsumeEquippedToolDurability(target.name, 1);
            return true;
        }

        private bool CanHitTarget(AimTargetable target)
        {
            if (target == null || !target.isActiveAndEnabled || !target.gameObject.activeInHierarchy)
            {
                return false;
            }

            var origin = _characterController.transform.position;
            var toTarget = target.ResolveDistanceReferencePosition(ResolveTargetAnchorHeightBias()) - origin;
            toTarget.y = 0f;
            var maxRadius = _config != null ? _config.MaxAimRadius : 2f;
            return toTarget.sqrMagnitude <= maxRadius * maxRadius;
        }

        private float ResolveFacingRotationSpeed()
        {
            return _config != null ? _config.FacingRotationSpeed : 14f;
        }

        private float ResolveSelectionDeadZone()
        {
            return _config != null ? _config.SelectionDeadZone : 0.08f;
        }

        private float ResolveTargetAnchorHeightBias()
        {
            return _config != null ? _config.TargetAnchorHeightBias : 0.9f;
        }

        private bool IsPlantingModeActive()
        {
            return _playerAimModeQuery != null && _playerAimModeQuery.IsPlantingModeActive;
        }
    }
}
