using System;
using TheLastCup.Data;

namespace TheLastCup.Domain
{
    public class TeaResult
    {
        public int MatchCount;
        public bool IsBaseMatched;
        public bool IsHeartMatched;
        public bool IsAftertasteMatched;
        public int RewardLight;
        public bool IsWhistleReady;
    }

    public class TeaMaker
    {
        private readonly HerbInfo[] _cupSlots;

        public TeaMaker(int slotsCount = 3)
        {
            _cupSlots = new HerbInfo[slotsCount > 0 ? slotsCount : 3];
        }

        public HerbInfo[] CupSlots
        {
            get { return _cupSlots; }
        }

        public bool IsCupFull
        {
            get
            {
                for (int i = 0; i < _cupSlots.Length; i++)
                {
                    if (_cupSlots[i] == null)
                        return false;
                }
                return _cupSlots.Length > 0;
            }
        }

        public bool AddHerb(HerbInfo herb)
        {
            if (herb == null)
                return false;

            int targetSlot = (int)herb.SlotType;
            if (targetSlot < 0 || targetSlot >= _cupSlots.Length)
                return false;

            _cupSlots[targetSlot] = herb;
            return true;
        }

        public void RemoveHerbAt(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _cupSlots.Length)
                _cupSlots[slotIndex] = null;
        }

        public void EmptyCup()
        {
            Array.Clear(_cupSlots, 0, _cupSlots.Length);
        }

        public TeaResult CheckTea(GuestInfo guest, SessionRulesData rules = null)
        {
            var result = new TeaResult();
            if (guest == null)
                return result;

            result.IsBaseMatched = _cupSlots.Length > 0 && IsSlotMatching(_cupSlots[0], guest, 0);
            result.IsHeartMatched = _cupSlots.Length > 1 && IsSlotMatching(_cupSlots[1], guest, 1);
            result.IsAftertasteMatched = _cupSlots.Length > 2 && IsSlotMatching(_cupSlots[2], guest, 2);

            if (result.IsBaseMatched) result.MatchCount++;
            if (result.IsHeartMatched) result.MatchCount++;
            if (result.IsAftertasteMatched) result.MatchCount++;

            int bestReward = rules != null ? rules.bestLight : 2;
            int normalReward = rules != null ? rules.okLight : 1;
            int lowReward = rules != null ? rules.badLight : 0;

            if (result.MatchCount >= 3) result.RewardLight = bestReward;
            else if (result.MatchCount == 2) result.RewardLight = normalReward;
            else result.RewardLight = lowReward;

            return result;
        }

        private bool IsSlotMatching(HerbInfo herb, GuestInfo guest, int slotIndex)
        {
            if (herb == null)
                return false;

            if (guest.wantedHerbs != null && slotIndex < guest.wantedHerbs.Length)
            {
                if (string.Equals(herb.id, guest.wantedHerbs[slotIndex], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            if (guest.wantedTags != null && slotIndex < guest.wantedTags.Length && herb.tags != null)
            {
                string wantedTag = guest.wantedTags[slotIndex];
                foreach (var herbTag in herb.tags)
                {
                    if (string.Equals(herbTag, wantedTag, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }
    }
}
