using System.Collections.Generic;

namespace TheLastCup.Data
{
    public class DataChecker
    {
        public bool CheckHerb(HerbInfo herb, out string error)
        {
            if (herb == null)
            {
                error = "Herb is null";
                return false;
            }

            if (string.IsNullOrWhiteSpace(herb.id))
            {
                error = "Herb ID cannot be empty";
                return false;
            }

            if (herb.slotIndex < 0 || herb.slotIndex > 2)
            {
                error = $"Herb '{herb.id}' has invalid slot index: {herb.slotIndex}";
                return false;
            }

            error = null;
            return true;
        }

        public bool CheckHerbList(HerbListData collection, out List<string> errors)
        {
            errors = new List<string>();

            if (collection == null || collection.herbs == null)
            {
                errors.Add("Herb collection is empty or null");
                return false;
            }

            var ids = new List<string>();
            foreach (var herb in collection.herbs)
            {
                string err;
                if (!CheckHerb(herb, out err))
                    errors.Add(err);

                if (herb != null && !string.IsNullOrEmpty(herb.id))
                {
                    if (ids.Contains(herb.id))
                        errors.Add($"Duplicate herb ID: {herb.id}");
                    else
                        ids.Add(herb.id);
                }
            }

            return errors.Count == 0;
        }

        public bool CheckGuest(GuestInfo spirit, GameDataStore registry, out string error)
        {
            if (spirit == null)
            {
                error = "Spirit is null";
                return false;
            }

            if (string.IsNullOrWhiteSpace(spirit.id))
            {
                error = "Spirit ID cannot be empty";
                return false;
            }

            if (spirit.wantedHerbs == null || spirit.wantedHerbs.Length != 3)
            {
                error = $"Spirit '{spirit.id}' must have 3 wantedHerbs";
                return false;
            }

            if (registry != null)
            {
                foreach (var herbId in spirit.wantedHerbs)
                {
                    if (string.IsNullOrWhiteSpace(herbId) || !registry.Herbs.ContainsKey(herbId))
                    {
                        error = $"Spirit '{spirit.id}' references unknown or empty herb ID: '{herbId}'";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        public bool CheckSessionRules(SessionRulesData config, GameDataStore registry, out List<string> errors)
        {
            errors = new List<string>();

            if (config == null)
            {
                errors.Add("SessionRulesData is null");
                return false;
            }

            if (config.slotsCount < 3)
                errors.Add($"SessionRulesData has invalid slotsCount: {config.slotsCount} (must be >= 3)");

            if (config.guestOrder == null || config.guestOrder.Length == 0)
            {
                errors.Add("SessionRulesData guestOrder is empty");
            }
            else if (registry != null)
            {
                foreach (var spiritId in config.guestOrder)
                {
                    if (string.IsNullOrWhiteSpace(spiritId) || !registry.Guests.ContainsKey(spiritId))
                        errors.Add($"SessionRulesData references unknown or empty spirit ID: '{spiritId}'");
                }
            }

            return errors.Count == 0;
        }
    }
}
