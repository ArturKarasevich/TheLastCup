using System;
using System.Collections.Generic;
using TheLastCup.Data;

namespace TheLastCup.Domain
{
    public enum ItemType
    {
        None,
        Whistle,
        TuningFork,
        Magnifier
    }

    public static class ItemIds
    {
        public const string Whistle = "whistle";
        public const string TuningFork = "tuning_fork";
        public const string Magnifier = "loupe";

        public static ItemType FromString(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return ItemType.None;

            switch (itemId.Trim().ToLowerInvariant())
            {
                case Whistle: return ItemType.Whistle;
                case TuningFork: return ItemType.TuningFork;
                case Magnifier: return ItemType.Magnifier;
                default: return ItemType.None;
            }
        }
    }

    public class ProgressManager
    {
        public int Light { get; private set; }
        public bool HasStoryTwist { get; private set; }

        public bool OwnsWhistle { get; private set; }
        public bool IsWhistleUsed { get; private set; }

        public bool OwnsTuningFork { get; private set; }
        public bool IsTuningForkUsedForGuest { get; private set; }

        public bool OwnsMagnifier { get; private set; }
        public bool IsMagnifierUsed { get; private set; }

        private readonly HashSet<string> _knownHerbSecrets = new HashSet<string>();
        private readonly Dictionary<string, TalkOutcomeData> _guestResults = new Dictionary<string, TalkOutcomeData>();
        private readonly Dictionary<string, int> _guestTeaScores = new Dictionary<string, int>();

        public HashSet<string> KnownHerbSecrets { get { return _knownHerbSecrets; } }
        public Dictionary<string, TalkOutcomeData> GuestResults { get { return _guestResults; } }

        public void SaveTeaScore(string guestId, int matchCount)
        {
            if (!string.IsNullOrEmpty(guestId))
                _guestTeaScores[guestId] = matchCount;
        }

        public int GetTeaScore(string guestId)
        {
            if (string.IsNullOrEmpty(guestId)) return -1;
            int score;
            if (_guestTeaScores.TryGetValue(guestId, out score))
                return score;
            return -1;
        }

        public void SaveGuestResult(string guestId, TalkOutcomeData result)
        {
            if (!string.IsNullOrEmpty(guestId) && result != null)
                _guestResults[guestId] = result;
        }

        public void AddLight(int amount)
        {
            if (amount > 0)
                Light += amount;
        }

        public void StartStoryTwist()
        {
            HasStoryTwist = true;
        }

        public void RevealHerbSecrets(IEnumerable<string> herbIds)
        {
            if (herbIds == null) return;

            foreach (var herbId in herbIds)
            {
                if (!string.IsNullOrEmpty(herbId))
                    _knownHerbSecrets.Add(herbId);
            }
        }

        public bool KnowsHerbSecret(string herbId)
        {
            return !string.IsNullOrEmpty(herbId) && _knownHerbSecrets.Contains(herbId);
        }

        public void GiveItem(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Whistle:
                    OwnsWhistle = true;
                    break;
                case ItemType.TuningFork:
                    OwnsTuningFork = true;
                    break;
                case ItemType.Magnifier:
                    OwnsMagnifier = true;
                    break;
            }
        }

        public void GiveItem(string itemId)
        {
            GiveItem(ItemIds.FromString(itemId));
        }

        public bool CanUseWhistle(GuestInfo currentGuest)
        {
            return OwnsWhistle && !IsWhistleUsed && currentGuest != null && !currentGuest.isTwistGuest;
        }

        public bool UseWhistle()
        {
            if (!OwnsWhistle || IsWhistleUsed) return false;
            IsWhistleUsed = true;
            return true;
        }

        public bool CanUseTuningFork()
        {
            return OwnsTuningFork && !IsTuningForkUsedForGuest;
        }

        public bool UseTuningFork()
        {
            if (!CanUseTuningFork()) return false;
            IsTuningForkUsedForGuest = true;
            return true;
        }

        public bool CanUseMagnifier()
        {
            return OwnsMagnifier && !IsMagnifierUsed;
        }

        public bool UseMagnifier(string herbId)
        {
            if (!CanUseMagnifier() || string.IsNullOrEmpty(herbId)) return false;
            IsMagnifierUsed = true;
            _knownHerbSecrets.Add(herbId);
            return true;
        }

        public void ResetForNextGuest()
        {
            IsTuningForkUsedForGuest = false;
        }
    }
}
