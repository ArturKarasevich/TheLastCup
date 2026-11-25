using System.Collections.Generic;
using UnityEngine;

namespace TheLastCup.Data
{
    public class GameDataStore
    {
        private readonly Dictionary<string, HerbInfo> _herbs = new Dictionary<string, HerbInfo>();
        private readonly List<HerbInfo> _herbsList = new List<HerbInfo>();
        private readonly Dictionary<string, GuestInfo> _guests = new Dictionary<string, GuestInfo>();

        public SessionRulesData SessionRules { get; set; } = new SessionRulesData();
        public Dictionary<string, HerbInfo> Herbs { get { return _herbs; } }
        public List<HerbInfo> HerbsList { get { return _herbsList; } }
        public Dictionary<string, GuestInfo> Guests { get { return _guests; } }

        public void LoadAllData(JsonLoader loader, DataChecker validator)
        {
            _herbs.Clear();
            _herbsList.Clear();
            _guests.Clear();

            var herbCollection = loader.LoadFromStreamingAssets<HerbListData>("herbs.json");
            List<string> herbErrors;
            if (!validator.CheckHerbList(herbCollection, out herbErrors))
            {
                foreach (var err in herbErrors)
                    Debug.LogWarning($"[GameDataStore] Herb collection issue: {err}");
            }

            if (herbCollection != null && herbCollection.herbs != null)
            {
                foreach (var herb in herbCollection.herbs)
                {
                    if (!validator.CheckHerb(herb, out string err))
                    {
                        Debug.LogWarning($"[GameDataStore] Skipped invalid herb: {err}");
                        continue;
                    }

                    if (_herbs.ContainsKey(herb.id))
                    {
                        Debug.LogWarning($"[GameDataStore] Skipped duplicate herb ID: '{herb.id}'");
                        continue;
                    }

                    _herbs[herb.id] = herb;
                    _herbsList.Add(herb);
                }
            }

            var spiritCollection = loader.LoadFromStreamingAssets<GuestListData>("spirits.json");
            if (spiritCollection != null && spiritCollection.guests != null)
            {
                foreach (var spirit in spiritCollection.guests)
                {
                    if (!validator.CheckGuest(spirit, this, out string err))
                    {
                        Debug.LogWarning($"[GameDataStore] Skipped invalid spirit: {err}");
                        continue;
                    }

                    if (_guests.ContainsKey(spirit.id))
                    {
                        Debug.LogWarning($"[GameDataStore] Skipped duplicate spirit ID: '{spirit.id}'");
                        continue;
                    }

                    _guests[spirit.id] = spirit;
                }
            }

            var config = loader.LoadFromStreamingAssets<SessionRulesData>("night_config.json");
            if (config != null)
            {
                List<string> configErrors;
                if (!validator.CheckSessionRules(config, this, out configErrors))
                {
                    foreach (var err in configErrors)
                        Debug.LogWarning($"[GameDataStore] SessionRulesData issue: {err}");
                }

                if (config.slotsCount < 3) config.slotsCount = 3;
                if (config.bestLight < 0) config.bestLight = 2;
                if (config.okLight < 0) config.okLight = 1;
                if (config.badLight < 0) config.badLight = 0;

                SessionRules = config;
            }
        }

        public HerbInfo GetHerb(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            HerbInfo herb;
            if (_herbs.TryGetValue(id, out herb))
                return herb;
            return null;
        }

        public GuestInfo GetGuest(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            GuestInfo guest;
            if (_guests.TryGetValue(id, out guest))
                return guest;
            return null;
        }
    }
}
