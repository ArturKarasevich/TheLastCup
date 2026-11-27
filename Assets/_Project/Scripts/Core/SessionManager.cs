using TheLastCup.Data;
using TheLastCup.Domain;
using UnityEngine;

namespace TheLastCup.Core
{
    public class SessionManager : MonoBehaviour
    {
        private GameDataStore _gameData;
        private ProgressManager _progress;
        private int _guestIndex;
        private GuestInfo _currentGuest;
        private bool _isSessionOver;

        public GameDataStore GameData { get { return _gameData; } }
        public ProgressManager Progress { get { return _progress; } }
        public GuestInfo CurrentGuest { get { return _currentGuest; } }

        public void Setup(GameDataStore gameData, ProgressManager progress)
        {
            _gameData = gameData;
            _progress = progress;
            _guestIndex = 0;
            _isSessionOver = false;

            StartCurrentGuest();
        }

        private void OnEnable()
        {
            GameEvents.OnTeaSubmitted += OnTeaServed;
            GameEvents.OnGuestLeft += OnGuestLeft;
            GameEvents.OnWhistleRetryRequested += OnWhistleRetryClicked;
            GameEvents.OnGuestResultReached += OnGuestFinished;
            GameEvents.OnTuningForkSubmitted += CheckCupWithTuningFork;
            GameEvents.OnMagnifierRequested += OnMagnifierUsed;
        }

        private void OnDisable()
        {
            GameEvents.OnTeaSubmitted -= OnTeaServed;
            GameEvents.OnGuestLeft -= OnGuestLeft;
            GameEvents.OnWhistleRetryRequested -= OnWhistleRetryClicked;
            GameEvents.OnGuestResultReached -= OnGuestFinished;
            GameEvents.OnTuningForkSubmitted -= CheckCupWithTuningFork;
            GameEvents.OnMagnifierRequested -= OnMagnifierUsed;
        }

        private void OnWhistleRetryClicked()
        {
            RetryWithWhistle();
        }

        private void OnMagnifierUsed(string herbId)
        {
            InspectHerbWithMagnifier(herbId);
        }

        private void OnGuestFinished(string guestId, TalkOutcomeData result)
        {
            if (_progress != null)
                _progress.SaveGuestResult(guestId, result);

            if (result != null && result.giveItem && _currentGuest != null)
            {
                string artifactKey = !string.IsNullOrEmpty(_currentGuest.itemId)
                    ? _currentGuest.itemId
                    : _currentGuest.giveItem;
                if (!string.IsNullOrEmpty(artifactKey))
                {
                    if (_progress != null)
                        _progress.GiveItem(artifactKey);
                    GameEvents.UnlockItem(artifactKey);
                }
            }
        }

        private void StartCurrentGuest()
        {
            if (_gameData == null || _gameData.SessionRules == null || _gameData.SessionRules.guestOrder == null ||
                _guestIndex >= _gameData.SessionRules.guestOrder.Length)
            {
                _isSessionOver = true;
                GameEvents.SendSessionEnded(GetSessionSummary());
                GameEvents.FinishDemo();
                return;
            }

            string spiritId = _gameData.SessionRules.guestOrder[_guestIndex];
            _currentGuest = _gameData.GetGuest(spiritId);

            if (_currentGuest == null)
            {
                _guestIndex++;
                StartCurrentGuest();
                return;
            }

            _progress.ResetForNextGuest();

            if (_currentGuest.isTwistGuest && !_progress.HasStoryTwist)
            {
                _progress.StartStoryTwist();
                GameEvents.StartStoryTwist();
            }

            GameEvents.SetHintPulse(_guestIndex == 0);
            GameEvents.SendGuestArrived(_currentGuest);
        }

