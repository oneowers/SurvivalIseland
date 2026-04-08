// Path: Assets/Project/Scripts/MobileControls/MobileControlsPresenter.cs
// Purpose: Controls visibility and interaction state of the scene-authored mobile controls root.
// Dependencies: UnityEngine, VContainer.

using UnityEngine;
using VContainer;

namespace ProjectResonance.MobileControls
{
    /// <summary>
    /// Presenter that toggles the scene-authored mobile controls root according to the current mobile mode.
    /// </summary>
    [AddComponentMenu("Project Resonance/Mobile/Mobile Controls Presenter")]
    [DisallowMultipleComponent]
    public sealed class MobileControlsPresenter : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup _canvasGroup;

        private IMobileModeService _mobileModeService;

        [Inject]
        private void Construct(IMobileModeService mobileModeService)
        {
            _mobileModeService = mobileModeService;
        }

        /// <summary>
        /// Applies the current mobile mode state immediately.
        /// </summary>
        public void Initialize()
        {
            EnsureCanvasGroup();
            ApplyVisibility();
        }

        private void Awake()
        {
            EnsureCanvasGroup();
        }

        private void OnEnable()
        {
            ApplyVisibility();
        }

        private void LateUpdate()
        {
            ApplyVisibility();
        }

        private void Reset()
        {
            EnsureCanvasGroup();
        }

        private void EnsureCanvasGroup()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void ApplyVisibility()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            var isVisible = ResolveVisibility();
            _canvasGroup.alpha = isVisible ? 1f : 0f;
            _canvasGroup.interactable = isVisible;
            _canvasGroup.blocksRaycasts = isVisible;
        }

        private bool ResolveVisibility()
        {
            if (_mobileModeService != null)
            {
                return _mobileModeService.IsMobileModeActive;
            }

            // Keep scene-authored mobile UI visible in the Editor even before DI has injected the presenter.
            return Application.isEditor || Application.isMobilePlatform;
        }
    }
}
