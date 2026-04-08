// Path: Assets/Project/Scripts/Player/PlayerAimPlantingInteractor.cs
// Purpose: Converts right-stick aim into snapped ground planting when a plantable resource item is selected in the active inventory slot.
// Dependencies: MessagePipe, UnityEngine, VContainer.Unity, ProjectResonance.Inventory, ProjectResonance.PlayerInput, ProjectResonance.ResourceNodes.

using System;
using MessagePipe;
using ProjectResonance.Inventory;
using ProjectResonance.PlayerInput;
using ProjectResonance.ResourceNodes;
using UnityEngine;
using VContainer.Unity;

namespace ProjectResonance.PlayerCombat
{
    /// <summary>
    /// Immutable description of the current planting candidate under the right-stick aim.
    /// </summary>
    public readonly struct PlantingCandidate
    {
        /// <summary>
        /// Creates a new planting candidate.
        /// </summary>
        /// <param name="snappedPoint">Snapped candidate point before the ground probe is resolved.</param>
        /// <param name="placementPosition">Resolved ground placement position.</param>
        /// <param name="probeOrigin">Raycast probe origin.</param>
        /// <param name="isValid">Whether the placement is currently valid.</param>
        /// <param name="failureReason">Reason the placement failed when invalid.</param>
        public PlantingCandidate(Vector3 snappedPoint, Vector3 placementPosition, Vector3 probeOrigin, bool isValid, PlantingFailureReason failureReason)
        {
            SnappedPoint = snappedPoint;
            PlacementPosition = placementPosition;
            ProbeOrigin = probeOrigin;
            IsValid = isValid;
            FailureReason = failureReason;
        }

        /// <summary>
        /// Gets the snapped candidate point before the ground hit is finalized.
        /// </summary>
        public Vector3 SnappedPoint { get; }

        /// <summary>
        /// Gets the final validated placement position on the ground.
        /// </summary>
        public Vector3 PlacementPosition { get; }

        /// <summary>
        /// Gets the origin used for the downward ground probe.
        /// </summary>
        public Vector3 ProbeOrigin { get; }

        /// <summary>
        /// Gets whether this candidate can currently spawn a planted tree.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the reason the candidate is invalid when validation fails.
        /// </summary>
        public PlantingFailureReason FailureReason { get; }
    }

    /// <summary>
    /// Enumerates planting validation failures.
    /// </summary>
    public enum PlantingFailureReason
    {
        /// <summary>
        /// The candidate is valid.
        /// </summary>
        None = 0,

        /// <summary>
        /// The player camera or character controller is unavailable.
        /// </summary>
        MissingReferences = 1,

        /// <summary>
        /// No valid plantable item is selected.
        /// </summary>
        MissingPlantableItem = 2,

        /// <summary>
        /// The downward probe did not hit valid ground.
        /// </summary>
        NoGround = 3,

        /// <summary>
        /// The ground probe hit an existing resource node instead of free terrain.
        /// </summary>
        HitExistingNode = 4,

        /// <summary>
        /// Another collider blocks the snapped planting position.
        /// </summary>
        Blocked = 5,
    }

    /// <summary>
    /// Handles right-stick planting when the player selects a plantable resource item such as a log.
    /// </summary>
    public sealed class PlayerAimPlantingInteractor : IStartable, ILateTickable, IDisposable, IPlayerAimModeQuery
    {
        private readonly CharacterController _characterController;
        private readonly Camera _playerCamera;
        private readonly AimTargetingConfig _aimTargetingConfig;
        private readonly InventorySystem _inventorySystem;
        private readonly PlantableResourceSpawnService _spawnService;
        private readonly PlantingPreviewVisualizer _plantingPreviewVisualizer;
        private readonly IBufferedSubscriber<AimInput> _aimInputSubscriber;
        private readonly IBufferedSubscriber<ActiveSlotChangedEvent> _activeSlotChangedSubscriber;

        private IDisposable _aimInputSubscription;
        private IDisposable _activeSlotChangedSubscription;
        private Collider[] _overlapResults;
        private Vector2 _aimInput;
        private int _activeSlotIndex;
        private bool _wasAimActive;
        private bool _lastLoggedPlantingMode;
        private bool _hasCandidate;
        private bool _hasLoggedCandidate;
        private bool _lastLoggedCandidateValid;
        private PlantingCandidate _currentCandidate;
        private Vector3 _lastLoggedSnappedPoint;
        private PlantingFailureReason _lastLoggedFailureReason;

