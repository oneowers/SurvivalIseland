// Path: Assets/Project/Scpripts/Crafting/CraftingSystem.cs
// Purpose: Validates world-space held-item recipes, executes crafting, and spawns crafted output previews.
// Dependencies: UniTask, MessagePipe, UnityEngine.Pool, VContainer, Inventory, Campfire, PlayerInput.

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using ProjectResonance.Campfire;
using ProjectResonance.Inventory;
using ProjectResonance.PlayerInput;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;

namespace ProjectResonance.Crafting
{
    /// <summary>
    /// Supported reasons for a crafting failure.
    /// </summary>
    public enum CraftFailureReason
    {
        /// <summary>
        /// The player is already crafting another recipe.
        /// </summary>
        Busy = 0,

        /// <summary>
        /// The player is not holding enough items to attempt crafting.
        /// </summary>
        NeedTwoHeldItems = 1,

        /// <summary>
        /// No recipe matched the current held items.
        /// </summary>
        NoMatchingRecipe = 2,

        /// <summary>
        /// Required ingredients are missing from inventory.
        /// </summary>
        MissingIngredients = 3,

        /// <summary>
        /// The recipe requires a campfire nearby.
        /// </summary>
        CampfireRequired = 4,

        /// <summary>
        /// The nearby campfire is below the required level.
        /// </summary>
        CampfireLevelTooLow = 5,

        /// <summary>
        /// There is not enough free inventory capacity for the output.
        /// </summary>
        InventoryFull = 6,

        /// <summary>
        /// The recipe output configuration is invalid.
        /// </summary>
        InvalidRecipe = 7,

        /// <summary>
        /// Crafting was interrupted before completion.
        /// </summary>
        Interrupted = 8,
    }

    /// <summary>
    /// Published after a successful craft.
    /// </summary>
    public readonly struct CraftSuccessEvent
    {
        /// <summary>
        /// Creates a new craft success event.
        /// </summary>
        /// <param name="recipe">Recipe that succeeded.</param>
        /// <param name="outputItem">Produced item definition.</param>
        /// <param name="outputCount">Produced item count.</param>
        public CraftSuccessEvent(CraftingRecipe recipe, ItemDefinition outputItem, int outputCount)
        {
            Recipe = recipe;
            OutputItem = outputItem;
            OutputCount = outputCount;
        }

        /// <summary>
        /// Gets the completed recipe.
        /// </summary>
        public CraftingRecipe Recipe { get; }

        /// <summary>
        /// Gets the produced item definition.
        /// </summary>
        public ItemDefinition OutputItem { get; }

        /// <summary>
        /// Gets the produced item count.
        /// </summary>
        public int OutputCount { get; }
    }

    /// <summary>
    /// Published after a failed craft attempt.
    /// </summary>
    public readonly struct CraftFailEvent
    {
        /// <summary>
        /// Creates a new craft fail event.
        /// </summary>
        /// <param name="recipe">Recipe that was attempted, or null when nothing matched.</param>
        /// <param name="reason">Failure reason.</param>
        public CraftFailEvent(CraftingRecipe recipe, CraftFailureReason reason)
        {
            Recipe = recipe;
            Reason = reason;
        }

        /// <summary>
        /// Gets the attempted recipe.
        /// </summary>
        public CraftingRecipe Recipe { get; }

        /// <summary>
        /// Gets the failure reason.
        /// </summary>
        public CraftFailureReason Reason { get; }
    }

    /// <summary>
    /// Runtime crafting service driven by held items and player input.
    /// </summary>
    public sealed class CraftingSystem : IStartable, IDisposable
    {
        private readonly RecipeDatabase _recipeDatabase;
        private readonly InventoryConfig _inventoryConfig;
        private readonly InventorySystem _inventorySystem;
        private readonly IItemVisualFactory _itemVisualFactory;
        private readonly HeldItemController _heldItemController;
        private readonly ISubscriber<CraftInput> _craftInputSubscriber;
        private readonly IPublisher<CraftSuccessEvent> _craftSuccessPublisher;
        private readonly IPublisher<CraftFailEvent> _craftFailPublisher;
        private readonly CampfireAnchor _campfireAnchor;
        private readonly CampfireState _campfireState;
        private readonly List<CraftingRecipe> _knownRecipes;
        private readonly ItemDefinition[] _heldInputsBuffer;
        private readonly Dictionary<ItemDefinition, ObjectPool<GameObject>> _previewPoolByItemDefinition;
        private readonly Dictionary<GameObject, ObjectPool<GameObject>> _ownerPoolByPreviewInstance;

