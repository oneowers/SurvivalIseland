// Path: Assets/Project/Scpripts/DayNight/SunLightController.cs
// Purpose: Drives the main directional sun/moon light from authored day/night curves without using Update.
// Dependencies: UniTask, UnityEngine, DayNightConfig, IDayNightService, VContainer.

using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace ProjectResonance.DayNight
{
    /// <summary>
    /// Applies authored sun and moon lighting to the scene directional light.
    /// </summary>
    [AddComponentMenu("Project Resonance/DayNight/Sun Light Controller")]
    [DisallowMultipleComponent]
    public sealed class SunLightController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField]
        private Light _directionalLight;

        [Header("Curves")]
        [SerializeField]
        private AnimationCurve _shadowAngleCurve = new AnimationCurve(
            new Keyframe(0f, 8f),
            new Keyframe(0.25f, 60f),
            new Keyframe(0.5f, 12f),
            new Keyframe(0.625f, -20f),
            new Keyframe(0.875f, -40f),
            new Keyframe(1f, 8f));

        [SerializeField]
        private AnimationCurve _moonBlendCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 0f),
            new Keyframe(0.58333334f, 0.35f),
            new Keyframe(0.625f, 1f),
            new Keyframe(0.875f, 1f),
            new Keyframe(1f, 0f));

        [Header("Moon")]
        [SerializeField]
        private Color _moonColor = new Color(0.18f, 0.26f, 0.4f);

        [SerializeField]
        [Range(0f, 1f)]
        private float _moonIntensityMultiplier = 0.1f;

        [SerializeField]
        private float _sunYaw = -30f;

        private IDayNightService _dayNightService;
        private DayNightConfig _config;
        private float _maxSunIntensity = 1f;

        [Inject]
        private void Construct(IDayNightService dayNightService, DayNightConfig config)
        {
            _dayNightService = dayNightService;
            _config = config;
        }

        private void Awake()
        {
            if (_directionalLight == null)
            {
                _directionalLight = RenderSettings.sun;
            }

            if (_directionalLight != null)
            {
                _directionalLight.useColorTemperature = true;
            }

            _maxSunIntensity = ResolveMaxSunIntensity();
        }

        private void Start()
        {
            RunLightLoopAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid RunLightLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ApplyLighting();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        private void ApplyLighting()
        {
            if (_directionalLight == null || _config == null || _dayNightService == null)
            {
                return;
            }

            var normalizedTime = _dayNightService.CurrentTimeNormalized;
            var sunIntensity = Mathf.Max(0f, _config.SunIntensityCurve.Evaluate(normalizedTime));
            var moonBlend = Mathf.Clamp01(_moonBlendCurve.Evaluate(normalizedTime));
            var moonIntensity = _maxSunIntensity * _moonIntensityMultiplier * moonBlend;
            var colorTemperature = Mathf.Max(1000f, _config.SunColorTemperatureCurve.Evaluate(normalizedTime));

            // The directional light stays single-source, so we blend authored sun values into moonlight instead of swapping objects.
            _directionalLight.intensity = Mathf.Max(sunIntensity, moonIntensity);
            _directionalLight.colorTemperature = colorTemperature;
            _directionalLight.color = Color.Lerp(
                Mathf.CorrelatedColorTemperatureToRGB(colorTemperature),
                _moonColor,
                moonBlend);
            _directionalLight.shadowStrength = Mathf.Lerp(1f, 0.45f, moonBlend);

            var pitch = _shadowAngleCurve.Evaluate(normalizedTime);
            _directionalLight.transform.rotation = Quaternion.Euler(pitch, _sunYaw, 0f);
        }

        private float ResolveMaxSunIntensity()
        {
            if (_config == null || _config.SunIntensityCurve == null || _config.SunIntensityCurve.length == 0)
            {
                return 1f;
            }

            var maxIntensity = 0f;
            for (var index = 0; index < _config.SunIntensityCurve.length; index++)
            {
                maxIntensity = Mathf.Max(maxIntensity, _config.SunIntensityCurve.keys[index].value);
            }

            return Mathf.Max(1f, maxIntensity);
        }
    }
}
