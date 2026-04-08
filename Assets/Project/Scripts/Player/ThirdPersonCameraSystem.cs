// Path: Assets/Project/Scpripts/Player/ThirdPersonCameraSystem.cs
// Purpose: Controls an over-the-shoulder third-person camera with lag, zoom, sprint offset and wall x-ray fading.
// Dependencies: MessagePipe, PlayerInput, PlayerWeight, Unity Input System, UnityEngine, VContainer.

using System;
using System.Collections.Generic;
using MessagePipe;
using ProjectResonance.MobileControls;
using ProjectResonance.PlayerInput;
using ProjectResonance.PlayerWeight;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace ProjectResonance.ThirdPersonCamera
{
    /// <summary>
    /// Defines the active third-person camera mode.
    /// </summary>
    public enum CameraDistanceMode
    {
        /// <summary>
        /// Default exploration mode.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Closer combat mode.
        /// </summary>
        Combat = 1,

        /// <summary>
        /// Wider construction mode.
        /// </summary>
        Build = 2,
    }

    /// <summary>
    /// ScriptableObject with all third-person camera tuning values.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "Project Resonance/Player/Camera Config")]
    public sealed class CameraConfig : ScriptableObject
    {
        [Header("Distances")]
        [SerializeField]
        [Min(0.1f)]
        private float _normalDistance = 3.5f;

        [SerializeField]
        [Min(0.1f)]
        private float _combatDistance = 2.5f;

        [SerializeField]
        [Min(0.1f)]
        private float _buildDistance = 5f;

        [SerializeField]
        [Min(0f)]
        private float _sprintDistanceOffset = 0.5f;

        [SerializeField]
        [Min(0.1f)]
        private float _minZoomDistance = 2f;

        [SerializeField]
        [Min(0.1f)]
        private float _maxZoomDistance = 6f;

        [SerializeField]
        [Min(0.01f)]
        private float _zoomSensitivity = 0.1f;

        [Header("Rotation")]
        [SerializeField]
        private Vector3 _shoulderOffset = new Vector3(0.55f, 0.2f, 0f);

        [SerializeField]
        [Min(0f)]
        private float _yawSensitivity = 0.12f;

        [SerializeField]
        [Min(0f)]
        private float _pitchSensitivity = 0.1f;

        [SerializeField]
        private float _minPitch = -30f;

        [SerializeField]
        private float _maxPitch = 70f;

        [Header("Lag")]
        [SerializeField]
        [Min(0.01f)]
        private float _cameraLag = 0.1f;

        [Header("X-Ray")]
        [SerializeField]
        private LayerMask _xRayMask = ~0;

        [SerializeField]
        [Range(0f, 1f)]
        private float _occludedAlpha = 0.25f;

        [SerializeField]
        [Min(0.1f)]
        private float _fadeSpeed = 6f;

        [SerializeField]
        [Min(0f)]
        private float _castRadius = 0.15f;

        /// <summary>
        /// Gets the normal camera distance.
        /// </summary>
        public float NormalDistance => _normalDistance;

        /// <summary>
        /// Gets the combat camera distance.
        /// </summary>
        public float CombatDistance => _combatDistance;

        /// <summary>
        /// Gets the construction camera distance.
        /// </summary>
        public float BuildDistance => _buildDistance;

        /// <summary>
        /// Gets the extra distance applied while sprinting.
        /// </summary>
        public float SprintDistanceOffset => _sprintDistanceOffset;

        /// <summary>
        /// Gets the minimum zoom distance.
        /// </summary>
        public float MinZoomDistance => _minZoomDistance;

        /// <summary>
        /// Gets the maximum zoom distance.
        /// </summary>
        public float MaxZoomDistance => _maxZoomDistance;

        /// <summary>
        /// Gets the zoom sensitivity.
        /// </summary>
        public float ZoomSensitivity => _zoomSensitivity;

        /// <summary>
        /// Gets the shoulder offset.
        /// </summary>
        public Vector3 ShoulderOffset => _shoulderOffset;

        /// <summary>
        /// Gets the yaw sensitivity.
        /// </summary>
        public float YawSensitivity => _yawSensitivity;

        /// <summary>
        /// Gets the pitch sensitivity.
        /// </summary>
        public float PitchSensitivity => _pitchSensitivity;

        /// <summary>
        /// Gets the minimum pitch angle.
        /// </summary>
        public float MinPitch => _minPitch;

        /// <summary>
        /// Gets the maximum pitch angle.
        /// </summary>
        public float MaxPitch => _maxPitch;

        /// <summary>
        /// Gets the camera lag time.
        /// </summary>
        public float CameraLag => _cameraLag;

        /// <summary>
        /// Gets the x-ray layer mask.
        /// </summary>
        public LayerMask XRayMask => _xRayMask;

        /// <summary>
        /// Gets the target alpha used for occluding walls.
        /// </summary>
        public float OccludedAlpha => _occludedAlpha;

        /// <summary>
        /// Gets the fade speed used for x-ray transitions.
        /// </summary>
        public float FadeSpeed => _fadeSpeed;

        /// <summary>
        /// Gets the sphere cast radius used to detect occluders.
        /// </summary>
        public float CastRadius => _castRadius;
    }

    /// <summary>
    /// Updates the third-person camera after gameplay movement.
    /// </summary>
    public sealed class ThirdPersonCameraSystem : IStartable, ILateTickable, IDisposable
    {
        private readonly Camera _playerCamera;
        private readonly PlayerCameraTarget _cameraTarget;
        private readonly CameraConfig _config;
        private readonly MobileControlsConfig _mobileControlsConfig;
        private readonly IMobileModeService _mobileModeService;
        private readonly PlayerWeightState _weightState;
        private readonly IBufferedSubscriber<SprintInput> _sprintInputSubscriber;
        private readonly IBufferedSubscriber<WeightChangedEvent> _weightChangedSubscriber;

        private readonly Dictionary<Renderer, OccluderState> _occluderStates = new Dictionary<Renderer, OccluderState>();
        private readonly HashSet<Renderer> _visibleOccluders = new HashSet<Renderer>();
        private readonly List<Renderer> _cleanupBuffer = new List<Renderer>();

        private IDisposable _sprintSubscription;
        private IDisposable _weightSubscription;

        private Vector3 _followPosition;
        private Vector3 _followVelocity;
        private float _yaw;
        private float _pitch;
        private float _zoomDistance;
        private bool _isSprintPressed;
        private PlayerWeightType _currentWeight;

        /// <summary>
        /// Creates the third-person camera system.
        /// </summary>
        /// <param name="playerCamera">Runtime player camera.</param>
        /// <param name="cameraTarget">Follow target anchor.</param>
        /// <param name="config">Camera configuration.</param>
        /// <param name="mobileControlsConfig">Mobile UI and camera configuration.</param>
        /// <param name="mobileModeService">Mobile mode state service.</param>
        /// <param name="weightState">Runtime weight state.</param>
        /// <param name="sprintInputSubscriber">Sprint subscriber.</param>
        /// <param name="weightChangedSubscriber">Weight state subscriber.</param>
        public ThirdPersonCameraSystem(
            Camera playerCamera,
            PlayerCameraTarget cameraTarget,
            CameraConfig config,
            MobileControlsConfig mobileControlsConfig,
            IMobileModeService mobileModeService,
            PlayerWeightState weightState,
            IBufferedSubscriber<SprintInput> sprintInputSubscriber,
            IBufferedSubscriber<WeightChangedEvent> weightChangedSubscriber)
        {
            _playerCamera = playerCamera;
            _cameraTarget = cameraTarget;
            _config = config;
            _mobileControlsConfig = mobileControlsConfig;
            _mobileModeService = mobileModeService;
            _weightState = weightState;
            _sprintInputSubscriber = sprintInputSubscriber;
            _weightChangedSubscriber = weightChangedSubscriber;
        }

        /// <summary>
        /// Initializes camera runtime state and subscribes to player events.
        /// </summary>
        public void Start()
        {
            var eulerAngles = _playerCamera.transform.rotation.eulerAngles;
            _yaw = eulerAngles.y;
            _pitch = NormalizePitch(eulerAngles.x);
            _followPosition = _cameraTarget.FollowPosition;
            _zoomDistance = Mathf.Clamp(_config.NormalDistance, _config.MinZoomDistance, _config.MaxZoomDistance);
            _currentWeight = _weightState.CurrentWeight;

            _sprintSubscription = _sprintInputSubscriber.Subscribe(OnSprintInputChanged);
            _weightSubscription = _weightChangedSubscriber.Subscribe(OnWeightChanged);
        }

        /// <summary>
        /// Updates the third-person camera after the player movement step.
        /// </summary>
        public void LateTick()
        {
            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            UpdateLookInput();
            UpdateZoomInput();
            UpdateFollow(deltaTime);
            UpdateOccluders(deltaTime);
        }

        /// <summary>
        /// Releases subscriptions and restores faded renderers.
        /// </summary>
        public void Dispose()
        {
            _sprintSubscription?.Dispose();
            _weightSubscription?.Dispose();

            foreach (var pair in _occluderStates)
            {
                pair.Value.Restore();
            }

            _occluderStates.Clear();
            _visibleOccluders.Clear();
            _cleanupBuffer.Clear();
        }

        private void OnSprintInputChanged(SprintInput message)
        {
            _isSprintPressed = message.IsPressed;
        }

        private void OnWeightChanged(WeightChangedEvent message)
        {
            _currentWeight = message.CurrentWeight;
        }

        private void UpdateLookInput()
        {
            if (IsMobileModeActive())
            {
                UpdateMobileLookInput();
                return;
            }

            if (Mouse.current == null)
            {
                return;
            }

            var mouseDelta = Mouse.current.delta.ReadValue();
            _yaw += mouseDelta.x * _config.YawSensitivity;
            _pitch = Mathf.Clamp(_pitch - (mouseDelta.y * _config.PitchSensitivity), _config.MinPitch, _config.MaxPitch);
        }

        private void UpdateZoomInput()
        {
            if (IsMobileModeActive())
            {
                _zoomDistance = ResolveMobileDistance();
                return;
            }

            if (Mouse.current == null)
            {
                return;
            }

            var scroll = Mouse.current.scroll.ReadValue();
            if (Mathf.Abs(scroll.y) <= Mathf.Epsilon)
            {
                return;
            }

            _zoomDistance = Mathf.Clamp(
                _zoomDistance - (scroll.y * _config.ZoomSensitivity * 0.01f),
                _config.MinZoomDistance,
                _config.MaxZoomDistance);
        }

        private void UpdateFollow(float deltaTime)
        {
            _followPosition = Vector3.SmoothDamp(
                _followPosition,
                _cameraTarget.FollowPosition,
                ref _followVelocity,
                _config.CameraLag,
                Mathf.Infinity,
                deltaTime);

            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            var authoredShoulderOffset = IsMobileModeActive() && _mobileControlsConfig != null
                ? _mobileControlsConfig.MobileCameraShoulderOffset
                : _config.ShoulderOffset;
            var shoulderOffset = rotation * authoredShoulderOffset;
            var focusPoint = _followPosition + shoulderOffset;
            var distance = ResolveDistance();
            var cameraPosition = focusPoint - (rotation * Vector3.forward * distance);

            _playerCamera.transform.SetPositionAndRotation(cameraPosition, rotation);
        }

        private float ResolveDistance()
        {
            if (IsMobileModeActive())
            {
                return ResolveMobileDistance();
            }

            var modeDistance = _config.NormalDistance;

            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                modeDistance = _config.CombatDistance;
            }
            else if (_currentWeight == PlayerWeightType.HeavyLog || _currentWeight == PlayerWeightType.TwoLogs)
            {
                // Carrying construction resources uses the wider framing from the design document.
                modeDistance = _config.BuildDistance;
            }

            var zoomDelta = _zoomDistance - _config.NormalDistance;
            var sprintOffset = _isSprintPressed ? _config.SprintDistanceOffset : 0f;
            return Mathf.Clamp(modeDistance + zoomDelta + sprintOffset, _config.MinZoomDistance, _config.MaxZoomDistance);
        }

        private void UpdateMobileLookInput()
        {
            _yaw = _mobileControlsConfig != null
                ? _mobileControlsConfig.MobileCameraYaw
                : _yaw;
            _pitch = _mobileControlsConfig != null
                ? _mobileControlsConfig.MobileCameraPitch
                : 22f;
        }

        private float ResolveMobileDistance()
        {
            if (_mobileControlsConfig == null)
            {
                return Mathf.Clamp(_config.NormalDistance, _config.MinZoomDistance, _config.MaxZoomDistance);
            }

            return Mathf.Clamp(
                _mobileControlsConfig.MobileCameraDistance,
                _config.MinZoomDistance,
                _config.MaxZoomDistance);
        }

        private bool IsMobileModeActive()
        {
            return _mobileModeService != null && _mobileModeService.IsMobileModeActive;
        }

        private void UpdateOccluders(float deltaTime)
        {
            _visibleOccluders.Clear();

            var targetPosition = _cameraTarget.FollowPosition;
            var cameraPosition = _playerCamera.transform.position;
            var direction = cameraPosition - targetPosition;
            var distance = direction.magnitude;

            if (distance > 0.001f)
            {
                direction /= distance;

                var hits = Physics.SphereCastAll(
                    targetPosition,
                    _config.CastRadius,
                    direction,
                    distance,
                    _config.XRayMask,
                    QueryTriggerInteraction.Ignore);

                for (var index = 0; index < hits.Length; index++)
                {
                    var renderer = hits[index].collider.GetComponentInParent<Renderer>();
                    if (renderer == null ||
                        renderer.transform.IsChildOf(_cameraTarget.transform) ||
                        renderer.transform.root == _cameraTarget.transform.root)
                    {
                        continue;
                    }

                    _visibleOccluders.Add(renderer);
                    if (!_occluderStates.ContainsKey(renderer))
                    {
                        _occluderStates.Add(renderer, new OccluderState(renderer, _config.OccludedAlpha));
                    }
                }
            }

            _cleanupBuffer.Clear();
            foreach (var pair in _occluderStates)
            {
                var targetAlpha = _visibleOccluders.Contains(pair.Key) ? _config.OccludedAlpha : 1f;
                var isFinished = pair.Value.Tick(targetAlpha, _config.FadeSpeed, deltaTime);

                if (isFinished && targetAlpha >= 0.999f)
                {
                    _cleanupBuffer.Add(pair.Key);
                }
            }

            for (var index = 0; index < _cleanupBuffer.Count; index++)
            {
                _occluderStates.Remove(_cleanupBuffer[index]);
            }
        }

        private static float NormalizePitch(float rawPitch)
        {
            if (rawPitch > 180f)
            {
                rawPitch -= 360f;
            }

            return rawPitch;
        }

        private sealed class OccluderState
        {
            private readonly Renderer _renderer;
            private readonly MaterialPropertyBlock _propertyBlock;
            private readonly int _colorPropertyId;
            private readonly Color _baseColor;
            private readonly float _minimumAlpha;

            private float _currentAlpha = 1f;

            /// <summary>
            /// Creates a new occluder fade state.
            /// </summary>
            /// <param name="renderer">Target renderer.</param>
            /// <param name="minimumAlpha">Minimum alpha during x-ray fade.</param>
            public OccluderState(Renderer renderer, float minimumAlpha)
            {
                _renderer = renderer;
                _propertyBlock = new MaterialPropertyBlock();
                _minimumAlpha = minimumAlpha;

                var sharedMaterial = renderer.sharedMaterial;
                if (sharedMaterial != null && sharedMaterial.HasProperty("_BaseColor"))
                {
                    _colorPropertyId = Shader.PropertyToID("_BaseColor");
                    _baseColor = sharedMaterial.GetColor(_colorPropertyId);
                }
                else if (sharedMaterial != null && sharedMaterial.HasProperty("_Color"))
                {
                    _colorPropertyId = Shader.PropertyToID("_Color");
                    _baseColor = sharedMaterial.GetColor(_colorPropertyId);
                }
                else
                {
                    _colorPropertyId = -1;
                    _baseColor = Color.white;
                }
            }

            /// <summary>
            /// Advances the occluder fade.
            /// </summary>
            /// <param name="targetAlpha">Target alpha value.</param>
            /// <param name="fadeSpeed">Fade speed.</param>
            /// <param name="deltaTime">Frame delta time.</param>
            /// <returns>True when the renderer is fully restored and can be removed from tracking.</returns>
            public bool Tick(float targetAlpha, float fadeSpeed, float deltaTime)
            {
                if (_colorPropertyId < 0)
                {
                    return targetAlpha >= 0.999f;
                }

                _currentAlpha = Mathf.MoveTowards(_currentAlpha, Mathf.Clamp(targetAlpha, _minimumAlpha, 1f), fadeSpeed * deltaTime);

                var color = _baseColor;
                color.a = _currentAlpha;

                _propertyBlock.Clear();
                _propertyBlock.SetColor(_colorPropertyId, color);
                _renderer.SetPropertyBlock(_propertyBlock);

                if (_currentAlpha >= 0.999f && targetAlpha >= 0.999f)
                {
                    Restore();
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Restores the renderer to its original state.
            /// </summary>
            public void Restore()
            {
                _renderer.SetPropertyBlock(null);
            }
        }
    }
}
