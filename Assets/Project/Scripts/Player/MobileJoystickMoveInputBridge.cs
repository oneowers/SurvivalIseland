// Path: Assets/Project/Scripts/Player/MobileJoystickMoveInputBridge.cs
// Purpose: Reads mobile joystick input and forwards it into the unified player input pipeline.
// Dependencies: UnityEngine, VContainer, Joystick Pack, ProjectResonance.MobileControls, PlayerInputHandler.

using ProjectResonance.MobileControls;
using UnityEngine;
using VContainer;

namespace ProjectResonance.PlayerInput
{
    /// <summary>
    /// Bridges a scene-authored mobile joystick into the shared player movement input stream.
    /// </summary>
    [AddComponentMenu("Project Resonance/Player/Mobile Joystick Move Input Bridge")]
    [DisallowMultipleComponent]
    public sealed class MobileJoystickMoveInputBridge : MonoBehaviour
    {
        [Header("Joystick")]
        [SerializeField]
        private Joystick _joystick;

        [SerializeField]
        [Range(0f, 1f)]
        private float _publishDeadZone = 0.05f;

        [Header("Axis")]
        [SerializeField]
        private bool _invertHorizontal;

        [SerializeField]
        private bool _invertVertical;

        private PlayerInputHandler _playerInputHandler;
        private MobileControlsConfig _mobileControlsConfig;
        private Vector2 _lastPublishedInput = new Vector2(float.NaN, float.NaN);

        [Inject]
        private void Construct(PlayerInputHandler playerInputHandler, MobileControlsConfig mobileControlsConfig)
        {
            _playerInputHandler = playerInputHandler;
            _mobileControlsConfig = mobileControlsConfig;
            ApplyAuthoredDeadZone();
        }

        private void Reset()
        {
            _joystick = GetComponent<Joystick>();
        }

        private void Awake()
        {
            if (_joystick == null)
            {
                _joystick = GetComponent<Joystick>();
            }

            ApplyAuthoredDeadZone();
        }

        private void Update()
        {
            if (_playerInputHandler == null || _joystick == null)
            {
                return;
            }

            var moveInput = ReadMoveInput();
            if (AreApproximatelyEqual(_lastPublishedInput, moveInput))
            {
                return;
            }

            _lastPublishedInput = moveInput;
            _playerInputHandler.SetExternalMoveInput(moveInput);
        }

        private void OnDisable()
        {
            _lastPublishedInput = new Vector2(float.NaN, float.NaN);

            if (_playerInputHandler != null)
            {
                _playerInputHandler.ClearExternalMoveInput();
            }
        }

        private Vector2 ReadMoveInput()
        {
            var rawInput = ResolveRawInput();
            var horizontal = _invertHorizontal ? -rawInput.x : rawInput.x;
            var vertical = _invertVertical ? -rawInput.y : rawInput.y;
            var moveInput = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);

            // A local dead zone prevents tiny UI jitter from stealing control from keyboard/gamepad.
            if (moveInput.sqrMagnitude <= _publishDeadZone * _publishDeadZone)
            {
                return Vector2.zero;
            }

            return moveInput;
        }

        private Vector2 ResolveRawInput()
        {
            return new Vector2(_joystick.Horizontal, _joystick.Vertical);
        }

        private static bool AreApproximatelyEqual(Vector2 left, Vector2 right)
        {
            return (left - right).sqrMagnitude <= 0.0001f;
        }

        private void ApplyAuthoredDeadZone()
        {
            _publishDeadZone = _mobileControlsConfig != null
                ? _mobileControlsConfig.JoystickDeadZone
                : Mathf.Clamp01(_publishDeadZone);

            if (_joystick != null)
            {
                _joystick.DeadZone = _publishDeadZone;
            }
        }
    }
}
