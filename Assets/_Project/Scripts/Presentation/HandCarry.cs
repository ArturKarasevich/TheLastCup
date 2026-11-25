using TheLastCup.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLastCup.Presentation
{
    public class HandCarry : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _holdDistance = 1.2f;
        [SerializeField] private float _followSpeed = 14f;
        [SerializeField] private LayerMask _wallLayers = ~0;

        private Transform _heldItem;
        private Quaternion _heldRotation;

        private void Awake()
        {
            if (!_camera && !TryGetComponent(out _camera))
                _camera = Camera.main;
        }

        private void OnEnable()
        {
            GameEvents.OnTeapotGrabbed += OnItemPicked;
        }

        private void OnDisable()
        {
            GameEvents.OnTeapotGrabbed -= OnItemPicked;
        }

        private void OnItemPicked(Transform item)
        {
            if (!item) return;

            _heldItem = item;
            _heldRotation = item.rotation;
            _heldItem.SetParent(null, true);
        }

        private void Update()
        {
            if (!_heldItem || !_camera) return;

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Ray ray = GetAimRay();
                Vector3 targetPos = ray.origin + ray.direction * _holdDistance;

                int rayMask = _wallLayers & ~(1 << 2);
                if (Physics.SphereCast(ray, 0.11f, out RaycastHit hit, _holdDistance, rayMask, QueryTriggerInteraction.Ignore))
                    targetPos = hit.point + hit.normal * 0.13f;

                if (Physics.SphereCast(targetPos + Vector3.up * 0.35f, 0.09f, Vector3.down, out RaycastHit floorHit, 0.55f, rayMask, QueryTriggerInteraction.Ignore))
                    targetPos.y = Mathf.Max(targetPos.y, floorHit.point.y + 0.11f);

                _heldItem.position = Vector3.Lerp(_heldItem.position, targetPos, Time.deltaTime * _followSpeed);
                _heldItem.rotation = Quaternion.Slerp(_heldItem.rotation, _heldRotation, Time.deltaTime * _followSpeed);
            }
            else
            {
                _heldItem = null;
                GameEvents.DropTeapot();
            }
        }

        private Ray GetAimRay()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                return _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            return _camera.ScreenPointToRay(mousePos);
        }
    }
}
