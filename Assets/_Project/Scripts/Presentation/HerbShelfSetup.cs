using System.Collections.Generic;
using TheLastCup.Core;
using TheLastCup.Data;
using UnityEngine;

namespace TheLastCup.Presentation
{
    public class HerbShelfSetup : MonoBehaviour
    {
        [SerializeField] private float _jarSpacingX = 0.14f;
        [SerializeField] private float _jarSpacingZ = 0.12f;
        [SerializeField] private int _maxJarsPerRow = 2;

        private void Start()
        {
            GameDataStore registry = GameLauncher.CurrentGameData;
            if (registry == null || registry.HerbsList == null || registry.HerbsList.Count == 0)
                return;

            var jars = FindObjectsByType<HerbJar>(FindObjectsSortMode.None);
            var boundHerbIds = new HashSet<string>();
            var jarsBySlot = new Dictionary<TeaSlotType, List<HerbJar>>();
            HerbJar anyTemplate = null;

            foreach (var jar in jars)
            {
                if (!anyTemplate) anyTemplate = jar;

                HerbInfo matched = registry.GetHerb(jar.HerbId);
                if (matched != null)
                {
                    jar.Setup(matched);
                    boundHerbIds.Add(matched.id);

                    List<HerbJar> list;
                    if (!jarsBySlot.TryGetValue(matched.SlotType, out list))
                    {
                        list = new List<HerbJar>();
                        jarsBySlot[matched.SlotType] = list;
                    }
                    list.Add(jar);
                }
            }

            foreach (var list in jarsBySlot.Values)
                list.Sort(CompareJarsByX);

            var modifiedSlots = new HashSet<TeaSlotType>();
            int fallbackIndex = 0;

            foreach (var herb in registry.HerbsList)
            {
                if (herb == null || boundHerbIds.Contains(herb.id))
                    continue;

                List<HerbJar> slotJars;
                jarsBySlot.TryGetValue(herb.SlotType, out slotJars);
                HerbJar template = slotJars != null && slotJars.Count > 0 ? slotJars[0] : anyTemplate;
                if (!template) continue;

                var clone = Instantiate(template, template.transform.parent);
                clone.Setup(herb);
                boundHerbIds.Add(herb.id);

                if (slotJars != null && slotJars.Count > 0)
                {
                    slotJars.Add(clone);
                    modifiedSlots.Add(herb.SlotType);
                }
                else
                {
                    int perRow = Mathf.Max(1, _maxJarsPerRow);
                    int col = fallbackIndex % perRow;
                    int row = fallbackIndex / perRow;
                    fallbackIndex++;

                    float parentScaleX = Mathf.Max(0.0001f, Mathf.Abs(template.transform.parent ? template.transform.parent.lossyScale.x : 1f));
                    float parentScaleZ = Mathf.Max(0.0001f, Mathf.Abs(template.transform.parent ? template.transform.parent.lossyScale.z : 1f));
                    clone.transform.localPosition = template.transform.localPosition
                                                    + Vector3.right * (_jarSpacingX / parentScaleX * (col + 1))
                                                    + Vector3.forward * (_jarSpacingZ / parentScaleZ * row);
                }
            }

            foreach (var slot in modifiedSlots)
            {
                var slotJars = jarsBySlot[slot];
                if (slotJars.Count < 2) continue;

                int count = slotJars.Count;
                Vector3 startPos = slotJars[0].transform.localPosition;
                Vector3 endPos = slotJars[count - 1].transform.localPosition;
                for (int k = count - 1; k >= 1; k--)
                {
                    if ((slotJars[k].transform.localPosition - startPos).sqrMagnitude >= 0.000001f)
                    {
                        endPos = slotJars[k].transform.localPosition;
                        break;
                    }
                }
                if ((endPos - startPos).sqrMagnitude < 0.000001f)
                {
                    float parentScaleX = 1f;
                    if (slotJars[0].transform.parent != null)
                        parentScaleX = Mathf.Max(0.0001f, Mathf.Abs(slotJars[0].transform.parent.lossyScale.x));
                    endPos = startPos + Vector3.right * (_jarSpacingX / parentScaleX);
                }

                for (int i = 0; i < count; i++)
                {
                    float t = count > 1 ? (float)i / (count - 1) : 0f;
                    slotJars[i].transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                }
            }
        }

        private int CompareJarsByX(HerbJar a, HerbJar b)
        {
            return a.transform.localPosition.x.CompareTo(b.transform.localPosition.x);
        }
    }
}
