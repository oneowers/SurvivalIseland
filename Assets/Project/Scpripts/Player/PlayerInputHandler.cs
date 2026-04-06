// Path: Assets/Project/Scpripts/Player/PlayerInputHandler.cs
// Purpose: Collects player input from the new Input System and publishes it through MessagePipe.
// Dependencies: MessagePipe, Unity Input System, UnityEngine.

using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace ProjectResonance.PlayerInput
{
    /// <summary>
    /// Publishes movement input.
    /// </summary>
    public readonly struct MoveInput
    {
        /// <summary>
        /// Creates a new movement input message.
        /// </summary>
        /// <param name="value">Normalized movement input.</param>
        public MoveInput(Vector2 value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the input vector.
        /// </summary>
        public Vector2 Value { get; }
    }

    /// <summary>
    /// Publishes sprint input state.
    /// </summary>
    public readonly struct SprintInput
    {
        /// <summary>
        /// Creates a new sprint input message.
        /// </summary>
        /// <param name="isPressed">Whether sprint is pressed.</param>
        public SprintInput(bool isPressed)
        {
            IsPressed = isPressed;
        }

        /// <summary>
        /// Gets whether sprint is pressed.
        /// </summary>
        public bool IsPressed { get; }
    }

    /// <summary>
    /// Publishes jump input.
    /// </summary>
    public readonly struct JumpInput
    {
    }

    /// <summary>
    /// Publishes crouch input.
    /// </summary>
    public readonly struct CrouchInput
    {
    }

    /// <summary>
    /// Publishes interact input.
    /// </summary>
    public readonly struct InteractInput
    {
    }

    /// <summary>
    /// Publishes heavy interact input.
    /// </summary>
    public readonly struct HeavyInteractInput
    {
    }

    /// <summary>
    /// Reads the new Input System and publishes input messages without gameplay logic.
    /// </summary>
    public sealed class PlayerInputHandler : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField]
        private bool _enableDebugLogs = true;

        [Header("Input Actions")]
        [SerializeField]
        private InputActionReference _moveAction;

        [SerializeField]
        private InputActionReference _sprintAction;

        [SerializeField]
        private InputActionReference _jumpAction;

        [SerializeField]
        private InputActionReference _crouchAction;

        [SerializeField]
        private InputActionReference _interactAction;

        [SerializeField]
        private InputActionReference _heavyInteractAction;

        private IBufferedPublisher<MoveInput> _movePublisher;
        private IBufferedPublisher<SprintInput> _sprintPublisher;
        private IPublisher<JumpInput> _jumpPublisher;
        private IPublisher<CrouchInput> _crouchPublisher;
        private IPublisher<InteractInput> _interactPublisher;
        private IPublisher<HeavyInteractInput> _heavyInteractPublisher;

        private bool _dependenciesInjected;
        private bool _isInitialized;

        [Inject]
        private void Construct(
            IBufferedPublisher<MoveInput> movePublisher,
            IBufferedPublisher<SprintInput> sprintPublisher,
            IPublisher<JumpInput> jumpPublisher,
            IPublisher<CrouchInput> crouchPublisher,
            IPublisher<InteractInput> interactPublisher,
            IPublisher<HeavyInteractInput> heavyInteractPublisher)
        {
            _movePublisher = movePublisher;
            _sprintPublisher = sprintPublisher;
            _jumpPublisher = jumpPublisher;
            _crouchPublisher = crouchPublisher;
            _interactPublisher = interactPublisher;
            _heavyInteractPublisher = heavyInteractPublisher;
            _dependenciesInjected = true;

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerInputHandler] Construct completed. Dependencies injected.", this);
            }
        }

        /// <summary>
        /// Enables actions and starts publishing player input.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerInputHandler] Initialize started.", this);
            }

            LogActionState("Move", _moveAction);
            LogActionState("Sprint", _sprintAction);
            LogActionState("Jump", _jumpAction);
            LogActionState("Crouch", _crouchAction);
            LogActionState("Interact", _interactAction);
            LogActionState("HeavyInteract", _heavyInteractAction);

            BindContinuousAction(_moveAction, OnMovePerformed, OnMoveCanceled);
            BindButtonAction(_sprintAction, OnSprintPerformed, OnSprintCanceled);
            BindButtonAction(_jumpAction, OnJumpPerformed, null);
            BindButtonAction(_crouchAction, OnCrouchPerformed, null);
            BindButtonAction(_interactAction, OnInteractPerformed, null);
            BindButtonAction(_heavyInteractAction, OnHeavyInteractPerformed, null);

            // Buffered state messages publish the initial values so late subscribers start from a deterministic state.
            _movePublisher.Publish(new MoveInput(Vector2.zero));
            _sprintPublisher.Publish(new SprintInput(false));

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerInputHandler] Initialize finished. Waiting for input...", this);
            }
        }

        private void Awake()
        {
            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerInputHandler] Awake fired.", this);
            }
        }

        private void Start()
        {
            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerInputHandler] Start fired. DependenciesInjected={_dependenciesInjected}, IsInitialized={_isInitialized}", this);
            }

            if (_isInitialized)
            {
                return;
            }

            if (!_dependenciesInjected)
            {
                Debug.LogError("[PlayerInputHandler] Start reached before DI injection. Check PlayerInstaller / LifetimeScope.", this);
                return;
            }

            Initialize();
        }

        private void OnDestroy()
        {
            DisposeBindings();
        }

        private void BindContinuousAction(
            InputActionReference actionReference,
            System.Action<InputAction.CallbackContext> onPerformed,
            System.Action<InputAction.CallbackContext> onCanceled)
        {
            if (actionReference == null || actionReference.action == null)
            {
                return;
            }

            actionReference.action.performed += onPerformed;
            actionReference.action.canceled += onCanceled;
            actionReference.action.Enable();

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerInputHandler] Enabled continuous action: {actionReference.action.name}", this);
            }
        }

        private void BindButtonAction(
            InputActionReference actionReference,
            System.Action<InputAction.CallbackContext> onPerformed,
            System.Action<InputAction.CallbackContext> onCanceled)
        {
            if (actionReference == null || actionReference.action == null)
            {
                return;
            }

            actionReference.action.performed += onPerformed;
            if (onCanceled != null)
            {
                actionReference.action.canceled += onCanceled;
            }

            actionReference.action.Enable();

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerInputHandler] Enabled button action: {actionReference.action.name}", this);
            }
        }

        private void LogActionState(string actionLabel, InputActionReference actionReference)
        {
            if (!_enableDebugLogs)
            {
                return;
            }

            if (actionReference == null)
            {
                Debug.LogError($"[PlayerInputHandler] {actionLabel} action reference is NULL.", this);
                return;
            }

            if (actionReference.action == null)
            {
                Debug.LogError($"[PlayerInputHandler] {actionLabel} action is NULL inside InputActionReference.", this);
                return;
            }

            Debug.Log(
                $"[PlayerInputHandler] {actionLabel} action ready. Name={actionReference.action.name}, Map={actionReference.action.actionMap?.name}",
                this);
        }

        private void DisposeBindings()
        {
            UnbindContinuousAction(_moveAction, OnMovePerformed, OnMoveCanceled);
            UnbindButtonAction(_sprintAction, OnSprintPerformed, OnSprintCanceled);
            UnbindButtonAction(_jumpAction, OnJumpPerformed, null);
            UnbindButtonAction(_crouchAction, OnCrouchPerformed, null);
            UnbindButtonAction(_interactAction, OnInteractPerformed, null);
            UnbindButtonAction(_heavyInteractAction, OnHeavyInteractPerformed, null);
        }

        private void UnbindContinuousAction(
            InputActionReference actionReference,
            System.Action<InputAction.CallbackContext> onPerformed,
            System.Action<InputAction.CallbackContext> onCanceled)
        {
            if (actionReference == null || actionReference.action == null)
            {
                return;
            }

            actionReference.action.performed -= onPerformed;
            actionReference.action.canceled -= onCanceled;
            actionReference.action.Disable();
        }

        private void UnbindButtonAction(
            InputActionReference actionReference,
            System.Action<InputAction.CallbackContext> onPerformed,
            System.Action<InputAction.CallbackContext> onCanceled)
        {
            if (actionReference == null || actionReference.action == null)
            {
                return;
            }

            actionReference.action.performed -= onPerformed;
            if (onCanceled != null)
            {
                actionReference.action.canceled -= onCanceled;
            }

            actionReference.action.Disable();
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            var value = context.ReadValue<Vector2>();
            _movePublisher.Publish(new MoveInput(Vector2.ClampMagnitude(value, 1f)));

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerInputHandler] Move performed: {value}", this);
            }
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            _movePublisher.Publish(new MoveInput(Vector2.zero));

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerInputHandler] Move canceled: (0,0)", this);
            }
        }

        private void OnSprintPerformed(InputAction.CallbackContext context)
        {
            _sprintPublisher.Publish(new SprintInput(true));

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerInputHandler] Sprint performed.", this);
            }
        }

        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            _sprintPublisher.Publish(new SprintInput(false));

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerInputHandler] Sprint canceled.", this);
            }
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            _jumpPublisher.Publish(new JumpInput());

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerInputHandler] Jump performed.", this);
            }
        }

        private void OnCrouchPerformed(InputAction.CallbackContext context)
        {
            _crouchPublisher.Publish(new CrouchInput());

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerInputHandler] Crouch performed.", this);
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            _interactPublisher.Publish(new InteractInput());

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerInputHandler] Interact performed.", this);
            }
        }

        private void OnHeavyInteractPerformed(InputAction.CallbackContext context)
        {
            _heavyInteractPublisher.Publish(new HeavyInteractInput());

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerInputHandler] HeavyInteract performed.", this);
            }
        }
    }
}
