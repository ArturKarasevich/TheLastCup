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
            GameEvents.OnSnapCameraRequested += OnCameraSnapped;
        }

        private void OnDisable()
        {
            GameEvents.OnTeaChecked -= OnTeaChecked;
            GameEvents.OnGuestSatAtTable -= OnGuestSatAtTable;
            GameEvents.OnGuestDone -= OnGuestDone;
            GameEvents.OnSnapCameraRequested -= OnCameraSnapped;
        }

        private void OnGuestSatAtTable(string spiritId, GameObject modelRoot)
        {
            if (_isSitting) return;
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

            ClearHighlight();
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

            ClearHighlight();

            GameEvents.RequestFadeMove(_cameraPoint, OnTableFadeComplete);
        }

        private void OnCameraSnapped(Transform target)
        {
            if (target == _cameraPoint)
            {
                _waitingForPlayer = false;
                ClearHighlight();
            }
        }

        private void ClearHighlight()
        {
            if (_sittingModel)
            {
                RemoveClickProxy(_sittingModel);
                if (_outline)
                    _outline.RemoveRenderer(_sittingModel);

                OutlineEffect[] modelOutlines = _sittingModel.GetComponentsInChildren<OutlineEffect>(true);
                for (int i = 0; i < modelOutlines.Length; i++)
                {
                    modelOutlines[i].SetPulse(false);
                    modelOutlines[i].SetLit(false);
                }

                _sittingModel = null;
            }

            if (_outline)
            {
                _outline.SetPulse(false);
                _outline.SetLit(false);
            }
        }

        private void OnTableFadeComplete()
        {
            GameEvents.SendTableTalkStarted(_savedResult, _savedGuest);
        }
    }
}
