// Path: Assets/Project/Scpripts/TreeFelling/TreeConfig.cs
// Purpose: Defines tree-felling configs, interaction contracts and gameplay messages for the tree module.
// Dependencies: UniTask, UnityEngine, PlayerWeight.

using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectResonance.PlayerWeight;
using ProjectResonance.ResourceNodes;
using UnityEngine;

namespace ProjectResonance.TreeFelling
{
    /// <summary>
    /// Describes the equipped axe tier used to resolve chopping damage.
    /// </summary>
    public enum AxeTier
    {
        /// <summary>
        /// No axe is equipped.
        /// </summary>
        None = 0,

        /// <summary>
        /// A basic stone axe is equipped.
        /// </summary>
        Stone = 1,

        /// <summary>
        /// An iron axe is equipped.
        /// </summary>
        Iron = 2,
    }

    /// <summary>
    /// Carries interaction data from the player or another interactor.
    /// </summary>
    public readonly struct InteractionContext
    {
        /// <summary>
        /// Creates a new interaction context.
        /// </summary>
        /// <param name="interactor">Transform that initiated the interaction.</param>
        /// <param name="origin">World-space origin of the interaction.</param>
        public InteractionContext(Transform interactor, Vector3 origin)
        {
            Interactor = interactor;
            Origin = origin;
        }

        /// <summary>
        /// Gets the transform that initiated the interaction.
        /// </summary>
        public Transform Interactor { get; }

        /// <summary>
        /// Gets the world-space interaction origin.
        /// </summary>
        public Vector3 Origin { get; }
    }

