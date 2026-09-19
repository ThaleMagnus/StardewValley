using System;
using Microsoft.Xna.Framework;

namespace ThaleMagnus.WalletTools;

public interface ISpecialPowerAPI
{
    bool RegisterPowerCategory(string uniqueID, Func<string> displayName, string iconTexture);
    bool RegisterPowerCategory(string uniqueID, Func<string> displayName, string iconTexture, Point sourceRectPosition, Point sourceRectSize);
}
