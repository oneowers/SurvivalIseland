// Path: Assets/Project/Scripts/Player/MobileAimJoystickBridge.cs
// Purpose: Reads the authored right-side mobile joystick and forwards it into the shared aim input stream.
// Dependencies: UnityEngine, UnityEngine.EventSystems, VContainer, Joystick Pack, ProjectResonance.MobileControls, PlayerInputHandler.

using ProjectResonance.MobileControls;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

namespace ProjectResonance.PlayerInput
{
    /// <summary>
    /// Bridges a scene-authored mobile joystick into the shared player aim input stream.
    /// </summary>
    [AddComponentMenu("Project Resonance/Player/Mobile Aim Joystick Bridge")]
    [DisallowMultipleComponent]
    public sealed class MobileAimJoystickBridge : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
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
        private IMobileModeService _mobileModeService;
        private Vector2 _lastPublishedInput = new Vector2(float.NaN, float.NaN);
        private bool _isPointerActive;

        [Inject]
        private void Construct(PlayerInputHandler playerInputHandler, MobileControlsConfig mobileControlsConfig, IMobileModeService mobileModeService)
        {
            _playerInputHandler = playerInputHandler;
            _mobileControlsConfig = mobileControlsConfig;
            _mobileModeService = mobileModeService;
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

            var aimInput = ResolvePublishedAimInput();
            if (AreApproximatelyEqual(_lastPublishedInput, aimInput))
            {
                return;
            }

            _lastPublishedInput = aimInput;
            _playerInputHandler.SetExternalAimInput(aimInput);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPointerActive = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            _isPointerActive = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPointerActive = false;
            ForceClearInput();
        }

        private void OnDisable()
        {
            _isPointerActive = false;
            _lastPublishedInput = new Vector2(float.NaN, float.NaN);

            if (_playerInputHandler != null)
            {
                _playerInputHandler.ClearExternalAimInput();
            }
        }

        private Vector2 ResolvePublishedAimInput()
        {
            if (_mobileModeService != null && !_mobileModeService.IsMobileModeActive)
            {
                return Vector2.zero;
            }

            return _isPointerActive ? ReadAimInput() : Vector2.zero;
        }

        private Vector2 ReadAimInput()
        {
            var rawInput = new Vector2(_joystick.Horizontal, _joystick.Vertical);
            var horizontal = _invertHorizontal ? -rawInput.x : rawInput.x;
            var vertical = _invertVertical ? -rawInput.y : rawInput.y;
            var aimInput = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);

            if (aimInput.sqrMagnitude <= _publishDeadZone * _publishDeadZone)
            {
                return Vector2.zero;
            }

            return aimInput;
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

        private void ForceClearInput()
        {
            _lastPublishedInput = new Vector2(float.NaN, float.NaN);

            if (_playerInputHandler != null)
            {
                _playerInputHandler.ClearExternalAimInput();
            }
        }
    }
}
