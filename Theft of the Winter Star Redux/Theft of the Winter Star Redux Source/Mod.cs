using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Spacechase.Shared.Patching;


using SpaceShared;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.Projectiles;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

using ThaleTheGreat.TheftOfTheWinterStar.Framework;
using ThaleTheGreat.TheftOfTheWinterStar.Patches;

using xTile;
using xTile.Layers;
using xTile.Tiles;

using SObject = StardewValley.Object;

namespace ThaleTheGreat.TheftOfTheWinterStar
{
    /// <summary>The mod entry class.</summary>
    public class Mod : StardewModdingAPI.Mod
    {
        /*********
        ** Fields
        *********/
        private const int CurrentSaveDataVersion = 2;
        private const string LegacyEventId = "ThaleTheGreat.TheftOfTheWinterStar_Intro";
        private const string EventIdPrefix = "ThaleTheGreat.TheftOfTheWinterStar_Intro.Year";
        private const string SaveDataKey = "ThaleTheGreat.TheftOfTheWinterStar.SaveData";
        public const string FestiveBigKeyAId = "ThaleTheGreat.TheftOfTheWinterStar_FestiveBigKeyA";
        public const string FestiveBigKeyBId = "ThaleTheGreat.TheftOfTheWinterStar_FestiveBigKeyB";
        public const string FestiveKeyId = "ThaleTheGreat.TheftOfTheWinterStar_FestiveKey";
        public const string FrostyStardropPieceId = "ThaleTheGreat.TheftOfTheWinterStar_FrostyStardropPiece";
        public const string TempusGlobeId = "ThaleTheGreat.TheftOfTheWinterStar_TempusGlobe";
        public const string FestiveScepterId = "ThaleTheGreat.TheftOfTheWinterStar_FestiveScepter";
        private const string LockFlagPrefix = "ThaleTheGreat.TheftOfTheWinterStar_Lock.";
        private const string LockedDoorAction = "ThaleTheGreat.TheftOfTheWinterStar_LockedDoor";
        private const string ActivateArenaAction = "ThaleTheGreat.TheftOfTheWinterStar_ActivateArena";
        private const string ItemPuzzleAction = "ThaleTheGreat.TheftOfTheWinterStar_ItemPuzzle";
        private const string BossKeyHalfAction = "ThaleTheGreat.TheftOfTheWinterStar_BossKeyHalf";
        private const string MovableAction = "ThaleTheGreat.TheftOfTheWinterStar_Movable";
        private const string BossPresentAction = "ThaleTheGreat.TheftOfTheWinterStar_BossPresent";

        private const string RewardBonus1Piece = "Bonus1Piece";
        private const string RewardBonus2Piece = "Bonus2Piece";
        private const string RewardBonus3Piece = "Bonus3Piece";
        private const string RewardWeaponRoomScepter = "WeaponRoomScepter";
        private const string RewardKeyRoomKey = "KeyRoomKey";
        private const string RewardMazeKeyHalfA = "MazeKeyHalfA";
        private const string RewardArenaKey = "ArenaKey";
        private const string RewardArenaPiece = "ArenaPiece";
        private const string RewardProjectilePiece = "ProjectilePiece";
        private const string RewardPushPuzzleKeyHalfB = "PushPuzzleKeyHalfB";
        private const int PushBlockTileIndex = 244;
        private const int PushPuzzleSolvedTileIndex = 257;

        private bool ShouldRestoreDungeonState;
        private readonly HashSet<string> AppliedDungeonLocations = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> SpawnedRewardChests = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>The saved player progress in the dungeons.</summary>
        private SaveData SaveData = new();

        /// <summary>The boss's health bar background.</summary>
        private Texture2D BossBarBg = null!;

        /// <summary>The boss's health bar foreground.</summary>
        private Texture2D BossBarFg = null!;

        /// <summary>Whether the player has started the boss fight.</summary>
        private bool StartedBoss;

        /// <summary>The projectiles fired by the boss which are still active.</summary>
        private List<Projectile>? PrevProjectiles;


        /// <summary>The names of the custom locations to load.</summary>
        private readonly string[] LocationNames = {
            "Entrance",
            "Arena",
            "Branch1",
            "ItemPuzzle",
            "Bonus1",
            "Bonus2",
            "WeaponRoom",
            "KeyRoom",
            "Branch2",
            "PushPuzzle",
            "Bonus3",
            "Maze",
            "Bonus4",
            "Boss"
        };

        /// <summary>The locations and tiles on which to drop decorations.</summary>
        private readonly IDictionary<string, Vector2[]> DecoSpots = new Dictionary<string, Vector2[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["BusStop"] = new Vector2[] { new(5, 8), new(9, 10), new(10, 14) },
            ["Backwoods"] = new Vector2[] { new(40, 30), new(32, 31), new(25, 29) },
            ["Tunnel"] = new Vector2[] { new(33, 10), new(23, 9), new(10, 8) }
        };


        /*********
        ** Accessors
        *********/
        public static Mod Instance { get; private set; } = null!;


        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);
            Mod.Instance = this;
            Log.Monitor = this.Monitor;

            this.BossBarBg = this.Helper.ModContent.Load<Texture2D>("assets/bossbar-bg.png");
            this.BossBarFg = this.Helper.ModContent.Load<Texture2D>("assets/bossbar-fg.png");

            helper.Events.GameLoop.SaveCreated += this.OnSaveCreated;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.Saving += this.OnSaving;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.DayEnding += this.OnDayEnding;
            helper.Events.Player.Warped += this.OnWarped;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Display.RenderedHud += this.OnRenderedHud;
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            helper.Events.Content.LocaleChanged += this.OnLocaleChanged;

            GameLocation.RegisterTileAction(LockedDoorAction, this.OnTileAction);
            GameLocation.RegisterTileAction(ActivateArenaAction, this.OnTileAction);
            GameLocation.RegisterTileAction(ItemPuzzleAction, this.OnTileAction);
            GameLocation.RegisterTileAction(BossKeyHalfAction, this.OnTileAction);
            GameLocation.RegisterTileAction(MovableAction, this.OnTileAction);
            GameLocation.RegisterTileAction(BossPresentAction, this.OnTileAction);

