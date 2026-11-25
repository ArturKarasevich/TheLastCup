using System;
using System.Collections;
using TheLastCup.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastCup.UI
{
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] private Image _fadeImage;
        [SerializeField] private float _fadeTime = 0.65f;
        [SerializeField] private float _blackHoldTime = 1.5f;

        private Coroutine _fadeCoroutine;

        private void OnEnable()
        {
            GameEvents.OnFadeMoveRequested += OnFadeMove;
            GameEvents.OnFadeReturnRequested += OnFadeReturn;
        }

        private void OnDisable()
        {
            GameEvents.OnFadeMoveRequested -= OnFadeMove;
            GameEvents.OnFadeReturnRequested -= OnFadeReturn;
        }

        private void Awake()
        {
            if (!_fadeImage)
                _fadeImage = GetComponent<Image>();

            if (_fadeImage)
            {
                SetAlphaNow(0f);
                _fadeImage.raycastTarget = false;
            }
        }

        private Transform _pendingMoveTarget;

        private void OnFadeMove(Transform targetPoint, Action onComplete)
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _pendingMoveTarget = targetPoint;
            _fadeCoroutine = StartCoroutine(FadeRoutine(SnapToPendingTarget, onComplete));
        }

        private void SnapToPendingTarget()
        {
            GameEvents.SnapCamera(_pendingMoveTarget);
        }

        private void OnFadeReturn()
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeRoutine(GameEvents.SnapCameraBack, null));
        }

        private IEnumerator FadeRoutine(Action duringBlackScreen, Action onComplete)
        {
            if (_fadeImage)
                _fadeImage.raycastTarget = true;

            yield return FadeToAlpha(1f);
            if (duringBlackScreen != null) duringBlackScreen.Invoke();
            yield return null;
            if (_blackHoldTime > 0f)
                yield return new WaitForSecondsRealtime(_blackHoldTime);
            yield return FadeToAlpha(0f);

            if (_fadeImage)
                _fadeImage.raycastTarget = false;

            if (onComplete != null) onComplete.Invoke();
        }

        private IEnumerator FadeToAlpha(float to)
        {
            if (!_fadeImage) yield break;

            float from = _fadeImage.color.a;
            float duration = Mathf.Max(0.01f, _fadeTime);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                SetAlphaNow(Mathf.Lerp(from, to, t));
                yield return null;
            }

            SetAlphaNow(to);
        }

        private void SetAlphaNow(float alpha)
        {
            Color c = _fadeImage.color;
            c.a = alpha;
            _fadeImage.color = c;
        }
    }
}
