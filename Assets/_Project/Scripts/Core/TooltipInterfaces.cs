using TheLastCup.Data;

namespace TheLastCup.Core
{
    public interface IHintText
    {
        string GetFirstGuestHint();
    }

    public interface ITeapotInfo
    {
        HerbInfo[] Slots { get; }
        bool HasHotWater { get; }
    }
}
