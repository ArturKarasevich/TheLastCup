using TheLastCup.Core;
using TheLastCup.Data;
using TheLastCup.Domain;
using UnityEngine;

namespace TheLastCup.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class GuestTable : MonoBehaviour, IClickable
    {
        [SerializeField] private string _guestId;
        [SerializeField] private Transform _cameraPoint;
        [SerializeField] private OutlineEffect _outline;

        private TeaResult _savedResult;
        private GuestInfo _savedGuest;
        private GameObject _sittingModel;
        private bool _waitingForPlayer;
        private bool _isSitting;

        private void OnEnable()
        {
            GameEvents.OnTeaChecked += OnTeaChecked;
            GameEvents.OnGuestSatAtTable += OnGuestSatAtTable;
            GameEvents.OnGuestDone += OnGuestDone;
        }

        private void OnDisable()
        {
            GameEvents.OnTeaChecked -= OnTeaChecked;
            GameEvents.OnGuestSatAtTable -= OnGuestSatAtTable;
            GameEvents.OnGuestDone -= OnGuestDone;
        }

        private void OnGuestSatAtTable(string spiritId, GameObject modelRoot)
        {
            if (!string.IsNullOrEmpty(_guestId) && _guestId != spiritId)
                return;

            _sittingModel = modelRoot;
            if (_sittingModel)
            {
                if (_outline) _outline.AddRenderer(_sittingModel);
                AddClickProxy(_sittingModel);
            }

            if (_savedResult != null)
            {
                _waitingForPlayer = true;
                if (_outline)
                    _outline.SetPulse(true);
            }
        }

        private void Awake()
        {
            if (!_outline)
                _outline = GetComponent<OutlineEffect>();

            if (_outline && _outline.ExtraTargets != null)
            {
                foreach (var extra in _outline.ExtraTargets)
                {
                    if (extra) AddClickProxy(extra);
                }
            }
        }

        private void AddClickProxy(GameObject targetRoot)
        {
            var colliders = targetRoot.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                ClickProxy proxy;
                if (!col.TryGetComponent<ClickProxy>(out proxy))
                    proxy = col.gameObject.AddComponent<ClickProxy>();
                proxy.Setup(this);
            }
        }

        private void RemoveClickProxy(GameObject targetRoot)
        {
            if (!targetRoot) return;
            var proxies = targetRoot.GetComponentsInChildren<ClickProxy>(true);
            foreach (var proxy in proxies)
                Destroy(proxy);
        }

        private void OnTeaChecked(TeaResult result, GuestInfo spirit)
        {
            if (result == null || result.MatchCount < 2 || spirit == null)
                return;

            if (!string.IsNullOrEmpty(_guestId) && _guestId != spirit.id)
                return;

            _savedResult = result;
            _savedGuest = spirit;
        }

        private void OnGuestDone()
        {
            if (_isSitting)
            {
                _isSitting = false;
                GameEvents.RequestFadeReturn();
            }

            _waitingForPlayer = false;
            _savedResult = null;
            _savedGuest = null;

            if (_outline)
            {
                if (_sittingModel)
                {
                    RemoveClickProxy(_sittingModel);
                    _outline.RemoveRenderer(_sittingModel);
                    _sittingModel = null;
                }
                _outline.SetPulse(false);
                _outline.SetLit(false);
            }
        }

        public void OnHoverStart()
        {
            if (!_waitingForPlayer) return;

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
            if (!_waitingForPlayer || !_cameraPoint) return;

            _waitingForPlayer = false;
            _isSitting = true;

            if (_outline)
            {
                if (_sittingModel)
                {
                    RemoveClickProxy(_sittingModel);
                    _outline.RemoveRenderer(_sittingModel);
                    _sittingModel = null;
                }
                _outline.SetPulse(false);
                _outline.SetLit(false);
            }

            GameEvents.RequestFadeMove(_cameraPoint, OnTableFadeComplete);
        }

        private void OnTableFadeComplete()
        {
            GameEvents.SendTableTalkStarted(_savedResult, _savedGuest);
        }
    }
}
