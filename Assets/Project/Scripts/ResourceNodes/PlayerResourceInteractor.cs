// Path: Assets/Project/Scripts/ResourceNodes/PlayerResourceInteractor.cs
// Purpose: Converts player interact inputs into generic resource interactions against the currently targeted node.
// Dependencies: UniTask, MessagePipe, UnityEngine, PlayerInput, ProjectResonance.TreeFelling, ProjectResonance.ResourceNodes, VContainer.

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MessagePipe;
using ProjectResonance.MobileControls;
using ProjectResonance.PlayerInput;
using ProjectResonance.TreeFelling;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace ProjectResonance.ResourceNodes
{
    /// <summary>
    /// Reads interact input events and executes interactions against world resources in front of the player.
    /// </summary>
    [AddComponentMenu("Project Resonance/Resource Nodes/Player Resource Interactor")]
    [DisallowMultipleComponent]
    public sealed class PlayerResourceInteractor : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField]
        private bool _enableDebugLogs = false;

        [Header("References")]
        [SerializeField]
        private Transform _interactionOrigin;

        [SerializeField]
        [FormerlySerializedAs("_treeTargetDetector")]
        private ResourceTargetDetector _resourceTargetDetector;

        private readonly List<MonoBehaviour> _componentBuffer = new List<MonoBehaviour>(8);

        private ISubscriber<InteractInput> _interactSubscriber;
        private ISubscriber<HeavyInteractInput> _heavyInteractSubscriber;
        private IMobileModeService _mobileModeService;

        private IDisposable _interactSubscription;
        private IDisposable _heavyInteractSubscription;
        private bool _isInitialized;

        [Inject]
        private void Construct(
            ISubscriber<InteractInput> interactSubscriber,
            ISubscriber<HeavyInteractInput> heavyInteractSubscriber,
            IMobileModeService mobileModeService)
        {
            _interactSubscriber = interactSubscriber;
            _heavyInteractSubscriber = heavyInteractSubscriber;
            _mobileModeService = mobileModeService;
        }

        /// <summary>
        /// Starts listening for interact inputs.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            _interactSubscription = _interactSubscriber.Subscribe(_ => HandleInteract(isHeavyInteraction: false));
            _heavyInteractSubscription = _heavyInteractSubscriber.Subscribe(_ => HandleInteract(isHeavyInteraction: true));

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerResourceInteractor] Initialized. TargetDetector={(_resourceTargetDetector != null ? _resourceTargetDetector.name : "null")}, Origin={(_interactionOrigin != null ? _interactionOrigin.name : "self")}", this);
            }
        }

        /// <summary>
        /// Stops listening for interact inputs.
        /// </summary>
        public void Shutdown()
        {
            _interactSubscription?.Dispose();
            _interactSubscription = null;

            _heavyInteractSubscription?.Dispose();
            _heavyInteractSubscription = null;

            _isInitialized = false;
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private void Reset()
        {
            _interactionOrigin = transform;
            _resourceTargetDetector = GetComponent<ResourceTargetDetector>();
        }

        private void HandleInteract(bool isHeavyInteraction)
        {
            if (_mobileModeService != null && _mobileModeService.IsMobileModeActive)
            {
                return;
            }

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerResourceInteractor] Input received. Heavy={isHeavyInteraction}", this);
            }

            if (!TryResolveInteractable(out var interactable))
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[PlayerResourceInteractor] No interactable found from current resource target.", this);
                }

                return;
            }

            var context = new InteractionContext(_interactionOrigin != null ? _interactionOrigin : transform, ResolveOriginPosition());

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerResourceInteractor] Interactable found: {((MonoBehaviour)interactable).name}. Heavy={isHeavyInteraction}", (MonoBehaviour)interactable);
            }

            if (isHeavyInteraction)
            {
                interactable.HeavyInteractAsync(context).Forget();
                return;
            }

            interactable.InteractAsync(context).Forget();
        }

        private bool TryResolveInteractable(out IInteractable interactable)
        {
            interactable = null;

            if (_resourceTargetDetector == null)
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning("[PlayerResourceInteractor] ResourceTargetDetector is not assigned.", this);
                }

                return false;
            }

            var currentNode = _resourceTargetDetector.CurrentTarget;
            if (currentNode == null)
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[PlayerResourceInteractor] ResourceTargetDetector has no current target.", this);
                }

                return false;
            }

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerResourceInteractor] Current target resolved: {currentNode.name}", currentNode);
            }

            _componentBuffer.Clear();
            currentNode.GetComponents(_componentBuffer);

            for (var index = 0; index < _componentBuffer.Count; index++)
            {
                if (_componentBuffer[index] is IInteractable candidate)
                {
                    interactable = candidate;
                    _componentBuffer.Clear();
                    return true;
                }
            }

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerResourceInteractor] Current target {currentNode.name}, but no IInteractable component was found on it.", currentNode);
            }

            _componentBuffer.Clear();
            return false;
        }

        private Vector3 ResolveOriginPosition()
        {
            return _interactionOrigin != null ? _interactionOrigin.position : transform.position;
        }
    }
}