        private void OnTeaServed(TeaMaker teaMaker)
        {
            if (_isSessionOver || _currentGuest == null || teaMaker == null)
                return;

            SessionRulesData rules = _gameData != null ? _gameData.SessionRules : null;
            TeaResult result = teaMaker.CheckTea(_currentGuest, rules);
            _progress.SaveTeaScore(_currentGuest.id, result.MatchCount);
            result.IsWhistleReady = result.MatchCount <= 1 && _progress.CanUseWhistle(_currentGuest);

            if (result.RewardLight > 0)
            {
                _progress.AddLight(result.RewardLight);
                GameEvents.SendLightChanged(_progress.Light);
            }

            if (result.MatchCount >= 2 && _currentGuest.wantedHerbs != null)
            {
                _progress.RevealHerbSecrets(_currentGuest.wantedHerbs);
                foreach (var herbId in _currentGuest.wantedHerbs)
                    GameEvents.UnlockHerbSecret(herbId);
            }

            if (result.MatchCount == 3 && (_currentGuest.bestTalk == null || string.IsNullOrEmpty(_currentGuest.bestTalk.question)))
            {
                string artifactKey = !string.IsNullOrEmpty(_currentGuest.itemId)
                    ? _currentGuest.itemId
                    : _currentGuest.giveItem;
                if (!string.IsNullOrEmpty(artifactKey))
                {
                    _progress.GiveItem(artifactKey);
                    GameEvents.UnlockItem(artifactKey);
                }
            }

            GameEvents.SendTeaChecked(result, _currentGuest);
        }

        private void OnGuestLeft()
        {
            if (_isSessionOver) return;

            _guestIndex++;
            GameEvents.ResetTeaCycle();
            StartCurrentGuest();
        }

        public bool RetryWithWhistle()
        {
            if (_currentGuest == null || !_progress.CanUseWhistle(_currentGuest))
                return false;

            if (!_progress.UseWhistle())
                return false;

            _progress.ResetForNextGuest();
            GameEvents.ResetTeaCycle();
            GameEvents.SetHintPulse(_guestIndex == 0);
            GameEvents.SendGuestArrived(_currentGuest);
            GameEvents.ShowSubtitle("Свисток проводника вернул гостя!");
            return true;
        }

        public void CheckCupWithTuningFork(TeaMaker teaMaker)
        {
            if (_currentGuest == null || teaMaker == null || !teaMaker.IsCupFull)
                return;

            if (!_progress.UseTuningFork())
            {
                GameEvents.ShowSubtitle("Камертон уже использован для этого гостя.");
                return;
            }

            var preview = teaMaker.CheckTea(_currentGuest);
            int totalSlots = teaMaker.CupSlots != null ? teaMaker.CupSlots.Length : 3;
            GameEvents.ShowSubtitle($"Камертон звенит: {preview.MatchCount} из {totalSlots} нот в гармонии.");
        }

        public bool InspectHerbWithMagnifier(string herbId)
        {
            if (!_progress.UseMagnifier(herbId))
                return false;

            GameEvents.UnlockHerbSecret(herbId);
            GameEvents.ShowSubtitle("Треснутая лупа раскрыла скрытое свойство травы!");
            return true;
        }

        public void SaveGuestResult(string guestId, TalkOutcomeData result)
        {
            GameEvents.SendGuestResultReached(guestId, result);
        }

        public void SkipToGuest(string guestId)
        {
            if (_gameData == null || _gameData.SessionRules == null || _gameData.SessionRules.guestOrder == null) return;

            for (int i = 0; i < _gameData.SessionRules.guestOrder.Length; i++)
            {
                if (_gameData.SessionRules.guestOrder[i] == guestId)
                {
                    _isSessionOver = false;
                    _guestIndex = i;
                    GameEvents.ResetTeaCycle();
                    StartCurrentGuest();
                    return;
                }
            }
        }

        public void TestTeaCheck(int matchCount)
        {
            if (_currentGuest == null) return;

            var fakeService = new TeaMaker();
            for (int i = 0; i < 3; i++)
            {
                if (i < matchCount && _currentGuest.wantedHerbs != null && i < _currentGuest.wantedHerbs.Length)
                {
                    HerbInfo h = _gameData.GetHerb(_currentGuest.wantedHerbs[i]);
                    if (h != null) fakeService.AddHerb(h);
                }
            }

            OnTeaServed(fakeService);
        }

