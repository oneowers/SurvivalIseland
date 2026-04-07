// Path: Assets/Project/Scpripts/Ghosts/GhostBase.cs
// Purpose: Provides pooled night-only ghost movement, damage, and light-response primitives shared by all ghost enemies.
// Dependencies: GhostLightDetector, GhostSpawnConfig, ICampfireService, IDayNightService, IHealthService, PlayerSurvivor, UnityEngine.Pool, VContainer.

using System.Collections.Generic;
using ProjectResonance.Campfire;
using ProjectResonance.DayNight;
using ProjectResonance.Health;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;

namespace ProjectResonance.Ghosts
{
    /// <summary>
    /// Shared pooled ghost behavior used by all ghost enemy variants.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(GhostLightDetector))]
    [DisallowMultipleComponent]
    public abstract class GhostBase : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField]
        [Min(0.1f)]
        private float _detectionRadius = 16f;

        [SerializeField]
        [Min(0.1f)]
        private float _fearRadius = 10f;

        [Header("Collision")]
        [SerializeField]
        [Min(0.1f)]
        private float _environmentIgnoreRefreshSeconds = 0.5f;

        [SerializeField]
        [Min(4)]
        private int _environmentOverlapBufferSize = 32;

        [Header("Combat")]
        [SerializeField]
        [Min(0.05f)]
        private float _contactDamageIntervalSeconds = 0.75f;

        private readonly List<Collider> _ignoredEnvironmentColliders = new List<Collider>(32);

        private CharacterController _characterController;
        private GhostLightDetector _lightDetector;
        private CharacterController _playerCharacterController;
        private PlayerSurvivor _playerSurvivor;
        private IHealthService _healthService;
        private ICampfireService _campfireService;
        private IDayNightService _dayNightService;
        private GhostSpawnConfig _config;
        private ObjectPool<GhostBase> _ownerPool;
        private Collider[] _selfColliders;
        private Collider[] _environmentOverlapBuffer;
        private Vector3 _initialScale;
        private float _environmentIgnoreRefreshTimer;
        private float _nextContactDamageTime;
        private int _environmentLayer = -1;
        private int _lastDamageFrame = -1;

        /// <summary>
        /// Gets the authored detection radius used by sensors and pursuit logic.
        /// </summary>
        public float DetectionRadius => _detectionRadius;

        /// <summary>
        /// Gets the authored fear radius used when strong light should force a retreat.
        /// </summary>
        public float FearRadius => _fearRadius;

        /// <summary>
        /// Gets whether the ghost is currently active in the scene.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Gets the current world-space player anchor.
        /// </summary>
        protected PlayerSurvivor PlayerSurvivor => _playerSurvivor;

        /// <summary>
        /// Gets the player health service used for contact damage.
        /// </summary>
        protected IHealthService HealthService => _healthService;

        /// <summary>
        /// Gets the runtime campfire service.
        /// </summary>
        protected ICampfireService CampfireService => _campfireService;

        /// <summary>
        /// Gets the runtime day/night service.
        /// </summary>
        protected IDayNightService DayNightService => _dayNightService;

        /// <summary>
        /// Gets the shared ghost configuration asset.
        /// </summary>
        protected GhostSpawnConfig Config => _config;

        /// <summary>
        /// Gets the character controller used for non-NavMesh movement.
        /// </summary>
        protected CharacterController CharacterController => _characterController;

        /// <summary>
        /// Gets the attached light detector.
        /// </summary>
        protected GhostLightDetector LightDetector => _lightDetector;

        [Inject]
        private void Construct(
            PlayerSurvivor playerSurvivor,
            IHealthService healthService,
            ICampfireService campfireService,
            IDayNightService dayNightService,
            GhostSpawnConfig config)
        {
            _playerSurvivor = playerSurvivor;
            _healthService = healthService;
            _campfireService = campfireService;
            _dayNightService = dayNightService;
            _config = config;
        }

        /// <summary>
        /// Binds the owner pool used when the ghost despawns.
        /// </summary>
        /// <param name="ownerPool">Owning pool instance.</param>
        public void BindPool(ObjectPool<GhostBase> ownerPool)
        {
            _ownerPool = ownerPool;
        }

        /// <summary>
        /// Activates the ghost at the provided world position.
        /// </summary>
        /// <param name="spawnPosition">World position used when the ghost spawns.</param>
        public virtual void Activate(Vector3 spawnPosition)
        {
            ResetIgnoredEnvironmentCollisions();

            if (_characterController != null)
            {
                _characterController.enabled = false;
            }

            transform.position = spawnPosition;
            transform.localScale = _initialScale;

            if (_characterController != null)
            {
                _characterController.enabled = true;
                _characterController.detectCollisions = false;
            }

            IsActive = IsNightPhase(_dayNightService != null ? _dayNightService.CurrentTimeOfDay : TimeOfDay.Night);
            _environmentIgnoreRefreshTimer = 0f;
            _lastDamageFrame = -1;
            _nextContactDamageTime = 0f;

            gameObject.SetActive(true);
            OnActivated();
            RefreshIgnoredEnvironmentCollisions();
        }

        /// <summary>
        /// Returns the ghost to the owning object pool.
        /// </summary>
        public void ReturnToPool()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            ResetIgnoredEnvironmentCollisions();
            if (_lightDetector != null)
            {
                _lightDetector.ResetSnapshot();
            }

            OnReturnedToPool();

            if (_ownerPool != null)
            {
                _ownerPool.Release(this);
                return;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Legacy alias kept for compatibility with older scene code.
        /// </summary>
        public void Despawn()
        {
            ReturnToPool();
        }

        /// <summary>
        /// Receives the summed normalized light intensity detected by the local light detector.
        /// </summary>
        /// <param name="lightIntensity">Normalized light intensity in the [0..1] range.</param>
        public abstract void OnLightDetected(float lightIntensity);

        /// <summary>
        /// Applies contact damage to the player health system.
        /// </summary>
        /// <param name="amount">Damage amount to apply.</param>
        public void DealDamage(float amount)
        {
            _healthService?.ApplyDamage(amount);
        }

        /// <summary>
        /// Gets the contact-damage amount for the current ghost type.
        /// </summary>
        protected abstract float ContactDamage { get; }

        /// <summary>
        /// Runs the ghost-specific behavior each frame while active.
        /// </summary>
        /// <param name="deltaTime">Current frame delta time.</param>
        protected abstract void TickGhost(float deltaTime);

        /// <summary>
        /// Called whenever the ghost becomes active from the pool.
        /// </summary>
        protected virtual void OnActivated()
        {
        }

        /// <summary>
        /// Called before the ghost is returned to the pool.
        /// </summary>
        protected virtual void OnReturnedToPool()
        {
        }

        /// <summary>
        /// Gets whether this ghost may remain active in the current frame.
        /// </summary>
        /// <returns>True when the ghost should remain alive in the scene.</returns>
        protected virtual bool CanRemainActive()
        {
            return _dayNightService != null && IsNightPhase(_dayNightService.CurrentTimeOfDay);
        }

        /// <summary>
        /// Moves the ghost toward a target position without using NavMesh.
        /// </summary>
        /// <param name="targetPosition">World-space movement target.</param>
        /// <param name="speed">Movement speed in units per second.</param>
        /// <param name="deltaTime">Current frame delta time.</param>
        protected void MoveTowards(Vector3 targetPosition, float speed, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            var currentPosition = transform.position;
            targetPosition.y = currentPosition.y;

            var nextPosition = Vector3.MoveTowards(currentPosition, targetPosition, speed * deltaTime);
            MoveBy(nextPosition - currentPosition);
        }

        /// <summary>
        /// Moves the ghost along a planar direction without using NavMesh.
        /// </summary>
        /// <param name="direction">Planar movement direction.</param>
        /// <param name="speed">Movement speed in units per second.</param>
        /// <param name="deltaTime">Current frame delta time.</param>
        protected void MoveInDirection(Vector3 direction, float speed, float deltaTime)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon || deltaTime <= 0f)
            {
                return;
            }

            MoveBy(direction.normalized * (speed * deltaTime));
        }

        /// <summary>
        /// Rotates the ghost to face the provided planar direction.
        /// </summary>
        /// <param name="direction">Planar look direction.</param>
        protected void FaceDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (Mathf.Approximately(direction.sqrMagnitude, 0f))
            {
                return;
            }

            transform.forward = direction.normalized;
        }

        /// <summary>
        /// Gets the player's current planar distance from the ghost.
        /// </summary>
        /// <returns>Planar distance to the player in world units.</returns>
        protected float GetDistanceToPlayer()
        {
            return GetPlanarDistance(transform.position, _playerSurvivor != null ? _playerSurvivor.Position : transform.position);
        }

        /// <summary>
        /// Gets the planar distance between two world positions.
        /// </summary>
        /// <param name="from">First world position.</param>
        /// <param name="to">Second world position.</param>
        /// <returns>Planar distance in world units.</returns>
        protected static float GetPlanarDistance(Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to);
        }

        /// <summary>
        /// Gets whether the supplied world position is inside the active campfire light radius.
        /// </summary>
        /// <param name="worldPosition">World position to evaluate.</param>
        /// <returns>True when the campfire is lit and the position is inside its light radius.</returns>
        protected bool IsInsideCampfireLight(Vector3 worldPosition)
        {
            if (_campfireService == null || !_campfireService.IsLit || _campfireService.LightRadius <= 0f)
            {
                return false;
            }

            return GetPlanarDistance(worldPosition, _campfireService.Position) <= _campfireService.LightRadius;
        }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _lightDetector = GetComponent<GhostLightDetector>();
            _selfColliders = GetComponentsInChildren<Collider>(true);
            _environmentOverlapBuffer = new Collider[Mathf.Max(4, _environmentOverlapBufferSize)];
            _environmentLayer = LayerMask.NameToLayer("Environment");
            _initialScale = transform.localScale;

            if (_characterController != null)
            {
                _characterController.detectCollisions = false;
                _characterController.enableOverlapRecovery = false;
            }

            // Trigger colliders let the ghost damage the player while still phasing through level geometry.
            for (var index = 0; index < _selfColliders.Length; index++)
            {
                var selfCollider = _selfColliders[index];
                if (selfCollider == null || selfCollider is CharacterController)
                {
                    continue;
                }

                selfCollider.isTrigger = true;
            }
        }

        private void Update()
        {
            if (!IsActive)
            {
                return;
            }

            if (_healthService != null && !_healthService.IsAlive)
            {
                ReturnToPool();
                return;
            }

            if (!CanRemainActive())
            {
                ReturnToPool();
                return;
            }

            _environmentIgnoreRefreshTimer -= Time.deltaTime;
            if (_environmentIgnoreRefreshTimer <= 0f)
            {
                RefreshIgnoredEnvironmentCollisions();
                _environmentIgnoreRefreshTimer = _environmentIgnoreRefreshSeconds;
            }

            TickGhost(Time.deltaTime);
            TryDealContactDamage();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsActive || other == null || Time.frameCount == _lastDamageFrame)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerSurvivor>() == null)
            {
                return;
            }

            ApplyContactDamage();
        }

        private void MoveBy(Vector3 displacement)
        {
            displacement.y = 0f;

            if (Mathf.Approximately(displacement.sqrMagnitude, 0f))
            {
                return;
            }

            if (_characterController != null && _characterController.enabled)
            {
                _characterController.Move(displacement);
            }
            else
            {
                transform.position += displacement;
            }

            // Use the intended planar movement vector, because CharacterController.Move can legitimately end on the same
            // position and would otherwise feed a zero vector into Transform.forward.
            FaceDirection(displacement);
        }

        private void TryDealContactDamage()
        {
            if (!IsActive || _playerSurvivor == null || Time.frameCount == _lastDamageFrame || Time.time < _nextContactDamageTime)
            {
                return;
            }

            var playerPosition = _playerSurvivor.Position;
            var contactRadius = GetContactDistance();

            if (contactRadius <= 0f)
            {
                return;
            }

            if (GetPlanarDistance(transform.position, playerPosition) > contactRadius)
            {
                return;
            }

            ApplyContactDamage();
        }

        private void ApplyContactDamage()
        {
            if (Time.frameCount == _lastDamageFrame || Time.time < _nextContactDamageTime)
            {
                return;
            }

            _lastDamageFrame = Time.frameCount;
            _nextContactDamageTime = Time.time + _contactDamageIntervalSeconds;
            DealDamage(ContactDamage);
        }

        private float GetContactDistance()
        {
            var ghostRadius = _characterController != null ? _characterController.radius : 0f;

            if (_selfColliders != null)
            {
                for (var index = 0; index < _selfColliders.Length; index++)
                {
                    var selfCollider = _selfColliders[index];
                    if (selfCollider == null || !selfCollider.enabled || selfCollider is CharacterController)
                    {
                        continue;
                    }

                    var extents = selfCollider.bounds.extents;
                    ghostRadius = Mathf.Max(ghostRadius, Mathf.Max(extents.x, extents.z));
                }
            }

            var playerController = ResolvePlayerCharacterController();
            var playerRadius = playerController != null ? playerController.radius : 0f;
            return ghostRadius + playerRadius;
        }

        private CharacterController ResolvePlayerCharacterController()
        {
            if (_playerCharacterController != null)
            {
                return _playerCharacterController;
            }

            _playerCharacterController = _playerSurvivor != null ? _playerSurvivor.GetComponentInParent<CharacterController>() : null;
            return _playerCharacterController;
        }

        private void RefreshIgnoredEnvironmentCollisions()
        {
            if (_environmentLayer < 0 || _selfColliders == null || _selfColliders.Length == 0)
            {
                return;
            }

            ResetIgnoredEnvironmentCollisions();

            var hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                Mathf.Max(_detectionRadius, _fearRadius),
                _environmentOverlapBuffer,
                1 << _environmentLayer,
                QueryTriggerInteraction.Ignore);

            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var environmentCollider = _environmentOverlapBuffer[hitIndex];
                if (environmentCollider == null)
                {
                    continue;
                }

                for (var selfIndex = 0; selfIndex < _selfColliders.Length; selfIndex++)
                {
                    var selfCollider = _selfColliders[selfIndex];
                    if (selfCollider == null)
                    {
                        continue;
                    }

                    Physics.IgnoreCollision(selfCollider, environmentCollider, true);
                }

                _ignoredEnvironmentColliders.Add(environmentCollider);
            }
        }

        private void ResetIgnoredEnvironmentCollisions()
        {
            if (_ignoredEnvironmentColliders.Count == 0 || _selfColliders == null)
            {
                _ignoredEnvironmentColliders.Clear();
                return;
            }

            for (var hitIndex = 0; hitIndex < _ignoredEnvironmentColliders.Count; hitIndex++)
            {
                var environmentCollider = _ignoredEnvironmentColliders[hitIndex];
                if (environmentCollider == null)
                {
                    continue;
                }

                for (var selfIndex = 0; selfIndex < _selfColliders.Length; selfIndex++)
                {
                    var selfCollider = _selfColliders[selfIndex];
                    if (selfCollider == null)
                    {
                        continue;
                    }

                    Physics.IgnoreCollision(selfCollider, environmentCollider, false);
                }
            }

            _ignoredEnvironmentColliders.Clear();
        }

        private static bool IsNightPhase(TimeOfDay timeOfDay)
        {
            return timeOfDay == TimeOfDay.Night || timeOfDay == TimeOfDay.PreDawn;
        }
    }
}
