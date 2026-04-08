// Path: Assets/Project/Scripts/Player/AimTargetingConfig.cs
// Purpose: Stores generic authored tuning for right-stick aim target selection and facing.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.PlayerCombat
{
    /// <summary>
    /// ScriptableObject with all right-stick aim targeting tuning values.
    /// </summary>
    [CreateAssetMenu(fileName = "AimTargetingConfig", menuName = "Project Resonance/Player/Aim Targeting Config")]
    public sealed class AimTargetingConfig : ScriptableObject
    {
        [Header("Selection")]
        [SerializeField]
        [Min(0.1f)]
        private float _maxAimRadius = 3f;

        [SerializeField]
        [Min(0.01f)]
        private float _scanFrequencySeconds = 0.05f;

        [SerializeField]
        private LayerMask _broadphaseLayerMask = ~0;

        [SerializeField]
        [Min(1)]
        private int _maxDetectedColliders = 32;

        [SerializeField]
        [Min(0f)]
        private float _targetAnchorHeightBias = 0.9f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _selectionDeadZone = 0.08f;

        [Header("Facing")]
        [SerializeField]
        [Min(0.1f)]
        private float _facingRotationSpeed = 14f;

        [Header("Feedback")]
        [SerializeField]
        private Color _selectionFlashColor = new Color(1f, 0.78f, 0.28f, 1f);

        [SerializeField]
        [Min(0.05f)]
        private float _selectionFlashDuration = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _selectionFlashStrength = 0.7f;

        [Header("Planting Preview")]
        [SerializeField]
        private Color _plantingPreviewValidColor = new Color(0.34f, 0.92f, 0.45f, 0.42f);

        [SerializeField]
        private Color _plantingPreviewInvalidColor = new Color(1f, 0.28f, 0.22f, 0.26f);

        [SerializeField]
        [Min(0f)]
        private float _plantingPreviewHeightOffset = 0.02f;

        /// <summary>
        /// Gets the maximum world radius reachable by the aim stick.
        /// </summary>
        public float MaxAimRadius => Mathf.Max(0.1f, _maxAimRadius > 0f ? _maxAimRadius : 3f);

        /// <summary>
        /// Gets the frequency used to rescan nearby candidates.
        /// </summary>
        public float ScanFrequencySeconds => Mathf.Max(0.01f, _scanFrequencySeconds);

        /// <summary>
        /// Gets the broadphase layer mask used while scanning for nearby targets.
        /// </summary>
        public LayerMask BroadphaseLayerMask => _broadphaseLayerMask;

        /// <summary>
        /// Gets the maximum number of colliders processed during each scan.
        /// </summary>
        public int MaxDetectedColliders => Mathf.Max(1, _maxDetectedColliders);

        /// <summary>
        /// Gets the vertical bias applied to target anchors.
        /// </summary>
        public float TargetAnchorHeightBias => Mathf.Max(0f, _targetAnchorHeightBias);

        /// <summary>
        /// Gets the joystick dead zone below which aim targeting is considered inactive.
        /// </summary>
        public float SelectionDeadZone => Mathf.Clamp01(_selectionDeadZone);

        /// <summary>
        /// Gets the player-facing rotation speed while the aim stick is active.
        /// </summary>
        public float FacingRotationSpeed => Mathf.Max(0.1f, _facingRotationSpeed);

        /// <summary>
        /// Gets the flash color shown when the joystick selects a target.
        /// </summary>
        public Color SelectionFlashColor => _selectionFlashColor.a > 0f ? _selectionFlashColor : new Color(1f, 0.78f, 0.28f, 1f);

        /// <summary>
        /// Gets the duration of the target-selection flash.
        /// </summary>
        public float SelectionFlashDuration => _selectionFlashDuration > 0.05f ? _selectionFlashDuration : 1f;

        /// <summary>
        /// Gets the blend strength of the target-selection flash.
        /// </summary>
        public float SelectionFlashStrength => _selectionFlashStrength > 0f ? Mathf.Clamp01(_selectionFlashStrength) : 0.85f;

        /// <summary>
        /// Gets the color used when the planting preview is currently valid.
        /// </summary>
        public Color PlantingPreviewValidColor => _plantingPreviewValidColor.a > 0f ? _plantingPreviewValidColor : new Color(0.34f, 0.92f, 0.45f, 0.42f);

        /// <summary>
        /// Gets the color used when the planting preview is blocked or invalid.
        /// </summary>
        public Color PlantingPreviewInvalidColor => _plantingPreviewInvalidColor.a > 0f ? _plantingPreviewInvalidColor : new Color(1f, 0.28f, 0.22f, 0.26f);

        /// <summary>
        /// Gets the small vertical offset applied so the planting preview does not z-fight with the ground.
        /// </summary>
        public float PlantingPreviewHeightOffset => Mathf.Max(0f, _plantingPreviewHeightOffset);

        private void OnValidate()
        {
            _maxAimRadius = Mathf.Max(0.1f, _maxAimRadius);
            _scanFrequencySeconds = Mathf.Max(0.01f, _scanFrequencySeconds);
            _maxDetectedColliders = Mathf.Max(1, _maxDetectedColliders);
            _targetAnchorHeightBias = Mathf.Max(0f, _targetAnchorHeightBias);
            _selectionDeadZone = Mathf.Clamp01(_selectionDeadZone);
            _facingRotationSpeed = Mathf.Max(0.1f, _facingRotationSpeed);
            _selectionFlashDuration = Mathf.Max(0.05f, _selectionFlashDuration);
            _selectionFlashStrength = Mathf.Clamp01(_selectionFlashStrength);
            _plantingPreviewHeightOffset = Mathf.Max(0f, _plantingPreviewHeightOffset);
        }
    }
}
