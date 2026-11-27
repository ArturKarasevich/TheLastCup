using System;
using System.Collections;
using System.Collections.Generic;
using TheLastCup.Core;
using TheLastCup.Data;
using UnityEngine;

namespace TheLastCup.Presentation
{
    public class GuestCharacter : MonoBehaviour
    {
        [System.Serializable]
        public struct GuestModelItem
        {
            public string spiritId;
            public GameObject modelRoot;
            public Transform counterSeatPoint;
            public Transform[] entrancePath;
            public Transform[] tablePath;
            public Transform[] exitFromTablePath;
        }

        [Header("Models")]
        [SerializeField] private GuestModelItem[] _guestModels;

        [Header("Paths")]
        [SerializeField] private Transform _doorSpawnPoint;
        [SerializeField] private Transform[] _enterPath;
        [SerializeField] private Transform _twistDoorSpawnPoint;
        [SerializeField] private Transform[] _twistEnterPath;
        [SerializeField] private Transform[] _tablePath;
        [SerializeField] private Transform[] _exitCounterPath;
        [SerializeField] private Transform[] _exitTablePath;
        [SerializeField] private Transform _lookPoint;
        [SerializeField] private float _moveSpeed = 2.5f;

        private readonly HashSet<string> _stayingGuestIds = new HashSet<string>();
        private GuestInfo _pendingGuest;
        private GuestModelItem _currentModelEntry;
        private GameObject _currentModel;
        private GuestAnimator _currentAnimator;
        private Coroutine _moveCoroutine;

        private bool _isSitting;
        private bool _isGoingToTable;
        private bool _isLeaving;
        private bool _waitFadeToReturn;
        private bool _waitFadeToHide;
        private string _pendingTableSpiritId;
        private GameObject _pendingTableModel;

        private void OnEnable()
        {
            GameEvents.OnGuestArrived += OnGuestArrived;
            GameEvents.OnGuestDoorOpened += OnDoorOpened;
            GameEvents.OnTeaChecked += OnTeaChecked;
            GameEvents.OnGuestResultReached += OnGuestResultReached;
            GameEvents.OnGuestDone += OnGuestFinished;
            GameEvents.OnSnapCameraBackRequested += OnCameraReturnedToCounter;
            GameEvents.OnSnapCameraRequested += OnCameraSnappedForEnding;
            GameEvents.OnGuestExitDoorOpened += OnExitDoorOpened;
            GameEvents.OnDebugSnapGuest += OnDebugSnapGuest;
        }

        private void OnDisable()
        {
            GameEvents.OnGuestArrived -= OnGuestArrived;
            GameEvents.OnGuestDoorOpened -= OnDoorOpened;
            GameEvents.OnTeaChecked -= OnTeaChecked;
            GameEvents.OnGuestResultReached -= OnGuestResultReached;
            GameEvents.OnGuestDone -= OnGuestFinished;
            GameEvents.OnSnapCameraBackRequested -= OnCameraReturnedToCounter;
            GameEvents.OnSnapCameraRequested -= OnCameraSnappedForEnding;
            GameEvents.OnGuestExitDoorOpened -= OnExitDoorOpened;
            GameEvents.OnDebugSnapGuest -= OnDebugSnapGuest;
        }

        private void Awake()
        {
            MakeGuestModelsUnique();
            HideTempModels();
        }

        private void MakeGuestModelsUnique()
        {
            if (_guestModels == null) return;

            var usedRoots = new HashSet<GameObject>();
            for (int i = 0; i < _guestModels.Length; i++)
            {
                GameObject root = _guestModels[i].modelRoot;
                if (!root) continue;

                if (!usedRoots.Add(root))
                {
                    var clone = Instantiate(root, root.transform.parent);
                    clone.name = $"{root.name}_{_guestModels[i].spiritId}";
                    clone.SetActive(false);
                    _guestModels[i].modelRoot = clone;
                }
            }
        }

        private void OnGuestResultReached(string spiritId, TalkOutcomeData outcome)
        {
            if (outcome == null) return;

            if (outcome.staysInside)
                _stayingGuestIds.Add(spiritId);
        }

        private void OnGuestArrived(GuestInfo spirit)
        {
            _isSitting = false;
            _isGoingToTable = false;
            _isLeaving = false;
            _waitFadeToReturn = false;
            _waitFadeToHide = false;

            HideTempModels();
            _pendingGuest = spirit;
        }

