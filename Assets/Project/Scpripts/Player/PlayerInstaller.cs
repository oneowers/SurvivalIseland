// Path: Assets/Project/Scpripts/Player/PlayerInstaller.cs
// Purpose: Registers the full player module in VContainer and wires MessagePipe publishers for character and camera systems.
// Dependencies: MessagePipe, PlayerInput, PlayerMovement, PlayerWeight, ThirdPersonCamera, UnityEngine, VContainer.

using System;
using MessagePipe;
using ProjectResonance.PlayerInput;
using ProjectResonance.PlayerMovement;
using ProjectResonance.PlayerWeight;
using ProjectResonance.ThirdPersonCamera;
using UnityEngine;
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

        [Header("Configs")]
        [SerializeField]
        private PlayerMovementConfig _playerMovementConfig;

        [SerializeField]
        private CameraConfig _cameraConfig;

        [SerializeField]
        private PlayerWeightState _playerWeightState;

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

            builder.RegisterInstance(_playerMovementConfig);
            builder.RegisterInstance(_cameraConfig);
            builder.RegisterInstance(_playerWeightState);

            RegisterMessagePipe(builder);

            builder.UseEntryPoints(entryPoints =>
            {
                entryPoints.Add<PlayerMovementSystem>();
                entryPoints.Add<ThirdPersonCameraSystem>();
            });

            builder.RegisterBuildCallback(container =>
            {
                container.InjectGameObject(_playerInputHandler.gameObject);
                container.InjectGameObject(_characterController.gameObject);
                container.InjectGameObject(_playerCamera.gameObject);
                container.InjectGameObject(_cameraTarget.gameObject);

                _playerWeightState.Initialize(container.Resolve<IBufferedPublisher<WeightChangedEvent>>());
                _playerInputHandler.Initialize();
            });
        }

        private void RegisterMessagePipe(IContainerBuilder builder)
        {
            var messagePipeBuilder = new BuiltinContainerBuilder();
            messagePipeBuilder.AddMessagePipe();
            messagePipeBuilder.AddMessageBroker<MoveInput>();
            messagePipeBuilder.AddMessageBroker<SprintInput>();
            messagePipeBuilder.AddMessageBroker<WeightChangedEvent>();
            messagePipeBuilder.AddMessageBroker<JumpInput>();
            messagePipeBuilder.AddMessageBroker<CrouchInput>();
            messagePipeBuilder.AddMessageBroker<InteractInput>();
            messagePipeBuilder.AddMessageBroker<HeavyInteractInput>();
            messagePipeBuilder.AddMessageBroker<FootstepEvent>();

            var serviceProvider = messagePipeBuilder.BuildServiceProvider();

            RegisterBufferedMessage<MoveInput>(builder, serviceProvider);
            RegisterBufferedMessage<SprintInput>(builder, serviceProvider);
            RegisterBufferedMessage<WeightChangedEvent>(builder, serviceProvider);

            RegisterMessage<JumpInput>(builder, serviceProvider);
            RegisterMessage<CrouchInput>(builder, serviceProvider);
            RegisterMessage<InteractInput>(builder, serviceProvider);
            RegisterMessage<HeavyInteractInput>(builder, serviceProvider);
            RegisterMessage<FootstepEvent>(builder, serviceProvider);
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
}
