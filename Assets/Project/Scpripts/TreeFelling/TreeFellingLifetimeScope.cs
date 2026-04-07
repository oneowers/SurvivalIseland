// Path: Assets/Project/Scpripts/TreeFelling/TreeFellingLifetimeScope.cs
// Purpose: Registers the full tree-felling module in VContainer and injects scene trees.
// Dependencies: MessagePipe, VContainer, UnityEngine, TreeFelling, PlayerWeight, Common.Random.

using System;
using MessagePipe;
using ProjectResonance.Common.Random;
using ProjectResonance.PlayerWeight;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ProjectResonance.TreeFelling
{
    /// <summary>
    /// Scene lifetime scope for the tree-felling gameplay module.
    /// </summary>
    [AddComponentMenu("Project Resonance/Tree Felling/Tree Felling Lifetime Scope")]
    [DisallowMultipleComponent]
    public sealed class TreeFellingLifetimeScope : LifetimeScope
    {
        [Header("Debug")]
        [SerializeField]
        private bool _enableDebugLogs = true;

        [Header("Configs")]
        [SerializeField]
        private TreeConfig _treeConfig;

        [SerializeField]
        private TreeFallConfig _treeFallConfig;

        [SerializeField]
        private PlayerWeightState _playerWeightState;

        [Header("Adapters")]
        [SerializeField]
        private InventoryQueryAdapter _inventoryQueryAdapter;

        [SerializeField]
        private InventoryWriteAdapter _inventoryWriteAdapter;

        [SerializeField]
        private PlayerCarryAnchorAdapter _playerCarryAnchorAdapter;

        /// <summary>
        /// Registers tree-felling services, scene adapters and message brokers.
        /// </summary>
        /// <param name="builder">Current container builder.</param>
        protected override void Configure(IContainerBuilder builder)
        {
            if (_enableDebugLogs)
            {
                Debug.Log($"[TreeFellingLifetimeScope] Configure started. TreeConfigAssigned={_treeConfig != null}, TreeFallConfigAssigned={_treeFallConfig != null}, PlayerWeightAssigned={_playerWeightState != null}", this);
            }

            builder.RegisterInstance(_treeConfig);
            builder.RegisterInstance(_treeFallConfig);
            builder.RegisterInstance(_playerWeightState);

            if (_inventoryQueryAdapter != null)
            {
                builder.RegisterComponent(_inventoryQueryAdapter).As<IInventoryQuery>();
            }

            if (_inventoryWriteAdapter != null)
            {
                builder.RegisterComponent(_inventoryWriteAdapter).As<IInventoryWriteService>();
            }

            if (_playerCarryAnchorAdapter != null)
            {
                builder.RegisterComponent(_playerCarryAnchorAdapter).As<IPlayerCarryAnchor>();
            }

            builder.Register<IRandomProvider, UnityRandomProvider>(Lifetime.Singleton);

            RegisterMessagePipe(builder);

            builder.UseEntryPoints(entryPoints =>
            {
                entryPoints.Add<TreeFallSystem>();
                entryPoints.Add<TreeDecalSystem>();
            });

            builder.RegisterBuildCallback(container =>
            {
                container.InjectGameObject(gameObject);

                InjectExistingTrees(container);
                InjectExistingLogs(container);

                if (_enableDebugLogs)
                {
                    Debug.Log("[TreeFellingLifetimeScope] Build callback completed. Existing trees and logs were injected.", this);
                }
            });
        }

        private void RegisterMessagePipe(IContainerBuilder builder)
        {
            var messagePipeBuilder = new BuiltinContainerBuilder();
            messagePipeBuilder.AddMessagePipe();
            messagePipeBuilder.AddMessageBroker<ChopEvent>();
            messagePipeBuilder.AddMessageBroker<TreeHitEvent>();
            messagePipeBuilder.AddMessageBroker<TreeFallStartEvent>();
            messagePipeBuilder.AddMessageBroker<SoundEvent>();
            messagePipeBuilder.AddMessageBroker<ParticleEvent>();

            var serviceProvider = messagePipeBuilder.BuildServiceProvider();

            RegisterMessage<ChopEvent>(builder, serviceProvider);
            RegisterMessage<TreeHitEvent>(builder, serviceProvider);
            RegisterMessage<TreeFallStartEvent>(builder, serviceProvider);
            RegisterMessage<SoundEvent>(builder, serviceProvider);
            RegisterMessage<ParticleEvent>(builder, serviceProvider);
        }

        private void InjectExistingTrees(IObjectResolver container)
        {
            var trees = FindObjectsByType<ChoppableTree>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var index = 0; index < trees.Length; index++)
            {
                var tree = trees[index];
                if (tree != null)
                {
                    container.InjectGameObject(tree.gameObject);
                }
            }
        }

        private void InjectExistingLogs(IObjectResolver container)
        {
            var logs = FindObjectsByType<LogPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var index = 0; index < logs.Length; index++)
            {
                var log = logs[index];
                if (log != null)
                {
                    container.InjectGameObject(log.gameObject);
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
