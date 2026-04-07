// Path: Assets/Project/Scpripts/TreeFelling/InventoryWriteAdapter.cs
// Purpose: Provides a simple editor-driven inventory write adapter for collected logs.
// Dependencies: UnityEngine, TreeFelling.

using UnityEngine;

namespace ProjectResonance.TreeFelling
{
    /// <summary>
    /// Inspector-configurable adapter that stores collected logs for the tree module.
    /// </summary>
    [AddComponentMenu("Project Resonance/Tree Felling/Inventory Write Adapter")]
    [DisallowMultipleComponent]
    public sealed class InventoryWriteAdapter : MonoBehaviour, IInventoryWriteService
    {
        [SerializeField]
        [Min(0)]
        private int _maxLogs = 2;

        [SerializeField]
        [Min(0)]
        private int _currentLogs;

        /// <summary>
        /// Gets the current collected log count.
        /// </summary>
        public int CurrentLogs => _currentLogs;

        /// <summary>
        /// Attempts to add logs to the local runtime inventory.
        /// </summary>
        /// <param name="amount">Number of logs to add.</param>
        /// <returns>True when the logs fit into the configured capacity.</returns>
        public bool TryAddLog(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            if (_currentLogs + amount > _maxLogs)
            {
                return false;
            }

            _currentLogs += amount;
            return true;
        }

        /// <summary>
        /// Removes logs from the local runtime inventory.
        /// </summary>
        /// <param name="amount">Number of logs to remove.</param>
        public void RemoveLog(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _currentLogs = Mathf.Max(0, _currentLogs - amount);
        }

        /// <summary>
        /// Clears the local runtime inventory.
        /// </summary>
        public void ResetInventory()
        {
            _currentLogs = 0;
        }
    }
}
