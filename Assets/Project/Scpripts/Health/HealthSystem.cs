// Path: Assets/Project/Scpripts/Health/HealthSystem.cs
// Purpose: Owns player health state and publishes health-related messages.
// Dependencies: HealthConfig, MessagePipe, VContainer.

using MessagePipe;
using ProjectResonance.Common.Messages;
using VContainer.Unity;

namespace ProjectResonance.Health
{
    /// <summary>
    /// Stores and mutates runtime player health.
    /// </summary>
    public sealed class HealthSystem : IHealthService, IStartable
    {
        private readonly HealthConfig _config;
        private readonly IBufferedPublisher<HealthChangedMessage> _healthChangedPublisher;
        private readonly IPublisher<HealthDepletedMessage> _healthDepletedPublisher;

        private float _currentHealth;

        /// <summary>
        /// Initializes the health system.
        /// </summary>
        /// <param name="config">Health configuration.</param>
        /// <param name="healthChangedPublisher">Buffered health state publisher.</param>
        /// <param name="healthDepletedPublisher">Health depleted publisher.</param>
        public HealthSystem(
            HealthConfig config,
            IBufferedPublisher<HealthChangedMessage> healthChangedPublisher,
            IPublisher<HealthDepletedMessage> healthDepletedPublisher)
        {
            _config = config;
            _healthChangedPublisher = healthChangedPublisher;
            _healthDepletedPublisher = healthDepletedPublisher;
            _currentHealth = config.MaxHealth;
        }

        /// <summary>
        /// Gets the current player health.
        /// </summary>
        public float CurrentHealth => _currentHealth;

        /// <summary>
        /// Gets the maximum player health.
        /// </summary>
        public float MaxHealth => _config.MaxHealth;

        /// <summary>
        /// Gets whether the player is alive.
        /// </summary>
        public bool IsAlive => _currentHealth > 0f;

        /// <summary>
        /// Publishes the initial health state when the container starts.
        /// </summary>
        public void Start()
        {
            _healthChangedPublisher.Publish(new HealthChangedMessage(_currentHealth, _config.MaxHealth, 0f));
        }

        /// <summary>
        /// Applies incoming damage to the player.
        /// </summary>
        /// <param name="amount">Damage amount to subtract from health.</param>
        public void ApplyDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            var previousHealth = _currentHealth;
            _currentHealth = UnityEngine.Mathf.Max(0f, _currentHealth - amount);

            _healthChangedPublisher.Publish(new HealthChangedMessage(_currentHealth, _config.MaxHealth, _currentHealth - previousHealth));

            if (_currentHealth <= 0f)
            {
                _healthDepletedPublisher.Publish(new HealthDepletedMessage());
            }
        }
    }
}
