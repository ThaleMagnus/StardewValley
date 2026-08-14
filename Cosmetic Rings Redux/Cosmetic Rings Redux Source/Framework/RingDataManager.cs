using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;
using System.Collections.Generic;
using System.Linq;

namespace ThaleTheGreat.CosmeticRingsRedux.Framework
{
    internal static class RingDataManager
    {
        internal const string TextureAssetName = "Mods/ThaleTheGreat.CosmeticRingsRedux/Rings";

        private const string HatMouseShopId = "HatMouse";
        private const int PurchasePrice = 100;

        private static readonly RingDefinition[] Rings =
        {
            new RingDefinition("ThaleTheGreat.CosmeticRingsRedux.Rings.PetalRing", "ring.petal.name", "ring.petal.description", 0),
            new RingDefinition("ThaleTheGreat.CosmeticRingsRedux.Rings.ButterflyRing", "ring.butterfly.name", "ring.butterfly.description", 1),
            new RingDefinition("ThaleTheGreat.CosmeticRingsRedux.Rings.FairyRing", "ring.fairy.name", "ring.fairy.description", 2),
            new RingDefinition("ThaleTheGreat.CosmeticRingsRedux.Rings.RaindropRing", "ring.raindrop.name", "ring.raindrop.description", 3),
            new RingDefinition("ThaleTheGreat.CosmeticRingsRedux.Rings.BunnyRing", "ring.bunny.name", "ring.bunny.description", 4),
            new RingDefinition("ThaleTheGreat.CosmeticRingsRedux.Rings.JunimoRing", "ring.junimo.name", "ring.junimo.description", 5),
            new RingDefinition("ThaleTheGreat.CosmeticRingsRedux.Rings.SlimeRing", "ring.slime.name", "ring.slime.description", 6),
            new RingDefinition("ThaleTheGreat.CosmeticRingsRedux.Rings.FireflyRing", "ring.firefly.name", "ring.firefly.description", 7),
            new RingDefinition("ThaleTheGreat.CosmeticRingsRedux.Rings.FrogRing", "ring.frog.name", "ring.frog.description", 8),
            new RingDefinition("ThaleTheGreat.CosmeticRingsRedux.Rings.DustRing", "ring.dust.name", "ring.dust.description", 9),
            new RingDefinition("ThaleTheGreat.CosmeticRingsRedux.Rings.BatRing", "ring.bat.name", "ring.bat.description", 10)
        };

        internal static void HandleAssetRequested(AssetRequestedEventArgs e, System.Func<string, string> translate)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(TextureAssetName))
            {
                e.LoadFromModFile<Texture2D>("assets/Rings.png", AssetLoadPriority.Medium);
                return;
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    IDictionary<string, ObjectData> data = asset.AsDictionary<string, ObjectData>().Data;
                    foreach (RingDefinition ring in Rings)
                    {
                        data[ring.ItemId] = new ObjectData
                        {
                            Name = ring.ItemId,
                            DisplayName = translate(ring.DisplayNameKey),
                            Description = translate(ring.DescriptionKey),
                            Type = "Ring",
                            Category = -96,
                            Price = 0,
                            Edibility = -300,
                            Texture = TextureAssetName,
                            SpriteIndex = ring.SpriteIndex
                        };
                    }
                });
                return;
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
            {
                e.Edit(asset =>
                {
                    IDictionary<string, ShopData> shops = asset.AsDictionary<string, ShopData>().Data;
                    if (!shops.TryGetValue(HatMouseShopId, out ShopData shop))
                        return;

                    shop.Items ??= new List<ShopItemData>();
                    foreach (RingDefinition ring in Rings)
                    {
                        string entryId = $"ThaleTheGreat.CosmeticRingsRedux.HatMouse.{ring.ItemId}";
                        ShopItemData item = shop.Items.FirstOrDefault(entry => entry.Id == entryId);
                        if (item == null)
                        {
                            shop.Items.Add(new ShopItemData
                            {
                                Id = entryId,
                                ItemId = $"(O){ring.ItemId}",
                                Price = PurchasePrice
                            });
                        }
                        else
                        {
                            item.ItemId = $"(O){ring.ItemId}";
                            item.Price = PurchasePrice;
                        }
                    }
                });
            }
        }

        private sealed class RingDefinition
        {
            internal string ItemId { get; }
            internal string DisplayNameKey { get; }
            internal string DescriptionKey { get; }
            internal int SpriteIndex { get; }

            internal RingDefinition(string itemId, string displayNameKey, string descriptionKey, int spriteIndex)
            {
                ItemId = itemId;
                DisplayNameKey = displayNameKey;
                DescriptionKey = descriptionKey;
                SpriteIndex = spriteIndex;
            }
        }
    }
}
