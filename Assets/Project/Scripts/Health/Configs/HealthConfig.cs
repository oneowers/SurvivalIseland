// Path: Assets/Project/Scpripts/Health/Configs/HealthConfig.cs
// Purpose: Stores tunable player health settings.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.Health
{
    /// <summary>
    /// ScriptableObject with player health settings.
    /// </summary>
    [CreateAssetMenu(fileName = "HealthConfig", menuName = "Project Resonance/Health/Health Config")]
    public sealed class HealthConfig : ScriptableObject
    {
        [SerializeField]
        [Min(1f)]
        private float _maxHealth = 100f;

        /// <summary>
        /// Gets the maximum player health.
        /// </summary>
        public float MaxHealth => _maxHealth;
    }
}
