// Path: Assets/Project/Scripts/Player/AimTargetable.cs
// Purpose: Marks a world object as selectable by the player's right-stick aim targeting system.
// Dependencies: System, UnityEngine, ProjectResonance.PlayerCombat, ProjectResonance.PlayerInstaller.

using System;
using UnityEngine;

namespace ProjectResonance.PlayerCombat
{
    /// <summary>
    /// Authored target marker used by the generic aim-targeting system.
    /// </summary>
    [AddComponentMenu("Project Resonance/Player Combat/Aim Targetable")]
    [DisallowMultipleComponent]
    public sealed class AimTargetable : MonoBehaviour
    {
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorPropertyId = Shader.PropertyToID("_EmissionColor");

        [SerializeField]
        private Transform _targetAnchor;

        [SerializeField]
        private MonoBehaviour _hitReceiverSource;

        [SerializeField]
        private Renderer[] _feedbackRenderers;

        [SerializeField]
        private bool _drawDebugGizmos = true;

        private FeedbackRendererState[] _feedbackStates = Array.Empty<FeedbackRendererState>();
        private bool _feedbackInitialized;
        private Color _activeFlashColor = Color.white;
        private float _activeFlashDuration = 1f;
        private float _activeFlashStrength = 0.85f;
        private float _flashTimeRemaining;
        private ProjectResonance.PlayerInstaller.PlayerInstaller _playerInstaller;

        private struct FeedbackMaterialState
        {
            public Material Material;
            public int BaseColorPropertyId;
            public Color BaseColor;
            public int EmissionColorPropertyId;
            public Color BaseEmissionColor;
            public bool IsValid;
        }

        private struct FeedbackRendererState
        {
            public Renderer Renderer;
            public FeedbackMaterialState[] Materials;
            public bool IsValid;
        }

        /// <summary>
        /// Gets the transform used as the target anchor.
        /// </summary>
        public Transform TargetAnchor => _targetAnchor != null ? _targetAnchor : transform;

        /// <summary>
        /// Resolves the world-space anchor position used while selecting this target.
        /// </summary>
        /// <param name="heightBias">Vertical offset applied to the authored anchor.</param>
        /// <returns>Resolved world-space anchor position.</returns>
        public Vector3 ResolveAnchorPosition(float heightBias)
        {
            var anchor = TargetAnchor;
            var anchorPosition = anchor.position;

            // The shared height bias only lifts targets that rely on their root transform as the anchor.
            if (_targetAnchor == null || anchor == transform)
            {
                anchorPosition += new Vector3(0f, Mathf.Max(0f, heightBias), 0f);
            }

            return anchorPosition;
        }

        /// <summary>
        /// Resolves the hit receiver used when the player attacks this target.
        /// </summary>
        /// <param name="receiver">Resolved player-hit receiver.</param>
        /// <returns>True when a valid hit receiver is available.</returns>
        public bool TryGetHitReceiver(out IPlayerHitReceiver receiver)
        {
            receiver = _hitReceiverSource as IPlayerHitReceiver;
            return receiver != null;
        }

        /// <summary>
        /// Plays a selection flash and fades the target back to its original color.
        /// </summary>
        /// <param name="flashColor">Color shown at the start of the flash.</param>
        /// <param name="duration">Duration of the fade back.</param>
        /// <param name="strength">Blend strength of the flash.</param>
        public void PlaySelectionFeedback(Color flashColor, float duration, float strength)
        {
            EnsureFeedbackInitialized();

            if (_feedbackStates.Length == 0)
            {
                return;
            }

            _activeFlashColor = flashColor;
            _activeFlashDuration = Mathf.Max(0.05f, duration);
            _activeFlashStrength = Mathf.Clamp01(strength);
            _flashTimeRemaining = _activeFlashDuration;
            ApplyFlash(1f);
        }

        private void Awake()
        {
            EnsureFeedbackInitialized();
        }

        private void OnEnable()
        {
            EnsureFeedbackInitialized();
            ApplyFlash(0f);
        }

        private void Update()
        {
            if (_flashTimeRemaining <= 0f)
            {
                return;
            }

            _flashTimeRemaining = Mathf.Max(0f, _flashTimeRemaining - Time.deltaTime);
            var normalizedTime = _activeFlashDuration > Mathf.Epsilon
                ? _flashTimeRemaining / _activeFlashDuration
                : 0f;

            ApplyFlash(normalizedTime);
        }

        private void OnDisable()
        {
            _flashTimeRemaining = 0f;
            ApplyFlash(0f);
        }

        private void OnDrawGizmos()
        {
            if (!_drawDebugGizmos || !Application.isPlaying)
            {
                return;
            }

            if (!TryResolveDebugPlayerOrigin(out var playerOrigin))
            {
                return;
            }

            if (!TryGetHitReceiver(out var receiver) || !receiver.CanReceiveHit)
            {
                return;
            }

            var anchorPosition = ResolveAnchorPosition(0f);
            var planarOffset = anchorPosition - playerOrigin;
            planarOffset.y = 0f;
            var debugHitRadius = ResolveDebugHitRadius();
            if (planarOffset.sqrMagnitude > debugHitRadius * debugHitRadius)
            {
                return;
            }

            Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.9f);
            Gizmos.DrawLine(playerOrigin, anchorPosition);
            Gizmos.DrawSphere(anchorPosition, 0.08f);
        }

        private void Reset()
        {
            _targetAnchor = transform;
            AutoAssignReceiver();
            AutoAssignFeedbackRenderers();
        }

        private void OnValidate()
        {
            if (_targetAnchor == null)
            {
                _targetAnchor = transform;
            }

            AutoAssignReceiver();
            AutoAssignFeedbackRenderers();
            _feedbackInitialized = false;
        }

