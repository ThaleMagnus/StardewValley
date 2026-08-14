using StardewModdingAPI;
using System;
using System.Collections.Generic;

namespace ThaleTheGreat.UniversalItemBags.Integration;

public interface IJsonAssetsApi
{
    void LoadAssets(string path);

    void LoadAssets(string path, ITranslationHelper translations);

    string GetObjectId(string name);

    string GetBigCraftableId(string name);

    List<string> GetAllObjectsFromContentPack(string contentPackId);

    List<string> GetAllBigCraftablesFromContentPack(string contentPackId);

    event EventHandler ItemsRegistered;
}
