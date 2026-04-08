// Path: Assets/Project/Scpripts/Crafting/RecipeDatabase.cs
// Purpose: Centralizes all crafting recipes and shared runtime tuning for world-space crafting.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.Crafting
{
    /// <summary>
    /// ScriptableObject database containing all crafting recipes.
    /// </summary>
    [CreateAssetMenu(fileName = "RecipeDatabase", menuName = "Project Resonance/Crafting/Recipe Database")]
    public sealed class RecipeDatabase : ScriptableObject
    {
        [SerializeField]
        private CraftingRecipe[] _allRecipes;

        [SerializeField]
        [Min(0.1f)]
        private float _campfireCraftRadius = 4f;

        [SerializeField]
        [Min(0.1f)]
        private float _craftedPreviewLifetime = 1.5f;

        /// <summary>
        /// Gets the central list of all authored recipes.
        /// </summary>
        public CraftingRecipe[] AllRecipes => _allRecipes;

        /// <summary>
        /// Gets the maximum distance from the campfire used by campfire recipes.
        /// </summary>
        public float CampfireCraftRadius => _campfireCraftRadius;

        /// <summary>
        /// Gets how long the crafted world preview remains visible.
        /// </summary>
        public float CraftedPreviewLifetime => _craftedPreviewLifetime;
    }
}
