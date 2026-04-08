// Path: Assets/Project/Scripts/MobileControls/MobileModeService.cs
// Purpose: Resolves whether mobile UI and simplified mobile camera should be active for the current runtime.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.MobileControls
{
    /// <summary>
    /// Exposes the resolved mobile mode state.
    /// </summary>
    public interface IMobileModeService
    {
        /// <summary>
        /// Gets whether mobile controls and the simplified mobile camera should be active.
        /// </summary>
        bool IsMobileModeActive { get; }
    }

    /// <summary>
    /// Default mobile mode resolver backed by <see cref="MobileControlsConfig"/>.
    /// </summary>
    public sealed class MobileModeService : IMobileModeService
    {
        private readonly MobileControlsConfig _config;

        /// <summary>
        /// Creates the mobile mode resolver.
        /// </summary>
        /// <param name="config">Shared mobile controls config.</param>
        public MobileModeService(MobileControlsConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Gets whether mobile controls and camera should currently be enabled.
        /// </summary>
        public bool IsMobileModeActive
        {
            get
            {
                if (_config == null)
                {
                    return Application.isEditor || Application.isMobilePlatform;
                }

                switch (_config.VisibilityMode)
                {
                    case MobileControlsVisibilityMode.MobileOnly:
                        return Application.isMobilePlatform;

                    case MobileControlsVisibilityMode.Manual:
                        return _config.ManualModeActive;

                    case MobileControlsVisibilityMode.EditorAndMobile:
                    default:
                        return Application.isEditor || Application.isMobilePlatform;
                }
            }
        }
    }
}