            HarmonyPatcher.Apply(this,
                new GamePatcher(),
                new HoeDirtPatcher()
            );
        }

        private void OnLocaleChanged(object? sender, LocaleChangedEventArgs e)
        {
            this.Helper.GameContent.InvalidateCache("Strings/StringsFromMaps");
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // scatter decorations
            if (this.IsPossibleDecoSpotsMap(e.NameWithoutLocale))
                e.Edit(asset =>
                {
                    if (this.TryGetDecoSpots(asset, out Vector2[] decoSpots) && asset.Data is Map map)
                        this.ScatterDecorationsIfNeeded(map, decoSpots);
                });

            // add map strings
            else if (e.NameWithoutLocale.IsEquivalentTo("Strings/StringsFromMaps"))
            {
                e.Edit(static asset =>
                {
                    var dict = asset.AsDictionary<string, string>().Data;
                    dict.Add("FrostDungeon.LockedEntrance", I18n.MapMessages_LockedEntrance());
                    dict.Add("FrostDungeon.Locked", I18n.MapMessages_LockedDoor());
                    dict.Add("FrostDungeon.LockedBoss", I18n.MapMessages_LockedBoss());
                    dict.Add("FrostDungeon.Unlock", I18n.MapMessages_Unlocked());
                    dict.Add("FrostDungeon.ItemPuzzle", I18n.MapMessages_ItemPuzzle());
                    dict.Add("FrostDungeon.Target", I18n.MapMessages_Target());
                    dict.Add("FrostDungeon.Trail0", I18n.MapMessages_TrailLights());
                    dict.Add("FrostDungeon.Trail1", I18n.MapMessages_TrailCandyCane());
                    dict.Add("FrostDungeon.Trail2", I18n.MapMessages_TrailOrnaments());
                    dict.Add("FrostDungeon.Trail3", I18n.MapMessages_TrailTree());
                });
            }

            // edit tunnel map
            if (e.NameWithoutLocale.IsEquivalentTo("Maps/Tunnel"))
            {
                e.Edit(asset =>
                {
                    var overlay = Game1.currentSeason == "winter" && Game1.dayOfMonth < 25
                        ? this.Helper.ModContent.Load<Map>("assets/OverlayPortal.tmx")
                        : this.Helper.ModContent.Load<Map>("assets/OverlayPortalLocked.tmx");

                    asset
                        .AsMap()
                        .PatchMap(overlay, targetArea: new Rectangle(7, 4, 3, 3));
                });
            }
        }

        /*********
        ** Private methods
        *********/
        /// <inheritdoc cref="IGameLoopEvents.SaveCreated"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveCreated(object? sender, SaveCreatedEventArgs e)
        {
            this.SaveData = this.CreateNewSaveData();
            this.ResetRuntimeDungeonState();
            this.ShouldRestoreDungeonState = true;
        }

        /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            SaveData? loaded = this.Helper.Data.ReadSaveData<SaveData>(SaveDataKey);
            if (loaded is null)
                this.SaveData = this.CreateNewSaveData();
            else
            {
                this.SaveData = loaded;
                this.NormalizeSaveData();
                this.MigrateLegacySaveData();
            }

            this.EnsureWinterCycle();
            this.ResetRuntimeDungeonState();
            this.ShouldRestoreDungeonState = true;
        }

        /// <inheritdoc cref="IGameLoopEvents.Saving"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaving(object? sender, SavingEventArgs e)
        {
            if (!Game1.IsMasterGame)
                return;

            this.SyncRewardClaims();
            this.Helper.Data.WriteSaveData(SaveDataKey, this.SaveData);
        }

        /// <inheritdoc cref="IGameLoopEvents.UpdateTicked"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (this.ShouldRestoreDungeonState)
            {
                this.ShouldRestoreDungeonState = false;
                this.RestoreDungeonState();
            }

            if (e.IsMultipleOf(15))
                this.SyncRewardClaims();

            GameLocation location = Game1.currentLocation;

            switch (location?.Name)
            {
                case "FrostDungeon.Arena":
                    if ((this.SaveData.ArenaStage is ArenaStage.Stage1 or ArenaStage.Stage2) && !location.characters.Any(npc => npc is Monster))
                    {
                        switch (this.SaveData.ArenaStage)
                        {
                            case ArenaStage.Stage1:
                            {
                                this.SaveData.ArenaStage = ArenaStage.Finished1;
                                this.SpawnRewardChest(RewardArenaKey);
                                Game1.playSound("questcomplete");
                            }
                            break;

                            case ArenaStage.Stage2:
                            {
                                this.SaveData.ArenaStage = ArenaStage.Finished2;
                                this.SpawnRewardChest(RewardArenaPiece);
                                Game1.playSound("questcomplete");
                            }
                            break;
                        }
                    }
                    break;

                case "FrostDungeon.Bonus4":
                    if (!this.SaveData.DidProjectilePuzzle)
                    {
                        var projectiles = location.projectiles.ToList();
                        if (this.PrevProjectiles != null)
                        {
                            foreach (var projectile in projectiles)
                            {
                                if (this.PrevProjectiles.Contains(projectile))
                                    this.PrevProjectiles.Remove(projectile);
                            }

                            foreach (var projectile in this.PrevProjectiles)
                            {
                                if (projectile.getBoundingBox().Intersects(new Rectangle((int)(8.5 * Game1.tileSize), (int)(8.5 * Game1.tileSize), Game1.tileSize * 2, Game1.tileSize * 2)))
                                {
                                    this.SaveData.DidProjectilePuzzle = true;
                                    this.SpawnRewardChest(RewardProjectilePiece);
                                    break;
                                }
                            }
                        }
                        this.PrevProjectiles = projectiles;
                    }
                    break;

                case "FrostDungeon.Boss":
                    if (this.StartedBoss && !this.SaveData.BeatBoss)
                    {
                        if (location.characters.Count(npc => npc is Monster) <= 0)
                        {
                            this.StartedBoss = false;
                            this.SaveData.BeatBoss = true;
                            Game1.playSound("achievement");

                            foreach (var player in Game1.getAllFarmers())
                            {
                                foreach (NPC npc in Utility.getAllCharacters())
                                    player.changeFriendship(250, npc);
                            }
                        }
                    }
                    break;
            }
        }

        /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (this.EnsureWinterCycle())
                this.ShouldRestoreDungeonState = true;

            // update maps
            this.Helper.GameContent.InvalidateCache("Maps/Tunnel");
            foreach (string mapName in this.DecoSpots.Keys)
                this.Helper.GameContent.InvalidateCache($"Maps/{mapName}");

            // apply Tempus Globe logic
            string seasonalDelimiterId = $"(BC){TempusGlobeId}";
            Utility.ForEachLocation(loc =>
            {
                if (!this.IsFarm(loc))
                    return true;
                foreach (var pair in loc.Objects.Pairs)
                {
                    var obj = pair.Value;
                    if (obj.QualifiedItemId == seasonalDelimiterId)
                    {
                        for (int ix = -2; ix <= 2; ++ix)
                        {
                            for (int iy = -2; iy <= 2; ++iy)
                            {
                                var key = new Vector2(pair.Key.X + ix, pair.Key.Y + iy);
                                if (!loc.terrainFeatures.TryGetValue(key, out TerrainFeature feature))
                                    continue;
                                if (feature is HoeDirt dirt)
                                {
                                    dirt.state.Value = HoeDirt.watered;
                                    dirt.updateNeighbors();
                                }
                            }
                        }

                        loc.temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 2176, 320, 320), 60f, 4, 100, pair.Key * 64f + new Vector2(sbyte.MinValue, sbyte.MinValue), false, false)
                        {
                            color = Color.White * 0.4f,
                            delayBeforeAnimationStart = Game1.random.Next(1000),
                            id = (int)(pair.Key.X * 4000f + pair.Key.Y)
                        });
                    }
                }

                return true;
            });
        }

        /// <inheritdoc cref="IGameLoopEvents.DayEnding"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            this.SyncRewardClaims();

            this.SaveData.ArenaStage = this.SaveData.ArenaStage switch
            {
                ArenaStage.Stage1 => ArenaStage.NotTriggered,
                ArenaStage.Stage2 => ArenaStage.Finished1,
                _ => this.SaveData.ArenaStage
            };

            GameLocation? arena = Game1.getLocationFromName("FrostDungeon.Arena");
            arena?.characters.Clear();

            GameLocation? bossArea = Game1.getLocationFromName("FrostDungeon.Boss");
            if (bossArea is not null && !this.SaveData.BeatBoss)
            {
                bossArea.characters.Clear();
                bossArea.netObjects.Clear();
                this.StartedBoss = false;
            }
        }

        /// <summary>Get whether a location can be farmed.</summary>
        /// <param name="location">The location to check.</param>
        private bool IsFarm(GameLocation location)
        {
            return location.IsFarm || location.IsGreenhouse || location is Farm or IslandWest;
        }

        private void RestoreDungeonState()
        {
            foreach (string locName in this.LocationNames)
            {
                GameLocation? location = Game1.getLocationFromName("FrostDungeon." + locName);
                if (location is not null)
                    this.ApplyDungeonLocationState(location);
            }

            foreach (RewardChestDefinition reward in this.GetRewardChestDefinitions())
            {
                if (this.IsRewardAvailable(reward.Id))
                    this.SpawnRewardChest(reward.Id);
            }
        }

        private void ApplyDungeonLocationState(GameLocation location)
        {
            if (!location.Name.StartsWith("FrostDungeon.", StringComparison.OrdinalIgnoreCase)
                || !this.AppliedDungeonLocations.Add(location.Name))
            {
                return;
            }

            this.ApplyUnlockedDoorState(location);
            this.ApplyItemPuzzleState(location);

            if (this.SaveData.BombedLocations.Contains(location.Name))
                this.ApplyBombedPassageState(location);

            if (location.Name == "FrostDungeon.Branch2")
                this.ApplyBossKeyState(location);
            else if (location.Name == "FrostDungeon.PushPuzzle")
                this.ApplyPushPuzzleState(location);
        }

        /// <inheritdoc cref="IPlayerEvents.Warped"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (!e.IsLocalPlayer)
                return;

            if (e.NewLocation.Name.StartsWith("FrostDungeon.", StringComparison.OrdinalIgnoreCase))
            {
                this.ApplyDungeonLocationState(e.NewLocation);
                if (e.NewLocation.Name == "FrostDungeon.Bonus4")
                    this.PrevProjectiles = null;
            }

            switch (e.NewLocation.Name)
            {
                case "Farm":
                {
                    string eventId = this.GetAnnualEventId(Game1.year);
                    if (!Game1.player.eventsSeen.Contains(eventId) && Game1.currentSeason == "winter" && Game1.dayOfMonth < 25)
                    {
                        string eventStr = $"continue/64 15/farmer 64 16 2 Lewis 64 18 0/skippable/pause 1500/speak Lewis \"{I18n.Event_LewisSpeech()}\"/pause 500/end";
                        e.NewLocation.currentEvent = new Event(eventStr, this.ModManifest.UniqueID, eventId, e.Player);
                        Game1.eventUp = true;
                        Game1.displayHUD = false;
                        Game1.player.CanMove = false;
                        Game1.player.showNotCarrying();

                        Game1.player.eventsSeen.Add(eventId);
                    }
                    break;
                }

                case "FrostDungeon.Boss":
                    if (!this.StartedBoss && !this.SaveData.BeatBoss)
                    {
                        var witch = new Witch();
                        e.NewLocation.characters.Add(witch);

                        var dummySpeaker = new NPC(new AnimatedSprite("Characters\\Penny"), new Vector2(-1, -1), "", 0, "Witch", false, witch.Portrait);
                        var dialogue = new Dialogue(dummySpeaker, string.Empty, I18n.FinalBoss_Speech());
                        var dialogueBox = new DialogueBox(dialogue);

                        Game1.activeClickableMenu = dialogueBox;
                        Game1.dialogueUp = true;
                        Game1.player.Halt();
                        Game1.player.CanMove = false;
                        Game1.currentSpeaker = dummySpeaker;

                        this.StartedBoss = true;
                    }
                    break;
            }
        }

        private bool IsPossibleDecoSpotsMap(IAssetName assetName)
            => assetName.StartsWith("Maps/") && this.DecoSpots.ContainsKey(Path.GetFileNameWithoutExtension(assetName.BaseName));

        /// <summary>Get the <see cref="DecoSpots"/> for a map asset, if any.</summary>
        /// <param name="asset">The map asset being edited.</param>
        /// <param name="decoSpots">The tiles on which to drop decorations.</param>
        private bool TryGetDecoSpots(IAssetInfo asset, out Vector2[] decoSpots)
        {
            // make sure it's a map asset
            if (!typeof(Map).IsAssignableFrom(asset.DataType))
            {
                decoSpots = Array.Empty<Vector2>();
                return false;
            }

            // check for deco spots
            string mapName = Path.GetFileNameWithoutExtension(asset.NameWithoutLocale.BaseName);
            if (this.DecoSpots.TryGetValue(mapName, out Vector2[]? found) && found.Length > 0)
            {
                decoSpots = found;
                return true;
            }

            decoSpots = Array.Empty<Vector2>();
            return false;
        }

        /// <summary>Drop decorations on the given tiles.</summary>
        /// <param name="map">The map to edit.</param>
        /// <param name="spots">The tiles on which to drop decorations.</param>
        private void ScatterDecorationsIfNeeded(Map map, Vector2[] spots)
        {
            if (Game1.currentSeason == "winter" && Game1.dayOfMonth < 25 && !this.SaveData.BeatBoss)
            {
                TileSheet? tilesheet = map.TileSheets.FirstOrDefault(p => p.ImageSource.Contains("trail-decorations"));
                if (tilesheet == null)
                {
                    // AddTileSheet sorts the tilesheets by ID after adding them.
                    // The game sometimes refers to tilesheets by their index (such as in Beach.fixBridge)
                    // Prepending this to the ID should ensure that this tilesheet is added to the end,
                    // which preserves the normal indices of the tilesheets.
                    char comeLast = '\u03a9'; // Omega

                    tilesheet = new TileSheet(map, this.Helper.ModContent.GetInternalAssetName("assets/trail-decorations.png").BaseName, new xTile.Dimensions.Size(2, 2), new xTile.Dimensions.Size(16, 16));
                    tilesheet.Id = comeLast + tilesheet.Id;
                    map.AddTileSheet(tilesheet);
                    map.LoadTileSheets(Game1.mapDisplayDevice);

                    Random r = new Random((int)Game1.uniqueIDForThisGame + map.assetPath.GetHashCode());
                    var buildingsLayer = map.GetLayer("Buildings");
                    foreach (var spot in spots)
                    {
                        int tile = r.Next(4);
                        buildingsLayer.Tiles[(int)spot.X, (int)spot.Y] = new StaticTile(buildingsLayer, tilesheet, BlendMode.Alpha, tile)
                        {
                            Properties = { ["Action"] = $"Message \"FrostDungeon.Trail{tile}\"" }
                        };
                    }
                }
            }
            else
            {
                var layer = map.GetLayer("Buildings");
                foreach (Vector2 spot in spots)
                    layer.Tiles[(int)spot.X, (int)spot.Y] = null;
            }
        }

        private SaveData CreateNewSaveData()
        {
            return new SaveData
            {
                DataVersion = CurrentSaveDataVersion,
                DungeonYear = Game1.currentSeason == "winter" ? Game1.year : Math.Max(0, Game1.year - 1)
            };
        }

        private void NormalizeSaveData()
        {
            this.SaveData.ClaimedRewards ??= new HashSet<string>();
            this.SaveData.UnlockedDoors ??= new HashSet<string>();
            this.SaveData.SolvedItemPuzzles ??= new HashSet<string>();
            this.SaveData.BombedLocations ??= new HashSet<string>();
            this.SaveData.InsertedBossKeyHalves ??= new HashSet<string>();
            this.SaveData.PushPuzzleBlocks ??= new List<SavedTilePosition>();
        }

        private void MigrateLegacySaveData()
        {
            if (this.SaveData.DataVersion >= CurrentSaveDataVersion)
            {
                this.RemoveLegacyLockFlags();
                return;
            }

            if (this.SaveData.DataVersion == 1)
            {
                this.SaveData.ArenaStage = this.SaveData.ArenaStage switch
                {
                    ArenaStage.Stage1 => ArenaStage.NotTriggered,
                    ArenaStage.Stage2 => ArenaStage.Finished1,
                    _ => this.SaveData.ArenaStage
                };
                this.SaveData.ClaimedRewards.Clear();

                Game1.getLocationFromName("FrostDungeon.Arena")?.characters.Clear();
                if (!this.SaveData.BeatBoss)
                    Game1.getLocationFromName("FrostDungeon.Boss")?.characters.Clear();

                this.RemoveLegacyLockFlags();
                this.SaveData.DataVersion = CurrentSaveDataVersion;
                return;
            }

            this.SaveData.DungeonYear = Game1.currentSeason == "winter"
                ? Game1.year
                : Math.Max(0, Game1.year - 1);

            if (Game1.currentSeason == "winter")
            {
                string annualEventId = this.GetAnnualEventId(Game1.year);
                foreach (Farmer player in Game1.getAllFarmers())
                {
                    if (player.eventsSeen.Contains(LegacyEventId))
                        player.eventsSeen.Add(annualEventId);
                }
            }

            foreach (Farmer player in Game1.getAllFarmers())
            {
                foreach (string flag in player.mailReceived.Where(flag => flag.StartsWith(LockFlagPrefix, StringComparison.OrdinalIgnoreCase)).ToArray())
                    this.SaveData.UnlockedDoors.Add(flag.Substring(LockFlagPrefix.Length));
            }

            this.MigrateLegacyItemPuzzleState();
            this.MigrateLegacyBombedPassageState();
            this.MigrateLegacyPushPuzzleState();
            this.MigrateLegacyBossKeyState();
            this.SaveData.ClaimedRewards.Clear();
            this.RemoveLegacyLockFlags();
            this.SaveData.DataVersion = CurrentSaveDataVersion;
        }

        private void MigrateLegacyItemPuzzleState()
        {
            GameLocation? location = Game1.getLocationFromName("FrostDungeon.ItemPuzzle");
            if (location is null)
                return;

            this.MigrateLegacyItemPuzzle(location, new Point(6, 8), "FrostDungeon.Bonus2");
            this.MigrateLegacyItemPuzzle(location, new Point(11, 8), "FrostDungeon.KeyRoom");
            this.MigrateLegacyItemPuzzle(location, new Point(13, 8), "FrostDungeon.WeaponRoom");
        }

        private void MigrateLegacyItemPuzzle(GameLocation location, Point position, string destination)
        {
            bool hasWarp = location.warps.Any(warp => warp.X == position.X && warp.Y == position.Y + 3 && warp.TargetName == destination);
            string action = location.doesTileHaveProperty(position.X, position.Y, "Action", "Buildings");
            if (hasWarp && string.IsNullOrEmpty(action))
                this.SaveData.SolvedItemPuzzles.Add(this.GetTileStateKey(location, position));
        }

        private void MigrateLegacyBombedPassageState()
        {
            GameLocation? itemPuzzle = Game1.getLocationFromName("FrostDungeon.ItemPuzzle");
            if (itemPuzzle?.warps.Any(warp => warp.TargetName == "FrostDungeon.Bonus1") == true)
                this.SaveData.BombedLocations.Add(itemPuzzle.Name);

            GameLocation? pushPuzzle = Game1.getLocationFromName("FrostDungeon.PushPuzzle");
            if (pushPuzzle?.warps.Any(warp => warp.TargetName == "FrostDungeon.Bonus3") == true)
                this.SaveData.BombedLocations.Add(pushPuzzle.Name);
        }

        private void MigrateLegacyPushPuzzleState()
        {
            GameLocation? location = Game1.getLocationFromName("FrostDungeon.PushPuzzle");
            if (location is null)
                return;

            Layer buildings = location.Map.GetLayer("Buildings");
            var movableTiles = new List<SavedTilePosition>();
            for (int x = 0; x < buildings.LayerWidth; x++)
            {
                for (int y = 0; y < buildings.LayerHeight; y++)
                {
                    if (location.doesTileHaveProperty(x, y, "Action", "Buildings") == MovableAction)
                        movableTiles.Add(new SavedTilePosition(x, y));
                }
            }

            const int targetX = 12;
            const int targetY = 11;
            if (movableTiles.Count == 0 && location.getTileIndexAt(targetX, targetY, "Back") == PushPuzzleSolvedTileIndex)
            {
                this.SaveData.PushPuzzleSolved = true;
                this.SaveData.PushPuzzleTarget = new SavedTilePosition(targetX, targetY);
                this.SaveData.PushPuzzleBlocks.Clear();
            }
            else if (movableTiles.Count > 0)
            {
                this.SaveData.PushPuzzleBlocks.Clear();
                this.SaveData.PushPuzzleBlocks.AddRange(movableTiles);
            }
        }

        private void MigrateLegacyBossKeyState()
        {
            GameLocation? location = Game1.getLocationFromName("FrostDungeon.Branch2");
            if (location is null)
                return;

            Layer buildings = location.Map.GetLayer("Buildings");
            if (buildings.Tiles[4, 9] is null)
                this.SaveData.InsertedBossKeyHalves.Add("A");
            if (buildings.Tiles[15, 9] is null)
                this.SaveData.InsertedBossKeyHalves.Add("B");
        }

        private void RemoveLegacyLockFlags()
        {
            foreach (Farmer player in Game1.getAllFarmers())
            {
                foreach (string flag in player.mailReceived.Where(flag => flag.StartsWith(LockFlagPrefix, StringComparison.OrdinalIgnoreCase)).ToArray())
                    player.mailReceived.Remove(flag);
            }
        }

        private bool EnsureWinterCycle()
        {
            if (Game1.currentSeason != "winter" || this.SaveData.DungeonYear == Game1.year)
                return false;

            this.SaveData.DungeonYear = Game1.year;
            this.SaveData.ArenaStage = ArenaStage.NotTriggered;
            this.SaveData.DidProjectilePuzzle = false;
            this.SaveData.BeatBoss = false;
            this.SaveData.ClaimedBossPresent = false;
            this.SaveData.PushPuzzleSolved = false;
            this.SaveData.PushPuzzleTarget = null;
            this.SaveData.ClaimedRewards.Clear();
            this.SaveData.UnlockedDoors.Clear();
            this.SaveData.SolvedItemPuzzles.Clear();
            this.SaveData.BombedLocations.Clear();
            this.SaveData.InsertedBossKeyHalves.Clear();
            this.SaveData.PushPuzzleBlocks.Clear();
            this.RemoveLegacyLockFlags();
            this.ResetRuntimeDungeonState();
            this.ResetDungeonLocationsInPlace();
            return true;
        }

        private void ResetRuntimeDungeonState()
        {
            this.AppliedDungeonLocations.Clear();
            this.SpawnedRewardChests.Clear();
            this.StartedBoss = false;
            this.PrevProjectiles = null;
        }

        private void ResetDungeonLocationsInPlace()
        {
            foreach (string locName in this.LocationNames)
            {
                GameLocation? location = Game1.getLocationFromName($"FrostDungeon.{locName}");
                if (location is null)
                    continue;

                location.reloadMap();
                location.characters.Clear();
                location.projectiles.Clear();
                location.overlayObjects.Clear();
                location.netObjects.Clear();
            }
        }

        private string GetAnnualEventId(int year)
        {
            return $"{EventIdPrefix}.{year}";
        }

        private string GetTileStateKey(GameLocation location, Point position)
        {
            return $"{location.Name}:{position.X},{position.Y}";
        }

        private void ApplyUnlockedDoorState(GameLocation location)
        {
            Layer buildings = location.Map.GetLayer("Buildings");
            for (int x = 0; x < buildings.LayerWidth; x++)
            {
                for (int y = 0; y < buildings.LayerHeight; y++)
                {
                    string unlockId = location.doesTileHaveProperty(x, y, "UnlockId", "Buildings");
                    if (string.IsNullOrEmpty(unlockId) || !this.SaveData.UnlockedDoors.Contains(unlockId))
                        continue;

                    location.setTileProperty(x, y, "Buildings", "Action", LockedDoorAction);
                    SetMapTileIndex(location, x, y - 2, 48, "Buildings");
                }
            }
        }

        private void ApplyItemPuzzleState(GameLocation location)
        {
            Layer buildings = location.Map.GetLayer("Buildings");
            var solvedTiles = new List<(Point Position, string Destination)>();
            for (int x = 0; x < buildings.LayerWidth; x++)
            {
                for (int y = 0; y < buildings.LayerHeight; y++)
                {
                    string action = location.doesTileHaveProperty(x, y, "Action", "Buildings");
                    if (string.IsNullOrWhiteSpace(action))
                        continue;

                    string[] args = action.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var position = new Point(x, y);
                    if (args.Length >= 3 && args[0] == ItemPuzzleAction && this.SaveData.SolvedItemPuzzles.Contains(this.GetTileStateKey(location, position)))
                        solvedTiles.Add((position, args[2]));
                }
            }

            foreach ((Point position, string destination) in solvedTiles)
                this.ApplyItemPuzzleSolution(location, position, destination);
        }

        private void ApplyItemPuzzleSolution(GameLocation location, Point position, string destination)
        {
            Layer buildings = location.Map.GetLayer("Buildings");
            Tile? pedestalTile = buildings.Tiles[position.X, position.Y];
            if (pedestalTile is null)
                return;

            int warpIndex = pedestalTile.TileIndex - 32;
            location.removeTileProperty(position.X, position.Y, "Buildings", "Action");

            Layer back = location.Map.GetLayer("Back");
            back.Tiles[position.X, position.Y + 3] = new StaticTile(back, location.Map.TileSheets[0], BlendMode.Additive, warpIndex);
            if (!location.warps.Any(warp => warp.X == position.X && warp.Y == position.Y + 3 && warp.TargetName == destination))
                location.warps.Add(new Warp(position.X, position.Y + 3, destination, 7, 9, false));
        }

        private void ApplyBossKeyState(GameLocation location)
        {
            if (this.SaveData.InsertedBossKeyHalves.Contains("A"))
            {
                location.removeTile(4, 8, "Front");
                location.removeTile(4, 9, "Buildings");
            }

            if (this.SaveData.InsertedBossKeyHalves.Contains("B"))
            {
                location.removeTile(15, 8, "Front");
                location.removeTile(15, 9, "Buildings");
            }

            if (!this.SaveData.InsertedBossKeyHalves.Contains("A") || !this.SaveData.InsertedBossKeyHalves.Contains("B"))
                return;

            SetMapTileIndex(location, 9, 4, 264, "Buildings");
            SetMapTileIndex(location, 10, 4, 265, "Buildings");
            SetMapTileIndex(location, 9, 5, 280, "Buildings");
            SetMapTileIndex(location, 10, 5, 281, "Buildings");

            for (int x = 9; x <= 10; x++)
            {
                string action = location.doesTileHaveProperty(x, 5, "UnlockAction", "Buildings");
                if (!string.IsNullOrEmpty(action))
                    location.setTileProperty(x, 5, "Buildings", "Action", action);
            }
        }

        private void ApplyPushPuzzleState(GameLocation location)
        {
            Layer buildings = location.Map.GetLayer("Buildings");
            var currentBlocks = new List<SavedTilePosition>();
            for (int x = 0; x < buildings.LayerWidth; x++)
            {
                for (int y = 0; y < buildings.LayerHeight; y++)
                {
                    if (location.doesTileHaveProperty(x, y, "Action", "Buildings") == MovableAction)
                        currentBlocks.Add(new SavedTilePosition(x, y));
                }
            }

            if (!this.SaveData.PushPuzzleSolved && this.SaveData.PushPuzzleBlocks.Count == 0)
                this.SaveData.PushPuzzleBlocks.AddRange(currentBlocks.Select(tile => new SavedTilePosition(tile.X, tile.Y)));

            foreach (SavedTilePosition tile in currentBlocks)
                location.removeTile(tile.X, tile.Y, "Buildings");

            if (this.SaveData.PushPuzzleSolved)
            {
                if (this.SaveData.PushPuzzleTarget is SavedTilePosition target)
                    SetMapTileIndex(location, target.X, target.Y, PushPuzzleSolvedTileIndex, "Back");
                return;
            }

            foreach (SavedTilePosition tile in this.SaveData.PushPuzzleBlocks)
            {
                buildings.Tiles[tile.X, tile.Y] = new StaticTile(buildings, location.Map.TileSheets[0], BlendMode.Additive, PushBlockTileIndex);
                location.setTileProperty(tile.X, tile.Y, "Buildings", "Action", MovableAction);
            }
        }

        private void ApplyBombedPassageState(GameLocation location)
        {
            Layer buildings = location.Map.GetLayer("Buildings");
            for (int x = 0; x < buildings.LayerWidth; x++)
            {
                for (int y = 0; y < buildings.LayerHeight; y++)
                {
                    if (!string.IsNullOrEmpty(location.doesTileHaveProperty(x, y, "Bombable", "Buildings")))
                    {
                        this.DoBombableCheck(location, new Vector2(x, y));
                        return;
                    }
                }
            }
        }

        private void SpawnArenaWave(GameLocation location, ArenaStage stage, Point position)
        {
            if (stage == ArenaStage.Stage1)
            {
                for (int i = 0; i < 9; ++i)
                {
                    int offsetX = (int)(Math.Cos(Math.PI * 2 / 9 * i) * 5);
                    int offsetY = (int)(Math.Sin(Math.PI * 2 / 9 * i) * 5);
                    var spawnPosition = new Vector2((position.X + offsetX) * Game1.tileSize, (position.Y + offsetY) * Game1.tileSize);
                    Monster monster = (i % 3) switch
                    {
                        0 => new Ghost(spawnPosition),
                        1 => new Skeleton(spawnPosition),
                        _ => new DustSpirit(spawnPosition)
                    };
                    location.addCharacter(monster);
                }
            }
            else if (stage == ArenaStage.Stage2)
            {
                for (int i = 0; i < 3; ++i)
                {
                    int offsetX = (int)(Math.Cos(Math.PI * 2 / 3 * i) * 4);
                    int offsetY = (int)(Math.Sin(Math.PI * 2 / 3 * i) * 4);
                    if (i % 2 == 0)
                    {
                        var spawnPosition = new Vector2((position.X + offsetX) * Game1.tileSize, (position.Y + offsetY) * Game1.tileSize);
                        location.addCharacter(new Bat(spawnPosition, 77377));
                    }
                }

                location.addCharacter(new DinoMonster(new Vector2(9 * Game1.tileSize, 8 * Game1.tileSize)));
            }
        }

        private IEnumerable<RewardChestDefinition> GetRewardChestDefinitions()
        {
            yield return new RewardChestDefinition(RewardBonus1Piece, "FrostDungeon.Bonus1", new Vector2(9, 9), () => ItemRegistry.Create<SObject>($"(O){FrostyStardropPieceId}"));
            yield return new RewardChestDefinition(RewardBonus2Piece, "FrostDungeon.Bonus2", new Vector2(13, 9), () => ItemRegistry.Create<SObject>($"(O){FrostyStardropPieceId}"));
            yield return new RewardChestDefinition(RewardBonus3Piece, "FrostDungeon.Bonus3", new Vector2(9, 9), () => ItemRegistry.Create<SObject>($"(O){FrostyStardropPieceId}"));
            yield return new RewardChestDefinition(RewardWeaponRoomScepter, "FrostDungeon.WeaponRoom", new Vector2(13, 9), () => ItemRegistry.Create<MeleeWeapon>($"(W){FestiveScepterId}"));
            yield return new RewardChestDefinition(RewardKeyRoomKey, "FrostDungeon.KeyRoom", new Vector2(13, 9), () => ItemRegistry.Create<SObject>($"(O){FestiveKeyId}"));
            yield return new RewardChestDefinition(RewardMazeKeyHalfA, "FrostDungeon.Maze", new Vector2(20, 26), () => ItemRegistry.Create<SObject>($"(O){FestiveBigKeyAId}"));
            yield return new RewardChestDefinition(RewardArenaKey, "FrostDungeon.Arena", new Vector2(6, 13), () => ItemRegistry.Create<SObject>($"(O){FestiveKeyId}"));
            yield return new RewardChestDefinition(RewardArenaPiece, "FrostDungeon.Arena", new Vector2(13, 13), () => ItemRegistry.Create<SObject>($"(O){FrostyStardropPieceId}"));
            yield return new RewardChestDefinition(RewardProjectilePiece, "FrostDungeon.Bonus4", new Vector2(9, 13), () => ItemRegistry.Create<SObject>($"(O){FrostyStardropPieceId}"));
            yield return new RewardChestDefinition(RewardPushPuzzleKeyHalfB, "FrostDungeon.PushPuzzle", new Vector2(14, 13), () => ItemRegistry.Create<SObject>($"(O){FestiveBigKeyBId}"));
        }

        private bool IsRewardAvailable(string rewardId)
        {
            return rewardId switch
            {
                RewardArenaKey => this.SaveData.ArenaStage is ArenaStage.Finished1 or ArenaStage.Stage2 or ArenaStage.Finished2,
                RewardArenaPiece => this.SaveData.ArenaStage == ArenaStage.Finished2,
                RewardProjectilePiece => this.SaveData.DidProjectilePuzzle,
                RewardPushPuzzleKeyHalfB => this.SaveData.PushPuzzleSolved,
                _ => true
            };
        }

        private void SpawnRewardChest(string rewardId)
        {
            if (this.SaveData.ClaimedRewards.Contains(rewardId) || !this.IsRewardAvailable(rewardId))
                return;

            RewardChestDefinition? reward = this.GetRewardChestDefinitions().FirstOrDefault(reward => reward.Id == rewardId);
            if (reward is null)
                return;

            GameLocation? location = Game1.getLocationFromName(reward.LocationName);
            if (location is null)
                return;

            if (location.overlayObjects.TryGetValue(reward.Position, out SObject? existingObject))
            {
                if (existingObject is Chest)
                    this.SpawnedRewardChests.Add(rewardId);
                else
                    Log.Error($"Can't place dungeon reward '{rewardId}' at {reward.LocationName} ({reward.Position.X}, {reward.Position.Y}) because the tile is occupied.");
                return;
            }

            location.overlayObjects[reward.Position] = new Chest(new List<Item> { reward.CreateItem() }, reward.Position);
            this.SpawnedRewardChests.Add(rewardId);
        }

        private void SyncRewardClaims()
        {
            foreach (string rewardId in this.SpawnedRewardChests.ToArray())
            {
                RewardChestDefinition? reward = this.GetRewardChestDefinitions().FirstOrDefault(reward => reward.Id == rewardId);
                if (reward is null)
                    continue;

                GameLocation? location = Game1.getLocationFromName(reward.LocationName);
                bool claimed = location is not null
                    && (!location.overlayObjects.TryGetValue(reward.Position, out SObject? obj)
                        || obj is not Chest chest
                        || !chest.Items.Any(item => item is not null));

                if (!claimed)
                    continue;

                this.SaveData.ClaimedRewards.Add(rewardId);
                this.SpawnedRewardChests.Remove(rewardId);
                location?.overlayObjects.Remove(reward.Position);
            }
        }

        private sealed class RewardChestDefinition
        {
            public RewardChestDefinition(string id, string locationName, Vector2 position, Func<Item> createItem)
            {
                this.Id = id;
                this.LocationName = locationName;
                this.Position = position;
                this.CreateItem = createItem;
            }

            public string Id { get; }
            public string LocationName { get; }
            public Vector2 Position { get; }
            public Func<Item> CreateItem { get; }
        }

        private bool PerformUnlockedDoorAction(GameLocation location, Farmer farmer, Point position)
        {
            string action = location.doesTileHaveProperty(position.X, position.Y, "UnlockAction", "Buildings");
            if (string.IsNullOrWhiteSpace(action))
            {
                Log.Error($"The unlocked door at {location.NameOrUniqueName} ({position.X}, {position.Y}) has no UnlockAction property.");
                return false;
            }

            return location.performAction(action, farmer, new xTile.Dimensions.Location(position.X, position.Y));
        }

        internal void AddFrostDungeonLocations()
        {
            Log.Debug("Adding frost dungeon");

            foreach (string locName in this.LocationNames)
            {
                string locationName = $"FrostDungeon.{locName}";
                if (Game1.getLocationFromName(locationName) is null)
                {
                    GameLocation location = new(this.Helper.ModContent.GetInternalAssetName($"assets/{locName}.tmx").BaseName, locationName);
                    Game1.locations.Add(location);
                }
            }

            this.ShouldRestoreDungeonState = true;
        }

        private bool OnTileAction(GameLocation location, string[] args, Farmer farmer, Point position)
        {
            switch (args[0])
            {
                case LockedDoorAction:
                {
                    string unlockId = location.doesTileHaveProperty(position.X, position.Y, "UnlockId", "Buildings");
                    if (string.IsNullOrWhiteSpace(unlockId))
                    {
                        Log.Error($"The locked door at {location.NameOrUniqueName} ({position.X}, {position.Y}) has no UnlockId property.");
                        return false;
                    }

                    if (this.SaveData.UnlockedDoors.Contains(unlockId))
                        return this.PerformUnlockedDoorAction(location, farmer, position);

                    if (farmer.ActiveObject?.QualifiedItemId == $"(O){FestiveKeyId}")
                    {
                        farmer.Items.ReduceId($"(O){FestiveKeyId}", 1);
                        this.SaveData.UnlockedDoors.Add(unlockId);
                        location.setTileProperty(position.X, position.Y, "Buildings", "Action", LockedDoorAction);
                        SetMapTileIndex(location, position.X, position.Y - 2, 48, "Buildings");

                        Game1.drawDialogueNoTyping(Game1.content.LoadString("Strings\\StringsFromMaps:FrostDungeon.Unlock"));
                        Game1.playSound("crystal");
                    }
                    else
                        Game1.drawDialogueNoTyping(Game1.content.LoadString("Strings\\StringsFromMaps:FrostDungeon.Locked"));

                    return true;
                }

                case ActivateArenaAction:
                {
                    if (location.Name != "FrostDungeon.Arena")
                        return true;

                    Log.Trace("Activate arena: Stage " + this.SaveData.ArenaStage);
                    Game1.playSound("batScreech");
                    Game1.playSound("rockGolemSpawn");
                    switch (this.SaveData.ArenaStage)
                    {
                        case ArenaStage.NotTriggered:
                            this.SaveData.ArenaStage = ArenaStage.Stage1;
                            this.SpawnArenaWave(location, ArenaStage.Stage1, position);
                            break;

                        case ArenaStage.Finished1:
                            this.SaveData.ArenaStage = ArenaStage.Stage2;
                            this.SpawnArenaWave(location, ArenaStage.Stage2, position);
                            break;
                    }

                    return true;
                }

                case ItemPuzzleAction:
                {
                    int itemId = int.Parse(args[1]);
                    if (farmer.ActiveObject?.QualifiedItemId == $"(O){itemId}")
                    {
                        farmer.Items.ReduceId($"(O){itemId}", 1);
                        this.SaveData.SolvedItemPuzzles.Add(this.GetTileStateKey(location, position));
                        this.ApplyItemPuzzleSolution(location, position, args[2]);
                        Game1.playSound("secret1");
                    }
                    else
                        Game1.drawDialogueNoTyping(Game1.content.LoadString("Strings\\StringsFromMaps:FrostDungeon.ItemPuzzle"));

                    return true;
                }

                case BossKeyHalfAction:
                {
                    string half = args[1] == "A" ? "A" : "B";
                    if (this.SaveData.InsertedBossKeyHalves.Contains(half))
                        return true;

                    string key = half == "A" ? FestiveBigKeyAId : FestiveBigKeyBId;
                    if (farmer.ActiveObject?.QualifiedItemId == $"(O){key}")
                    {
                        farmer.Items.ReduceId($"(O){key}", 1);
                        this.SaveData.InsertedBossKeyHalves.Add(half);
                        this.ApplyBossKeyState(location);
                        Game1.playSound("secret1");
                    }

                    return true;
                }

                case BossPresentAction:
                {
                    if (location.Name != "FrostDungeon.Boss")
                        return true;

                    if (!this.SaveData.BeatBoss)
                    {
                        Game1.drawObjectDialogue(I18n.FinalBoss_PresentLocked());
                        return true;
                    }

                    if (this.SaveData.ClaimedBossPresent)
                    {
                        Game1.drawObjectDialogue(I18n.FinalBoss_PresentEmpty());
                        return true;
                    }

                    this.SaveData.ClaimedBossPresent = true;

                    Item stardropPiece = ItemRegistry.Create<SObject>($"(O){FrostyStardropPieceId}");
                    if (!farmer.addItemToInventoryBool(stardropPiece, false))
                        Game1.createItemDebris(stardropPiece, farmer.Position, farmer.FacingDirection, location, -1, true);

                    bool shouldTeachRecipe = !farmer.knowsRecipe("Tempus Globe");
                    foreach (Farmer player in Game1.getAllFarmers())
                    {
                        if (!player.knowsRecipe("Tempus Globe"))
                            player.craftingRecipes.Add("Tempus Globe", 0);
                    }

                    string victoryMessage = I18n.FinalBoss_VictoryMessage();
                    if (!shouldTeachRecipe)
                        victoryMessage = victoryMessage.Split('\n')[0];

                    this.Helper.GameContent.InvalidateCache("Maps/Tunnel");
                    foreach (string mapName in this.DecoSpots.Keys)
                        this.Helper.GameContent.InvalidateCache($"Maps/{mapName}");

                    Game1.playSound("questcomplete");
                    Game1.drawObjectDialogue(victoryMessage);
                    return true;
                }

                case MovableAction:
                {
                    int offsetX = 0;
                    int offsetY = 0;
                    switch (farmer.FacingDirection)
                    {
                        case Game1.down: offsetY = 1; break;
                        case Game1.up: offsetY = -1; break;
                        case Game1.left: offsetX = -1; break;
                        case Game1.right: offsetX = 1; break;
                    }

                    int[] validPuzzleTiles =
                    {
                        240, 241, 242, 243,
                        256, 257, 258, 259, 260,
                        272, 273, 274, 275, 276
                    };
                    const int target = 243;

                    int targetX = position.X;
                    int targetY = position.Y;
                    while (true)
                    {
                        targetX += offsetX;
                        targetY += offsetY;
                        if (!validPuzzleTiles.Contains(location.getTileIndexAt(targetX, targetY, "Back"))
                            || location.doesTileHaveProperty(targetX, targetY, "Action", "Buildings") == MovableAction)
                        {
                            targetX -= offsetX;
                            targetY -= offsetY;
                            break;
                        }
                    }

                    int tileIndex = location.getTileIndexAt(position.X, position.Y, "Buildings");
                    location.removeTile(position.X, position.Y, "Buildings");
                    Layer buildings = location.Map.GetLayer("Buildings");
                    buildings.Tiles[targetX, targetY] = new StaticTile(buildings, location.Map.TileSheets[0], BlendMode.Additive, tileIndex);
                    location.setTileProperty(targetX, targetY, "Buildings", "Action", MovableAction);
                    this.SaveData.PushPuzzleBlocks.RemoveAll(tile => tile.X == position.X && tile.Y == position.Y);
                    this.SaveData.PushPuzzleBlocks.Add(new SavedTilePosition(targetX, targetY));
                    Game1.playSound("throw");

                    if (location.getTileIndexAt(targetX, targetY, "Back") == target)
                    {
                        Layer back = location.Map.GetLayer("Back");
                        back.Tiles[targetX, targetY] = new StaticTile(back, location.Map.TileSheets[0], BlendMode.Additive, 257);
                        this.SaveData.PushPuzzleSolved = true;
                        this.SaveData.PushPuzzleTarget = new SavedTilePosition(targetX, targetY);
                        this.SaveData.PushPuzzleBlocks.Clear();
                        this.SpawnRewardChest(RewardPushPuzzleKeyHalfB);
                        Game1.playSound("secret1");

                        for (int x = 0; x < back.LayerWidth; ++x)
                        {
                            for (int y = 0; y < back.LayerHeight; ++y)
                            {
                                if (location.doesTileHaveProperty(x, y, "Action", "Buildings") == MovableAction)
                                    location.removeTile(x, y, "Buildings");
                            }
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        private static void SetMapTileIndex(GameLocation location, int tileX, int tileY, int index, string layerName, int tileSheetIndex = 0)
        {
            Layer layer = location.Map.GetLayer(layerName)
                ?? throw new InvalidOperationException($"Map '{location.NameOrUniqueName}' has no '{layerName}' layer.");

            if (layer.Tiles[tileX, tileY] is Tile tile)
                tile.TileIndex = index;
            else
                layer.Tiles[tileX, tileY] = new StaticTile(layer, location.Map.TileSheets[tileSheetIndex], BlendMode.Alpha, index);
        }

        /// <inheritdoc cref="IInputEvents.ButtonPressed"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button.IsActionButton() && Context.IsPlayerFree)
            {
                if (Game1.player.CurrentTool is MeleeWeapon weapon && weapon.QualifiedItemId == $"(W){FestiveScepterId}")
                {
                    if (MeleeWeapon.defenseCooldown > 0)
                        return;

                    _ = new Beam(Game1.player, e.Cursor.AbsolutePixels);
                }
            }
        }

        /// <inheritdoc cref="IDisplayEvents.RenderedHud"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            var b = e.SpriteBatch;

            if (Game1.currentLocation.characters.SingleOrDefault(npc => npc is Witch) is Witch witch)
            {
                int posX = (Game1.viewport.Width - this.BossBarBg.Width * 4) / 2;
                b.Draw(this.BossBarBg, new Vector2(posX, 5), null, Color.White, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 1);

                float percent = (float)witch.Health / Witch.WitchHealth;
                Rectangle sourceRect = new Rectangle(0, 0, (int)(this.BossBarFg.Width * percent), this.BossBarFg.Height);
                if (sourceRect.Width > 0)
                {
                    b.Draw(this.BossBarFg, new Vector2(posX, 5), sourceRect, Color.Green, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 1);
                }
            }
        }

        internal void HandleBombExploded(GameLocation location, Vector2 position, int bombRadius, Farmer who)
        {
            if (!location.Name.StartsWith("FrostDungeon."))
                return;

            int radius = bombRadius + 2;
            bool[,] circleOutlineGrid2 = Game1.getCircleOutlineGrid(radius);

            bool flag = false;
            bool changed = false;
            Vector2 index1 = new Vector2((int)(position.X - (double)radius), (int)(position.Y - (double)radius));
            for (int index2 = 0; index2 < radius * 2 + 1; ++index2)
            {
                for (int index3 = 0; index3 < radius * 2 + 1; ++index3)
                {
                    if (index2 == 0 || index3 == 0 || (index2 == radius * 2 || index3 == radius * 2))
                        flag = circleOutlineGrid2[index2, index3];
                    else if (circleOutlineGrid2[index2, index3])
                    {
                        flag = !flag;
                        if (!flag)
                        {
                            changed |= this.DoBombableCheck(location, index1);
                        }
                    }
                    if (flag)
                    {
                        changed |= this.DoBombableCheck(location, index1);
                    }
                    ++index1.Y;
                    index1.Y = Math.Min(location.map.Layers[0].LayerHeight - 1, Math.Max(0.0f, index1.Y));
                }
                ++index1.X;
                index1.X = Math.Min(location.map.Layers[0].LayerWidth - 1, Math.Max(0.0f, index1.X));
                index1.Y = position.Y - radius;
                index1.Y = Math.Min(location.map.Layers[0].LayerHeight - 1, Math.Max(0.0f, index1.Y));
            }

            if (changed)
                this.SaveData.BombedLocations.Add(location.Name);
        }

        private bool DoBombableCheck(GameLocation location, Vector2 tile)
        {
            string propVal = location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Bombable", "Buildings");
            if (string.IsNullOrEmpty(propVal))
                return false;

            string[] bombActions = propVal.Split(' ');
            foreach (string actStr in bombActions)
            {
                int eqIndex = actStr.IndexOf('=');
                string action = actStr.Substring(0, eqIndex);
                string arguments = actStr.Substring(eqIndex + 1);

                switch (action)
                {
                    case "Buildings":
                    {
                        int index = int.Parse(arguments);
                        var buildings = location.Map.GetLayer("Buildings");
                        var existingTile = buildings.Tiles[(int)tile.X, (int)tile.Y];
                        buildings.Tiles[(int)tile.X, (int)tile.Y] = (index == -1) ? null : new StaticTile(buildings, existingTile.TileSheet, BlendMode.Additive, index);
                    }
                    break;

                    case "Warp":
                    {
                        string[] tokens = arguments.Split(',');
                        var warp = new Warp((int)tile.X, (int)tile.Y, tokens[2], int.Parse(tokens[0]), int.Parse(tokens[1]), false);
                        location.warps.Add(warp);
                    }
                    break;
                }
            }

            location.removeTileProperty((int)tile.X, (int)tile.Y, "Buildings", "Bombable");
            this.DoBombableCheck(location, new Vector2(tile.X + 1, tile.Y));
            this.DoBombableCheck(location, new Vector2(tile.X - 1, tile.Y));
            this.DoBombableCheck(location, new Vector2(tile.X, tile.Y + 1));
            this.DoBombableCheck(location, new Vector2(tile.X, tile.Y - 1));
            return true;
        }
    }
}
