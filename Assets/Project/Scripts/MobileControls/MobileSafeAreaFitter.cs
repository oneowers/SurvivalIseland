// Path: Assets/Project/Scripts/MobileControls/MobileSafeAreaFitter.cs
// Purpose: Fits a scene-authored mobile UI rect into the device safe area so controls avoid notches and system bars.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.MobileControls
{
    /// <summary>
    /// Applies the current device safe area to a RectTransform.
    /// </summary>
    [AddComponentMenu("Project Resonance/Mobile/Mobile Safe Area Fitter")]
    [DisallowMultipleComponent]
    public sealed class MobileSafeAreaFitter : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _targetRect;

        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        /// <summary>
        /// Initializes the fitter with the target rect that should be clamped into the safe area.
        /// </summary>
        /// <param name="targetRect">Rect to fit into the safe area.</param>
        public void Initialize(RectTransform targetRect)
        {
            _targetRect = targetRect;
            ApplySafeArea(force: true);
        }

        private void Awake()
        {
            if (_targetRect == null)
            {
                _targetRect = transform as RectTransform;
            }
        }

        private void LateUpdate()
        {
            ApplySafeArea(force: false);
        }

        private void ApplySafeArea(bool force)
        {
            if (_targetRect == null)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (!force && safeArea == _lastSafeArea && screenSize == _lastScreenSize)
            {
                return;
            }

            _lastSafeArea = safeArea;
            _lastScreenSize = screenSize;

            if (screenSize.x <= 0 || screenSize.y <= 0)
            {
                return;
            }

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= screenSize.x;
            anchorMin.y /= screenSize.y;
            anchorMax.x /= screenSize.x;
            anchorMax.y /= screenSize.y;

            _targetRect.anchorMin = anchorMin;
            _targetRect.anchorMax = anchorMax;
            _targetRect.offsetMin = Vector2.zero;
            _targetRect.offsetMax = Vector2.zero;
        }
    }
}
