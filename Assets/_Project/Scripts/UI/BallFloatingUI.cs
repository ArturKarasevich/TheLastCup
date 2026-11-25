using TheLastCup.Core;
using TheLastCup.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastCup.UI
{
    public class BallFloatingUI : MonoBehaviour
    {
        [SerializeField] private GameObject _canvas;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _shakeButton;
        [SerializeField] private Button _closeButton;

        private void OnEnable()
        {
            GameEvents.OnBallFocusChanged += OnBallFocusChanged;
            GameEvents.OnStoryTwistStarted += OnStoryTwistStarted;
            GameEvents.OnSessionEnded += OnSessionEnded;
        }

        private void OnDisable()
        {
            GameEvents.OnBallFocusChanged -= OnBallFocusChanged;
            GameEvents.OnStoryTwistStarted -= OnStoryTwistStarted;
            GameEvents.OnSessionEnded -= OnSessionEnded;
        }

        private void Awake()
        {
            if (_shakeButton)
            {
                _shakeButton.onClick.RemoveListener(OnShakeClicked);
                _shakeButton.onClick.AddListener(OnShakeClicked);
            }

            if (_closeButton)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
                _closeButton.onClick.AddListener(OnCloseClicked);
            }

            SetVisibleNow(false);
        }

        private void OnBallFocusChanged(bool isOpen)
        {
            SetVisibleNow(isOpen);
        }

        private void OnStoryTwistStarted()
        {
            SetVisibleNow(false);
        }

        private void OnSessionEnded(SessionSummary summary)
        {
            SetVisibleNow(false);
        }

        private void OnShakeClicked()
        {
            CrystalBall ball = GetComponent<CrystalBall>();
            if (ball == null) ball = FindFirstObjectByType<CrystalBall>();
            if (ball != null) ball.ShakeBall();
        }

        private void OnCloseClicked()
        {
            CrystalBall ball = GetComponent<CrystalBall>();
            if (ball == null) ball = FindFirstObjectByType<CrystalBall>();
            if (ball != null) ball.CloseBall();
            else SetVisibleNow(false);
        }

        private void SetVisibleNow(bool visible)
        {
            if (_canvas) _canvas.SetActive(visible);
            if (_canvasGroup) _canvasGroup.alpha = visible ? 1f : 0f;
        }
    }
}
