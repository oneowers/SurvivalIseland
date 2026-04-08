// Path: Assets/Project/Scpripts/Ghosts/Configs/GhostSpawnConfig.cs
// Purpose: Stores authored spawn counts, prefabs, speeds, and light thresholds for the ghost enemy module.
// Dependencies: PaleDrift, LordWraith, UnityEngine.

using UnityEngine;

namespace ProjectResonance.Ghosts
{
    /// <summary>
    /// ScriptableObject with ghost spawning and movement tuning values.
    /// </summary>
    [CreateAssetMenu(fileName = "GhostSpawnConfig", menuName = "Project Resonance/Ghosts/Ghost Spawn Config")]
    public class GhostSpawnConfig : ScriptableObject
    {
        [Header("Prefabs")]
        [SerializeField]
        private PaleDrift _paleDriftPrefab;

        [SerializeField]
        private LordWraith _lordWraithPrefab;

        [Header("Spawn Counts")]
        [SerializeField]
        [Min(1)]
        private int _basePaleDriftCount = 3;

        [SerializeField]
        [Min(0)]
        private int _paleDriftPerDay = 1;

        [SerializeField]
        [Min(1)]
        private int _maxPaleDrifts = 8;

        [Header("Spawn Constraints")]
        [SerializeField]
        [Min(0.1f)]
        private float _minSpawnDistanceFromPlayer = 20f;

        [SerializeField]
        [Min(0f)]
        private float _minSpawnDistanceFromCampfire = 4f;

        [SerializeField]
        [Min(1)]
        private int _spawnPositionAttempts = 12;

        [Header("Movement")]
        [SerializeField]
        [Min(0.1f)]
        private float _wanderSpeed = 1.5f;

        [SerializeField]
        [Min(0.1f)]
        private float _pursuitSpeed = 3.5f;

        [Header("Light")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _fearLightThreshold = 0.6f;

        [Header("Pools")]
        [SerializeField]
        [Min(1)]
        private int _paleDriftPoolCapacity = 8;

        [SerializeField]
        [Min(1)]
        private int _lordWraithPoolCapacity = 1;

        [Header("Lord Wraith")]
        [SerializeField]
        [Min(0.1f)]
        private float _lordWraithPullStrength = 2.5f;

        [SerializeField]
        [Min(0.1f)]
        private float _lordWraithPullRadius = 14f;

        /// <summary>
        /// Gets the Pale Drift prefab used by the spawn pool.
        /// </summary>
        public PaleDrift PaleDriftPrefab => _paleDriftPrefab;

        /// <summary>
        /// Gets the Lord Wraith prefab used by the spawn pool.
        /// </summary>
        public LordWraith LordWraithPrefab => _lordWraithPrefab;

        /// <summary>
        /// Gets the base number of Pale Drifts spawned on the first night.
        /// </summary>
        public int BasePaleDriftCount => _basePaleDriftCount;

        /// <summary>
        /// Gets the extra Pale Drifts added for every new day survived.
        /// </summary>
        public int PaleDriftPerDay => _paleDriftPerDay;

        /// <summary>
        /// Gets the maximum Pale Drift count allowed per night.
        /// </summary>
        public int MaxPaleDrifts => _maxPaleDrifts;

        /// <summary>
        /// Gets the minimum spawn distance from the player.
        /// </summary>
        public float MinSpawnDistanceFromPlayer => _minSpawnDistanceFromPlayer;

        /// <summary>
        /// Gets the minimum spawn distance from the campfire perimeter.
        /// </summary>
        public float MinSpawnDistanceFromCampfire => _minSpawnDistanceFromCampfire;

        /// <summary>
        /// Gets the maximum number of random spawn-position attempts.
        /// </summary>
        public int SpawnPositionAttempts => _spawnPositionAttempts;

        /// <summary>
        /// Gets the default Pale Drift wander speed.
        /// </summary>
        public float WanderSpeed => _wanderSpeed;

        /// <summary>
        /// Gets the default pursuit speed for aggressive ghost movement.
        /// </summary>
        public float PursuitSpeed => _pursuitSpeed;

        /// <summary>
        /// Gets the summed normalized light value that forces a fear response.
        /// </summary>
        public float FearLightThreshold => _fearLightThreshold;

        /// <summary>
        /// Gets the initial Pale Drift object-pool capacity.
        /// </summary>
        public int PaleDriftPoolCapacity => _paleDriftPoolCapacity;

        /// <summary>
        /// Gets the initial Lord Wraith object-pool capacity.
        /// </summary>
        public int LordWraithPoolCapacity => _lordWraithPoolCapacity;

        /// <summary>
        /// Gets the strength used by the Lord Wraith gravity pull event.
        /// </summary>
        public float LordWraithPullStrength => _lordWraithPullStrength;

        /// <summary>
        /// Gets the effective Lord Wraith pull radius.
        /// </summary>
        public float LordWraithPullRadius => _lordWraithPullRadius;

        /// <summary>
        /// Resolves the Pale Drift count for the provided day number.
        /// </summary>
        /// <param name="dayNumber">Current one-based day number.</param>
        /// <returns>Night spawn count clamped to the configured maximum.</returns>
        public int GetPaleDriftCountForDay(int dayNumber)
        {
            var clampedDayNumber = Mathf.Max(1, dayNumber);
            var spawnedCount = _basePaleDriftCount + ((clampedDayNumber - 1) * _paleDriftPerDay);
            return Mathf.Clamp(spawnedCount, 0, _maxPaleDrifts);
        }
    }
}
