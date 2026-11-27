using TheLastCup.Core;
using TheLastCup.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLastCup.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class HerbJar : MonoBehaviour, IClickable, IHintText
    {
        [SerializeField] private string _herbId;
        [SerializeField] private OutlineEffect _outline;
        [SerializeField] private TMP_Text _herbNameText;
        [SerializeField] private GameObject _herbPinchPrefab;
        [SerializeField] private float _pinchSize = 1f;
        [SerializeField] private Material _leafMaterial;
        [SerializeField] private LayerMask _blockingLayers = ~0;

        [Header("Audio")]
        [SerializeField] private AudioSource _jarOpenSource;
        [SerializeField] private AudioSource _herbPickSource;

        private HerbInfo _herbData;
        private GameObject _draggedPinch;
        private Camera _camera;
        private Vector3 _dragStartPoint;

        private bool _isStationActive;
        private bool _isLocked;
        private bool _isDragging;
        private Transform _teapotTransform;
        private Material _cachedLeafMaterial;

        public string HerbId { get { return _herbId; } }
        public HerbInfo HerbInfo { get { return _herbData; } }
        public bool CanInteract { get { return _isStationActive && !_isLocked; } }

        public string GetFirstGuestHint()
        {
            if (!CanInteract) return null;

            string herbName = _herbData != null && !string.IsNullOrEmpty(_herbData.name) ? _herbData.name : _herbId;
            string shelfName = "Трава";
            if (_herbData != null)
            {
                switch (_herbData.SlotType)
                {
                    case TeaSlotType.Base: shelfName = "Основа"; break;
                    case TeaSlotType.Heart: shelfName = "Сердцевина"; break;
                    case TeaSlotType.Aftertaste: shelfName = "Послевкусие"; break;
                }
            }
            return $"Перетащи «{herbName}» в чайник ({shelfName})";
        }

        private void OnEnable()
        {
            GameEvents.OnCameraLockChanged += OnStationFocusChanged;
            GameEvents.OnTeaSlotChanged += OnTeaSlotChanged;
            GameEvents.OnTeapotBroken += OnTeapotReset;
            GameEvents.OnResetTeaCycle += OnTeapotReset;
            GameEvents.OnTeaDumped += OnTeapotReset;
            GameEvents.OnTeapotSpawned += OnTeapotSpawned;
        }

        private void OnDisable()
        {
            GameEvents.OnCameraLockChanged -= OnStationFocusChanged;
            GameEvents.OnTeaSlotChanged -= OnTeaSlotChanged;
            GameEvents.OnTeapotBroken -= OnTeapotReset;
            GameEvents.OnResetTeaCycle -= OnTeapotReset;
            GameEvents.OnTeaDumped -= OnTeapotReset;
            GameEvents.OnTeapotSpawned -= OnTeapotSpawned;
        }

        private void OnTeapotSpawned(Transform teapot)
        {
            _teapotTransform = teapot;
        }

        private void OnTeapotReset()
        {
            _isLocked = false;
        }

        private void Awake()
        {
            if (!_outline)
                _outline = GetComponent<OutlineEffect>();

            _camera = Camera.main;
        }

        public void Setup(HerbInfo data)
        {
            _herbData = data;
            if (_herbData != null)
            {
                _herbId = _herbData.id;
                name = $"Jar_{_herbData.id} ({_herbData.name})";
                if (_herbNameText)
                    _herbNameText.text = _herbData.name;
            }
            else if (_herbNameText)
            {
                _herbNameText.text = string.Empty;
            }
        }

        private void OnStationFocusChanged(bool isFocusedOnStation)
        {
            _isStationActive = isFocusedOnStation;

            if (!_isStationActive && _outline)
                _outline.SetLit(false);
        }

        private void OnTeaSlotChanged(int slotIndex, HerbInfo herb)
        {
            if (_herbData == null) return;

            if ((int)_herbData.SlotType == slotIndex)
            {
                _isLocked = herb != null;
                if (_isLocked && _outline)
                    _outline.SetLit(false);
            }
        }

        public void OnHoverStart()
        {
            if (!CanInteract) return;

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
            if (!_isStationActive)
            {
                GameEvents.RequestEnterTeaStation();
                return;
            }

            if (!CanInteract) return;
            StartDrag();
        }

        private void Update()
        {
            if (!_isDragging) return;

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                UpdateDrag();
            }
            else
            {
                StopDrag();
            }
        }

        private void StartDrag()
        {
            _isDragging = true;
            AudioHelper.Play(_jarOpenSource);
            AudioHelper.Play(_herbPickSource);

            _dragStartPoint = _teapotTransform ? _teapotTransform.position : transform.position;

            if (_herbPinchPrefab)
            {
                _draggedPinch = Instantiate(_herbPinchPrefab);
                _draggedPinch.transform.localScale *= _pinchSize;
            }
            else
            {
                _draggedPinch = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _draggedPinch.transform.localScale = Vector3.one * 0.04f;

                MeshRenderer mr = _draggedPinch.GetComponent<MeshRenderer>();
                if (_herbData != null && mr != null)
                    SetMaterialColor(mr, _herbData.GetTeaColor());
            }

            SetCollidersEnabled(_draggedPinch, false);

            UpdateDrag();
        }

        private void UpdateDrag()
        {
            if (!_draggedPinch) return;

            if (!_camera)
                _camera = Camera.main;

            if (!_camera || Mouse.current == null) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(mousePos);

            Plane plane = new Plane(-_camera.transform.forward, _dragStartPoint);
            float planeDist = 5f;
            if (plane.Raycast(ray, out float enter))
                planeDist = enter;

            if (Physics.Raycast(ray, out RaycastHit hit, planeDist, _blockingLayers, QueryTriggerInteraction.Ignore))
            {
                _draggedPinch.transform.position = hit.point - ray.direction * 0.03f;
            }
            else
            {
                _draggedPinch.transform.position = ray.GetPoint(planeDist);
            }
        }

        private void StopDrag()
        {
            _isDragging = false;
            AudioHelper.Play(_herbPickSource);
            DropDraggedPinch();
        }

        private void DropDraggedPinch()
        {
            if (!_draggedPinch) return;

            GameObject leaf = _draggedPinch;
            _draggedPinch = null;
            leaf.name = "Falling_Leaf";

            if (leaf.GetComponentInChildren<Collider>(true) == null)
                leaf.AddComponent<SphereCollider>();
            SetCollidersEnabled(leaf, true);

            var leafComp = leaf.GetComponent<FallingLeaf>();
            if (leafComp == null)
                leafComp = leaf.AddComponent<FallingLeaf>();
            leafComp.Setup(_herbData);

            var rb = leaf.GetComponent<Rigidbody>();
            if (rb == null)
                rb = leaf.AddComponent<Rigidbody>();
            rb.mass = 0.02f;
            rb.linearDamping = 0.8f;
            rb.angularDamping = 2.0f;
            rb.linearVelocity = new Vector3(0f, -0.2f, 0f);
        }

        private static void SetCollidersEnabled(GameObject target, bool enabled)
        {
            if (!target) return;

            Collider[] cols = target.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
                cols[i].enabled = enabled;
        }

        private void SetMaterialColor(MeshRenderer mr, Color color)
        {
            if (_cachedLeafMaterial == null)
            {
                if (_leafMaterial != null)
                {
                    _cachedLeafMaterial = new Material(_leafMaterial);
                }
                else
                {
                    Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (urpShader != null)
                        _cachedLeafMaterial = new Material(urpShader);
                }
            }

            if (_cachedLeafMaterial != null)
            {
                _cachedLeafMaterial.SetColor("_BaseColor", color);
                _cachedLeafMaterial.color = color;
                mr.sharedMaterial = _cachedLeafMaterial;
            }
        }
    }
}
