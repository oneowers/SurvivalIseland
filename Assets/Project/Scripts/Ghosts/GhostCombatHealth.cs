// Path: Assets/Project/Scripts/Ghosts/GhostCombatHealth.cs
// Purpose: Adds simple pooled combat health to ghost prefabs so they can participate in the generic aim-hit pipeline.
// Dependencies: UnityEngine, ProjectResonance.Ghosts.

using UnityEngine;

namespace ProjectResonance.Ghosts
{
    /// <summary>
    /// Runtime combat health for a pooled ghost enemy.
    /// </summary>
    [AddComponentMenu("Project Resonance/Ghosts/Ghost Combat Health")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GhostBase))]
    public sealed class GhostCombatHealth : MonoBehaviour
    {
        [SerializeField]
        [Min(1)]
        private int _maxHealth = 3;

        [SerializeField]
        private GhostBase _ghost;

        private int _currentHealth;

        /// <summary>
        /// Gets the current runtime health.
        /// </summary>
        public int CurrentHealth => _currentHealth;

        /// <summary>
        /// Gets the authored maximum health.
        /// </summary>
        public int MaxHealth => Mathf.Max(1, _maxHealth);

        /// <summary>
        /// Gets whether the ghost can currently receive combat damage.
        /// </summary>
        public bool IsAlive => gameObject.activeInHierarchy && _currentHealth > 0;

        /// <summary>
        /// Applies player damage and despawns the ghost when health reaches zero.
        /// </summary>
        /// <param name="damage">Incoming damage amount.</param>
        public void ApplyDamage(int damage)
        {
            if (damage <= 0 || !gameObject.activeInHierarchy)
            {
                return;
            }

            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            if (_currentHealth > 0)
            {
                return;
            }

            if (_ghost != null)
            {
                _ghost.ReturnToPool();
            }
        }

        private void Awake()
        {
            EnsureReferences();
            ResetHealth();
        }

        private void OnEnable()
        {
            ResetHealth();
        }

        private void Reset()
        {
            EnsureReferences();
        }

        private void OnValidate()
        {
            _maxHealth = Mathf.Max(1, _maxHealth);
            EnsureReferences();
        }

        private void ResetHealth()
        {
            _currentHealth = MaxHealth;
        }

        private void EnsureReferences()
        {
            if (_ghost == null)
            {
                _ghost = GetComponent<GhostBase>();
            }
        }
    }
}
