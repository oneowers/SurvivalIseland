// Path: Assets/Project/Scpripts/Ghosts/GhostPresenter.cs
// Purpose: Controls the movement and attacks of a pooled ghost instance.
// Dependencies: GhostSpawnerConfig, ICampfireService, IHealthService, PlayerSurvivor, UnityEngine.Pool, VContainer.

using ProjectResonance.Campfire;
using ProjectResonance.Health;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;

namespace ProjectResonance.Ghosts
{
    /// <summary>
    /// Runtime behavior for a pooled ghost actor.
    /// </summary>
    public sealed class GhostPresenter : MonoBehaviour
    {
        private PlayerSurvivor _player;
        private IHealthService _healthService;
        private ICampfireService _campfireService;
        private GhostSpawnerConfig _config;
        private ObjectPool<GhostPresenter> _ownerPool;

        private bool _isActive;
        private float _nextAttackTime;

        [Inject]
        private void Construct(
            PlayerSurvivor player,
            IHealthService healthService,
            ICampfireService campfireService,
            GhostSpawnerConfig config)
        {
            _player = player;
            _healthService = healthService;
            _campfireService = campfireService;
            _config = config;
        }

        /// <summary>
        /// Binds the owning object pool used for returning this ghost.
        /// </summary>
        /// <param name="ownerPool">Owning object pool.</param>
        public void BindPool(ObjectPool<GhostPresenter> ownerPool)
        {
            _ownerPool = ownerPool;
        }

        /// <summary>
        /// Activates the ghost at the provided world position.
        /// </summary>
        /// <param name="spawnPosition">World position where the ghost appears.</param>
        public void Activate(Vector3 spawnPosition)
        {
            transform.position = spawnPosition;
            _nextAttackTime = Time.time;
            _isActive = true;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Returns the ghost to its owner pool.
        /// </summary>
        public void Despawn()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;

            if (_ownerPool != null)
            {
                _ownerPool.Release(this);
                return;
            }

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isActive || !_healthService.IsAlive)
            {
                return;
            }

            var currentPosition = transform.position;
            var playerPosition = _player.Position;
            var shouldRetreat = _campfireService.IsLit &&
                                GetPlanarDistance(currentPosition, _campfireService.Position) <= _campfireService.ProtectionRadius;

            if (shouldRetreat)
            {
                // Ghosts immediately break aggression when the campfire protection field reaches them.
                var retreatDirection = (currentPosition - _campfireService.Position);
                retreatDirection.y = 0f;

                if (retreatDirection.sqrMagnitude <= Mathf.Epsilon)
                {
                    retreatDirection = Vector3.back;
                }

                Move(currentPosition + retreatDirection.normalized, _config.MovementSpeed * _config.RetreatSpeedMultiplier);
                return;
            }

            Move(playerPosition, _config.MovementSpeed);

            if (GetPlanarDistance(transform.position, playerPosition) > _config.AttackRange)
            {
                return;
            }

            if (Time.time < _nextAttackTime)
            {
                return;
            }

            _healthService.ApplyDamage(_config.AttackDamage);
            _nextAttackTime = Time.time + _config.AttackIntervalSeconds;
        }

        private void Move(Vector3 targetPosition, float speed)
        {
            targetPosition.y = transform.position.y;

            var previousPosition = transform.position;
            transform.position = Vector3.MoveTowards(previousPosition, targetPosition, speed * Time.deltaTime);

            var lookDirection = transform.position - previousPosition;
            if (lookDirection.sqrMagnitude > Mathf.Epsilon)
            {
                transform.forward = lookDirection.normalized;
            }
        }

        private static float GetPlanarDistance(Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to);
        }
    }
}