        /// <summary>
        /// Creates the player planting interactor.
        /// </summary>
        /// <param name="characterController">Player character controller.</param>
        /// <param name="playerCamera">Runtime player camera.</param>
        /// <param name="aimTargetingConfig">Shared aim targeting config used for radius and dead zone.</param>
        /// <param name="inventorySystem">Shared player inventory.</param>
        /// <param name="spawnService">Plantable resource spawn bridge.</param>
        /// <param name="plantingPreviewVisualizer">World-space preview visualizer for planting candidates.</param>
        /// <param name="aimInputSubscriber">Buffered aim input subscriber.</param>
        /// <param name="activeSlotChangedSubscriber">Buffered active-slot subscriber.</param>
        public PlayerAimPlantingInteractor(
            CharacterController characterController,
            Camera playerCamera,
            AimTargetingConfig aimTargetingConfig,
            InventorySystem inventorySystem,
            PlantableResourceSpawnService spawnService,
            PlantingPreviewVisualizer plantingPreviewVisualizer,
            IBufferedSubscriber<AimInput> aimInputSubscriber,
            IBufferedSubscriber<ActiveSlotChangedEvent> activeSlotChangedSubscriber)
        {
            _characterController = characterController;
            _playerCamera = playerCamera;
            _aimTargetingConfig = aimTargetingConfig;
            _inventorySystem = inventorySystem;
            _spawnService = spawnService;
            _plantingPreviewVisualizer = plantingPreviewVisualizer;
            _aimInputSubscriber = aimInputSubscriber;
            _activeSlotChangedSubscriber = activeSlotChangedSubscriber;
        }

        /// <summary>
        /// Gets whether the active right-stick mode is planting.
        /// </summary>
        public bool IsPlantingModeActive => ResolveSelectedPlantableDefinition() != null;

        /// <summary>
        /// Starts listening for aim and active-slot changes.
        /// </summary>
        public void Start()
        {
            _overlapResults = new Collider[Mathf.Max(1, _aimTargetingConfig != null ? _aimTargetingConfig.MaxDetectedColliders : 32)];
            _aimInputSubscription = _aimInputSubscriber.Subscribe(OnAimInputChanged);
            _activeSlotChangedSubscription = _activeSlotChangedSubscriber.Subscribe(OnActiveSlotChanged);
            RefreshPlantingModeLog();
        }

        /// <summary>
        /// Updates the current planting candidate and debug feedback.
        /// </summary>
        public void LateTick()
        {
            RefreshPlantingModeLog();

            if (!IsPlantingModeActive || !HasActiveAim())
            {
                _hasCandidate = false;
                _plantingPreviewVisualizer?.Hide();
                return;
            }

            var plantableDefinition = ResolveSelectedPlantableDefinition();
            _currentCandidate = EvaluateCandidate(plantableDefinition);
            _hasCandidate = true;
            LogCandidateIfChanged();
            DrawDebugCandidate();
            _plantingPreviewVisualizer?.Show(plantableDefinition, _currentCandidate);
        }

        /// <summary>
        /// Releases subscriptions owned by the planting interactor.
        /// </summary>
        public void Dispose()
        {
            _aimInputSubscription?.Dispose();
            _aimInputSubscription = null;
            _activeSlotChangedSubscription?.Dispose();
            _activeSlotChangedSubscription = null;
            _plantingPreviewVisualizer?.Dispose();
        }

        private void OnAimInputChanged(AimInput message)
        {
            var nextAimInput = Vector2.ClampMagnitude(message.Value, 1f);
            var wasAimActive = HasActiveAim();
            var willBeAimActive = nextAimInput.sqrMagnitude > ResolveSelectionDeadZone() * ResolveSelectionDeadZone();

            if (wasAimActive && !willBeAimActive && IsPlantingModeActive)
            {
                TryPlantCurrentCandidate();
            }

            _aimInput = nextAimInput;
            _wasAimActive = willBeAimActive;
        }

        private void OnActiveSlotChanged(ActiveSlotChangedEvent message)
        {
            _activeSlotIndex = message.CurrentSlotIndex;
            _hasCandidate = false;
            _plantingPreviewVisualizer?.Hide();
            RefreshPlantingModeLog();
        }

