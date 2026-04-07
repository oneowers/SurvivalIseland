// Path: Assets/Project/Scpripts/TreeFelling/PlayerTreeInteractor.cs
// Purpose: Converts player interact inputs into interactions with the currently selected nearby tree target.
// Dependencies: UniTask, MessagePipe, UnityEngine, PlayerInput, VContainer.

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MessagePipe;
using ProjectResonance.PlayerInput;
using UnityEngine;
using VContainer;

namespace ProjectResonance.TreeFelling
{
    /// <summary>
    /// Reads interact input events and executes interactions against world objects in front of the player.
    /// </summary>
    [AddComponentMenu("Project Resonance/Tree Felling/Player Tree Interactor")]
    [DisallowMultipleComponent]
    public sealed class PlayerTreeInteractor : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField]
        private bool _enableDebugLogs = true;

        [Header("References")]
        [SerializeField]
        private Transform _interactionOrigin;

        [SerializeField]
        private TreeTargetDetector _treeTargetDetector;

        private readonly List<MonoBehaviour> _componentBuffer = new List<MonoBehaviour>(8);

        private ISubscriber<InteractInput> _interactSubscriber;
        private ISubscriber<HeavyInteractInput> _heavyInteractSubscriber;

        private IDisposable _interactSubscription;
        private IDisposable _heavyInteractSubscription;
        private bool _isInitialized;

        [Inject]
        private void Construct(
            ISubscriber<InteractInput> interactSubscriber,
            ISubscriber<HeavyInteractInput> heavyInteractSubscriber)
        {
            _interactSubscriber = interactSubscriber;
            _heavyInteractSubscriber = heavyInteractSubscriber;
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
                Debug.Log($"[PlayerTreeInteractor] Initialized. TargetDetector={(_treeTargetDetector != null ? _treeTargetDetector.name : "null")}, Origin={(_interactionOrigin != null ? _interactionOrigin.name : "self")}", this);
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
            _treeTargetDetector = GetComponent<TreeTargetDetector>();
        }

        private void HandleInteract(bool isHeavyInteraction)
        {
            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerTreeInteractor] Input received. Heavy={isHeavyInteraction}", this);
            }

            if (!TryResolveInteractable(out var interactable))
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[PlayerTreeInteractor] No interactable found from current tree target.", this);
                }

                return;
            }

            var context = new InteractionContext(_interactionOrigin != null ? _interactionOrigin : transform, ResolveOriginPosition());

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerTreeInteractor] Interactable found: {((MonoBehaviour)interactable).name}. Heavy={isHeavyInteraction}", (MonoBehaviour)interactable);
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

            if (_treeTargetDetector == null)
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning("[PlayerTreeInteractor] TreeTargetDetector is not assigned.", this);
                }

                return false;
            }

            var currentTree = _treeTargetDetector.CurrentTarget;
            if (currentTree == null)
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[PlayerTreeInteractor] TreeTargetDetector has no current target.", this);
                }

                return false;
            }

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerTreeInteractor] Current target resolved: {currentTree.name}", currentTree);
            }

            _componentBuffer.Clear();
            currentTree.GetComponents(_componentBuffer);

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
                Debug.Log($"[PlayerTreeInteractor] Current target {currentTree.name}, but no IInteractable component was found on it.", currentTree);
            }

            _componentBuffer.Clear();
            return false;
        }

        private Vector3 ResolveOriginPosition()
        {
            if (_interactionOrigin != null)
            {
                return _interactionOrigin.position;
            }

            return transform.position;
        }
    }
}
