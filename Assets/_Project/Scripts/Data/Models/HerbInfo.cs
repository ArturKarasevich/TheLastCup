using System;
using UnityEngine;

namespace TheLastCup.Data
{
    public enum TeaSlotType
    {
        Base = 0,
        Heart = 1,
        Aftertaste = 2
    }

    [Serializable]
    public class HerbInfo
    {
        public string id;
        public string name;
        public string slotType;
        public int slotIndex;
        public string colorHex;
        public string liquidColorHex;
        public string lookInfo;
        public string smellHint;
        public string slotName;
        public string essence;
        public string bodyEffect;
        public string moodEffect;
        public string taste;
        public string secretAuthor;
        public string secretNote;

        public TeaSlotType SlotType
        {
            get
            {
                if (slotType == "Base") return TeaSlotType.Base;
                if (slotType == "Heart") return TeaSlotType.Heart;
                if (slotType == "Aftertaste") return TeaSlotType.Aftertaste;
                return (TeaSlotType)slotIndex;
            }
        }

        public Color GetTeaColor()
        {
            Color color;
            if (ColorUtility.TryParseHtmlString(liquidColorHex, out color))
                return color;
            return Color.white;
        }
    }

    [Serializable]
    public class HerbListData
    {
        public HerbInfo[] herbs;
    }
}
