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
        [SerializeField] private Material _leafMaterial;
        [SerializeField] private int _leafBurstCount = 7;
        [SerializeField] private LayerMask _blockingLayers = ~0;

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

            _dragStartPoint = _teapotTransform ? _teapotTransform.position : transform.position;

            if (_herbPinchPrefab)
            {
                _draggedPinch = Instantiate(_herbPinchPrefab);
            }
            else
            {
                _draggedPinch = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _draggedPinch.transform.localScale = Vector3.one * 0.04f;
                Collider col = _draggedPinch.GetComponent<Collider>();
                if (col != null)
                    Destroy(col);

                MeshRenderer mr = _draggedPinch.GetComponent<MeshRenderer>();
                if (_herbData != null && mr != null)
                    SetMaterialColor(mr, _herbData.GetTeaColor());
            }

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
            Vector3 releasePos = _draggedPinch ? _draggedPinch.transform.position : transform.position;

            SpawnFallingLeaves(releasePos);

            if (_draggedPinch)
            {
                Destroy(_draggedPinch);
                _draggedPinch = null;
            }
        }

        private void SpawnFallingLeaves(Vector3 spawnPos)
        {
            Color leafColor = _herbData != null ? _herbData.GetTeaColor() : new Color(0.3f, 0.5f, 0.2f);

            for (int i = 0; i < _leafBurstCount; i++)
            {
                var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaf.name = "Falling_Leaf";

                float size = Random.Range(0.012f, 0.02f);
                leaf.transform.localScale = new Vector3(size, size * 0.5f, size);
                leaf.transform.position = spawnPos + Random.insideUnitSphere * 0.02f;
                leaf.transform.rotation = Random.rotation;

                MeshRenderer mr = leaf.GetComponent<MeshRenderer>();
                if (mr != null)
                    SetMaterialColor(mr, leafColor);

                var leafComp = leaf.AddComponent<FallingLeaf>();
                leafComp.Setup(_herbData);

                var rb = leaf.AddComponent<Rigidbody>();
                rb.mass = 0.02f;
                rb.linearDamping = 0.8f;
                rb.angularDamping = 2.0f;
                rb.linearVelocity = new Vector3(Random.Range(-0.04f, 0.04f), -0.2f, Random.Range(-0.04f, 0.04f));

                Destroy(leaf, 2.0f);
            }
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
                    if (urpShader == null) urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
                    if (urpShader == null) urpShader = Shader.Find("Sprites/Default");

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