        private void TryPlantCurrentCandidate()
        {
            var plantableDefinition = ResolveSelectedPlantableDefinition();
            if (plantableDefinition == null)
            {
                return;
            }

            if (!_hasCandidate)
            {
                _currentCandidate = EvaluateCandidate(plantableDefinition);
                _hasCandidate = true;
            }

            if (!_currentCandidate.IsValid)
            {
                Debug.LogWarning(
                    $"[PlayerAimPlantingInteractor] Planting aborted for '{plantableDefinition.DisplayName}'. Reason={_currentCandidate.FailureReason}, Candidate={_currentCandidate.SnappedPoint}");
                return;
            }

            if (!_spawnService.TrySpawn(plantableDefinition, _currentCandidate.PlacementPosition, Quaternion.identity, out var spawnedObject))
            {
                Debug.LogWarning($"[PlayerAimPlantingInteractor] Planting spawn failed for '{plantableDefinition.DisplayName}' at {_currentCandidate.PlacementPosition}.");
                return;
            }

            if (!_inventorySystem.TryConsumeItemAt(_activeSlotIndex, 1))
            {
                Debug.LogWarning(
                    $"[PlayerAimPlantingInteractor] Planting rollback triggered because active slot {_activeSlotIndex} no longer contained '{plantableDefinition.DisplayName}'.");

                if (spawnedObject != null)
                {
                    UnityEngine.Object.Destroy(spawnedObject);
                }

                return;
            }

            var remainingCount = _inventorySystem.GetSlot(_activeSlotIndex).Count;
            Debug.Log(
                $"[PlayerAimPlantingInteractor] Planted '{plantableDefinition.NodeDisplayName}' at {_currentCandidate.PlacementPosition}. RemainingLogsInActiveSlot={remainingCount}, Slot={_activeSlotIndex}");
        }

