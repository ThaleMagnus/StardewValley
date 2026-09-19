using System;
using System.Collections.Generic;

namespace ThaleMagnus.UniversalItemBags.Discovery;

internal static class ClassificationRules
{
    private const int GemCategory = -2;
    private const int FishCategory = -4;
    private const int EggCategory = -5;
    private const int MilkCategory = -6;
    private const int CookingCategory = -7;
    private const int MineralCategory = -12;
    private const int MeatCategory = -14;
    private const int MetalResourceCategory = -15;
    private const int BuildingResourceCategory = -16;
    private const int SellAtPierresAndMarniesCategory = -18;
    private const int FertilizerCategory = -19;
    private const int JunkCategory = -20;
    private const int BaitCategory = -21;
    private const int TackleCategory = -22;
    private const int SellAtFishShopCategory = -23;
    private const int CookingIngredientCategory = -25;
    private const int ArtisanGoodsCategory = -26;
    private const int SyrupCategory = -27;
    private const int MonsterLootCategory = -28;
    private const int SeedsCategory = -74;
    private const int VegetableCategory = -75;
    private const int FruitsCategory = -79;
    private const int FlowersCategory = -80;
    private const int GreensCategory = -81;
    private const int LitterCategory = -999;

    private static readonly IReadOnlySet<int> QualityCategories = new HashSet<int>
    {
        FishCategory,
        EggCategory,
        MilkCategory,
        MeatCategory,
        SellAtPierresAndMarniesCategory,
        ArtisanGoodsCategory,
        VegetableCategory,
        FruitsCategory,
        FlowersCategory,
        GreensCategory
    };

