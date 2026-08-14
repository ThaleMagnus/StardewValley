using System.Linq;
using Microsoft.Xna.Framework;
using ThaleTheGreat.PyromancersJourney.Framework;
using ThaleTheGreat.PyromancersJourney.Integrations;
using SpaceShared;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using SObject = StardewValley.Object;

namespace ThaleTheGreat.PyromancersJourney
{
    public class Mod : StardewModdingAPI.Mod
    {
        private const string ArcadeTileAction = "ThaleTheGreat.PyromancersJourney_FireArcadeGame";
        private const string CompletionMailFlag = "ThaleTheGreat.PyromancersJourney_Beaten";
        private const string PrizeMailFlag = "ThaleTheGreat.PyromancersJourney_PrizeClaimed";

        public static Mod Instance { get; private set; } = null!;
        public ModConfig Config { get; private set; } = new();

        private PendingRunState PendingRun;

        public override void Entry(IModHelper helper)
        {
            Mod.Instance = this;
            Log.Monitor = this.Monitor;
            this.Config = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Player.Warped += this.OnWarped;

            GameLocation.RegisterTileAction(ArcadeTileAction, OnActionActivated);

            helper.ConsoleCommands.Add("pyrojourney", this.T("command.start.description"), this.DoCommands);
        }

        public void QueueCompletedRun(bool usedInfiniteHealth)
        {
            this.PendingRun = usedInfiniteHealth
                ? PendingRunState.ShowIneligibleResult
                : PendingRunState.ShowEligibleResult;
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (this.PendingRun == PendingRunState.None
                || !Context.IsWorldReady
                || Game1.currentMinigame is not null
                || Game1.activeClickableMenu is not null)
            {
                return;
            }

            switch (this.PendingRun)
            {
                case PendingRunState.ShowIneligibleResult:
                    this.PendingRun = PendingRunState.None;
                    Game1.drawObjectDialogue(this.T("result.win.ineligible"));
                    break;

                case PendingRunState.ShowEligibleResult:
                    if (Game1.player.hasOrWillReceiveMail(PrizeMailFlag))
                    {
                        this.PendingRun = PendingRunState.None;
                        Game1.drawObjectDialogue(this.T("result.win"));
                    }
                    else
                    {
                        this.PendingRun = PendingRunState.DeliverPrize;
                        Game1.drawObjectDialogue(this.T("result.win.prize"));
                    }
                    break;

                case PendingRunState.DeliverPrize:
                    Game1.player.addItemByMenuIfNecessaryElseHoldUp(new SObject("848", 25));
                    Game1.player.mailReceived.Add(CompletionMailFlag);
                    Game1.player.mailReceived.Add(PrizeMailFlag);
                    this.PendingRun = PendingRunState.None;
                    break;
            }
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            IGenericModConfigMenuApi? gmcm = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcm is null)
                return;

            gmcm.Register(
                this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            gmcm.AddBoolOption(
                this.ModManifest,
                getValue: () => this.Config.InfiniteHealth,
                setValue: value => this.Config.InfiniteHealth = value,
                name: () => this.T("config.infinite-health.name"),
                tooltip: () => this.T("config.infinite-health.tooltip"),
                fieldId: nameof(ModConfig.InfiniteHealth)
            );

            gmcm.AddBoolOption(
                this.ModManifest,
                getValue: () => this.Config.ShowReticle,
                setValue: value => this.Config.ShowReticle = value,
                name: () => this.T("config.show-reticle.name"),
                tooltip: () => this.T("config.show-reticle.tooltip"),
                fieldId: nameof(ModConfig.ShowReticle)
            );

            gmcm.AddTextOption(
                this.ModManifest,
                getValue: () => this.Config.ReticleColor,
                setValue: value => this.Config.ReticleColor = value,
                name: () => this.T("config.reticle-color.name"),
                tooltip: () => this.T("config.reticle-color.tooltip"),
                allowedValues: ReticlePalette.Names,
                formatAllowedValue: value => this.T($"config.reticle-color.values.{value.ToLowerInvariant()}"),
                fieldId: nameof(ModConfig.ReticleColor)
            );
        }

        internal string T(string key)
        {
            return this.Helper.Translation.Get(key).ToString();
        }

        private bool OnActionActivated(GameLocation loc, string[] args, Farmer farmer, Point pos)
        {
            Game1.currentMinigame = new PyromancerMinigame();
            return true;
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (e.NewLocation is VolcanoDungeon vd && vd.level.Value == 5)
            {
                var ts = vd.Map.TileSheets.FirstOrDefault(t => t.ImageSource.Contains("arcade-machine"));
                if (ts == null)
                {
                    ts = new xTile.Tiles.TileSheet(vd.Map, this.Helper.ModContent.GetInternalAssetName("assets/arcade-machine.png").BaseName, new xTile.Dimensions.Size(2, 2), new xTile.Dimensions.Size(16, 16));
                    ts.Id = "z" + ts.Id;
                    vd.Map.AddTileSheet(ts);
                    SetMapTile(vd, 31, 28, 3, "Buildings", ts.Id, ArcadeTileAction);
                    SetMapTile(vd, 31, 27, 1, "Front", ts.Id);
                    Game1.mapDisplayDevice.LoadTileSheet(ts);
                }
            }
        }

        private void DoCommands(string cmd, string[] args)
        {
            if (cmd == "pyrojourney")
            {
                if (!Context.IsPlayerFree)
                    Log.Info(this.T("command.error.player-not-free"));
                else
                    Game1.currentMinigame = new PyromancerMinigame();
            }
        }

        private static void SetMapTile(GameLocation location, int tileX, int tileY, int index, string layerName, string tileSheetId, string? action = null)
        {
            var layer = location.Map.GetLayer(layerName) ?? throw new global::System.InvalidOperationException($"Map layer '{layerName}' was not found.");
            var tileSheet = location.Map.GetTileSheet(tileSheetId) ?? throw new global::System.InvalidOperationException($"Map tilesheet '{tileSheetId}' was not found.");
            var tile = new xTile.Tiles.StaticTile(layer, tileSheet, xTile.Tiles.BlendMode.Alpha, index);
            layer.Tiles[tileX, tileY] = tile;
            if (action is not null && layerName == "Buildings")
                tile.Properties.Add("Action", action);
        }

        private enum PendingRunState
        {
            None,
            ShowEligibleResult,
            ShowIneligibleResult,
            DeliverPrize
        }
    }
}
