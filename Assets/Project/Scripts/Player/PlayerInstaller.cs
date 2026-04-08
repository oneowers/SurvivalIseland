// Path: Assets/Project/Scpripts/Player/PlayerInstaller.cs
// Purpose: Registers the full player module in VContainer and wires MessagePipe publishers for character and camera systems.
// Dependencies: MessagePipe, PlayerInput, PlayerMovement, PlayerWeight, ThirdPersonCamera, UnityEngine, VContainer.

using System;
using MessagePipe;
using ProjectResonance.Campfire;
using ProjectResonance.Crafting;
using ProjectResonance.Inventory;
using ProjectResonance.InventoryUI;
using ProjectResonance.MobileControls;
using ProjectResonance.PlayerCombat;
using ProjectResonance.PlayerInput;
using ProjectResonance.PlayerMovement;
using ProjectResonance.PlayerWeight;
using ProjectResonance.ResourceNodes;
using ProjectResonance.TreeFelling;
using ProjectResonance.ThirdPersonCamera;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

namespace ProjectResonance.PlayerInstaller
{
    /// <summary>
    /// LifetimeScope for the complete player module.
    /// </summary>
    public sealed class PlayerInstaller : LifetimeScope
    {
        [Header("Scene References")]
        [SerializeField]
        private PlayerInputHandler _playerInputHandler;

        [SerializeField]
        private CharacterController _characterController;

        [SerializeField]
        private Camera _playerCamera;

        [SerializeField]
        private PlayerCameraTarget _cameraTarget;

        [SerializeField]
        [FormerlySerializedAs("_treeTargetDetector")]
        private ResourceTargetDetector _resourceTargetDetector;

        [SerializeField]
        [FormerlySerializedAs("_playerTreeInteractor")]
        private PlayerResourceInteractor _playerResourceInteractor;

        [SerializeField]
        private HeldItemController _heldItemController;

        [SerializeField]
        private InventoryHUD _inventoryHud;

        [SerializeField]
        private GameObject _mobileControlsRoot;

        [SerializeField]
        private PlayerInventoryBridgeAdapter _inventoryBridgeAdapter;

        [Header("Optional Inject Roots")]
        [SerializeField]
        private GameObject[] _additionalInjectedObjects;

        [Header("Configs")]
        [SerializeField]
        private PlayerMovementConfig _playerMovementConfig;

        [SerializeField]
        private CameraConfig _cameraConfig;

        [SerializeField]
        private PlayerWeightState _playerWeightState;

        [SerializeField]
        private RecipeDatabase _recipeDatabase;

        [SerializeField]
        private CampfireAnchor _campfireAnchor;

        [SerializeField]
        private CampfireState _campfireState;

        [SerializeField]
        private AimTargetingConfig _aimTargetingConfig;

        /// <summary>
        /// Gets the authored aim-targeting config used by the active player combat pipeline.
        /// </summary>
        public AimTargetingConfig AimTargetingConfigAsset => _aimTargetingConfig;

        /// <summary>
        /// Registers the player module services and scene dependencies.
        /// </summary>
        /// <param name="builder">Current VContainer builder.</param>
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_playerInputHandler);
            builder.RegisterComponent(_characterController);
            builder.RegisterComponent(_playerCamera);
            builder.RegisterComponent(_cameraTarget);
            if (_resourceTargetDetector != null)
            {
                builder.RegisterComponent(_resourceTargetDetector);
            }
            if (_playerResourceInteractor != null)
            {
                builder.RegisterComponent(_playerResourceInteractor);
            }
            if (_heldItemController != null)
            {
                builder.RegisterComponent(_heldItemController);
            }
            if (_inventoryHud != null)
            {
                builder.RegisterComponent(_inventoryHud);
            }

            var resolvedAimTargetingConfig = _aimTargetingConfig != null
                ? _aimTargetingConfig
                : ScriptableObject.CreateInstance<AimTargetingConfig>();

            builder.RegisterInstance(_playerMovementConfig);
            builder.RegisterInstance(_cameraConfig);
            builder.RegisterInstance(_playerWeightState);
            builder.RegisterInstance(resolvedAimTargetingConfig);
            if (_recipeDatabase != null)
            {
                builder.RegisterInstance(_recipeDatabase);
            }
            if (_campfireAnchor != null)
            {
                builder.RegisterInstance(_campfireAnchor);
            }
            if (_campfireState != null)
            {
                builder.RegisterInstance(_campfireState);
            }

            if (_inventoryBridgeAdapter != null)
            {
                builder.RegisterComponent(_inventoryBridgeAdapter).As<IInventoryQuery>();
            }
            else
            {
                builder.RegisterInstance((IInventoryQuery)new NullInventoryQuery());
            }
            builder.Register<PlayerHitDamageResolver>(Lifetime.Singleton);
            builder.Register<AimTargetingSystem>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();

            RegisterMessagePipe(builder);

            // Register entry points explicitly so interdependent runtime systems
            // resolve the same scoped instance through VContainer.
            EntryPointsBuilder.EnsureDispatcherRegistered(builder);
            builder.RegisterEntryPoint<PlayerMovementSystem>();
            builder.RegisterEntryPoint<PlayerAimCombatInteractor>();
            builder.RegisterEntryPoint<ThirdPersonCameraSystem>();
            if (_heldItemController != null)
            {
                builder.RegisterEntryPoint<CraftingSystem>();
            }

