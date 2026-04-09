// Path: Assets/Project/Scpripts/Campfire/CampfireLifetimeScope.cs
// Purpose: Registers the full campfire module and its runtime services.
// Dependencies: VContainer, UnityEngine, Campfire, Common.Random, DayNight, Ghosts.

using System;
using Cysharp.Threading.Tasks;
using ProjectResonance.Common.Random;
using ProjectResonance.DayNight;
using ProjectResonance.Ghosts;
using ProjectResonance.TreeFelling;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ProjectResonance.Campfire
{
    /// <summary>
    /// Scene lifetime scope for the campfire gameplay module.
    /// </summary>
    [AddComponentMenu("Project Resonance/Campfire/Campfire Lifetime Scope")]
    [DisallowMultipleComponent]
    public sealed class CampfireLifetimeScope : LifetimeScope
    {
        [Header("Config")]
        [SerializeField]
        private CampfireConfig _campfireConfig;

        [SerializeField]
        private CampfireState _campfireState;

        [Header("Scene References")]
        [SerializeField]
        private CampfireAnchor _campfireAnchor;

        [SerializeField]
        private CampfireProtectionZone _campfireProtectionZone;

        [SerializeField]
        private CampfireLightController _campfireLightController;

        [SerializeField]
        private CampfireSavePoint _campfireSavePoint;

        [SerializeField]
        private CampfireInteraction _campfireInteraction;

        [Header("Optional Services")]
        [SerializeField]
        private MonoBehaviour _weatherServiceSource;

        [SerializeField]
        private MonoBehaviour _respawnServiceSource;

        [SerializeField]
        private MonoBehaviour _menuPresenterSource;

        [SerializeField]
        private MonoBehaviour _inventoryBridgeSource;

        /// <summary>
        /// Registers campfire services, state assets and scene components.
        /// </summary>
        /// <param name="builder">Current container builder.</param>
        protected override void Configure(IContainerBuilder builder)
        {
            var nullWeatherService = new NullCampfireWeatherService();
            var nullRespawnService = new NullRespawnService();
            var nullMenuPresenter = new NullCampfireMenuPresenter();
            var nullInventoryBridge = new NullCampfireInventoryBridge();

            builder.RegisterInstance(_campfireConfig);
            builder.RegisterInstance(_campfireState);

            if (_campfireAnchor != null)
            {
                builder.RegisterComponent(_campfireAnchor);
            }

            if (_campfireProtectionZone != null)
            {
                builder.RegisterComponent(_campfireProtectionZone);
            }

            if (_campfireLightController != null)
            {
                builder.RegisterComponent(_campfireLightController);
            }

            if (_campfireSavePoint != null)
            {
                builder.RegisterComponent(_campfireSavePoint).As<ISavePoint>();
            }

            if (_campfireInteraction != null)
            {
                builder.RegisterComponent(_campfireInteraction);
            }

            if (_weatherServiceSource is ICampfireWeatherService weatherService)
            {
                builder.RegisterInstance<ICampfireWeatherService>(weatherService);
            }
            else
            {
                builder.RegisterInstance<ICampfireWeatherService>(nullWeatherService);
            }

            if (_respawnServiceSource is IRespawnService respawnService)
            {
                builder.RegisterInstance(respawnService);
            }
            else
            {
                builder.RegisterInstance<IRespawnService>(nullRespawnService);
            }

            if (_menuPresenterSource is ICampfireMenuPresenter menuPresenter)
            {
                builder.RegisterInstance(menuPresenter);
            }
            else
            {
                builder.RegisterInstance<ICampfireMenuPresenter>(nullMenuPresenter);
            }

            if (_inventoryBridgeSource is IInventoryQuery inventoryQuery)
            {
                builder.RegisterInstance(inventoryQuery);
            }
            else
            {
                builder.RegisterInstance<IInventoryQuery>(nullInventoryBridge);
            }

            if (_inventoryBridgeSource is IInventoryWriteService inventoryWriteService)
            {
                builder.RegisterInstance(inventoryWriteService);
            }
            else
            {
                builder.RegisterInstance<IInventoryWriteService>(nullInventoryBridge);
            }

            builder.Register<IRandomProvider, UnityRandomProvider>(Lifetime.Singleton);

            EntryPointsBuilder.EnsureDispatcherRegistered(builder);
            builder.Register<CampfireSystem>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<TemperatureSystem>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<GhostSpawnSystem>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();

            builder.RegisterBuildCallback(container =>
            {
                if (_inventoryBridgeSource != null)
                {
                    container.InjectGameObject(_inventoryBridgeSource.gameObject);
                }

                if (_campfireAnchor != null)
                {
                    container.InjectGameObject(_campfireAnchor.gameObject);
                }

                if (_campfireProtectionZone != null)
                {
                    container.InjectGameObject(_campfireProtectionZone.gameObject);
                }

                if (_campfireLightController != null)
                {
                    container.InjectGameObject(_campfireLightController.gameObject);
                }

                if (_campfireSavePoint != null)
                {
                    container.InjectGameObject(_campfireSavePoint.gameObject);
                }

                if (_campfireInteraction != null)
                {
                    container.InjectGameObject(_campfireInteraction.gameObject);
                }
            });
        }

        private sealed class NullCampfireWeatherService : ICampfireWeatherService
        {
            /// <summary>
            /// Gets whether rain is currently active.
            /// </summary>
            public bool IsRaining => false;

            /// <summary>
            /// Gets whether strong wind is currently active.
            /// </summary>
            public bool IsWindy => false;
        }

        private sealed class NullRespawnService : IRespawnService
        {
            /// <summary>
            /// Sets the current active respawn point.
            /// </summary>
            /// <param name="savePoint">Save point to register.</param>
            public void SetRespawnPoint(ISavePoint savePoint)
            {
                // Projects that have not wired a dedicated respawn system yet still need campfire scope to boot cleanly.
            }
        }

        private sealed class NullCampfireMenuPresenter : ICampfireMenuPresenter
        {
            /// <summary>
            /// Opens the campfire context menu.
            /// </summary>
            /// <param name="savePoint">Save point requesting the menu.</param>
            /// <param name="cancellationToken">Cancellation token.</param>
            /// <returns>Selected menu action.</returns>
            public UniTask<CampfireMenuSelection> OpenAsync(ISavePoint savePoint, System.Threading.CancellationToken cancellationToken = default)
            {
                return UniTask.FromResult(CampfireMenuSelection.Cancel);
            }
        }

        private sealed class NullCampfireInventoryBridge : IInventoryQuery, IInventoryWriteService, ICampfireInventoryQuery, ICampfireInventoryWriteService
        {
            /// <summary>
            /// Returns the currently equipped axe tier.
            /// </summary>
            /// <returns>Equipped axe tier.</returns>
            public AxeTier GetEquippedAxeTier()
            {
                return AxeTier.None;
            }

            /// <summary>
            /// Attempts to add logs to the inventory.
            /// </summary>
            /// <param name="amount">Number of logs to add.</param>
            /// <returns>True when the logs were accepted by the inventory.</returns>
            public bool TryAddLog(int amount)
            {
                return false;
            }

            /// <summary>
            /// Gets whether the inventory currently contains flint.
            /// </summary>
            public bool HasFlint()
            {
                return false;
            }

            /// <summary>
            /// Gets whether the inventory currently contains firesteel.
            /// </summary>
            public bool HasFiresteel()
            {
                return false;
            }

            /// <summary>
            /// Gets whether any valid ignition source is available.
            /// </summary>
            /// <returns>True when ignition is possible.</returns>
            public bool HasIgnitionSource()
            {
                return false;
            }

            /// <summary>
            /// Attempts to consume logs from the inventory.
            /// </summary>
            /// <param name="amount">Number of logs to consume.</param>
            /// <returns>True when enough logs were available.</returns>
            public bool TryConsumeLog(int amount)
            {
                return false;
            }
        }
    }
}