        private void AutoAssignReceiver()
        {
            if (_hitReceiverSource is IPlayerHitReceiver)
            {
                return;
            }

            var behaviours = GetComponents<MonoBehaviour>();
            for (var index = 0; index < behaviours.Length; index++)
            {
                var behaviour = behaviours[index];
                if (behaviour is IPlayerHitReceiver)
                {
                    _hitReceiverSource = behaviour;
                    return;
                }
            }
        }

        private void AutoAssignFeedbackRenderers()
        {
            if (_feedbackRenderers != null && _feedbackRenderers.Length > 0)
            {
                return;
            }

            _feedbackRenderers = GetComponentsInChildren<Renderer>(true);
        }

        private void EnsureFeedbackInitialized()
        {
            if (_feedbackInitialized)
            {
                return;
            }

            if (_feedbackRenderers == null || _feedbackRenderers.Length == 0)
            {
                AutoAssignFeedbackRenderers();
            }

            if (_feedbackRenderers == null || _feedbackRenderers.Length == 0)
            {
                _feedbackStates = Array.Empty<FeedbackRendererState>();
                _feedbackInitialized = true;
                return;
            }

            _feedbackStates = new FeedbackRendererState[_feedbackRenderers.Length];
            for (var index = 0; index < _feedbackRenderers.Length; index++)
            {
                var renderer = _feedbackRenderers[index];
                if (renderer == null)
                {
                    continue;
                }

                var materials = renderer.materials;
                if (materials == null || materials.Length == 0)
                {
                    continue;
                }

                var materialStates = new FeedbackMaterialState[materials.Length];
                var hasValidMaterial = false;
                for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    var material = materials[materialIndex];
                    if (material == null)
                    {
                        continue;
                    }

                    var baseColorPropertyId = 0;
                    var baseColor = Color.white;
                    if (material.HasProperty(BaseColorPropertyId))
                    {
                        baseColorPropertyId = BaseColorPropertyId;
                        baseColor = material.GetColor(BaseColorPropertyId);
                    }
                    else if (material.HasProperty(ColorPropertyId))
                    {
                        baseColorPropertyId = ColorPropertyId;
                        baseColor = material.GetColor(ColorPropertyId);
                    }

                    var emissionColorPropertyId = 0;
                    var baseEmissionColor = Color.black;
                    if (material.HasProperty(EmissionColorPropertyId))
                    {
                        emissionColorPropertyId = EmissionColorPropertyId;
                        material.EnableKeyword("_EMISSION");
                        baseEmissionColor = material.GetColor(EmissionColorPropertyId);
                    }

                    if (baseColorPropertyId == 0 && emissionColorPropertyId == 0)
                    {
                        continue;
                    }

                    materialStates[materialIndex] = new FeedbackMaterialState
                    {
                        Material = material,
                        BaseColorPropertyId = baseColorPropertyId,
                        BaseColor = baseColor,
                        EmissionColorPropertyId = emissionColorPropertyId,
                        BaseEmissionColor = baseEmissionColor,
                        IsValid = true,
                    };
                    hasValidMaterial = true;
                }

                if (!hasValidMaterial)
                {
                    continue;
                }

                _feedbackStates[index] = new FeedbackRendererState
                {
                    Renderer = renderer,
                    Materials = materialStates,
                    IsValid = true,
                };
            }

            _feedbackInitialized = true;
        }

        private void ApplyFlash(float normalizedTime)
        {
            if (!_feedbackInitialized)
            {
                return;
            }

            var flashWeight = Mathf.Clamp01(normalizedTime) * _activeFlashStrength;
            var emissionWeight = Mathf.Clamp01(normalizedTime) * Mathf.Max(1f, _activeFlashStrength * 2f);
            for (var index = 0; index < _feedbackStates.Length; index++)
            {
                var rendererState = _feedbackStates[index];
                if (!rendererState.IsValid || rendererState.Renderer == null || rendererState.Materials == null)
                {
                    continue;
                }

                for (var materialIndex = 0; materialIndex < rendererState.Materials.Length; materialIndex++)
                {
                    var materialState = rendererState.Materials[materialIndex];
                    if (!materialState.IsValid || materialState.Material == null)
                    {
                        continue;
                    }

                    if (materialState.BaseColorPropertyId != 0)
                    {
                        materialState.Material.SetColor(
                            materialState.BaseColorPropertyId,
                            Color.Lerp(materialState.BaseColor, _activeFlashColor, flashWeight));
                    }

                    if (materialState.EmissionColorPropertyId != 0)
                    {
                        materialState.Material.SetColor(
                            materialState.EmissionColorPropertyId,
                            Color.Lerp(materialState.BaseEmissionColor, _activeFlashColor * emissionWeight, flashWeight));
                    }
                }
            }
        }

        private bool TryResolveDebugPlayerOrigin(out Vector3 playerOrigin)
        {
            var characterController = FindFirstObjectByType<CharacterController>();
            if (characterController != null)
            {
                playerOrigin = characterController.transform.position;
                return true;
            }

            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerOrigin = playerObject.transform.position;
                return true;
            }

            playerOrigin = Vector3.zero;
            return false;
        }

        private float ResolveDebugHitRadius()
        {
            if (_playerInstaller == null)
            {
                _playerInstaller = FindFirstObjectByType<ProjectResonance.PlayerInstaller.PlayerInstaller>();
            }

            if (_playerInstaller != null && _playerInstaller.AimTargetingConfigAsset != null)
            {
                return _playerInstaller.AimTargetingConfigAsset.MaxAimRadius;
            }

            return 3f;
        }
    }
}
