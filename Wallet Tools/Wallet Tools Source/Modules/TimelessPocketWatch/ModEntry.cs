using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.Powers;
using StardewValley.Tools;
using Object = StardewValley.Object;

using ThaleMagnus.WalletTools;

namespace ThaleMagnus.WalletToolsForTimelessPocketWatch;

internal sealed class TimelessPocketWatchModule : WalletModule
{
    internal const string ModuleKey = "TimelessPocketWatch";

    private const string RequiredModId = "PeacefulEnd.TimelessPocketWatch";
    private const string PocketWatchToolId = "PeacefulEnd.PocketWatch_Tools_PocketWatch";
    private const string PocketWatchQualifiedToolId = "(T)PeacefulEnd.PocketWatch_Tools_PocketWatch";
    private const string PocketWatchTypeMarker = "PeacefulEnd.PocketWatch.IsActive";
    private const string GoldGearId = "PeacefulEnd.PocketWatch_Objects_Clockwork_Gear_Gold";
    private const string IridiumGearId = "PeacefulEnd.PocketWatch_Objects_Clockwork_Gear_Iridium";
    private const string RadioactiveGearId = "PeacefulEnd.PocketWatch_Objects_Clockwork_Gear_Radioactive";
    private const string PocketWatchTexturePath = "PocketWatch/Textures/Tools/PocketWatch";

    private const string WalletFlagKey = "ThaleMagnus.WalletToolsForTimelessPocketWatch/HasPocketWatch";
    private const string WalletPowerId = "ThaleMagnus.WalletTools_TimelessPocketWatch";
    private const string OvernightExposureMarker = "ThaleMagnus.WalletToolsForTimelessPocketWatch/OvernightExposure";
    private const string OwnerPlayerIdMarker = "ThaleMagnus.WalletToolsForTimelessPocketWatch/OwnerPlayerId";

    private static TimelessPocketWatchModule? Instance;

    private readonly Dictionary<long, Tool> StoredWatchByPlayer = new();
    private readonly Dictionary<long, string> LastPowerStateByPlayer = new();
    private readonly PerScreen<bool> SuppressInventoryConversionScreen = new();
    private readonly PerScreen<bool> PendingInventoryConversionScreen = new();

    private ModConfig Config = new();
    private Harmony Harmony = null!;
    private bool GmcmRegistered;
    private bool BridgeWarningLogged;
    private object? PocketWatchToolManager;
    private MethodInfo? GetPocketWatchMethod;
    private PropertyInfo? PocketWatchIsActiveProperty;
    private MethodInfo? PocketWatchDeactivateMethod;

    private bool SuppressInventoryConversion
    {
        get => SuppressInventoryConversionScreen.Value;
        set => SuppressInventoryConversionScreen.Value = value;
    }

    private bool PendingInventoryConversion
    {
        get => PendingInventoryConversionScreen.Value;
        set => PendingInventoryConversionScreen.Value = value;
    }

    internal TimelessPocketWatchModule(ModEntry host)
        : base(host, ModuleKey, "module.timeless-pocket-watch.name", string.Empty, RequiredModId)
    {
    }

    internal override void Initialize()
    {
        Instance = this;
        Config = Host.ReadModuleConfig<ModConfig>(ModuleKey);

        Helper.Events.Content.AssetRequested += OnAssetRequested;
        Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        Helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        Helper.Events.GameLoop.DayStarted += OnDayStarted;
        Helper.Events.GameLoop.DayEnding += OnDayEnding;
        Helper.Events.GameLoop.Saving += OnSaving;
        Helper.Events.GameLoop.Saved += OnSaved;
        Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        Helper.Events.Player.InventoryChanged += OnInventoryChanged;
        Helper.Events.Input.ButtonPressed += OnButtonPressed;

        Harmony = new Harmony($"{ModManifest.UniqueID}.TimelessPocketWatch");
        PatchItemExistenceCheck();
    }

    internal override void OnGameLaunched()
    {
        ResolvePocketWatchBridge();
        RegisterGmcm();
    }

    private void PatchItemExistenceCheck()
    {
        MethodInfo? target = AccessTools.Method(typeof(Utility), nameof(Utility.doesItemExistAnywhere), new[] { typeof(string) });
        MethodInfo? postfix = AccessTools.Method(typeof(DoesItemExistAnywherePatch), nameof(DoesItemExistAnywherePatch.Postfix));
        if (target is not null && postfix is not null)
            Harmony.Patch(target, postfix: new HarmonyMethod(postfix));
    }