        private void OnDoorOpened()
        {
            if (_pendingGuest == null || _guestModels == null) return;

            _currentModel = null;
            GuestModelItem? fallbackEntry = null;
            foreach (var entry in _guestModels)
            {
                if (!entry.modelRoot) continue;
                if (fallbackEntry == null)
                    fallbackEntry = entry;

                if (entry.spiritId == _pendingGuest.id)
                {
                    _currentModelEntry = entry;
                    _currentModel = entry.modelRoot;
                    break;
                }
            }

            if (!_currentModel && fallbackEntry.HasValue)
            {
                _currentModelEntry = fallbackEntry.Value;
                _currentModel = _currentModelEntry.modelRoot;
            }

            if (!_currentModel)
            {
                GameEvents.SendGuestAtCounter(_pendingGuest);
                return;
            }

            bool isTwist = _pendingGuest.isTwistGuest;
            Transform spawnPoint = isTwist && _twistDoorSpawnPoint ? _twistDoorSpawnPoint : _doorSpawnPoint;
            Transform[] fallbackEnter = isTwist ? GetValidPath(_twistEnterPath, _enterPath) : _enterPath;
            Transform[] enterPath = GetValidPath(_currentModelEntry.entrancePath, fallbackEnter);

            if (spawnPoint)
            {
                _currentModel.transform.position = spawnPoint.position;
                _currentModel.transform.rotation = spawnPoint.rotation;
            }
            else if (_currentModelEntry.counterSeatPoint && (enterPath == null || enterPath.Length == 0))
            {
                _currentModel.transform.position = _currentModelEntry.counterSeatPoint.position;
                _currentModel.transform.rotation = _currentModelEntry.counterSeatPoint.rotation;
            }

            _currentModel.SetActive(true);
            _currentAnimator = FindAnimator(_currentModel);
            if (_currentAnimator != null)
            {
                _currentAnimator.ResetAnimator();
                _currentAnimator.SetWalking(enterPath != null && enterPath.Length > 0);
            }

            StartMovingOnPath(enterPath, OnArrivedAtCounter);
        }

        private void OnArrivedAtCounter()
        {
            bool isTwist = _pendingGuest != null && _pendingGuest.isTwistGuest;
            if (_currentAnimator != null)
            {
                _currentAnimator.SetWalking(false);
                if (isTwist)
                    _currentAnimator.SetCollapsed(true);
                else if (_currentModelEntry.counterSeatPoint != null)
                    _currentAnimator.SetSitting(true);
            }

            LookAtGuest();
            GameEvents.SendGuestAtCounter(_pendingGuest);
        }

        private void OnTeaChecked(Domain.TeaResult result, GuestInfo spirit)
        {
            if (result != null && _currentAnimator != null)
                _currentAnimator.PlayTeaReaction(result.MatchCount);

            if (spirit != null && spirit.isTwistGuest)
            {
                LookAtGuest();
                return;
            }

            Transform[] tablePath = GetValidPath(_currentModelEntry.tablePath, _tablePath);
            if (result != null && result.MatchCount >= 2 && _currentModel && tablePath != null && tablePath.Length > 0)
            {
                _isSitting = true;
                _isGoingToTable = true;
                if (_currentAnimator != null)
                {
                    _currentAnimator.SetSitting(false);
                    _currentAnimator.SetWalking(true);
                }
                _pendingTableSpiritId = spirit.id;
                _pendingTableModel = _currentModel;
                StartMovingOnPath(tablePath, OnArrivedAtTable);
                return;
            }

            LookAtGuest();
        }

        private void OnArrivedAtTable()
        {
            _isGoingToTable = false;
            if (_currentAnimator != null)
            {
                _currentAnimator.SetWalking(false);
                _currentAnimator.SetSitting(true);
            }
            GameEvents.SendGuestSatAtTable(_pendingTableSpiritId, _pendingTableModel);
        }

        private void OnGuestFinished()
        {
            if (_isSitting)
            {
                _waitFadeToReturn = true;
                return;
            }

            StartGuestExit();
        }

        private void OnCameraReturnedToCounter()
        {
            if (!_waitFadeToReturn) return;
            _waitFadeToReturn = false;

            StartGuestExit();
        }

        private void OnCameraSnappedForEnding(Transform _)
        {
            if (!_waitFadeToHide) return;
            _waitFadeToHide = false;

            _isSitting = false;
            HideTempModels();
            _pendingGuest = null;
        }

