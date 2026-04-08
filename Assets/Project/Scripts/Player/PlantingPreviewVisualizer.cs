// Path: Assets/Project/Scripts/Player/PlantingPreviewVisualizer.cs
// Purpose: Builds and updates a lightweight world-space ghost preview for plantable prefabs while the player aims a planting location.
// Dependencies: UnityEngine, ProjectResonance.Inventory, ProjectResonance.PlayerCombat.

using System;
using System.Collections.Generic;
using ProjectResonance.Inventory;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectResonance.PlayerCombat
{
    /// <summary>
    /// Maintains a lightweight mesh-only preview used by planting aim.
    /// </summary>
    public sealed class PlantingPreviewVisualizer : IDisposable
    {
        private readonly AimTargetingConfig _aimTargetingConfig;

        private GameObject _previewRoot;
        private readonly List<Material> _previewMaterials = new List<Material>(16);
        private ItemDefinition _currentDefinition;
        private GameObject _currentPrefab;
        private bool _isVisible;

        /// <summary>
        /// Creates the planting preview visualizer.
        /// </summary>
        /// <param name="aimTargetingConfig">Shared aim config that stores preview colors and offsets.</param>
        public PlantingPreviewVisualizer(AimTargetingConfig aimTargetingConfig)
        {
            _aimTargetingConfig = aimTargetingConfig;
        }

        /// <summary>
        /// Shows or updates the planting preview for the supplied candidate.
        /// </summary>
        /// <param name="definition">Currently selected plantable item definition.</param>
        /// <param name="candidate">Latest evaluated planting candidate.</param>
        public void Show(ItemDefinition definition, PlantingCandidate candidate)
        {
            if (definition == null || !definition.TryGetPlantSpawnPrefab(out var plantSpawnPrefab) || plantSpawnPrefab == null)
            {
                Hide();
                return;
            }

            if (_previewRoot == null || _currentDefinition != definition || _currentPrefab != plantSpawnPrefab)
            {
                RebuildPreview(definition, plantSpawnPrefab);
            }

            if (_previewRoot == null)
            {
                return;
            }

            var previewPosition = candidate.PlacementPosition + (Vector3.up * ResolvePreviewHeightOffset());
            _previewRoot.transform.SetPositionAndRotation(previewPosition, Quaternion.identity);
            ApplyPreviewColor(candidate.IsValid ? ResolveValidColor() : ResolveInvalidColor());

            if (!_isVisible)
            {
                _previewRoot.SetActive(true);
                _isVisible = true;
            }
        }

        /// <summary>
        /// Hides the current planting preview.
        /// </summary>
        public void Hide()
        {
            if (_previewRoot != null && _previewRoot.activeSelf)
            {
                _previewRoot.SetActive(false);
            }

            _isVisible = false;
        }

        /// <summary>
        /// Releases the preview instance and owned material.
        /// </summary>
        public void Dispose()
        {
            if (_previewRoot != null)
            {
                UnityEngine.Object.Destroy(_previewRoot);
                _previewRoot = null;
            }

            DestroyPreviewMaterials();

            _currentDefinition = null;
            _currentPrefab = null;
            _isVisible = false;
        }

        private void RebuildPreview(ItemDefinition definition, GameObject plantSpawnPrefab)
        {
            if (_previewRoot != null)
            {
                UnityEngine.Object.Destroy(_previewRoot);
                _previewRoot = null;
            }

            DestroyPreviewMaterials();

            _previewRoot = new GameObject($"{plantSpawnPrefab.name}_PlantingPreview");
            _previewRoot.hideFlags = HideFlags.DontSave;
            _previewRoot.SetActive(false);

            CloneRenderableHierarchy(plantSpawnPrefab.transform, _previewRoot.transform);

            _currentDefinition = definition;
            _currentPrefab = plantSpawnPrefab;
            _isVisible = false;
        }

        private Transform CloneRenderableHierarchy(Transform source, Transform parent)
        {
            if (source == null || !HasRenderableInSubtree(source))
            {
                return null;
            }

            var createdTransform = CreateTransformNode(source, parent);
            TryCopyLocalRenderer(source, createdTransform, out _);

            for (var childIndex = 0; childIndex < source.childCount; childIndex++)
            {
                CloneRenderableHierarchy(source.GetChild(childIndex), createdTransform);
            }

            return createdTransform;
        }

        private bool TryCopyLocalRenderer(Transform source, Transform parent, out Transform createdTransform)
        {
            createdTransform = parent;

            if (source.TryGetComponent<MeshRenderer>(out var meshRenderer) && source.TryGetComponent<MeshFilter>(out var meshFilter))
            {
                var createdMeshFilter = createdTransform.gameObject.AddComponent<MeshFilter>();
                createdMeshFilter.sharedMesh = meshFilter.sharedMesh;

                var createdMeshRenderer = createdTransform.gameObject.AddComponent<MeshRenderer>();
                createdMeshRenderer.sharedMaterials = CreatePreviewMaterials(meshRenderer.sharedMaterials);
                ConfigureRenderer(createdMeshRenderer);
                return true;
            }

            if (source.TryGetComponent<SkinnedMeshRenderer>(out var skinnedMeshRenderer))
            {
                var createdSkinnedMeshRenderer = createdTransform.gameObject.AddComponent<SkinnedMeshRenderer>();
                createdSkinnedMeshRenderer.sharedMesh = skinnedMeshRenderer.sharedMesh;
                createdSkinnedMeshRenderer.sharedMaterials = CreatePreviewMaterials(skinnedMeshRenderer.sharedMaterials);
                ConfigureRenderer(createdSkinnedMeshRenderer);
                return true;
            }

            return false;
        }

        private static bool HasRenderableInSubtree(Transform source)
        {
            if (source == null)
            {
                return false;
            }

            if (source.GetComponent<MeshRenderer>() != null
                || source.GetComponent<SkinnedMeshRenderer>() != null)
            {
                return true;
            }

            for (var childIndex = 0; childIndex < source.childCount; childIndex++)
            {
                if (HasRenderableInSubtree(source.GetChild(childIndex)))
                {
                    return true;
                }
            }

            return false;
        }

        private Transform CreateTransformNode(Transform source, Transform parent)
        {
            var previewNode = new GameObject(source.name);
            previewNode.layer = parent.gameObject.layer;
            previewNode.transform.SetParent(parent, false);
            previewNode.transform.localPosition = source.localPosition;
            previewNode.transform.localRotation = source.localRotation;
            previewNode.transform.localScale = source.localScale;
            return previewNode.transform;
        }

        private Material[] CreatePreviewMaterials(Material[] sourceMaterials)
        {
            var resolvedMaterialCount = Mathf.Max(1, sourceMaterials != null ? sourceMaterials.Length : 0);
            var materials = new Material[resolvedMaterialCount];
            for (var index = 0; index < resolvedMaterialCount; index++)
            {
                var sourceMaterial = sourceMaterials != null && index < sourceMaterials.Length
                    ? sourceMaterials[index]
                    : null;

                materials[index] = CreatePreviewMaterial(sourceMaterial);
            }

            return materials;
        }

        private void ConfigureRenderer(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        private void ApplyPreviewColor(Color color)
        {
            for (var index = 0; index < _previewMaterials.Count; index++)
            {
                var previewMaterial = _previewMaterials[index];
                if (previewMaterial == null)
                {
                    continue;
                }

                ApplyColor(previewMaterial, color);
            }
        }

        private Material CreatePreviewMaterial(Material sourceMaterial)
        {
            Material material;
            if (sourceMaterial != null)
            {
                material = new Material(sourceMaterial)
                {
                    name = $"{sourceMaterial.name}_PlantingPreview",
                };
            }
            else
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("Universal Render Pipeline/Unlit")
                             ?? Shader.Find("Standard");

                material = new Material(shader)
                {
                    name = "PlantingPreviewMaterial",
                };
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            ApplyColor(material, ResolveValidColor());
            _previewMaterials.Add(material);
            return material;
        }

        private void DestroyPreviewMaterials()
        {
            for (var index = 0; index < _previewMaterials.Count; index++)
            {
                if (_previewMaterials[index] != null)
                {
                    UnityEngine.Object.Destroy(_previewMaterials[index]);
                }
            }

            _previewMaterials.Clear();
        }

        private static void ApplyColor(Material material, Color overlayColor)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                var baseColor = material.GetColor("_BaseColor");
                baseColor.a = overlayColor.a;
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_Color"))
            {
                var color = material.GetColor("_Color");
                color.a = overlayColor.a;
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                var emissionColor = overlayColor * 0.1f;
                emissionColor.a = 0f;
                material.SetColor("_EmissionColor", emissionColor);
            }
        }

        private Color ResolveValidColor()
        {
            return _aimTargetingConfig != null
                ? _aimTargetingConfig.PlantingPreviewValidColor
                : new Color(0.34f, 0.92f, 0.45f, 0.42f);
        }

        private Color ResolveInvalidColor()
        {
            return _aimTargetingConfig != null
                ? _aimTargetingConfig.PlantingPreviewInvalidColor
                : new Color(1f, 0.28f, 0.22f, 0.26f);
        }

        private float ResolvePreviewHeightOffset()
        {
            return _aimTargetingConfig != null ? _aimTargetingConfig.PlantingPreviewHeightOffset : 0.02f;
        }
    }
}
