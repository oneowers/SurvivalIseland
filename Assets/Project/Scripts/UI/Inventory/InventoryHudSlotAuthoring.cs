// Path: Assets/Project/Scripts/UI/Inventory/InventoryHudSlotAuthoring.cs
// Purpose: Represents one authored inventory HUD slot and encapsulates all UI element references for passive slot rendering.
// Dependencies: TMPro, UnityEngine.UI.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectResonance.InventoryUI
{
    /// <summary>
    /// Authored view component for one inventory HUD slot.
    /// </summary>
    [AddComponentMenu("Project Resonance/UI/Inventory HUD Slot Authoring")]
    [DisallowMultipleComponent]
    public sealed class InventoryHudSlotAuthoring : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        [Min(0)]
        private int _slotIndex;

        [SerializeField]
        private Image _backgroundImage;

        [SerializeField]
        private Image _iconImage;

        [SerializeField]
        private TMP_Text _countText;

        /// <summary>
        /// Raised when the slot is pressed by pointer or touch.
        /// </summary>
        public event Action<int> Clicked;

        /// <summary>
        /// Gets the authored inventory slot index.
        /// </summary>
        public int SlotIndex => Mathf.Max(0, _slotIndex);

        /// <summary>
        /// Assigns a runtime slot index when the scene did not author one explicitly.
        /// </summary>
        /// <param name="slotIndex">Resolved runtime slot index.</param>
        public void AssignRuntimeSlotIndex(int slotIndex)
        {
            _slotIndex = Mathf.Max(0, slotIndex);
        }

        /// <summary>
        /// Applies the slot background color.
        /// </summary>
        /// <param name="color">Target background color.</param>
        public void SetBackgroundColor(Color color)
        {
            EnsureReferences();

            if (_backgroundImage != null)
            {
                _backgroundImage.color = color;
            }
        }

        /// <summary>
        /// Applies the slot icon sprite and tint.
        /// </summary>
        /// <param name="icon">Icon sprite to display.</param>
        /// <param name="tint">Icon tint color.</param>
        public void SetIcon(Sprite icon, Color tint)
        {
            EnsureReferences();

            if (_iconImage == null)
            {
                return;
            }

            _iconImage.sprite = icon;
            _iconImage.color = tint;
            _iconImage.preserveAspect = true;
            _iconImage.enabled = icon != null || tint.a > 0f;
        }

        /// <summary>
        /// Applies the slot count label.
        /// </summary>
        /// <param name="countText">Displayed count text.</param>
        public void SetCount(string countText)
        {
            EnsureReferences();

            if (_countText == null)
            {
                return;
            }

            _countText.text = countText;
            _countText.enabled = !string.IsNullOrWhiteSpace(countText);
        }

        /// <summary>
        /// Publishes a slot click so the HUD can change the active slot.
        /// </summary>
        /// <param name="eventData">Pointer event data.</param>
        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(SlotIndex);
        }

        private void Reset()
        {
            EnsureReferences();
        }

        private void OnValidate()
        {
            _slotIndex = Mathf.Max(0, _slotIndex);
            EnsureReferences();
        }

        private void EnsureReferences()
        {
            if (_backgroundImage == null)
            {
                _backgroundImage = GetComponent<Image>();
            }

            if (_iconImage == null)
            {
                var images = GetComponentsInChildren<Image>(true);
                for (var index = 0; index < images.Length; index++)
                {
                    if (images[index] != null && images[index] != _backgroundImage)
                    {
                        _iconImage = images[index];
                        break;
                    }
                }
            }

            if (_countText == null)
            {
                _countText = GetComponentInChildren<TMP_Text>(true);
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.raycastTarget = true;
            }

            if (_iconImage != null)
            {
                _iconImage.raycastTarget = false;
            }

            if (_countText != null)
            {
                _countText.raycastTarget = false;
            }
        }
    }
}
