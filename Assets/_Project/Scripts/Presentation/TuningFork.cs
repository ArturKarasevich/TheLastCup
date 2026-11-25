using System.Collections;
using TheLastCup.Core;
using TheLastCup.Data;
using TheLastCup.Domain;
using UnityEngine;

namespace TheLastCup.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class TuningFork : MonoBehaviour, IClickable
    {
        [SerializeField] private GameObject _modelRoot;
        [SerializeField] private OutlineEffect _outline;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _ringSound;

        private bool _isUnlocked;
        private Coroutine _shakeCoroutine;
        private Vector3 _originPos;

        private void OnEnable()
        {
            GameEvents.OnItemUnlocked += OnItemUnlocked;
            GameEvents.OnTeaChecked += OnTeaChecked;
        }

        private void OnDisable()
        {
            GameEvents.OnItemUnlocked -= OnItemUnlocked;
            GameEvents.OnTeaChecked -= OnTeaChecked;
        }

        private void Awake()
        {
            if (!_outline)
                _outline = GetComponent<OutlineEffect>();

            if (!_audioSource)
                _audioSource = GetComponent<AudioSource>();

            if (_modelRoot)
                _modelRoot.SetActive(false);

            Transform originTarget = _modelRoot != null ? _modelRoot.transform : transform;
            _originPos = originTarget.localPosition;
        }

        private void OnItemUnlocked(string artifactKey)
        {
            if (ItemIds.FromString(artifactKey) != ItemType.TuningFork)
                return;

            _isUnlocked = true;
            if (_modelRoot) _modelRoot.SetActive(true);
        }

        private void OnTeaChecked(TeaResult result, GuestInfo spirit)
        {
            if (_isUnlocked && result != null && result.MatchCount >= 3)
                PlayRing();
        }

        public void OnHoverStart()
        {
            if (!_isUnlocked) return;
            if (_outline) _outline.SetLit(true);
        }

        public void OnHoverEnd()
        {
            if (_outline) _outline.SetLit(false);
        }

        public void OnClick()
        {
            if (!_isUnlocked) return;
            PlayRing();
            GameEvents.RequestTuningFork();
        }

        public void PlayRing()
        {
            if (!_audioSource)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 0.6f;
            }

            if (_ringSound != null)
                _audioSource.PlayOneShot(_ringSound, 0.45f);

            if (_shakeCoroutine != null)
                StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            Transform target = _modelRoot != null ? _modelRoot.transform : transform;
            float elapsed = 0f;
            const float duration = 0.65f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - Mathf.Clamp01(elapsed / duration);
                float offset = Mathf.Sin(elapsed * 85f) * 0.0025f * decay;
                target.localPosition = _originPos + new Vector3(offset, 0f, 0f);
                yield return null;
            }

            target.localPosition = _originPos;
            _shakeCoroutine = null;
        }
    }
}
