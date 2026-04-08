// Path: Assets/Project/Scpripts/Player/PlayerMovementSystem.cs
// Purpose: Moves the player with CharacterController, inertia, jump, crouch and footstep events.
// Dependencies: MessagePipe, PlayerInput, PlayerWeight, UnityEngine, VContainer.

using System;
using MessagePipe;
using ProjectResonance.PlayerInput;
using ProjectResonance.PlayerWeight;
using UnityEngine;
using VContainer.Unity;

namespace ProjectResonance.PlayerMovement
{
    /// <summary>
    /// Publishes a footstep event for audio systems.
    /// </summary>
    public readonly struct FootstepEvent
    {
        /// <summary>
        /// Creates a new footstep event.
        /// </summary>
        /// <param name="volume">Normalized footstep volume.</param>
        public FootstepEvent(float volume)
        {
            Volume = volume;
        }

        /// <summary>
        /// Gets the normalized footstep volume.
        /// </summary>
        public float Volume { get; }
    }

    /// <summary>
    /// Published when an external system pulls the player toward a ghost source.
    /// </summary>
    public readonly struct PlayerGravityPullEvent
    {
        /// <summary>
        /// Creates a new player gravity pull event.
        /// </summary>
        /// <param name="sourcePosition">World position of the pulling source.</param>
        /// <param name="strength">Horizontal pull strength.</param>
        /// <param name="radius">Effective pull radius.</param>
        public PlayerGravityPullEvent(Vector3 sourcePosition, float strength, float radius)
        {
            SourcePosition = sourcePosition;
            Strength = strength;
            Radius = radius;
        }

        /// <summary>
        /// Gets the world-space source position.
        /// </summary>
        public Vector3 SourcePosition { get; }

        /// <summary>
        /// Gets the authored pull strength.
        /// </summary>
        public float Strength { get; }

        /// <summary>
        /// Gets the effective pull radius.
        /// </summary>
        public float Radius { get; }
    }

    /// <summary>
    /// ScriptableObject with all player movement tuning values.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "Project Resonance/Player/Player Movement Config")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField]
        [Min(0.1f)]
        private float _walkSpeed = 4f;

        [SerializeField]
        [Min(0.1f)]
        private float _sprintSpeed = 6.5f;

        [SerializeField]
        [Range(0.1f, 1f)]
        private float _crouchSpeedMultiplier = 0.5f;

        [SerializeField]
        [Min(0.01f)]
        private float _accelerationTime = 0.1f;

        [SerializeField]
        [Min(0.01f)]
        private float _directionChangeTime = 0.18f;

        [SerializeField]
        [Min(0.01f)]
        private float _stopTimeEmpty = 0.1f;

        [SerializeField]
        [Min(0.01f)]
        private float _stopTimeLightItem = 0.2f;

        [SerializeField]
        [Min(0.01f)]
        private float _stopTimeHeavyLog = 0.4f;

        [SerializeField]
        [Min(0.01f)]
        private float _stopTimeTwoLogs = 0.7f;

        [SerializeField]
        [Min(1f)]
        private float _rotationSharpness = 12f;

        [Header("Jump And Gravity")]
        [SerializeField]
        [Min(0f)]
        private float _jumpHeight = 1.35f;

        [SerializeField]
        [Min(0.1f)]
        private float _gravity = 28f;

        [SerializeField]
        [Min(0.1f)]
        private float _groundStickForce = 3f;

        [Header("Character Controller")]
        [SerializeField]
        [Min(0.1f)]
        private float _standingHeight = 1.8f;

        [SerializeField]
        [Min(0.1f)]
        private float _crouchingHeight = 1.15f;

        [SerializeField]
        [Min(0.1f)]
        private float _heightLerpSpeed = 8f;

        [Header("Footsteps")]
        [SerializeField]
        [Min(0.1f)]
        private float _walkStepDistance = 2.1f;

