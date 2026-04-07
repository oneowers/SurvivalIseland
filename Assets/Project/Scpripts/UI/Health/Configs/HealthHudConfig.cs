// Path: Assets/Project/Scpripts/UI/Health/Configs/HealthHudConfig.cs
// Purpose: Stores visual tuning for the runtime health HUD.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.HealthUI
{
    /// <summary>
    /// ScriptableObject with tuning for the player health HUD.
    /// </summary>
    [CreateAssetMenu(fileName = "HealthHudConfig", menuName = "Project Resonance/UI/Health HUD Config")]
    public sealed class HealthHudConfig : ScriptableObject
    {
        [Header("Fill")]
        [SerializeField]
        private Color _normalFillColor = new Color(0.81f, 0.18f, 0.18f, 1f);

        [SerializeField]
        private Color _criticalFillColor = new Color(1f, 0.58f, 0.12f, 1f);

        [SerializeField]
        private Color _damageFillColor = new Color(0.39f, 0.08f, 0.08f, 0.95f);

        [SerializeField]
        private Color _flashColor = new Color(1f, 0.42f, 0.42f, 0.65f);

        [SerializeField]
        private Color _idleFlashColor = new Color(1f, 1f, 1f, 0f);

        [Header("Thresholds")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _criticalThresholdNormalized = 0.25f;

        [Header("Animation")]
        [SerializeField]
        [Min(0.01f)]
        private float _fillLerpSpeed = 8f;

        [SerializeField]
        [Min(0.01f)]
        private float _damageLagSpeed = 2.5f;

        [SerializeField]
        [Min(0f)]
        private float _damageLagDelay = 0.35f;

        [SerializeField]
        [Min(0.01f)]
        private float _flashFadeSpeed = 9f;

        [Header("Text")]
        [SerializeField]
        private string _healthFormat = "{0:0}/{1:0}";

        /// <summary>
        /// Gets the normal health fill color.
        /// </summary>
        public Color NormalFillColor => _normalFillColor;

        /// <summary>
        /// Gets the critical health fill color.
        /// </summary>
        public Color CriticalFillColor => _criticalFillColor;

        /// <summary>
        /// Gets the delayed damage fill color.
        /// </summary>
        public Color DamageFillColor => _damageFillColor;

        /// <summary>
        /// Gets the damage flash color.
        /// </summary>
        public Color FlashColor => _flashColor;

        /// <summary>
        /// Gets the idle flash color.
        /// </summary>
        public Color IdleFlashColor => _idleFlashColor;

        /// <summary>
        /// Gets the health threshold that marks the player as critical.
        /// </summary>
        public float CriticalThresholdNormalized => _criticalThresholdNormalized;

        /// <summary>
        /// Gets the interpolation speed for the main fill.
        /// </summary>
        public float FillLerpSpeed => _fillLerpSpeed;

        /// <summary>
        /// Gets the interpolation speed for the delayed damage fill.
        /// </summary>
        public float DamageLagSpeed => _damageLagSpeed;

        /// <summary>
        /// Gets the delay before the damage fill starts catching up.
        /// </summary>
        public float DamageLagDelay => _damageLagDelay;

        /// <summary>
        /// Gets the fade speed for the damage flash.
        /// </summary>
        public float FlashFadeSpeed => _flashFadeSpeed;

        /// <summary>
        /// Gets the label format used for current and max health.
        /// </summary>
        public string HealthFormat => _healthFormat;
    }
}
