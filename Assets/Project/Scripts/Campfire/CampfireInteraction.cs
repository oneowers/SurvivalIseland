// Path: Assets/Project/Scpripts/Campfire/CampfireInteraction.cs
// Purpose: Converts player interactions into campfire fuel, menu and ignition actions.
// Dependencies: UniTask, Unity Input System, TreeFelling.IInteractable, CampfireSystem, UnityEngine, VContainer.

using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectResonance.TreeFelling;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace ProjectResonance.Campfire
{
    /// <summary>
    /// Read-only campfire-specific inventory contract.
    /// </summary>
    public interface ICampfireInventoryQuery
    {
        /// <summary>
        /// Gets whether the inventory currently contains flint.
        /// </summary>
        bool HasFlint();

        /// <summary>
        /// Gets whether the inventory currently contains firesteel.
        /// </summary>
        bool HasFiresteel();

        /// <summary>
        /// Gets whether any valid ignition source is available.
        /// </summary>
        /// <returns>True when ignition is possible.</returns>
        bool HasIgnitionSource();
    }

    /// <summary>
    /// Write-capable campfire inventory contract.
    /// </summary>
    public interface ICampfireInventoryWriteService
    {
        /// <summary>
        /// Attempts to consume logs from the inventory.
        /// </summary>
        /// <param name="amount">Number of logs to consume.</param>
        /// <returns>True when enough logs were available.</returns>
        bool TryConsumeLog(int amount);
    }

    /// <summary>
    /// Handles campfire world interaction rules.
    /// </summary>
    [AddComponentMenu("Project Resonance/Campfire/Campfire Interaction")]
    [DisallowMultipleComponent]
    public sealed class CampfireInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField]
        [Min(0.1f)]
        private float _contextHoldDurationSeconds = 0.6f;

        [SerializeField]
        private CampfireSavePoint _campfireSavePoint;

        private ICampfireService _campfireService;
        private CampfireConfig _campfireConfig;
        private ICampfireInventoryQuery _inventoryQuery;
        private ICampfireInventoryWriteService _inventoryWriteService;
        private bool _isInteractionRunning;

        [Inject]
        private void Construct(
            ICampfireService campfireService,
            CampfireConfig campfireConfig,
            IInventoryQuery inventoryQuery = null,
            IInventoryWriteService inventoryWriteService = null)
        {
            _campfireService = campfireService;
            _campfireConfig = campfireConfig;
            _inventoryQuery = inventoryQuery as ICampfireInventoryQuery;
            _inventoryWriteService = inventoryWriteService as ICampfireInventoryWriteService;
        }

        private void Reset()
        {
            _campfireSavePoint = GetComponent<CampfireSavePoint>();
        }

        /// <summary>
        /// Handles the default interact input.
        /// </summary>
        /// <param name="context">Interaction context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async operation handle.</returns>
        public async UniTask InteractAsync(InteractionContext context, CancellationToken cancellationToken = default)
        {
            if (_isInteractionRunning)
            {
                return;
            }

            _isInteractionRunning = true;

            try
            {
                var holdReached = await WaitForContextHoldAsync(cancellationToken);
                if (holdReached)
                {
                    if (_campfireSavePoint != null)
                    {
                        await _campfireSavePoint.OpenMenuAsync(cancellationToken);
                    }

                    return;
                }

                TryAddFuel();
            }
            finally
            {
                _isInteractionRunning = false;
            }
        }

        /// <summary>
        /// Handles the ignition input.
        /// </summary>
        /// <param name="context">Interaction context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async operation handle.</returns>
        public UniTask HeavyInteractAsync(InteractionContext context, CancellationToken cancellationToken = default)
        {
            if (_inventoryQuery != null && _inventoryQuery.HasIgnitionSource())
            {
                _campfireService.Ignite();
            }

            return UniTask.CompletedTask;
        }

        private void TryAddFuel()
        {
            if (_inventoryWriteService == null || _campfireConfig == null)
            {
                return;
            }

            if (_inventoryWriteService.TryConsumeLog(1))
            {
                _campfireService.AddFuel(_campfireConfig.FuelPerLog);
            }
        }

        private async UniTask<bool> WaitForContextHoldAsync(CancellationToken cancellationToken)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.eKey.isPressed)
            {
                return false;
            }

            var holdReached = false;
            var holdStartTime = Time.unscaledTime;

            await UniTask.WaitUntil(
                () =>
                {
                    if (!keyboard.eKey.isPressed)
                    {
                        return true;
                    }

                    if (Time.unscaledTime - holdStartTime >= _contextHoldDurationSeconds)
                    {
                        holdReached = true;
                        return true;
                    }

                    return false;
                },
                PlayerLoopTiming.Update,
                cancellationToken);

            return holdReached;
        }
    }
}
