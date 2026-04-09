// Path: Assets/Project/Scripts/ResourceNodes/PlayerResourceInteractor.cs
// Purpose: Converts player interact inputs into generic resource interactions against the currently targeted node.
// Dependencies: UniTask, UnityEngine, PlayerInput, ProjectResonance.TreeFelling, ProjectResonance.ResourceNodes, VContainer.

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        [Header("References")]
        [SerializeField]
        private Transform _interactionOrigin;

        [SerializeField]
        [FormerlySerializedAs("_treeTargetDetector")]
        private ResourceTargetDetector _resourceTargetDetector;

        private readonly List<MonoBehaviour> _componentBuffer = new List<MonoBehaviour>(8);

        private PlayerInputHandler _playerInputHandler;
        private IMobileModeService _mobileModeService;

        private bool _isInitialized;

        [Inject]
        private void Construct(
            PlayerInputHandler playerInputHandler,
            IMobileModeService mobileModeService)
        {
            _playerInputHandler = playerInputHandler;
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
            if (_playerInputHandler != null)
            {
                _playerInputHandler.InteractPerformed += OnInteractPerformed;
                _playerInputHandler.HeavyInteractPerformed += OnHeavyInteractPerformed;
            }
        }

        /// <summary>
        /// Stops listening for interact inputs.
        /// </summary>
        public void Shutdown()
        {
            if (_playerInputHandler != null)
            {
                _playerInputHandler.InteractPerformed -= OnInteractPerformed;
                _playerInputHandler.HeavyInteractPerformed -= OnHeavyInteractPerformed;
            }

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

        private void OnInteractPerformed(InteractInput _)
        {
            HandleInteract(isHeavyInteraction: false);
        }

        private void OnHeavyInteractPerformed(HeavyInteractInput _)
        {
            HandleInteract(isHeavyInteraction: true);
        }

        private void HandleInteract(bool isHeavyInteraction)
        {
            if (_mobileModeService != null && _mobileModeService.IsMobileModeActive)
            {
                return;
            }

            if (!TryResolveInteractable(out var interactable))
            {
                return;
            }

            var context = new InteractionContext(_interactionOrigin != null ? _interactionOrigin : transform, ResolveOriginPosition());

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
                return false;
            }

            var currentNode = _resourceTargetDetector.CurrentTarget;
            if (currentNode == null)
            {
                return false;
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

            _componentBuffer.Clear();
            return false;
        }

        private Vector3 ResolveOriginPosition()
        {
            return _interactionOrigin != null ? _interactionOrigin.position : transform.position;
        }
    }
}
