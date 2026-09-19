using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using ThaleMagnus.UniversalItemBags.Api;
using ThaleMagnus.UniversalItemBags.Discovery;
using ThaleMagnus.UniversalItemBags.Integration;

namespace ThaleMagnus.UniversalItemBags;

public sealed class ModEntry : Mod
{
    private ModConfig config = new();
    private UniversalDiscoveryService? discovery;
    private UniversalItemBagsApi? api;

    public override void Entry(IModHelper helper)
    {
        config = helper.ReadConfig<ModConfig>();

        ItemBagsBridge bridge = new(Monitor);
        UniversalDiscoveryService service = new(helper, Monitor, ModManifest, config, bridge);
        discovery = service;
        service.RegisterEvents();
        api = new UniversalItemBagsApi(service);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.ConsoleCommands.Add(
            "ttg_universal_item_bags_refresh",
            "Refresh automatic Item Bags category assignments.",
            (_, _) => service.ScheduleRefresh(1)
        );
    }

    public override object GetApi()
    {
        return api ?? throw new InvalidOperationException("The mod API is not initialized.");
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        GmcmIntegration.Register(
            Helper,
            Monitor,
            ModManifest,
            config,
            () => discovery?.ScheduleRefresh(1)
        );
    }
}
