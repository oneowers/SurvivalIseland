// Path: Assets/Project/Scpripts/Campfire/CampfireState.cs
// Purpose: Stores the shared runtime campfire state and publishes state transition events.
// Dependencies: CampfireConfig, MessagePipe, UnityEngine.

using MessagePipe;
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
    /// Shared runtime state asset for the campfire gameplay loop.
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

        private CampfireConfig _config;
        private IBufferedPublisher<FuelChangedEvent> _fuelChangedPublisher;
        private IPublisher<CampfireLitEvent> _campfireLitPublisher;
        private IPublisher<CampfireDyingEvent> _campfireDyingPublisher;
        private IPublisher<CampfireExtinguishedEvent> _campfireExtinguishedPublisher;
        private IPublisher<CampfireLevelUpEvent> _campfireLevelUpPublisher;

        /// <summary>
        /// Gets the current campfire level.
        /// </summary>
        public CampfireLevel Level => _level;

        /// <summary>
        /// Gets the current fuel amount.
        /// </summary>
        public float CurrentFuel => _currentFuel;

        /// <summary>
        /// Gets the current maximum fuel capacity.
        /// </summary>
        public float MaxFuel => _maxFuel;

        /// <summary>
        /// Gets whether the campfire is lit.
        /// </summary>
        public bool IsLit => _isLit;

        /// <summary>
        /// Gets whether the campfire is in the dying state.
        /// </summary>
        public bool IsDying
        {
            get
            {
                if (!_isLit || _config == null || _maxFuel <= 0f)
                {
                    return false;
                }

                return CurrentFuelNormalized < _config.DyingThreshold;
            }
        }

        /// <summary>
        /// Gets the normalized fuel value in the [0..1] range.
        /// </summary>
        public float CurrentFuelNormalized => _maxFuel > 0f ? Mathf.Clamp01(_currentFuel / _maxFuel) : 0f;

        /// <summary>
        /// Gets the active protection radius for the current level.
        /// </summary>
        public float ProtectionRadius => ResolveLevelData().ProtectionRadius;

        /// <summary>
        /// Gets the active light radius for the current level.
        /// </summary>
        public float LightRadius => ResolveLevelData().LightRadius;

        /// <summary>
        /// Binds the runtime dependencies used to resolve data and publish events.
        /// </summary>
        /// <param name="config">Campfire configuration asset.</param>
        /// <param name="fuelChangedPublisher">Buffered fuel publisher.</param>
        /// <param name="campfireLitPublisher">Lit event publisher.</param>
        /// <param name="campfireDyingPublisher">Dying event publisher.</param>
        /// <param name="campfireExtinguishedPublisher">Extinguished event publisher.</param>
        /// <param name="campfireLevelUpPublisher">Level-up event publisher.</param>
        public void Initialize(
            CampfireConfig config,
            IBufferedPublisher<FuelChangedEvent> fuelChangedPublisher,
            IPublisher<CampfireLitEvent> campfireLitPublisher,
            IPublisher<CampfireDyingEvent> campfireDyingPublisher,
            IPublisher<CampfireExtinguishedEvent> campfireExtinguishedPublisher,
            IPublisher<CampfireLevelUpEvent> campfireLevelUpPublisher)
        {
            _config = config;
            _fuelChangedPublisher = fuelChangedPublisher;
            _campfireLitPublisher = campfireLitPublisher;
            _campfireDyingPublisher = campfireDyingPublisher;
            _campfireExtinguishedPublisher = campfireExtinguishedPublisher;
            _campfireLevelUpPublisher = campfireLevelUpPublisher;

            RefreshLevelData();

            // Sprint 4 uses the campfire state asset as a scene-start template, so each run begins with a fully fueled active fire.
            if (_level == CampfireLevel.None || _maxFuel <= 0f)
            {
                _currentFuel = 0f;
                _isLit = false;
                return;
            }

            _currentFuel = _maxFuel;
            _isLit = true;
        }

        /// <summary>
        /// Publishes the current state snapshot to late subscribers.
        /// </summary>
        public void PublishSnapshot()
        {
            PublishFuelChanged();
        }

        /// <summary>
        /// Applies a new runtime state snapshot and publishes the resulting transitions.
        /// </summary>
        /// <param name="level">New campfire level.</param>
        /// <param name="currentFuel">New fuel amount.</param>
        /// <param name="isLit">New lit state.</param>
        public void ApplyRuntimeState(CampfireLevel level, float currentFuel, bool isLit)
        {
            var previousLevel = _level;
            var previousLit = _isLit;
            var previousDying = IsDying;

            _level = level;
            RefreshLevelData();

            _currentFuel = Mathf.Clamp(currentFuel, 0f, _maxFuel);
            _isLit = _level != CampfireLevel.None && isLit;

            var currentDying = IsDying;

            if (previousLevel != _level && (int)_level > (int)previousLevel)
            {
                _campfireLevelUpPublisher?.Publish(new CampfireLevelUpEvent(previousLevel, _level));
            }

            if (!previousLit && _isLit)
            {
                _campfireLitPublisher?.Publish(new CampfireLitEvent(_level));
            }

            if (!previousDying && currentDying)
            {
                _campfireDyingPublisher?.Publish(new CampfireDyingEvent(_currentFuel, _maxFuel, _level));
            }

            if (previousLit && !_isLit)
            {
                _campfireExtinguishedPublisher?.Publish(new CampfireExtinguishedEvent(_level));
            }

            PublishFuelChanged();
        }

        private void RefreshLevelData()
        {
            if (_config == null || _level == CampfireLevel.None)
            {
                _maxFuel = 0f;
                return;
            }

            _maxFuel = Mathf.Max(0f, _config.GetLevelData(_level).FuelCapacity);
        }

        private CampfireLevelData ResolveLevelData()
        {
            if (_config == null || _level == CampfireLevel.None)
            {
                return default;
            }

            return _config.GetLevelData(_level);
        }

        private void PublishFuelChanged()
        {
            _fuelChangedPublisher?.Publish(new FuelChangedEvent(_currentFuel, _maxFuel, _level, _isLit, IsDying, ProtectionRadius));
        }
    }
}
