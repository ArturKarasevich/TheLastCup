using System.Collections;
using TheLastCup.Core;
using TheLastCup.Data;
using TheLastCup.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLastCup.Presentation
{
    public enum TeapotState
    {
        CollectingHerbs,
        ReadyForBoiler,
        CarriedForBoiler,
        UnderBoiler,
        ReadyToServe,
        CarriedForGuest,
        Served,
        Falling,
        Broken
    }

    public class Teapot : MonoBehaviour, IClickable, ITeapotInfo, IHintText
    {
        [SerializeField] private MeshRenderer _teaLiquidRenderer;
        [SerializeField] private ParticleSystem _steamEffect;
        [SerializeField] private OutlineEffect _outline;
        [SerializeField] private float _autoExitDelay = 0.5f;
        [SerializeField] private float _snapSpeed = 12f;

        [Header("Break Settings")]
        [SerializeField] private LayerMask _floorLayer = ~0;
        [SerializeField] private float _minBreakSpeed = 1.5f;
        [SerializeField] private int _shardsCount = 8;
        [SerializeField] private Material _shardMaterial;

        [Header("Discard Settings")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _dumpSound;

        private TeaMaker _teaMaker;
        private Collider[] _colliders;
        private Rigidbody _rigidbody;
        private Coroutine _moveCoroutine;
        private TeapotState _state = TeapotState.CollectingHerbs;
        private TeapotState _stateBeforeFall = TeapotState.ReadyForBoiler;
        private bool _hasHotWater;
        private bool _isHovered;
        private int _defaultLayer;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;

        public TeaMaker TeaMaker { get { return _teaMaker; } }
        public TeapotState State { get { return _state; } }
        public bool HasHotWater { get { return _hasHotWater; } }
        public HerbInfo[] Slots { get { return _teaMaker != null ? _teaMaker.CupSlots : null; } }
        public bool CanInteract { get { return _state == TeapotState.ReadyForBoiler || _state == TeapotState.ReadyToServe || _state == TeapotState.Falling; } }

        public string GetFirstGuestHint()
        {
            if (_state != TeapotState.CollectingHerbs || _teaMaker == null || _teaMaker.CupSlots == null)
                return null;

            int count = 0;
            for (int i = 0; i < _teaMaker.CupSlots.Length; i++)
            {
                if (_teaMaker.CupSlots[i] != null)
                    count++;
            }
            return $"Перетащи в чайник по одной траве с каждой полки ({count} из {_teaMaker.CupSlots.Length})";
        }

        private void OnEnable()
        {
            GameEvents.OnHerbAddRequested += OnHerbAdded;
            GameEvents.OnTeapotDropped += OnReleased;
            GameEvents.OnTeapotPlacedUnderBoiler += OnPlacedUnderBoiler;
            GameEvents.OnHotWaterRequested += OnHotWaterPoured;
            GameEvents.OnTeapotServed += OnServedToGuest;
            GameEvents.OnResetTeaCycle += ResetTeapot;
            GameEvents.OnTuningForkRequested += OnTuningForkUsed;
        }

        private void OnDisable()
        {
            GameEvents.OnHerbAddRequested -= OnHerbAdded;
            GameEvents.OnTeapotDropped -= OnReleased;
            GameEvents.OnTeapotPlacedUnderBoiler -= OnPlacedUnderBoiler;
            GameEvents.OnHotWaterRequested -= OnHotWaterPoured;
            GameEvents.OnTeapotServed -= OnServedToGuest;
            GameEvents.OnResetTeaCycle -= ResetTeapot;
            GameEvents.OnTuningForkRequested -= OnTuningForkUsed;
        }

        private void OnTuningForkUsed()
        {
            GameEvents.SubmitTuningFork(_teaMaker);
        }

        private void ResetTeapot()
        {
            Destroy(gameObject);
        }

        private void Awake()
        {
            _defaultLayer = gameObject.layer;
            _homePosition = transform.position;
            _homeRotation = transform.rotation;
            int slotsCount = 3;
            if (GameLauncher.CurrentGameData != null && GameLauncher.CurrentGameData.SessionRules != null)
                slotsCount = GameLauncher.CurrentGameData.SessionRules.slotsCount;
            _teaMaker = new TeaMaker(slotsCount);
            _colliders = GetComponentsInChildren<Collider>(true);

            if (!TryGetComponent(out _rigidbody))
                _rigidbody = gameObject.AddComponent<Rigidbody>();

            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            if (!_outline)
                _outline = GetComponent<OutlineEffect>();

            UpdateLiquidLook();
            GameEvents.SendTeapotSpawned(transform);
        }

        private void OnHerbAdded(HerbInfo herb)
        {
            if (herb == null || _state != TeapotState.CollectingHerbs) return;

            int slot = (int)herb.SlotType;
            if (_teaMaker.CupSlots[slot] != null)
            {
                GameEvents.ShowSubtitle("Этого уже достаточно в чайнике...");
                return;
            }

            _teaMaker.AddHerb(herb);
            GameEvents.SetTeaSlot(slot, herb);
            GameEvents.ShowSubtitle($"Добавлено: {herb.name}");

            if (_teaMaker.IsCupFull)
                StartCoroutine(FinishAddingHerbsRoutine());
        }

        private IEnumerator FinishAddingHerbsRoutine()
        {
            yield return new WaitForSeconds(_autoExitDelay);

            _state = TeapotState.ReadyForBoiler;
            if (_outline)
                _outline.SetPulse(true);

            GameEvents.SendAllHerbsAdded(transform);
            GameEvents.ShowSubtitle("Сбор готов. Зажмите ЛКМ на чайнике и перенесите к бойлеру.");
        }

        private void OnReleased()
        {
            if (_state != TeapotState.CarriedForBoiler && _state != TeapotState.CarriedForGuest)
                return;

            StartCoroutine(FallIfNotPlacedRoutine());
        }

        private IEnumerator FallIfNotPlacedRoutine()
        {
            yield return null;

            if (_state != TeapotState.CarriedForBoiler && _state != TeapotState.CarriedForGuest)
                yield break;

            _stateBeforeFall = _state == TeapotState.CarriedForBoiler
                ? TeapotState.ReadyForBoiler
                : TeapotState.ReadyToServe;

            _state = TeapotState.Falling;
            SetLayerForAllChildren(gameObject, _defaultLayer);
            SetCollidersEnabled(true);
            MakeMeshCollidersConvex(true);
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            if (_outline)
                _outline.SetPulse(true);
        }

        private void MakeMeshCollidersConvex(bool convex)
        {
            if (_colliders == null) return;

            bool needsFallbackBox = false;
            foreach (var col in _colliders)
            {
                MeshCollider mc = col as MeshCollider;
                if (mc != null && !mc.isTrigger)
                {
                    if (convex && !mc.convex && mc.sharedMesh != null && !mc.sharedMesh.isReadable)
                    {
                        mc.enabled = false;
                        needsFallbackBox = true;
                    }
                    else
                    {
                        mc.convex = convex;
                    }
                }
            }

            if (needsFallbackBox)
                AddBoxColliderIfMissing();
        }

        private void AddBoxColliderIfMissing()
        {
            foreach (var col in _colliders)
            {
                BoxCollider bc = col as BoxCollider;
                if (bc != null && !bc.isTrigger && bc.enabled)
                    return;
            }

            var box = gameObject.AddComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = new Vector3(0.2f, 0.18f, 0.2f);
            _colliders = GetComponentsInChildren<Collider>(true);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_state != TeapotState.Falling) return;

            bool isFloor = (_floorLayer.value & (1 << collision.gameObject.layer)) != 0;
            if (!isFloor || collision.relativeVelocity.magnitude < _minBreakSpeed)
                return;

            BreakTeapot();
        }

        private void BreakTeapot()
        {
            _state = TeapotState.Broken;
            SpawnShards();
            GameEvents.ShowSubtitle("Чайник разбился! Заварите заново.");
            GameEvents.SendTeapotBroken();
            Destroy(gameObject);
        }

        private void SpawnShards()
        {
            for (int i = 0; i < _shardsCount; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "Teapot_Shard";
                shard.transform.position = transform.position + Random.insideUnitSphere * 0.08f;
                shard.transform.rotation = Random.rotation;
                shard.transform.localScale = new Vector3(
                    Random.Range(0.02f, 0.05f),
                    Random.Range(0.01f, 0.02f),
                    Random.Range(0.02f, 0.05f)
                );

                MeshRenderer mr = shard.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    if (_shardMaterial != null)
                    {
                        mr.material = new Material(_shardMaterial);
                    }
                    else
                    {
                        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                        if (urpShader != null)
                            mr.material = new Material(urpShader);
                    }
                }

                var rb = shard.AddComponent<Rigidbody>();
                rb.AddExplosionForce(1.5f, transform.position, 0.5f, 0.3f, ForceMode.Impulse);
                Destroy(shard, 3f);
            }
        }

        private void OnPlacedUnderBoiler(Transform slotPoint)
        {
            if (_state != TeapotState.CarriedForBoiler || !slotPoint) return;

            _state = TeapotState.UnderBoiler;
            if (_outline)
                _outline.SetPulse(false);

            SetLayerForAllChildren(gameObject, _defaultLayer);
            MoveToPoint(slotPoint.position, slotPoint.rotation, EnableColliders);
            GameEvents.ShowSubtitle("Нажмите на краник бойлера, чтобы налить кипяток.");
        }

        private void OnHotWaterPoured()
        {
            if (_state != TeapotState.UnderBoiler) return;

            _hasHotWater = true;
            _state = TeapotState.ReadyToServe;
            UpdateLiquidLook();

            if (_outline)
                _outline.SetPulse(true);

            if (_steamEffect)
                _steamEffect.Play();

            GameEvents.BrewTea();
            GameEvents.ShowSubtitle("Чай заварен! Зажмите ЛКМ на чайнике и отнесите гостю.");
        }

        private void OnServedToGuest(Transform servePoint)
        {
            if (_state != TeapotState.CarriedForGuest || !servePoint) return;

            _state = TeapotState.Served;
            if (_outline)
                _outline.SetPulse(false);

            SetLayerForAllChildren(gameObject, _defaultLayer);
            MoveToPoint(servePoint.position, servePoint.rotation, EnableColliders);
            GameEvents.SubmitTea(_teaMaker);
        }

        private void MoveToPoint(Vector3 targetPos, Quaternion targetRot, System.Action onComplete = null)
        {
            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _moveCoroutine = StartCoroutine(MoveToPointRoutine(targetPos, targetRot, onComplete));
        }

        private IEnumerator MoveToPointRoutine(Vector3 targetPos, Quaternion targetRot, System.Action onComplete)
        {
            while (Vector3.Distance(transform.position, targetPos) > 0.005f ||
                   Quaternion.Angle(transform.rotation, targetRot) > 0.5f)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * _snapSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * _snapSpeed);
                yield return null;
            }

            transform.position = targetPos;
            transform.rotation = targetRot;
            if (onComplete != null) onComplete.Invoke();
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (_colliders == null) return;

            foreach (var col in _colliders)
            {
                if (col) col.enabled = enabled;
            }
        }

        private const int LiquidRenderQueue = 2990;

        private void UpdateLiquidLook()
        {
            if (!_teaLiquidRenderer) return;

            if (!HasHotWater)
            {
                _teaLiquidRenderer.enabled = false;
                return;
            }

            _teaLiquidRenderer.enabled = true;
            _teaLiquidRenderer.material.renderQueue = LiquidRenderQueue;
            _teaLiquidRenderer.material.color = GetMixedTeaColor();
        }

        private Color GetMixedTeaColor()
        {
            Color result = Color.clear;
            int count = 0;

            if (_teaMaker != null && _teaMaker.CupSlots != null)
            {
                for (int i = 0; i < _teaMaker.CupSlots.Length; i++)
                {
                    if (_teaMaker.CupSlots[i] != null)
                    {
                        result += _teaMaker.CupSlots[i].GetTeaColor();
                        count++;
                    }
                }
            }

            return count > 0 ? (result / count) : new Color(0.9f, 0.9f, 0.85f, 0.2f);
        }

        private void Update()
        {
            if (_isHovered && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                DumpTea();
        }

        public bool DumpTea()
        {
            if (_state == TeapotState.Served || _state == TeapotState.Broken || _state == TeapotState.Falling)
                return false;

            bool hasAnyHerb = false;
            if (_teaMaker != null && _teaMaker.CupSlots != null)
            {
                for (int i = 0; i < _teaMaker.CupSlots.Length; i++)
                {
                    if (_teaMaker.CupSlots[i] != null)
                    {
                        hasAnyHerb = true;
                        break;
                    }
                }
            }

            if (!hasAnyHerb && !_hasHotWater)
                return false;

            StopAllCoroutines();
            _moveCoroutine = null;

            _teaMaker.EmptyCup();
            for (int i = 0; i < _teaMaker.CupSlots.Length; i++)
                GameEvents.SetTeaSlot(i, null);

            _hasHotWater = false;
            _state = TeapotState.CollectingHerbs;

            if (_steamEffect)
                _steamEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            UpdateLiquidLook();

            if (_outline)
            {
                _outline.SetPulse(false);
                _outline.SetLit(false);
            }

            if (_audioSource && _dumpSound)
                _audioSource.PlayOneShot(_dumpSound);

            SetLayerForAllChildren(gameObject, _defaultLayer);
            MoveToPoint(_homePosition, _homeRotation, EnableColliders);
            GameEvents.SendTeaDumped();
            GameEvents.ShowSubtitle("Сбор сброшен. Заварник снова пуст.");
            return true;
        }

        private void EnableColliders()
        {
            SetCollidersEnabled(true);
        }

        public void OnHoverStart()
        {
            _isHovered = true;
            if (!CanInteract) return;

            if (_outline)
                _outline.SetLit(true);
        }

        public void OnHoverEnd()
        {
            _isHovered = false;
            if (_outline)
                _outline.SetLit(false);
        }

        public void OnClick()
        {
            if (!CanInteract) return;

            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            if (_outline)
            {
                _outline.SetPulse(false);
                _outline.SetLit(false);
            }

            if (_state == TeapotState.Falling)
                _state = _stateBeforeFall;

            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _stateBeforeFall = _state;

            _state = _state == TeapotState.ReadyForBoiler
                ? TeapotState.CarriedForBoiler
                : TeapotState.CarriedForGuest;

            SetLayerForAllChildren(gameObject, 2);
            GameEvents.GrabTeapot(transform);
        }

        private void SetLayerForAllChildren(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerForAllChildren(child.gameObject, layer);
        }
    }
}