    private void RegisterGmcm()
    {
        if (GmcmRegistered)
            return;

        IGenericModConfigMenuApi? gmcm = Host.GetDirectModuleGmcmApi(ModuleKey);
        if (gmcm is null)
            return;

        try
        {
            Host.RegisterModuleConfigCallbacks(ModuleKey, ResetConfig, SaveConfig);
            gmcm.AddPage(ModManifest, Host.GetModulePageId(ModuleKey), () => Host.Translate("module.timeless-pocket-watch.name"));
            gmcm.AddSectionTitle(ModManifest, () => Host.Translate("config.timeless-pocket-watch.section"));
            gmcm.AddBoolOption(ModManifest, () => Config.Enabled, SetEnabled, () => Host.Translate("config.timeless-pocket-watch.enabled.name"), () => Host.Translate("config.timeless-pocket-watch.enabled.tooltip"), $"{ModuleKey}.Enabled");
            gmcm.AddBoolOption(ModManifest, () => Config.AutoStoreFromInventory, SetAutoStoreFromInventory, () => Host.Translate("config.timeless-pocket-watch.auto-store.name"), () => Host.Translate("config.timeless-pocket-watch.auto-store.tooltip"), $"{ModuleKey}.AutoStoreFromInventory");
            gmcm.AddBoolOption(ModManifest, () => Config.ShowWalletPower, SetShowWalletPower, () => Host.Translate("config.timeless-pocket-watch.power.name"), () => Host.Translate("config.timeless-pocket-watch.power.tooltip"), $"{ModuleKey}.ShowWalletPower");
            gmcm.AddBoolOption(ModManifest, () => Config.PlayToolSwapSound, value => Config.PlayToolSwapSound = value, () => Host.Translate("config.timeless-pocket-watch.swap-sound.name"), () => Host.Translate("config.timeless-pocket-watch.swap-sound.tooltip"), $"{ModuleKey}.PlayToolSwapSound");
            gmcm.AddBoolOption(ModManifest, () => Config.ShowStoredMessage, value => Config.ShowStoredMessage = value, () => Host.Translate("config.timeless-pocket-watch.stored-message.name"), () => Host.Translate("config.timeless-pocket-watch.stored-message.tooltip"), $"{ModuleKey}.ShowStoredMessage");
            gmcm.AddBoolOption(ModManifest, () => Config.ShowGearMessage, value => Config.ShowGearMessage = value, () => Host.Translate("config.timeless-pocket-watch.gear-message.name"), () => Host.Translate("config.timeless-pocket-watch.gear-message.tooltip"), $"{ModuleKey}.ShowGearMessage");
            gmcm.AddBoolOption(ModManifest, () => Config.ShowMissingMessage, value => Config.ShowMissingMessage = value, () => Host.Translate("config.timeless-pocket-watch.missing-message.name"), () => Host.Translate("config.timeless-pocket-watch.missing-message.tooltip"), $"{ModuleKey}.ShowMissingMessage");
            gmcm.AddKeybindList(ModManifest, () => Config.UsePocketWatchHotkey, value => Config.UsePocketWatchHotkey = value, () => Host.Translate("config.timeless-pocket-watch.use.name"), () => Host.Translate("config.timeless-pocket-watch.use.tooltip"), $"{ModuleKey}.UsePocketWatchHotkey");
            GmcmRegistered = true;
        }
        catch (Exception ex)
        {
            Monitor.Log($"Wallet Tools could not register the Timeless Pocket Watch configuration page: {ex}", LogLevel.Error);
        }
    }

    private void ResetConfig()
    {
        Config = new ModConfig();
        ApplyConfigState(showReturnMessage: false);
    }

    private void SaveConfig()
    {
        Host.WriteModuleConfig(ModuleKey, Config);
        ApplyConfigState(showReturnMessage: false);
    }

    private void SetEnabled(bool value)
    {
        if (Config.Enabled == value)
            return;

        Config.Enabled = value;
        ApplyConfigState(showReturnMessage: !value);
    }

    private void SetAutoStoreFromInventory(bool value)
    {
        if (Config.AutoStoreFromInventory == value)
            return;

        Config.AutoStoreFromInventory = value;
        if (!value)
        {
            PendingInventoryConversion = false;
            return;
        }

        if (Config.Enabled && Context.IsWorldReady)
        {
            ConvertInventoryWatches(Game1.player);
            UpdatePendingConversionState(Game1.player);
        }
    }

    private void SetShowWalletPower(bool value)
    {
        Config.ShowWalletPower = value;
        InvalidatePowers();
    }

    private void ApplyConfigState(bool showReturnMessage)
    {
        if (!Context.IsWorldReady)
            return;

        Farmer player = Game1.player;
        if (!Config.Enabled)
        {
            ReturnStoredWatchToInventory(player, useOverflowMenu: true, showReturnMessage);
            ClearWalletMarkersFromInventory(player, keepOwnedOvernightExposure: false);
        }
        else
        {
            CollectOvernightExposedWatch(player);
            CollectLostAndFoundWatch(player);
            if (Config.AutoStoreFromInventory)
                ConvertInventoryWatches(player);
        }

        UpdatePendingConversionState(player);
        UpdateWalletFlag(player);
        InvalidatePowers();
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Powers"))
            return;

        e.Edit(asset =>
        {
            IDictionary<string, PowersData> powers = asset.AsDictionary<string, PowersData>().Data;
            Tool? watch = Context.IsWorldReady ? GetStoredWatch(Game1.player) : null;
            if (!Context.IsWorldReady || !Config.Enabled || !Config.ShowWalletPower || watch is null)
            {
                powers.Remove(WalletPowerId);
                return;
            }

            powers[WalletPowerId] = new PowersData
            {
                DisplayName = GetWatchDisplayName(watch),
                Description = GetWatchDescription(watch),
                TexturePath = PocketWatchTexturePath,
                TexturePosition = Point.Zero,
                UnlockedCondition = $"PLAYER_MOD_DATA Current {WalletFlagKey} true",
                CustomFields = Host.GetWalletPowerCustomFields()
            };
        });
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        ClearVolatileState(invalidatePowers: false);
        ClearWalletMarkersFromInventory(Game1.player, keepOwnedOvernightExposure: Config.Enabled);

        if (Config.Enabled)
        {
            CollectOvernightExposedWatch(Game1.player);
            CollectLostAndFoundWatch(Game1.player);
            if (Config.AutoStoreFromInventory)
                ConvertInventoryWatches(Game1.player);
        }

        UpdatePendingConversionState(Game1.player);
        UpdateWalletFlag(Game1.player);
        InvalidatePowers();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        foreach (Tool watch in StoredWatchByPlayer.Values.Distinct().ToArray())
            TryDeactivateWatch(watch);

        ClearVolatileState();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!Config.Enabled)
            return;