            builder.RegisterBuildCallback(container =>
            {
                container.InjectGameObject(_playerInputHandler.gameObject);
                container.InjectGameObject(_characterController.gameObject);
                container.InjectGameObject(_playerCamera.gameObject);
                container.InjectGameObject(_cameraTarget.gameObject);
                if (_resourceTargetDetector != null)
                {
                    container.InjectGameObject(_resourceTargetDetector.gameObject);
                }
                if (_playerResourceInteractor != null)
                {
                    container.InjectGameObject(_playerResourceInteractor.gameObject);
                }
                if (_heldItemController != null)
                {
                    container.InjectGameObject(_heldItemController.gameObject);
                }
                if (_inventoryHud != null)
                {
                    container.InjectGameObject(_inventoryHud.gameObject);
                }
                if (_mobileControlsRoot != null)
                {
                    container.InjectGameObject(_mobileControlsRoot);

                    var mobileControlsPresenter = _mobileControlsRoot.GetComponent<MobileControlsPresenter>();
                    if (mobileControlsPresenter != null)
                    {
                        mobileControlsPresenter.Initialize();
                    }
                }
                if (_additionalInjectedObjects != null)
                {
                    for (var index = 0; index < _additionalInjectedObjects.Length; index++)
                    {
                        var injectedObject = _additionalInjectedObjects[index];
                        if (injectedObject == null)
                        {
                            continue;
                        }

                        container.InjectGameObject(injectedObject);
                    }
                }

                _playerWeightState.Initialize(container.Resolve<IBufferedPublisher<WeightChangedEvent>>());
                _playerInputHandler.Initialize();
                if (_resourceTargetDetector != null)
                {
                    _resourceTargetDetector.Initialize();
                }
                if (_playerResourceInteractor != null)
                {
                    _playerResourceInteractor.Initialize();
                }
            });
        }

        private void RegisterMessagePipe(IContainerBuilder builder)
        {
            var messagePipeBuilder = new BuiltinContainerBuilder();
            messagePipeBuilder.AddMessagePipe();
            messagePipeBuilder.AddMessageBroker<MoveInput>();
            messagePipeBuilder.AddMessageBroker<SprintInput>();
            messagePipeBuilder.AddMessageBroker<AimInput>();
            messagePipeBuilder.AddMessageBroker<WeightChangedEvent>();
            messagePipeBuilder.AddMessageBroker<JumpInput>();
            messagePipeBuilder.AddMessageBroker<CrouchInput>();
            messagePipeBuilder.AddMessageBroker<InteractInput>();
            messagePipeBuilder.AddMessageBroker<HeavyInteractInput>();
            messagePipeBuilder.AddMessageBroker<CraftInput>();
            messagePipeBuilder.AddMessageBroker<FootstepEvent>();
            messagePipeBuilder.AddMessageBroker<CraftSuccessEvent>();
            messagePipeBuilder.AddMessageBroker<CraftFailEvent>();

            var serviceProvider = messagePipeBuilder.BuildServiceProvider();

            RegisterBufferedMessage<MoveInput>(builder, serviceProvider);
            RegisterBufferedMessage<SprintInput>(builder, serviceProvider);
            RegisterBufferedMessage<AimInput>(builder, serviceProvider);
            RegisterBufferedMessage<WeightChangedEvent>(builder, serviceProvider);

            RegisterMessage<JumpInput>(builder, serviceProvider);
            RegisterMessage<CrouchInput>(builder, serviceProvider);
            RegisterMessage<InteractInput>(builder, serviceProvider);
            RegisterMessage<HeavyInteractInput>(builder, serviceProvider);
            RegisterMessage<CraftInput>(builder, serviceProvider);
            RegisterMessage<FootstepEvent>(builder, serviceProvider);
            RegisterMessage<CraftSuccessEvent>(builder, serviceProvider);
            RegisterMessage<CraftFailEvent>(builder, serviceProvider);
        }

        private void RegisterMessage<TMessage>(IContainerBuilder builder, IServiceProvider serviceProvider)
        {
            builder.RegisterInstance((IPublisher<TMessage>)serviceProvider.GetService(typeof(IPublisher<TMessage>)));
            builder.RegisterInstance((ISubscriber<TMessage>)serviceProvider.GetService(typeof(ISubscriber<TMessage>)));
        }

        private void RegisterBufferedMessage<TMessage>(IContainerBuilder builder, IServiceProvider serviceProvider)
        {
            builder.RegisterInstance((IPublisher<TMessage>)serviceProvider.GetService(typeof(IPublisher<TMessage>)));
            builder.RegisterInstance((ISubscriber<TMessage>)serviceProvider.GetService(typeof(ISubscriber<TMessage>)));
            builder.RegisterInstance((IBufferedPublisher<TMessage>)serviceProvider.GetService(typeof(IBufferedPublisher<TMessage>)));
            builder.RegisterInstance((IBufferedSubscriber<TMessage>)serviceProvider.GetService(typeof(IBufferedSubscriber<TMessage>)));
        }

        private sealed class NullInventoryQuery : IInventoryQuery
        {
            public AxeTier GetEquippedAxeTier()
            {
                return AxeTier.None;
            }
        }
    }
}
