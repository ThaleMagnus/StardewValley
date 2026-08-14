using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using ThaleTheGreat.UniversalItemBags.Integration;

namespace ThaleTheGreat.UniversalItemBags.Discovery;

internal sealed class UniversalDiscoveryService
{
    private const string ItemBagsModId = "SlayerDharok.Item_Bags";
    private const string JsonAssetsModId = "spacechase0.JsonAssets";

    private static readonly string[] RelevantAssets =
    {
        "Data/Objects",
        "Data/BigCraftables",
        "Data/ObjectContextTags",
        "Data/Fish",
        "Data/Crops",
        "Data/FruitTrees",
        "Data/Machines",
        "Data/CookingRecipes"
    };

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly IManifest manifest;
    private readonly ModConfig config;
    private readonly ItemBagsBridge bridge;
    private readonly Dictionary<string, List<ExplicitRegistration>> explicitRegistrations = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<string>> assignments = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> unclassifiedItems = new(StringComparer.OrdinalIgnoreCase);

    private IJsonAssetsApi? jsonAssetsApi;
    private int refreshDelay = -1;
    private bool refreshing;

    public UniversalDiscoveryService(
        IModHelper helper,
        IMonitor monitor,
        IManifest manifest,
        ModConfig config,
        ItemBagsBridge bridge
    )
    {
        this.helper = helper;
        this.monitor = monitor;
        this.manifest = manifest;
        this.config = config;
        this.bridge = bridge;
    }

    public void RegisterEvents()
    {
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
    }

