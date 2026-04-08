// Path: Assets/Project/Scripts/ResourceNodes/ResourceNodeLifetimeScope.cs
// Purpose: Registers the generic resource-node gameplay module in VContainer and injects existing authored resource nodes in the scene.
// Dependencies: MessagePipe, VContainer, UnityEngine, ProjectResonance.ResourceNodes, ProjectResonance.TreeFelling, ProjectResonance.PlayerWeight.

using System;
using MessagePipe;
using ProjectResonance.PlayerWeight;
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
        [Header("Debug")]
        [SerializeField]
        private bool _enableDebugLogs = true;

        [Header("Adapters")]
        [SerializeField]
        private PlayerWeightState _playerWeightState;

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
            if (_enableDebugLogs)
            {
                Debug.Log($"[ResourceNodeLifetimeScope] Configure started. PlayerWeightAssigned={_playerWeightState != null}, InventoryBridgeAssigned={_inventoryBridgeSource != null}", this);
            }

            if (_playerWeightState != null)
            {
                builder.RegisterInstance(_playerWeightState);
            }

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

            RegisterMessagePipe(builder);

            builder.RegisterBuildCallback(container =>
            {
                container.InjectGameObject(gameObject);
                InjectExistingResourceNodes(container);

                if (_enableDebugLogs)
                {
                    Debug.Log("[ResourceNodeLifetimeScope] Build callback completed. Existing resource nodes were injected.", this);
                }
            });
        }

        private void RegisterMessagePipe(IContainerBuilder builder)
        {
            var messagePipeBuilder = new BuiltinContainerBuilder();
            messagePipeBuilder.AddMessagePipe();
            messagePipeBuilder.AddMessageBroker<ResourceHitRequestEvent>();
            messagePipeBuilder.AddMessageBroker<ResourceHitEvent>();
            messagePipeBuilder.AddMessageBroker<ResourceDestroyedEvent>();
            messagePipeBuilder.AddMessageBroker<SoundEvent>();
            messagePipeBuilder.AddMessageBroker<ParticleEvent>();

            var serviceProvider = messagePipeBuilder.BuildServiceProvider();

            RegisterMessage<ResourceHitRequestEvent>(builder, serviceProvider);
            RegisterMessage<ResourceHitEvent>(builder, serviceProvider);
            RegisterMessage<ResourceDestroyedEvent>(builder, serviceProvider);
            RegisterMessage<SoundEvent>(builder, serviceProvider);
            RegisterMessage<ParticleEvent>(builder, serviceProvider);
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

        private void RegisterMessage<TMessage>(IContainerBuilder builder, IServiceProvider serviceProvider)
        {
            builder.RegisterInstance((IPublisher<TMessage>)serviceProvider.GetService(typeof(IPublisher<TMessage>)));
            builder.RegisterInstance((ISubscriber<TMessage>)serviceProvider.GetService(typeof(ISubscriber<TMessage>)));
        }
    }
}
