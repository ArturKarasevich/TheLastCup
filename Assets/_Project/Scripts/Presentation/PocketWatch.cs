using TheLastCup.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheLastCup.Presentation
{
    public class PocketWatch : MonoBehaviour, IClickable, IHintText
    {
        private enum WatchMode
        {
            None,
            StartMenu,
            PauseMenu
        }

        [Header("Watch Setup")]
        [SerializeField] private Transform _cameraPoint;
        [SerializeField] private Transform _lid;
        [SerializeField] private Vector3 _openAngle = new Vector3(0, 110f, 0);
        [SerializeField] private GameObject _menuCanvas;
        [SerializeField] private Button _backButton;
        [SerializeField] private OutlineEffect _outline;

        [Header("Menu Modes")]
        [SerializeField] private bool _startInMenu = false;
        [SerializeField] private bool _pauseOnMenu = true;

        [Header("Optional Custom Roots and Buttons (Auto-created if empty)")]
        [SerializeField] private GameObject _startMenuPanel;
        [SerializeField] private GameObject _pauseMenuPanel;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private TMP_Text _titleText;

        private bool _isOpen;
        private bool _wasOpened;

        public bool WasOpened { get { return _wasOpened; } }

        public string GetFirstGuestHint()
        {
            return _wasOpened ? null : "Если захочешь взять перерыв — взгляни на карманные часы";
        }
        private bool _isGameStarted;
        private bool _isCameraLocked;
        private bool _skipMenuOnce;
        private WatchMode _mode = WatchMode.None;
        private Quaternion _closedRotation;

        private bool _movedQuitButton;
        private float _buttonStartY = -0.2966f;
        private float _buttonGap = 0.23f;

        public bool IsOpen { get { return _isOpen; } }
        public bool IsGameStarted { get { return _isGameStarted; } }

        private void Awake()
        {
            _skipMenuOnce = GameEvents.SkipMenuNextTime;

            if (_lid)
                _closedRotation = _lid.localRotation;

            if (!_outline)
                _outline = GetComponent<OutlineEffect>();

            CheckMenuButtons();

            if (_menuCanvas)
                _menuCanvas.SetActive(false);

            UpdateMenuVisuals(WatchMode.None);
        }

        private void OnEnable()
        {
            GameEvents.OnCameraLockChanged += OnCameraLockChanged;
            GameEvents.OnStartGameRequested += OnExternalStartGame;

            if (_backButton) _backButton.onClick.AddListener(CloseWatch);
            if (_playButton) _playButton.onClick.AddListener(StartFromMenu);
            if (_restartButton) _restartButton.onClick.AddListener(RestartGame);
            if (_quitButton) _quitButton.onClick.AddListener(QuitGame);
        }

        private void OnDisable()
        {
            GameEvents.OnCameraLockChanged -= OnCameraLockChanged;
            GameEvents.OnStartGameRequested -= OnExternalStartGame;

            if (_backButton) _backButton.onClick.RemoveListener(CloseWatch);
            if (_playButton) _playButton.onClick.RemoveListener(StartFromMenu);
            if (_restartButton) _restartButton.onClick.RemoveListener(RestartGame);
            if (_quitButton) _quitButton.onClick.RemoveListener(QuitGame);
        }

        private void OnDestroy()
        {
            if (Time.timeScale == 0f)
                Time.timeScale = 1f;
        }

        private void Start()
        {
            GameEvents.SkipMenuNextTime = false;

            if (!_skipMenuOnce && FindFirstObjectByType<UI.MainMenu>() != null)
            {
                _isGameStarted = false;
                _isOpen = false;
                _mode = WatchMode.None;
                return;
            }

            if (_startInMenu && !_skipMenuOnce)
            {
                OpenStartMenu();
            }
            else
            {
                _isGameStarted = true;
                if (!_wasOpened && _outline)
                    _outline.SetPulse(true);
            }
        }

        private void OnExternalStartGame()
        {
            if (_isGameStarted) return;

            _isGameStarted = true;
            _isOpen = false;
            _mode = WatchMode.None;

            if (_menuCanvas)
                _menuCanvas.SetActive(false);

            UpdateMenuVisuals(WatchMode.None);

            if (!_wasOpened && _outline)
                _outline.SetPulse(true);
        }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
                return;

            if (_isOpen && _mode == WatchMode.PauseMenu)
            {
                CloseWatch();
            }
            else if (!_isOpen && _isGameStarted && !_isCameraLocked)
            {
                OpenPauseMenu();
            }
        }

        private void OnCameraLockChanged(bool isLocked)
        {
            _isCameraLocked = isLocked;

            if (!isLocked && _isOpen && _mode == WatchMode.PauseMenu)
                CloseWatch();
        }

        public void OnHoverStart()
        {
            if (_isOpen) return;

            if (_outline)
                _outline.SetLit(true);
        }

        public void OnHoverEnd()
        {
            if (_outline)
                _outline.SetLit(false);
        }

        public void OnClick()
        {
            if (_isOpen) return;

            if (_isGameStarted)
                OpenPauseMenu();
            else
                OpenStartMenu();
        }

        public void OpenStartMenu()
        {
            Time.timeScale = 1f;
            _isOpen = true;
            _isGameStarted = false;
            _mode = WatchMode.StartMenu;

            if (_outline)
            {
                _outline.SetPulse(false);
                _outline.SetLit(false);
            }

            if (_cameraPoint)
                GameEvents.RequestCameraFocus(_cameraPoint);

            if (_lid)
                _lid.localRotation = Quaternion.Euler(_openAngle);

            if (_menuCanvas)
                _menuCanvas.SetActive(true);

            UpdateMenuVisuals(WatchMode.StartMenu);
        }

        public void OpenPauseMenu()
        {
            _isOpen = true;
            _wasOpened = true;
            _mode = WatchMode.PauseMenu;

            if (_pauseOnMenu)
                Time.timeScale = 0f;

            if (_outline)
            {
                _outline.SetPulse(false);
                _outline.SetLit(false);
            }

            if (_cameraPoint)
                GameEvents.RequestCameraFocus(_cameraPoint);

            if (_lid)
                _lid.localRotation = Quaternion.Euler(_openAngle);

            if (_menuCanvas)
                _menuCanvas.SetActive(true);

            UpdateMenuVisuals(WatchMode.PauseMenu);
        }

        public void OpenWatch()
        {
            if (_isGameStarted)
                OpenPauseMenu();
            else
                OpenStartMenu();
        }

        public void StartFromMenu()
        {
            if (_isGameStarted) return;

            _isGameStarted = true;
            _wasOpened = true;
            CloseWatch();
            GameEvents.RequestStartGame();
        }

        public void CloseWatch()
        {
            if (!_isOpen) return;

            if (_mode == WatchMode.StartMenu && !_isGameStarted)
                return;

            Time.timeScale = 1f;
            _isOpen = false;
            _mode = WatchMode.None;

            if (_outline)
                _outline.SetLit(false);

            if (_lid)
                _lid.localRotation = _closedRotation;

            if (_menuCanvas)
                _menuCanvas.SetActive(false);

            UpdateMenuVisuals(WatchMode.None);
            GameEvents.RequestCameraFreeLook();
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

        private void CheckMenuButtons()
        {
            if (!_menuCanvas || !_backButton)
                return;

            RectTransform templateRect;

            if (_backButton.TryGetComponent<RectTransform>(out templateRect))
            {
                _buttonStartY = templateRect.anchoredPosition.y;
                float height = templateRect.rect.height > 0.001f ? templateRect.rect.height : templateRect.sizeDelta.y;
                if (height > 0.001f)
                    _buttonGap = height * 1.15f;
            }

            SetButtonLabel(_backButton, "Продолжить");

            if (!_startMenuPanel && !_pauseMenuPanel)
            {
                if (!_playButton)
                    _playButton = CopyButton("PlayBtn", "Начать", _buttonStartY);

                if (!_restartButton)
                    _restartButton = CopyButton("RestartBtn", "Заново", _buttonStartY - _buttonGap);

                if (!_quitButton)
                {
                    _quitButton = CopyButton("QuitBtn", "Выход", _buttonStartY - _buttonGap * 2f);
                    _movedQuitButton = true;
                }
            }
        }

        private Button CopyButton(string objectName, string label, float anchoredY)
        {
            GameObject clone = Instantiate(_backButton.gameObject, _menuCanvas.transform, false);
            clone.name = objectName;

            RectTransform rect;

            if (clone.TryGetComponent<RectTransform>(out rect))
            {
                Vector2 pos = rect.anchoredPosition;
                pos.y = anchoredY;
                rect.anchoredPosition = pos;
            }

            Button btn = clone.GetComponent<Button>();
            if (btn)
                btn.onClick.RemoveAllListeners();

            SetButtonLabel(btn, label);
            return btn;
        }

        private static void SetButtonLabel(Button btn, string label)
        {
            if (!btn) return;

            TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(true);
            if (tmp)
                tmp.text = label;
        }

        private void UpdateMenuVisuals(WatchMode mode)
        {
            bool isStart = mode == WatchMode.StartMenu;
            bool isPause = mode == WatchMode.PauseMenu;

            if (_startMenuPanel) _startMenuPanel.SetActive(isStart);
            if (_pauseMenuPanel) _pauseMenuPanel.SetActive(isPause);

            if (_playButton) _playButton.gameObject.SetActive(isStart);
            if (_backButton) _backButton.gameObject.SetActive(isPause);
            if (_restartButton) _restartButton.gameObject.SetActive(isPause);
            if (_quitButton)
            {
                _quitButton.gameObject.SetActive(isStart || isPause);
                RectTransform rect;
                if (_movedQuitButton && _quitButton.TryGetComponent<RectTransform>(out rect))
                {
                    Vector2 pos = rect.anchoredPosition;
                    pos.y = isStart ? (_buttonStartY - _buttonGap) : (_buttonStartY - _buttonGap * 2f);
                    rect.anchoredPosition = pos;
                }
            }

            if (_titleText)
            {
                if (isStart) _titleText.text = "THE LAST CUP";
                else if (isPause) _titleText.text = "ПАУЗА";
            }
        }
    }
}
