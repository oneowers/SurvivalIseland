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
    /// Publishes buffered aim-stick input used to select a world-space hit target.
    /// </summary>
    public readonly struct AimInput
    {
        /// <summary>
        /// Creates a new aim input message.
        /// </summary>
        /// <param name="value">Normalized aim vector.</param>
        public AimInput(Vector2 value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the input vector.
        /// </summary>
        public Vector2 Value { get; }
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
    /// Publishes craft input.
    /// </summary>
    public readonly struct CraftInput
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

        [SerializeField]
        private InputActionReference _craftAction;

        private IBufferedPublisher<MoveInput> _movePublisher;
        private IBufferedPublisher<SprintInput> _sprintPublisher;
        private IBufferedPublisher<AimInput> _aimPublisher;
        private IPublisher<JumpInput> _jumpPublisher;
        private IPublisher<CrouchInput> _crouchPublisher;
        private IPublisher<InteractInput> _interactPublisher;
        private IPublisher<HeavyInteractInput> _heavyInteractPublisher;
        private IPublisher<CraftInput> _craftPublisher;
        private InputAction _runtimeCraftAction;
        private Vector2 _actionMoveInput;
        private Vector2 _externalMoveInput;
        private Vector2 _lastPublishedMoveInput;
        private Vector2 _externalAimInput;
        private Vector2 _lastPublishedAimInput;
        private bool _actionSprintPressed;
        private bool _externalSprintPressed;
        private bool _lastPublishedSprintPressed;
        private bool _isExternalMoveInputActive;
        private bool _isExternalAimInputActive;

        private bool _dependenciesInjected;
        private bool _isInitialized;

        [Inject]
        private void Construct(
            IBufferedPublisher<MoveInput> movePublisher,
            IBufferedPublisher<SprintInput> sprintPublisher,
            IBufferedPublisher<AimInput> aimPublisher,
            IPublisher<JumpInput> jumpPublisher,
            IPublisher<CrouchInput> crouchPublisher,
            IPublisher<InteractInput> interactPublisher,
            IPublisher<HeavyInteractInput> heavyInteractPublisher,
            IPublisher<CraftInput> craftPublisher)
        {
            _movePublisher = movePublisher;
            _sprintPublisher = sprintPublisher;
            _aimPublisher = aimPublisher;
            _jumpPublisher = jumpPublisher;
            _crouchPublisher = crouchPublisher;
            _interactPublisher = interactPublisher;
            _heavyInteractPublisher = heavyInteractPublisher;
            _craftPublisher = craftPublisher;
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
            LogActionState("Craft", ResolveCraftAction());

            BindContinuousAction(_moveAction, OnMovePerformed, OnMoveCanceled);
            BindButtonAction(_sprintAction, OnSprintPerformed, OnSprintCanceled);
            BindButtonAction(_jumpAction, OnJumpPerformed, null);
            BindButtonAction(_crouchAction, OnCrouchPerformed, null);
            BindButtonAction(_interactAction, OnInteractPerformed, null);
            BindButtonAction(_heavyInteractAction, OnHeavyInteractPerformed, null);
            BindButtonAction(ResolveCraftAction(), OnCraftPerformed, null);

            // Buffered state messages publish the initial values so late subscribers start from a deterministic state.
            PublishResolvedMoveInput(force: true);
            PublishResolvedSprintInput(force: true);
            PublishResolvedAimInput(force: true);

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

        /// <summary>
        /// Sets movement coming from an external non-Input System source such as a mobile joystick.
        /// </summary>
        /// <param name="value">External normalized movement vector.</param>
        public void SetExternalMoveInput(Vector2 value)
        {
            _externalMoveInput = Vector2.ClampMagnitude(value, 1f);
            _isExternalMoveInputActive = _externalMoveInput.sqrMagnitude > 0.0001f;
            PublishResolvedMoveInput();
        }

        /// <summary>
        /// Clears the current external movement source and falls back to authored Input Actions.
        /// </summary>
        public void ClearExternalMoveInput()
        {
            _externalMoveInput = Vector2.zero;
            _isExternalMoveInputActive = false;
            PublishResolvedMoveInput();
        }

        /// <summary>
        /// Sets aim input coming from an external non-Input System source such as a mobile aim joystick.
        /// </summary>
        /// <param name="value">External normalized aim vector.</param>
        public void SetExternalAimInput(Vector2 value)
        {
            _externalAimInput = Vector2.ClampMagnitude(value, 1f);
            _isExternalAimInputActive = _externalAimInput.sqrMagnitude > 0.0001f;
            PublishResolvedAimInput();
        }

        /// <summary>
        /// Clears the current external aim source.
        /// </summary>
        public void ClearExternalAimInput()
        {
            _externalAimInput = Vector2.zero;
            _isExternalAimInputActive = false;
            PublishResolvedAimInput();
        }

        /// <summary>
        /// Sets sprint state coming from an external non-Input System source such as a mobile hold button.
        /// </summary>
        /// <param name="isPressed">Whether sprint should be considered pressed.</param>
        public void SetExternalSprintState(bool isPressed)
        {
            _externalSprintPressed = isPressed;
            PublishResolvedSprintInput();
        }

        /// <summary>
        /// Publishes a jump trigger coming from mobile UI.
        /// </summary>
        public void TriggerExternalJump()
        {
            _jumpPublisher.Publish(new JumpInput());
        }

        /// <summary>
        /// Publishes a crouch trigger coming from mobile UI.
        /// </summary>
        public void TriggerExternalCrouch()
        {
            _crouchPublisher.Publish(new CrouchInput());
        }

        /// <summary>
        /// Publishes an interact trigger coming from mobile UI.
        /// </summary>
        public void TriggerExternalInteract()
        {
            _interactPublisher.Publish(new InteractInput());
        }

        /// <summary>
        /// Publishes a heavy interact trigger coming from mobile UI.
        /// </summary>
        public void TriggerExternalHeavyInteract()
        {
            _heavyInteractPublisher.Publish(new HeavyInteractInput());
        }

        /// <summary>
        /// Publishes a craft trigger coming from mobile UI.
        /// </summary>
        public void TriggerExternalCraft()
        {
            _craftPublisher.Publish(new CraftInput());
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

        private void BindButtonAction(
            InputAction action,
            System.Action<InputAction.CallbackContext> onPerformed,
            System.Action<InputAction.CallbackContext> onCanceled)
        {
            if (action == null)
            {
                return;
            }

            action.performed += onPerformed;
            if (onCanceled != null)
            {
                action.canceled += onCanceled;
            }

            action.Enable();

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerInputHandler] Enabled button action: {action.name}", this);
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

        private void LogActionState(string actionLabel, InputAction action)
        {
            if (!_enableDebugLogs)
            {
                return;
            }

            if (action == null)
            {
                Debug.LogError($"[PlayerInputHandler] {actionLabel} runtime action is NULL.", this);
                return;
            }

            Debug.Log(
                $"[PlayerInputHandler] {actionLabel} action ready. Name={action.name}, Map={action.actionMap?.name}",
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
            UnbindButtonAction(ResolveCraftAction(createIfMissing: false), OnCraftPerformed, null);

            if (_runtimeCraftAction != null)
            {
                _runtimeCraftAction.Dispose();
                _runtimeCraftAction = null;
            }
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

        private void UnbindButtonAction(
            InputAction action,
            System.Action<InputAction.CallbackContext> onPerformed,
            System.Action<InputAction.CallbackContext> onCanceled)
        {
            if (action == null)
            {
                return;
            }

            action.performed -= onPerformed;
            if (onCanceled != null)
            {
                action.canceled -= onCanceled;
            }

            action.Disable();
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            var value = Vector2.ClampMagnitude(context.ReadValue<Vector2>(), 1f);
            _actionMoveInput = value;
            PublishResolvedMoveInput();

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerInputHandler] Move performed: {value}", this);
            }
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            _actionMoveInput = Vector2.zero;
            PublishResolvedMoveInput();

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerInputHandler] Move canceled: (0,0)", this);
            }
        }

        private void OnSprintPerformed(InputAction.CallbackContext context)
        {
            _actionSprintPressed = true;
            PublishResolvedSprintInput();

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerInputHandler] Sprint performed.", this);
            }
        }

        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            _actionSprintPressed = false;
            PublishResolvedSprintInput();

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

        private void OnCraftPerformed(InputAction.CallbackContext context)
        {
            _craftPublisher.Publish(new CraftInput());

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerInputHandler] Craft performed.", this);
            }
        }

        private InputAction ResolveCraftAction(bool createIfMissing = true)
        {
            if (_craftAction != null && _craftAction.action != null)
            {
                return _craftAction.action;
            }

            if (!createIfMissing)
            {
                return _runtimeCraftAction;
            }

            if (_runtimeCraftAction == null)
            {
                // Crafting uses an explicit R fallback so the feature works even before a dedicated input asset action is authored.
                _runtimeCraftAction = new InputAction("Craft", InputActionType.Button, "<Keyboard>/r");
            }

            return _runtimeCraftAction;
        }

        private void PublishResolvedMoveInput(bool force = false)
        {
            var resolvedInput = ResolveMoveInput();
            if (!force && AreApproximatelyEqual(_lastPublishedMoveInput, resolvedInput))
            {
                return;
            }

            _lastPublishedMoveInput = resolvedInput;
            _movePublisher.Publish(new MoveInput(resolvedInput));
        }

        private Vector2 ResolveMoveInput()
        {
            // Mobile joystick should take priority only while it is actively moved.
            return _isExternalMoveInputActive ? _externalMoveInput : _actionMoveInput;
        }

        private void PublishResolvedAimInput(bool force = false)
        {
            var resolvedInput = ResolveAimInput();
            if (!force && AreApproximatelyEqual(_lastPublishedAimInput, resolvedInput))
            {
                return;
            }

            _lastPublishedAimInput = resolvedInput;
            _aimPublisher.Publish(new AimInput(resolvedInput));
        }

        private Vector2 ResolveAimInput()
        {
            return _isExternalAimInputActive ? _externalAimInput : Vector2.zero;
        }

        private static bool AreApproximatelyEqual(Vector2 left, Vector2 right)
        {
            return (left - right).sqrMagnitude <= 0.0001f;
        }

        private void PublishResolvedSprintInput(bool force = false)
        {
            var resolvedSprintState = _actionSprintPressed || _externalSprintPressed;
            if (!force && resolvedSprintState == _lastPublishedSprintPressed)
            {
                return;
            }

            _lastPublishedSprintPressed = resolvedSprintState;
            _sprintPublisher.Publish(new SprintInput(resolvedSprintState));
        }
    }
}
