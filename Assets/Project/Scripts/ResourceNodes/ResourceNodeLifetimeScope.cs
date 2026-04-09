// Path: Assets/Project/Scripts/ResourceNodes/ResourceNodeLifetimeScope.cs
// Purpose: Registers the generic resource-node gameplay module in VContainer and injects existing authored resource nodes in the scene.
// Dependencies: VContainer, UnityEngine, ProjectResonance.ResourceNodes, ProjectResonance.TreeFelling.

using ProjectResonance.TreeFelling;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ProjectResonance.ResourceNodes
{
    /// <summary>
    /// Scene lifetime scope for generic resource-node gameplay.
    /// </summary>
    [AddComponentMenu("Project Resonance/Resource Nodes/Resource Node Lifetime Scope")]
    [DisallowMultipleComponent]
    public sealed class ResourceNodeLifetimeScope : LifetimeScope
    {
        [SerializeField]
        private MonoBehaviour _inventoryBridgeSource;

        [SerializeField]
        private PlayerCarryAnchorAdapter _playerCarryAnchorAdapter;

        /// <summary>
        /// Registers generic resource-node services, scene adapters, and message brokers.
        /// </summary>
        /// <param name="builder">Current container builder.</param>
        protected override void Configure(IContainerBuilder builder)
        {
            if (_inventoryBridgeSource is IInventoryQuery inventoryQuery)
            {
                builder.RegisterInstance(inventoryQuery);
            }

            if (_inventoryBridgeSource is IInventoryWriteService inventoryWriteService)
            {
                builder.RegisterInstance(inventoryWriteService);
            }

            if (_playerCarryAnchorAdapter != null)
            {
                builder.RegisterComponent(_playerCarryAnchorAdapter).As<IPlayerCarryAnchor>();
            }

            builder.RegisterBuildCallback(container =>
            {
                container.InjectGameObject(gameObject);
                InjectExistingResourceNodes(container);
            });
        }

        private void InjectExistingResourceNodes(IObjectResolver container)
        {
            var resourceNodes = FindObjectsByType<ResourceNodeRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var index = 0; index < resourceNodes.Length; index++)
            {
                var resourceNode = resourceNodes[index];
                if (resourceNode != null)
                {
                    container.InjectGameObject(resourceNode.gameObject);
                }
            }
        }

        /// <summary>
        /// Instantiates a resource-node prefab through the resource-node scope so all gameplay dependencies are injected.
        /// </summary>
        /// <param name="prefab">Prefab to spawn.</param>
        /// <param name="position">World spawn position.</param>
        /// <param name="rotation">World spawn rotation.</param>
        /// <returns>Injected resource-node instance, or null when the scope is unavailable.</returns>
        public GameObject InstantiateResourceNode(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                return null;
            }

            if (Container == null)
            {
                return null;
            }

            return Container.Instantiate(prefab, position, rotation);
        }
    }
}
