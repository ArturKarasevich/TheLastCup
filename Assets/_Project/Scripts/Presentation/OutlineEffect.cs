using System.Collections.Generic;
using UnityEngine;
using TheLastCup.Core;

namespace TheLastCup.Presentation
{
    public class OutlineEffect : MonoBehaviour
    {
        [Header("Materials")]
        [SerializeField] private Material _outlineMat;
        [SerializeField] private Material _maskMat;

        [Header("Extra Objects")]
        [SerializeField] private GameObject[] _extraRenderers;

        [Header("Custom Outline Settings")]
        [SerializeField] private float _thickness = 0.01f;
        [SerializeField] private Color _outlineColor = Color.white;

        [Header("Pulse Settings")]
        [SerializeField] private float _pulseSpeed = 3.5f;
        [SerializeField] private float _minAlpha = 0.15f;

        private readonly Dictionary<GameObject, List<GameObject>> _masks = new Dictionary<GameObject, List<GameObject>>();
        private readonly Dictionary<GameObject, List<GameObject>> _outlines = new Dictionary<GameObject, List<GameObject>>();
        private MaterialPropertyBlock _matProps;
        private bool _isLit;
        private bool _isPulsing;
        private static bool _canPulse = true;

        private static readonly int ThicknessProp = Shader.PropertyToID("_Thickness");
        private static readonly int ColorProp = Shader.PropertyToID("_OutlineColor");

        public bool IsLit { get { return _isLit; } }
        public bool IsPulsing { get { return _isPulsing; } }
        public GameObject[] ExtraTargets { get { return _extraRenderers; } }

        private void Awake()
        {
            if (_outlineColor.a <= 0.01f)
                _outlineColor = Color.white;

            _matProps = new MaterialPropertyBlock();
            _matProps.SetFloat(ThicknessProp, _thickness);
            _matProps.SetColor(ColorProp, _outlineColor);

            AddRenderer(gameObject);

            if (_extraRenderers != null)
            {
                foreach (var target in _extraRenderers)
                {
                    if (target) AddRenderer(target);
                }
            }
        }

        private void OnEnable()
        {
            GameEvents.OnHintPulseChanged += OnHintPulseChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnHintPulseChanged -= OnHintPulseChanged;
        }

        private void OnHintPulseChanged(bool allowed)
        {
            _canPulse = allowed;
            if (!allowed)
                SetPulse(false);
        }

        private void Update()
        {
            if (!_isPulsing || _isLit)
                return;

            float t = (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f;
            Color c = _outlineColor;
            c.a = Mathf.Lerp(_minAlpha, _outlineColor.a, t);
            SetColor(c);
        }

        public void AddRenderer(GameObject targetRoot)
        {
            if (!targetRoot || _masks.ContainsKey(targetRoot))
                return;

            var masks = new List<GameObject>();
            var outlines = new List<GameObject>();
            bool visible = _isLit || _isPulsing;

            var meshFilters = targetRoot.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (!mf.sharedMesh || mf.gameObject.name == "Stencil_Mask" || mf.gameObject.name == "Stencil_Outline" || mf.GetComponent<TMPro.TMP_Text>() != null || mf.gameObject.name.Contains("TMP") || mf.gameObject.name.Contains("Page_Sheet"))
                    continue;

                int submeshCount = mf.sharedMesh.subMeshCount;

                if (_maskMat)
                {
                    var maskObj = new GameObject("Stencil_Mask");
                    maskObj.transform.SetParent(mf.transform, false);
                    maskObj.transform.localPosition = Vector3.zero;
                    maskObj.transform.localRotation = Quaternion.identity;
                    maskObj.transform.localScale = Vector3.one;

                    var filter = maskObj.AddComponent<MeshFilter>();
                    filter.sharedMesh = mf.sharedMesh;

                    var mr = maskObj.AddComponent<MeshRenderer>();
                    var maskMats = new Material[submeshCount];
                    for (int i = 0; i < submeshCount; i++)
                        maskMats[i] = _maskMat;

                    mr.sharedMaterials = maskMats;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;

                    maskObj.SetActive(visible);
                    masks.Add(maskObj);
                }

                if (_outlineMat)
                {
                    var outlineObj = new GameObject("Stencil_Outline");
                    outlineObj.transform.SetParent(mf.transform, false);
                    outlineObj.transform.localPosition = Vector3.zero;
                    outlineObj.transform.localRotation = Quaternion.identity;
                    outlineObj.transform.localScale = Vector3.one;

                    var filter = outlineObj.AddComponent<MeshFilter>();
                    filter.sharedMesh = mf.sharedMesh;

                    var mr = outlineObj.AddComponent<MeshRenderer>();
                    var outlineMats = new Material[submeshCount];
                    for (int i = 0; i < submeshCount; i++)
                        outlineMats[i] = _outlineMat;

                    mr.sharedMaterials = outlineMats;
                    mr.SetPropertyBlock(_matProps);
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;

                    outlineObj.SetActive(visible);
                    outlines.Add(outlineObj);
                }
            }

            _masks[targetRoot] = masks;
            _outlines[targetRoot] = outlines;
        }

        public void RemoveRenderer(GameObject targetRoot)
        {
            if (!targetRoot) return;

            List<GameObject> masks;
            if (_masks.TryGetValue(targetRoot, out masks))
            {
                foreach (var m in masks)
                    if (m) Destroy(m);
                _masks.Remove(targetRoot);
            }

            List<GameObject> outlines;
            if (_outlines.TryGetValue(targetRoot, out outlines))
            {
                foreach (var o in outlines)
                    if (o) Destroy(o);
                _outlines.Remove(targetRoot);
            }
        }

        public void SetLit(bool enable)
        {
            if (_isLit == enable) return;
            _isLit = enable;

            if (_isLit)
                SetColor(_outlineColor);

            UpdateVisibility();
        }

        public void SetPulse(bool enable)
        {
            if (enable && !_canPulse)
                enable = false;

            if (_isPulsing == enable) return;
            _isPulsing = enable;

            if (!_isPulsing)
                SetColor(_outlineColor);

            UpdateVisibility();
        }

        public void SetThickness(float thickness)
        {
            _thickness = thickness;
            if (_matProps == null)
                _matProps = new MaterialPropertyBlock();

            _matProps.SetFloat(ThicknessProp, _thickness);
            UpdateMatProps();
        }

        private void UpdateVisibility()
        {
            bool visible = _isLit || _isPulsing;

            foreach (var list in _masks.Values)
            {
                foreach (var m in list)
                    if (m) m.SetActive(visible);
            }

            foreach (var list in _outlines.Values)
            {
                foreach (var o in list)
                    if (o) o.SetActive(visible);
            }
        }

        private void SetColor(Color color)
        {
            if (_matProps == null)
                _matProps = new MaterialPropertyBlock();

            _matProps.SetColor(ColorProp, color);
            UpdateMatProps();
        }

        private void UpdateMatProps()
        {
            foreach (var list in _outlines.Values)
            {
                foreach (var o in list)
                {
                    MeshRenderer mr;
                    if (o && o.TryGetComponent<MeshRenderer>(out mr))
                        mr.SetPropertyBlock(_matProps);
                }
            }
        }
    }
}
