// Path: Assets/Project/Scripts/ResourceNodes/ResourceNodeMessages.cs
// Purpose: Defines generic pub/sub messages for authored resource-node interaction, hit response, and destruction flow.
// Dependencies: UnityEngine, ProjectResonance.Inventory, ProjectResonance.ResourceNodes.

using ProjectResonance.Inventory;
using UnityEngine;

namespace ProjectResonance.ResourceNodes
{
    /// <summary>
    /// Requests damage application to a specific resource node.
    /// </summary>
    public readonly struct ResourceHitRequestEvent
    {
        /// <summary>
        /// Creates a new resource hit request event.
        /// </summary>
        /// <param name="target">Target resource node.</param>
        /// <param name="hitDirection">World-space hit direction.</param>
        /// <param name="damage">Damage dealt by the hit.</param>
        public ResourceHitRequestEvent(ResourceNodeRuntime target, Vector3 hitDirection, int damage)
        {
            Target = target;
            HitDirection = hitDirection;
            Damage = damage;
        }

        /// <summary>
        /// Gets the target resource node.
        /// </summary>
        public ResourceNodeRuntime Target { get; }

        /// <summary>
        /// Gets the world-space hit direction.
        /// </summary>
        public Vector3 HitDirection { get; }

        /// <summary>
        /// Gets the damage dealt by the hit.
        /// </summary>
        public int Damage { get; }
    }

    /// <summary>
    /// Broadcasts a successful hit on a resource node.
    /// </summary>
    public readonly struct ResourceHitEvent
    {
        /// <summary>
        /// Creates a new resource hit event.
        /// </summary>
        /// <param name="target">Hit resource node.</param>
        /// <param name="currentHealth">Remaining health after the hit.</param>
        /// <param name="maxHealth">Maximum health of the node.</param>
        /// <param name="strikeCount">Total strike count applied so far.</param>
        /// <param name="accumulatedHitDirection">Accumulated planar hit direction.</param>
        /// <param name="hitPoint">Transform used for decal placement.</param>
        public ResourceHitEvent(
            ResourceNodeRuntime target,
            int currentHealth,
            int maxHealth,
            int strikeCount,
            Vector3 accumulatedHitDirection,
            Transform hitPoint)
        {
            Target = target;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            StrikeCount = strikeCount;
            AccumulatedHitDirection = accumulatedHitDirection;
            HitPoint = hitPoint;
        }

        /// <summary>
        /// Gets the hit resource node.
        /// </summary>
        public ResourceNodeRuntime Target { get; }

        /// <summary>
        /// Gets the remaining health after the hit.
        /// </summary>
        public int CurrentHealth { get; }

        /// <summary>
        /// Gets the node maximum health.
        /// </summary>
        public int MaxHealth { get; }

        /// <summary>
        /// Gets the total strike count applied so far.
        /// </summary>
        public int StrikeCount { get; }

        /// <summary>
        /// Gets the accumulated planar hit direction.
        /// </summary>
        public Vector3 AccumulatedHitDirection { get; }

        /// <summary>
        /// Gets the transform used for hit visuals.
        /// </summary>
        public Transform HitPoint { get; }
    }

    /// <summary>
    /// Broadcasts when a resource node reaches zero health and enters its destroy flow.
    /// </summary>
    public readonly struct ResourceDestroyedEvent
    {
        /// <summary>
        /// Creates a new resource destroyed event.
        /// </summary>
        /// <param name="target">Destroyed resource node.</param>
        /// <param name="dropItemDefinition">Item dropped by the node.</param>
        /// <param name="dropCount">Amount of spawned drops.</param>
        public ResourceDestroyedEvent(ResourceNodeRuntime target, ItemDefinition dropItemDefinition, int dropCount)
        {
            Target = target;
            DropItemDefinition = dropItemDefinition;
            DropCount = dropCount;
        }

        /// <summary>
        /// Gets the destroyed resource node.
        /// </summary>
        public ResourceNodeRuntime Target { get; }

        /// <summary>
        /// Gets the item dropped by the node.
        /// </summary>
        public ItemDefinition DropItemDefinition { get; }

        /// <summary>
        /// Gets the total amount of spawned drops.
        /// </summary>
        public int DropCount { get; }
    }
}
