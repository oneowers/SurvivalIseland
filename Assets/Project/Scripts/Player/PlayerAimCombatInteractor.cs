// Path: Assets/Project/Scripts/Player/PlayerAimCombatInteractor.cs
// Purpose: Rotates the player toward the selected aim target and applies hits when the mobile HIT action is pressed.
// Dependencies: MessagePipe, UnityEngine, VContainer.Unity, ProjectResonance.PlayerInput, ProjectResonance.PlayerCombat.

using System;
using MessagePipe;
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
        private readonly IBufferedSubscriber<AimInput> _aimInputSubscriber;
        private readonly ISubscriber<HeavyInteractInput> _heavyInteractSubscriber;

        private IDisposable _aimInputSubscription;
        private IDisposable _heavyInteractSubscription;
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
        /// <param name="aimInputSubscriber">Buffered aim input subscriber.</param>
        /// <param name="heavyInteractSubscriber">Heavy interact subscriber used by the HIT button.</param>
        public PlayerAimCombatInteractor(
            CharacterController characterController,
            AimTargetingSystem aimTargetingSystem,
            AimTargetingConfig config,
            PlayerHitDamageResolver damageResolver,
            EquippedToolDurabilityService equippedToolDurabilityService,
            IPlayerAimModeQuery playerAimModeQuery,
            IBufferedSubscriber<AimInput> aimInputSubscriber,
            ISubscriber<HeavyInteractInput> heavyInteractSubscriber)
        {
            _characterController = characterController;
            _aimTargetingSystem = aimTargetingSystem;
            _config = config;
            _damageResolver = damageResolver;
            _equippedToolDurabilityService = equippedToolDurabilityService;
            _playerAimModeQuery = playerAimModeQuery;
            _aimInputSubscriber = aimInputSubscriber;
            _heavyInteractSubscriber = heavyInteractSubscriber;
        }

        /// <summary>
        /// Starts listening for aim and hit input messages.
        /// </summary>
        public void Start()
        {
            _aimInputSubscription = _aimInputSubscriber.Subscribe(OnAimInputChanged);
            _heavyInteractSubscription = _heavyInteractSubscriber.Subscribe(OnHeavyInteract);
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
            _aimInputSubscription?.Dispose();
            _aimInputSubscription = null;

            _heavyInteractSubscription?.Dispose();
            _heavyInteractSubscription = null;
        }

        private void OnAimInputChanged(AimInput message)
        {
            var nextAimInput = Vector2.ClampMagnitude(message.Value, 1f);
            var isAimActive = nextAimInput.sqrMagnitude > ResolveSelectionDeadZone() * ResolveSelectionDeadZone();

            if (_wasAimActive && !isAimActive)
            {
                if (!IsPlantingModeActive())
                {
                    TryPerformReleaseHit();
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

        private void TryPerformReleaseHit()
        {
            var currentTarget = _aimTargetingSystem != null ? _aimTargetingSystem.CurrentTarget : null;
            if (TryPerformHit(currentTarget))
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

            if (!target.TryGetHitReceiver(out var hitReceiver) || !hitReceiver.CanReceiveHit)
            {
                return false;
            }

            var resolvedHit = _damageResolver.ResolveCurrentHit();
            var origin = _characterController.transform.position;
            var hitDirection = target.ResolveAnchorPosition(ResolveTargetAnchorHeightBias()) - origin;
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
            Debug.Log($"[PlayerAimCombatInteractor] Successful hit on '{target.name}'. Damage={resolvedHit.Damage}, AxeTier={resolvedHit.AxeTier}");
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
            var toTarget = target.ResolveAnchorPosition(ResolveTargetAnchorHeightBias()) - origin;
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