        private PlantingCandidate EvaluateCandidate(ItemDefinition definition)
        {
            if (_characterController == null || (_playerCamera == null && Camera.main == null))
            {
                return new PlantingCandidate(Vector3.zero, Vector3.zero, Vector3.zero, false, PlantingFailureReason.MissingReferences);
            }

            if (definition == null || !definition.CanPlantFromInventory)
            {
                return new PlantingCandidate(Vector3.zero, Vector3.zero, Vector3.zero, false, PlantingFailureReason.MissingPlantableItem);
            }

            var desiredPoint = ResolveDesiredPoint();
            var snappedPoint = ResolveSnappedPoint(definition, desiredPoint);
            var probeOrigin = new Vector3(
                snappedPoint.x,
                _characterController.transform.position.y + definition.PlantingProbeHeight,
                snappedPoint.z);

            if (!Physics.Raycast(
                    probeOrigin,
                    Vector3.down,
                    out var groundHit,
                    definition.PlantingProbeDistance,
                    definition.PlantingGroundMask.value,
                    QueryTriggerInteraction.Ignore))
            {
                return new PlantingCandidate(snappedPoint, snappedPoint, probeOrigin, false, PlantingFailureReason.NoGround);
            }

            if (groundHit.collider != null && groundHit.collider.GetComponentInParent<ResourceNodeRuntime>() != null)
            {
                return new PlantingCandidate(snappedPoint, groundHit.point, probeOrigin, false, PlantingFailureReason.HitExistingNode);
            }

            var placementPosition = groundHit.point;
            var blockingHitCount = Physics.OverlapSphereNonAlloc(
                placementPosition,
                definition.PlantingClearRadius,
                _overlapResults,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (var index = 0; index < blockingHitCount; index++)
            {
                var blockingCollider = _overlapResults[index];
                if (blockingCollider == null)
                {
                    continue;
                }

                if (blockingCollider == groundHit.collider)
                {
                    continue;
                }

                if (_characterController != null && blockingCollider.transform.root == _characterController.transform.root)
                {
                    continue;
                }

                return new PlantingCandidate(snappedPoint, placementPosition, probeOrigin, false, PlantingFailureReason.Blocked);
            }

            return new PlantingCandidate(snappedPoint, placementPosition, probeOrigin, true, PlantingFailureReason.None);
        }

        private Vector3 ResolveDesiredPoint()
        {
            var origin = _characterController.transform.position;
            var cameraToUse = _playerCamera != null ? _playerCamera : Camera.main;

            var planarForward = cameraToUse.transform.forward;
            planarForward.y = 0f;
            if (planarForward.sqrMagnitude <= Mathf.Epsilon)
            {
                planarForward = _characterController.transform.forward;
                planarForward.y = 0f;
            }

            planarForward.Normalize();

            var planarRight = cameraToUse.transform.right;
            planarRight.y = 0f;
            if (planarRight.sqrMagnitude <= Mathf.Epsilon)
            {
                planarRight = Vector3.Cross(Vector3.up, planarForward);
            }

            planarRight.Normalize();

            var planarDirection = (planarRight * _aimInput.x) + (planarForward * _aimInput.y);
            if (planarDirection.sqrMagnitude > Mathf.Epsilon)
            {
                planarDirection.Normalize();
            }

            var radius = Mathf.Clamp01(_aimInput.magnitude) * ResolveMaxAimRadius();
            return origin + (planarDirection * radius);
        }

        private static Vector3 ResolveSnappedPoint(ItemDefinition definition, Vector3 desiredPoint)
        {
            if (definition == null || !definition.SnapPlantingToIntegerGrid)
            {
                return desiredPoint;
            }

            return new Vector3(
                Mathf.Round(desiredPoint.x),
                desiredPoint.y,
                Mathf.Round(desiredPoint.z));
        }

        private void LogCandidateIfChanged()
        {
            if (!_hasCandidate)
            {
                return;
            }

            var candidateChanged = !_hasLoggedCandidate
                || _lastLoggedCandidateValid != _currentCandidate.IsValid
                || _lastLoggedFailureReason != _currentCandidate.FailureReason
                || (_lastLoggedSnappedPoint - _currentCandidate.SnappedPoint).sqrMagnitude > 0.0001f;

            if (!candidateChanged)
            {
                return;
            }

            _hasLoggedCandidate = true;
            _lastLoggedCandidateValid = _currentCandidate.IsValid;
            _lastLoggedSnappedPoint = _currentCandidate.SnappedPoint;
            _lastLoggedFailureReason = _currentCandidate.FailureReason;

            if (_currentCandidate.IsValid)
            {
                Debug.Log(
                    $"[PlayerAimPlantingInteractor] Valid planting candidate at {_currentCandidate.PlacementPosition} from snapped point {_currentCandidate.SnappedPoint}.");
                return;
            }

            Debug.LogWarning(
                $"[PlayerAimPlantingInteractor] Invalid planting candidate. Reason={_currentCandidate.FailureReason}, SnappedPoint={_currentCandidate.SnappedPoint}");
        }

        private void DrawDebugCandidate()
        {
            var debugColor = _currentCandidate.IsValid ? Color.green : Color.red;
            var playerOrigin = _characterController.transform.position + (Vector3.up * 0.9f);
            Debug.DrawLine(playerOrigin, _currentCandidate.ProbeOrigin, debugColor);
            Debug.DrawLine(_currentCandidate.ProbeOrigin, _currentCandidate.PlacementPosition, debugColor);
            Debug.DrawRay(_currentCandidate.PlacementPosition, Vector3.up * 0.75f, debugColor);
        }

        private void RefreshPlantingModeLog()
        {
            var isPlantingModeActive = IsPlantingModeActive;
            if (_lastLoggedPlantingMode == isPlantingModeActive)
            {
                return;
            }

            _lastLoggedPlantingMode = isPlantingModeActive;
            _hasLoggedCandidate = false;

            if (isPlantingModeActive)
            {
                var definition = ResolveSelectedPlantableDefinition();
                Debug.Log(
                    $"[PlayerAimPlantingInteractor] Entered planting mode. Item={(definition != null ? definition.DisplayName : "null")}, Slot={_activeSlotIndex}");
                return;
            }

            Debug.Log("[PlayerAimPlantingInteractor] Exited planting mode.");
        }

        private ItemDefinition ResolveSelectedPlantableDefinition()
        {
            if (_inventorySystem == null)
            {
                return null;
            }

            var activeSlot = _inventorySystem.GetSlot(_activeSlotIndex);
            return activeSlot.ItemDefinition != null
                   && activeSlot.ItemDefinition.IsResourceNode
                   && activeSlot.ItemDefinition.CanPlantFromInventory
                ? activeSlot.ItemDefinition
                : null;
        }

        private bool HasActiveAim()
        {
            return _wasAimActive || _aimInput.sqrMagnitude > ResolveSelectionDeadZone() * ResolveSelectionDeadZone();
        }

        private float ResolveSelectionDeadZone()
        {
            return _aimTargetingConfig != null ? _aimTargetingConfig.SelectionDeadZone : 0.08f;
        }

        private float ResolveMaxAimRadius()
        {
            return _aimTargetingConfig != null ? _aimTargetingConfig.MaxAimRadius : 3f;
        }
    }
}
