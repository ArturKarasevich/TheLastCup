using System;

namespace TheLastCup.Data
{
    [Serializable]
    public class TalkOutcomeData
    {
        public string id;
        public string title;
        public string tag;
        public string summaryText;
        public bool giveItem = true;
        public bool staysInside;
        public string endingRank;
    }

    [Serializable]
    public class TalkBranchData
    {
        public string guestLine;
        public string choiceAText;
        public string choiceBText;
        public TalkOutcomeData outcomeA;
        public TalkOutcomeData outcomeB;
    }

    [Serializable]
    public class GoodTalkTreeData
    {
        public string question;
        public string choice1AText;
        public string choice1BText;
        public TalkBranchData branchA;
        public TalkBranchData branchB;
        public TalkOutcomeData outcome1A;
        public TalkOutcomeData outcome1B;
    }

    [Serializable]
    public class GuestInfo
    {
        public string id;
        public string name;
        public string role;
        public bool isTwistGuest;
        public string introLines;
        public string[] wantedHerbs;
        public string[] keyWords;
        public string giveItem;
        public string itemId;
        public string badTalk;
        public string okTalk;
        public string goodTalk;
        public GoodTalkTreeData bestTalk;
    }

    [Serializable]
    public class GuestListData
    {
        public GuestInfo[] guests;
    }
}
