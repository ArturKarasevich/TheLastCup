using System.Collections;
using System.Collections.Generic;
using System.Text;
using TheLastCup.Core;
using TheLastCup.Data;
using TheLastCup.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TheLastCup.UI
{
    public class HerbBook : MonoBehaviour, IClickable, IHintText
    {
        private class BookPage
        {
            public int Index;
            public Transform Pivot;
            public TMP_Text FrontText;
            public TMP_Text BackText;
            public TMP_Text FrontPageNumber;
            public TMP_Text BackPageNumber;
        }

        [SerializeField] private Transform _cameraPoint;
        [SerializeField] private Transform _firstPage;
        [SerializeField] private Transform _pageTemplate;
        [SerializeField] private TalkChoiceButton _magnifierButton;

        [SerializeField] private float PageThickness = 0.002f;
        [SerializeField] private float FlipTime = 0.35f;

        [SerializeField] private Vector3 LeftPagePos = new Vector3(0.025f, 0f, 0.0019f);
        [SerializeField] private Vector3 RightPagePos = new Vector3(-0.025f, 0f, 0.008f);
        [SerializeField] private Vector3 PageRotation = new Vector3(0f, 180f, 0f);

        private readonly List<HerbInfo> _herbs = new List<HerbInfo>();
        private readonly HashSet<string> _knownSecrets = new HashSet<string>();
        private readonly List<BookPage> _pages = new List<BookPage>();

        private OutlineEffect _outline;
        private Transform _pagesContainer;
        private int _currentHerbIndex;
        private bool _isOpen;
        private bool _wasOpened;
        private bool _hasMagnifier;
        private bool _usedMagnifier;
        private int _openedOnFrame = -1;
        private Coroutine _flipCoroutine;
        private int _nextPageIndex = -1;

        public bool IsOpen { get { return _isOpen; } }
        public int CurrentHerbIndex { get { return _currentHerbIndex; } }
        public int TotalPages { get { return _herbs.Count; } }

        public string GetFirstGuestHint()
        {
            return "Загляни в травник на столе — там описаны свойства всех трав";
        }

        private void Awake()
        {
            _outline = GetComponent<OutlineEffect>();
        }

        private void Start()
        {
            LoadHerbs();
            SetupPages();
            UpdateAllPageTexts();
            UpdatePagesVisibility();
            _isOpen = false;
            UpdateButtons(GetCurrentHerb());
        }

        private void OnEnable()
        {
            GameEvents.OnHerbSecretUnlocked += OnSecretUnlocked;
            GameEvents.OnItemUnlocked += OnItemUnlocked;
            GameEvents.OnCameraLockChanged += OnCameraLockChanged;
            GameEvents.OnFirstTeaStationEntered += OnFirstTeaStationEntered;
        }

        private void OnDisable()
        {
            StopPageFlip(true);

            GameEvents.OnHerbSecretUnlocked -= OnSecretUnlocked;
            GameEvents.OnItemUnlocked -= OnItemUnlocked;
            GameEvents.OnCameraLockChanged -= OnCameraLockChanged;
            GameEvents.OnFirstTeaStationEntered -= OnFirstTeaStationEntered;
        }

        private void Update()
        {
            if (!_isOpen || Time.frameCount == _openedOnFrame)
                return;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    CloseBook();
                    return;
                }

                if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
                    PreviousPage();
                else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
                    NextPage();
            }

            if (Mouse.current != null)
            {
                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    CloseBook();
                    return;
                }

                if (Mouse.current.leftButton.wasPressedThisFrame && ShouldCloseOnOutsideClick())
                {
                    CloseBook();
                    return;
                }

                float scrollY = Mouse.current.scroll.ReadValue().y;
                if (scrollY > 0.01f)
                    PreviousPage();
                else if (scrollY < -0.01f)
                    NextPage();
            }
        }

        private bool ShouldCloseOnOutsideClick()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return false;

            Camera cam = Camera.main;
            if (!cam || Mouse.current == null)
                return false;

            Ray ray = Cursor.lockState == CursorLockMode.Locked
                ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : cam.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit, 15f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    return false;

                if (_magnifierButton && (hit.transform == _magnifierButton.transform || hit.transform.IsChildOf(_magnifierButton.transform)))
                    return false;
            }

            return true;
        }

        public void OnHoverStart()
        {
            if (_isOpen) return;
            if (_outline) _outline.SetLit(true);
        }

        public void OnHoverEnd()
        {
            if (_outline) _outline.SetLit(false);
        }

        public void OnClick()
        {
            if (!_isOpen)
            {
                OpenBook();
                return;
            }

            if (Time.frameCount == _openedOnFrame)
                return;

            OnPageClicked();
        }

        public void OpenBook()
        {
            if (_isOpen) return;

            _wasOpened = true;
            _isOpen = true;
            _openedOnFrame = Time.frameCount;

            if (_outline)
            {
                _outline.SetPulse(false);
                _outline.SetLit(false);
            }

            GameEvents.SendBookFocusChanged(true);

            if (_cameraPoint)
                GameEvents.RequestCameraFocus(_cameraPoint);
            else
                GameEvents.SetUIMode(true);

            UpdatePagesVisibility();
            UpdateButtons(GetCurrentHerb());
        }

        public void CloseBook()
        {
            if (!_isOpen) return;

            StopPageFlip(true);
            _isOpen = false;
            UpdateButtons(GetCurrentHerb());

            if (_outline)
                _outline.SetLit(false);

            GameEvents.SendBookFocusChanged(false);

            if (_cameraPoint)
                GameEvents.RequestCameraFreeLook();
            else
                GameEvents.SetUIMode(false);
        }

        private void OnFirstTeaStationEntered()
        {
            if (!_wasOpened && !_isOpen && _outline)
                _outline.SetPulse(true);
        }

        public void NextPage()
        {
            int current = _nextPageIndex >= 0 ? _nextPageIndex : _currentHerbIndex;
            if (_herbs.Count == 0 || current >= _herbs.Count - 1)
                return;

            FlipToPage(current + 1, true);
        }

        public void PreviousPage()
        {
            int current = _nextPageIndex >= 0 ? _nextPageIndex : _currentHerbIndex;
            if (_herbs.Count == 0 || current <= 0)
                return;

            FlipToPage(current - 1, false);
        }

        public void UseMagnifierOnHerb()
        {
            HerbInfo current = GetCurrentHerb();
            if (current == null || !CanUseMagnifierOnHerb(current))
                return;

            bool unlockedBefore = _knownSecrets.Contains(current.id);
            GameEvents.RequestMagnifier(current.id);

            if (_knownSecrets.Contains(current.id) && !unlockedBefore)
                _usedMagnifier = true;

            UpdateAllPageTexts();
            UpdateButtons(current);
        }

        private void SetupPages()
        {
            _pages.Clear();
            if (!_firstPage || !_pageTemplate) return;

            _pagesContainer = _pageTemplate.parent;
            if (_pagesContainer)
            {
                foreach (Transform child in _pagesContainer)
                {
                    if (child != _firstPage && child != _pageTemplate)
                        child.gameObject.SetActive(false);
                }
            }

            int neededLeaves = _herbs.Count > 0 ? _herbs.Count + 1 : 0;
            for (int i = 0; i < neededLeaves; i++)
            {
                Transform pivot;
                if (i == 0)
                    pivot = _firstPage;
                else if (i == 1)
                    pivot = _pageTemplate;
                else
                    pivot = Instantiate(_pageTemplate.gameObject, _pagesContainer, false).transform;

                if (i >= 2)
                    pivot.name = $"Leaf_{i:00}";

                pivot.gameObject.SetActive(true);

                Transform frontTr = pivot.Find("FrontPage_TMP");
                Transform backTr = pivot.Find("BackPage_TMP");
                Transform frontNumTr = pivot.Find("FrontPageNumber_TMP");
                Transform backNumTr = pivot.Find("BackPageNumber_TMP");

                if (i == 0)
                {
                    if (frontTr) frontTr.gameObject.SetActive(false);
                    if (frontNumTr) frontNumTr.gameObject.SetActive(false);
                }

                if (i == neededLeaves - 1)
                {
                    if (backTr) backTr.gameObject.SetActive(false);
                    if (backNumTr) backNumTr.gameObject.SetActive(false);
                }

                _pages.Add(new BookPage
                {
                    Index = i,
                    Pivot = pivot,
                    FrontText = i > 0 && frontTr ? frontTr.GetComponent<TMP_Text>() : null,
                    BackText = i < _herbs.Count && backTr ? backTr.GetComponent<TMP_Text>() : null,
                    FrontPageNumber = i > 0 && frontNumTr ? frontNumTr.GetComponent<TMP_Text>() : null,
                    BackPageNumber = i < _herbs.Count && backNumTr ? backNumTr.GetComponent<TMP_Text>() : null
                });
            }
        }

        private void GetPageSizes(
            out Quaternion baseRot,
            out Vector3 spineCenter,
            out Vector3 frontNormal,
            out float dirSign)
        {
            baseRot = Quaternion.Euler(PageRotation);
            frontNormal = (baseRot * Vector3.back).normalized;
            Vector3 readingRight = (baseRot * Vector3.right).normalized;
            spineCenter = (LeftPagePos + RightPagePos) * 0.5f;

            float rightOffset = Vector3.Dot(RightPagePos - spineCenter, readingRight);
            dirSign = rightOffset >= 0f ? 1f : -1f;
        }

        private void UpdateAllPageTexts()
        {
            for (int k = 0; k < _pages.Count; k++)
            {
                var leaf = _pages[k];

                if (leaf.FrontText && k > 0 && k - 1 < _herbs.Count)
                    leaf.FrontText.text = GetRightPageText(_herbs[k - 1], k - 1);

                if (leaf.FrontPageNumber && k > 0 && k - 1 < _herbs.Count)
                    leaf.FrontPageNumber.text = $"— {(k - 1) * 2 + 2} —";

                if (leaf.BackText && k < _herbs.Count)
                    leaf.BackText.text = GetLeftPageText(_herbs[k], k);

                if (leaf.BackPageNumber && k < _herbs.Count)
                    leaf.BackPageNumber.text = $"— {k * 2 + 1} —";
            }
        }

        private void UpdatePagesVisibility()
        {
            if (_pages.Count == 0)
                return;

            GetPageSizes(out Quaternion baseRot, out Vector3 spineCenter, out Vector3 frontNormal, out float dirSign);
            float stackStep = PageThickness * 0.35f + 0.0003f;
            float baseHeight = PageThickness * 0.5f;

            for (int k = 0; k < _pages.Count; k++)
            {
                var leaf = _pages[k];
                if (!leaf.Pivot) continue;

                bool onLeftStack = k <= _currentHerbIndex;
                int heightLevel = onLeftStack ? k : (_pages.Count - 1 - k);

                leaf.Pivot.localPosition = spineCenter + frontNormal * (baseHeight + heightLevel * stackStep);
                leaf.Pivot.localRotation = onLeftStack
                    ? baseRot * Quaternion.AngleAxis(180f * dirSign, Vector3.up)
                    : baseRot;

                bool showFront = k == _currentHerbIndex + 1;
                if (leaf.FrontText)
                    leaf.FrontText.gameObject.SetActive(showFront);
                if (leaf.FrontPageNumber)
                    leaf.FrontPageNumber.gameObject.SetActive(showFront);

                bool showBack = k == _currentHerbIndex;
                if (leaf.BackText)
                    leaf.BackText.gameObject.SetActive(showBack);
                if (leaf.BackPageNumber)
                    leaf.BackPageNumber.gameObject.SetActive(showBack);
            }
        }

        private void FlipToPage(int targetIndex, bool forward)
        {
            StopPageFlip(true);

            if (!Application.isPlaying || !gameObject.activeInHierarchy || _pages.Count == 0 || FlipTime <= 0.01f)
            {
                _currentHerbIndex = targetIndex;
                UpdatePagesVisibility();
                UpdateButtons(GetCurrentHerb());
                return;
            }

            _nextPageIndex = targetIndex;
            _flipCoroutine = StartCoroutine(FlipPageRoutine(_currentHerbIndex, targetIndex, forward));
        }

        private void StopPageFlip(bool commitPendingIndex)
        {
            if (_flipCoroutine != null)
            {
                StopCoroutine(_flipCoroutine);
                _flipCoroutine = null;
            }

            if (commitPendingIndex && _nextPageIndex >= 0)
                _currentHerbIndex = _nextPageIndex;

            _nextPageIndex = -1;
            UpdatePagesVisibility();
            UpdateButtons(GetCurrentHerb());
        }

        private IEnumerator FlipPageRoutine(int fromIndex, int targetIndex, bool forward)
        {
            int turningLeafIndex = forward ? fromIndex + 1 : fromIndex;
            if (turningLeafIndex < 0 || turningLeafIndex >= _pages.Count)
            {
                _currentHerbIndex = targetIndex;
                _nextPageIndex = -1;
                UpdatePagesVisibility();
                UpdateButtons(GetCurrentHerb());
                _flipCoroutine = null;
                yield break;
            }

            BookPage turningLeaf = _pages[turningLeafIndex];

            GetPageSizes(out Quaternion baseRot, out Vector3 spineCenter, out Vector3 frontNormal, out float dirSign);
            float thickness = PageThickness;
            float stackStep = thickness * 0.35f + 0.0003f;
            float baseHeight = thickness * 0.5f;
            float maxStackHeight = baseHeight + _pages.Count * stackStep;

            for (int k = 0; k < _pages.Count; k++)
            {
                var leaf = _pages[k];
                if (k == turningLeafIndex) continue;

                bool showFrontUnderlying = k == (forward ? targetIndex + 1 : fromIndex + 1);
                if (leaf.FrontText)
                    leaf.FrontText.gameObject.SetActive(showFrontUnderlying);
                if (leaf.FrontPageNumber)
                    leaf.FrontPageNumber.gameObject.SetActive(showFrontUnderlying);

                bool showBackUnderlying = k == (forward ? fromIndex : targetIndex);
                if (leaf.BackText)
                    leaf.BackText.gameObject.SetActive(showBackUnderlying);
                if (leaf.BackPageNumber)
                    leaf.BackPageNumber.gameObject.SetActive(showBackUnderlying);
            }

            float startAngle = forward ? 0f : 180f * dirSign;
            float endAngle = forward ? 180f * dirSign : 0f;

            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, FlipTime);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                float angle = Mathf.Lerp(startAngle, endAngle, smoothT);
                float lift = Mathf.Sin(t * Mathf.PI) * (thickness * 2f);

                turningLeaf.Pivot.localPosition = spineCenter + frontNormal * (maxStackHeight + thickness + lift);
                turningLeaf.Pivot.localRotation = baseRot * Quaternion.AngleAxis(angle, Vector3.up);

                bool pastMidpoint = t >= 0.5f;
                bool showFront = forward ? !pastMidpoint : pastMidpoint;
                if (turningLeaf.FrontText) turningLeaf.FrontText.gameObject.SetActive(showFront);
                if (turningLeaf.FrontPageNumber) turningLeaf.FrontPageNumber.gameObject.SetActive(showFront);
                if (turningLeaf.BackText) turningLeaf.BackText.gameObject.SetActive(!showFront);
                if (turningLeaf.BackPageNumber) turningLeaf.BackPageNumber.gameObject.SetActive(!showFront);

                yield return null;
            }

            _currentHerbIndex = targetIndex;
            _nextPageIndex = -1;
            _flipCoroutine = null;

            UpdatePagesVisibility();
            UpdateButtons(GetCurrentHerb());
        }

        private void OnCameraLockChanged(bool isLocked)
        {
            if (!isLocked && _isOpen)
                CloseBook();
        }

        private void OnSecretUnlocked(string herbId)
        {
            if (string.IsNullOrEmpty(herbId)) return;

            if (_knownSecrets.Add(herbId))
            {
                UpdateAllPageTexts();
                UpdateButtons(GetCurrentHerb());
            }
        }

        private void OnItemUnlocked(string artifactKey)
        {
            if (Domain.ItemIds.FromString(artifactKey) == Domain.ItemType.Magnifier)
            {
                _hasMagnifier = true;
                UpdateAllPageTexts();
                UpdateButtons(GetCurrentHerb());
            }
        }

        private void OnPageClicked()
        {
            Camera cam = Camera.main;
            if (!cam || Mouse.current == null)
            {
                NextPage();
                return;
            }

            Ray ray = Cursor.lockState == CursorLockMode.Locked
                ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : cam.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore) &&
                (hit.transform == transform || hit.transform.IsChildOf(transform)))
            {
                Transform space = _pagesContainer ? _pagesContainer : transform;
                Vector3 localHit = space.InverseTransformPoint(hit.point);
                float distToLeft = Vector3.SqrMagnitude(localHit - LeftPagePos);
                float distToRight = Vector3.SqrMagnitude(localHit - RightPagePos);

                if (distToLeft < distToRight)
                    PreviousPage();
                else
                    NextPage();
            }
        }

        private void LoadHerbs()
        {
            _herbs.Clear();

            var registry = GameLauncher.CurrentGameData;
            if (registry != null && registry.HerbsList != null && registry.HerbsList.Count > 0)
            {
                for (int i = 0; i < registry.HerbsList.Count; i++)
                {
                    if (registry.HerbsList[i] != null)
                        _herbs.Add(registry.HerbsList[i]);
                }
            }

            if (_currentHerbIndex >= _herbs.Count)
                _currentHerbIndex = Mathf.Max(0, _herbs.Count - 1);
        }

        private HerbInfo GetCurrentHerb()
        {
            if (_herbs.Count == 0 || _currentHerbIndex < 0 || _currentHerbIndex >= _herbs.Count)
                return null;

            return _herbs[_currentHerbIndex];
        }

        private bool CanUseMagnifierOnHerb(HerbInfo herb)
        {
            return _hasMagnifier && !_usedMagnifier && herb != null && !_knownSecrets.Contains(herb.id);
        }

        private void UpdateButtons(HerbInfo currentHerb)
        {
            bool canLoupe = CanUseMagnifierOnHerb(currentHerb);

            if (_magnifierButton)
            {
                if (_isOpen && canLoupe) _magnifierButton.Setup("Лупа", UseMagnifierOnHerb);
                else _magnifierButton.Hide();
            }
        }

        private string GetLeftPageText(HerbInfo herb, int index)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<size=118%><b>{index + 1}. {herb.name}</b></size>");
            sb.AppendLine($"<color=#5C3A21><size=88%><i>{GetSlotName(herb)}</i></size></color>");
            sb.AppendLine();

            string essenceText = !string.IsNullOrEmpty(herb.essence) ? herb.essence : herb.smellHint;
            AddLabeledLine(sb, "Суть", essenceText);
            AddLabeledLine(sb, "Вид", herb.lookInfo);
            AddLabeledLine(sb, "Физика", herb.bodyEffect);
            AddLabeledLine(sb, "Эмоции", herb.moodEffect);
            AddLabeledLine(sb, "Вкус", herb.taste);

            return sb.ToString();
        }

        private string GetRightPageText(HerbInfo herb, int index)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<size=108%><b>Дополнительная страница</b></size>");
            sb.AppendLine();

            if (_knownSecrets.Contains(herb.id))
            {
                string author = !string.IsNullOrEmpty(herb.secretAuthor) ? herb.secretAuthor : "Заметка мастера";
                sb.AppendLine($"<color=#6B3E00><b>{author}:</b></color>");

                if (!string.IsNullOrEmpty(herb.secretNote))
                    sb.AppendLine($"<i>«{herb.secretNote}»</i>");

                if (herb.tags != null && herb.tags.Length > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"<color=#4A4A4A><size=84%>Теги: {string.Join(", ", herb.tags)}</size></color>");
                }
            }
            else
            {
                sb.AppendLine("<color=#6D645B><i>Строки на этой странице выцвели от времени и дорожной сырости...</i></color>");
                sb.AppendLine();
                sb.AppendLine("Откроется при верной заварке для гостя, с которым связана эта трава.");

                if (CanUseMagnifierOnHerb(herb))
                {
                    sb.AppendLine();
                    sb.AppendLine("<color=#7B5200><b>[Треснутая лупа]</b> Нажмите на кнопку ниже, чтобы проявить запись.</color>");
                }
            }

            return sb.ToString();
        }

        private static void AddLabeledLine(StringBuilder sb, string label, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            sb.AppendLine($"<b>{label}:</b> {value}");
        }

        private static string GetSlotName(HerbInfo herb)
        {
            if (!string.IsNullOrEmpty(herb.slotName))
                return herb.slotName;

            switch (herb.SlotType)
            {
                case TeaSlotType.Base: return "Полка 1: Основание";
                case TeaSlotType.Heart: return "Полка 2: Сердцевина";
                case TeaSlotType.Aftertaste: return "Полка 3: Послевкусие";
                default: return herb.slotType;
            }
        }
    }
}
