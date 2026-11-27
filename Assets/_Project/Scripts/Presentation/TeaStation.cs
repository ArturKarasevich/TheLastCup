using TheLastCup.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TheLastCup.Presentation
{
    public class TeaStation : MonoBehaviour, IClickable
    {
        [SerializeField] private Transform _cameraPoint;
        [SerializeField] private Button _backButton;
        [SerializeField] private OutlineEffect _outline;

        private bool _isFocused;
        private bool _isLocked;
        private bool _wasEntered;
        private bool _isSubFocusOpen;

        public bool IsFocused { get { return _isFocused; } }

        private void OnEnable()
        {
            GameEvents.OnCameraLockChanged += OnCameraLockChanged;
            GameEvents.OnAllHerbsAdded += OnAllHerbsAdded;
            GameEvents.OnTeapotBroken += OnTeapotReset;
            GameEvents.OnResetTeaCycle += OnTeapotReset;
            GameEvents.OnTeaDumped += OnTeapotReset;
            GameEvents.OnBookFocusChanged += OnBookFocusChanged;
            GameEvents.OnBallFocusChanged += OnSubFocusChanged;
            GameEvents.OnBallInspected += OnBallInspected;
            GameEvents.OnEnterTeaStationRequested += OnEnterRequested;

            if (_backButton)
                _backButton.onClick.AddListener(ExitStation);
        }

        private void OnDisable()
        {
            GameEvents.OnCameraLockChanged -= OnCameraLockChanged;
            GameEvents.OnAllHerbsAdded -= OnAllHerbsAdded;
            GameEvents.OnTeapotBroken -= OnTeapotReset;
            GameEvents.OnResetTeaCycle -= OnTeapotReset;
            GameEvents.OnTeaDumped -= OnTeapotReset;
            GameEvents.OnBookFocusChanged -= OnBookFocusChanged;
            GameEvents.OnBallFocusChanged -= OnSubFocusChanged;
            GameEvents.OnBallInspected -= OnBallInspected;
            GameEvents.OnEnterTeaStationRequested -= OnEnterRequested;

            if (_backButton)
                _backButton.onClick.RemoveListener(ExitStation);
        }

        private void OnTeapotReset()
        {
            _isLocked = false;
        }

        private void Awake()
        {
            if (!_outline)
                _outline = GetComponent<OutlineEffect>();

            if (_backButton)
                _backButton.gameObject.SetActive(false);
        }

        private void OnBallInspected()
        {
            if (!_wasEntered && !_isLocked && _outline)
                _outline.SetPulse(true);
        }

        private void OnCameraLockChanged(bool isLocked)
        {
            if (!isLocked)
            {
                _isFocused = false;
                if (_backButton)
                    _backButton.gameObject.SetActive(false);
            }
        }

        private void OnBookFocusChanged(bool isJournalOpen)
        {
            OnSubFocusChanged(isJournalOpen);
        }

        private void Update()
        {
            if (_isFocused && !_isSubFocusOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                ExitStation();
        }

        private void OnSubFocusChanged(bool isSubFocusOpen)
        {
            _isSubFocusOpen = isSubFocusOpen;

            if (!_isFocused || !_backButton)
                return;

            _backButton.gameObject.SetActive(!isSubFocusOpen);
        }

        private void OnAllHerbsAdded(Transform teapotTransform)
        {
            _isLocked = true;
            _isFocused = false;

            if (_outline)
                _outline.SetLit(false);

            if (_backButton)
                _backButton.gameObject.SetActive(false);

            GameEvents.RequestCameraReturnAndLook(teapotTransform);
        }

        public void SetLocked(bool locked)
        {
            _isLocked = locked;
            if (_isLocked && _outline)
                _outline.SetLit(false);
        }

        public void OnHoverStart()
        {
            if (_isFocused || _isLocked) return;

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
            if (_isFocused || _isLocked) return;
            EnterStation();
        }

        private void OnEnterRequested()
        {
            if (_isFocused || _isLocked) return;
            EnterStation();
        }

        public void EnterStation()
        {
            bool isFirstEnter = !_wasEntered;
            _isFocused = true;
            _wasEntered = true;

            if (_outline)
            {
                _outline.SetPulse(false);
                _outline.SetLit(false);
            }

            if (_cameraPoint)
                GameEvents.RequestCameraFocus(_cameraPoint);

            if (_backButton)
                _backButton.gameObject.SetActive(true);

            if (isFirstEnter)
                GameEvents.SendFirstTeaStationEntered();
        }

        public void ExitStation()
        {
            _isFocused = false;

            if (_outline)
                _outline.SetLit(false);

            if (_backButton)
                _backButton.gameObject.SetActive(false);

            GameEvents.RequestCameraFreeLook();
        }
    }
}
