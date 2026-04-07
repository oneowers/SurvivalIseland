// Path: Assets/Project/Scpripts/Campfire/CampfireProtectionZone.cs
// Purpose: Periodically scans the campfire safe zone and publishes player/ghost presence events.
// Dependencies: UniTask, MessagePipe, Physics, PlayerSurvivor, GhostPresenter, UnityEngine, VContainer.

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using ProjectResonance.Ghosts;
using ProjectResonance.Health;
using UnityEngine;
using VContainer;

namespace ProjectResonance.Campfire
{
    /// <summary>
    /// Published when the player enters or leaves the campfire safe zone.
    /// </summary>
    public readonly struct PlayerInSafeZoneEvent
    {
        /// <summary>
        /// Creates a new safe-zone event.
        /// </summary>
        /// <param name="isInside">Whether the player is currently inside the safe zone.</param>
        public PlayerInSafeZoneEvent(bool isInside)
        {
            IsInside = isInside;
        }

        /// <summary>
        /// Gets whether the player is inside the safe zone.
        /// </summary>
        public bool IsInside { get; }
    }

    /// <summary>
    /// Published for ghosts detected inside the campfire light zone.
    /// </summary>
    public readonly struct GhostInLightEvent
    {
        /// <summary>
        /// Creates a new ghost-in-light event.
        /// </summary>
        /// <param name="ghost">Detected ghost instance.</param>
        public GhostInLightEvent(GhostPresenter ghost)
        {
            Ghost = ghost;
        }

        /// <summary>
        /// Gets the detected ghost.
        /// </summary>
        public GhostPresenter Ghost { get; }
    }

    /// <summary>
    /// Periodic protection-zone scanner for the campfire.
    /// </summary>
    [AddComponentMenu("Project Resonance/Campfire/Campfire Protection Zone")]
    [DisallowMultipleComponent]
    public sealed class CampfireProtectionZone : MonoBehaviour
    {
        [Header("Scan")]
        [SerializeField]
        [Min(0.1f)]
        private float _scanIntervalSeconds = 0.5f;

        [SerializeField]
        private LayerMask _ghostLayerMask = ~0;

        [SerializeField]
        [Min(4)]
        private int _overlapBufferSize = 32;

        private readonly List<GhostPresenter> _ghostBuffer = new List<GhostPresenter>(16);

        private CampfireState _campfireState;
        private CampfireAnchor _campfireAnchor;
        private PlayerSurvivor _playerSurvivor;
        private IBufferedPublisher<PlayerInSafeZoneEvent> _playerInSafeZonePublisher;
        private IPublisher<GhostInLightEvent> _ghostInLightPublisher;
        private Collider[] _overlapResults;
        private bool _wasPlayerInside;
        private bool _hasPublishedPlayerState;

        [Inject]
        private void Construct(
            CampfireState campfireState,
            CampfireAnchor campfireAnchor,
            PlayerSurvivor playerSurvivor,
            IBufferedPublisher<PlayerInSafeZoneEvent> playerInSafeZonePublisher,
            IPublisher<GhostInLightEvent> ghostInLightPublisher)
        {
            _campfireState = campfireState;
            _campfireAnchor = campfireAnchor;
            _playerSurvivor = playerSurvivor;
            _playerInSafeZonePublisher = playerInSafeZonePublisher;
            _ghostInLightPublisher = ghostInLightPublisher;
        }

        private void Awake()
        {
            _overlapResults = new Collider[Mathf.Max(4, _overlapBufferSize)];
        }

        private void Start()
        {
            ScanProtectionZone();
            RunProtectionLoopAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid RunProtectionLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ScanProtectionZone();

                await UniTask.Delay(
                    (int)(_scanIntervalSeconds * 1000f),
                    DelayType.DeltaTime,
                    PlayerLoopTiming.Update,
                    cancellationToken);
            }
        }

        private void ScanProtectionZone()
        {
            if (_campfireState == null || _campfireAnchor == null || _playerSurvivor == null)
            {
                return;
            }

            if (!_campfireState.IsLit || _campfireState.ProtectionRadius <= 0f)
            {
                if (_wasPlayerInside || !_hasPublishedPlayerState)
                {
                    _wasPlayerInside = false;
                    _hasPublishedPlayerState = true;
                    _playerInSafeZonePublisher?.Publish(new PlayerInSafeZoneEvent(false));
                }

                return;
            }

            var center = _campfireAnchor.FirePoint.position;
            var radius = _campfireState.ProtectionRadius;
            var isPlayerInside = IsInsideSafeZone(center, radius);

            if (!_hasPublishedPlayerState || isPlayerInside != _wasPlayerInside)
            {
                _wasPlayerInside = isPlayerInside;
                _hasPublishedPlayerState = true;
                _playerInSafeZonePublisher?.Publish(new PlayerInSafeZoneEvent(isPlayerInside));
            }

            var hitCount = Physics.OverlapSphereNonAlloc(
                center,
                radius,
                _overlapResults,
                _ghostLayerMask,
                QueryTriggerInteraction.Collide);

            _ghostBuffer.Clear();

            // A ghost can have multiple colliders, so we collapse duplicate hits before publishing.
            for (var index = 0; index < hitCount; index++)
            {
                var ghost = _overlapResults[index] != null ? _overlapResults[index].GetComponentInParent<GhostPresenter>() : null;
                if (ghost == null || _ghostBuffer.Contains(ghost))
                {
                    continue;
                }

                _ghostBuffer.Add(ghost);
                _ghostInLightPublisher?.Publish(new GhostInLightEvent(ghost));
            }
        }

        private bool IsInsideSafeZone(Vector3 center, float radius)
        {
            var playerPosition = _playerSurvivor.Position;
            playerPosition.y = center.y;
            center.y = playerPosition.y;
            return Vector3.SqrMagnitude(playerPosition - center) <= radius * radius;
        }
    }
}
