using System.Collections;
using TheLastCup.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheLastCup.UI
{
    public class EndScreen : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _summaryText;
        [SerializeField] private TMP_Text _lightText;
        [SerializeField] private TMP_Text _logText;
        [SerializeField] private TMP_Text _creditsText;
        [SerializeField] private RectTransform _creditsRect;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _quitButton;

        [Header("Paper Look")]
        [SerializeField] private Color _fadedTextColor = new Color(0.38f, 0.28f, 0.20f, 1f);

        [Header("Timing and Credits")]
        [SerializeField] private float _showDelay = 2.4f;
        [SerializeField] private float _fadeTime = 1.2f;
        [SerializeField] private float _creditsSpeed = 55f;
        [TextArea(6, 16)]
        [SerializeField] private string _creditsTextData =
            "<b>THE LAST CUP</b>\n\n" +
            "НТО РКИ 2026–2027\n" +
            "Команда «Северный Флот»\n\n" +
            "<b>Разработчики:</b>\n" +
            "Акулов Алексей — Геймдизайнер\n" +
            "Габбасов Артур — Программист\n" +
            "Андрей Андронов — Художник\n" +
            "Ярослав — Художник\n\n" +
            "Спасибо за рейс.\n" +
            "До встречи на рассвете.";

        private Coroutine _showCoroutine;
        private bool _isVisible;
        private bool _creditsDone;

        private void OnEnable()
        {
            GameEvents.OnSessionEnded += OnSessionEnded;

            if (_restartButton) _restartButton.onClick.AddListener(RestartGame);
            if (_quitButton) _quitButton.onClick.AddListener(QuitGame);
        }

        private void OnDisable()
        {
            GameEvents.OnSessionEnded -= OnSessionEnded;

            if (_restartButton) _restartButton.onClick.RemoveListener(RestartGame);
            if (_quitButton) _quitButton.onClick.RemoveListener(QuitGame);
        }

        private void Awake()
        {
            if (_panel)
                _panel.SetActive(false);
        }

        private void Update()
        {
            if (!_isVisible || _creditsDone || !_creditsText || _creditsSpeed <= 0f)
                return;

            RectTransform creditsRect = _creditsText.rectTransform;
            Vector2 pos = creditsRect.anchoredPosition;
            pos.y += _creditsSpeed * Time.unscaledDeltaTime;
            creditsRect.anchoredPosition = pos;

            float areaHeight = _creditsRect ? _creditsRect.rect.height : 1080f;
            float textHeight = creditsRect.rect.height > 0f ? creditsRect.rect.height : 600f;
            if (pos.y - textHeight > areaHeight + 40f)
                _creditsDone = true;
        }

        private void OnSessionEnded(SessionSummary summary)
        {
            if (summary == null) return;

            if (_showCoroutine != null)
                StopCoroutine(_showCoroutine);

            _showCoroutine = StartCoroutine(ShowEndScreenRoutine(summary));
        }

        private IEnumerator ShowEndScreenRoutine(SessionSummary summary)
        {
            if (_showDelay > 0f)
                yield return new WaitForSecondsRealtime(_showDelay);

            if (!_panel)
                yield break;

            FillSummaryText(summary);
            _panel.SetActive(true);

            _isVisible = true;
            _creditsDone = false;
            GameEvents.SetUIMode(true);

            if (_canvasGroup && _fadeTime > 0.01f)
            {
                _canvasGroup.alpha = 0f;
                float elapsed = 0f;
                while (elapsed < _fadeTime)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _canvasGroup.alpha = Mathf.Clamp01(elapsed / _fadeTime);
                    yield return null;
                }
                _canvasGroup.alpha = 1f;
            }

            _showCoroutine = null;
        }

        private void FillSummaryText(SessionSummary summary)
        {
            Color titleColor = new Color(0.16f, 0.22f, 0.34f, 1f);
            if (summary.Rank == EndingRank.GoldenDawn)
                titleColor = new Color(0.48f, 0.24f, 0.04f, 1f);
            else if (summary.Rank == EndingRank.PaleDawn)
                titleColor = new Color(0.22f, 0.30f, 0.38f, 1f);

            if (_titleText)
            {
                _titleText.text = summary.Title;
                _titleText.color = titleColor;
            }

            if (_summaryText)
                _summaryText.text = summary.Description;

            if (_lightText)
                _lightText.text = $"Накопленный Свет: {summary.TotalLight} / 8";

            if (_logText)
            {
                string mutedHex = ColorUtility.ToHtmlStringRGB(_fadedTextColor);
                string log = "<b>ПУТЕВОЙ ЛИСТ СМЕНЫ</b>\n\n";

                foreach (var entry in summary.GuestItems)
                {
                    string scoreText = entry.TeaScore >= 0 ? $"{entry.TeaScore}/3" : "—";
                    log += $"• <b>{entry.GuestName}</b>  <color=#{mutedHex}>({scoreText})</color>\n";
                    log += $"  <color=#{mutedHex}><i>{entry.ResultTitle}</i></color>\n\n";
                }

                _logText.text = log;
            }

            if (_creditsText && !string.IsNullOrEmpty(_creditsTextData))
                _creditsText.text = _creditsTextData;
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            GameEvents.SkipMenuNextTime = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitGame()
        {
            Time.timeScale = 1f;
            Application.Quit();
        }
    }
}