        [SerializeField]
        [Min(0.1f)]
        private float _sprintStepDistance = 1.5f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _walkStepVolume = 0.85f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _sprintStepVolume = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _crouchStepVolume = 0.35f;

        /// <summary>
        /// Gets the walk speed.
        /// </summary>
        public float WalkSpeed => _walkSpeed;

        /// <summary>
        /// Gets the sprint speed.
        /// </summary>
        public float SprintSpeed => _sprintSpeed;

        /// <summary>
        /// Gets the crouch speed multiplier.
        /// </summary>
        public float CrouchSpeedMultiplier => _crouchSpeedMultiplier;

        /// <summary>
        /// Gets the acceleration time.
        /// </summary>
        public float AccelerationTime => _accelerationTime;

        /// <summary>
        /// Gets the direction change smoothing time.
        /// </summary>
        public float DirectionChangeTime => _directionChangeTime;

        /// <summary>
        /// Gets the rotation sharpness.
        /// </summary>
        public float RotationSharpness => _rotationSharpness;

        /// <summary>
        /// Gets the jump height.
        /// </summary>
        public float JumpHeight => _jumpHeight;

        /// <summary>
        /// Gets the gravity magnitude.
        /// </summary>
        public float Gravity => _gravity;

        /// <summary>
        /// Gets the downward force applied while grounded.
        /// </summary>
        public float GroundStickForce => _groundStickForce;

        /// <summary>
        /// Gets the standing controller height.
        /// </summary>
        public float StandingHeight => _standingHeight;

        /// <summary>
        /// Gets the crouching controller height.
        /// </summary>
        public float CrouchingHeight => _crouchingHeight;

        /// <summary>
        /// Gets the controller height interpolation speed.
        /// </summary>
        public float HeightLerpSpeed => _heightLerpSpeed;

        /// <summary>
        /// Gets the walking step distance.
        /// </summary>
        public float WalkStepDistance => _walkStepDistance;

        /// <summary>
        /// Gets the sprint step distance.
        /// </summary>
        public float SprintStepDistance => _sprintStepDistance;

        /// <summary>
        /// Gets the walking footstep volume.
        /// </summary>
        public float WalkStepVolume => _walkStepVolume;

        /// <summary>
        /// Gets the sprint footstep volume.
        /// </summary>
        public float SprintStepVolume => _sprintStepVolume;

        /// <summary>
        /// Gets the crouching footstep volume.
        /// </summary>
        public float CrouchStepVolume => _crouchStepVolume;

