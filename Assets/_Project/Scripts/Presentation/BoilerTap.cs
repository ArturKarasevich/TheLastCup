using System.Collections;
using TheLastCup.Core;
using UnityEngine;

namespace TheLastCup.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class BoilerTap : MonoBehaviour, IClickable
    {
        [SerializeField] private ParticleSystem _waterEffect;
        [SerializeField] private Transform _tapHandle;
        [SerializeField] private Vector3 _openAngle = new Vector3(0f, 0f, -45f);
        [SerializeField] private float _pourTime = 1.5f;
        [SerializeField] private OutlineEffect _outline;

        [Header("Audio")]
        [SerializeField] private AudioSource _pourAudioSource;
        [SerializeField] private AudioSource _boilAudioSource;
        [SerializeField] private GameObject _tapHalo;

        private bool _isActive;
        private bool _isPouring;

        public bool IsActive { get { return _isActive; } }
        public bool IsPouring { get { return _isPouring; } }
        private Quaternion _closedRotation;

        private void OnEnable()
        {
            GameEvents.OnTeapotPlacedUnderBoiler += OnTeapotPlaced;
            GameEvents.OnTeapotBroken += OnReset;
            GameEvents.OnResetTeaCycle += OnReset;
            GameEvents.OnTeaDumped += OnReset;
        }

        private void OnDisable()
        {
            GameEvents.OnTeapotPlacedUnderBoiler -= OnTeapotPlaced;
            GameEvents.OnTeapotBroken -= OnReset;
            GameEvents.OnResetTeaCycle -= OnReset;
            GameEvents.OnTeaDumped -= OnReset;
        }

        private void Awake()
        {
            if (!_tapHandle)
                _tapHandle = transform;

            _closedRotation = _tapHandle.localRotation;

            if (!_outline)
                _outline = GetComponent<OutlineEffect>();

            if (!_pourAudioSource)
                _pourAudioSource = GetComponent<AudioSource>();

            if (_boilAudioSource && _boilAudioSource.clip)
            {
                _boilAudioSource.loop = true;
                if (!_boilAudioSource.isPlaying)
                    _boilAudioSource.Play();
            }

            SetHaloVisible(false);
        }

        private void SetHaloVisible(bool visible)
        {
            if (_tapHalo)
                _tapHalo.SetActive(visible);
        }

        private void OnTeapotPlaced(Transform slotPoint)
        {
            _isActive = true;
            SetHaloVisible(true);
            if (_outline)
                _outline.SetPulse(true);
        }

        private void OnReset()
        {
            StopAllCoroutines();
            _isActive = false;
            _isPouring = false;

            if (_waterEffect != null)
                _waterEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (_pourAudioSource != null && _pourAudioSource.isPlaying)
                _pourAudioSource.Stop();

            if (_tapHandle != null)
                _tapHandle.localRotation = _closedRotation;

            if (_outline != null)
            {
                _outline.SetPulse(false);
                _outline.SetLit(false);
            }
            SetHaloVisible(false);
        }

        public void OnHoverStart()
        {
            if (!_isActive || _isPouring) return;

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
            if (!_isActive || _isPouring) return;

            _isActive = false;
            SetHaloVisible(false);
            if (_outline)
            {
                _outline.SetPulse(false);
                _outline.SetLit(false);
            }

            StartCoroutine(PourWaterRoutine());
        }

        private IEnumerator PourWaterRoutine()
        {
            _isPouring = true;

            yield return RotateTapRoutine(_closedRotation * Quaternion.Euler(_openAngle), 0.25f);

            if (_waterEffect)
                _waterEffect.Play();

            if (_pourAudioSource && _pourAudioSource.clip)
            {
                _pourAudioSource.loop = false;
                _pourAudioSource.Play();
            }

            yield return new WaitForSeconds(_pourTime);

            if (_waterEffect)
                _waterEffect.Stop();

            yield return RotateTapRoutine(_closedRotation, 0.25f);

            _isPouring = false;
            GameEvents.RequestHotWater();

            if (_pourAudioSource && _pourAudioSource.isPlaying)
                StartCoroutine(StopPourAudioDelayed(0.5f));
        }

        private IEnumerator RotateTapRoutine(Quaternion targetRotation, float duration)
        {
            if (!_tapHandle) yield break;

            Quaternion startRotation = _tapHandle.localRotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                _tapHandle.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }

            _tapHandle.localRotation = targetRotation;
        }

        private IEnumerator StopPourAudioDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (_pourAudioSource && _pourAudioSource.isPlaying)
                _pourAudioSource.Stop();
        }
    }
}