        private void StartGuestExit()
        {
            if (_pendingGuest != null && _pendingGuest.isTwistGuest)
            {
                _waitFadeToHide = true;
                GameEvents.SendGuestLeft();
                return;
            }

            if (_pendingGuest != null && _stayingGuestIds.Contains(_pendingGuest.id))
            {
                _stayingGuestIds.Add(_pendingGuest.id);
                _isSitting = false;
                _currentModel = null;
                GameEvents.SendGuestLeft();
                return;
            }

            _isLeaving = true;
            GameEvents.RequestGuestExitDoor();
        }

        private void OnExitDoorOpened()
        {
            if (!_isLeaving) return;

            Transform[] exitPath = _isSitting
                ? GetValidPath(_currentModelEntry.exitFromTablePath, _exitTablePath)
                : _exitCounterPath;

            if (_currentModel && exitPath != null && exitPath.Length > 0)
            {
                if (_currentAnimator != null)
                {
                    _currentAnimator.PlayLeaveAnimation();
                    _currentAnimator.SetWalking(true);
                }
                StartMovingOnPath(exitPath, FinishExit);
            }
            else
            {
                FinishExit();
            }
        }

        private static GuestAnimator FindAnimator(GameObject model)
        {
            if (!model) return null;

            GuestAnimator anim = model.GetComponent<GuestAnimator>();
            if (anim == null)
                anim = model.AddComponent<GuestAnimator>();
            else
                anim.FindAnimator();

            return anim;
        }

        private void FinishExit()
        {
            _isLeaving = false;
            _isSitting = false;
            HideTempModels();
            GameEvents.SendGuestLeft();
        }

        private void OnDebugSnapGuest()
        {
            if (!_currentModel) return;

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            if (_isLeaving)
            {
                FinishExit();
            }
            else if (_isGoingToTable)
            {
                _isGoingToTable = false;
                Transform[] tablePath = GetValidPath(_currentModelEntry.tablePath, _tablePath);
                SnapToPathEnd(tablePath);
                if (_pendingGuest != null)
                    GameEvents.SendGuestSatAtTable(_pendingGuest.id, _currentModel);
            }
            else if (_pendingGuest != null)
            {
                SnapToPathEnd(_enterPath);
                LookAtGuest();
                GameEvents.SendGuestAtCounter(_pendingGuest);
            }
        }

        private void SnapToPathEnd(Transform[] path)
        {
            if (path == null || path.Length == 0 || !_currentModel) return;

            Transform last = path[path.Length - 1];
            if (!last) return;

            _currentModel.transform.position = last.position;
            _currentModel.transform.rotation = last.rotation;
        }

        private void StartMovingOnPath(Transform[] path, Action onComplete)
        {
            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            if (path == null || path.Length == 0 || !_currentModel)
            {
                if (onComplete != null) onComplete.Invoke();
                return;
            }

            _moveCoroutine = StartCoroutine(FollowPathRoutine(path, onComplete));
        }

        private IEnumerator FollowPathRoutine(Transform[] path, Action onComplete)
        {
            for (int i = 0; i < path.Length; i++)
            {
                Transform point = path[i];
                if (!point) continue;

                while (Vector3.Distance(_currentModel.transform.position, point.position) > 0.05f ||
                       Quaternion.Angle(_currentModel.transform.rotation, point.rotation) > 2.0f)
                {
                    _currentModel.transform.position = Vector3.MoveTowards(
                        _currentModel.transform.position,
                        point.position,
                        Time.deltaTime * _moveSpeed
                    );
                    _currentModel.transform.rotation = Quaternion.RotateTowards(
                        _currentModel.transform.rotation,
                        point.rotation,
                        Time.deltaTime * 90f
                    );
                    yield return null;
                }

                _currentModel.transform.position = point.position;
                _currentModel.transform.rotation = point.rotation;
            }

            if (onComplete != null) onComplete.Invoke();
        }

        private Transform[] GetValidPath(Transform[] primary, Transform[] fallback)
        {
            return (primary != null && primary.Length > 0) ? primary : fallback;
        }

        private void LookAtGuest()
        {
            Transform target = _lookPoint;
            if (!target && _enterPath != null && _enterPath.Length > 0)
                target = _enterPath[_enterPath.Length - 1];

            if (target)
                GameEvents.RequestCameraLook(target);
        }

        private void HideTempModels()
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            _currentModel = null;
            if (_guestModels == null) return;

            foreach (var entry in _guestModels)
            {
                if (entry.modelRoot && !_stayingGuestIds.Contains(entry.spiritId))
                    entry.modelRoot.SetActive(false);
            }
        }
    }
}
