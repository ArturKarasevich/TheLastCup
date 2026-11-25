using System.Collections;
using TheLastCup.Core;
using TheLastCup.Data;
using UnityEngine;

namespace TheLastCup.Presentation
{
    public enum TrainDoorType
    {
        Entrance,
        Exit
    }

    public class TrainDoor : MonoBehaviour
    {
        [SerializeField] private TrainDoorType _doorType = TrainDoorType.Entrance;

        [Header("Door Motion")]
        [SerializeField] private Transform _doorObject;
        [SerializeField] private bool _slideDoor;
        [SerializeField] private Vector3 _openAngle = new Vector3(0f, 90f, 0f);
        [SerializeField] private Vector3 _openOffset = new Vector3(1.2f, 0f, 0f);
        [SerializeField] private float _doorSpeed = 4f;

        [Header("Twist Blizzard (for Exit on twist)")]
        [SerializeField] private ParticleSystem _snowBurstEffect;
        [SerializeField] private float _snowTime = 4.5f;
        [SerializeField] private float _autoCloseDelay = 0.85f;

        private Vector3 _closedPos;
        private Quaternion _closedRot;
        private Coroutine _moveCoroutine;
        private Coroutine _snowCoroutine;
        private Coroutine _autoCloseCoroutine;
        private bool _openingForTwist;
        private Vector3 _snowWorldPos;
        private Quaternion _snowWorldRot;
        private bool _hasSnowPoint;

        private void OnEnable()
        {
            GameEvents.OnGuestArrived += OnGuestArrived;
            GameEvents.OnGuestAtCounter += OnGuestAtCounter;

            if (_doorType == TrainDoorType.Exit)
            {
                GameEvents.OnGuestExitDoorRequested += OnExitDoorRequested;
                GameEvents.OnGuestLeft += OnGuestLeft;
            }
        }

        private void OnDisable()
        {
            GameEvents.OnGuestArrived -= OnGuestArrived;
            GameEvents.OnGuestAtCounter -= OnGuestAtCounter;

            if (_doorType == TrainDoorType.Exit)
            {
                GameEvents.OnGuestExitDoorRequested -= OnExitDoorRequested;
                GameEvents.OnGuestLeft -= OnGuestLeft;
            }
        }

        private void Awake()
        {
            if (!_doorObject)
                _doorObject = transform;

            _closedPos = _doorObject.localPosition;
            _closedRot = _doorObject.localRotation;

            if (_snowBurstEffect)
            {
                _snowWorldPos = _snowBurstEffect.transform.position;
                _snowWorldRot = _snowBurstEffect.transform.rotation;
                _hasSnowPoint = true;
            }
        }

        private void LateUpdate()
        {
            if (_hasSnowPoint && _snowBurstEffect)
                _snowBurstEffect.transform.SetPositionAndRotation(_snowWorldPos, _snowWorldRot);
        }

        private bool IsMatchingDoor(GuestInfo spirit)
        {
            bool isTwist = spirit != null && spirit.isTwistGuest;
            return _doorType == TrainDoorType.Entrance ? !isTwist : isTwist;
        }

        private void OnGuestArrived(GuestInfo spirit)
        {
            if (IsMatchingDoor(spirit))
            {
                _openingForTwist = spirit != null && spirit.isTwistGuest;
                OpenDoor();
            }
        }

        private void OnGuestAtCounter(GuestInfo spirit)
        {
            if (IsMatchingDoor(spirit))
                CloseDoor();
        }

        private void OnExitDoorRequested()
        {
            _openingForTwist = false;
            OpenDoor();
        }

        private void OnGuestLeft()
        {
            CloseDoor();
        }

        public void OpenDoor()
        {
            bool asTwist = _openingForTwist;
            if (asTwist)
                PlaySnowBurst();

            Vector3 targetPos = _slideDoor ? _closedPos + _openOffset : _closedPos;
            Quaternion targetRot = _slideDoor ? _closedRot : _closedRot * Quaternion.Euler(_openAngle);

            StartMove(targetPos, targetRot, OnOpenDoorComplete);
        }

        private void OnOpenDoorComplete()
        {
            if (_openingForTwist || _doorType == TrainDoorType.Entrance)
            {
                GameEvents.SendGuestDoorOpened();
                if (_openingForTwist)
                    StartAutoClose();
            }
            else
            {
                GameEvents.SendGuestExitDoorOpened();
            }
        }

        private void StartAutoClose()
        {
            if (_autoCloseCoroutine != null)
                StopCoroutine(_autoCloseCoroutine);
            _autoCloseCoroutine = StartCoroutine(AutoCloseRoutine());
        }

        private IEnumerator AutoCloseRoutine()
        {
            yield return new WaitForSeconds(_autoCloseDelay);
            _autoCloseCoroutine = null;
            CloseDoor();
        }

        private void PlaySnowBurst()
        {
            if (_snowBurstEffect == null) return;

            _snowBurstEffect.Play();
            if (_snowCoroutine != null)
                StopCoroutine(_snowCoroutine);
            _snowCoroutine = StartCoroutine(StopSnowLater());
        }

        private IEnumerator StopSnowLater()
        {
            yield return new WaitForSeconds(_snowTime);
            if (_snowBurstEffect != null)
                _snowBurstEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            _snowCoroutine = null;
        }

        public void CloseDoor()
        {
            if (_autoCloseCoroutine != null)
            {
                StopCoroutine(_autoCloseCoroutine);
                _autoCloseCoroutine = null;
            }

            StartMove(_closedPos, _closedRot, StopSnowBurst);
        }

        private void StopSnowBurst()
        {
            if (_snowCoroutine != null)
            {
                StopCoroutine(_snowCoroutine);
                _snowCoroutine = null;
            }

            if (_snowBurstEffect != null)
                _snowBurstEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void StartMove(Vector3 targetPos, Quaternion targetRot, System.Action onComplete)
        {
            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _moveCoroutine = StartCoroutine(MoveDoorRoutine(targetPos, targetRot, onComplete));
        }

        private IEnumerator MoveDoorRoutine(Vector3 targetPos, Quaternion targetRot, System.Action onComplete)
        {
            while (Vector3.Distance(_doorObject.localPosition, targetPos) > 0.005f ||
                   Quaternion.Angle(_doorObject.localRotation, targetRot) > 0.5f)
            {
                _doorObject.localPosition = Vector3.Lerp(_doorObject.localPosition, targetPos, Time.deltaTime * _doorSpeed);
                _doorObject.localRotation = Quaternion.Slerp(_doorObject.localRotation, targetRot, Time.deltaTime * _doorSpeed);
                yield return null;
            }

            _doorObject.localPosition = targetPos;
            _doorObject.localRotation = targetRot;
            _moveCoroutine = null;
            if (onComplete != null)
                onComplete.Invoke();
        }
    }
}
