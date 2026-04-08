// Path: Assets/Project/Scpripts/Crafting/CraftingRecipe.cs
// Purpose: Defines a single world-space crafting recipe with campfire requirements and timing.
// Dependencies: UnityEngine, Campfire, Inventory.

using ProjectResonance.Campfire;
using ProjectResonance.Inventory;
using UnityEngine;

namespace ProjectResonance.Crafting
{
    /// <summary>
    /// Authoring asset for a crafting recipe.
    /// </summary>
    [CreateAssetMenu(fileName = "CraftingRecipe", menuName = "Project Resonance/Crafting/Crafting Recipe")]
    public sealed class CraftingRecipe : ScriptableObject
    {
        [SerializeField]
        private ItemStack[] _requiredItems;

        [SerializeField]
        private ItemDefinition _outputItem;

        [SerializeField]
        [Min(1)]
        private int _outputCount = 1;

        [SerializeField]
        private bool _requiresCampfire;

        [SerializeField]
        private CampfireLevel _minimumLevel = CampfireLevel.Basic;

        [SerializeField]
        [Min(0f)]
        private float _craftDuration;

        [SerializeField]
        private string _animationTrigger;

        /// <summary>
        /// Gets the required ingredient stacks for this recipe.
        /// </summary>
        public ItemStack[] RequiredItems => _requiredItems;

        /// <summary>
        /// Gets the resulting output item.
        /// </summary>
        public ItemDefinition OutputItem => _outputItem;

        /// <summary>
        /// Gets the number of output items produced.
        /// </summary>
        public int OutputCount => Mathf.Max(1, _outputCount);

        /// <summary>
        /// Gets whether an active campfire is required.
        /// </summary>
        public bool RequiresCampfire => _requiresCampfire;

        /// <summary>
        /// Gets the minimum campfire level required by the recipe.
        /// </summary>
        public CampfireLevel MinimumLevel => _minimumLevel;

        /// <summary>
        /// Gets the craft duration in seconds.
        /// </summary>
        public float CraftDuration => _craftDuration;

        /// <summary>
        /// Gets the animation trigger played during crafting.
        /// </summary>
        public string AnimationTrigger => _animationTrigger;
    }
}
