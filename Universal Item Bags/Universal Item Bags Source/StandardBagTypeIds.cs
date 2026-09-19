using System.Collections.Generic;

namespace ThaleMagnus.UniversalItemBags;

public static class StandardBagTypeIds
{
    public const string GemBag = "64fd96d5-b15f-40bb-a60f-181f57f597a0";
    public const string SmithingBag = "e5ccd506-99ac-4238-98ad-4df34f182143";
    public const string MineralBag = "7ccf7f7f-b406-4088-82b1-438164a39e13";
    public const string MiningBag = "4bbd80c6-fc49-4878-9061-7a41a9e25fbb";
    public const string ResourceBag = "be6830c4-9ceb-451a-a5ed-905db9c7cf3f";
    public const string ConstructionBag = "c5b27c87-7a7d-485b-ab63-95389b41ce65";
    public const string TreeBag = "bbdaf9f5-0389-4232-b466-97ac371d51e5";
    public const string AnimalProductsBag = "60b29c0d-1d2e-4433-ada9-1f981ab9c0c1";
    public const string RecyclingBag = "4582c416-eb5a-4a73-ae94-da1eb0cbe027";
    public const string LootBag = "07b31f0d-1cf3-4e59-b581-915b185e77a4";
    public const string ForagingBag = "040d414b-3a55-40d2-aa89-8121f4c0b387";
    public const string ArtifactBag = "c47bd42a-dcfd-4070-a268-adc91c13d727";
    public const string SeedBag = "7c79118b-09d3-4173-87f1-2809715e0983";
    public const string OceanFishBag = "66519acd-7f45-4091-b31f-b60997b3987e";
    public const string RiverFishBag = "74857d55-8889-4e62-b70e-05d4c7ae523d";
    public const string LakeFishBag = "9d23058a-ec74-4bdc-b118-547eeec6b002";
    public const string MiscFishBag = "64207326-abf1-49f8-a02e-c9d675dbc588";
    public const string FishBag = "62e478ee-9d5d-4b88-a34d-c9f490db8c6c";
    public const string FarmersBag = "49d045ab-47d8-47fc-aed0-745da4a6d8fa";
    public const string FoodBag = "f2d4a639-53ab-4124-a80d-c59b1ce67a4b";
    public const string CropBag = "f05f7a2a-1c68-4f87-9bc9-10a13856b9bc";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>
    {
        GemBag,
        SmithingBag,
        MineralBag,
        MiningBag,
        ResourceBag,
        ConstructionBag,
        TreeBag,
        AnimalProductsBag,
        RecyclingBag,
        LootBag,
        ForagingBag,
        ArtifactBag,
        SeedBag,
        OceanFishBag,
        RiverFishBag,
        LakeFishBag,
        MiscFishBag,
        FishBag,
        FarmersBag,
        FoodBag,
        CropBag
    };
}
