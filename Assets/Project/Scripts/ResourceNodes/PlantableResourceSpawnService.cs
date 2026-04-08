// Path: Assets/Project/Scripts/ResourceNodes/PlantableResourceSpawnService.cs
// Purpose: Spawns plantable resource prefabs through the resource-node lifetime scope so injected runtime nodes stay fully functional.
// Dependencies: UnityEngine, VContainer.Unity, ProjectResonance.Inventory, ProjectResonance.ResourceNodes.

using ProjectResonance.Inventory;
using UnityEngine;
using VContainer.Unity;

namespace ProjectResonance.ResourceNodes
{
    /// <summary>
    /// Spawns resource-node prefabs through the active resource-node lifetime scope.
    /// </summary>
    public sealed class PlantableResourceSpawnService
    {
        private ResourceNodeLifetimeScope _resourceNodeLifetimeScope;

        /// <summary>
        /// Attempts to spawn a plantable resource node from the provided definition.
        /// </summary>
        /// <param name="definition">Plantable resource definition.</param>
        /// <param name="position">World spawn position.</param>
        /// <param name="rotation">World spawn rotation.</param>
        /// <param name="spawnedObject">Resolved spawned object when successful.</param>
        /// <returns>True when the resource node was spawned successfully.</returns>
        public bool TrySpawn(ItemDefinition definition, Vector3 position, Quaternion rotation, out GameObject spawnedObject)
        {
            spawnedObject = null;

            if (definition == null)
            {
                Debug.LogWarning("[PlantableResourceSpawnService] TrySpawn ignored because definition is null.");
                return false;
            }

            if (!definition.IsResourceNode || !definition.CanPlantFromInventory)
            {
                Debug.LogWarning($"[PlantableResourceSpawnService] TrySpawn ignored because '{definition.DisplayName}' is not plantable.");
                return false;
            }

            if (!definition.TryGetPlantSpawnPrefab(out var plantSpawnPrefab))
            {
                Debug.LogWarning($"[PlantableResourceSpawnService] TrySpawn ignored because '{definition.DisplayName}' is missing PlantSpawnPrefab.");
                return false;
            }

            var lifetimeScope = ResolveResourceNodeLifetimeScope();
            if (lifetimeScope == null)
            {
                Debug.LogWarning("[PlantableResourceSpawnService] TrySpawn failed because no active ResourceNodeLifetimeScope was found.");
                return false;
            }

            spawnedObject = lifetimeScope.InstantiateResourceNode(plantSpawnPrefab, position, rotation);
            if (spawnedObject == null)
            {
                Debug.LogWarning($"[PlantableResourceSpawnService] TrySpawn failed for '{definition.DisplayName}' because scope instantiation returned null.");
                return false;
            }

            Debug.Log($"[PlantableResourceSpawnService] Spawned '{spawnedObject.name}' for plantable resource '{definition.DisplayName}' at {position}.");
            return true;
        }

        private ResourceNodeLifetimeScope ResolveResourceNodeLifetimeScope()
        {
            if (_resourceNodeLifetimeScope != null && _resourceNodeLifetimeScope.Container != null)
            {
                return _resourceNodeLifetimeScope;
            }

            _resourceNodeLifetimeScope = LifetimeScope.Find<ResourceNodeLifetimeScope>() as ResourceNodeLifetimeScope;
            return _resourceNodeLifetimeScope != null && _resourceNodeLifetimeScope.Container != null
                ? _resourceNodeLifetimeScope
                : null;
        }
    }
}
