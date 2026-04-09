// Path: Assets/Project/Scpripts/Campfire/CampfireState.cs
// Purpose: Stores authored campfire runtime template data.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.Campfire
{
    /// <summary>
    /// Supported campfire upgrade tiers.
    /// </summary>
    public enum CampfireLevel
    {
        /// <summary>
        /// Campfire is not constructed.
        /// </summary>
        None = 0,

        /// <summary>
        /// Base campfire tier.
        /// </summary>
        Basic = 1,

        /// <summary>
        /// Improved campfire tier with stronger radius and capacity.
        /// </summary>
        Reinforced = 2,

        /// <summary>
        /// Final campfire tier with the strongest signal fire.
        /// </summary>
        Signal = 3,
    }

    /// <summary>
    /// Publishes the latest fuel state for the campfire.
    /// </summary>
    public readonly struct FuelChangedEvent
    {
        /// <summary>
        /// Creates a new fuel changed event.
        /// </summary>
        /// <param name="currentFuel">Current fuel amount.</param>
        /// <param name="maxFuel">Maximum fuel capacity.</param>
        /// <param name="level">Current campfire level.</param>
        /// <param name="isLit">Whether the campfire is lit.</param>
        /// <param name="isDying">Whether the campfire is dying.</param>
        /// <param name="protectionRadius">Current protection radius.</param>
        public FuelChangedEvent(float currentFuel, float maxFuel, CampfireLevel level, bool isLit, bool isDying, float protectionRadius)
        {
            CurrentFuel = currentFuel;
            MaxFuel = maxFuel;
            Level = level;
            IsLit = isLit;
            IsDying = isDying;
            ProtectionRadius = protectionRadius;
        }

        /// <summary>
        /// Gets the current fuel amount.
        /// </summary>
        public float CurrentFuel { get; }

        /// <summary>
        /// Gets the maximum fuel capacity.
        /// </summary>
        public float MaxFuel { get; }

        /// <summary>
        /// Gets the current campfire level.
        /// </summary>
        public CampfireLevel Level { get; }

        /// <summary>
        /// Gets whether the campfire is lit.
        /// </summary>
        public bool IsLit { get; }

        /// <summary>
        /// Gets whether the campfire is dying.
        /// </summary>
        public bool IsDying { get; }

        /// <summary>
        /// Gets the current protection radius.
        /// </summary>
        public float ProtectionRadius { get; }
    }

    /// <summary>
    /// Published when the campfire becomes lit.
    /// </summary>
    public readonly struct CampfireLitEvent
    {
        /// <summary>
        /// Creates a new lit event.
        /// </summary>
        /// <param name="level">Current campfire level.</param>
        public CampfireLitEvent(CampfireLevel level)
        {
            Level = level;
        }

        /// <summary>
        /// Gets the current campfire level.
        /// </summary>
        public CampfireLevel Level { get; }
    }

    /// <summary>
    /// Published when the campfire enters the dying state.
    /// </summary>
    public readonly struct CampfireDyingEvent
    {
        /// <summary>
        /// Creates a new dying event.
        /// </summary>
        /// <param name="currentFuel">Current fuel amount.</param>
        /// <param name="maxFuel">Maximum fuel capacity.</param>
        /// <param name="level">Current campfire level.</param>
        public CampfireDyingEvent(float currentFuel, float maxFuel, CampfireLevel level)
        {
            CurrentFuel = currentFuel;
            MaxFuel = maxFuel;
            Level = level;
        }

        /// <summary>
        /// Gets the current fuel amount.
        /// </summary>
        public float CurrentFuel { get; }

        /// <summary>
        /// Gets the maximum fuel capacity.
        /// </summary>
        public float MaxFuel { get; }

        /// <summary>
        /// Gets the current campfire level.
        /// </summary>
        public CampfireLevel Level { get; }
    }

    /// <summary>
    /// Published when the campfire fully extinguishes.
    /// </summary>
    public readonly struct CampfireExtinguishedEvent
    {
        /// <summary>
        /// Creates a new extinguished event.
        /// </summary>
        /// <param name="level">Current campfire level.</param>
        public CampfireExtinguishedEvent(CampfireLevel level)
        {
            Level = level;
        }

        /// <summary>
        /// Gets the current campfire level.
        /// </summary>
        public CampfireLevel Level { get; }
    }

    /// <summary>
    /// Published when the campfire upgrades to a higher tier.
    /// </summary>
    public readonly struct CampfireLevelUpEvent
    {
        /// <summary>
        /// Creates a new campfire level-up event.
        /// </summary>
        /// <param name="previousLevel">Previous campfire level.</param>
        /// <param name="currentLevel">New campfire level.</param>
        public CampfireLevelUpEvent(CampfireLevel previousLevel, CampfireLevel currentLevel)
        {
            PreviousLevel = previousLevel;
            CurrentLevel = currentLevel;
        }

        /// <summary>
        /// Gets the previous campfire level.
        /// </summary>
        public CampfireLevel PreviousLevel { get; }

        /// <summary>
        /// Gets the new campfire level.
        /// </summary>
        public CampfireLevel CurrentLevel { get; }
    }

    /// <summary>
    /// Authored data asset describing the initial campfire runtime template.
    /// </summary>
    [CreateAssetMenu(fileName = "CampfireState", menuName = "Project Resonance/Campfire/Campfire State")]
    public sealed class CampfireState : ScriptableObject
    {
        [SerializeField]
        private CampfireLevel _level = CampfireLevel.Basic;

        [SerializeField]
        [Min(0f)]
        private float _currentFuel;

        [SerializeField]
        [Min(0f)]
        private float _maxFuel;

        [SerializeField]
        private bool _isLit = true;

        /// <summary>
        /// Gets the authored initial campfire level.
        /// </summary>
        public CampfireLevel InitialLevel => _level;

        /// <summary>
        /// Gets the authored initial fuel amount template.
        /// </summary>
        public float InitialFuel => Mathf.Max(0f, _currentFuel);

        /// <summary>
        /// Gets the authored initial maximum fuel template.
        /// </summary>
        public float InitialMaxFuel => Mathf.Max(0f, _maxFuel);

        /// <summary>
        /// Gets whether the campfire starts lit.
        /// </summary>
        public bool StartsLit => _isLit;

        /// <summary>
        /// Resolves the authored initial fuel ratio for the runtime state bootstrap.
        /// </summary>
        public float ResolveInitialFuelRatio()
        {
            if (_level == CampfireLevel.None)
            {
                return 0f;
            }
            if (InitialMaxFuel > 0f)
            {
                return Mathf.Clamp01(InitialFuel / InitialMaxFuel);
            }

            return StartsLit ? 1f : 0f;
        }
    }
}
