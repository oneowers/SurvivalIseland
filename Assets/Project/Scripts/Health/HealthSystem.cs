// Path: Assets/Project/Scpripts/Health/HealthSystem.cs
// Purpose: Owns player health state and exposes health-related runtime events.
// Dependencies: HealthConfig, VContainer.

using System;
using ProjectResonance.Common.Messages;
using VContainer.Unity;

namespace ProjectResonance.Health
{
    /// <summary>
    /// Stores and mutates runtime player health.
    /// </summary>
    public sealed class HealthSystem : IHealthService, IStartable, IDisposable
    {
        private readonly HealthConfig _config;
        private float _currentHealth;

        /// <summary>
        /// Initializes the health system.
        /// </summary>
        /// <param name="config">Health configuration.</param>
        public HealthSystem(HealthConfig config)
        {
            _config = config;
            _currentHealth = config.MaxHealth;
        }

        /// <inheritdoc />
        public event Action<HealthChangedMessage> HealthChanged;

        /// <inheritdoc />
        public event Action<HealthDepletedMessage> HealthDepleted;

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
            HealthChanged?.Invoke(new HealthChangedMessage(_currentHealth, _config.MaxHealth, 0f));
        }

        /// <summary>
        /// Stops runtime subscriptions owned by the health system.
        /// </summary>
        public void Dispose()
        {
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

            HealthChanged?.Invoke(new HealthChangedMessage(_currentHealth, _config.MaxHealth, _currentHealth - previousHealth));

            if (_currentHealth <= 0f)
            {
                HealthDepleted?.Invoke(new HealthDepletedMessage());
            }
        }

        /// <summary>
        /// Applies incoming healing to the player.
        /// </summary>
        /// <param name="amount">Healing amount to add to health.</param>
        public void ApplyHealing(float amount)
        {
            if (!IsAlive || amount <= 0f || _currentHealth >= _config.MaxHealth)
            {
                return;
            }

            var previousHealth = _currentHealth;
            _currentHealth = UnityEngine.Mathf.Min(_config.MaxHealth, _currentHealth + amount);
            HealthChanged?.Invoke(new HealthChangedMessage(_currentHealth, _config.MaxHealth, _currentHealth - previousHealth));
        }
    }
}
