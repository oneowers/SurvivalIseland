// Path: Assets/Project/Scpripts/UI/Health/HealthHudPresenter.cs
// Purpose: Renders the player's runtime HP state in the HUD.
// Dependencies: UniTask, TMPro, UnityEngine.UI, HealthHudConfig, HealthChangedMessage, VContainer.

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectResonance.Common.Messages;
using ProjectResonance.Health;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ProjectResonance.HealthUI
{
    /// <summary>
    /// Runtime presenter for the player's health HUD.
    /// </summary>
    [AddComponentMenu("Project Resonance/UI/Health HUD Presenter")]
    [DisallowMultipleComponent]
    public sealed class HealthHudPresenter : MonoBehaviour, IDisposable
    {
        [Header("Bars")]
        [SerializeField]
        private Image _fillImage;

        [SerializeField]
        private Image _damageLagImage;

        [SerializeField]
        private Image _flashImage;

        [Header("Text")]
        [SerializeField]
        private TMP_Text _healthValueText;

        [SerializeField]
        private TMP_Text _statusText;

        [Header("Labels")]
        [SerializeField]
        private string _normalStatusLabel = "STABLE";

        [SerializeField]
        private string _criticalStatusLabel = "CRITICAL";

        [SerializeField]
        private string _depletedStatusLabel = "DEPLETED";

        private IHealthService _healthService;
        private HealthHudConfig _config;
        private float _targetFillNormalized = 1f;
        private float _visualFillNormalized = 1f;
        private float _damageLagNormalized = 1f;
        private float _damageLagDelayTimer;
        private float _currentHealth = 100f;
        private float _maxHealth = 100f;
        private Color _flashColor;
        private bool _isDepleted;

        [Inject]
        private void Construct(IHealthService healthService, HealthHudConfig config)
        {
            _healthService = healthService;
            _config = config;
        }

        private void Awake()
        {
            if (_fillImage != null)
            {
                _fillImage.type = Image.Type.Filled;
                _fillImage.fillMethod = Image.FillMethod.Horizontal;
            }

            if (_damageLagImage != null)
            {
                _damageLagImage.type = Image.Type.Filled;
                _damageLagImage.fillMethod = Image.FillMethod.Horizontal;
            }
        }

        private void Start()
        {
            if (_healthService != null)
            {
                _currentHealth = _healthService.CurrentHealth;
                _maxHealth = _healthService.MaxHealth;
                _targetFillNormalized = _maxHealth > 0f ? Mathf.Clamp01(_currentHealth / _maxHealth) : 0f;
                _visualFillNormalized = _targetFillNormalized;
                _damageLagNormalized = _targetFillNormalized;
                _isDepleted = !_healthService.IsAlive;
                _healthService.HealthChanged += OnHealthChanged;
                _healthService.HealthDepleted += OnHealthDepleted;
            }

            ApplyImmediateState();
            RunVisualLoopAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        /// <summary>
        /// Disposes runtime subscriptions owned by this presenter.
        /// </summary>
        public void Dispose()
        {
            if (_healthService != null)
            {
                _healthService.HealthChanged -= OnHealthChanged;
                _healthService.HealthDepleted -= OnHealthDepleted;
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private async UniTaskVoid RunVisualLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TickVisuals(Time.unscaledDeltaTime);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // The presenter stops animating when the HUD object is destroyed.
            }
        }

        private void OnHealthChanged(HealthChangedMessage message)
        {
            var maxHealth = Mathf.Max(1f, message.MaxHealth);
            var normalized = Mathf.Clamp01(message.CurrentHealth / maxHealth);

            _targetFillNormalized = normalized;
            _damageLagDelayTimer = 0f;
            _isDepleted = message.CurrentHealth <= 0f;
            _currentHealth = message.CurrentHealth;
            _maxHealth = message.MaxHealth;

            if (message.Delta < 0f)
            {
                // Damage refreshes the flash immediately so incoming hits feel readable without a separate animation system.
                _flashColor = _config != null ? _config.FlashColor : new Color(1f, 0.42f, 0.42f, 0.65f);
            }

            if (message.Delta > 0f && _damageLagNormalized < normalized)
            {
                _damageLagNormalized = normalized;
            }

            UpdateText(message.CurrentHealth, message.MaxHealth, normalized);
        }

        private void OnHealthDepleted(HealthDepletedMessage _)
        {
            _isDepleted = true;

            if (_statusText != null)
            {
                _statusText.text = _depletedStatusLabel;
            }
        }

        private void TickVisuals(float deltaTime)
        {
            if (_config == null || deltaTime <= 0f)
            {
                return;
            }

            _visualFillNormalized = Mathf.MoveTowards(
                _visualFillNormalized,
                _targetFillNormalized,
                _config.FillLerpSpeed * deltaTime);

            _damageLagDelayTimer += deltaTime;
            if (_damageLagDelayTimer >= _config.DamageLagDelay)
            {
                _damageLagNormalized = Mathf.MoveTowards(
                    _damageLagNormalized,
                    _targetFillNormalized,
                    _config.DamageLagSpeed * deltaTime);
            }

            _flashColor = Color.Lerp(_flashColor, _config.IdleFlashColor, _config.FlashFadeSpeed * deltaTime);

            ApplyBarState();
        }

        private void ApplyImmediateState()
        {
            if (_config == null)
            {
                return;
            }

            _flashColor = _config.IdleFlashColor;
            ApplyBarState();
            UpdateText(_currentHealth, _maxHealth, _targetFillNormalized);
        }

        private void ApplyBarState()
        {
            if (_config == null)
            {
                return;
            }

            var fillColor = _targetFillNormalized <= _config.CriticalThresholdNormalized
                ? _config.CriticalFillColor
                : _config.NormalFillColor;

            if (_fillImage != null)
            {
                _fillImage.fillAmount = _visualFillNormalized;
                _fillImage.color = fillColor;
            }

            if (_damageLagImage != null)
            {
                _damageLagImage.fillAmount = _damageLagNormalized;
                _damageLagImage.color = _config.DamageFillColor;
            }

            if (_flashImage != null)
            {
                _flashImage.color = _flashColor;
            }
        }

        private void UpdateText(float currentHealth, float maxHealth, float normalized)
        {
            if (_healthValueText != null)
            {
                var format = _config != null && !string.IsNullOrWhiteSpace(_config.HealthFormat)
                    ? _config.HealthFormat
                    : "{0:0}/{1:0}";
                _healthValueText.text = string.Format(format, currentHealth, maxHealth);
            }

            if (_statusText == null || _isDepleted)
            {
                return;
            }

            _statusText.text = normalized <= (_config != null ? _config.CriticalThresholdNormalized : 0.25f)
                ? _criticalStatusLabel
                : _normalStatusLabel;
        }
    }
}
