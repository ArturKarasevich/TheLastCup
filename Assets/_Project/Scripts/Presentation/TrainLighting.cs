using System.Collections;
using TheLastCup.Core;
using UnityEngine;

namespace TheLastCup.Presentation
{
    public class TrainLighting : MonoBehaviour
    {
        [Header("Lighting")]
        [SerializeField] private Light[] _roomLights;
        [SerializeField] private Light[] _redLights;
        [SerializeField] private float _twistLightScale = 0.2f;

        [Header("Climax / Train Slowdown")]
        [SerializeField] private AudioSource _wheelsAudio;
        [SerializeField] private float _twistWheelPitch = 0.35f;
        [SerializeField] private float _pitchSpeed = 0.45f;
        [SerializeField] private Light _fireLight;
        [SerializeField] private Color _fireTwistColor = new Color(0.55f, 0.08f, 0.04f, 1f);
        [SerializeField] private float _fireTwistScale = 0.35f;

        [Header("Fog")]
        [SerializeField] private float _normalFog = 0.01f;
        [SerializeField] private float _twistFog = 0.06f;

        [Header("Dawn / Endings")]
        [SerializeField] private Light _sunLight;
        [SerializeField] private Vector3 _darkSunAngle = new Vector3(270f, 0f, 0f);
        [SerializeField] private Vector3 _dawnSunAngle = new Vector3(2.80410266f, 227.817261f, 192.773819f);
        [SerializeField] private Color _goldSunColor = new Color(1.0f, 0.82f, 0.52f, 1f);
        [SerializeField] private float _goldSunPower = 2.2f;
        [SerializeField] private Color _paleSunColor = new Color(0.75f, 0.82f, 0.92f, 1f);
        [SerializeField] private float _paleSunPower = 1.0f;
        [SerializeField] private float _dawnFog = 0.004f;
        [SerializeField] private float _coldFog = 0.07f;
        [SerializeField] private Color _goldFogColor = new Color(0.92f, 0.75f, 0.50f, 1f);
        [SerializeField] private Color _paleFogColor = new Color(0.58f, 0.67f, 0.78f, 1f);
        [SerializeField] private Color _coldFogColor = new Color(0.14f, 0.19f, 0.28f, 1f);
        [SerializeField] private float _endFadeTime = 2.5f;

        private float[] _startLightPowers;
        private Color _startFireColor;
        private float _startFirePower;
        private bool _isTwistActive;
        private bool _isEndingActive;
        private float _targetPitch = 1f;
        private Coroutine _redLightCoroutine;
        private Coroutine _endingCoroutine;

        private void OnEnable()
        {
            GameEvents.OnStoryTwistStarted += OnStoryTwistStarted;
            GameEvents.OnSessionEnded += OnSessionEnded;
        }

        private void OnDisable()
        {
            GameEvents.OnStoryTwistStarted -= OnStoryTwistStarted;
            GameEvents.OnSessionEnded -= OnSessionEnded;
        }

        private void Awake()
        {
            if (_roomLights != null)
            {
                _startLightPowers = new float[_roomLights.Length];
                for (int i = 0; i < _roomLights.Length; i++)
                {
                    if (_roomLights[i])
                        _startLightPowers[i] = _roomLights[i].intensity;
                }
            }

            if (_redLights != null)
            {
                foreach (var light in _redLights)
                {
                    if (!light) continue;
                    light.enabled = false;
                    light.gameObject.SetActive(false);
                }
            }

            if (_sunLight)
            {
                _sunLight.gameObject.SetActive(true);
                _sunLight.enabled = true;
                _sunLight.transform.rotation = Quaternion.Euler(_darkSunAngle);
            }

            if (_fireLight)
            {
                _startFireColor = _fireLight.color;
                _startFirePower = _fireLight.intensity;
            }

            RenderSettings.fogDensity = _normalFog;
        }

        private void Update()
        {
            if (_wheelsAudio)
                _wheelsAudio.pitch = Mathf.MoveTowards(_wheelsAudio.pitch, _targetPitch, _pitchSpeed * Time.deltaTime);

            if (_fireLight && (_isTwistActive || _isEndingActive))
            {
                Color targetCol = _isTwistActive ? _fireTwistColor : _startFireColor;
                float targetInt = _isTwistActive ? _startFirePower * _fireTwistScale : _startFirePower;
                _fireLight.color = Color.Lerp(_fireLight.color, targetCol, Time.deltaTime * 1.2f);
                _fireLight.intensity = Mathf.Lerp(_fireLight.intensity, targetInt, Time.deltaTime * 1.2f);
            }
        }

        private void OnStoryTwistStarted()
        {
            if (_isTwistActive) return;
            _isTwistActive = true;
            _targetPitch = _twistWheelPitch;

            if (_roomLights != null)
            {
                for (int i = 0; i < _roomLights.Length; i++)
                {
                    if (_roomLights[i])
                        _roomLights[i].intensity = _startLightPowers[i] * _twistLightScale;
                }
            }

            RenderSettings.fog = true;
            RenderSettings.fogDensity = _twistFog;

            if (_redLights != null && _redLights.Length > 0)
            {
                foreach (var light in _redLights)
                {
                    if (!light) continue;
                    light.gameObject.SetActive(true);
                    light.enabled = true;
                }
            }

            if (_redLightCoroutine != null)
                StopCoroutine(_redLightCoroutine);
            _redLightCoroutine = StartCoroutine(BlinkRedLightsRoutine());
        }

