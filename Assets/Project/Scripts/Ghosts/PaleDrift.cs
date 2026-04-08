// Path: Assets/Project/Scpripts/Ghosts/PaleDrift.cs
// Purpose: Implements the Pale Drift night ghost with wandering, light-seeking, pursuit, and retreat state transitions.
// Dependencies: GhostBase, GhostLightDetector, GhostSpawnConfig, ICampfireService, PlayerSurvivor, UnityEngine.

using ProjectResonance.Campfire;
using UnityEngine;

namespace ProjectResonance.Ghosts
{
    /// <summary>
    /// Simple state-driven drifting ghost that reacts to weak and strong light.
    /// </summary>
    [AddComponentMenu("Project Resonance/Ghosts/Pale Drift")]
    public class PaleDrift : GhostBase
    {
        private enum PaleDriftState
        {
            Wandering = 0,
            LightSeeking = 1,
            PlayerPursuing = 2,
            Retreating = 3,
        }

        [Header("Wandering")]
        [SerializeField]
        [Min(0.1f)]
        private float _wanderRadius = 6f;

        [SerializeField]
        [Min(0.01f)]
        private float _wanderNoiseFrequency = 0.28f;

        [SerializeField]
        [Min(0.1f)]
        private float _wanderNoiseAmplitude = 1.6f;

