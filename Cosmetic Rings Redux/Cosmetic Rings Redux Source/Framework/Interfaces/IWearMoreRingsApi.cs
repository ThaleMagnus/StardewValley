using StardewValley.Objects;

namespace ThaleTheGreat.CosmeticRingsRedux.Framework.Interfaces
{
    public interface IWearMoreRingsApi
    {
        int RingSlotCount();

        Ring GetRing(int slot);

        void SetRing(int slot, Ring ring);
    }
}