        private IEnumerator BlinkRedLightsRoutine()
        {
            while (_isTwistActive)
            {
                if (_redLights != null)
                {
                    float intensity = 1.0f + Mathf.PingPong(Time.time * 4f, 2.5f);
                    foreach (var light in _redLights)
                    {
                        if (light) light.intensity = intensity;
                    }
                }

                if (_roomLights != null && _startLightPowers != null)
                {
                    float flicker = 0.65f + Mathf.PerlinNoise(Time.time * 11f, 0.5f) * 0.55f;
                    for (int i = 0; i < _roomLights.Length; i++)
                    {
                        if (_roomLights[i])
                            _roomLights[i].intensity = _startLightPowers[i] * _twistLightScale * flicker;
                    }
                }

                yield return null;
            }
        }

        private void OnSessionEnded(SessionSummary summary)
        {
            if (summary == null) return;

            _isTwistActive = false;
            _isEndingActive = true;
            _targetPitch = summary.Rank == EndingRank.FrozenNight ? 0f : 1f;
            if (_redLightCoroutine != null)
            {
                StopCoroutine(_redLightCoroutine);
                _redLightCoroutine = null;
            }

            if (_redLights != null)
            {
                foreach (var light in _redLights)
                {
                    if (!light) continue;
                    light.enabled = false;
                    light.gameObject.SetActive(false);
                }
            }

            if (_endingCoroutine != null)
                StopCoroutine(_endingCoroutine);

            _endingCoroutine = StartCoroutine(FadeEndingLightingRoutine(summary.Rank));
        }

        private IEnumerator FadeEndingLightingRoutine(EndingRank tier)
        {
            bool enableSun = tier == EndingRank.GoldenDawn || tier == EndingRank.PaleDawn;
            float targetSunIntensity = tier == EndingRank.GoldenDawn ? _goldSunPower : _paleSunPower;
            Color targetSunColor = tier == EndingRank.GoldenDawn ? _goldSunColor : _paleSunColor;

            Quaternion startSunRot = _sunLight ? _sunLight.transform.rotation : Quaternion.identity;
            Quaternion targetSunRot = enableSun ? Quaternion.Euler(_dawnSunAngle) : Quaternion.Euler(_darkSunAngle);
            float startSunIntensity = _sunLight ? _sunLight.intensity : 0f;
            Color startSunColor = _sunLight ? _sunLight.color : Color.white;

            if (_sunLight)
            {
                _sunLight.gameObject.SetActive(true);
                _sunLight.enabled = true;
            }

            float startFog = RenderSettings.fogDensity;
            float targetFog = _coldFog;
            if (tier == EndingRank.GoldenDawn) targetFog = _dawnFog;
            else if (tier == EndingRank.PaleDawn) targetFog = _normalFog;

            Color startFogColor = RenderSettings.fogColor;
            Color targetFogColor = _coldFogColor;
            if (tier == EndingRank.GoldenDawn) targetFogColor = _goldFogColor;
            else if (tier == EndingRank.PaleDawn) targetFogColor = _paleFogColor;

            RenderSettings.fog = true;

            float[] startCabin = _roomLights != null ? new float[_roomLights.Length] : null;
            if (_roomLights != null)
            {
                for (int i = 0; i < _roomLights.Length; i++)
                    startCabin[i] = _roomLights[i] ? _roomLights[i].intensity : 0f;
            }

            float cabinMultiplier = tier == EndingRank.FrozenNight ? 0f : 1f;
            float duration = Mathf.Max(0.1f, _endFadeTime);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

                if (_sunLight)
                {
                    _sunLight.transform.rotation = Quaternion.Slerp(startSunRot, targetSunRot, t);
                    if (enableSun)
                    {
                        _sunLight.intensity = Mathf.Lerp(startSunIntensity, targetSunIntensity, t);
                        _sunLight.color = Color.Lerp(startSunColor, targetSunColor, t);
                    }
                }

                RenderSettings.fogDensity = Mathf.Lerp(startFog, targetFog, t);
                RenderSettings.fogColor = Color.Lerp(startFogColor, targetFogColor, t);

                if (_roomLights != null && _startLightPowers != null)
                {
                    for (int i = 0; i < _roomLights.Length; i++)
                    {
                        if (_roomLights[i])
                            _roomLights[i].intensity = Mathf.Lerp(startCabin[i], _startLightPowers[i] * cabinMultiplier, t);
                    }
                }

                yield return null;
            }

            if (_sunLight)
            {
                _sunLight.transform.rotation = targetSunRot;
                if (enableSun)
                {
                    _sunLight.intensity = targetSunIntensity;
                    _sunLight.color = targetSunColor;
                }
            }

            RenderSettings.fogDensity = targetFog;
            RenderSettings.fogColor = targetFogColor;
            _endingCoroutine = null;
        }
    }
}
