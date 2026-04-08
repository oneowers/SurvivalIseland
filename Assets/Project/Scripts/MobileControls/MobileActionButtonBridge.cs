// Path: Assets/Project/Scripts/MobileControls/MobileActionButtonBridge.cs
// Purpose: Bridges scene-authored mobile UI buttons into the unified player input pipeline.
// Dependencies: UnityEngine, UnityEngine.EventSystems, VContainer, PlayerInputHandler.

using ProjectResonance.PlayerInput;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

namespace ProjectResonance.MobileControls
{
    /// <summary>
    /// Supported mobile action button types.
    /// </summary>
    public enum MobileActionType
    {
        /// <summary>
        /// Hold-to-sprint input.
        /// </summary>
        Sprint = 0,

        /// <summary>
        /// Jump input.
        /// </summary>
        Jump = 1,

        /// <summary>
        /// Crouch input.
        /// </summary>
        Crouch = 2,

        /// <summary>
        /// Standard interact input.
        /// </summary>
        Interact = 3,

        /// <summary>
        /// Heavy interact input.
        /// </summary>
        HeavyInteract = 4,

        /// <summary>
        /// Craft input.
        /// </summary>
        Craft = 5,
    }

    /// <summary>
    /// Bridges pointer input from a scene-authored mobile action button into <see cref="PlayerInputHandler"/>.
    /// </summary>
    [AddComponentMenu("Project Resonance/Mobile/Mobile Action Button Bridge")]
    [DisallowMultipleComponent]
    public sealed class MobileActionButtonBridge : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField]
        private MobileActionType _actionType;

        private PlayerInputHandler _playerInputHandler;

        [Inject]
        private void Construct(PlayerInputHandler playerInputHandler)
        {
            _playerInputHandler = playerInputHandler;
        }

        /// <summary>
        /// Configures which gameplay action this bridge should publish.
        /// </summary>
        /// <param name="actionType">Target mobile action type.</param>
        public void Configure(MobileActionType actionType)
        {
            _actionType = actionType;
        }

        /// <summary>
        /// Publishes the configured action when the pointer starts pressing the button.
        /// </summary>
        /// <param name="eventData">Current pointer event data.</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_playerInputHandler == null)
            {
                return;
            }

            switch (_actionType)
            {
                case MobileActionType.Sprint:
                    _playerInputHandler.SetExternalSprintState(true);
                    break;

                case MobileActionType.Jump:
                    _playerInputHandler.TriggerExternalJump();
                    break;

                case MobileActionType.Crouch:
                    _playerInputHandler.TriggerExternalCrouch();
                    break;

                case MobileActionType.Interact:
                    _playerInputHandler.TriggerExternalInteract();
                    break;

                case MobileActionType.HeavyInteract:
                    _playerInputHandler.TriggerExternalHeavyInteract();
                    break;

                case MobileActionType.Craft:
                    _playerInputHandler.TriggerExternalCraft();
                    break;
            }
        }

        /// <summary>
        /// Releases sprint when the pointer leaves the button.
        /// </summary>
        /// <param name="eventData">Current pointer event data.</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            ReleaseHoldAction();
        }

        /// <summary>
        /// Releases sprint when the pointer stops pressing the button.
        /// </summary>
        /// <param name="eventData">Current pointer event data.</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            ReleaseHoldAction();
        }

        private void OnDisable()
        {
            ReleaseHoldAction();
        }

        private void ReleaseHoldAction()
        {
            if (_playerInputHandler == null)
            {
                return;
            }

            if (_actionType == MobileActionType.Sprint)
            {
                _playerInputHandler.SetExternalSprintState(false);
            }
        }
    }
}