        [Header("Light Seeking")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _minimumInterestingLight = 0.05f;

        [Header("Retreat")]
        [SerializeField]
        [Min(0.1f)]
        private float _retreatPerimeterPadding = 1.5f;

        [SerializeField]
        [Min(0.1f)]
        private float _retreatHoldSeconds = 1.8f;

        [Header("Combat")]
        [SerializeField]
        [Min(0f)]
        private float _contactDamage = 15f;

        private PaleDriftState _state;
        private Vector3 _spawnAnchor;
        private float _noiseSeedX;
        private float _noiseSeedZ;
        private float _retreatUntilTime;

        /// <summary>
        /// Gets the current runtime state used by the Pale Drift.
        /// </summary>
        public string CurrentState => _state.ToString();

        /// <summary>
        /// Gets the contact damage dealt by the Pale Drift.
        /// </summary>
        protected override float ContactDamage => _contactDamage;

        /// <summary>
        /// Receives strong light notifications from the detector and forces a retreat state.
        /// </summary>
        /// <param name="lightIntensity">Summed normalized light intensity.</param>
        public override void OnLightDetected(float lightIntensity)
        {
            _retreatUntilTime = Time.time + _retreatHoldSeconds;
            _state = PaleDriftState.Retreating;
        }

        /// <summary>
        /// Resets the Pale Drift runtime state when spawned from the pool.
        /// </summary>
        protected override void OnActivated()
        {
            _spawnAnchor = transform.position;
            _noiseSeedX = Mathf.Abs(transform.position.x) + (GetInstanceID() * 0.0137f);
            _noiseSeedZ = Mathf.Abs(transform.position.z) + (GetInstanceID() * 0.0211f);
            _retreatUntilTime = 0f;
            _state = PaleDriftState.Wandering;
        }

        /// <summary>
        /// Ticks the Pale Drift finite-state machine.
        /// </summary>
        /// <param name="deltaTime">Current frame delta time.</param>
        protected override void TickGhost(float deltaTime)
        {
            _state = ResolveState();

            switch (_state)
            {
                case PaleDriftState.LightSeeking:
                    TickLightSeeking(deltaTime);
                    break;
                case PaleDriftState.PlayerPursuing:
                    TickPlayerPursuit(deltaTime);
                    break;
                case PaleDriftState.Retreating:
                    TickRetreat(deltaTime);
                    break;
                default:
                    TickWandering(deltaTime);
                    break;
            }
        }

        private PaleDriftState ResolveState()
        {
            // Retreat has the highest priority so bright campfires and torch clusters immediately break aggression.
            if (ShouldRetreat())
            {
                return PaleDriftState.Retreating;
            }

            // Player pursuit starts once the survivor leaves the campfire safety perimeter.
            if (ShouldPursuePlayer())
            {
                return PaleDriftState.PlayerPursuing;
            }

            // Weak lights are attractive only when they are not already strong enough to scare the ghost away.
            if (ShouldSeekLight())
            {
                return PaleDriftState.LightSeeking;
            }

            return PaleDriftState.Wandering;
        }

        private bool ShouldRetreat()
        {
            if (Time.time < _retreatUntilTime)
            {
                return true;
            }

            if (CampfireService == null || !CampfireService.IsLit)
            {
                return false;
            }

            // Even a weak campfire remains hostile territory for the Pale Drift once it reaches the lit perimeter.
            if (IsInsideCampfireLight(transform.position))
            {
                return true;
            }

            if (CampfireService.Level < CampfireLevel.Reinforced)
            {
                return false;
            }

            return GetPlanarDistance(transform.position, CampfireService.Position) <= FearRadius;
        }

        private bool ShouldPursuePlayer()
        {
            if (PlayerSurvivor == null || GetDistanceToPlayer() > DetectionRadius)
            {
                return false;
            }

            if (CampfireService == null || !CampfireService.IsLit)
            {
                return true;
            }

            return GetPlanarDistance(PlayerSurvivor.Position, CampfireService.Position) > CampfireService.ProtectionRadius;
        }

        private bool ShouldSeekLight()
        {
            if (LightDetector == null)
            {
                return false;
            }

            if (!LightDetector.TryGetWeakLightTarget(out var lightPosition, out var lightIntensity))
            {
                return false;
            }

            if (lightIntensity < _minimumInterestingLight)
            {
                return false;
            }

            // The campfire is handled as a danger zone, so it must never become an attractive weak-light target.
            return !IsInsideCampfireLight(lightPosition);
        }

        private void TickWandering(float deltaTime)
        {
            var noiseTime = Time.time * _wanderNoiseFrequency;
            var noiseOffset = new Vector3(
                (Mathf.PerlinNoise(_noiseSeedX, noiseTime) * 2f) - 1f,
                0f,
                (Mathf.PerlinNoise(_noiseSeedZ, noiseTime) * 2f) - 1f);

            var driftTarget = _spawnAnchor + (noiseOffset.normalized * _wanderRadius);
            driftTarget += noiseOffset * _wanderNoiseAmplitude;

            MoveTowards(driftTarget, Config != null ? Config.WanderSpeed : 1.5f, deltaTime);
        }

        private void TickLightSeeking(float deltaTime)
        {
            if (LightDetector == null || !LightDetector.TryGetWeakLightTarget(out var lightPosition, out var lightIntensity))
            {
                TickWandering(deltaTime);
                return;
            }

            if (lightIntensity < _minimumInterestingLight || IsInsideCampfireLight(lightPosition))
            {
                TickWandering(deltaTime);
                return;
            }

            MoveTowards(lightPosition, Config != null ? Config.WanderSpeed : 1.5f, deltaTime);
        }

        private void TickPlayerPursuit(float deltaTime)
        {
            if (PlayerSurvivor == null)
            {
                return;
            }

            MoveTowards(PlayerSurvivor.Position, Config != null ? Config.PursuitSpeed : 3.5f, deltaTime);
        }

        private void TickRetreat(float deltaTime)
        {
            if (CampfireService == null || !CampfireService.IsLit)
            {
                TickWandering(deltaTime);
                return;
            }

            var retreatDirection = transform.position - CampfireService.Position;
            retreatDirection.y = 0f;

            if (retreatDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                retreatDirection = transform.forward.sqrMagnitude > Mathf.Epsilon ? transform.forward : Vector3.back;
            }

            // Retreating targets the outer light perimeter so the ghost fully leaves illuminated space instead of
            // stopping inside the campfire glow and immediately re-triggering retreat.
            var perimeterDistance = Mathf.Max(CampfireService.ProtectionRadius, CampfireService.LightRadius) + _retreatPerimeterPadding;
            var retreatTarget = CampfireService.Position + (retreatDirection.normalized * perimeterDistance);
            retreatTarget.y = transform.position.y;

            MoveTowards(retreatTarget, Config != null ? Config.PursuitSpeed : 3.5f, deltaTime);
        }
    }
}
