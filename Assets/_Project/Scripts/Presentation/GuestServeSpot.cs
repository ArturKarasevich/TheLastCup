using TheLastCup.Core;
using UnityEngine;

namespace TheLastCup.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class GuestServeSpot : MonoBehaviour
    {
        [SerializeField] private Transform _servePoint;
        [SerializeField] private OutlineEffect _outline;

        private bool _isTeaReady;
        private bool _isActive;
        private bool _hasTeapot;
        private int _teapotColliderCount;

        private void OnEnable()
        {
            GameEvents.OnTeaBrewed += OnTeaBrewed;
            GameEvents.OnTeapotGrabbed += OnTeapotGrabbed;
            GameEvents.OnTeapotDropped += OnTeapotDropped;
            GameEvents.OnTeapotBroken += OnReset;
            GameEvents.OnResetTeaCycle += OnReset;
            GameEvents.OnTeaDumped += OnReset;
        }

        private void OnDisable()
        {
            GameEvents.OnTeaBrewed -= OnTeaBrewed;
            GameEvents.OnTeapotGrabbed -= OnTeapotGrabbed;
            GameEvents.OnTeapotDropped -= OnTeapotDropped;
            GameEvents.OnTeapotBroken -= OnReset;
            GameEvents.OnResetTeaCycle -= OnReset;
            GameEvents.OnTeaDumped -= OnReset;
        }

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;

            if (!_servePoint)
                _servePoint = transform;

            if (!_outline)
                _outline = GetComponent<OutlineEffect>();
        }

        private void OnTeaBrewed()
        {
            _isTeaReady = true;
        }

        private void OnTeapotGrabbed(Transform teapot)
        {
            if (!_isTeaReady || !teapot) return;

            Teapot station;

            if (teapot.TryGetComponent<Teapot>(out station) && station.State == TeapotState.CarriedForGuest)
            {
                _isActive = true;
                if (_outline)
                    _outline.SetPulse(true);
            }
        }

        private void OnTeapotDropped()
        {
            if (!_isActive) return;

            if (_outline)
                _outline.SetPulse(false);

            if (_hasTeapot)
            {
                _isActive = false;
                _isTeaReady = false;
                _hasTeapot = false;
                _teapotColliderCount = 0;

                if (_outline)
                    _outline.SetLit(false);

                GameEvents.ServeTeapot(_servePoint);
            }
            else
            {
                _isActive = false;
            }
        }

        private void OnReset()
        {
            _isTeaReady = false;
            _isActive = false;
            _hasTeapot = false;
            _teapotColliderCount = 0;

            if (_outline)
            {
                _outline.SetPulse(false);
                _outline.SetLit(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<Teapot>() == null) return;

            _teapotColliderCount++;
            _hasTeapot = true;
            if (_isActive && _outline)
                _outline.SetLit(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<Teapot>() == null) return;

            _teapotColliderCount = Mathf.Max(0, _teapotColliderCount - 1);
            _hasTeapot = _teapotColliderCount > 0;
            if (!_hasTeapot && _outline)
                _outline.SetLit(false);
        }
    }
}
