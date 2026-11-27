using TheLastCup.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TheLastCup.Presentation
{
    public class MousePicker : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _maxRayDistance = 10f;
        [SerializeField] private LayerMask _clickableLayer = ~0;

        private IClickable _hoveredObject;
        private OutlineEffect _passiveOutline;
        private bool _isBookOpen;
        private bool _isBallOpen;

        public IClickable CurrentObject { get { return _hoveredObject; } }

        private void OnEnable()
        {
            GameEvents.OnCameraLockChanged += OnCameraLockChanged;
            GameEvents.OnBookFocusChanged += OnBookFocusChanged;
            GameEvents.OnBallFocusChanged += OnBallFocusChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnCameraLockChanged -= OnCameraLockChanged;
            GameEvents.OnBookFocusChanged -= OnBookFocusChanged;
            GameEvents.OnBallFocusChanged -= OnBallFocusChanged;
        }

        private void Awake()
        {
            if (!_camera && !TryGetComponent(out _camera))
                _camera = Camera.main;
        }

        private void OnCameraLockChanged(bool isLocked)
        {
            SetHoveredObject(null);
            SetPassiveOutline(null);
        }

        private void OnBookFocusChanged(bool isOpen)
        {
            _isBookOpen = isOpen;
            SetHoveredObject(null);
            SetPassiveOutline(null);
        }

        private void OnBallFocusChanged(bool isOpen)
        {
            _isBallOpen = isOpen;
            SetHoveredObject(null);
            SetPassiveOutline(null);
        }

        private void Update()
        {
            if (Time.timeScale == 0f || _isBallOpen)
            {
                SetHoveredObject(null);
                SetPassiveOutline(null);
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                SetHoveredObject(null);
                SetPassiveOutline(null);
                return;
            }

            Ray ray = GetMouseRay();

            if (Physics.Raycast(ray, out RaycastHit hit, _maxRayDistance, _clickableLayer, QueryTriggerInteraction.Ignore))
            {
                var interactable = hit.collider.GetComponentInParent<IClickable>();
                if (_isBookOpen && !(interactable is UI.HerbBook) && !(interactable is TalkChoiceButton))
                    interactable = null;

                SetHoveredObject(interactable);

                if (interactable == null && !_isBookOpen)
                {
                    var highlightable = hit.collider.GetComponentInParent<OutlineEffect>();
                    SetPassiveOutline(highlightable);
                }
                else
                {
                    SetPassiveOutline(null);
                }

                if (_hoveredObject != null && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                    _hoveredObject.OnClick();
            }
            else
            {
                SetHoveredObject(null);
                SetPassiveOutline(null);
            }
        }

        private Ray GetMouseRay()
        {
            if (!_camera)
                return new Ray(transform.position, transform.forward);

            if (Cursor.lockState == CursorLockMode.Locked)
                return _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            return _camera.ScreenPointToRay(mousePos);
        }

        private void SetPassiveOutline(OutlineEffect target)
        {
            if (_passiveOutline == target) return;

            if (_passiveOutline) _passiveOutline.SetLit(false);
            _passiveOutline = target;
            if (_passiveOutline) _passiveOutline.SetLit(true);
        }

        private void SetHoveredObject(IClickable target)
        {
            if (_hoveredObject == target) return;

            if (_hoveredObject != null) _hoveredObject.OnHoverEnd();
            _hoveredObject = target;
            if (_hoveredObject != null) _hoveredObject.OnHoverStart();
        }
    }
}
