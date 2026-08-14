using StardewModdingAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ThaleTheGreat.UniversalItemBags.Integration;

internal sealed class ItemBagsBridge
{
    private const string ItemBagsAssemblyName = "ItemBags";

    private readonly IMonitor monitor;
    private readonly List<RuntimeAddition> runtimeAdditions = new();

    private PropertyInfo? bagConfigProperty;
    private PropertyInfo? indexedBagTypesProperty;
    private PropertyInfo? bagTypeSizeSettingsProperty;
    private PropertyInfo? sizeConfigSizeProperty;
    private PropertyInfo? sizeConfigItemsProperty;
    private Type? storeableBagItemType;
    private PropertyInfo? storeableItemIdProperty;
    private PropertyInfo? storeableItemHasQualitiesProperty;
    private PropertyInfo? storeableItemQualitiesProperty;
    private PropertyInfo? storeableItemIsBigCraftableProperty;
    private Type? boundedBagType;
    private MethodInfo? getAllBagsMethod;
    private MethodInfo? getBagTypeIdMethod;
    private MethodInfo? refreshBagMethod;
    private bool bindingFailed;

    public ItemBagsBridge(IMonitor monitor)
    {
        this.monitor = monitor;
    }

    public bool IsReady => TryBind() && GetBagConfig() is not null;

    public bool AddItem(
        string bagTypeId,
        string itemId,
        bool isBigCraftable,
        bool hasQualities,
        UniversalBagSize minimumBagSize
    )
    {
        if (!TryGetBagType(bagTypeId, out object? bagType) || bagType is null)
            return false;

        bool added = false;
        foreach (object sizeConfig in GetSizeSettings(bagType))
        {
            int size = Convert.ToInt32(sizeConfigSizeProperty!.GetValue(sizeConfig));
            if (size < (int)minimumBagSize)
                continue;

            if (sizeConfigItemsProperty!.GetValue(sizeConfig) is not IList items
                || ContainsItem(items, itemId, isBigCraftable))
            {
                continue;
            }

            object item = Activator.CreateInstance(storeableBagItemType!)
                ?? throw new InvalidOperationException("Item Bags returned a null StoreableBagItem instance.");

            storeableItemIdProperty!.SetValue(item, itemId);
            storeableItemHasQualitiesProperty!.SetValue(item, hasQualities);
            storeableItemQualitiesProperty!.SetValue(item, null);
            storeableItemIsBigCraftableProperty!.SetValue(item, isBigCraftable);

            items.Add(item);
            runtimeAdditions.Add(new RuntimeAddition(items, item));
            added = true;
        }

        return added;
    }

    public bool RemoveRuntimeAdditions()
    {
        bool removed = false;
        foreach (RuntimeAddition addition in runtimeAdditions)
        {
            if (!addition.Items.Contains(addition.Item))
                continue;

            addition.Items.Remove(addition.Item);
            removed = true;
        }

        runtimeAdditions.Clear();
        return removed;
    }

    public void RefreshLiveBags()
    {
        if (!TryBind())
            return;

        object? result = getAllBagsMethod!.Invoke(null, new object[] { true });
        if (result is not IEnumerable bags)
            return;

        foreach (object? bag in bags)
        {
            if (bag is null || !boundedBagType!.IsInstanceOfType(bag))
                continue;

            string? typeId = getBagTypeIdMethod!.Invoke(bag, Array.Empty<object>()) as string;
            if (string.IsNullOrWhiteSpace(typeId) || !StandardBagTypeIds.All.Contains(typeId))
                continue;

            refreshBagMethod!.Invoke(bag, Array.Empty<object>());
        }
    }

