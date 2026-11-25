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
        }

        private void OnDisable()
        {
            GameEvents.OnTeapotPlacedUnderBoiler -= OnTeapotPlaced;
            GameEvents.OnTeapotBroken -= OnReset;
            GameEvents.OnResetTeaCycle -= OnReset;
        }

        private void Awake()
        {
            if (_tapHandle)
                _closedRotation = _tapHandle.localRotation;

            if (!_outline)
                _outline = GetComponent<OutlineEffect>();
        }

        private void OnTeapotPlaced(Transform slotPoint)
        {
            _isActive = true;
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

            if (_tapHandle != null)
                _tapHandle.localRotation = _closedRotation;

            if (_outline != null)
            {
                _outline.SetPulse(false);
                _outline.SetLit(false);
            }
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

            if (_tapHandle)
                _tapHandle.localRotation = Quaternion.Euler(_openAngle);

            if (_waterEffect)
                _waterEffect.Play();

            yield return new WaitForSeconds(_pourTime);

            if (_waterEffect)
                _waterEffect.Stop();

            if (_tapHandle)
                _tapHandle.localRotation = _closedRotation;

            _isPouring = false;
            GameEvents.RequestHotWater();
        }
    }
}
