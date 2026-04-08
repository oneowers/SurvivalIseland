// Path: Assets/Project/Scripts/Inventory/Editor/ItemDefinitionEditor.cs
// Purpose: Provides a type-driven custom inspector for the unified ItemDefinition asset authoring workflow.
// Dependencies: UnityEditor, UnityEngine, ProjectResonance.Inventory.

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProjectResonance.Inventory.Editor
{
    /// <summary>
    /// Custom inspector that shows only the relevant sections for the selected item category.
    /// </summary>
    [CustomEditor(typeof(ItemDefinition))]
    public sealed class ItemDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty _itemIdProperty;
        private SerializedProperty _displayNameProperty;
        private SerializedProperty _iconProperty;
        private SerializedProperty _weightProperty;
        private SerializedProperty _isStackableProperty;
        private SerializedProperty _maxStackSizeProperty;
        private SerializedProperty _itemTypeProperty;
        private SerializedProperty _worldPrefabProperty;

        private SerializedProperty _axeTierProperty;
        private SerializedProperty _usesDurabilityProperty;
        private SerializedProperty _maxDurabilityProperty;
        private SerializedProperty _breaksOnZeroDurabilityProperty;

        private SerializedProperty _resourceIdProperty;
        private SerializedProperty _nodeDisplayNameProperty;
        private SerializedProperty _resourceNodeTypeProperty;
        private SerializedProperty _maxHealthProperty;
        private SerializedProperty _dropCountProperty;

        private SerializedProperty _canPlantFromInventoryProperty;
        private SerializedProperty _plantSpawnPrefabProperty;
        private SerializedProperty _snapPlantingToIntegerGridProperty;
        private SerializedProperty _plantingGroundMaskProperty;
        private SerializedProperty _plantingProbeHeightProperty;
        private SerializedProperty _plantingProbeDistanceProperty;
        private SerializedProperty _plantingClearRadiusProperty;

        private void OnEnable()
        {
            _itemIdProperty = serializedObject.FindProperty("_itemId");
            _displayNameProperty = serializedObject.FindProperty("_displayName");
            _iconProperty = serializedObject.FindProperty("_icon");
            _weightProperty = serializedObject.FindProperty("_weight");
            _isStackableProperty = serializedObject.FindProperty("_isStackable");
            _maxStackSizeProperty = serializedObject.FindProperty("_maxStackSize");
            _itemTypeProperty = serializedObject.FindProperty("_itemType");
            _worldPrefabProperty = serializedObject.FindProperty("_worldPrefab");

            _axeTierProperty = serializedObject.FindProperty("_axeTier");
            _usesDurabilityProperty = serializedObject.FindProperty("_usesDurability");
            _maxDurabilityProperty = serializedObject.FindProperty("_maxDurability");
            _breaksOnZeroDurabilityProperty = serializedObject.FindProperty("_breaksOnZeroDurability");

            _resourceIdProperty = serializedObject.FindProperty("_resourceId");
            _nodeDisplayNameProperty = serializedObject.FindProperty("_nodeDisplayName");
            _resourceNodeTypeProperty = serializedObject.FindProperty("_resourceNodeType");
            _maxHealthProperty = serializedObject.FindProperty("_maxHealth");
            _dropCountProperty = serializedObject.FindProperty("_dropCount");

            _canPlantFromInventoryProperty = serializedObject.FindProperty("_canPlantFromInventory");
            _plantSpawnPrefabProperty = serializedObject.FindProperty("_plantSpawnPrefab");
            _snapPlantingToIntegerGridProperty = serializedObject.FindProperty("_snapPlantingToIntegerGrid");
            _plantingGroundMaskProperty = serializedObject.FindProperty("_plantingGroundMask");
            _plantingProbeHeightProperty = serializedObject.FindProperty("_plantingProbeHeight");
            _plantingProbeDistanceProperty = serializedObject.FindProperty("_plantingProbeDistance");
            _plantingClearRadiusProperty = serializedObject.FindProperty("_plantingClearRadius");
        }

        /// <summary>
        /// Draws the unified item inspector with type-specific sections.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawCommonSection();

            var itemType = (ItemType)_itemTypeProperty.enumValueIndex;
            switch (itemType)
            {
                case ItemType.Tool:
                    DrawToolSection();
                    break;

                case ItemType.Resource:
                    DrawResourceSection();
                    break;

                case ItemType.Utility:
                case ItemType.BuildingMaterial:
                    DrawUtilitySection(itemType);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCommonSection()
        {
            EditorGUILayout.LabelField("Common", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_itemIdProperty);
            EditorGUILayout.PropertyField(_displayNameProperty);
            EditorGUILayout.PropertyField(_iconProperty);
            EditorGUILayout.PropertyField(_weightProperty);
            EditorGUILayout.PropertyField(_itemTypeProperty);
            EditorGUILayout.PropertyField(_worldPrefabProperty);
            EditorGUILayout.PropertyField(_isStackableProperty);

            using (new EditorGUI.DisabledScope(!_isStackableProperty.boolValue))
            {
                EditorGUILayout.PropertyField(_maxStackSizeProperty);
            }

            EditorGUILayout.Space();
        }

        private void DrawToolSection()
        {
            EditorGUILayout.LabelField("Tool", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_axeTierProperty);
            EditorGUILayout.PropertyField(_usesDurabilityProperty);

            using (new EditorGUI.DisabledScope(!_usesDurabilityProperty.boolValue))
            {
                EditorGUILayout.PropertyField(_maxDurabilityProperty);
                EditorGUILayout.PropertyField(_breaksOnZeroDurabilityProperty);
            }
        }

        private void DrawResourceSection()
        {
            EditorGUILayout.LabelField("Resource Node", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_resourceIdProperty);
            EditorGUILayout.PropertyField(_nodeDisplayNameProperty);
            EditorGUILayout.PropertyField(_resourceNodeTypeProperty);
            EditorGUILayout.PropertyField(_maxHealthProperty);
            EditorGUILayout.PropertyField(_dropCountProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Planting", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_canPlantFromInventoryProperty);

            using (new EditorGUI.DisabledScope(!_canPlantFromInventoryProperty.boolValue))
            {
                EditorGUILayout.PropertyField(_plantSpawnPrefabProperty);
                EditorGUILayout.PropertyField(_snapPlantingToIntegerGridProperty);
                EditorGUILayout.PropertyField(_plantingGroundMaskProperty);
                EditorGUILayout.PropertyField(_plantingProbeHeightProperty);
                EditorGUILayout.PropertyField(_plantingProbeDistanceProperty);
                EditorGUILayout.PropertyField(_plantingClearRadiusProperty);
            }
        }

        private static void DrawUtilitySection(ItemType itemType)
        {
            EditorGUILayout.HelpBox(
                itemType == ItemType.Utility
                    ? "Utility items use only the common section unless a gameplay system needs additional data later."
                    : "Building materials currently use only the common section.",
                MessageType.Info);
        }
    }
}
#endif