    private bool ContainsItem(IEnumerable items, string itemId, bool isBigCraftable)
    {
        foreach (object? item in items)
        {
            if (item is null)
                continue;

            string? existingId = storeableItemIdProperty!.GetValue(item) as string;
            bool existingBigCraftable = Convert.ToBoolean(storeableItemIsBigCraftableProperty!.GetValue(item));
            if (existingBigCraftable == isBigCraftable
                && string.Equals(existingId, itemId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerable<object> GetSizeSettings(object bagType)
    {
        object? value = bagTypeSizeSettingsProperty!.GetValue(bagType);
        return value is IEnumerable values
            ? values.Cast<object>()
            : Enumerable.Empty<object>();
    }

    private bool TryGetBagType(string bagTypeId, out object? bagType)
    {
        bagType = null;
        object? bagConfig = GetBagConfig();
        if (bagConfig is null)
            return false;

        if (indexedBagTypesProperty!.GetValue(bagConfig) is not IDictionary indexedBagTypes
            || !indexedBagTypes.Contains(bagTypeId))
        {
            return false;
        }

        bagType = indexedBagTypes[bagTypeId];
        return bagType is not null;
    }

    private object? GetBagConfig()
    {
        return TryBind() ? bagConfigProperty!.GetValue(null) : null;
    }

    private bool TryBind()
    {
        if (bagConfigProperty is not null)
            return true;
        if (bindingFailed)
            return false;

        try
        {
            Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(value => string.Equals(value.GetName().Name, ItemBagsAssemblyName, StringComparison.OrdinalIgnoreCase));
            if (assembly is null)
                return false;

            Type itemBagsModType = RequireType(assembly, "ItemBags.ItemBagsMod");
            Type bagConfigType = RequireType(assembly, "ItemBags.Persistence.BagConfig");
            Type bagTypeType = RequireType(assembly, "ItemBags.Persistence.BagType");
            Type bagSizeConfigType = RequireType(assembly, "ItemBags.Persistence.BagSizeConfig");
            storeableBagItemType = RequireType(assembly, "ItemBags.Persistence.StoreableBagItem");
            Type itemBagType = RequireType(assembly, "ItemBags.Bags.ItemBag");
            boundedBagType = RequireType(assembly, "ItemBags.Bags.BoundedBag");

            bagConfigProperty = RequireProperty(itemBagsModType, "BagConfig", BindingFlags.Public | BindingFlags.Static);
            indexedBagTypesProperty = RequireProperty(bagConfigType, "IndexedBagTypes", BindingFlags.Public | BindingFlags.Instance);
            bagTypeSizeSettingsProperty = RequireProperty(bagTypeType, "SizeSettings", BindingFlags.Public | BindingFlags.Instance);
            sizeConfigSizeProperty = RequireProperty(bagSizeConfigType, "Size", BindingFlags.Public | BindingFlags.Instance);
            sizeConfigItemsProperty = RequireProperty(bagSizeConfigType, "Items", BindingFlags.Public | BindingFlags.Instance);
            storeableItemIdProperty = RequireProperty(storeableBagItemType, "Id", BindingFlags.Public | BindingFlags.Instance);
            storeableItemHasQualitiesProperty = RequireProperty(storeableBagItemType, "HasQualities", BindingFlags.Public | BindingFlags.Instance);
            storeableItemQualitiesProperty = RequireProperty(storeableBagItemType, "Qualities", BindingFlags.Public | BindingFlags.Instance);
            storeableItemIsBigCraftableProperty = RequireProperty(storeableBagItemType, "IsBigCraftable", BindingFlags.Public | BindingFlags.Instance);

            getAllBagsMethod = itemBagType.GetMethod(
                "GetAllBags",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(bool) },
                modifiers: null
            ) ?? throw new MissingMethodException(itemBagType.FullName, "GetAllBags(bool)");

            getBagTypeIdMethod = boundedBagType.GetMethod("GetTypeId", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException(boundedBagType.FullName, "GetTypeId()");
            refreshBagMethod = boundedBagType.GetMethod("OnModdedBagItemsUpdated", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingMethodException(boundedBagType.FullName, "OnModdedBagItemsUpdated()");

            return true;
        }
        catch (Exception ex)
        {
            bindingFailed = true;
            monitor.Log($"Could not bind to Item Bags' runtime model. Universal item discovery is disabled.\n{ex}", LogLevel.Error);
            return false;
        }
    }

    private static Type RequireType(Assembly assembly, string fullName)
    {
        return assembly.GetType(fullName, throwOnError: false, ignoreCase: false)
            ?? throw new TypeLoadException($"Required Item Bags type '{fullName}' was not found.");
    }

    private static PropertyInfo RequireProperty(Type type, string name, BindingFlags flags)
    {
        return type.GetProperty(name, flags)
            ?? throw new MissingMemberException(type.FullName, name);
    }

    private sealed class RuntimeAddition
    {
        public RuntimeAddition(IList items, object item)
        {
            Items = items;
            Item = item;
        }

        public IList Items { get; }

        public object Item { get; }
    }
}