        CollectOvernightExposedWatch(Game1.player);
        CollectLostAndFoundWatch(Game1.player);
        if (Config.AutoStoreFromInventory)
            ConvertInventoryWatches(Game1.player);
        UpdatePendingConversionState(Game1.player);
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        PendingInventoryConversion = false;
        ExposeStoredWatchForSave(Game1.player);
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        PendingInventoryConversion = false;
        ExposeStoredWatchForSave(Game1.player);
    }

    private void OnSaved(object? sender, SavedEventArgs e)
    {
        if (!Config.Enabled)
        {
            ClearWalletMarkersFromInventory(Game1.player, keepOwnedOvernightExposure: false);
            UpdateWalletFlag(Game1.player);
            InvalidatePowers();
            return;
        }

        CollectOvernightExposedWatch(Game1.player);
        if (Config.AutoStoreFromInventory)
            ConvertInventoryWatches(Game1.player);
        UpdatePendingConversionState(Game1.player);
    }

    private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
    {
        if (SuppressInventoryConversion
            || !Context.IsWorldReady
            || !Config.Enabled
            || !Config.AutoStoreFromInventory
            || e.Player.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
        {
            return;
        }

        if (!CanConvertInventoryWatches(e.Player))
        {
            PendingInventoryConversion = true;
            return;
        }

        ConvertInventoryWatches(e.Player);
        UpdatePendingConversionState(e.Player);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || !Config.Enabled || Game1.fadeToBlack)
            return;

        if (TryLoadClickedGearIntoStoredWatch(e))
            return;

        if (Game1.activeClickableMenu is not null || !Context.CanPlayerMove || !Config.UsePocketWatchHotkey.JustPressed())
            return;

        Helper.Input.Suppress(e.Button);
        Tool? watch = GetStoredWatch(Game1.player);
        if (watch is null)
        {
            if (Config.ShowMissingMessage)
                Game1.addHUDMessage(new HUDMessage(Host.Translate("hud.timeless-pocket-watch-missing"), HUDMessage.error_type));
            return;
        }

        ToggleStoredWatch(Game1.player, watch);
    }

    private bool TryLoadClickedGearIntoStoredWatch(ButtonPressedEventArgs e)
    {
        if (e.Button != SButton.MouseRight
            || Game1.activeClickableMenu is null
            || (!Helper.Input.IsDown(SButton.LeftShift) && !Helper.Input.IsDown(SButton.RightShift))
            || !TryGetClickedPlayerInventoryGear(Game1.player, Game1.getMouseX(), Game1.getMouseY(), out int slot, out Object gear))
        {
            return false;
        }

        Tool? watch = GetStoredWatch(Game1.player);
        if (watch is null)
        {
            if (Config.ShowMissingMessage)
                Game1.addHUDMessage(new HUDMessage(Host.Translate("hud.timeless-pocket-watch-gear-missing-watch"), HUDMessage.error_type));
            return false;
        }

        Helper.Input.Suppress(e.Button);
        bool? active = IsWatchActive(watch);
        if (!active.HasValue)
        {
            Game1.addHUDMessage(new HUDMessage(Host.Translate("hud.timeless-pocket-watch-gear-state-unknown"), HUDMessage.error_type));
            return true;
        }

        if (active.Value)
        {
            if (!TryDeactivateWatch(watch))
            {
                Game1.addHUDMessage(new HUDMessage(Host.Translate("hud.timeless-pocket-watch-gear-deactivation-failed"), HUDMessage.error_type));
                return true;
            }

            RefreshPowerIfWatchStateChanged(Game1.player, watch, force: true);
        }

        if (watch.attachments is null)
        {
            Monitor.Log("Wallet Tools found a stored Timeless Pocket Watch without an attachment collection. Gear loading was cancelled.", LogLevel.Error);
            Game1.addHUDMessage(new HUDMessage(Host.Translate("hud.timeless-pocket-watch-gear-attachment-unavailable"), HUDMessage.error_type));
            return true;
        }

        Object? attached = GetAttachedGear(watch);
        SuppressInventoryConversion = true;
        try
        {
            if (attached is not null && string.Equals(attached.QualifiedItemId, gear.QualifiedItemId, StringComparison.OrdinalIgnoreCase))
            {
                int maximum = Math.Max(attached.Stack, attached.maximumStackSize());
                int moved = Math.Min(gear.Stack, Math.Max(0, maximum - attached.Stack));
                if (moved <= 0)
                {
                    if (Config.ShowGearMessage)
                        Game1.addHUDMessage(new HUDMessage(Host.Translate("hud.timeless-pocket-watch-gear-full", new { gearName = attached.DisplayName, count = attached.Stack }), HUDMessage.error_type));
                    return true;
                }

                attached.Stack += moved;
                gear.Stack -= moved;
                if (gear.Stack <= 0)
                    Game1.player.Items[slot] = null;

                if (Config.ShowGearMessage)
                {
                    Game1.addHUDMessage(new HUDMessage(Host.Translate("hud.timeless-pocket-watch-gear-added", new
                    {
                        gearName = attached.DisplayName,
                        added = moved,
                        count = attached.Stack
                    }), HUDMessage.newQuest_type));
                }
            }
            else
            {
                watch.attachments[0] = gear;
                Game1.player.Items[slot] = attached;

                if (Config.ShowGearMessage)
                {
                    string message = attached is null
                        ? Host.Translate("hud.timeless-pocket-watch-gear-loaded", new { gearName = gear.DisplayName, count = gear.Stack })
                        : Host.Translate("hud.timeless-pocket-watch-gear-replaced", new
                        {
                            oldGearName = attached.DisplayName,
                            oldCount = attached.Stack,
                            newGearName = gear.DisplayName,
                            newCount = gear.Stack
                        });
                    Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type));
                }
            }
        }
        finally
        {
            SuppressInventoryConversion = false;
        }

        NormalizePlayerItemsAfterCleanup(Game1.player);
        RefreshPowerIfWatchStateChanged(Game1.player, watch, force: true);
        return true;
    }

    private static bool TryGetClickedPlayerInventoryGear(Farmer player, int x, int y, out int slot, out Object gear)
    {
        slot = -1;
        gear = null!;
        if (Game1.activeClickableMenu is null)
            return false;

        foreach (object inventoryMenu in EnumerateVisibleInventoryMenus(Game1.activeClickableMenu))
        {
            if (!UsesPlayerInventory(inventoryMenu, player))
                continue;

            MethodInfo? getItemAt = AccessTools.Method(inventoryMenu.GetType(), "getItemAt", new[] { typeof(int), typeof(int) });
            if (getItemAt is null)
                continue;

            Object? candidate;
            try
            {
                candidate = getItemAt.Invoke(inventoryMenu, new object[] { x, y }) as Object;
            }
            catch
            {
                continue;
            }

            if (candidate is null || !IsClockworkGear(candidate))
                continue;

            for (int index = 0; index < player.Items.Count; index++)
            {
                if (!ReferenceEquals(player.Items[index], candidate))
                    continue;

                slot = index;
                gear = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool UsesPlayerInventory(object inventoryMenu, Farmer player)
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (MemberInfo member in inventoryMenu.GetType().GetMembers(flags))
        {
            if ((member.MemberType != MemberTypes.Field && member.MemberType != MemberTypes.Property)
                || !member.Name.Equals("actualInventory", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return ReferenceEquals(GetMemberValue(member, inventoryMenu), player.Items);
        }

        return false;
    }

    private static IEnumerable<object> EnumerateVisibleInventoryMenus(object activeMenu)
    {
        List<object> owners = new() { activeMenu };
        MethodInfo? getCurrentPage = AccessTools.Method(activeMenu.GetType(), "GetCurrentPage", Type.EmptyTypes);
        if (getCurrentPage is not null)
        {
            try
            {
                object? currentPage = getCurrentPage.Invoke(activeMenu, null);
                if (currentPage is not null && !owners.Any(owner => ReferenceEquals(owner, currentPage)))
                    owners.Add(currentPage);
            }
            catch
            {
            }
        }

        PropertyInfo? currentPageProperty = AccessTools.Property(activeMenu.GetType(), "CurrentPage");
        if (currentPageProperty is not null)
        {
            try
            {
                object? currentPage = currentPageProperty.GetValue(activeMenu);
                if (currentPage is not null && !owners.Any(owner => ReferenceEquals(owner, currentPage)))
                    owners.Add(currentPage);
            }
            catch
            {
            }
        }

        List<object> inventories = new();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (object owner in owners)
        {
            if (owner.GetType().Name.Equals("InventoryMenu", StringComparison.OrdinalIgnoreCase))
                AddDistinctInventory(inventories, owner);

            foreach (MemberInfo member in owner.GetType().GetMembers(flags))
            {
                if (member.MemberType != MemberTypes.Field && member.MemberType != MemberTypes.Property)
                    continue;

                object? value = GetMemberValue(member, owner);
                if (value is not null && value.GetType().Name.Equals("InventoryMenu", StringComparison.OrdinalIgnoreCase))
                    AddDistinctInventory(inventories, value);
            }
        }

        return inventories;
    }

    private static void AddDistinctInventory(List<object> inventories, object candidate)
    {
        if (!inventories.Any(existing => ReferenceEquals(existing, candidate)))
            inventories.Add(candidate);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        Farmer player = Game1.player;
        Tool? watch = GetStoredWatch(player);
        if (watch is not null)
        {
            try
            {
                watch.tickUpdate(Game1.currentGameTime, player);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Wallet Tools could not update the stored Timeless Pocket Watch and returned it to the inventory: {ex}", LogLevel.Error);
                TryDeactivateWatch(watch);
                ReturnStoredWatchToInventory(player, useOverflowMenu: true, showMessage: false);
                return;
            }

            RefreshPowerIfWatchStateChanged(player, watch);
        }

        if (PendingInventoryConversion && CanConvertInventoryWatches(player))
        {
            ConvertInventoryWatches(player);
            UpdatePendingConversionState(player);
        }

        if (Config.Enabled && Config.AutoStoreFromInventory && e.IsMultipleOf(60))
        {
            ConvertInventoryWatches(player);
            UpdatePendingConversionState(player);
        }

        if (Config.Enabled && e.IsMultipleOf(300))
            CollectLostAndFoundWatch(player);
    }

    private bool CanConvertInventoryWatches(Farmer player)
    {
        return Config.Enabled
            && Config.AutoStoreFromInventory
            && !player.UsingTool;
    }

    private bool ConvertInventoryWatches(Farmer player)
    {
        if (!CanConvertInventoryWatches(player))
            return false;

        int candidateIndex = FindBestInventoryWatchIndex(player);
        if (candidateIndex < 0 || player.Items[candidateIndex] is not Tool candidate)
            return false;

        Tool? stored = GetStoredWatch(player);
        if (stored is not null && CompareWatchPower(candidate, stored) <= 0)
            return false;

        if (stored is not null && !TryDeactivateWatch(stored))
        {
            Monitor.Log("Wallet Tools did not replace the stored Timeless Pocket Watch because its active time-stop effect could not be safely ended.", LogLevel.Error);
            return false;
        }

        SuppressInventoryConversion = true;
        try
        {
            ClearWalletMarkers(candidate);
            if (stored is null)
            {
                player.Items[candidateIndex] = null;
            }
            else
            {
                ClearWalletMarkers(stored);
                player.Items[candidateIndex] = stored;
            }

            SetStoredWatch(player, candidate);
        }
        finally
        {
            SuppressInventoryConversion = false;
        }

        NormalizePlayerItemsAfterCleanup(player);
        RefreshWalletState(player);
        if (Config.ShowStoredMessage)
            Game1.addHUDMessage(new HUDMessage(Host.Translate("hud.timeless-pocket-watch-stored"), HUDMessage.newQuest_type));
        return true;
    }

    private int FindBestInventoryWatchIndex(Farmer player)
    {
        int bestIndex = -1;
        Tool? bestWatch = null;
        for (int index = 0; index < player.Items.Count; index++)
        {
            if (player.Items[index] is not Tool watch
                || !IsPocketWatch(watch)
                || IsOvernightExposure(watch)
                || IsCurrentlySelectedItem(player, index, watch))
            {
                continue;
            }

            if (bestWatch is null || CompareWatchPower(watch, bestWatch) > 0)
            {
                bestIndex = index;
                bestWatch = watch;
            }
        }

        return bestIndex;
    }

    private void UpdatePendingConversionState(Farmer player)
    {
        if (!Config.Enabled || !Config.AutoStoreFromInventory)
        {
            PendingInventoryConversion = false;
            return;
        }

        Tool? stored = GetStoredWatch(player);
        PendingInventoryConversion = player.Items.Any(item =>
            item is Tool watch
            && IsPocketWatch(watch)
            && !IsOvernightExposure(watch)
            && ReferenceEquals(player.CurrentItem, watch)
            && (stored is null || CompareWatchPower(watch, stored) > 0));
    }

    private void ToggleStoredWatch(Farmer player, Tool watch)
    {
        bool? wasActive = IsWatchActive(watch);
        if (!TryRunRealWatchUsePath(player, watch))
            return;

        bool? isActive = IsWatchActive(watch);
        if (Config.PlayToolSwapSound && wasActive.HasValue && isActive.HasValue && wasActive.Value != isActive.Value)
            Game1.playSound("toolSwap");

        RefreshPowerIfWatchStateChanged(player, watch, force: true);
        if (wasActive == false && isActive == false && GetAttachedGear(watch) is null)
            Game1.addHUDMessage(new HUDMessage(Host.Translate("hud.timeless-pocket-watch-empty"), HUDMessage.error_type));
    }

    private bool TryRunRealWatchUsePath(Farmer player, Tool watch)
    {
        try
        {
            Vector2 toolLocation = player.GetToolLocation(false);
            watch.beginUsing(player.currentLocation, (int)toolLocation.X, (int)toolLocation.Y, player);
            return IsWatchActive(watch).HasValue;
        }
        catch (Exception ex)
        {
            Monitor.Log($"Wallet Tools could not activate the stored Timeless Pocket Watch: {ex}", LogLevel.Error);
            return false;
        }
    }

    private bool ReturnStoredWatchToInventory(Farmer player, bool useOverflowMenu, bool showMessage)
    {
        Tool? watch = GetStoredWatch(player);
        if (watch is null)
            return false;

        if (!TryDeactivateWatch(watch))
            Monitor.Log("Wallet Tools returned the Timeless Pocket Watch to the inventory, but could not confirm that its time-stop effect was inactive.", LogLevel.Error);

        SetStoredWatch(player, null);
        ClearWalletMarkers(watch);
        SuppressInventoryConversion = true;
        try
        {
            AddToolToInventory(player, watch, useOverflowMenu);
        }
        finally
        {
            SuppressInventoryConversion = false;
        }

        RefreshWalletState(player);
        if (showMessage)
            Game1.addHUDMessage(new HUDMessage(Host.Translate("hud.timeless-pocket-watch-returned"), HUDMessage.newQuest_type));
        return true;
    }

    private bool ExposeStoredWatchForSave(Farmer player)
    {
        Tool? watch = GetStoredWatch(player);
        if (watch is null || HasOvernightExposure(player))
            return false;

        if (!TryDeactivateWatch(watch))
            Monitor.Log("Wallet Tools exposed the Timeless Pocket Watch for saving, but could not confirm that its time-stop effect was inactive.", LogLevel.Error);

        MarkOvernightExposure(watch, player);
        SuppressInventoryConversion = true;
        try
        {
            AddToolToInventory(player, watch, useOverflowMenu: false);
            SetStoredWatch(player, null);
        }
        finally
        {
            SuppressInventoryConversion = false;
        }

        RefreshWalletState(player);
        return true;
    }

    private bool CollectOvernightExposedWatch(Farmer player)
    {
        if (!Config.Enabled)
            return false;

        bool changed = false;
        SuppressInventoryConversion = true;
        try
        {
            for (int index = player.Items.Count - 1; index >= 0; index--)
            {
                if (player.Items[index] is not Tool candidate
                    || !IsPocketWatch(candidate)
                    || !IsOvernightExposureForPlayer(candidate, player))
                {
                    continue;
                }

                ClearWalletMarkers(candidate);
                Tool? stored = GetStoredWatch(player);
                if (stored is null)
                {
                    SetStoredWatch(player, candidate);
                    player.Items[index] = null;
                }
                else if (CompareWatchPower(candidate, stored) > 0)
                {
                    player.Items[index] = stored;
                    SetStoredWatch(player, candidate);
                }

                changed = true;
            }
        }
        finally
        {
            SuppressInventoryConversion = false;
        }

        if (changed)
        {
            NormalizePlayerItemsAfterCleanup(player);
            RefreshWalletState(player);
        }

        return changed;
    }

    private bool CollectLostAndFoundWatch(Farmer player)
    {
        if (!Config.Enabled)
            return false;

        bool changed = false;
        SuppressInventoryConversion = true;
        try
        {
            for (int attempt = 0; attempt < 16; attempt++)
            {
                if (!TryTakeLostFoundWatch(player, out Tool recoveredWatch))
                    break;

                ClearWalletMarkers(recoveredWatch);
                Tool? stored = GetStoredWatch(player);
                if (stored is null)
                {
                    SetStoredWatch(player, recoveredWatch);
                }
                else if (CompareWatchPower(recoveredWatch, stored) > 0)
                {
                    if (!TryDeactivateWatch(stored))
                    {
                        AddToolToInventory(player, recoveredWatch, useOverflowMenu: true);
                        continue;
                    }

                    SetStoredWatch(player, recoveredWatch);
                    ClearWalletMarkers(stored);
                    AddToolToInventory(player, stored, useOverflowMenu: true);
                }
                else
                {
                    AddToolToInventory(player, recoveredWatch, useOverflowMenu: true);
                }

                changed = true;
            }
        }
        finally
        {
            SuppressInventoryConversion = false;
        }

        if (changed)
            RefreshWalletState(player);

        return changed;
    }

    private void ResolvePocketWatchBridge()
    {
        if (PocketWatchToolManager is not null && GetPocketWatchMethod is not null)
            return;

        try
        {
            Type? entryType = AccessTools.TypeByName("TimelessPocketWatch.ModEntry");
            FieldInfo? managerField = entryType is null ? null : AccessTools.Field(entryType, "toolManager");
            object? manager = managerField?.GetValue(null);
            MethodInfo? getPocketWatch = manager is null ? null : AccessTools.Method(manager.GetType(), "GetPocketWatch", new[] { typeof(Tool) });
            if (manager is null || getPocketWatch is null)
            {
                LogBridgeWarningOnce("Wallet Tools could not find the Timeless Pocket Watch runtime bridge. Watch storage was left intact, but activation state could not be verified.");
                return;
            }

            PocketWatchToolManager = manager;
            GetPocketWatchMethod = getPocketWatch;
        }
        catch (Exception ex)
        {
            LogBridgeWarningOnce($"Wallet Tools could not resolve the Timeless Pocket Watch integration bridge: {ex.Message}");
        }
    }

    private object? GetPocketWatchController(Tool watch)
    {
        ResolvePocketWatchBridge();
        if (PocketWatchToolManager is null || GetPocketWatchMethod is null)
            return null;

        try
        {
            object? controller = GetPocketWatchMethod.Invoke(PocketWatchToolManager, new object[] { watch });
            if (controller is null)
                return null;

            PocketWatchIsActiveProperty ??= AccessTools.Property(controller.GetType(), "IsActive");
            PocketWatchDeactivateMethod ??= AccessTools.Method(controller.GetType(), "Deactivate", Type.EmptyTypes);
            return controller;
        }
        catch (TargetInvocationException ex)
        {
            Monitor.Log($"Wallet Tools could not access the Timeless Pocket Watch runtime controller: {ex.InnerException ?? ex}", LogLevel.Error);
            return null;
        }
        catch (Exception ex)
        {
            Monitor.Log($"Wallet Tools could not access the Timeless Pocket Watch runtime controller: {ex}", LogLevel.Error);
            return null;
        }
    }

    private bool? IsWatchActive(Tool watch)
    {
        object? controller = GetPocketWatchController(watch);
        if (controller is null || PocketWatchIsActiveProperty is null)
            return null;

        try
        {
            return PocketWatchIsActiveProperty.GetValue(controller) is bool active ? active : null;
        }
        catch (Exception ex)
        {
            Monitor.Log($"Wallet Tools could not read the Timeless Pocket Watch active state: {ex.Message}", LogLevel.Error);
            return null;
        }
    }

    private bool TryDeactivateWatch(Tool watch)
    {
        object? controller = GetPocketWatchController(watch);
        if (controller is null || PocketWatchIsActiveProperty is null || PocketWatchDeactivateMethod is null)
            return false;

        try
        {
            if (PocketWatchIsActiveProperty.GetValue(controller) is not bool active)
                return false;

            if (!active)
                return true;

            PocketWatchDeactivateMethod.Invoke(controller, null);
            return PocketWatchIsActiveProperty.GetValue(controller) is bool after && !after;
        }
        catch (TargetInvocationException ex)
        {
            Monitor.Log($"Wallet Tools could not deactivate the Timeless Pocket Watch: {ex.InnerException ?? ex}", LogLevel.Error);
            return false;
        }
        catch (Exception ex)
        {
            Monitor.Log($"Wallet Tools could not deactivate the Timeless Pocket Watch: {ex}", LogLevel.Error);
            return false;
        }
    }

    private void LogBridgeWarningOnce(string message)
    {
        if (BridgeWarningLogged)
            return;

        BridgeWarningLogged = true;
        Monitor.Log(message, LogLevel.Warn);
    }

    private Tool? GetStoredWatch(Farmer? player)
    {
        StoredWatchByPlayer.TryGetValue(GetWalletOwnerId(player), out Tool? watch);
        return watch;
    }

    private void SetStoredWatch(Farmer? player, Tool? watch)
    {
        long ownerId = GetWalletOwnerId(player);
        if (watch is null)
        {
            StoredWatchByPlayer.Remove(ownerId);
            LastPowerStateByPlayer.Remove(ownerId);
        }
        else
        {
            StoredWatchByPlayer[ownerId] = watch;
            LastPowerStateByPlayer[ownerId] = GetPowerStateSignature(watch);
        }
    }

    private void RefreshWalletState(Farmer player)
    {
        UpdateWalletFlag(player);
        InvalidatePowers();
    }

    private void RefreshPowerIfWatchStateChanged(Farmer player, Tool watch, bool force = false)
    {
        long ownerId = GetWalletOwnerId(player);
        string current = GetPowerStateSignature(watch);
        if (!force && LastPowerStateByPlayer.TryGetValue(ownerId, out string? previous) && previous == current)
            return;

        LastPowerStateByPlayer[ownerId] = current;
        InvalidatePowers();
    }

    private void UpdateWalletFlag(Farmer player)
    {
        if (Config.Enabled && GetStoredWatch(player) is not null)
            player.modData[WalletFlagKey] = "true";
        else
            player.modData.Remove(WalletFlagKey);
    }

    private void InvalidatePowers()
    {
        Helper.GameContent.InvalidateCache("Data/Powers");
    }

    private void ClearVolatileState(bool invalidatePowers = true)
    {
        StoredWatchByPlayer.Clear();
        LastPowerStateByPlayer.Clear();
        SuppressInventoryConversion = false;
        PendingInventoryConversion = false;
        PocketWatchToolManager = null;
        GetPocketWatchMethod = null;
        PocketWatchIsActiveProperty = null;
        PocketWatchDeactivateMethod = null;
        BridgeWarningLogged = false;

        if (invalidatePowers)
            InvalidatePowers();
    }

    private string GetWatchDisplayName(Tool watch)
    {
        return !string.IsNullOrWhiteSpace(watch.DisplayName)
            ? watch.DisplayName
            : Host.Translate("item.timeless-pocket-watch.name");
    }

    private string GetWatchDescription(Tool watch)
    {
        string description;
        try
        {
            description = watch.getDescription();
        }
        catch
        {
            description = Host.Translate("item.timeless-pocket-watch.description");
        }

        Object? gear = GetAttachedGear(watch);
        string gearLine = gear is null
            ? Host.Translate("power.timeless-pocket-watch.empty")
            : Host.Translate("power.timeless-pocket-watch.gear", new { gearName = gear.DisplayName, count = Math.Max(1, gear.Stack) });
        string statusLine = Host.Translate(IsWatchActive(watch) == true
            ? "power.timeless-pocket-watch.active"
            : "power.timeless-pocket-watch.inactive");

        string attachHint = Host.Translate("power.timeless-pocket-watch.attach-hint");
        return string.Join(Environment.NewLine, new[] { description, gearLine, statusLine, attachHint }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private string GetPowerStateSignature(Tool watch)
    {
        Object? gear = GetAttachedGear(watch);
        return string.Join("|", watch.QualifiedItemId, gear?.QualifiedItemId ?? string.Empty, gear?.Stack ?? 0, IsWatchActive(watch) == true);
    }

    private static int CompareWatchPower(Tool left, Tool right)
    {
        int compare = GetGearTier(left).CompareTo(GetGearTier(right));
        if (compare != 0)
            return compare;

        return GetAttachedGearStack(left).CompareTo(GetAttachedGearStack(right));
    }

    private static int GetGearTier(Tool watch)
    {
        Object? gear = GetAttachedGear(watch);
        if (gear is null)
            return 0;

        if (ItemMatchesId(gear, RadioactiveGearId))
            return 3;
        if (ItemMatchesId(gear, IridiumGearId))
            return 2;
        if (ItemMatchesId(gear, GoldGearId))
            return 1;
        return 0;
    }

    private static int GetAttachedGearStack(Tool watch)
    {
        return Math.Max(0, GetAttachedGear(watch)?.Stack ?? 0);
    }

    private static Object? GetAttachedGear(Tool watch)
    {
        return watch.attachments?.FirstOrDefault();
    }

    private static bool ItemMatchesId(Item item, string itemId)
    {
        return ItemRegistry.HasItemId(item, itemId)
            || ItemRegistry.HasItemId(item, $"(O){itemId}")
            || string.Equals(item.ItemId, itemId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.QualifiedItemId, $"(O){itemId}", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsClockworkGear(Item? item)
    {
        return item is Object gear
            && (ItemMatchesId(gear, GoldGearId)
                || ItemMatchesId(gear, IridiumGearId)
                || ItemMatchesId(gear, RadioactiveGearId));
    }

    private static bool IsPocketWatch(Item? item)
    {
        if (item is not Tool tool)
            return false;

        return tool.modData.ContainsKey(PocketWatchTypeMarker)
            || ItemRegistry.HasItemId(tool, PocketWatchQualifiedToolId)
            || ItemRegistry.HasItemId(tool, PocketWatchToolId)
            || string.Equals(tool.ItemId, PocketWatchToolId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(tool.QualifiedItemId, PocketWatchQualifiedToolId, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCurrentlySelectedItem(Farmer player, int slot, Item item)
    {
        return slot == player.CurrentToolIndex && ReferenceEquals(player.CurrentItem, item);
    }

    private static long GetWalletOwnerId(Farmer? player)
    {
        return player?.UniqueMultiplayerID ?? 0L;
    }

    private static void MarkOvernightExposure(Tool watch, Farmer player)
    {
        watch.modData[OvernightExposureMarker] = "true";
        watch.modData[OwnerPlayerIdMarker] = player.UniqueMultiplayerID.ToString();
    }

    private static bool IsOvernightExposure(Item? item)
    {
        return item is Tool tool && tool.modData.ContainsKey(OvernightExposureMarker);
    }

    private static bool IsOvernightExposureForPlayer(Item? item, Farmer player)
    {
        return item is Tool tool
            && tool.modData.ContainsKey(OvernightExposureMarker)
            && IsOwnedByPlayer(tool, player);
    }

    private static bool IsOwnedByPlayer(Tool watch, Farmer player)
    {
        if (!watch.modData.TryGetValue(OwnerPlayerIdMarker, out string? rawOwnerId))
            return !Context.IsMultiplayer;

        return long.TryParse(rawOwnerId, out long ownerId) && ownerId == player.UniqueMultiplayerID;
    }

    private static bool HasOvernightExposure(Farmer player)
    {
        return player.Items.Any(item => IsOvernightExposureForPlayer(item, player) && IsPocketWatch(item));
    }

    private static void ClearWalletMarkers(Tool watch)
    {
        watch.modData.Remove(OvernightExposureMarker);
        watch.modData.Remove(OwnerPlayerIdMarker);
    }

    private static void ClearWalletMarkersFromInventory(Farmer player, bool keepOwnedOvernightExposure)
    {
        foreach (Item? item in player.Items)
        {
            if (item is not Tool watch || !IsPocketWatch(watch))
                continue;

            if (keepOwnedOvernightExposure && IsOvernightExposureForPlayer(watch, player))
                continue;

            ClearWalletMarkers(watch);
        }
    }

    private static void AddToolToInventory(Farmer player, Tool watch, bool useOverflowMenu)
    {
        int maxItems = GetPlayerMaxItemCount(player);
        int searchCount = Math.Min(player.Items.Count, maxItems);
        for (int index = 0; index < searchCount; index++)
        {
            if (player.Items[index] is null)
            {
                player.Items[index] = watch;
                NormalizePlayerItemsAfterCleanup(player);
                return;
            }
        }

        if (player.Items.Count < maxItems)
        {
            player.Items.Add(watch);
            NormalizePlayerItemsAfterCleanup(player);
            return;
        }

        if (useOverflowMenu)
            player.addItemByMenuIfNecessary(watch);
        else
            player.Items.Add(watch);

        NormalizePlayerItemsAfterCleanup(player);
    }

    private static int GetPlayerMaxItemCount(Farmer player)
    {
        foreach (string memberName in new[] { "MaxItems", "maxItems" })
        {
            try
            {
                PropertyInfo? property = player.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property?.GetValue(player) is int propertyValue && propertyValue > 0)
                    return Math.Max(12, propertyValue);

                FieldInfo? field = player.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field?.GetValue(player) is int fieldValue && fieldValue > 0)
                    return Math.Max(12, fieldValue);
            }
            catch
            {
            }
        }

        return 36;
    }

    private static void NormalizePlayerItemsAfterCleanup(Farmer player)
    {
        int maxItems = GetPlayerMaxItemCount(player);
        if (player.Items.Count <= maxItems)
            return;

        for (int index = maxItems; index < player.Items.Count; index++)
        {
            if (player.Items[index] is not null)
                return;
        }

        while (player.Items.Count > maxItems)
            player.Items.RemoveAt(player.Items.Count - 1);
    }

    private static bool TryTakeLostFoundWatch(Farmer player, out Tool watch)
    {
        watch = null!;
        object? team = player.team;
        if (team is null)
            return false;

        bool requireOwner = Context.IsMultiplayer;
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (MemberInfo member in team.GetType().GetMembers(flags))
        {
            if (member.MemberType != MemberTypes.Field && member.MemberType != MemberTypes.Property)
                continue;

            object? value = GetMemberValue(member, team);
            if (!IsLikelyLostFoundWatchContainer(member, value))
                continue;

            if (value is Tool directWatch
                && IsPocketWatch(directWatch)
                && WatchIsEligibleForOwner(directWatch, player, requireOwner)
                && SetMemberValue(member, team, null))
            {
                ClearWalletMarkers(directWatch);
                watch = directWatch;
                return true;
            }

            if (TryTakeWatchFromValue(value, out watch, player, requireOwner))
                return true;
        }

        return false;
    }

    private static bool IsLikelyLostFoundWatchContainer(MemberInfo member, object? value)
    {
        if (value is null || value is string)
            return false;

        string name = member.Name;
        bool nameLooksRelevant = name.Contains("lost", StringComparison.OrdinalIgnoreCase)
            || name.Contains("found", StringComparison.OrdinalIgnoreCase)
            || name.Contains("return", StringComparison.OrdinalIgnoreCase)
            || name.Contains("tool", StringComparison.OrdinalIgnoreCase)
            || name.Contains("watch", StringComparison.OrdinalIgnoreCase);
        if (!nameLooksRelevant)
            return false;

        Type valueType = value.GetType();
        if (typeof(Tool).IsAssignableFrom(valueType) || typeof(IList).IsAssignableFrom(valueType) || typeof(IDictionary).IsAssignableFrom(valueType))
            return true;

        if (TryGetNetValue(value) is Tool)
            return true;

        PropertyInfo? valueProperty = valueType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return valueProperty is not null && valueProperty.GetIndexParameters().Length == 0;
    }

    private static bool TryTakeWatchFromValue(object? value, out Tool watch, Farmer? player = null, bool requireOwner = false)
    {
        watch = null!;
        if (value is null || value is string)
            return false;

        if (TryTakeWatchFromWrappedValue(value, out watch, player, requireOwner))
            return true;

        object? unwrapped = TryGetNetValue(value);
        if (!ReferenceEquals(unwrapped, value) && unwrapped is not Tool && TryTakeWatchFromValue(unwrapped, out watch, player, requireOwner))
            return true;

        if (value is IList list)
        {
            for (int index = 0; index < list.Count; index++)
            {
                if (list[index] is Tool candidate && IsPocketWatch(candidate) && WatchIsEligibleForOwner(candidate, player, requireOwner))
                {
                    ClearWalletMarkers(candidate);
                    list.RemoveAt(index);
                    watch = candidate;
                    return true;
                }

                if (TryTakeWatchFromValue(list[index], out watch, player, requireOwner))
                    return true;
            }
        }

        if (value is IDictionary dictionary)
        {
            foreach (object? key in dictionary.Keys.Cast<object?>().ToArray())
            {
                if (key is null)
                    continue;

                object? dictionaryValue = dictionary[key];
                if (dictionaryValue is Tool candidate && IsPocketWatch(candidate) && WatchIsEligibleForOwner(candidate, player, requireOwner))
                {
                    ClearWalletMarkers(candidate);
                    dictionary.Remove(key);
                    watch = candidate;
                    return true;
                }

                if (TryTakeWatchFromValue(dictionaryValue, out watch, player, requireOwner))
                    return true;
            }
        }

        return false;
    }

    private static bool TryTakeWatchFromWrappedValue(object value, out Tool watch, Farmer? player = null, bool requireOwner = false)
    {
        watch = null!;
        try
        {
            PropertyInfo? property = value.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property is null || property.GetIndexParameters().Length != 0 || !property.CanRead || !property.CanWrite)
                return false;

            object? inner = property.GetValue(value);
            if (inner is not Tool candidate || !IsPocketWatch(candidate) || !WatchIsEligibleForOwner(candidate, player, requireOwner))
                return false;

            ClearWalletMarkers(candidate);
            property.SetValue(value, null);
            watch = candidate;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool WatchIsEligibleForOwner(Tool watch, Farmer? player, bool requireOwner)
    {
        return !requireOwner || (player is not null && IsOwnedByPlayer(watch, player));
    }

    private static object? TryGetNetValue(object value)
    {
        try
        {
            PropertyInfo? property = value.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return property?.GetIndexParameters().Length == 0 ? property.GetValue(value) : value;
        }
        catch
        {
            return value;
        }
    }

    private static object? GetMemberValue(MemberInfo member, object owner)
    {
        try
        {
            return member switch
            {
                FieldInfo field => field.GetValue(owner),
                PropertyInfo property when property.GetIndexParameters().Length == 0 => property.GetValue(owner),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static bool SetMemberValue(MemberInfo member, object owner, object? value)
    {
        try
        {
            switch (member)
            {
                case FieldInfo field when !field.IsInitOnly:
                    field.SetValue(owner, value);
                    return true;
                case PropertyInfo property when property.GetIndexParameters().Length == 0 && property.CanWrite:
                    property.SetValue(owner, value);
                    return true;
                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private bool ShouldReportStoredItem(string itemId)
    {
        if (!Context.IsWorldReady || !Config.Enabled || GetStoredWatch(Game1.player) is not Tool watch)
            return false;

        if (MatchesRequestedItemId(itemId, PocketWatchToolId, PocketWatchQualifiedToolId))
            return true;

        Object? gear = GetAttachedGear(watch);
        return gear is not null && MatchesRequestedItemId(itemId, gear.ItemId, gear.QualifiedItemId);
    }

    private static bool MatchesRequestedItemId(string requestedId, string itemId, string qualifiedItemId)
    {
        return string.Equals(requestedId, itemId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(requestedId, qualifiedItemId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(requestedId, $"(O){itemId}", StringComparison.OrdinalIgnoreCase)
            || string.Equals(requestedId, $"(T){itemId}", StringComparison.OrdinalIgnoreCase);
    }

    private static class DoesItemExistAnywherePatch
    {
        public static void Postfix(string __0, ref bool __result)
        {
            if (!__result && Instance?.ShouldReportStoredItem(__0) == true)
                __result = true;
        }
    }
}
