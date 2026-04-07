// Path: Assets/Project/Scpripts/Bootstrap/ProjectBootstrapLifetimeScope.cs
// Purpose: Registers the shared global gameplay systems, scene references, and MessagePipe brokers.
// Dependencies: MessagePipe, VContainer, UnityEngine, Health, DayNight, Ghosts, Common.Random.

using System;
using MessagePipe;
using ProjectResonance.Campfire;
using ProjectResonance.Common.Messages;
using ProjectResonance.Common.Random;
using ProjectResonance.DayNight;
using ProjectResonance.Ghosts;
using ProjectResonance.Health;
using ProjectResonance.HealthUI;
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
        }

        if (_healthHudConfig != null)
        {
            builder.RegisterInstance(_healthHudConfig);
        }

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

        RegisterMessagePipe(builder);

        builder.RegisterEntryPoint<HealthSystem>();
        builder.RegisterEntryPoint<DayNightSystem>();
        builder.RegisterEntryPoint<TimeOfDayEventsSystem>();

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

    private void RegisterMessagePipe(IContainerBuilder builder)
    {
        var messagePipeBuilder = new BuiltinContainerBuilder();
        messagePipeBuilder.AddMessagePipe();
        messagePipeBuilder.AddMessageBroker<FuelChangedEvent>();
        messagePipeBuilder.AddMessageBroker<HealthChangedMessage>();
        messagePipeBuilder.AddMessageBroker<HealthDepletedMessage>();
        messagePipeBuilder.AddMessageBroker<PlayerInSafeZoneEvent>();
        messagePipeBuilder.AddMessageBroker<TimeTickEvent>();
        messagePipeBuilder.AddMessageBroker<TimeOfDayChangedEvent>();
        messagePipeBuilder.AddMessageBroker<SunsetWarningEvent>();
        messagePipeBuilder.AddMessageBroker<ThermalDamageEvent>();
        messagePipeBuilder.AddMessageBroker<ThermalHealEvent>();
        messagePipeBuilder.AddMessageBroker<BirdsStartSingingEvent>();
        messagePipeBuilder.AddMessageBroker<BirdsStopSingingEvent>();
        messagePipeBuilder.AddMessageBroker<GhostsActivateEvent>();
        messagePipeBuilder.AddMessageBroker<LordWraithSpawnRequestEvent>();

        var serviceProvider = messagePipeBuilder.BuildServiceProvider();

        RegisterBufferedMessage<FuelChangedEvent>(builder, serviceProvider);
        RegisterBufferedMessage<HealthChangedMessage>(builder, serviceProvider);
        RegisterMessage<HealthDepletedMessage>(builder, serviceProvider);
        RegisterBufferedMessage<PlayerInSafeZoneEvent>(builder, serviceProvider);
        RegisterBufferedMessage<TimeTickEvent>(builder, serviceProvider);
        RegisterBufferedMessage<TimeOfDayChangedEvent>(builder, serviceProvider);
        RegisterMessage<SunsetWarningEvent>(builder, serviceProvider);
        RegisterMessage<ThermalDamageEvent>(builder, serviceProvider);
        RegisterMessage<ThermalHealEvent>(builder, serviceProvider);
        RegisterMessage<BirdsStartSingingEvent>(builder, serviceProvider);
        RegisterMessage<BirdsStopSingingEvent>(builder, serviceProvider);
        RegisterMessage<GhostsActivateEvent>(builder, serviceProvider);
        RegisterMessage<LordWraithSpawnRequestEvent>(builder, serviceProvider);
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
}