    public static ItemClassification Classify(
        ItemCandidate candidate,
        IReadOnlySet<string> cookingIngredientIds,
        IReadOnlySet<int> cookingIngredientCategories
    )
    {
        IReadOnlySet<string> tags = candidate.ContextTags;
        HashSet<string> bags = new(StringComparer.OrdinalIgnoreCase);

        bool isForage = HasAnyTag(tags, "forage_item");
        bool isFruitTreeItem = HasAnyTag(tags, "fruit_tree_item");
        bool isTreeSeed = HasAnyTag(tags, "tree_seed_item");
        bool isAnimalProduct = candidate.Category is EggCategory or MilkCategory or MeatCategory or SellAtPierresAndMarniesCategory
            || HasAnyTag(
                tags,
                "egg_item",
                "milk_item",
                "cow_milk_item",
                "goat_milk_item",
                "large_egg_item",
                "large_milk_item",
                "mayo_item",
                "honey_item",
                "slime_egg_item"
            );
        bool isFish = candidate.Category is FishCategory or BaitCategory or TackleCategory or SellAtFishShopCategory
            || HasAnyTag(tags, "fish_ocean", "fish_river", "fish_lake", "fish_freshwater", "fish_crab_pot", "fish_nonfish", "algae_item");
        bool isArtifact = string.Equals(candidate.ObjectType, "Arch", StringComparison.OrdinalIgnoreCase)
            || HasAnyTag(
                tags,
                "fossil_item",
                "bone_item",
                "ancient_item",
                "dwarvish_item",
                "elvish_item",
                "prehistoric_item",
                "scroll_item",
                "golden_relic_item",
                "doll_item",
                "strange_doll_1",
                "strange_doll_2"
            );
        bool isFood = candidate.Category is CookingCategory or CookingIngredientCategory
            || cookingIngredientIds.Contains(candidate.ItemId)
            || cookingIngredientCategories.Contains(candidate.Category)
            || HasAnyTag(
                tags,
                "cooking_item",
                "medicine_item",
                "potion_item",
                "drink_item",
                "jelly_item",
                "juice_item",
                "pickle_item",
                "alcohol_item",
                "food_bakery",
                "food_breakfast",
                "food_cake",
                "food_party",
                "food_pasta",
                "food_salad",
                "food_sauce",
                "food_seafood",
                "food_soup",
                "food_spicy",
                "food_sushi",
                "food_sweet"
            );

        switch (candidate.Category)
        {
            case GemCategory:
                bags.Add(StandardBagTypeIds.GemBag);
                bags.Add(StandardBagTypeIds.MiningBag);
                break;

            case MineralCategory:
                bags.Add(StandardBagTypeIds.MineralBag);
                bags.Add(StandardBagTypeIds.MiningBag);
                break;

            case MetalResourceCategory:
                bags.Add(StandardBagTypeIds.SmithingBag);
                bags.Add(StandardBagTypeIds.MiningBag);
                bags.Add(StandardBagTypeIds.ResourceBag);
                break;

            case BuildingResourceCategory:
                bags.Add(StandardBagTypeIds.ResourceBag);
                bags.Add(StandardBagTypeIds.ConstructionBag);
                break;

            case EggCategory:
            case MilkCategory:
            case MeatCategory:
            case SellAtPierresAndMarniesCategory:
                bags.Add(StandardBagTypeIds.AnimalProductsBag);
                break;

            case FertilizerCategory:
                bags.Add(StandardBagTypeIds.FarmersBag);
                break;

            case JunkCategory:
            case LitterCategory:
                bags.Add(StandardBagTypeIds.RecyclingBag);
                break;

            case MonsterLootCategory:
                bags.Add(StandardBagTypeIds.LootBag);
                break;

            case SeedsCategory:
                bags.Add(isTreeSeed || isFruitTreeItem ? StandardBagTypeIds.TreeBag : StandardBagTypeIds.SeedBag);
                break;

            case SyrupCategory:
                bags.Add(StandardBagTypeIds.TreeBag);
                break;

            case VegetableCategory:
            case FruitsCategory:
            case FlowersCategory:
                if (isForage)
                    bags.Add(StandardBagTypeIds.ForagingBag);
                else if (isFruitTreeItem)
                    bags.Add(StandardBagTypeIds.TreeBag);
                else
                    bags.Add(StandardBagTypeIds.CropBag);
                break;

            case GreensCategory:
                bags.Add(isForage ? StandardBagTypeIds.ForagingBag : StandardBagTypeIds.CropBag);
                break;

            case ArtisanGoodsCategory:
                if (isAnimalProduct)
                    bags.Add(StandardBagTypeIds.AnimalProductsBag);
                if (HasAnyTag(tags, "syrup_item"))
                    bags.Add(StandardBagTypeIds.TreeBag);
                if (isFood)
                    bags.Add(StandardBagTypeIds.FoodBag);
                break;
        }

        if (isAnimalProduct)
            bags.Add(StandardBagTypeIds.AnimalProductsBag);

        if (isForage)
            bags.Add(StandardBagTypeIds.ForagingBag);

        if (!isForage && !isFruitTreeItem && HasAnyTag(tags, "fruit_item", "flower_item"))
            bags.Add(StandardBagTypeIds.CropBag);

        if (isFruitTreeItem || isTreeSeed || HasAnyTag(tags, "wood_item", "syrup_item", "tapper_item"))
            bags.Add(StandardBagTypeIds.TreeBag);

        if (HasAnyTag(tags, "fertilizer_item", "quality_fertilizer_item", "crow_scare"))
            bags.Add(StandardBagTypeIds.FarmersBag);

        if (HasAnyTag(tags, "sign_item", "torch_item", "campfire_item"))
            bags.Add(StandardBagTypeIds.ConstructionBag);

        if (HasAnyTag(tags, "trash_item"))
            bags.Add(StandardBagTypeIds.RecyclingBag);

        if (HasAnyTag(tags, "ore_item"))
        {
            bags.Add(StandardBagTypeIds.SmithingBag);
            bags.Add(StandardBagTypeIds.MiningBag);
            bags.Add(StandardBagTypeIds.ResourceBag);
        }

        if (HasAnyTag(tags, "geode", "furnace_item"))
        {
            bags.Add(StandardBagTypeIds.SmithingBag);
            bags.Add(StandardBagTypeIds.MiningBag);
        }

        if (HasAnyTag(tags, "bomb_item"))
            bags.Add(StandardBagTypeIds.MiningBag);

        if (HasAnyTag(tags, "slime_item", "slime_egg_item"))
            bags.Add(StandardBagTypeIds.LootBag);

        if (HasAnyTag(tags, "totem_item"))
            bags.Add(StandardBagTypeIds.FarmersBag);

        if (isArtifact)
            bags.Add(StandardBagTypeIds.ArtifactBag);

        if (isFood)
            bags.Add(StandardBagTypeIds.FoodBag);

        if (isFish)
        {
            bags.Add(StandardBagTypeIds.FishBag);

            if (HasAnyTag(tags, "fish_ocean"))
                bags.Add(StandardBagTypeIds.OceanFishBag);

            if (HasAnyTag(tags, "fish_river"))
                bags.Add(StandardBagTypeIds.RiverFishBag);

            if (HasAnyTag(tags, "fish_lake"))
                bags.Add(StandardBagTypeIds.LakeFishBag);

            if (HasAnyTag(tags, "fish_crab_pot", "fish_nonfish", "algae_item"))
                bags.Add(StandardBagTypeIds.MiscFishBag);
        }

        bool hasQualities = !candidate.IsBigCraftable && SupportsQualities(candidate.Category);
        return bags.Count == 0
            ? ItemClassification.Empty
            : new ItemClassification(bags, hasQualities);
    }

    public static bool SupportsQualities(int category)
    {
        return QualityCategories.Contains(category);
    }

    private static bool HasAnyTag(IReadOnlySet<string> tags, params string[] expectedTags)
    {
        foreach (string expectedTag in expectedTags)
        {
            if (tags.Contains(expectedTag))
                return true;
        }

        return false;
    }
}
