// Path: Assets/Project/Scripts/Inventory/ItemVisualFactory.cs
// Purpose: Centralizes safe item visual instantiation for held items, crafting previews, and world-pickup fallback visuals.
// Dependencies: UnityEngine, VContainer, ItemDefinition, ProjectResonance.ResourceNodes.

using ProjectResonance.ResourceNodes;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ProjectResonance.Inventory
{
    /// <summary>
    /// Creates safe runtime visuals for inventory-backed items.
    /// </summary>
    public interface IItemVisualFactory
    {
        /// <summary>
        /// Creates a held-item visual for the provided item definition.
        /// </summary>
        /// <param name="itemDefinition">Item definition to visualize.</param>
        /// <returns>Instantiated held-item visual or a placeholder when the item has no valid prefab.</returns>
        GameObject CreateHeldVisual(ItemDefinition itemDefinition);

        /// <summary>
        /// Creates a crafting-preview visual for the provided item definition.
        /// </summary>
        /// <param name="itemDefinition">Item definition to visualize.</param>
        /// <returns>Instantiated craft-preview visual or a placeholder when the item has no valid prefab.</returns>
        GameObject CreateCraftPreviewVisual(ItemDefinition itemDefinition);

        /// <summary>
        /// Tries to create a fallback world visual for a pooled item pickup.
        /// </summary>
        /// <param name="itemDefinition">Item definition to visualize.</param>
        /// <param name="parent">Parent transform that should own the created visual.</param>
        /// <param name="instance">Created visual instance when successful.</param>
        /// <returns>True when a valid world visual was created.</returns>
        bool TryCreatePickupFallbackVisual(ItemDefinition itemDefinition, Transform parent, out GameObject instance);
    }

    /// <summary>
    /// Default DI-backed implementation that safely instantiates item visuals.
    /// </summary>
    public sealed class ItemVisualFactory : IItemVisualFactory
    {
        private readonly IObjectResolver _resolver;

        /// <summary>
        /// Creates a new item visual factory.
        /// </summary>
        /// <param name="resolver">Object resolver used to instantiate DI-aware prefabs.</param>
        public ItemVisualFactory(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        /// <inheritdoc />
        public GameObject CreateHeldVisual(ItemDefinition itemDefinition)
        {
            return CreateRequiredVisual(itemDefinition, "HeldItem", null);
        }

        /// <inheritdoc />
        public GameObject CreateCraftPreviewVisual(ItemDefinition itemDefinition)
        {
            return CreateRequiredVisual(itemDefinition, "CraftedPreview", null);
        }

        /// <inheritdoc />
        public bool TryCreatePickupFallbackVisual(ItemDefinition itemDefinition, Transform parent, out GameObject instance)
        {
            if (!TryInstantiateItemPrefab(itemDefinition, parent, "Pickup", out instance))
            {
                return false;
            }

            if (instance != null)
            {
                instance.name = $"{ResolveDisplayName(itemDefinition)}_PickupVisual";
            }

            return instance != null;
        }

        private GameObject CreateRequiredVisual(ItemDefinition itemDefinition, string fallbackName, Transform parent)
        {
            if (TryInstantiateItemPrefab(itemDefinition, parent, fallbackName, out var instance))
            {
                return instance;
            }

            var placeholderName = $"{ResolveDisplayName(itemDefinition)}_{fallbackName}";
            return new GameObject(placeholderName);
        }

        private bool TryInstantiateItemPrefab(ItemDefinition itemDefinition, Transform parent, string usageContext, out GameObject instance)
        {
            instance = null;

            if (itemDefinition == null || !itemDefinition.TryGetWorldPrefab(out var worldPrefab) || worldPrefab == null)
            {
                return false;
            }

            if (ContainsGameplayOnlyComponents(worldPrefab))
            {
                Debug.LogWarning(
                    $"[ItemVisualFactory] '{itemDefinition.DisplayName}' uses gameplay prefab '{worldPrefab.name}' as WorldPrefab. {usageContext} visuals require a clean visual prefab, not a resource-node prefab.",
                    itemDefinition);
                return false;
            }

            try
            {
                instance = _resolver != null
                    ? _resolver.Instantiate(worldPrefab, parent, false)
                    : Object.Instantiate(worldPrefab, parent, false);
            }
            catch (VContainerException exception)
            {
                Debug.LogWarning(
                    $"[ItemVisualFactory] Failed to DI-instantiate WorldPrefab '{worldPrefab.name}' for item '{ResolveDisplayName(itemDefinition)}' while creating {usageContext} visual. Assign a visual-only prefab. Exception={exception.Message}",
                    itemDefinition);
                instance = null;
                return false;
            }
            catch (MissingReferenceException)
            {
                Debug.LogWarning(
                    $"[ItemVisualFactory] Missing WorldPrefab on item '{ResolveDisplayName(itemDefinition)}' while creating {usageContext} visual. Reassign ItemDefinition._worldPrefab.",
                    itemDefinition);
                instance = null;
                return false;
            }

            return instance != null;
        }

        private static bool ContainsGameplayOnlyComponents(GameObject worldPrefab)
        {
            return worldPrefab.GetComponentInChildren<ResourceNodeRuntime>(true) != null
                   || worldPrefab.GetComponentInChildren<ResourceNodeAuthoring>(true) != null;
        }

        private static string ResolveDisplayName(ItemDefinition itemDefinition)
        {
            return itemDefinition != null ? itemDefinition.DisplayName : "Item";
        }
    }
}