    public void ScheduleRefresh(int ticks)
    {
        int normalizedTicks = Math.Max(1, ticks);
        if (refreshDelay < 0 || normalizedTicks < refreshDelay)
            refreshDelay = normalizedTicks;
    }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetAssignments()
    {
        return assignments.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase
        );
    }

    public IReadOnlyList<string> GetUnclassifiedItems()
    {
        return unclassifiedItems.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public bool RegisterItem(
        string ownerModId,
        string qualifiedItemId,
        IEnumerable<string> bagTypeIds,
        UniversalBagSize minimumBagSize,
        bool? hasQualities
    )
    {
        if (string.IsNullOrWhiteSpace(ownerModId)
            || !Enum.IsDefined(typeof(UniversalBagSize), minimumBagSize)
            || !TryParseQualifiedObjectId(qualifiedItemId, out string itemId, out bool isBigCraftable))
        {
            return false;
        }

        string[] normalizedBagTypeIds = (bagTypeIds ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (normalizedBagTypeIds.Length == 0 || normalizedBagTypeIds.Any(value => !StandardBagTypeIds.All.Contains(value)))
            return false;

        ExplicitRegistration registration = new(
            ownerModId.Trim(),
            $"{(isBigCraftable ? "(BC)" : "(O)")}{itemId}",
            itemId,
            isBigCraftable,
            normalizedBagTypeIds,
            minimumBagSize,
            hasQualities
        );

        if (!explicitRegistrations.TryGetValue(registration.OwnerModId, out List<ExplicitRegistration>? registrations))
        {
            registrations = new List<ExplicitRegistration>();
            explicitRegistrations.Add(registration.OwnerModId, registrations);
        }

        registrations.RemoveAll(value => string.Equals(value.QualifiedItemId, registration.QualifiedItemId, StringComparison.OrdinalIgnoreCase));
        registrations.Add(registration);
        ScheduleRefresh(1);
        return true;
    }

    public bool UnregisterItems(string ownerModId)
    {
        if (string.IsNullOrWhiteSpace(ownerModId) || !explicitRegistrations.Remove(ownerModId.Trim()))
            return false;

        ScheduleRefresh(1);
        return true;
    }

    private void RefreshNow()
    {
        if (refreshing || !Context.IsWorldReady)
            return;

        refreshing = true;
        try
        {
            bool changed = bridge.RemoveRuntimeAdditions();
            assignments.Clear();
            unclassifiedItems.Clear();

            if (!bridge.IsReady)
                return;

            HashSet<string> explicitlyRegisteredItems = new(StringComparer.OrdinalIgnoreCase);
            foreach (ExplicitRegistration registration in explicitRegistrations.Values.SelectMany(value => value))
            {
                explicitlyRegisteredItems.Add(registration.QualifiedItemId);
                if (!ItemRegistry.Exists(registration.QualifiedItemId))
                {
                    if (config.DebugLogging)
                        monitor.Log($"Explicit registration skipped because '{registration.QualifiedItemId}' does not exist.", LogLevel.Debug);
                    continue;
                }

                bool hasQualities = registration.HasQualities
                    ?? GetDefaultQualitySupport(registration.QualifiedItemId, registration.IsBigCraftable);
                changed |= ApplyItem(
                    registration.QualifiedItemId,
                    registration.ItemId,
                    registration.IsBigCraftable,
                    hasQualities,
                    registration.MinimumBagSize,
                    registration.BagTypeIds
                );
            }

            int candidateCount = 0;
            if (config.EnableAutomaticDiscovery)
            {
                (HashSet<string> cookingIngredientIds, HashSet<int> cookingIngredientCategories) = GetCookingIngredients();
                HashSet<ItemCandidate> candidates = GetModdedCandidates();
                candidateCount = candidates.Count;

                foreach (ItemCandidate candidate in candidates.OrderBy(value => value.QualifiedItemId, StringComparer.OrdinalIgnoreCase))
                {
                    if (explicitlyRegisteredItems.Contains(candidate.QualifiedItemId))
                        continue;

                    ItemClassification classification = ClassificationRules.Classify(
                        candidate,
                        cookingIngredientIds,
                        cookingIngredientCategories
                    );
                    if (classification.BagTypeIds.Count == 0)
                    {
                        unclassifiedItems.Add(candidate.QualifiedItemId);
                        continue;
                    }

                    changed |= ApplyItem(
                        candidate.QualifiedItemId,
                        candidate.ItemId,
                        candidate.IsBigCraftable,
                        classification.HasQualities,
                        config.MinimumBagSize,
                        classification.BagTypeIds
                    );
                }
            }

            if (changed)
                bridge.RefreshLiveBags();

            if (config.DebugLogging)
            {
                monitor.Log(
                    $"Examined {candidateCount} verified mod-owned object(s), assigned {assignments.Count}, and left {unclassifiedItems.Count} unclassified.",
                    LogLevel.Debug
                );
            }
        }
        catch (Exception ex)
        {
            monitor.Log($"Universal item discovery failed.\n{ex}", LogLevel.Error);
        }
        finally
        {
            refreshing = false;
        }
    }

    private bool ApplyItem(
        string qualifiedItemId,
        string itemId,
        bool isBigCraftable,
        bool hasQualities,
        UniversalBagSize minimumBagSize,
        IEnumerable<string> bagTypeIds
    )
    {
        List<string> appliedBagTypeIds = new();
        foreach (string bagTypeId in bagTypeIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (bridge.AddItem(bagTypeId, itemId, isBigCraftable, hasQualities, minimumBagSize))
                appliedBagTypeIds.Add(bagTypeId);
        }

        if (appliedBagTypeIds.Count == 0)
            return false;

        if (assignments.TryGetValue(qualifiedItemId, out IReadOnlyList<string>? existing))
        {
            appliedBagTypeIds = existing
                .Concat(appliedBagTypeIds)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        assignments[qualifiedItemId] = appliedBagTypeIds
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return true;
    }

    private bool GetDefaultQualitySupport(string qualifiedItemId, bool isBigCraftable)
    {
        if (isBigCraftable)
            return false;

        try
        {
            var parsedData = ItemRegistry.GetData(qualifiedItemId);
            return parsedData is not null && ClassificationRules.SupportsQualities(parsedData.Category);
        }
        catch (Exception ex)
        {
            if (config.DebugLogging)
                monitor.Log($"Could not determine quality support for '{qualifiedItemId}': {ex.Message}", LogLevel.Debug);
            return false;
        }
    }

    private HashSet<ItemCandidate> GetModdedCandidates()
    {
        HashSet<ItemCandidate> candidates = new();
        HashSet<string> loadedModIds = helper.ModRegistry.GetAll()
            .Select(mod => mod.Manifest.UniqueID)
            .Where(id => !string.IsNullOrWhiteSpace(id)
                && !id.Equals(ItemBagsModId, StringComparison.OrdinalIgnoreCase)
                && !id.Equals(manifest.UniqueID, StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        HashSet<string> jsonAssetsObjects = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> jsonAssetsBigCraftables = new(StringComparer.OrdinalIgnoreCase);
        AddJsonAssetsIds(loadedModIds, jsonAssetsObjects, jsonAssetsBigCraftables);

        foreach (string itemId in Game1.objectData.Keys)
        {
            if (!jsonAssetsObjects.Contains(itemId) && !HasVerifiedModSource(itemId, loadedModIds))
                continue;

            ItemCandidate? candidate = CreateCandidate(itemId, isBigCraftable: false);
            if (candidate is not null)
                candidates.Add(candidate);
        }

        foreach (string itemId in Game1.bigCraftableData.Keys)
        {
            if (!jsonAssetsBigCraftables.Contains(itemId) && !HasVerifiedModSource(itemId, loadedModIds))
                continue;

            ItemCandidate? candidate = CreateCandidate(itemId, isBigCraftable: true);
            if (candidate is not null)
                candidates.Add(candidate);
        }

        return candidates;
    }

    private ItemCandidate? CreateCandidate(string itemId, bool isBigCraftable)
    {
        string qualifiedItemId = $"{(isBigCraftable ? "(BC)" : "(O)")}{itemId}";
        try
        {
            var parsedData = ItemRegistry.GetData(qualifiedItemId);
            if (parsedData is null)
                return null;

            Item item = ItemRegistry.Create(qualifiedItemId);
            HashSet<string> contextTags = item.GetContextTags();
            return new ItemCandidate(
                itemId,
                isBigCraftable,
                parsedData.Category,
                parsedData.ObjectType ?? string.Empty,
                contextTags
            );
        }
        catch (Exception ex)
        {
            if (config.DebugLogging)
                monitor.Log($"Could not inspect '{qualifiedItemId}': {ex.Message}", LogLevel.Debug);
            return null;
        }
    }

    private void AddJsonAssetsIds(
        IEnumerable<string> loadedModIds,
        ISet<string> objectIds,
        ISet<string> bigCraftableIds
    )
    {
        if (jsonAssetsApi is null)
            return;

        foreach (string modId in loadedModIds)
        {
            try
            {
                foreach (string name in jsonAssetsApi.GetAllObjectsFromContentPack(modId) ?? new List<string>())
                {
                    string id = NormalizeLocalId(jsonAssetsApi.GetObjectId(name), "(O)");
                    if (!string.IsNullOrWhiteSpace(id) && Game1.objectData.ContainsKey(id))
                        objectIds.Add(id);
                }

                foreach (string name in jsonAssetsApi.GetAllBigCraftablesFromContentPack(modId) ?? new List<string>())
                {
                    string id = NormalizeLocalId(jsonAssetsApi.GetBigCraftableId(name), "(BC)");
                    if (!string.IsNullOrWhiteSpace(id) && Game1.bigCraftableData.ContainsKey(id))
                        bigCraftableIds.Add(id);
                }
            }
            catch (Exception ex)
            {
                if (config.DebugLogging)
                    monitor.Log($"Could not enumerate Json Assets items for '{modId}': {ex.Message}", LogLevel.Debug);
            }
        }
    }

    private (HashSet<string> ItemIds, HashSet<int> Categories) GetCookingIngredients()
    {
        HashSet<string> itemIds = new(StringComparer.OrdinalIgnoreCase);
        HashSet<int> categories = new();

        foreach (string recipeName in CraftingRecipe.cookingRecipes.Keys)
        {
            try
            {
                CraftingRecipe recipe = new(recipeName, true);
                foreach (string ingredientId in recipe.recipeList.Keys)
                {
                    if (int.TryParse(ingredientId, out int numericId) && numericId < 0)
                        categories.Add(numericId);
                    else
                        itemIds.Add(NormalizeLocalId(ingredientId, "(O)"));
                }
            }
            catch (Exception ex)
            {
                if (config.DebugLogging)
                    monitor.Log($"Could not inspect cooking recipe '{recipeName}': {ex.Message}", LogLevel.Debug);
            }
        }

        return (itemIds, categories);
    }

    private static bool HasVerifiedModSource(string itemId, IEnumerable<string> loadedModIds)
    {
        foreach (string modId in loadedModIds)
        {
            if (HasIdentifierPrefix(itemId, modId))
                return true;
        }

        return false;
    }

    private static bool HasIdentifierPrefix(string? value, string prefix)
    {
        if (string.IsNullOrWhiteSpace(value)
            || string.IsNullOrWhiteSpace(prefix)
            || !value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (value.Length == prefix.Length)
            return true;

        char boundary = value[prefix.Length];
        return boundary is '.' or '_' or ':' or '/' or '\\';
    }

    private static string NormalizeLocalId(string? itemId, string qualifier)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return string.Empty;

        return itemId.StartsWith(qualifier, StringComparison.OrdinalIgnoreCase)
            ? itemId.Substring(qualifier.Length)
            : itemId;
    }

    private static bool TryParseQualifiedObjectId(string? qualifiedItemId, out string itemId, out bool isBigCraftable)
    {
        itemId = string.Empty;
        isBigCraftable = false;
        if (string.IsNullOrWhiteSpace(qualifiedItemId))
            return false;

        string value = qualifiedItemId.Trim();
        if (value.StartsWith("(O)", StringComparison.OrdinalIgnoreCase))
        {
            itemId = value.Substring(3);
            return !string.IsNullOrWhiteSpace(itemId);
        }

        if (value.StartsWith("(BC)", StringComparison.OrdinalIgnoreCase))
        {
            itemId = value.Substring(4);
            isBigCraftable = true;
            return !string.IsNullOrWhiteSpace(itemId);
        }

        return false;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        if (!helper.ModRegistry.IsLoaded(JsonAssetsModId))
            return;

        jsonAssetsApi = helper.ModRegistry.GetApi<IJsonAssetsApi>(JsonAssetsModId);
        if (jsonAssetsApi is not null)
            jsonAssetsApi.ItemsRegistered += OnJsonAssetsItemsRegistered;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        ScheduleRefresh(2);
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        refreshDelay = -1;
        bridge.RemoveRuntimeAdditions();
        assignments.Clear();
        unclassifiedItems.Clear();
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (refreshDelay < 0)
            return;

        refreshDelay--;
        if (refreshDelay == 0)
        {
            refreshDelay = -1;
            RefreshNow();
        }
    }

    private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (e.NamesWithoutLocale.Any(name => RelevantAssets.Any(asset => name.IsEquivalentTo(asset))))
            ScheduleRefresh(2);
    }

    private void OnJsonAssetsItemsRegistered(object? sender, EventArgs e)
    {
        if (Context.IsWorldReady)
            ScheduleRefresh(2);
    }
}
