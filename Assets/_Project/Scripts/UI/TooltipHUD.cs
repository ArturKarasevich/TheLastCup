using System.Collections.Generic;
using TheLastCup.Core;
using TheLastCup.Data;
using TheLastCup.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastCup.UI
{
    public class TooltipHUD : MonoBehaviour
    {
        public float FadeInTime = 0.15f;
        public float FadeOutTime = 0.15f;

        private MousePicker _mousePicker;
        private CrystalBall _ball;
        private OutlineEffect _ballOutline;
        private HerbBook _book;
        private OutlineEffect _bookOutline;
        private Teapot _teapot;
        private OutlineEffect _teapotOutline;
        private BoilerTap _tap;
        private OutlineEffect _tapOutline;
        private OutlineEffect _boilerSpotOutline;
        private OutlineEffect _serveSpotOutline;
        private OutlineEffect _teaStationOutline;
        private OutlineEffect _guestTableOutline;
        private PocketWatch _watch;
        private OutlineEffect _watchOutline;

        [Header("UI References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _hintText;
        [SerializeField] private CanvasGroup _teapotCard;
        [SerializeField] private TMP_Text _teapotHerbsText;
        [SerializeField] private TMP_Text _teapotStatusText;

        private float _targetAlpha;
        private string _lastHint;
        private bool _isFirstGuest = true;
        private bool _watchHintShown;
        private bool _watchHintDismissed;
        private float _cardTargetAlpha;
        private string _lastCardKey;
        private Camera _camera;
        private RectTransform _cardRect;
        private RectTransform _canvasRect;
        private Canvas _parentCanvas;
        private readonly List<string> _cardNames = new List<string>(3);
        private float _nextTeapotSearchTime;

        private void Awake()
        {
            FindSceneObjects();
            _camera = Camera.main;

            if (_canvasGroup)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            if (_teapotCard)
            {
                _teapotCard.alpha = 0f;
                _teapotCard.blocksRaycasts = false;
                _teapotCard.interactable = false;
                _cardRect = _teapotCard.GetComponent<RectTransform>();
                _parentCanvas = _teapotCard.GetComponentInParent<Canvas>();
                if (_parentCanvas)
                    _canvasRect = _parentCanvas.GetComponent<RectTransform>();
                StyleTeapotCard();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnHintPulseChanged += OnHintPulseChanged;
            GameEvents.OnTeapotSpawned += OnTeapotSpawned;
        }

        private void OnDisable()
        {
            GameEvents.OnHintPulseChanged -= OnHintPulseChanged;
            GameEvents.OnTeapotSpawned -= OnTeapotSpawned;
        }

        private void OnTeapotSpawned(Transform teapotTransform)
        {
            if (teapotTransform != null)
            {
                _teapot = teapotTransform.GetComponent<Teapot>();
                if (_teapot != null)
                    _teapotOutline = _teapot.GetComponent<OutlineEffect>();
            }
        }

        private void OnHintPulseChanged(bool enabled)
        {
            _isFirstGuest = enabled;
        }

        private void FindSceneObjects()
        {
            _mousePicker = FindFirstObjectByType<MousePicker>();

            _ball = FindFirstObjectByType<CrystalBall>();
            if (_ball) _ballOutline = _ball.GetComponent<OutlineEffect>();

            _book = FindFirstObjectByType<HerbBook>();
            if (_book) _bookOutline = _book.GetComponent<OutlineEffect>();

            _teapot = FindFirstObjectByType<Teapot>();
            if (_teapot) _teapotOutline = _teapot.GetComponent<OutlineEffect>();

            _tap = FindFirstObjectByType<BoilerTap>();
            if (_tap) _tapOutline = _tap.GetComponent<OutlineEffect>();

            var boilerSlot = FindFirstObjectByType<BoilerSpot>();
            if (boilerSlot) _boilerSpotOutline = boilerSlot.GetComponent<OutlineEffect>();

            var servingZone = FindFirstObjectByType<GuestServeSpot>();
            if (servingZone) _serveSpotOutline = servingZone.GetComponent<OutlineEffect>();

            var brewingStation = FindFirstObjectByType<TeaStation>();
            if (brewingStation) _teaStationOutline = brewingStation.GetComponent<OutlineEffect>();

            var guestTable = FindFirstObjectByType<GuestTable>();
            if (guestTable) _guestTableOutline = guestTable.GetComponent<OutlineEffect>();

            _watch = FindFirstObjectByType<PocketWatch>();
            if (_watch) _watchOutline = _watch.GetComponent<OutlineEffect>();
        }

        private void Update()
        {
            if (_teapot == null && Time.unscaledTime >= _nextTeapotSearchTime)
            {
                _nextTeapotSearchTime = Time.unscaledTime + 0.5f;
                _teapot = FindFirstObjectByType<Teapot>();
                if (_teapot != null) _teapotOutline = _teapot.GetComponent<OutlineEffect>();
            }

            string hint = GetCurrentHint();
            if (!string.IsNullOrEmpty(hint))
            {
                _targetAlpha = 1f;
                if (hint != _lastHint)
                {
                    _lastHint = hint;
                    if (_hintText) _hintText.text = hint;
                }
            }
            else
            {
                _targetAlpha = 0f;
            }

            if (_canvasGroup)
            {
                float speed = _targetAlpha > _canvasGroup.alpha
                    ? (1f / FadeInTime)
                    : (1f / FadeOutTime);
                _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, _targetAlpha, speed * Time.unscaledDeltaTime);
            }

            UpdateTeapotCard();
        }

        private void UpdateTeapotCard()
        {
            if (!_teapotCard) return;

            IClickable hovered = _mousePicker ? _mousePicker.CurrentObject : null;
            ITeapotInfo hoveredTeapot = hovered as ITeapotInfo;
            if (hoveredTeapot != null)
            {
                _cardTargetAlpha = 1f;
                HerbInfo[] slots = hoveredTeapot.Slots;
                _cardNames.Clear();
                if (slots != null)
                {
                    for (int i = 0; i < slots.Length; i++)
                    {
                        if (slots[i] != null)
                            _cardNames.Add(slots[i].name);
                    }
                }

                string herbsLine = _cardNames.Count > 0 ? string.Join(" · ", _cardNames) : "Заварник пуст";
                string statusLine = hoveredTeapot.HasHotWater ? "Залит кипяток" : "Сухой сбор";

                string key = $"{herbsLine}|{statusLine}";
                if (key != _lastCardKey)
                {
                    _lastCardKey = key;
                    if (_teapotHerbsText) _teapotHerbsText.text = herbsLine;
                    if (_teapotStatusText) _teapotStatusText.text = statusLine;
                }

                Component comp = hoveredTeapot as Component;
                if (comp != null)
                    MoveCardNearTeapot(comp.transform);
            }
            else
            {
                _cardTargetAlpha = 0f;
            }

            float speed = _cardTargetAlpha > _teapotCard.alpha
                ? (1f / FadeInTime)
                : (1f / FadeOutTime);
            _teapotCard.alpha = Mathf.MoveTowards(_teapotCard.alpha, _cardTargetAlpha, speed * Time.unscaledDeltaTime);
        }

        private void StyleTeapotCard()
        {
            if (!_teapotCard) return;

            if (_cardRect)
            {
                _cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                _cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                _cardRect.pivot = new Vector2(0.5f, 0.5f);
                Vector2 size = _cardRect.sizeDelta;
                _cardRect.sizeDelta = new Vector2(Mathf.Max(size.x, 420f), Mathf.Max(size.y, 90f));
            }

            var bg = _teapotCard.GetComponent<Image>();
            if (bg)
                bg.enabled = false;

            var titleTransform = _teapotCard.transform.Find("Title");
            if (titleTransform)
                titleTransform.gameObject.SetActive(false);

            if (_teapotHerbsText)
            {
                _teapotHerbsText.color = new Color(0.96f, 0.90f, 0.78f, 1f);
                _teapotHerbsText.fontSize = 24f;
            }

            if (_teapotStatusText)
            {
                _teapotStatusText.color = new Color(0.82f, 0.76f, 0.66f, 0.9f);
                _teapotStatusText.fontSize = 18f;
            }
        }

        private void MoveCardNearTeapot(Transform teapotTransform)
        {
            if (!_cardRect || !_canvasRect || !teapotTransform) return;
            if (!_camera) _camera = Camera.main;
            if (!_camera) return;

            Vector3 worldPos = teapotTransform.position + Vector3.down * 0.04f + _camera.transform.right * 0.22f;
            Vector3 screenPos = _camera.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0f) return;

            Camera uiCam = _parentCanvas && _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _parentCanvas.worldCamera
                : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, uiCam, out Vector2 localPos))
            {
                Vector2 halfCanvas = _canvasRect.rect.size * 0.5f;
                Vector2 halfCard = _cardRect.rect.size * 0.5f;
                float margin = 16f;
                localPos.x = Mathf.Clamp(localPos.x, -halfCanvas.x + halfCard.x + margin, halfCanvas.x - halfCard.x - margin);
                localPos.y = Mathf.Clamp(localPos.y, -halfCanvas.y + halfCard.y + margin, halfCanvas.y - halfCard.y - margin);
                _cardRect.anchoredPosition = localPos;
            }
        }

        private const string WatchHintText = "Если захочешь взять перерыв — взгляни на карманные часы";

        private string GetCurrentHint()
        {
            if (!_isFirstGuest)
                return null;

            string otherHint = GetOtherHint();
            if (!string.IsNullOrEmpty(otherHint))
            {
                if (_watchHintShown)
                    _watchHintDismissed = true;
                return otherHint;
            }

            if (!_watchHintDismissed && _watch && !_watch.WasOpened && _watchOutline && _watchOutline.IsPulsing)
            {
                _watchHintShown = true;
                return WatchHintText;
            }

            return null;
        }

        private string GetOtherHint()
        {
            if (_ball && _ball.IsOpen)
                return "Встряхни шар, чтобы акцентировать мысли гостя";

            if (_book && _book.IsOpen)
                return "Кликай по страницам травника, чтобы их листать";

            if (_ballOutline && _ballOutline.IsPulsing)
                return "Посмотри на стеклянный шар — он акцентирует важные слова гостя";

            if (_guestTableOutline && _guestTableOutline.IsPulsing)
                return "Посмотри на столик гостя и выслушай путника";

            if (_teaStationOutline && _teaStationOutline.IsPulsing)
                return "Подойди к столу заваривания — пора собрать чай для гостя";

            if (_bookOutline && _bookOutline.IsPulsing)
                return "Загляни в травник на столе — там описаны свойства всех трав";

            if (_tapOutline && _tapOutline.IsPulsing)
                return "Нажми на кран титана, чтобы залить сбор кипятком";

            if ((_boilerSpotOutline && _boilerSpotOutline.IsPulsing) ||
                (_teapot && _teapot.State == TeapotState.ReadyForBoiler && _teapotOutline && _teapotOutline.IsPulsing))
                return "Перетащи чайник под кран титана";

            if ((_serveSpotOutline && _serveSpotOutline.IsPulsing) ||
                (_teapot && _teapot.State == TeapotState.ReadyToServe && _teapotOutline && _teapotOutline.IsPulsing))
                return "Отнеси готовый чай на поднос гостю";

            IClickable hovered = _mousePicker ? _mousePicker.CurrentObject : null;
            IHintText hintProvider = hovered as IHintText;
            if (hintProvider != null)
                return hintProvider.GetFirstGuestHint();

            return null;
        }
    }
}
