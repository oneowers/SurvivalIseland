// Path: Assets/Project/Scpripts/Ghosts/LordWraith.cs
// Purpose: Implements the Lord Wraith boss ghost with pre-dawn spawn window, player gravity pull, and strong-light retreat behavior.
// Dependencies: GhostBase, GhostSpawnConfig, ICampfireService, IDayNightService, MessagePipe, PlayerGravityPullEvent, UnityEngine, VContainer.

using MessagePipe;
using ProjectResonance.Campfire;
using ProjectResonance.PlayerMovement;
using UnityEngine;
using VContainer;

namespace ProjectResonance.Ghosts
{
    /// <summary>
    /// Heavy pre-dawn ghost that can pull the player and ignore low-tier campfire protection.
    /// </summary>
    [AddComponentMenu("Project Resonance/Ghosts/Lord Wraith")]
    public sealed class LordWraith : GhostBase
    {
        [Header("Combat")]
        [SerializeField]
        [Min(0f)]
        private float _contactDamage = 30f;

        [Header("Retreat")]
        [SerializeField]
        [Min(0.1f)]
        private float _retreatPerimeterPadding = 2f;

        [SerializeField]
        [Min(0.1f)]
        private float _retreatHoldSeconds = 2f;

        [Header("Pull")]
        [SerializeField]
        [Min(0.1f)]
        private float _pullPublishDistanceMultiplier = 1f;

        private IPublisher<PlayerGravityPullEvent> _playerGravityPullPublisher;
        private Vector3 _baseScale;
        private float _retreatUntilTime;

        /// <summary>
        /// Gets the contact damage dealt by the Lord Wraith.
        /// </summary>
        protected override float ContactDamage => _contactDamage;

        [Inject]
        private void Construct(IPublisher<PlayerGravityPullEvent> playerGravityPullPublisher)
        {
            _playerGravityPullPublisher = playerGravityPullPublisher;
        }

        /// <summary>
        /// Receives strong-light notifications from the detector and forces a retreat response.
        /// </summary>
        /// <param name="lightIntensity">Summed normalized light intensity.</param>
        public override void OnLightDetected(float lightIntensity)
        {
            _retreatUntilTime = Time.time + _retreatHoldSeconds;
        }

        /// <summary>
        /// Resets the Lord Wraith runtime state when spawned from the pool.
        /// </summary>
        protected override void OnActivated()
        {
            if (_baseScale == default)
            {
                _baseScale = transform.localScale;
            }

            transform.localScale = _baseScale * 2f;
            _retreatUntilTime = 0f;
        }

        /// <summary>
        /// Restricts the Lord Wraith lifetime to the authored 03:00-04:00 window.
        /// </summary>
        /// <returns>True when the Lord Wraith may remain active.</returns>
        protected override bool CanRemainActive()
        {
            return base.CanRemainActive() && IsWithinSpawnWindow();
        }

        /// <summary>
        /// Ticks the Lord Wraith priority-driven behavior tree.
        /// </summary>
        /// <param name="deltaTime">Current frame delta time.</param>
        protected override void TickGhost(float deltaTime)
        {
            if (ShouldRetreat())
            {
                TickRetreat(deltaTime);
                return;
            }

            TickPlayerPursuit(deltaTime);
            PublishGravityPull();
        }

        private bool ShouldRetreat()
        {
            if (Time.time < _retreatUntilTime)
            {
                return true;
            }

            if (CampfireService != null &&
                CampfireService.IsLit &&
                CampfireService.Level >= CampfireLevel.Reinforced &&
                GetPlanarDistance(transform.position, CampfireService.Position) <= FearRadius)
            {
                return true;
            }

            return LightDetector != null && LightDetector.TotalLightIntensity > (Config != null ? Config.FearLightThreshold : 0.6f);
        }

        private void TickPlayerPursuit(float deltaTime)
        {
            if (PlayerSurvivor == null)
            {
                return;
            }

            // Level 1 campfires are not strong enough to block the Lord Wraith, so it keeps pushing through the camp perimeter.
            MoveTowards(PlayerSurvivor.Position, Config != null ? Config.PursuitSpeed : 3.5f, deltaTime);
        }

        private void TickRetreat(float deltaTime)
        {
            Vector3 retreatTarget;

            if (CampfireService != null && CampfireService.IsLit)
            {
                var retreatDirection = transform.position - CampfireService.Position;
                retreatDirection.y = 0f;

                if (retreatDirection.sqrMagnitude <= Mathf.Epsilon)
                {
                    retreatDirection = transform.forward.sqrMagnitude > Mathf.Epsilon ? transform.forward : Vector3.back;
                }

                // The Lord Wraith has to clear the full lit perimeter, not only the protection radius, otherwise it can
                // remain stuck inside the same fear source that triggered the retreat.
                var retreatDistance = Mathf.Max(CampfireService.ProtectionRadius, CampfireService.LightRadius) + _retreatPerimeterPadding;
                retreatTarget = CampfireService.Position + (retreatDirection.normalized * retreatDistance);
            }
            else
            {
                retreatTarget = transform.position - transform.forward;
            }

            retreatTarget.y = transform.position.y;
            MoveTowards(retreatTarget, Config != null ? Config.PursuitSpeed : 3.5f, deltaTime);
        }

        private void PublishGravityPull()
        {
            if (_playerGravityPullPublisher == null || PlayerSurvivor == null || Config == null)
            {
                return;
            }

            var distanceToPlayer = GetDistanceToPlayer();
            var pullRadius = Config.LordWraithPullRadius * _pullPublishDistanceMultiplier;
            if (distanceToPlayer > pullRadius)
            {
                return;
            }

            _playerGravityPullPublisher.Publish(new PlayerGravityPullEvent(
                transform.position,
                Config.LordWraithPullStrength,
                pullRadius));
        }

        private bool IsWithinSpawnWindow()
        {
            // The global day cycle begins at dawn, so the offset remaps normalized time back to a familiar 24-hour clock.
            var clockHour = Mathf.Repeat((DayNightService != null ? DayNightService.CurrentTimeNormalized : 0f) * 24f + 6f, 24f);
            return clockHour >= 3f && clockHour < 4f;
        }
    }
}