        private IDisposable _craftInputSubscription;
        private CancellationTokenSource _disposeCancellation;
        private bool _isCrafting;

        /// <summary>
        /// Creates the world-space crafting system.
        /// </summary>
        /// <param name="inventorySystem">Runtime inventory service.</param>
        /// <param name="inventoryConfig">Shared inventory authoring config.</param>
        /// <param name="itemVisualFactory">Shared visual factory used for safe preview creation.</param>
        /// <param name="heldItemController">Held item controller.</param>
        /// <param name="craftInputSubscriber">Craft input subscriber.</param>
        /// <param name="craftSuccessPublisher">Craft success publisher.</param>
        /// <param name="craftFailPublisher">Craft fail publisher.</param>
        /// <param name="recipeDatabase">Optional recipe database asset.</param>
        /// <param name="campfireAnchor">Optional campfire anchor reference.</param>
        /// <param name="campfireState">Optional campfire runtime state.</param>
        public CraftingSystem(
            InventorySystem inventorySystem,
            InventoryConfig inventoryConfig,
            IItemVisualFactory itemVisualFactory,
            HeldItemController heldItemController,
            ISubscriber<CraftInput> craftInputSubscriber,
            IPublisher<CraftSuccessEvent> craftSuccessPublisher,
            IPublisher<CraftFailEvent> craftFailPublisher,
            RecipeDatabase recipeDatabase = null,
            CampfireAnchor campfireAnchor = null,
            CampfireState campfireState = null)
        {
            _inventorySystem = inventorySystem;
            _inventoryConfig = inventoryConfig;
            _itemVisualFactory = itemVisualFactory;
            _heldItemController = heldItemController;
            _craftInputSubscriber = craftInputSubscriber;
            _craftSuccessPublisher = craftSuccessPublisher;
            _craftFailPublisher = craftFailPublisher;
            _recipeDatabase = recipeDatabase;
            _campfireAnchor = campfireAnchor;
            _campfireState = campfireState;
            _knownRecipes = new List<CraftingRecipe>();
            _heldInputsBuffer = new ItemDefinition[2];
            _previewPoolByItemDefinition = new Dictionary<ItemDefinition, ObjectPool<GameObject>>();
            _ownerPoolByPreviewInstance = new Dictionary<GameObject, ObjectPool<GameObject>>();

            if (_recipeDatabase != null && _recipeDatabase.AllRecipes != null)
            {
                for (var index = 0; index < _recipeDatabase.AllRecipes.Length; index++)
                {
                    var recipe = _recipeDatabase.AllRecipes[index];
                    if (recipe != null)
                    {
                        _knownRecipes.Add(recipe);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the known runtime recipe list.
        /// </summary>
        public IReadOnlyList<CraftingRecipe> KnownRecipes => _knownRecipes;

        /// <summary>
        /// Starts listening for craft input messages.
        /// </summary>
        public void Start()
        {
            _disposeCancellation = new CancellationTokenSource();
            _craftInputSubscription = _craftInputSubscriber.Subscribe(_ => TryCraftAsync(_disposeCancellation.Token).Forget());
        }

        /// <summary>
        /// Stops subscriptions and disposes pooled previews.
        /// </summary>
        public void Dispose()
        {
            _craftInputSubscription?.Dispose();
            _craftInputSubscription = null;

            if (_disposeCancellation != null)
            {
                _disposeCancellation.Cancel();
                _disposeCancellation.Dispose();
                _disposeCancellation = null;
            }

            foreach (var pool in _previewPoolByItemDefinition.Values)
            {
                pool.Dispose();
            }

            _previewPoolByItemDefinition.Clear();
            _ownerPoolByPreviewInstance.Clear();
        }

        /// <summary>
        /// Returns the first craftable recipe matching the provided input items.
        /// </summary>
        /// <param name="inputs">Held input item definitions.</param>
        /// <returns>Matching recipe, or null when nothing matches.</returns>
        public CraftingRecipe CheckCraftable(ItemDefinition[] inputs)
        {
            var inputCount = CountNonNullInputs(inputs);
            if (inputCount <= 0)
            {
                return null;
            }

            for (var recipeIndex = 0; recipeIndex < _knownRecipes.Count; recipeIndex++)
            {
                var recipe = _knownRecipes[recipeIndex];
                if (recipe != null && MatchesRecipe(recipe, inputs, inputCount))
                {
                    return recipe;
                }
            }

            return null;
        }

        /// <summary>
        /// Executes a craft request and mutates inventory when the recipe succeeds.
        /// </summary>
        /// <param name="recipe">Recipe to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True when crafting completed successfully.</returns>
        public async UniTask<bool> ExecuteCraft(CraftingRecipe recipe, CancellationToken cancellationToken = default)
        {
            if (_isCrafting)
            {
                PublishFailure(recipe, CraftFailureReason.Busy);
                return false;
            }

            if (!CanCraft(recipe, out var failureReason))
            {
                PublishFailure(recipe, failureReason);
                return false;
            }

            _isCrafting = true;

            try
            {
                _heldItemController.TriggerAnimation(recipe.AnimationTrigger);

                if (recipe.CraftDuration > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(recipe.CraftDuration),
                        DelayType.DeltaTime,
                        PlayerLoopTiming.Update,
                        cancellationToken);
                }

                if (!ConsumeIngredients(recipe))
                {
                    PublishFailure(recipe, CraftFailureReason.MissingIngredients);
                    return false;
                }

                if (!_inventorySystem.AddItem(recipe.OutputItem, recipe.OutputCount))
                {
                    RestoreIngredients(recipe);
                    PublishFailure(recipe, CraftFailureReason.InventoryFull);
                    return false;
                }

                _heldItemController.ClearHeldItems();
                SpawnCraftedPreview(recipe.OutputItem, cancellationToken).Forget();
                _craftSuccessPublisher.Publish(new CraftSuccessEvent(recipe, recipe.OutputItem, recipe.OutputCount));
                return true;
            }
            catch (OperationCanceledException)
            {
                PublishFailure(recipe, CraftFailureReason.Interrupted);
                return false;
            }
            finally
            {
                _isCrafting = false;
            }
        }

        private async UniTaskVoid TryCraftAsync(CancellationToken cancellationToken)
        {
            if (_isCrafting)
            {
                PublishFailure(null, CraftFailureReason.Busy);
                return;
            }

            var heldCount = _heldItemController != null
                ? _heldItemController.GetHeldItemsNonAlloc(_heldInputsBuffer)
                : 0;

            if (heldCount < 2)
            {
                PublishFailure(null, CraftFailureReason.NeedTwoHeldItems);
                return;
            }

            var recipe = CheckCraftable(_heldInputsBuffer);
            if (recipe == null)
            {
                PublishFailure(null, CraftFailureReason.NoMatchingRecipe);
                return;
            }

            await ExecuteCraft(recipe, cancellationToken);
        }

        private bool CanCraft(CraftingRecipe recipe, out CraftFailureReason failureReason)
        {
            if (recipe == null || recipe.OutputItem == null || recipe.OutputCount <= 0)
            {
                failureReason = CraftFailureReason.InvalidRecipe;
                return false;
            }

            var requiredItems = recipe.RequiredItems;
            if (requiredItems == null || requiredItems.Length == 0)
            {
                failureReason = CraftFailureReason.InvalidRecipe;
                return false;
            }

            if (recipe.RequiresCampfire)
            {
                if (_campfireAnchor == null || _campfireState == null || !_campfireState.IsLit)
                {
                    failureReason = CraftFailureReason.CampfireRequired;
                    return false;
                }

                var craftRadius = _recipeDatabase != null ? _recipeDatabase.CampfireCraftRadius : 0f;
                var distanceToCampfire = Vector3.Distance(_heldItemController.transform.position, _campfireAnchor.FirePoint.position);
                if (craftRadius <= 0f || distanceToCampfire > craftRadius)
                {
                    failureReason = CraftFailureReason.CampfireRequired;
                    return false;
                }

                if (_campfireState.Level < recipe.MinimumLevel)
                {
                    failureReason = CraftFailureReason.CampfireLevelTooLow;
                    return false;
                }
            }

            for (var ingredientIndex = 0; ingredientIndex < requiredItems.Length; ingredientIndex++)
            {
                var ingredient = requiredItems[ingredientIndex];
                var itemDefinition = ingredient.ItemDefinition;
                if (itemDefinition == null || ingredient.Count <= 0)
                {
                    failureReason = CraftFailureReason.InvalidRecipe;
                    return false;
                }

                if (WasItemAlreadyValidated(requiredItems, ingredientIndex, itemDefinition))
                {
                    continue;
                }

                var requiredCount = SumRequiredCount(requiredItems, itemDefinition);
                if (!_inventorySystem.HasItem(itemDefinition, requiredCount))
                {
                    failureReason = CraftFailureReason.MissingIngredients;
                    return false;
                }
            }

            if (!_inventorySystem.CanAddItem(recipe.OutputItem, recipe.OutputCount))
            {
                failureReason = CraftFailureReason.InventoryFull;
                return false;
            }

            failureReason = CraftFailureReason.InvalidRecipe;
            return true;
        }

        private bool ConsumeIngredients(CraftingRecipe recipe)
        {
            var requiredItems = recipe.RequiredItems;
            for (var ingredientIndex = 0; ingredientIndex < requiredItems.Length; ingredientIndex++)
            {
                var itemDefinition = requiredItems[ingredientIndex].ItemDefinition;
                if (itemDefinition == null || WasItemAlreadyValidated(requiredItems, ingredientIndex, itemDefinition))
                {
                    continue;
                }

                var requiredCount = SumRequiredCount(requiredItems, itemDefinition);
                if (!_inventorySystem.RemoveItem(itemDefinition, requiredCount))
                {
                    return false;
                }
            }

            return true;
        }

        private void RestoreIngredients(CraftingRecipe recipe)
        {
            var requiredItems = recipe.RequiredItems;
            for (var ingredientIndex = 0; ingredientIndex < requiredItems.Length; ingredientIndex++)
            {
                var itemDefinition = requiredItems[ingredientIndex].ItemDefinition;
                if (itemDefinition == null || WasItemAlreadyValidated(requiredItems, ingredientIndex, itemDefinition))
                {
                    continue;
                }

                var requiredCount = SumRequiredCount(requiredItems, itemDefinition);
                _inventorySystem.AddItem(itemDefinition, requiredCount);
            }
        }

        private bool MatchesRecipe(CraftingRecipe recipe, ItemDefinition[] inputs, int inputCount)
        {
            var requiredItems = recipe.RequiredItems;
            if (requiredItems == null || requiredItems.Length == 0)
            {
                return false;
            }

            var requiredInputCount = 0;
            for (var ingredientIndex = 0; ingredientIndex < requiredItems.Length; ingredientIndex++)
            {
                var ingredient = requiredItems[ingredientIndex];
                if (ingredient.ItemDefinition == null || ingredient.Count <= 0)
                {
                    return false;
                }

                requiredInputCount += ingredient.Count;
            }

            if (requiredInputCount != inputCount)
            {
                return false;
            }

            for (var ingredientIndex = 0; ingredientIndex < requiredItems.Length; ingredientIndex++)
            {
                var ingredient = requiredItems[ingredientIndex];
                var requiredCount = CountMatchingInputs(inputs, ingredient.ItemDefinition);
                if (requiredCount != SumRequiredCount(requiredItems, ingredient.ItemDefinition))
                {
                    return false;
                }
            }

            // Inputs also need to be restricted to recipe ingredients so extra items never pass on count alone.
            for (var inputIndex = 0; inputIndex < inputs.Length; inputIndex++)
            {
                var input = inputs[inputIndex];
                if (input == null)
                {
                    continue;
                }

                if (!ContainsIngredient(requiredItems, input))
                {
                    return false;
                }
            }

            return true;
        }

        private async UniTaskVoid SpawnCraftedPreview(ItemDefinition outputItem, CancellationToken cancellationToken)
        {
            if (outputItem == null)
            {
                return;
            }

            var previewPool = ResolvePreviewPool(outputItem);
            var previewInstance = previewPool.Get();
            _ownerPoolByPreviewInstance[previewInstance] = previewPool;

            var originPosition = _heldItemController != null ? _heldItemController.CraftSpawnPosition : Vector3.zero;
            var originRotation = _heldItemController != null ? _heldItemController.CraftSpawnRotation : Quaternion.identity;

            previewInstance.transform.SetParent(null, false);
            previewInstance.transform.SetPositionAndRotation(originPosition, originRotation);
            PreparePreviewInstance(previewInstance);

            try
            {
                var previewLifetime = _recipeDatabase != null ? _recipeDatabase.CraftedPreviewLifetime : 0f;
                if (previewLifetime <= 0f)
                {
                    previewPool.Release(previewInstance);
                    return;
                }

                await UniTask.Delay(
                    TimeSpan.FromSeconds(previewLifetime),
                    DelayType.DeltaTime,
                    PlayerLoopTiming.Update,
                    cancellationToken);

                previewPool.Release(previewInstance);
            }
            catch (OperationCanceledException)
            {
                previewPool.Release(previewInstance);
            }
        }

        private ObjectPool<GameObject> ResolvePreviewPool(ItemDefinition itemDefinition)
        {
            if (_previewPoolByItemDefinition.TryGetValue(itemDefinition, out var existingPool))
            {
                return existingPool;
            }

            var defaultCapacity = _inventoryConfig != null
                ? _inventoryConfig.CraftedPreviewPoolCapacityPerItem
                : Mathf.Max(1, _inventorySystem.MaxSlots);
            var maxPoolSize = _inventoryConfig != null
                ? Mathf.Max(defaultCapacity, _inventoryConfig.MaxPooledVisualsPerItem)
                : Mathf.Max(defaultCapacity, _inventorySystem.MaxSlots);

            var pool = new ObjectPool<GameObject>(
                () => CreatePreviewInstance(itemDefinition),
                instance => instance.SetActive(true),
                instance =>
                {
                    if (instance != null)
                    {
                        instance.transform.SetParent(null, false);
                        instance.SetActive(false);
                    }
                },
                instance =>
                {
                    if (instance != null)
                    {
                        UnityEngine.Object.Destroy(instance);
                    }
                },
                false,
                defaultCapacity,
                maxPoolSize);

            _previewPoolByItemDefinition.Add(itemDefinition, pool);
            return pool;
        }

        private GameObject CreatePreviewInstance(ItemDefinition itemDefinition)
        {
            var previewInstance = _itemVisualFactory != null
                ? _itemVisualFactory.CreateCraftPreviewVisual(itemDefinition)
                : new GameObject(itemDefinition != null ? itemDefinition.DisplayName : "CraftedPreview");
            previewInstance.SetActive(false);
            return previewInstance;
        }

        private void PreparePreviewInstance(GameObject previewInstance)
        {
            if (previewInstance == null)
            {
                return;
            }

            var colliders = previewInstance.GetComponentsInChildren<Collider>(true);
            for (var index = 0; index < colliders.Length; index++)
            {
                if (colliders[index] != null)
                {
                    colliders[index].enabled = false;
                }
            }

            if (previewInstance.TryGetComponent<Rigidbody>(out var rigidbody))
            {
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }
        }

        private void PublishFailure(CraftingRecipe recipe, CraftFailureReason reason)
        {
            _craftFailPublisher.Publish(new CraftFailEvent(recipe, reason));
        }

        private static int CountNonNullInputs(ItemDefinition[] inputs)
        {
            if (inputs == null)
            {
                return 0;
            }

            var inputCount = 0;
            for (var inputIndex = 0; inputIndex < inputs.Length; inputIndex++)
            {
                if (inputs[inputIndex] != null)
                {
                    inputCount++;
                }
            }

            return inputCount;
        }

        private static bool ContainsIngredient(ItemStack[] requiredItems, ItemDefinition itemDefinition)
        {
            for (var ingredientIndex = 0; ingredientIndex < requiredItems.Length; ingredientIndex++)
            {
                if (requiredItems[ingredientIndex].ItemDefinition == itemDefinition)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountMatchingInputs(ItemDefinition[] inputs, ItemDefinition itemDefinition)
        {
            var matchingCount = 0;
            for (var inputIndex = 0; inputIndex < inputs.Length; inputIndex++)
            {
                if (inputs[inputIndex] == itemDefinition)
                {
                    matchingCount++;
                }
            }

            return matchingCount;
        }

        private static int SumRequiredCount(ItemStack[] requiredItems, ItemDefinition itemDefinition)
        {
            var requiredCount = 0;
            for (var ingredientIndex = 0; ingredientIndex < requiredItems.Length; ingredientIndex++)
            {
                if (requiredItems[ingredientIndex].ItemDefinition == itemDefinition)
                {
                    requiredCount += requiredItems[ingredientIndex].Count;
                }
            }

            return requiredCount;
        }

        private static bool WasItemAlreadyValidated(ItemStack[] requiredItems, int currentIngredientIndex, ItemDefinition itemDefinition)
        {
            for (var ingredientIndex = 0; ingredientIndex < currentIngredientIndex; ingredientIndex++)
            {
                if (requiredItems[ingredientIndex].ItemDefinition == itemDefinition)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
