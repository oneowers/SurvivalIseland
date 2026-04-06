// Path: Assets/Project/Scpripts/Ghosts/Configs/GhostSpawnerConfig.cs
// Purpose: Stores tunable settings for ghost spawning and behavior.
// Dependencies: GhostPresenter, UnityEngine.

using UnityEngine;

namespace ProjectResonance.Ghosts
{
    /// <summary>
    /// ScriptableObject with ghost spawning and combat settings.
    /// </summary>
    [CreateAssetMenu(fileName = "GhostSpawnerConfig", menuName = "Project Resonance/Ghosts/Ghost Spawner Config")]
    public sealed class GhostSpawnerConfig : ScriptableObject
    {
        [SerializeField]
        private GhostPresenter _ghostPrefab;

        [SerializeField]
        [Min(0.1f)]
        private float _spawnIntervalSeconds = 8f;

        [SerializeField]
        [Min(1)]
        private int _maxAliveGhosts = 8;

        [SerializeField]
        [Min(1)]
        private int _defaultPoolCapacity = 8;

        [SerializeField]
        [Min(1)]
        private int _spawnPositionAttempts = 8;

        [SerializeField]
        [Min(0.1f)]
        private float _movementSpeed = 2.5f;

        [SerializeField]
        [Min(0.1f)]
        private float _retreatSpeedMultiplier = 1.5f;

        [SerializeField]
        [Min(0.1f)]
        private float _attackRange = 1.5f;

        [SerializeField]
        [Min(1f)]
        private float _attackDamage = 10f;

        [SerializeField]
        [Min(0.1f)]
        private float _attackIntervalSeconds = 1.5f;

        [SerializeField]
        [Min(0f)]
        private float _minSpawnDistanceFromPlayer = 10f;

        [SerializeField]
        [Min(0f)]
        private float _minDistanceFromCampfire = 2f;

        /// <summary>
        /// Gets the ghost prefab used by the object pool.
        /// </summary>
        public GhostPresenter GhostPrefab => _ghostPrefab;

        /// <summary>
        /// Gets the spawn interval in seconds.
        /// </summary>
        public float SpawnIntervalSeconds => _spawnIntervalSeconds;

        /// <summary>
        /// Gets the maximum allowed number of alive ghosts.
        /// </summary>
        public int MaxAliveGhosts => _maxAliveGhosts;

        /// <summary>
        /// Gets the default object pool capacity.
        /// </summary>
        public int DefaultPoolCapacity => _defaultPoolCapacity;

        /// <summary>
        /// Gets the number of attempts used to find a valid spawn position.
        /// </summary>
        public int SpawnPositionAttempts => _spawnPositionAttempts;

        /// <summary>
        /// Gets the movement speed used while chasing the player.
        /// </summary>
        public float MovementSpeed => _movementSpeed;

        /// <summary>
        /// Gets the speed multiplier used while retreating from the campfire.
        /// </summary>
        public float RetreatSpeedMultiplier => _retreatSpeedMultiplier;

        /// <summary>
        /// Gets the ghost attack range.
        /// </summary>
        public float AttackRange => _attackRange;

        /// <summary>
        /// Gets the damage dealt per ghost attack.
        /// </summary>
        public float AttackDamage => _attackDamage;

        /// <summary>
        /// Gets the interval between ghost attacks in seconds.
        /// </summary>
        public float AttackIntervalSeconds => _attackIntervalSeconds;

        /// <summary>
        /// Gets the minimum allowed spawn distance from the player.
        /// </summary>
        public float MinSpawnDistanceFromPlayer => _minSpawnDistanceFromPlayer;

        /// <summary>
        /// Gets the extra distance ghosts keep away from the campfire when spawning.
        /// </summary>
        public float MinDistanceFromCampfire => _minDistanceFromCampfire;
    }
}
