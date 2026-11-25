using System.Collections;
using System.Collections.Generic;
using TheLastCup.Core;
using TheLastCup.Data;
using TheLastCup.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TheLastCup.Presentation
{
    public class CrystalBall : MonoBehaviour, IClickable
    {
        [SerializeField] private TMP_FontAsset _wordFont;
        [SerializeField] private Material _particleMaterial;
        [SerializeField] private AudioSource _breakAudioSource;
        [SerializeField] private AudioClip _breakSound;

        [SerializeField] private float CloseDistance = 0.095f;
        [SerializeField] private float InspectHeightOffset = 0.005f;
        [SerializeField] private float MoveSpeed = 10f;

        public int MaxLineChars = 25;
        public int MaxWords = 3;
        public float WordSpacing = 0.85f;
        public float ParticleTextScale = 2.25f;
        public float WordFontSize = 0.11f;

        [SerializeField] private float SurfaceOffset = 0.015f;
        [SerializeField] private float WordFadeTime = 0.85f;
        [SerializeField] private float TextFadeTime = 0.65f;

        [SerializeField] private Vector2 WordBoxSize = new Vector2(0.24f, 0.055f);
        [SerializeField] private Color WordColor = new Color(1f, 0.85f, 0.35f, 1f);

        private readonly List<TextMeshPro> _wordLabels = new List<TextMeshPro>();
        private readonly HashSet<string> _shownWords = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        private OutlineEffect _outline;
        private BallParticles _particles;
        private BallBreaker _breaker;
        private GuestInfo _currentGuest;
        private bool _isBroken;
        private bool _shouldBreakOnWall;
        private bool _hasStartedBreaking;
        private bool _isOpen;

        public bool IsOpen { get { return _isOpen; } }
        public bool IsBroken { get { return _isBroken; } }

        private bool _wasInspected;
        private bool _sentInspectedEvent;
        private bool _areWordsShown;
        private int _openedOnFrame = -1;

        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private Coroutine _moveCoroutine;
        private Coroutine _breakDelayCoroutine;
        private Coroutine _shakeCoroutine;
        private Coroutine _wordFadeCoroutine;
        private Transform _wordsContainer;

        private void OnEnable()
        {
            GameEvents.OnGuestArrived += OnGuestArrived;
            GameEvents.OnStoryTwistStarted += OnStoryTwistStarted;
            GameEvents.OnGuestDoorOpened += OnGuestDoorOpened;
            GameEvents.OnBallWordShown += OnWordRevealed;
            GameEvents.OnIntroTalkEnded += OnIntroSpeechEnded;
        }

        private void OnDisable()
        {
            GameEvents.OnGuestArrived -= OnGuestArrived;
            GameEvents.OnStoryTwistStarted -= OnStoryTwistStarted;
            GameEvents.OnGuestDoorOpened -= OnGuestDoorOpened;
            GameEvents.OnBallWordShown -= OnWordRevealed;
            GameEvents.OnIntroTalkEnded -= OnIntroSpeechEnded;
        }

        private void Awake()
        {
            _homePosition = transform.position;
            _homeRotation = transform.rotation;
            _outline = GetComponent<OutlineEffect>();
            _particles = GetComponent<BallParticles>();
            if (_particles == null) _particles = gameObject.AddComponent<BallParticles>();
            _breaker = GetComponent<BallBreaker>();
            if (_breaker == null) _breaker = gameObject.AddComponent<BallBreaker>();

            GetBallBounds(out Vector3 localCenter, out float localRadius, out _);
            float scale = Mathf.Max(0.001f, transform.lossyScale.x);
            _particles.Setup(localCenter, localRadius, SurfaceOffset / scale, _particleMaterial);

            if (!FindFirstObjectByType<BallFloatingUI>())
                gameObject.AddComponent<BallFloatingUI>();
        }

        private void Update()
        {
            if (!_isOpen || Time.frameCount == _openedOnFrame)
                return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseBall();
                return;
            }

            if (Mouse.current == null) return;

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                CloseBall();
                return;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (!IsMouseOverBall() && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                    CloseBall();
            }
        }

        private void LateUpdate()
        {
            if (_isBroken)
                return;

            if (_areWordsShown && _wordLabels.Count > 0)
                FaceWordsToCamera();

            if (_isOpen || _moveCoroutine != null || _shakeCoroutine != null || _wordFadeCoroutine != null)
                _particles.UpdateParticles();
        }

        private void OnGuestArrived(GuestInfo spirit)
        {
            StopWordFadeCoroutine();
            _currentGuest = spirit;
            _areWordsShown = false;
            _particles.Reset();
            _shownWords.Clear();
            ClearWords();
        }

        private void StopWordFadeCoroutine()
        {
            if (_wordFadeCoroutine != null)
            {
                StopCoroutine(_wordFadeCoroutine);
                _wordFadeCoroutine = null;
            }
        }

        private void OnWordRevealed(string keyword)
        {
            if (string.IsNullOrEmpty(keyword) || _isBroken)
                return;

            if (_shownWords.Add(keyword) && _areWordsShown)
            {
                if (_wordFadeCoroutine != null)
                    StopCoroutine(_wordFadeCoroutine);
                _wordFadeCoroutine = StartCoroutine(ShowNewWordSmoothlyRoutine(keyword));
            }
        }

        private IEnumerator ShowNewWordSmoothlyRoutine(string newlyAddedKeyword)
        {
            RebuildWords(1f, newlyAddedKeyword);
            _particles.CreateLetterTargets(_wordLabels);

            string wrappedTarget = WrapTextByLength(newlyAddedKeyword, MaxLineChars);
            TextMeshPro newLabel = null;
            foreach (var lbl in _wordLabels)
            {
                if (lbl && string.Equals(lbl.text, wrappedTarget, System.StringComparison.OrdinalIgnoreCase))
                {
                    newLabel = lbl;
                    break;
                }
            }

            yield return FadeInWordRoutine(0.25f, Mathf.Max(0.2f, WordFadeTime * 0.75f), newLabel);
            _wordFadeCoroutine = null;
        }

        private IEnumerator FadeInWordRoutine(float convergeFrom, float convergeDuration, TextMeshPro singleLabel = null)
        {
            float elapsed = 0f;
            while (elapsed < convergeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float smooth = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / convergeDuration));
                _particles.SetTextProgress(Mathf.Lerp(convergeFrom, 1f, smooth), singleLabel ? 1f : 0f);
                yield return null;
            }

            float tmpDuration = Mathf.Max(0.15f, TextFadeTime);
            float tmpElapsed = 0f;
            while (tmpElapsed < tmpDuration)
            {
                tmpElapsed += Time.unscaledDeltaTime;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tmpElapsed / tmpDuration));
                if (singleLabel)
                {
                    Color c = WordColor;
                    c.a = a;
                    singleLabel.color = c;
                    _particles.SetTextProgress(1f, 1f);
                }
                else
                {
                    _particles.SetTextProgress(1f, a);
                    SetWordsAlpha(a);
                }
                yield return null;
            }

            _particles.SetTextProgress(1f, 1f);
            SetWordsAlpha(1f);
        }

        private void OnIntroSpeechEnded(GuestInfo spirit)
        {
            if (!_wasInspected && !_isOpen && !_isBroken && _outline)
                _outline.SetPulse(true);
        }

        private void OnStoryTwistStarted()
        {
            StopWordFadeCoroutine();
            _isBroken = true;
            _shouldBreakOnWall = true;
            if (_isOpen) CloseBall();

            if (_outline)
            {
                _outline.SetPulse(false);
                _outline.SetLit(false);
            }
            SetWordsVisible(false);
            _particles.StopAndClear();

            if (_breakDelayCoroutine != null)
                StopCoroutine(_breakDelayCoroutine);
            _breakDelayCoroutine = StartCoroutine(DelayedBreakRoutine());
        }

        private void OnGuestDoorOpened()
        {
            if (!_shouldBreakOnWall || _hasStartedBreaking) return;
            BreakOnWallNow();
        }

        private IEnumerator DelayedBreakRoutine()
        {
            yield return new WaitForSeconds(1.2f);
            if (_shouldBreakOnWall && !_hasStartedBreaking)
                BreakOnWallNow();
        }

        private void BreakOnWallNow()
        {
            _shouldBreakOnWall = false;
            _hasStartedBreaking = true;

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }
            if (_breakDelayCoroutine != null)
            {
                StopCoroutine(_breakDelayCoroutine);
                _breakDelayCoroutine = null;
            }

            _breaker.StartBreak(_breakAudioSource, _breakSound);
        }

        public void OnHoverStart()
        {
            if (_isOpen || _isBroken) return;
            if (_outline) _outline.SetLit(true);
        }

        public void OnHoverEnd()
        {
            if (_outline) _outline.SetLit(false);
        }

        public void OnClick()
        {
            if (_isBroken) return;

            if (!_isOpen)
                OpenBall();
            else if (Time.frameCount != _openedOnFrame)
                ShakeBall();
        }

        public void OpenBall()
        {
            if (_isOpen) return;

            _isOpen = true;
            _wasInspected = true;
            _areWordsShown = false;
            _openedOnFrame = Time.frameCount;

            if (_outline)
            {
                _outline.SetPulse(false);
                _outline.SetLit(false);
            }

            GameEvents.SendBallFocusChanged(true);
            GameEvents.SetUIMode(true);
            SetWordsVisible(false);

            MoveBallTo(GetInspectPosition(), _homeRotation);
        }

        public void ShakeBall()
        {
            if (_isBroken) return;

            StopWordFadeCoroutine();
            if (_shakeCoroutine != null)
                StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ShakeAndShowWordsRoutine());
        }

        public void CloseBall()
        {
            if (!_isOpen) return;

            _isOpen = false;
            _areWordsShown = false;
            _particles.SetTextProgress(0f, 0f);

            StopWordFadeCoroutine();
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;
            }

            SetWordsVisible(false);

            GameEvents.SendBallFocusChanged(false);
            GameEvents.SetUIMode(false);

            if (!_sentInspectedEvent)
            {
                _sentInspectedEvent = true;
                GameEvents.SendBallInspected();
            }

            MoveBallTo(_homePosition, _homeRotation);
        }

        private IEnumerator ShakeAndShowWordsRoutine()
        {
            _particles.SetTextProgress(0f, 0f);
            SetWordsAlpha(0f);

            Vector3 basePos = GetInspectPosition();
            const float shakeDuration = 0.34f;
            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float damp = 1f - Mathf.Clamp01(elapsed / shakeDuration);
                transform.position = basePos + Random.insideUnitSphere * (0.010f * damp);
                yield return null;
            }

            transform.position = basePos;

            _areWordsShown = true;
            RebuildWords(0f);
            FaceWordsToCamera();
            _particles.CreateLetterTargets(_wordLabels);

            yield return FadeInWordRoutine(0f, Mathf.Max(0.2f, WordFadeTime));
            _shakeCoroutine = null;
        }

        private Vector3 GetInspectPosition()
        {
            Camera cam = Camera.main;
            if (!cam) return _homePosition;

            GetBallBounds(out Vector3 localSphereCenter, out _, out _);
            Vector3 pivotToSphereWorld = _homeRotation * Vector3.Scale(localSphereCenter, transform.lossyScale);

            Vector3 targetSphereCenter = cam.transform.position
                                         + cam.transform.forward * CloseDistance
                                         + cam.transform.up * InspectHeightOffset;

            return targetSphereCenter - pivotToSphereWorld;
        }

        private bool IsMouseOverBall()
        {
            Camera cam = Camera.main;
            if (!cam || Mouse.current == null) return false;

            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore))
                return hit.transform == transform || hit.transform.IsChildOf(transform);

            return false;
        }

        private void MoveBallTo(Vector3 targetPos, Quaternion targetRot)
        {
            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _moveCoroutine = StartCoroutine(MoveBallRoutine(targetPos, targetRot));
        }

        private IEnumerator MoveBallRoutine(Vector3 targetPos, Quaternion targetRot)
        {
            float elapsed = 0f;
            const float maxDuration = 1.0f;

            while (elapsed < maxDuration &&
                   (Vector3.Distance(transform.position, targetPos) > 0.002f ||
                    Quaternion.Angle(transform.rotation, targetRot) > 0.5f))
            {
                float dt = Time.unscaledDeltaTime;
                elapsed += dt;
                transform.position = Vector3.Lerp(transform.position, targetPos, dt * MoveSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, dt * MoveSpeed);
                yield return null;
            }

            transform.position = targetPos;
            transform.rotation = targetRot;
            _moveCoroutine = null;
        }

        private void ClearWords()
        {
            foreach (var label in _wordLabels)
            {
                if (label) Destroy(label.gameObject);
            }
            _wordLabels.Clear();
        }

        private void RebuildWords(float initialAlpha, string zeroAlphaKeyword = null)
        {
            if (!_wordsContainer)
            {
                var rootGo = new GameObject("BallKeywords_TMP");
                rootGo.transform.SetParent(transform, false);
                _wordsContainer = rootGo.transform;
            }

            ClearWords();

            string[] allKeywords = _currentGuest != null ? _currentGuest.keyWords : null;
            if (allKeywords == null || allKeywords.Length == 0)
                return;

            GetBallBounds(out Vector3 localSphereCenter, out float localRadius, out Vector3 localUp);
            _wordsContainer.localPosition = localSphereCenter;
            _wordsContainer.gameObject.SetActive(true);

            float scale = Mathf.Max(0.001f, transform.lossyScale.x);
            float invScale = (1f / (scale * scale)) * ParticleTextScale;
            float localOffset = SurfaceOffset / scale;
            float innerR = Mathf.Max(0.005f, (localRadius - localOffset) * 0.54f);
            int totalSlots = Mathf.Min(allKeywords.Length, MaxWords);

            for (int slotIdx = 0; slotIdx < totalSlots; slotIdx++)
            {
                string kw = allKeywords[slotIdx];
                if (string.IsNullOrEmpty(kw) || !_shownWords.Contains(kw))
                    continue;

                float t = totalSlots > 1 ? (slotIdx / (float)(totalSlots - 1)) : 0.5f;
                float verticalOffset = totalSlots > 1 ? Mathf.Lerp(innerR * WordSpacing, -innerR * WordSpacing, t) : 0f;

                var labelGo = new GameObject($"BallInnerWord_TMP_{slotIdx}");
                var tmp = labelGo.AddComponent<TextMeshPro>();
                labelGo.transform.SetParent(_wordsContainer, false);
                labelGo.transform.localPosition = localUp * verticalOffset;
                labelGo.transform.localScale = Vector3.one * invScale;

                bool isNewZero = !string.IsNullOrEmpty(zeroAlphaKeyword) &&
                                 string.Equals(kw, zeroAlphaKeyword, System.StringComparison.OrdinalIgnoreCase);
                float a = isNewZero ? 0f : initialAlpha;

                if (_wordFont) tmp.font = _wordFont;
                tmp.text = WrapTextByLength(kw, MaxLineChars);
                tmp.color = new Color(WordColor.r, WordColor.g, WordColor.b, a);
                tmp.fontSize = WordFontSize;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.lineSpacing = -16f;
                tmp.rectTransform.sizeDelta = WordBoxSize;

                MeshRenderer tmpMr;

                if (tmp.TryGetComponent<MeshRenderer>(out tmpMr))
                {
                    tmpMr.enabled = true;
                    tmpMr.sortingOrder = 15;
                }

                _wordLabels.Add(tmp);
            }

            FaceWordsToCamera();
        }

        private static string WrapTextByLength(string text, int maxCharsPerLine)
        {
            if (string.IsNullOrEmpty(text) || maxCharsPerLine <= 0)
                return text;

            var result = new System.Text.StringBuilder(text.Length + 8);
            string[] paragraphs = text.Replace("\r\n", "\n").Split('\n');

            for (int p = 0; p < paragraphs.Length; p++)
            {
                if (p > 0)
                    result.Append('\n');

                string[] words = paragraphs[p].Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                int currentLineLen = 0;

                for (int w = 0; w < words.Length; w++)
                {
                    string word = words[w];
                    if (currentLineLen == 0)
                    {
                        result.Append(word);
                        currentLineLen = word.Length;
                    }
                    else if (currentLineLen + 1 + word.Length <= maxCharsPerLine)
                    {
                        result.Append(' ').Append(word);
                        currentLineLen += 1 + word.Length;
                    }
                    else
                    {
                        result.Append('\n').Append(word);
                        currentLineLen = word.Length;
                    }
                }
            }

            return result.ToString();
        }

        private void FaceWordsToCamera()
        {
            Camera cam = Camera.main;
            if (!cam) return;

            Vector3 camUp = cam.transform.up;
            for (int i = 0; i < _wordLabels.Count; i++)
            {
                TextMeshPro label = _wordLabels[i];
                if (!label) continue;

                Vector3 lookDir = label.transform.position - cam.transform.position;
                if (lookDir.sqrMagnitude > 0.0001f)
                    label.transform.rotation = Quaternion.LookRotation(lookDir.normalized, camUp);
            }
        }

        private void SetWordsAlpha(float alpha)
        {
            for (int i = 0; i < _wordLabels.Count; i++)
            {
                TextMeshPro label = _wordLabels[i];
                if (!label) continue;

                Color c = WordColor;
                c.a = alpha;
                label.color = c;
            }
        }

        private void GetBallBounds(out Vector3 localCenter, out float localRadius, out Vector3 localUp)
        {
            float scale = Mathf.Max(0.001f, transform.lossyScale.x);
            float localOffset = SurfaceOffset / scale;

            localUp = (Quaternion.Inverse(_homeRotation) * Vector3.up).normalized;

            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Bounds b = mf.sharedMesh.bounds;
                Vector3 ext = b.extents;

                float horizExtent = Mathf.Min(ext.y, ext.z);
                if (Mathf.Abs(localUp.z) > 0.7f)
                    horizExtent = Mathf.Min(ext.x, ext.y);
                else if (Mathf.Abs(localUp.y) > 0.7f)
                    horizExtent = Mathf.Min(ext.x, ext.z);

                float upExtent = Mathf.Abs(Vector3.Dot(ext, localUp));
                localCenter = b.center + localUp * Mathf.Max(0f, upExtent - horizExtent);
                localRadius = horizExtent + localOffset;
                return;
            }

            localCenter = Vector3.zero;
            localRadius = 0.5f + localOffset;
        }

        private void SetWordsVisible(bool visible)
        {
            if (_wordsContainer)
                _wordsContainer.gameObject.SetActive(visible);
        }
    }
}
