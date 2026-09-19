using StardewValley.Objects;

namespace ThaleMagnus.CosmeticRingsRedux.Framework.Interfaces
{
    public interface IWearMoreRingsApi
    {
        int RingSlotCount();

        Ring GetRing(int slot);

        void SetRing(int slot, Ring ring);
    }
}
