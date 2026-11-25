using System.Collections;
using System.Collections.Generic;
using TheLastCup.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLastCup.Presentation
{
    public class CameraController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _mouseSensitivity = 0.06f;
        [SerializeField] private bool _invertY = false;

        [Header("Limits")]
        [SerializeField] private float _maxHorizontalAngle = 120f;
        [SerializeField] private float _maxVerticalAngle = 80f;

        [Header("Transition")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _turnSpeed = 4f;

        [Header("Ending View")]
        [SerializeField] private Transform _endingCameraPoint;

        private float _horizontalAngle;
        private float _verticalAngle;
        private Quaternion _startRotation;
        private Vector3 _startPosition;

        private Vector3 _savedPosition;
        private Quaternion _savedRotation;
        private float _savedHorizontalAngle;
        private float _savedVerticalAngle;

        private Vector3 _counterPosition;
        private Quaternion _counterRotation;
        private bool _hasCounterSaved;
        private bool _isAtGuestTable;

        private struct CameraFocusState
        {
            public Transform Target;
            public Vector3 OldPosition;
            public Quaternion OldRotation;
            public float OldHorizontalAngle;
            public float OldVerticalAngle;
        }

        private readonly Stack<CameraFocusState> _focusHistory = new Stack<CameraFocusState>();
        private Transform _currentTarget;
        private Coroutine _moveCoroutine;
        private Transform _endingPointInstance;
        private bool _isLocked;
        private bool _isEndingLocked;
        private bool _isTurningToTarget;
        private int _skipMouseFrames;
        private float _pendingYaw;
        private float _pendingPitch;

        public bool IsLocked { get { return _isLocked; } }

        private void OnEnable()
        {
            GameEvents.OnCameraFocusRequested += FocusOnTarget;
            GameEvents.OnCameraFreeLookRequested += ReturnToFreeLook;
            GameEvents.OnCameraReturnAndLookRequested += ReturnAndTurnToTarget;
            GameEvents.OnCameraLookRequested += TurnToTarget;
            GameEvents.OnSnapCameraRequested += SnapToPoint;
            GameEvents.OnSnapCameraBackRequested += SnapBackToCounter;
            GameEvents.OnUIModeChanged += OnUIModeChanged;
            GameEvents.OnSessionEnded += OnSessionEnded;
        }

        private void OnDisable()
        {
            GameEvents.OnCameraFocusRequested -= FocusOnTarget;
            GameEvents.OnCameraFreeLookRequested -= ReturnToFreeLook;
            GameEvents.OnCameraReturnAndLookRequested -= ReturnAndTurnToTarget;
            GameEvents.OnCameraLookRequested -= TurnToTarget;
            GameEvents.OnSnapCameraRequested -= SnapToPoint;
            GameEvents.OnSnapCameraBackRequested -= SnapBackToCounter;
            GameEvents.OnUIModeChanged -= OnUIModeChanged;
            GameEvents.OnSessionEnded -= OnSessionEnded;
        }

        private void OnDestroy()
        {
            if (_endingPointInstance)
                Destroy(_endingPointInstance.gameObject);
        }

        private void OnSessionEnded(SessionSummary summary)
        {
            _focusHistory.Clear();
            _isTurningToTarget = false;
            _isEndingLocked = true;
            _isLocked = true;
            LockCursor(false);
            GameEvents.SetCameraLocked(true);

            Transform target = _endingCameraPoint;
            if (!target)
            {
                if (!_endingPointInstance)
                    _endingPointInstance = new GameObject("GeneratedEndingWindowPoint").transform;

                _endingPointInstance.position = _isAtGuestTable ? _counterPosition : _startPosition;
                _endingPointInstance.rotation = Quaternion.LookRotation(new Vector3(0.25f, 0.02f, 1f).normalized, Vector3.up);
                target = _endingPointInstance;
            }

            GameEvents.RequestFadeMove(target, OnEndingFadeComplete);
        }

        private void OnEndingFadeComplete()
        {
            _isLocked = true;
            LockCursor(false);
            GameEvents.SetCameraLocked(true);
        }

        private void SaveCounterPositionIfNeeded()
        {
            if (_hasCounterSaved) return;
            _counterPosition = transform.position;
            _counterRotation = transform.rotation;
            _hasCounterSaved = true;
        }

        public void MoveToMenuWindow()
        {
            SaveCounterPositionIfNeeded();

            Transform target = _endingCameraPoint;
            if (!target)
            {
                if (!_endingPointInstance)
                    _endingPointInstance = new GameObject("GeneratedMenuWindowPoint").transform;

                _endingPointInstance.position = _counterPosition;
                _endingPointInstance.rotation = Quaternion.LookRotation(new Vector3(0.25f, 0.02f, 1f).normalized, Vector3.up);
                target = _endingPointInstance;
            }

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            _focusHistory.Clear();
            _currentTarget = null;
            _isTurningToTarget = false;
            _isLocked = true;
            LockCursor(false);
            GameEvents.SetCameraLocked(true);

            transform.position = target.position;
            transform.rotation = target.rotation;
            _startPosition = target.position;
            _startRotation = target.rotation;
            _horizontalAngle = 0f;
            _verticalAngle = 0f;
        }

        public void MoveFromMenuToCounter()
        {
            _isLocked = true;
            _isEndingLocked = false;
            _currentTarget = null;
            _startPosition = _counterPosition;
            _startRotation = _counterRotation;
            _horizontalAngle = 0f;
            _verticalAngle = 0f;

            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _moveCoroutine = StartCoroutine(ReturnFromMenuRoutine());
        }

        private IEnumerator ReturnFromMenuRoutine()
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float elapsed = 0f;
            const float duration = 0.85f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                transform.position = Vector3.Lerp(startPos, _counterPosition, t);
                transform.rotation = Quaternion.Slerp(startRot, _counterRotation, t);
                yield return null;
            }

            transform.position = _counterPosition;
            transform.rotation = _counterRotation;
            _horizontalAngle = 0f;
            _verticalAngle = 0f;
            _moveCoroutine = null;
            _isLocked = false;
            LockCursor(true);
            GameEvents.SetCameraLocked(false);
        }

        private void SnapToPoint(Transform target)
        {
            if (!target) return;

            _isTurningToTarget = false;
            _focusHistory.Clear();
            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            if (!_isAtGuestTable && !_isEndingLocked)
            {
                _counterPosition = _startPosition;
                _counterRotation = _startRotation;
                _isAtGuestTable = true;
            }

            _currentTarget = null;
            _startPosition = target.position;
            _startRotation = target.rotation;
            _horizontalAngle = 0f;
            _verticalAngle = 0f;

            transform.position = _startPosition;
            transform.rotation = _startRotation;
            _isLocked = _isEndingLocked;
            LockCursor(!_isEndingLocked);
            GameEvents.SetCameraLocked(_isEndingLocked);
        }

        private void SnapBackToCounter()
        {
            if (!_isAtGuestTable) return;

            _isTurningToTarget = false;
            _focusHistory.Clear();
            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _isAtGuestTable = false;
            _currentTarget = null;
            _startPosition = _counterPosition;
            _startRotation = _counterRotation;
            _horizontalAngle = 0f;
            _verticalAngle = 0f;

            transform.position = _startPosition;
            transform.rotation = _startRotation;
            _isLocked = false;
            LockCursor(true);
            GameEvents.SetCameraLocked(false);
        }

        private void OnUIModeChanged(bool isUiActive)
        {
            if (_isEndingLocked || (!isUiActive && _currentTarget != null))
            {
                _isLocked = true;
                LockCursor(false);
                GameEvents.SetCameraLocked(true);
                return;
            }

            _isLocked = isUiActive;
            LockCursor(!isUiActive);
            GameEvents.SetCameraLocked(isUiActive);
        }

        private void Start()
        {
            SaveCounterPositionIfNeeded();
            if (_isLocked)
                return;

            _startPosition = _counterPosition;
            _startRotation = _counterRotation;
            _savedPosition = _startPosition;
            _savedRotation = _startRotation;

            LockCursor(true);
            GameEvents.SetCameraLocked(false);
        }

        private void Update()
        {
            if (_isLocked || _isEndingLocked || _currentTarget != null || _isTurningToTarget)
                return;

            if (_skipMouseFrames > 0)
            {
                _skipMouseFrames--;
                return;
            }

            Vector2 delta = GetMouseInput();
            float sens = _mouseSensitivity * GameSettings.CameraSensitivity;
            bool invert = GameSettings.InvertY || _invertY;

            _horizontalAngle += delta.x * sens;
            _horizontalAngle = Mathf.Clamp(_horizontalAngle, -_maxHorizontalAngle, _maxHorizontalAngle);

            float pitchDelta = delta.y * sens * (invert ? 1f : -1f);
            _verticalAngle += pitchDelta;
            _verticalAngle = Mathf.Clamp(_verticalAngle, -_maxVerticalAngle, _maxVerticalAngle);

            Quaternion yawRot = Quaternion.AngleAxis(_horizontalAngle, Vector3.up);
            Quaternion pitchRot = Quaternion.AngleAxis(_verticalAngle, Vector3.right);

            transform.rotation = _startRotation * yawRot * pitchRot;
        }

        private Vector2 GetMouseInput()
        {
            if (Mouse.current == null)
                return Vector2.zero;

            Vector2 delta = Mouse.current.delta.ReadValue();
            if (delta.sqrMagnitude < 0.001f)
                return Vector2.zero;

            return Vector2.ClampMagnitude(delta, 100f);
        }

        public void FocusOnTarget(Transform target)
        {
            if (target == null) return;

            if (_currentTarget != null && _currentTarget != target)
            {
                _focusHistory.Push(new CameraFocusState
                {
                    Target = _currentTarget,
                    OldPosition = _savedPosition,
                    OldRotation = _savedRotation,
                    OldHorizontalAngle = _savedHorizontalAngle,
                    OldVerticalAngle = _savedVerticalAngle
                });
            }

            _isTurningToTarget = false;
            _savedPosition = transform.position;
            _savedRotation = transform.rotation;
            _savedHorizontalAngle = _horizontalAngle;
            _savedVerticalAngle = _verticalAngle;

            _currentTarget = target;
            _isLocked = true;
            LockCursor(false);
            GameEvents.SetCameraLocked(true);

            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _moveCoroutine = StartCoroutine(SmoothMoveRoutine(target.position, target.rotation));
        }

        public void ReturnToFreeLook()
        {
            _isTurningToTarget = false;

            if (_focusHistory.Count > 0)
            {
                CameraFocusState previous = _focusHistory.Pop();
                if (previous.Target != null)
                {
                    _currentTarget = previous.Target;
                    _savedPosition = previous.OldPosition;
                    _savedRotation = previous.OldRotation;
                    _savedHorizontalAngle = previous.OldHorizontalAngle;
                    _savedVerticalAngle = previous.OldVerticalAngle;
                    _isLocked = true;
                    LockCursor(false);

                    if (_moveCoroutine != null)
                        StopCoroutine(_moveCoroutine);

                    _moveCoroutine = StartCoroutine(SmoothMoveRoutine(previous.Target.position, previous.Target.rotation));
                    return;
                }
            }

            _currentTarget = null;

            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _moveCoroutine = StartCoroutine(SmoothMoveRoutine(_savedPosition, _savedRotation, OnReturnToFreeLookComplete));
        }

        private void OnReturnToFreeLookComplete()
        {
            _horizontalAngle = _savedHorizontalAngle;
            _verticalAngle = _savedVerticalAngle;
            _isLocked = false;

            LockCursor(true);
            GameEvents.SetCameraLocked(false);
        }

        public void TurnToTarget(Transform target)
        {
            if (!target || _isLocked || _isEndingLocked || _currentTarget != null) return;

            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            Vector3 dir = (target.position - transform.position).normalized;
            Quaternion targetWorldRot = Quaternion.LookRotation(dir, Vector3.up);

            Quaternion localOffset = Quaternion.Inverse(_startRotation) * targetWorldRot;
            Vector3 euler = localOffset.eulerAngles;
            float yaw = Mathf.Clamp(FixAngle(euler.y), -_maxHorizontalAngle, _maxHorizontalAngle);
            float pitch = Mathf.Clamp(FixAngle(euler.x), -_maxVerticalAngle, _maxVerticalAngle);

            _horizontalAngle = yaw;
            _verticalAngle = pitch;
            _isTurningToTarget = true;

            Quaternion clampedRot = _startRotation * Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, Vector3.right);
            _moveCoroutine = StartCoroutine(SmoothMoveRoutine(transform.position, clampedRot, OnTurnToTargetComplete, _turnSpeed));
        }

        private void OnTurnToTargetComplete()
        {
            _isTurningToTarget = false;
        }

        public void ReturnAndTurnToTarget(Transform lookTarget)
        {
            if (!lookTarget)
            {
                ReturnToFreeLook();
                return;
            }

            _currentTarget = null;

            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            Vector3 dir = (lookTarget.position - _savedPosition).normalized;
            Quaternion targetWorldRot = Quaternion.LookRotation(dir, Vector3.up);

            Quaternion localOffset = Quaternion.Inverse(_startRotation) * targetWorldRot;
            Vector3 euler = localOffset.eulerAngles;
            float yaw = FixAngle(euler.y);
            float pitch = FixAngle(euler.x);

            yaw = Mathf.Clamp(yaw, -_maxHorizontalAngle, _maxHorizontalAngle);
            pitch = Mathf.Clamp(pitch, -_maxVerticalAngle, _maxVerticalAngle);

            Quaternion clampedRot = _startRotation * Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, Vector3.right);
            _pendingYaw = yaw;
            _pendingPitch = pitch;

            _moveCoroutine = StartCoroutine(SmoothMoveRoutine(_savedPosition, clampedRot, OnReturnAndTurnComplete));
        }

        private void OnReturnAndTurnComplete()
        {
            _horizontalAngle = _pendingYaw;
            _verticalAngle = _pendingPitch;
            _isLocked = false;

            LockCursor(true);
            GameEvents.SetCameraLocked(false);
        }

        private float FixAngle(float angle)
        {
            if (angle > 180f) angle -= 360f;
            return angle;
        }

        private IEnumerator SmoothMoveRoutine(Vector3 targetPos, Quaternion targetRot, System.Action onComplete = null, float speedOverride = -1f)
        {
            float speed = speedOverride > 0f ? speedOverride : _moveSpeed;
            float elapsed = 0f;
            const float maxDuration = 2.0f;

            while (elapsed < maxDuration &&
                   (Vector3.Distance(transform.position, targetPos) > 0.005f ||
                    Quaternion.Angle(transform.rotation, targetRot) > 1.0f))
            {
                float dt = Time.unscaledDeltaTime;
                elapsed += dt;
                transform.position = Vector3.Lerp(transform.position, targetPos, dt * speed);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, dt * speed);
                yield return null;
            }

            transform.position = targetPos;
            transform.rotation = targetRot;
            _moveCoroutine = null;
            if (onComplete != null) onComplete.Invoke();
        }

        private void LockCursor(bool locked)
        {
            if (locked && Cursor.lockState != CursorLockMode.Locked)
                _skipMouseFrames = 2;

            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
