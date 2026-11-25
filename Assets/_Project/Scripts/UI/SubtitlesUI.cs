using System.Collections;
using System.Collections.Generic;
using TheLastCup.Core;
using TheLastCup.Data;
using TheLastCup.Domain;
using TheLastCup.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLastCup.UI
{
    public class SubtitlesUI : MonoBehaviour
    {
        [Header("World Subtitle")]
        [SerializeField] private TMP_Text _subtitleText;
        [SerializeField] private TMP_Text _speakerText;
        [SerializeField] private GameObject _panel;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Timings")]
        [SerializeField] private float _typingSpeed = 45f;
        [SerializeField] private float _pauseBetweenLines = 2.0f;
        [SerializeField] private float _endDelay = 3.0f;
        [SerializeField] private int _maxLineLength = 85;

        [Header("World 3D Buttons")]
        [SerializeField] private TalkChoiceButton _choiceAButton;
        [SerializeField] private TalkChoiceButton _choiceBButton;
        [SerializeField] private TalkChoiceButton _retryButton;
        [SerializeField] private TalkChoiceButton _nextGuestButton;

        private Coroutine _dialogueCoroutine;
        private GuestInfo _currentGuest;
        private TalkBranchData _currentBranch;
        private bool _skipLine;
        private bool _skipAllLines;

        private Vector3 _counterPos;
        private Quaternion _counterRot;
        private Vector3 _cameraOffsetPos;
        private Quaternion _cameraOffsetRot;

        private void Awake()
        {
            if (!_panel)
                _panel = gameObject;

            _counterPos = transform.position;
            _counterRot = transform.rotation;

            if (Camera.main != null)
            {
                Transform cam = Camera.main.transform;
                _cameraOffsetPos = Quaternion.Inverse(cam.rotation) * (_counterPos - cam.position);
                _cameraOffsetRot = Quaternion.Inverse(cam.rotation) * _counterRot;
            }

            HideNow();
        }

        private void OnEnable()
        {
            GameEvents.OnGuestAtCounter += OnGuestArrived;
            GameEvents.OnTeaChecked += OnTeaChecked;
            GameEvents.OnTableTalkStarted += OnTableTalkStarted;
            GameEvents.OnDemoEnded += OnDemoEnded;
            GameEvents.OnSnapCameraRequested += OnMovedToTable;
            GameEvents.OnSnapCameraBackRequested += OnReturnedToCounter;
            GameEvents.OnDebugSkipTalk += OnDebugSkipTalk;
        }

        private void OnDisable()
        {
            GameEvents.OnGuestAtCounter -= OnGuestArrived;
            GameEvents.OnTeaChecked -= OnTeaChecked;
            GameEvents.OnTableTalkStarted -= OnTableTalkStarted;
            GameEvents.OnDemoEnded -= OnDemoEnded;
            GameEvents.OnSnapCameraRequested -= OnMovedToTable;
            GameEvents.OnSnapCameraBackRequested -= OnReturnedToCounter;
            GameEvents.OnDebugSkipTalk -= OnDebugSkipTalk;
        }

        private void OnDebugSkipTalk()
        {
            _skipAllLines = true;
            _skipLine = true;
        }

        private void OnMovedToTable(Transform tableCameraPoint)
        {
            if (!tableCameraPoint) return;

            transform.position = tableCameraPoint.position + tableCameraPoint.rotation * _cameraOffsetPos;
            transform.rotation = tableCameraPoint.rotation * _cameraOffsetRot;
        }

        private void OnReturnedToCounter()
        {
            transform.position = _counterPos;
            transform.rotation = _counterRot;
        }

        private void Update()
        {
            if (!_panel || !_panel.activeSelf) return;

            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                _skipLine = true;
        }

        private void OnGuestArrived(GuestInfo spirit)
        {
            _currentGuest = spirit;
            _currentBranch = null;

            if (_speakerText)
            {
                _speakerText.gameObject.SetActive(true);
                _speakerText.text = $"{spirit.name} — {spirit.role}";
            }

            HideButtons();
            ShowPanel();

            RunCoroutine(PlayIntroRoutine(spirit.introLines));
        }

        private void OnTeaChecked(TeaResult result, GuestInfo spirit)
        {
            if (result != null && result.MatchCount >= 2 && spirit != null && !spirit.isTwistGuest)
                return;

            StartTeaTalk(result, spirit);
        }

        private void OnTableTalkStarted(TeaResult result, GuestInfo spirit)
        {
            StartTeaTalk(result, spirit);
        }

        private void StartTeaTalk(TeaResult result, GuestInfo spirit)
        {
            _currentGuest = spirit;
            _currentBranch = null;

            if (_speakerText)
            {
                _speakerText.gameObject.SetActive(true);
                _speakerText.text = $"{spirit.name} — {spirit.role}";
            }

            HideButtons();
            ShowPanel();

            if (result.MatchCount <= 1)
            {
                TalkChoiceButton whistleBtn = _retryButton ? _retryButton : _choiceAButton;
                TalkChoiceButton nextGuestBtn = _nextGuestButton ? _nextGuestButton : _choiceBButton;

                if (result.IsWhistleReady && nextGuestBtn)
                {
                    RunCoroutine(SpeakAndShowChoicesRoutine(spirit.badTalk, ShowWhistleChoices));
                }
                else
                {
                    RunCoroutine(SpeakAndLeaveRoutine(spirit.badTalk));
                }
            }
            else if (result.MatchCount == 2)
            {
                RunCoroutine(SpeakAndLeaveRoutine(spirit.okTalk));
            }
            else
            {
                var tree = spirit.bestTalk;
                if (tree != null && !string.IsNullOrEmpty(tree.question))
                {
                    RunCoroutine(SpeakAndShowChoicesRoutine(tree.question, ShowFirstChoices));
                }
                else
                {
                    RunCoroutine(SpeakAndLeaveRoutine(spirit.goodTalk));
                }
            }
        }

        private void ShowWhistleChoices()
        {
            TalkChoiceButton whistleBtn = _retryButton != null ? _retryButton : _choiceAButton;
            TalkChoiceButton nextGuestBtn = _nextGuestButton != null ? _nextGuestButton : _choiceBButton;
            if (whistleBtn != null) whistleBtn.Setup("Свисток проводника", OnRetryWithWhistleClicked);
            if (nextGuestBtn != null) nextGuestBtn.Setup("Отпустить гостя", FinishAndNextGuest);
        }

        private void ShowFirstChoices()
        {
            if (_currentGuest == null || _currentGuest.bestTalk == null) return;
            if (_choiceAButton != null) _choiceAButton.Setup(_currentGuest.bestTalk.choice1AText, OnPickChoiceA);
            if (_choiceBButton != null) _choiceBButton.Setup(_currentGuest.bestTalk.choice1BText, OnPickChoiceB);
        }

        private void ShowBranchChoices()
        {
            if (_currentBranch == null) return;
            if (_choiceAButton != null) _choiceAButton.Setup(_currentBranch.choiceAText, OnPickChoiceA);
            if (_choiceBButton != null) _choiceBButton.Setup(_currentBranch.choiceBText, OnPickChoiceB);
        }

        private void OnPickChoiceA()
        {
            OnChoicePicked(true);
        }

        private void OnPickChoiceB()
        {
            OnChoicePicked(false);
        }

        private void OnChoicePicked(bool chooseA)
        {
            if (_currentGuest == null || _currentGuest.bestTalk == null) return;

            HideButtons();

            if (_currentBranch == null)
            {
                var tree = _currentGuest.bestTalk;

                if (tree.branchA == null && tree.branchB == null && (tree.outcome1A != null || tree.outcome1B != null))
                {
                    FinishGuestOutcome(chooseA ? tree.outcome1A : tree.outcome1B);
                    return;
                }

                _currentBranch = chooseA ? tree.branchA : tree.branchB;
                if (_currentBranch == null)
                {
                    FinishAndNextGuest();
                    return;
                }

                RunCoroutine(SpeakAndShowChoicesRoutine(_currentBranch.guestLine, ShowBranchChoices));
                return;
            }

            FinishGuestOutcome(chooseA ? _currentBranch.outcomeA : _currentBranch.outcomeB);
        }

        private void FinishGuestOutcome(TalkOutcomeData outcome)
        {
            _currentBranch = null;

            if (outcome != null)
                GameEvents.SendGuestResultReached(_currentGuest.id, outcome);

            string finalText = (outcome != null && outcome.summaryText != null) ? outcome.summaryText : _currentGuest.goodTalk;
            RunCoroutine(SpeakAndLeaveRoutine(finalText));
        }

        private void FinishAndNextGuest()
        {
            _currentBranch = null;
            StopCoroutineIfRunning();
            HideNow();
            GameEvents.CompleteGuest();
        }

        private void OnRetryWithWhistleClicked()
        {
            _currentBranch = null;
            StopCoroutineIfRunning();
            HideNow();
            GameEvents.RequestWhistleRetry();
        }

        private void OnDemoEnded()
        {
            HideButtons();
            HideNow();
        }

        private IEnumerator PlayIntroRoutine(string text)
        {
            yield return PlayLinesRoutine(text, true, true);
            yield return FadeOutRoutine();
            HideNow();
            ShowAllCurrentWords();
            GameEvents.SendIntroTalkEnded(_currentGuest);
        }

        private IEnumerator SpeakAndLeaveRoutine(string text)
        {
            yield return PlayLinesRoutine(text, true);
            yield return FadeOutRoutine();

            HideNow();
            GameEvents.CompleteGuest();
        }

        private IEnumerator SpeakAndShowChoicesRoutine(string text, System.Action showButtons)
        {
            yield return PlayLinesRoutine(text, false);
            if (showButtons != null) showButtons.Invoke();
        }

        private IEnumerator PlayLinesRoutine(string fullText, bool waitAfterLast, bool revealKeywords = false)
        {
            _skipAllLines = false;
            List<string> sentences = SplitToLines(fullText);
            string spokenAccumulated = string.Empty;
            var revealedThisSpeech = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < sentences.Count; i++)
            {
                if (_skipAllLines)
                {
                    if (_subtitleText)
                    {
                        _subtitleText.text = sentences[sentences.Count - 1];
                        _subtitleText.ForceMeshUpdate();
                        _subtitleText.maxVisibleCharacters = _subtitleText.textInfo.characterCount;
                    }
                    if (revealKeywords)
                        ShowAllCurrentWords();
                    break;
                }

                yield return TypeLineRoutine(sentences[i]);

                if (revealKeywords)
                {
                    spokenAccumulated = string.IsNullOrEmpty(spokenAccumulated)
                        ? sentences[i]
                        : $"{spokenAccumulated} {sentences[i]}";
                    ShowWordsInBallIfNeeded(spokenAccumulated, revealedThisSpeech);
                }

                if (_skipAllLines) continue;

                bool isLast = i == sentences.Count - 1;
                if (!isLast)
                    yield return WaitOrSkip(_pauseBetweenLines);
                else if (waitAfterLast)
                    yield return WaitOrSkip(_endDelay);
            }

            _skipAllLines = false;
        }

        private void ShowWordsInBallIfNeeded(string spokenText, HashSet<string> revealedSet)
        {
            if (_currentGuest == null || _currentGuest.keyWords == null || string.IsNullOrEmpty(spokenText))
                return;

            foreach (string kw in _currentGuest.keyWords)
            {
                if (string.IsNullOrEmpty(kw) || revealedSet.Contains(kw))
                    continue;

                if (spokenText.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    revealedSet.Add(kw);
                    GameEvents.ShowBallWord(kw);
                }
            }
        }

        private void ShowAllCurrentWords()
        {
            if (_currentGuest == null || _currentGuest.keyWords == null) return;

            foreach (string kw in _currentGuest.keyWords)
            {
                if (!string.IsNullOrEmpty(kw))
                    GameEvents.ShowBallWord(kw);
            }
        }

        private List<string> SplitToLines(string text)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return result;

            string src = text.Trim();
            int start = 0;
            for (int i = 0; i < src.Length; i++)
            {
                char ch = src[i];
                if (ch == '.' || ch == '!' || ch == '?' || ch == '…')
                {
                    while (i + 1 < src.Length && (src[i + 1] == '.' || src[i + 1] == '!' || src[i + 1] == '?' || src[i + 1] == '…' || src[i + 1] == '»' || src[i + 1] == '"' || src[i + 1] == ')'))
                        i++;

                    if (i + 1 >= src.Length || char.IsWhiteSpace(src[i + 1]))
                    {
                        string part = src.Substring(start, i - start + 1).Trim();
                        if (!string.IsNullOrEmpty(part))
                            AddShortLine(part, result);
                        start = i + 1;
                    }
                }
            }

            if (start < src.Length)
            {
                string tail = src.Substring(start).Trim();
                if (!string.IsNullOrEmpty(tail))
                    AddShortLine(tail, result);
            }

            return result;
        }

        private void AddShortLine(string sentence, List<string> output)
        {
            while (sentence.Length > _maxLineLength)
            {
                int splitIdx = FindSplitIndex(sentence, _maxLineLength);
                if (splitIdx <= 0) break;

                string head = sentence.Substring(0, splitIdx).Trim();
                string tail = sentence.Substring(splitIdx).Trim();

                if (CountWords(tail) <= 3)
                {
                    output.Add($"{head} {tail}".Trim());
                    return;
                }

                output.Add(head);
                sentence = tail;
            }

            if (!string.IsNullOrEmpty(sentence))
                output.Add(sentence);
        }

        private int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return text.Split(new char[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private int FindSplitIndex(string text, int maxLen)
        {
            int limit = Mathf.Min(maxLen, text.Length - 1);

            for (int i = limit; i >= maxLen / 2; i--)
            {
                char c = text[i - 1];
                if ((c == ',' || c == ';' || c == ':' || c == '—') && i < text.Length && char.IsWhiteSpace(text[i]))
                    return i;
            }

            for (int i = limit; i >= 1; i--)
            {
                if (char.IsWhiteSpace(text[i]))
                    return i;
            }

            return limit;
        }

        private IEnumerator TypeLineRoutine(string sentence)
        {
            if (!_subtitleText) yield break;

            _skipLine = false;
            _subtitleText.text = sentence;
            _subtitleText.ForceMeshUpdate();
            int totalChars = _subtitleText.textInfo.characterCount;
            _subtitleText.maxVisibleCharacters = 0;

            float visibleCount = 0f;
            while (visibleCount < totalChars)
            {
                if (_skipLine)
                {
                    _skipLine = false;
                    break;
                }

                float charsPerSec = GameSettings.SubtitleSpeed > 0f ? GameSettings.SubtitleSpeed : _typingSpeed;
                visibleCount += Time.deltaTime * charsPerSec;
                _subtitleText.maxVisibleCharacters = Mathf.Clamp(Mathf.FloorToInt(visibleCount), 0, totalChars);
                yield return null;
            }

            _subtitleText.maxVisibleCharacters = totalChars;
        }

        private IEnumerator WaitOrSkip(float duration)
        {
            _skipLine = false;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (_skipLine)
                {
                    _skipLine = false;
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator FadeOutRoutine()
        {
            if (!_canvasGroup) yield break;

            float t = 1f;
            while (t > 0f)
            {
                t -= Time.deltaTime * 3f;
                _canvasGroup.alpha = Mathf.Max(0f, t);
                yield return null;
            }
        }

        private void RunCoroutine(IEnumerator routine)
        {
            StopCoroutineIfRunning();
            _dialogueCoroutine = StartCoroutine(routine);
        }

        private void StopCoroutineIfRunning()
        {
            if (_dialogueCoroutine != null)
            {
                StopCoroutine(_dialogueCoroutine);
                _dialogueCoroutine = null;
            }
        }

        private void ShowPanel()
        {
            if (_panel) _panel.SetActive(true);
            if (_canvasGroup) _canvasGroup.alpha = 1f;
        }

        private void HideButtons()
        {
            if (_choiceAButton != null) _choiceAButton.Hide();
            if (_choiceBButton != null) _choiceBButton.Hide();
            if (_retryButton != null) _retryButton.Hide();
            if (_nextGuestButton != null) _nextGuestButton.Hide();
        }

        private void HideNow()
        {
            if (_subtitleText)
            {
                _subtitleText.text = "";
                _subtitleText.maxVisibleCharacters = 0;
            }
            if (_speakerText) _speakerText.gameObject.SetActive(false);
            if (_canvasGroup) _canvasGroup.alpha = 0f;
            HideButtons();

            if (_panel) _panel.SetActive(false);
        }
    }
}