    /// <summary>
    /// Common interaction contract used by tree-felling objects.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Executes the default interaction.
        /// </summary>
        /// <param name="context">Interaction context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async operation handle.</returns>
        UniTask InteractAsync(InteractionContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the heavy interaction variant.
        /// </summary>
        /// <param name="context">Interaction context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async operation handle.</returns>
        UniTask HeavyInteractAsync(InteractionContext context, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Read-only inventory contract used by chopping interactions.
    /// </summary>
    public interface IInventoryQuery
    {
        /// <summary>
        /// Returns the currently equipped axe tier.
        /// </summary>
        /// <returns>Equipped axe tier.</returns>
        AxeTier GetEquippedAxeTier();
    }

    /// <summary>
    /// Inventory mutation contract used by log pickups.
    /// </summary>
    public interface IInventoryWriteService
    {
        /// <summary>
        /// Attempts to add logs to the inventory.
        /// </summary>
        /// <param name="amount">Number of logs to add.</param>
        /// <returns>True when the logs were accepted by the inventory.</returns>
        bool TryAddLog(int amount);
    }

    /// <summary>
    /// Supplies the transform where carried logs should visually follow the player.
    /// </summary>
    public interface IPlayerCarryAnchor
    {
        /// <summary>
        /// Gets the transform that carried logs follow.
        /// </summary>
        Transform FollowTransform { get; }
    }

    /// <summary>
    /// Legacy compatibility message that requests damage application to a specific resource node.
    /// </summary>
    public readonly struct ChopEvent
    {
        /// <summary>
        /// Creates a new chop event.
        /// </summary>
        /// <param name="tree">Target resource node.</param>
        /// <param name="hitDirection">World-space hit direction.</param>
        /// <param name="damage">Damage dealt by the hit.</param>
        public ChopEvent(ResourceNodeRuntime tree, Vector3 hitDirection, int damage)
        {
            Tree = tree;
            HitDirection = hitDirection;
            Damage = damage;
        }

        /// <summary>
        /// Gets the target resource node.
        /// </summary>
        public ResourceNodeRuntime Tree { get; }

        /// <summary>
        /// Gets the world-space hit direction.
        /// </summary>
        public Vector3 HitDirection { get; }

        /// <summary>
        /// Gets the damage dealt by the hit.
        /// </summary>
        public int Damage { get; }
    }

    /// <summary>
    /// Legacy compatibility message that broadcasts a successful hit on a resource node.
    /// </summary>
    public readonly struct TreeHitEvent
    {
        /// <summary>
        /// Creates a new tree hit event.
        /// </summary>
        /// <param name="tree">Target resource node.</param>
        /// <param name="hitsRemaining">Remaining durability before the tree falls.</param>
        public TreeHitEvent(ResourceNodeRuntime tree, int hitsRemaining)
        {
            Tree = tree;
            HitsRemaining = hitsRemaining;
        }

        /// <summary>
        /// Gets the hit resource node.
        /// </summary>
        public ResourceNodeRuntime Tree { get; }

        /// <summary>
        /// Gets the remaining durability of the tree.
        /// </summary>
        public int HitsRemaining { get; }
    }

    /// <summary>
    /// Legacy compatibility message that requests a destroy sequence for a specific resource node.
    /// </summary>
    public readonly struct TreeFallStartEvent
    {
        /// <summary>
        /// Creates a new tree fall start event.
        /// </summary>
        /// <param name="tree">Resource node that should start its legacy fall flow.</param>
        public TreeFallStartEvent(ResourceNodeRuntime tree)
        {
            Tree = tree;
        }

        /// <summary>
        /// Gets the resource node that should start its legacy fall flow.
        /// </summary>
        public ResourceNodeRuntime Tree { get; }
    }

    /// <summary>
    /// Requests a one-shot sound at a world position.
    /// </summary>
    public readonly struct SoundEvent
    {
        /// <summary>
        /// Creates a new sound event.
        /// </summary>
        /// <param name="soundId">Semantic sound identifier.</param>
        /// <param name="position">World-space playback position.</param>
        /// <param name="clip">Optional direct clip reference.</param>
        public SoundEvent(string soundId, Vector3 position, AudioClip clip)
        {
            SoundId = soundId;
            Position = position;
            Clip = clip;
        }

        /// <summary>
        /// Gets the semantic sound identifier.
        /// </summary>
        public string SoundId { get; }

        /// <summary>
        /// Gets the world-space playback position.
        /// </summary>
        public Vector3 Position { get; }

        /// <summary>
        /// Gets the optional clip reference.
        /// </summary>
        public AudioClip Clip { get; }
    }

    /// <summary>
    /// Requests a particle effect at a world position.
    /// </summary>
    public readonly struct ParticleEvent
    {
        /// <summary>
        /// Creates a new particle event.
        /// </summary>
        /// <param name="particleId">Semantic particle identifier.</param>
        /// <param name="position">World-space spawn position.</param>
        public ParticleEvent(string particleId, Vector3 position)
        {
            ParticleId = particleId;
            Position = position;
        }

        /// <summary>
        /// Gets the semantic particle identifier.
        /// </summary>
        public string ParticleId { get; }

        /// <summary>
        /// Gets the world-space spawn position.
        /// </summary>
        public Vector3 Position { get; }
    }

    /// <summary>
    /// Stores per-tree tuning, audio and decal settings.
    /// </summary>
    [CreateAssetMenu(fileName = "TreeConfig", menuName = "Project Resonance/Tree Felling/Tree Config")]
    public sealed class TreeConfig : ScriptableObject
    {
        [Header("Durability")]
        [SerializeField]
        [Min(1)]
        private int _maxHits = 4;

        [Header("Fragments")]
        [SerializeField]
        [Min(1)]
        private int _logsOnFall = 2;

        [SerializeField]
        [Min(0)]
        private int _branchesOnFall = 3;

        [Header("Motion")]
        [SerializeField]
        [Min(0.05f)]
        private float _swayDuration = 0.5f;

        [Header("Decals")]
        [SerializeField]
        private GameObject _decalPrefab;

        [SerializeField]
        private Vector3 _decalSize = new Vector3(0.32f, 0.32f, 0.32f);

        [SerializeField]
        [Range(0f, 1f)]
        private float _minDecalOpacity = 0.3f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _maxDecalOpacity = 1f;

        [SerializeField]
        [Min(1)]
        private int _decalPoolCapacity = 12;

        [Header("Audio")]
        [SerializeField]
        private AudioClip[] _hitSounds;

        [SerializeField]
        private AudioClip _fallSound;

        [SerializeField]
        private AudioClip _crackSound;

        /// <summary>
        /// Gets the durability threshold required to fell the tree.
        /// </summary>
        public int MaxHits => _maxHits;

        /// <summary>
        /// Gets the base amount of logs spawned after the fall.
        /// </summary>
        public int LogsOnFall => _logsOnFall;

        /// <summary>
        /// Gets the amount of branch fragments spawned after the fall.
        /// </summary>
        public int BranchesOnFall => _branchesOnFall;

        /// <summary>
        /// Gets the sway duration used before the final fall.
        /// </summary>
        public float SwayDuration => _swayDuration;

        /// <summary>
        /// Gets the decal prefab used for chopping marks.
        /// </summary>
        public GameObject DecalPrefab => _decalPrefab;

        /// <summary>
        /// Gets the world-space decal size.
        /// </summary>
        public Vector3 DecalSize => _decalSize;

        /// <summary>
        /// Gets the minimum decal opacity.
        /// </summary>
        public float MinDecalOpacity => _minDecalOpacity;

        /// <summary>
        /// Gets the maximum decal opacity.
        /// </summary>
        public float MaxDecalOpacity => _maxDecalOpacity;

        /// <summary>
        /// Gets the initial decal pool capacity.
        /// </summary>
        public int DecalPoolCapacity => _decalPoolCapacity;

        /// <summary>
        /// Gets the clip played after the tree lands.
        /// </summary>
        public AudioClip FallSound => _fallSound;

        /// <summary>
        /// Gets the clip played when the trunk gives way.
        /// </summary>
        public AudioClip CrackSound => _crackSound;

        /// <summary>
        /// Resolves a deterministic hit sound based on the strike index.
        /// </summary>
        /// <param name="strikeIndex">Zero-based strike index.</param>
        /// <returns>Audio clip for the strike, or null when none is configured.</returns>
        public AudioClip GetHitSound(int strikeIndex)
        {
            if (_hitSounds == null || _hitSounds.Length == 0)
            {
                return null;
            }

            var soundIndex = Mathf.Abs(strikeIndex) % _hitSounds.Length;
            return _hitSounds[soundIndex];
        }
    }

    /// <summary>
    /// Stores fall sequence timing, pooling and fragment placement settings.
    /// </summary>
    [CreateAssetMenu(fileName = "TreeFallConfig", menuName = "Project Resonance/Tree Felling/Tree Fall Config")]
    public sealed class TreeFallConfig : ScriptableObject
    {
        [Header("Timings")]
        [SerializeField]
        [Min(0.05f)]
        private float _fallDuration = 0.75f;

        [SerializeField]
        [Min(0.05f)]
        private float _branchLifetimeSeconds = 18f;

        [Header("Rotation")]
        [SerializeField]
        [Range(1f, 25f)]
        private float _swayAngleDegrees = 8f;

        [SerializeField]
        [Range(45f, 100f)]
        private float _fallAngleDegrees = 88f;

        [Header("Logs")]
        [SerializeField]
        private LogPickup _logPrefab;

        [SerializeField]
        [Min(1)]
        private int _minLogsOnFall = 2;

        [SerializeField]
        [Min(1)]
        private int _maxLogsOnFall = 3;

        [SerializeField]
        [Min(0.1f)]
        private float _logSpawnHeight = 0.55f;

        [SerializeField]
        [Min(0.1f)]
        private float _logSpawnLineLength = 2.6f;

        [SerializeField]
        [Min(0.1f)]
        private float _logLateralSpacing = 0.4f;

        [SerializeField]
        [Min(1)]
        private int _logPoolCapacity = 8;

        [Header("Branches")]
        [SerializeField]
        private GameObject _branchFragmentPrefab;

        [SerializeField]
        [Min(0.1f)]
        private float _branchSpawnHeight = 1.2f;

        [SerializeField]
        [Min(0.1f)]
        private float _branchScatterRadius = 1.4f;

        [SerializeField]
        [Min(0f)]
        private float _branchImpulse = 1.5f;

        [SerializeField]
        [Min(1)]
        private int _branchPoolCapacity = 12;

        /// <summary>
        /// Gets the fall duration after the sway.
        /// </summary>
        public float FallDuration => _fallDuration;

        /// <summary>
        /// Gets the lifetime of pooled branch fragments.
        /// </summary>
        public float BranchLifetimeSeconds => _branchLifetimeSeconds;

        /// <summary>
        /// Gets the sway angle in degrees.
        /// </summary>
        public float SwayAngleDegrees => _swayAngleDegrees;

        /// <summary>
        /// Gets the final fall angle in degrees.
        /// </summary>
        public float FallAngleDegrees => _fallAngleDegrees;

        /// <summary>
        /// Gets the pooled log pickup prefab.
        /// </summary>
        public LogPickup LogPrefab => _logPrefab;

        /// <summary>
        /// Gets the minimum allowed amount of logs per fall.
        /// </summary>
        public int MinLogsOnFall => _minLogsOnFall;

        /// <summary>
        /// Gets the maximum allowed amount of logs per fall.
        /// </summary>
        public int MaxLogsOnFall => _maxLogsOnFall;

        /// <summary>
        /// Gets the height offset used when spawning logs.
        /// </summary>
        public float LogSpawnHeight => _logSpawnHeight;

        /// <summary>
        /// Gets the line length used to distribute logs along the fallen trunk.
        /// </summary>
        public float LogSpawnLineLength => _logSpawnLineLength;

        /// <summary>
        /// Gets the lateral spacing used between spawned logs.
        /// </summary>
        public float LogLateralSpacing => _logLateralSpacing;

        /// <summary>
        /// Gets the initial log pool capacity.
        /// </summary>
        public int LogPoolCapacity => _logPoolCapacity;

        /// <summary>
        /// Gets the pooled branch fragment prefab.
        /// </summary>
        public GameObject BranchFragmentPrefab => _branchFragmentPrefab;

        /// <summary>
        /// Gets the height offset used when spawning branches.
        /// </summary>
        public float BranchSpawnHeight => _branchSpawnHeight;

        /// <summary>
        /// Gets the scatter radius used for branch fragments.
        /// </summary>
        public float BranchScatterRadius => _branchScatterRadius;

        /// <summary>
        /// Gets the impulse applied to branch rigidbodies.
        /// </summary>
        public float BranchImpulse => _branchImpulse;

        /// <summary>
        /// Gets the initial branch pool capacity.
        /// </summary>
        public int BranchPoolCapacity => _branchPoolCapacity;
    }
}
