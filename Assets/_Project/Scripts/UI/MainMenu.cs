using System.Collections;
using TheLastCup.Core;
using TheLastCup.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastCup.UI
{
    public class MainMenu : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup _root;
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private CanvasGroup _mainMenuCanvas;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private CanvasGroup _settingsCanvas;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private Slider _volumeSlider;
        [SerializeField] private TMP_Text _volumeText;
        [SerializeField] private Slider _textSpeedSlider;
        [SerializeField] private TMP_Text _textSpeedText;
        [SerializeField] private Slider _sensitivitySlider;
        [SerializeField] private TMP_Text _sensitivityText;
        [SerializeField] private Button _invertYButton;
        [SerializeField] private TMP_Text _invertYText;
        [SerializeField] private Button _fullscreenButton;
        [SerializeField] private TMP_Text _fullscreenText;

        private Coroutine _menuSwitchCoroutine;
        private CameraController _camera;
        private bool _isStarting;

        private void Awake()
        {
            GameSettings.LoadSettings();
            _camera = FindFirstObjectByType<CameraController>();
            SetupButtonsAndSliders();
            OpenSettings(false, true);
        }

        private void Start()
        {
            if (GameEvents.SkipMenuNextTime)
            {
                Destroy(gameObject);
                return;
            }

            if (_camera != null)
                _camera.MoveToMenuWindow();
        }

        private void SetupButtonsAndSliders()
        {
            if (_playButton != null) _playButton.onClick.AddListener(OnPlayClicked);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(OnSettingsClicked);
            if (_quitButton != null) _quitButton.onClick.AddListener(OnQuitClicked);
            if (_backButton != null) _backButton.onClick.AddListener(OnBackClicked);

            if (_volumeSlider != null)
            {
                _volumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
                if (_volumeText != null) _volumeText.text = $"{Mathf.RoundToInt(GameSettings.MasterVolume * 100f)}%";
                _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }

            if (_textSpeedSlider != null)
            {
                _textSpeedSlider.SetValueWithoutNotify(GameSettings.SubtitleSpeed);
                if (_textSpeedText != null) _textSpeedText.text = $"{Mathf.RoundToInt(GameSettings.SubtitleSpeed)} симв/с";
                _textSpeedSlider.onValueChanged.AddListener(OnTextSpeedChanged);
            }

            if (_sensitivitySlider != null)
            {
                _sensitivitySlider.SetValueWithoutNotify(GameSettings.CameraSensitivity);
                if (_sensitivityText != null) _sensitivityText.text = $"{GameSettings.CameraSensitivity:F1}x";
                _sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            }

            if (_invertYButton != null)
            {
                UpdateToggleButton(_invertYText, GameSettings.InvertY);
                _invertYButton.onClick.AddListener(OnInvertYClicked);
            }

            if (_fullscreenButton != null)
            {
                UpdateToggleButton(_fullscreenText, GameSettings.Fullscreen);
                _fullscreenButton.onClick.AddListener(OnFullscreenClicked);
            }
        }

        private void OnSettingsClicked()
        {
            OpenSettings(true);
        }

        private void OnBackClicked()
        {
            GameSettings.SaveSettings();
            OpenSettings(false);
        }

        private void OnVolumeChanged(float value)
        {
            GameSettings.MasterVolume = value;
            if (_volumeText != null)
                _volumeText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }

        private void OnTextSpeedChanged(float value)
        {
            GameSettings.SubtitleSpeed = value;
            if (_textSpeedText != null)
                _textSpeedText.text = $"{Mathf.RoundToInt(value)} симв/с";
        }

        private void OnSensitivityChanged(float value)
        {
            GameSettings.CameraSensitivity = value;
            if (_sensitivityText != null)
                _sensitivityText.text = $"{value:F1}x";
        }

        private void OnInvertYClicked()
        {
            bool next = !GameSettings.InvertY;
            GameSettings.InvertY = next;
            UpdateToggleButton(_invertYText, next);
        }

        private void OnFullscreenClicked()
        {
            bool next = !GameSettings.Fullscreen;
            GameSettings.Fullscreen = next;
            UpdateToggleButton(_fullscreenText, next);
        }

        private static void UpdateToggleButton(TMP_Text stateTmp, bool isOn)
        {
            if (stateTmp == null) return;
            stateTmp.text = isOn ? "Вкл" : "Выкл";
            stateTmp.color = isOn
                ? new Color(0.96f, 0.76f, 0.34f, 1f)
                : new Color(0.65f, 0.59f, 0.52f, 0.9f);
        }

        private void OpenSettings(bool open, bool immediate = false)
        {
            if (immediate)
            {
                if (_mainMenuPanel != null) _mainMenuPanel.SetActive(!open);
                if (_mainMenuCanvas != null) _mainMenuCanvas.alpha = open ? 0f : 1f;
                if (_settingsPanel != null) _settingsPanel.SetActive(open);
                if (_settingsCanvas != null) _settingsCanvas.alpha = open ? 1f : 0f;
                return;
            }

            if (_menuSwitchCoroutine != null)
                StopCoroutine(_menuSwitchCoroutine);
            _menuSwitchCoroutine = StartCoroutine(SwitchMenuRoutine(open));
        }

        private IEnumerator SwitchMenuRoutine(bool openSettings)
        {
            GameObject outgoing = openSettings ? _mainMenuPanel : _settingsPanel;
            CanvasGroup outGroup = openSettings ? _mainMenuCanvas : _settingsCanvas;
            GameObject incoming = openSettings ? _settingsPanel : _mainMenuPanel;
            CanvasGroup inGroup = openSettings ? _settingsCanvas : _mainMenuCanvas;

            if (outGroup != null) outGroup.interactable = false;
            if (inGroup != null) inGroup.interactable = false;

            const float fadeDuration = 0.16f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                if (outGroup != null) outGroup.alpha = 1f - t;
                yield return null;
            }

            if (outgoing != null) outgoing.SetActive(false);
            if (incoming != null)
            {
                incoming.SetActive(true);
                RectTransform parentRect = incoming.transform.parent as RectTransform;
                if (parentRect != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }
            if (inGroup != null) inGroup.alpha = 0f;

            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                if (inGroup != null) inGroup.alpha = t;
                yield return null;
            }

            if (inGroup != null)
            {
                inGroup.alpha = 1f;
                inGroup.interactable = true;
            }
            if (outGroup != null) outGroup.interactable = true;
            _menuSwitchCoroutine = null;
        }

        private void OnPlayClicked()
        {
            if (_isStarting) return;
            _isStarting = true;

            GameSettings.SaveSettings();
            StartCoroutine(FadeAndStartRoutine());
        }

        private IEnumerator FadeAndStartRoutine()
        {
            if (_camera != null)
                _camera.MoveFromMenuToCounter();

            float elapsed = 0f;
            const float duration = 0.4f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_root != null)
                    _root.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            GameEvents.RequestStartGame();
            Destroy(gameObject);
        }

        private static void OnQuitClicked()
        {
            GameSettings.SaveSettings();
            Application.Quit();
        }
    }
}