        /// <summary>
        /// Gets the stop inertia time for the current carry weight.
        /// </summary>
        /// <param name="weightType">Weight state used to resolve the inertia time.</param>
        /// <returns>Stop smoothing time in seconds.</returns>
        public float GetStopTime(PlayerWeightType weightType)
        {
            switch (weightType)
            {
                case PlayerWeightType.LightItem:
                    return _stopTimeLightItem;
                case PlayerWeightType.HeavyLog:
                    return _stopTimeHeavyLog;
                case PlayerWeightType.TwoLogs:
                    return _stopTimeTwoLogs;
                default:
                    return _stopTimeEmpty;
            }
        }
    }

    /// <summary>
    /// Moves the player using CharacterController and state-driven inertia.
    /// </summary>
    public sealed class PlayerMovementSystem : IStartable, ITickable, IDisposable
    {
        private readonly CharacterController _characterController;
        private readonly Camera _playerCamera;
        private readonly PlayerMovementConfig _config;
        private readonly PlayerWeightState _weightState;
        private readonly IBufferedSubscriber<MoveInput> _moveInputSubscriber;
        private readonly IBufferedSubscriber<SprintInput> _sprintInputSubscriber;
        private readonly ISubscriber<JumpInput> _jumpInputSubscriber;
        private readonly ISubscriber<CrouchInput> _crouchInputSubscriber;
        private readonly IBufferedSubscriber<WeightChangedEvent> _weightChangedSubscriber;
        private readonly IBufferedSubscriber<PlayerGravityPullEvent> _gravityPullSubscriber;
        private readonly IPublisher<FootstepEvent> _footstepPublisher;

        private IDisposable _moveSubscription;
        private IDisposable _sprintSubscription;
        private IDisposable _jumpSubscription;
        private IDisposable _crouchSubscription;
        private IDisposable _weightSubscription;
        private IDisposable _gravityPullSubscription;

        private Vector2 _moveInput;
        private Vector3 _horizontalVelocity;
        private Vector3 _horizontalVelocitySmoothRef;
        private Vector3 _lastPlanarPosition;
        private Vector3 _initialControllerCenter;
        private Vector3 _gravityPullSourcePosition;
        private float _verticalVelocity;
        private float _stepDistanceAccumulator;
        private float _currentTargetHeight;
        private float _gravityPullStrength;
        private float _gravityPullRadius;
        private bool _isSprintRequested;
        private bool _isCrouching;
        private bool _jumpRequested;
        private bool _hasGravityPull;
        private PlayerWeightType _currentWeight;
        private int _gravityPullFrame = -1;

        /// <summary>
        /// Creates the player movement system.
        /// </summary>
        /// <param name="characterController">Player character controller.</param>
        /// <param name="playerCamera">Camera used for camera-relative movement.</param>
        /// <param name="config">Movement configuration.</param>
        /// <param name="weightState">Runtime player weight state.</param>
        /// <param name="moveInputSubscriber">Movement input subscriber.</param>
        /// <param name="sprintInputSubscriber">Sprint input subscriber.</param>
        /// <param name="jumpInputSubscriber">Jump input subscriber.</param>
        /// <param name="crouchInputSubscriber">Crouch input subscriber.</param>
        /// <param name="weightChangedSubscriber">Weight changed subscriber.</param>
        /// <param name="gravityPullSubscriber">Ghost gravity pull subscriber.</param>
        /// <param name="footstepPublisher">Footstep publisher.</param>
        public PlayerMovementSystem(
            CharacterController characterController,
            Camera playerCamera,
            PlayerMovementConfig config,
            PlayerWeightState weightState,
            IBufferedSubscriber<MoveInput> moveInputSubscriber,
            IBufferedSubscriber<SprintInput> sprintInputSubscriber,
            ISubscriber<JumpInput> jumpInputSubscriber,
            ISubscriber<CrouchInput> crouchInputSubscriber,
            IBufferedSubscriber<WeightChangedEvent> weightChangedSubscriber,
            IBufferedSubscriber<PlayerGravityPullEvent> gravityPullSubscriber,
            IPublisher<FootstepEvent> footstepPublisher)
        {
            _characterController = characterController;
            _playerCamera = playerCamera;
            _config = config;
            _weightState = weightState;
            _moveInputSubscriber = moveInputSubscriber;
            _sprintInputSubscriber = sprintInputSubscriber;
            _jumpInputSubscriber = jumpInputSubscriber;
            _crouchInputSubscriber = crouchInputSubscriber;
            _weightChangedSubscriber = weightChangedSubscriber;
            _gravityPullSubscriber = gravityPullSubscriber;
            _footstepPublisher = footstepPublisher;
        }

        /// <summary>
        /// Subscribes to input messages and initializes runtime state.
        /// </summary>
        public void Start()
        {
            _currentWeight = _weightState.CurrentWeight;
            _currentTargetHeight = _config.StandingHeight;
            _lastPlanarPosition = _characterController.transform.position;
            _initialControllerCenter = _characterController.center;

            ApplyControllerHeight(_currentTargetHeight);

            _moveSubscription = _moveInputSubscriber.Subscribe(OnMoveInputChanged);
            _sprintSubscription = _sprintInputSubscriber.Subscribe(OnSprintInputChanged);
            _jumpSubscription = _jumpInputSubscriber.Subscribe(OnJumpRequested);
            _crouchSubscription = _crouchInputSubscriber.Subscribe(OnCrouchRequested);
            _weightSubscription = _weightChangedSubscriber.Subscribe(OnWeightChanged);
            _gravityPullSubscription = _gravityPullSubscriber.Subscribe(OnGravityPullRequested);
        }

        /// <summary>
        /// Ticks the player movement every frame.
        /// </summary>
        public void Tick()
        {
            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            UpdateControllerHeight(deltaTime);
            UpdateVerticalVelocity(deltaTime);
            UpdateHorizontalVelocity(deltaTime);
            ApplyGravityPull(deltaTime);
            MoveCharacter(deltaTime);
            UpdateFootsteps();
        }

        /// <summary>
        /// Releases input subscriptions.
        /// </summary>
        public void Dispose()
        {
            _moveSubscription?.Dispose();
            _sprintSubscription?.Dispose();
            _jumpSubscription?.Dispose();
            _crouchSubscription?.Dispose();
            _weightSubscription?.Dispose();
            _gravityPullSubscription?.Dispose();
        }

        private void OnMoveInputChanged(MoveInput message)
        {
            _moveInput = Vector2.ClampMagnitude(message.Value, 1f);
        }

        private void OnSprintInputChanged(SprintInput message)
        {
            _isSprintRequested = message.IsPressed;
        }

        private void OnJumpRequested(JumpInput message)
        {
            _jumpRequested = true;
        }

        private void OnCrouchRequested(CrouchInput message)
        {
            _isCrouching = !_isCrouching;
            _currentTargetHeight = _isCrouching ? _config.CrouchingHeight : _config.StandingHeight;
        }

        private void OnWeightChanged(WeightChangedEvent message)
        {
            _currentWeight = message.CurrentWeight;
        }

        private void OnGravityPullRequested(PlayerGravityPullEvent message)
        {
            _gravityPullSourcePosition = message.SourcePosition;
            _gravityPullStrength = Mathf.Max(0f, message.Strength);
            _gravityPullRadius = Mathf.Max(0.1f, message.Radius);
            _hasGravityPull = _gravityPullStrength > 0f;
            _gravityPullFrame = Time.frameCount;
        }

        private void UpdateVerticalVelocity(float deltaTime)
        {
            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -_config.GroundStickForce;
            }

            if (_jumpRequested && _characterController.isGrounded && _currentWeight != PlayerWeightType.TwoLogs)
            {
                _verticalVelocity = Mathf.Sqrt(2f * _config.Gravity * _config.JumpHeight);
            }

            _jumpRequested = false;
            _verticalVelocity -= _config.Gravity * deltaTime;
        }

        private void UpdateHorizontalVelocity(float deltaTime)
        {
            var moveDirection = ResolveMoveDirection();
            var moveMagnitude = Mathf.Clamp01(_moveInput.magnitude);

            var targetSpeed = _config.WalkSpeed;
            if (_isSprintRequested && !_isCrouching && moveMagnitude > 0.01f)
            {
                targetSpeed = _config.SprintSpeed;
            }

            if (_isCrouching)
            {
                targetSpeed *= _config.CrouchSpeedMultiplier;
            }

            var desiredVelocity = moveDirection * (targetSpeed * moveMagnitude);
            var smoothTime = ResolveHorizontalSmoothTime(desiredVelocity);

            _horizontalVelocity = Vector3.SmoothDamp(
                _horizontalVelocity,
                desiredVelocity,
                ref _horizontalVelocitySmoothRef,
                smoothTime,
                Mathf.Infinity,
                deltaTime);

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                var targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                _characterController.transform.rotation = Quaternion.Slerp(
                    _characterController.transform.rotation,
                    targetRotation,
                    _config.RotationSharpness * deltaTime);
            }
        }

        private void ApplyGravityPull(float deltaTime)
        {
            if (!_hasGravityPull)
            {
                return;
            }

            if (Time.frameCount - _gravityPullFrame > 1)
            {
                _hasGravityPull = false;
                return;
            }

            var offset = _gravityPullSourcePosition - _characterController.transform.position;
            offset.y = 0f;

            var sqrDistance = offset.sqrMagnitude;
            if (sqrDistance <= Mathf.Epsilon)
            {
                return;
            }

            var distance = Mathf.Sqrt(sqrDistance);
            if (distance > _gravityPullRadius)
            {
                return;
            }

            // The pull scales with proximity so the Lord Wraith feels heavier the closer it gets.
            var pullWeight = 1f - Mathf.Clamp01(distance / _gravityPullRadius);
            _horizontalVelocity += offset.normalized * (_gravityPullStrength * pullWeight * deltaTime);
        }

        private Vector3 ResolveMoveDirection()
        {
            var cameraForward = _playerCamera.transform.forward;
            var cameraRight = _playerCamera.transform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            var moveDirection = (cameraForward * _moveInput.y) + (cameraRight * _moveInput.x);
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            return moveDirection;
        }

        private float ResolveHorizontalSmoothTime(Vector3 desiredVelocity)
        {
            if (desiredVelocity.sqrMagnitude <= 0.0001f)
            {
                return _config.GetStopTime(_currentWeight);
            }

            if (_horizontalVelocity.sqrMagnitude <= 0.0001f)
            {
                return _config.AccelerationTime;
            }

            var currentDirection = _horizontalVelocity.normalized;
            var desiredDirection = desiredVelocity.normalized;

            // Direction reversals must feel heavier than simple acceleration so the character does not snap instantly.
            return Vector3.Dot(currentDirection, desiredDirection) < 0f
                ? _config.DirectionChangeTime
                : _config.AccelerationTime;
        }

        private void MoveCharacter(float deltaTime)
        {
            var displacement = _horizontalVelocity;
            displacement.y = _verticalVelocity;
            _characterController.Move(displacement * deltaTime);
        }

        private void UpdateControllerHeight(float deltaTime)
        {
            var currentHeight = _characterController.height;
            var nextHeight = Mathf.Lerp(currentHeight, _currentTargetHeight, _config.HeightLerpSpeed * deltaTime);
            ApplyControllerHeight(nextHeight);
        }

        private void ApplyControllerHeight(float nextHeight)
        {
            _characterController.height = nextHeight;
            _characterController.center = _initialControllerCenter;
        }

        private void UpdateFootsteps()
        {
            var currentPlanarPosition = _characterController.transform.position;
            currentPlanarPosition.y = 0f;

            var previousPlanarPosition = _lastPlanarPosition;
            previousPlanarPosition.y = 0f;

            var planarDistance = Vector3.Distance(previousPlanarPosition, currentPlanarPosition);
            _lastPlanarPosition = _characterController.transform.position;

            if (!_characterController.isGrounded || _horizontalVelocity.sqrMagnitude <= 0.04f)
            {
                _stepDistanceAccumulator = 0f;
                return;
            }

            _stepDistanceAccumulator += planarDistance;
            var stepDistance = ResolveStepDistance();

            if (_stepDistanceAccumulator < stepDistance)
            {
                return;
            }

            _stepDistanceAccumulator = 0f;
            _footstepPublisher.Publish(new FootstepEvent(ResolveFootstepVolume()));
        }

        private float ResolveStepDistance()
        {
            if (_isCrouching)
            {
                return _config.WalkStepDistance;
            }

            return _isSprintRequested ? _config.SprintStepDistance : _config.WalkStepDistance;
        }

        private float ResolveFootstepVolume()
        {
            if (_isCrouching)
            {
                return _config.CrouchStepVolume;
            }

            return _isSprintRequested ? _config.SprintStepVolume : _config.WalkStepVolume;
        }
    }
}
