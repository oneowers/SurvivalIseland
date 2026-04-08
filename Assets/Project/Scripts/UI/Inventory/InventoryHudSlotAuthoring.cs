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

        [SerializeField]
        private GameObject _durabilityBarRoot;

        [SerializeField]
        private Image _durabilityBarFill;

        private Sprite _fallbackIconSprite;

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

            _iconImage.sprite = ResolveIconSprite(icon, tint);
            _iconImage.color = tint;
            _iconImage.preserveAspect = true;
            _iconImage.enabled = _iconImage.sprite != null || tint.a > 0f;
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
        /// Applies the normalized durability value to the authored slot bar.
        /// </summary>
        /// <param name="normalizedDurability">Normalized durability in the range [0..1].</param>
        public void SetDurabilityNormalized(float normalizedDurability)
        {
            EnsureReferences();

            if (_durabilityBarFill == null)
            {
                return;
            }

            _durabilityBarFill.fillAmount = Mathf.Clamp01(normalizedDurability);
            _durabilityBarFill.color = new Color(0.39f, 0.75f, 0.29f, 1f);
            _durabilityBarFill.SetVerticesDirty();
            _durabilityBarFill.SetMaterialDirty();
        }

        /// <summary>
        /// Shows or hides the authored durability bar.
        /// </summary>
        /// <param name="isVisible">Whether the durability bar should be visible.</param>
        public void SetDurabilityVisible(bool isVisible)
        {
            EnsureReferences();

            if (_durabilityBarRoot != null)
            {
                _durabilityBarRoot.SetActive(isVisible);
                return;
            }

            if (_durabilityBarFill != null)
            {
                _durabilityBarFill.enabled = isVisible;
            }
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

            if (_durabilityBarRoot == null)
            {
                var durabilityRoot = transform.Find("DurabilityBarRoot");
                if (durabilityRoot != null)
                {
                    _durabilityBarRoot = durabilityRoot.gameObject;
                }
            }

            if (_durabilityBarFill == null)
            {
                var durabilityFill = transform.Find("DurabilityBarRoot/DurabilityBarFill");
                if (durabilityFill != null)
                {
                    _durabilityBarFill = durabilityFill.GetComponent<Image>();
                }
            }

            if (_durabilityBarFill != null)
            {
                _durabilityBarFill.type = Image.Type.Filled;
                _durabilityBarFill.fillMethod = Image.FillMethod.Horizontal;
                _durabilityBarFill.fillOrigin = 0;
                _durabilityBarFill.raycastTarget = false;
            }
        }

        private Sprite ResolveIconSprite(Sprite icon, Color tint)
        {
            if (icon != null || tint.a <= 0f)
            {
                return icon;
            }

            _fallbackIconSprite ??= Resources.GetBuiltinResource<Sprite>("UISprite.psd");
            return _fallbackIconSprite;
        }
    }
}
