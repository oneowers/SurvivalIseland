// Path: Assets/Project/Scpripts/Player/PlayerInstaller.cs
// Purpose: Registers the full player module and its explicit runtime event services.
// Dependencies: PlayerInput, PlayerMovement, PlayerWeight, ThirdPersonCamera, UnityEngine, VContainer.

using System;
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
            builder.Register<PlayerWeightRuntime>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<PlayerMovementSignals>(Lifetime.Singleton).AsSelf();
            builder.Register<PlayerHitDamageResolver>(Lifetime.Singleton);
            builder.Register<PlantableResourceSpawnService>(Lifetime.Singleton);
            builder.Register<PlantingPreviewVisualizer>(Lifetime.Singleton);
            builder.Register<PlayerAimPlantingInteractor>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<AimTargetingSystem>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<PlayerMovementSystem>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<PlayerAimCombatInteractor>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<ThirdPersonCameraSystem>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            if (_heldItemController != null)
            {
                builder.Register<CraftingSystem>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            }

            // Register entry points explicitly so interdependent runtime systems
            // resolve the same scoped instance through VContainer.
            EntryPointsBuilder.EnsureDispatcherRegistered(builder);

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

        private sealed class NullInventoryQuery : IInventoryQuery
        {
            public AxeTier GetEquippedAxeTier()
            {
                return AxeTier.None;
            }
        }
    }
}