        public void TestGiveAllItems()
        {
            if (_progress != null)
            {
                _progress.GiveItem(ItemType.Whistle);
                _progress.GiveItem(ItemType.TuningFork);
                _progress.GiveItem(ItemType.Magnifier);
            }
            GameEvents.UnlockItem(ItemIds.Whistle);
            GameEvents.UnlockItem(ItemIds.TuningFork);
            GameEvents.UnlockItem(ItemIds.Magnifier);
        }

        private SessionSummary GetSessionSummary()
        {
            var summary = new SessionSummary
            {
                TotalLight = _progress != null ? _progress.Light : 0
            };

            string[] order = (_gameData != null && _gameData.SessionRules != null) ? _gameData.SessionRules.guestOrder : null;
            if (order != null)
            {
                foreach (string id in order)
                {
                    GuestInfo spirit = _gameData.GetGuest(id);
                    int matches = _progress != null ? _progress.GetTeaScore(id) : -1;

                    string outcomeTitle = "Не обслужен";
                    if (matches == 3)
                        outcomeTitle = "Идеальный чай";
                    else if (matches == 2)
                        outcomeTitle = "Частичное согревание";
                    else if (matches >= 0)
                        outcomeTitle = "Холодный уход";

                    TalkOutcomeData outcome = null;
                    if (_progress != null &&
                        _progress.GuestResults.TryGetValue(id, out outcome) &&
                        outcome != null &&
                        !string.IsNullOrEmpty(outcome.title))
                    {
                        outcomeTitle = outcome.title;
                    }

                    summary.GuestItems.Add(new GuestEndingItem
                    {
                        GuestName = spirit != null ? spirit.name : id,
                        TeaScore = matches,
                        ResultTitle = outcomeTitle
                    });
                }
            }

            string finalSpiritId = null;
            if (order != null)
            {
                foreach (string id in order)
                {
                    GuestInfo s = _gameData.GetGuest(id);
                    if (s != null && s.isTwistGuest)
                        finalSpiritId = id;
                }

                if (string.IsNullOrEmpty(finalSpiritId) && order.Length > 0)
                    finalSpiritId = order[order.Length - 1];
            }

            int finalMatches = !string.IsNullOrEmpty(finalSpiritId) && _progress != null
                ? _progress.GetTeaScore(finalSpiritId)
                : -1;
            TalkOutcomeData finalOutcome = null;
            if (!string.IsNullOrEmpty(finalSpiritId) && _progress != null)
                _progress.GuestResults.TryGetValue(finalSpiritId, out finalOutcome);

            EndingTextsData endings = new EndingTextsData();
            if (_gameData != null && _gameData.SessionRules != null && _gameData.SessionRules.endings != null)
                endings = _gameData.SessionRules.endings;

            if (finalOutcome != null)
            {
                if (string.Equals(finalOutcome.endingRank, "FrozenNight", System.StringComparison.OrdinalIgnoreCase))
                {
                    summary.Rank = EndingRank.FrozenNight;
                    summary.Title = string.IsNullOrEmpty(finalOutcome.title) ? endings.coldTitle : finalOutcome.title.ToUpperInvariant();
                    summary.Description = endings.coldText;
                }
                else if (string.Equals(finalOutcome.endingRank, "PaleDawn", System.StringComparison.OrdinalIgnoreCase))
                {
                    summary.Rank = EndingRank.PaleDawn;
                    summary.Title = string.IsNullOrEmpty(finalOutcome.title) ? endings.paleDawnTitle : finalOutcome.title.ToUpperInvariant();
                    summary.Description = endings.paleDawnText;
                }
                else
                {
                    summary.Rank = EndingRank.GoldenDawn;
                    summary.Title = string.IsNullOrEmpty(finalOutcome.title) ? endings.goldenDawnTitle : finalOutcome.title.ToUpperInvariant();
                    summary.Description = summary.TotalLight >= 6 ? endings.goldenDawnBestText : endings.goldenDawnText;
                }
            }
            else if (finalMatches == 2)
            {
                summary.Rank = EndingRank.PaleDawn;
                summary.Title = endings.paleDawnTitle;
                summary.Description = endings.paleDawnText;
            }
            else
            {
                summary.Rank = EndingRank.FrozenNight;
                summary.Title = endings.coldTitle;
                summary.Description = endings.coldText;
            }

            return summary;
        }
    }
}
