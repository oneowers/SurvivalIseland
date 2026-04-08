// Path: Assets/Project/Scripts/MobileControls/MobileControlsConfig.cs
// Purpose: Stores authoring data for scene-authored mobile controls visibility and fixed mobile camera behavior.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.MobileControls
{
    /// <summary>
    /// Defines how mobile controls become visible at runtime.
    /// </summary>
    public enum MobileControlsVisibilityMode
    {
        /// <summary>
        /// Show mobile controls in the Unity Editor and on mobile platforms.
        /// </summary>
        EditorAndMobile = 0,

        /// <summary>
        /// Show mobile controls only on mobile platforms.
        /// </summary>
        MobileOnly = 1,

        /// <summary>
        /// Visibility is controlled only by the manual toggle.
        /// </summary>
        Manual = 2,
    }

    /// <summary>
    /// ScriptableObject that defines scene-authored mobile controls visibility and fixed camera tuning.
    /// </summary>
    [CreateAssetMenu(fileName = "MobileControlsConfig", menuName = "Project Resonance/Mobile/Mobile Controls Config")]
    public sealed class MobileControlsConfig : ScriptableObject
    {
        [Header("Visibility")]
        [SerializeField]
        private MobileControlsVisibilityMode _visibilityMode = MobileControlsVisibilityMode.EditorAndMobile;

        [SerializeField]
        private bool _manualModeActive;

        [Header("Input")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _joystickDeadZone = 0.08f;

        [Header("Mobile Camera")]
        [SerializeField]
        private float _mobileCameraYaw = -32f;

        [SerializeField]
        private float _mobileCameraPitch = 22f;

        [SerializeField]
        [Min(0.1f)]
        private float _mobileCameraDistance = 4.4f;

        [SerializeField]
        private Vector3 _mobileCameraShoulderOffset = new Vector3(0.2f, 0.8f, 0f);

        /// <summary>
        /// Gets the runtime visibility mode for mobile controls.
        /// </summary>
        public MobileControlsVisibilityMode VisibilityMode => _visibilityMode;

        /// <summary>
        /// Gets whether manual mode should currently be enabled.
        /// </summary>
        public bool ManualModeActive => _manualModeActive;

        /// <summary>
        /// Gets the joystick dead zone used before move input is published.
        /// </summary>
        public float JoystickDeadZone => Mathf.Clamp01(_joystickDeadZone);

        /// <summary>
        /// Gets the fixed yaw used by the mobile camera.
        /// </summary>
        public float MobileCameraYaw => _mobileCameraYaw;

        /// <summary>
        /// Gets the fixed pitch used by the mobile camera.
        /// </summary>
        public float MobileCameraPitch => _mobileCameraPitch;

        /// <summary>
        /// Gets the fixed follow distance used by the mobile camera.
        /// </summary>
        public float MobileCameraDistance => Mathf.Max(0.1f, _mobileCameraDistance);

        /// <summary>
        /// Gets the follow offset used by the mobile camera.
        /// </summary>
        public Vector3 MobileCameraShoulderOffset => _mobileCameraShoulderOffset;

        private void OnValidate()
        {
            _joystickDeadZone = Mathf.Clamp01(_joystickDeadZone);
            _mobileCameraDistance = Mathf.Max(0.1f, _mobileCameraDistance);
        }
    }
}
