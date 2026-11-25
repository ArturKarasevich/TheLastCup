using System;

namespace TheLastCup.Data
{
    [Serializable]
    public class EndingTextsData
    {
        public string coldTitle = "ВЕЧНЫЙ СОН В СУГРОБЕ";
        public string coldText = "Топка погасла, буря взяла перевал. Ночной состав замер во льдах до самого утра.";
        public string paleDawnTitle = "ТЯЖЁЛЫЙ ПРОЛОМ";
        public string paleDawnText = "На последних остатках пара локомотив продавил лавину и вышел к бледному северному рассвету.";
        public string goldenDawnTitle = "ЗОЛОТОЙ РАССВЕТ";
        public string goldenDawnBestText = "Перевал пройден. Буря рассеялась, а вагон залил тёплый золотой свет чистого рассвета.";
        public string goldenDawnText = "Поезд прорвался сквозь лавину и встретил утреннее солнце на открытом перевале.";
    }

    [Serializable]
    public class SessionRulesData
    {
        public int slotsCount = 3;
        public int bestLight = 2;
        public int okLight = 1;
        public int badLight = 0;
        public string[] guestOrder;
        public EndingTextsData endings = new EndingTextsData();
    }
}
