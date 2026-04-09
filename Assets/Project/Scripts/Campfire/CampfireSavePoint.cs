// Path: Assets/Project/Scpripts/Campfire/CampfireSavePoint.cs
// Purpose: Provides campfire save-point behavior, sleep requests and upgrade menu handling.
// Dependencies: UniTask, UnityEngine, VContainer, CampfireSystem, IDayNightService.

using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectResonance.DayNight;
using UnityEngine;
using VContainer;

namespace ProjectResonance.Campfire
{
    /// <summary>
    /// Common save-point contract used by respawn systems.
    /// </summary>
    public interface ISavePoint
    {
        /// <summary>
        /// Gets the world transform where the player should respawn.
        /// </summary>
        Transform RespawnTransform { get; }

        /// <summary>
        /// Opens the save-point menu.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async operation handle.</returns>
        UniTask OpenMenuAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Respawn-system contract used by campfire save points.
    /// </summary>
    public interface IRespawnService
    {
        /// <summary>
        /// Sets the current active respawn point.
        /// </summary>
        /// <param name="savePoint">Save point to register.</param>
        void SetRespawnPoint(ISavePoint savePoint);
    }

    /// <summary>
    /// Supported campfire menu actions.
    /// </summary>
    public enum CampfireMenuSelection
    {
        /// <summary>
        /// No action was selected.
        /// </summary>
        Cancel = 0,

        /// <summary>
        /// Player requested sleep.
        /// </summary>
        Sleep = 1,

        /// <summary>
        /// Player requested a campfire upgrade.
        /// </summary>
        Upgrade = 2,
    }

    /// <summary>
    /// UI presenter contract used to show the campfire radial or context menu.
    /// </summary>
    public interface ICampfireMenuPresenter
    {
        /// <summary>
        /// Opens the campfire context menu.
        /// </summary>
        /// <param name="savePoint">Save point requesting the menu.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Selected menu action.</returns>
        UniTask<CampfireMenuSelection> OpenAsync(ISavePoint savePoint, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Published when the player wants to sleep at a campfire.
    /// </summary>
    public readonly struct SleepRequestEvent
    {
        /// <summary>
        /// Creates a new sleep request event.
        /// </summary>
        /// <param name="savePoint">Save point used by the player.</param>
        public SleepRequestEvent(ISavePoint savePoint)
        {
            SavePoint = savePoint;
        }

        /// <summary>
        /// Gets the save point used by the player.
        /// </summary>
        public ISavePoint SavePoint { get; }
    }

    /// <summary>
    /// Campfire save-point behavior that opens the sleep or upgrade menu.
    /// </summary>
    [AddComponentMenu("Project Resonance/Campfire/Campfire Save Point")]
    [DisallowMultipleComponent]
    public sealed class CampfireSavePoint : MonoBehaviour, ISavePoint
    {
        [SerializeField]
        private Transform _respawnTransform;

        private ICampfireService _campfireService;
        private IRespawnService _respawnService;
        private ICampfireMenuPresenter _menuPresenter;
        private IDayNightService _dayNightService;

        /// <summary>
        /// Raised when the player chooses to sleep at this campfire.
        /// </summary>
        public event System.Action<SleepRequestEvent> SleepRequested;

        [Inject]
        private void Construct(
            ICampfireService campfireService,
            IDayNightService dayNightService,
            IRespawnService respawnService = null,
            ICampfireMenuPresenter menuPresenter = null)
        {
            _campfireService = campfireService;
            _respawnService = respawnService;
            _menuPresenter = menuPresenter;
            _dayNightService = dayNightService;
        }

        /// <summary>
        /// Gets the transform used as the respawn point.
        /// </summary>
        public Transform RespawnTransform => _respawnTransform != null ? _respawnTransform : transform;

        /// <summary>
        /// Opens the campfire sleep or upgrade menu and registers this fire as the active respawn point.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async operation handle.</returns>
        public async UniTask OpenMenuAsync(CancellationToken cancellationToken = default)
        {
            _respawnService?.SetRespawnPoint(this);

            if (_menuPresenter == null)
            {
                return;
            }

            var selection = await _menuPresenter.OpenAsync(this, cancellationToken);
            switch (selection)
            {
                case CampfireMenuSelection.Sleep:
                    _dayNightService?.SkipToMorning();
                    SleepRequested?.Invoke(new SleepRequestEvent(this));
                    break;
                case CampfireMenuSelection.Upgrade:
                    _campfireService?.TryUpgrade();
                    break;
            }
        }
    }
}
