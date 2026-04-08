// Path: Assets/Project/Scpripts/DayNight/AmbientLightController.cs
// Purpose: Drives RenderSettings ambient light and swaps post-processing profiles according to time of day.
// Dependencies: UniTask, UnityEngine, UnityEngine.Rendering, DayNightConfig, IDayNightService, VContainer.

using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using VContainer;

namespace ProjectResonance.DayNight
{
    /// <summary>
    /// Runtime controller for ambient light and post-processing profiles.
    /// </summary>
    [AddComponentMenu("Project Resonance/DayNight/Ambient Light Controller")]
    [DisallowMultipleComponent]
    public sealed class AmbientLightController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField]
        private Component _globalVolumeComponent;

        [Header("Ambient")]
        [SerializeField]
        private Color _ambientBaseColor = new Color(0.62f, 0.7f, 0.82f);

        [SerializeField]
        [Min(0f)]
        private float _nightAmbientFloor = 0.02f;

        [Header("Volume Profiles")]
        [SerializeField]
        private Object _dayProfile;

        [SerializeField]
        private Object _sunsetProfile;

        [SerializeField]
        private Object _nightProfile;

        private IDayNightService _dayNightService;
        private DayNightConfig _config;
        private Object _activeProfile;

        [Inject]
        private void Construct(IDayNightService dayNightService, DayNightConfig config)
        {
            _dayNightService = dayNightService;
            _config = config;
        }

        private void Start()
        {
            RunAmbientLoopAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid RunAmbientLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ApplyAmbient();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        private void ApplyAmbient()
        {
            if (_dayNightService == null || _config == null)
            {
                return;
            }

            var normalizedTime = _dayNightService.CurrentTimeNormalized;
            var ambientIntensity = Mathf.Max(_nightAmbientFloor, _config.AmbientIntensityCurve.Evaluate(normalizedTime));

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = _ambientBaseColor * ambientIntensity;

            var targetProfile = ResolveProfile(_dayNightService.CurrentTimeOfDay);
            if (_globalVolumeComponent != null && targetProfile != null && _activeProfile != targetProfile)
            {
                if (TryAssignSharedProfile(_globalVolumeComponent, targetProfile))
                {
                    _activeProfile = targetProfile;
                }
            }
        }

        private Object ResolveProfile(TimeOfDay timeOfDay)
        {
            switch (timeOfDay)
            {
                case TimeOfDay.Sunset:
                case TimeOfDay.Dusk:
                    return _sunsetProfile != null ? _sunsetProfile : _dayProfile;
                case TimeOfDay.Night:
                case TimeOfDay.PreDawn:
                    return _nightProfile != null ? _nightProfile : _sunsetProfile;
                default:
                    return _dayProfile;
            }
        }

        private static bool TryAssignSharedProfile(Component volumeComponent, Object profile)
        {
            var componentType = volumeComponent.GetType();
            var sharedProfileProperty = componentType.GetProperty("sharedProfile");
            if (sharedProfileProperty != null && sharedProfileProperty.CanWrite)
            {
                sharedProfileProperty.SetValue(volumeComponent, profile);
                return true;
            }

            var sharedProfileField = componentType.GetField("sharedProfile");
            if (sharedProfileField != null)
            {
                sharedProfileField.SetValue(volumeComponent, profile);
                return true;
            }

            return false;
        }
    }
}
