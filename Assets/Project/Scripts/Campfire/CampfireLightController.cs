// Path: Assets/Project/Scpripts/Campfire/CampfireLightController.cs
// Purpose: Drives point-light flicker, dying-state blinking and extinguish fade for the campfire while exposing it as a ghost light source.
// Dependencies: UniTask, Common.Random, CampfireState, ILightSource, UnityEngine, VContainer.

using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectResonance.Common.Random;
using ProjectResonance.Ghosts;
using UnityEngine;
using VContainer;

namespace ProjectResonance.Campfire
{
    /// <summary>
    /// Controls the visual light behavior of the campfire.
    /// </summary>
    [AddComponentMenu("Project Resonance/Campfire/Campfire Light Controller")]
    [DisallowMultipleComponent]
    public sealed class CampfireLightController : MonoBehaviour, ILightSource
    {
        [Header("References")]
        [SerializeField]
        private Light _pointLight;

        [Header("Normal Flicker")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _normalIntensityJitter = 0.12f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _normalRangeJitter = 0.08f;

        [SerializeField]
        [Min(0.01f)]
        private float _normalFlickerIntervalSeconds = 0.08f;

        [Header("Dying Flicker")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _dyingIntensityJitter = 0.22f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _dyingRangeJitter = 0.14f;

        [SerializeField]
        [Min(0.01f)]
        private float _dyingFlickerIntervalSeconds = 0.04f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _dyingBaseIntensityMultiplier = 0.65f;

        [Header("Fade")]
        [SerializeField]
        [Min(0.1f)]
        private float _extinguishFadeDurationSeconds = 3f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _minimumFuelIntensityMultiplier = 0.35f;

        private CampfireState _campfireState;
        private CampfireAnchor _campfireAnchor;
        private IRandomProvider _randomProvider;
        private float _baseIntensity;
        private float _baseRange;

        /// <summary>
        /// Gets the world-space position of the campfire light source.
        /// </summary>
        public Vector3 Position => _pointLight != null ? _pointLight.transform.position : transform.position;

        /// <summary>
        /// Gets the normalized light intensity used by ghost AI.
        /// </summary>
        public float NormalizedIntensity
        {
            get
            {
                if (_campfireState == null || !_campfireState.IsLit)
                {
                    return 0f;
                }

                var levelIntensity = _campfireState.Level == CampfireLevel.Basic
                    ? 0.45f
                    : _campfireState.Level == CampfireLevel.Reinforced
                        ? 0.8f
                        : 1f;

                return Mathf.Clamp01(levelIntensity * Mathf.Lerp(_minimumFuelIntensityMultiplier, 1f, _campfireState.CurrentFuelNormalized));
            }
        }

        /// <summary>
        /// Gets whether the campfire is currently emitting light.
        /// </summary>
        public bool IsEmittingLight => _campfireState != null && _campfireState.IsLit;

        [Inject]
        private void Construct(CampfireState campfireState, IRandomProvider randomProvider, CampfireAnchor campfireAnchor = null)
        {
            _campfireState = campfireState;
            _randomProvider = randomProvider;
            _campfireAnchor = campfireAnchor;
        }

        private void Awake()
        {
            if (_pointLight == null)
            {
                _pointLight = GetComponentInChildren<Light>();
            }

            if (_pointLight != null)
            {
                _baseIntensity = _pointLight.intensity;
                _baseRange = _pointLight.range;
            }
        }

        private void Start()
        {
            RunLightLoopAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid RunLightLoopAsync(CancellationToken cancellationToken)
        {
            if (_pointLight == null)
            {
                return;
            }

            var wasLit = _campfireState != null && _campfireState.IsLit;
            UpdateEmbersState(wasLit);

            if (!wasLit)
            {
                _pointLight.enabled = false;
                _pointLight.intensity = 0f;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_campfireState == null)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    continue;
                }

                if (_campfireState.IsLit)
                {
                    wasLit = true;
                    UpdateEmbersState(true);
                    ApplyFlickerSample();

                    await UniTask.Delay(
                        (int)((_campfireState.IsDying ? _dyingFlickerIntervalSeconds : _normalFlickerIntervalSeconds) * 1000f),
                        DelayType.DeltaTime,
                        PlayerLoopTiming.Update,
                        cancellationToken);

                    continue;
                }

                if (wasLit)
                {
                    UpdateEmbersState(false);
                    await FadeOutAsync(cancellationToken);
                    wasLit = false;
                }
                else
                {
                    UpdateEmbersState(false);
                    _pointLight.enabled = false;
                    _pointLight.intensity = 0f;
                    await UniTask.Delay(100, DelayType.DeltaTime, PlayerLoopTiming.Update, cancellationToken);
                }
            }
        }

        private void UpdateEmbersState(bool isLit)
        {
            _campfireAnchor?.SetEmbersActive(isLit);
        }

        private void ApplyFlickerSample()
        {
            if (_pointLight == null || _randomProvider == null)
            {
                return;
            }

            _pointLight.enabled = true;

            var normalizedFuel = _campfireState.CurrentFuelNormalized;
            var baseIntensityMultiplier = Mathf.Lerp(_minimumFuelIntensityMultiplier, 1f, normalizedFuel);
            var intensityJitter = _campfireState.IsDying ? _dyingIntensityJitter : _normalIntensityJitter;
            var rangeJitter = _campfireState.IsDying ? _dyingRangeJitter : _normalRangeJitter;

            if (_campfireState.IsDying)
            {
                baseIntensityMultiplier *= _dyingBaseIntensityMultiplier;
            }

            var intensityOffset = _randomProvider.Range(-intensityJitter, intensityJitter);
            var rangeOffset = _randomProvider.Range(-rangeJitter, rangeJitter);

            _pointLight.intensity = Mathf.Max(0f, (_baseIntensity * baseIntensityMultiplier) + (_baseIntensity * intensityOffset));
            _pointLight.range = Mathf.Max(0f, _campfireState.LightRadius + (_baseRange * rangeOffset));
        }

        private async UniTask FadeOutAsync(CancellationToken cancellationToken)
        {
            if (_pointLight == null)
            {
                return;
            }

            var startIntensity = _pointLight.intensity;
            var startRange = _pointLight.range;
            var elapsed = 0f;

            while (elapsed < _extinguishFadeDurationSeconds && !_campfireState.IsLit && !cancellationToken.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                var t = _extinguishFadeDurationSeconds > 0f ? Mathf.Clamp01(elapsed / _extinguishFadeDurationSeconds) : 1f;

                _pointLight.intensity = Mathf.Lerp(startIntensity, 0f, t);
                _pointLight.range = Mathf.Lerp(startRange, 0f, t);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (!_campfireState.IsLit)
            {
                _pointLight.enabled = false;
                _pointLight.intensity = 0f;
                _pointLight.range = 0f;
            }
        }
    }
}
