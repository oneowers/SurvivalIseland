// Path: Assets/Project/Scpripts/Bootstrap/ProjectBootstrapLifetimeScope.cs
// Purpose: Registers the shared global gameplay systems, scene references, and runtime services.
// Dependencies: VContainer, UnityEngine, Health, DayNight, Ghosts, Common.Random.

using System;
using ProjectResonance.Campfire;
using ProjectResonance.Common.Messages;
using ProjectResonance.Common.Random;
using ProjectResonance.DayNight;
using ProjectResonance.Ghosts;
using ProjectResonance.Health;
using ProjectResonance.HealthUI;
using ProjectResonance.Inventory;
using ProjectResonance.MobileControls;
using ProjectResonance.PlayerMovement;
using ProjectResonance.TreeDrops;
using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// Root lifetime scope for globally shared survival gameplay systems.
/// </summary>
[AddComponentMenu("Project Resonance/Bootstrap/Project Bootstrap Lifetime Scope")]
[DisallowMultipleComponent]
public sealed class ProjectBootstrapLifetimeScope : LifetimeScope
{
    [Header("Config")]
    [SerializeField]
    private HealthConfig _healthConfig;

    [SerializeField]
    private DayNightConfig _dayNightConfig;

    [SerializeField]
    private GhostSpawnerConfig _ghostSpawnerConfig;

    [SerializeField]
    private HealthHudConfig _healthHudConfig;

    [SerializeField]
    private InventoryConfig _inventoryConfig;

    [SerializeField]
    private WorldItemPickupConfig _worldItemPickupConfig;

    [SerializeField]
    private MobileControlsConfig _mobileControlsConfig;

    [Header("Scene References")]
    [SerializeField]
    private PlayerSurvivor _playerSurvivor;

    [SerializeField]
    private GhostSpawnArea _ghostSpawnArea;

    [SerializeField]
    private SunLightController _sunLightController;

    [SerializeField]
    private AmbientLightController _ambientLightController;

    [SerializeField]
    private HealthHudPresenter _healthHudPresenter;

    private InventoryConfig _runtimeInventoryConfig;
    private WorldItemPickupConfig _runtimeWorldItemPickupConfig;
    private MobileControlsConfig _runtimeMobileControlsConfig;

    /// <summary>
    /// Registers shared global systems, brokers, configs, and scene references.
    /// </summary>
    /// <param name="builder">Current container builder.</param>
    protected override void Configure(IContainerBuilder builder)
    {
        if (_healthConfig != null)
        {
            builder.RegisterInstance(_healthConfig);
        }

        if (_dayNightConfig != null)
        {
            builder.RegisterInstance(_dayNightConfig);
        }

        if (_ghostSpawnerConfig != null)
        {
            builder.RegisterInstance(_ghostSpawnerConfig);
            builder.RegisterInstance((GhostSpawnConfig)_ghostSpawnerConfig);
        }

        if (_healthHudConfig != null)
        {
            builder.RegisterInstance(_healthHudConfig);
        }

        builder.RegisterInstance(ResolveInventoryConfig());
        builder.RegisterInstance(ResolveWorldItemPickupConfig());
        builder.RegisterInstance(ResolveMobileControlsConfig());

        if (_playerSurvivor != null)
        {
            builder.RegisterComponent(_playerSurvivor);
        }

        if (_ghostSpawnArea != null)
        {
            builder.RegisterComponent(_ghostSpawnArea);
        }

        if (_sunLightController != null)
        {
            builder.RegisterComponent(_sunLightController);
        }

        if (_ambientLightController != null)
        {
            builder.RegisterComponent(_ambientLightController);
        }

        if (_healthHudPresenter != null)
        {
            builder.RegisterComponent(_healthHudPresenter);
        }

        builder.Register<IRandomProvider, UnityRandomProvider>(Lifetime.Singleton);
        EntryPointsBuilder.EnsureDispatcherRegistered(builder);
        builder.Register<InventorySystem>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
        builder.Register<EquippedToolDurabilityService>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
        builder.Register<IItemVisualFactory, ItemVisualFactory>(Lifetime.Singleton);
        builder.Register<ItemPickupPoolService>(Lifetime.Singleton);
        builder.Register<IMobileModeService, MobileModeService>(Lifetime.Singleton);
        builder.Register<HealthSystem>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
        builder.Register<DayNightSystem>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
        builder.Register<TimeOfDayEventsSystem>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();

        builder.RegisterBuildCallback(container =>
        {
            if (_ghostSpawnArea != null)
            {
                container.InjectGameObject(_ghostSpawnArea.gameObject);
            }

            if (_sunLightController != null)
            {
                container.InjectGameObject(_sunLightController.gameObject);
            }

            if (_ambientLightController != null)
            {
                container.InjectGameObject(_ambientLightController.gameObject);
            }

            if (_healthHudPresenter != null)
            {
                container.InjectGameObject(_healthHudPresenter.gameObject);
            }
        });
    }

    private InventoryConfig ResolveInventoryConfig()
    {
        if (_inventoryConfig != null)
        {
            return _inventoryConfig;
        }

        if (_runtimeInventoryConfig == null)
        {
            _runtimeInventoryConfig = ScriptableObject.CreateInstance<InventoryConfig>();
            _runtimeInventoryConfig.hideFlags = HideFlags.HideAndDontSave;
        }

        return _runtimeInventoryConfig;
    }

    private WorldItemPickupConfig ResolveWorldItemPickupConfig()
    {
        if (_worldItemPickupConfig != null)
        {
            return _worldItemPickupConfig;
        }

        if (_runtimeWorldItemPickupConfig == null)
        {
            _runtimeWorldItemPickupConfig = ScriptableObject.CreateInstance<WorldItemPickupConfig>();
            _runtimeWorldItemPickupConfig.hideFlags = HideFlags.HideAndDontSave;
        }

        return _runtimeWorldItemPickupConfig;
    }

    private MobileControlsConfig ResolveMobileControlsConfig()
    {
        if (_mobileControlsConfig != null)
        {
            return _mobileControlsConfig;
        }

        if (_runtimeMobileControlsConfig == null)
        {
            _runtimeMobileControlsConfig = ScriptableObject.CreateInstance<MobileControlsConfig>();
            _runtimeMobileControlsConfig.hideFlags = HideFlags.HideAndDontSave;
        }

        return _runtimeMobileControlsConfig;
    }
}
