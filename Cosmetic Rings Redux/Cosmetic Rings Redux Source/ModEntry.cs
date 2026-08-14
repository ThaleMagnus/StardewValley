using ThaleTheGreat.CosmeticRingsRedux.Framework;
using ThaleTheGreat.CosmeticRingsRedux.Framework.Interfaces;
using ThaleTheGreat.CosmeticRingsRedux.Framework.Patches;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ThaleTheGreat.CosmeticRingsRedux
{
    public sealed class ModEntry : Mod
    {
        internal static ModConfig Config { get; private set; }

        private IWearMoreRingsApi wearMoreRingsApi;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();

            ResourceManager.SetUpAssets(helper);

            try
            {
                Harmony harmony = new Harmony(ModManifest.UniqueID);
                new RingPatch().Apply(harmony);
                new UtilityPatch().Apply(harmony);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply required Harmony patches.\n{ex}", LogLevel.Error);
                return;
            }

            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Content.LocaleChanged += OnLocaleChanged;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.GameLoop.Saved += OnSaved;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdateTicked;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            IGenericModConfigMenuAPI gmcm = ApiManager.GetGenericModConfigMenuApi(Helper);
            if (gmcm != null)
            {
                gmcm.Register(
                    ModManifest,
                    () => Config = new ModConfig(),
                    () => Helper.WriteConfig(Config)
                );
                gmcm.AddNumberOption(
                    ModManifest,
                    () => Config.walkingSpeed,
                    value => Config.walkingSpeed = value,
                    () => Helper.Translation.Get("config.walking-speed.name").ToString(),
                    min: 1,
                    max: 8,
                    interval: 1,
                    fieldId: "walkingSpeed"
                );
            }

            wearMoreRingsApi = ApiManager.GetWearMoreRingsApi(Helper);
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            RingDataManager.HandleAssetRequested(e, key => Helper.Translation.Get(key).ToString());
        }

        private void OnLocaleChanged(object sender, LocaleChangedEventArgs e)
        {
            Helper.GameContent.InvalidateCache("Data/Objects");
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            PurgeCustomFollowers();
            LoadEquippedRings();
        }

        private void OnSaving(object sender, SavingEventArgs e)
        {
            RingManager.RemoveAllEffects();
            PurgeCustomFollowers();
        }

        private void OnSaved(object sender, SavedEventArgs e)
        {
            LoadEquippedRings();
        }

        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            RingManager.RemoveAllEffects();
            RingManager.Reset();
        }

        private void OnOneSecondUpdateTicked(object sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.player?.currentLocation == null)
                return;

            RingManager.UpdateRingEffects(Game1.player, Game1.player.currentLocation);
        }

        private void LoadEquippedRings()
        {
            if (!Context.IsWorldReady || Game1.player?.currentLocation == null)
                return;

            RingManager.LoadWornRings(Game1.player, Game1.player.currentLocation, GetEquippedRings());
        }

        private IEnumerable<Ring> GetEquippedRings()
        {
            if (wearMoreRingsApi != null)
            {
                int slotCount = wearMoreRingsApi.RingSlotCount();
                for (int slot = 0; slot < slotCount; slot++)
                {
                    Ring ring = wearMoreRingsApi.GetRing(slot);
                    if (ring != null)
                        yield return ring;
                }

                yield break;
            }

            if (Game1.player.leftRing.Value != null)
                yield return Game1.player.leftRing.Value;
            if (Game1.player.rightRing.Value != null)
                yield return Game1.player.rightRing.Value;
        }

        private static void PurgeCustomFollowers()
        {
            foreach (GameLocation location in Game1.locations.Where(location => location != null))
            {
                if (location.critters != null)
                {
                    foreach (var critter in location.critters.Where(IsCustomFollower).ToList())
                        location.critters.Remove(critter);
                }

                foreach (NPC character in location.characters.Where(IsCustomFollower).ToList())
                    location.characters.Remove(character);
            }
        }

        internal static bool IsCustomFollower(object follower)
        {
            return follower?.GetType().Namespace == typeof(ModEntry).Namespace + ".Framework.Critters";
        }
    }
}
