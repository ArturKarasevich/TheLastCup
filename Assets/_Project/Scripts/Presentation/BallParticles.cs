using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TheLastCup.Presentation
{
    public class BallParticles : MonoBehaviour
    {
        private const float MinBallRadius = 0.00005f;

        [SerializeField] private int _maxParticles = 4000;
        [SerializeField] private int _idleParticlesCount = 1000;
        [SerializeField] private float _cloudRadius = 1.05f;
        [SerializeField] private int _pointsPerWord = 250;
        [SerializeField] private float _idleParticleSize = 0.003f;
        [SerializeField] private float _letterParticleSize = 0.0045f;
        [SerializeField] private float _driftSpeed = 0.42f;
        [SerializeField] private Color _wordColor = new Color(1f, 0.85f, 0.35f, 1f);
        [SerializeField] private Material _particleMaterial;

        private ParticleSystem _particles;
        private ParticleSystem.Particle[] _particlesArray;
        private Vector3[] _particleStartPositions;
        private Vector3[] _particleOffsets;
        private readonly List<Vector3> _letterTargets = new List<Vector3>();

        private float _ballRadius = 0.05f;
        private float _gatherProgress;
        private float _textRevealProgress;

        public void Setup(Vector3 centerPos, float radius, float offset, Material particleMaterial = null)
        {
            if (_particles != null) return;
            if (particleMaterial != null)
                _particleMaterial = particleMaterial;

            _ballRadius = Mathf.Max(MinBallRadius, (radius - offset) * 0.90f);

            GameObject psGo = new GameObject("BallInnerParticles");
            psGo.transform.SetParent(transform, false);
            psGo.transform.localPosition = centerPos;
            psGo.transform.localRotation = Quaternion.identity;
            psGo.transform.localScale = Vector3.one;

            _particles = psGo.AddComponent<ParticleSystem>();
            var main = _particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startLifetime = 99999f;
            main.startSpeed = 0f;
            main.startSize = _ballRadius * 0.035f;
            main.maxParticles = _maxParticles;

            var emission = _particles.emission;
            emission.enabled = false;

            var shape = _particles.shape;
            shape.enabled = false;

            ParticleSystemRenderer psRenderer = psGo.GetComponent<ParticleSystemRenderer>();
            if (psRenderer != null)
            {
                psRenderer.sortingOrder = 10;
                if (_particleMaterial != null)
                    psRenderer.sharedMaterial = _particleMaterial;
            }

            _particlesArray = new ParticleSystem.Particle[_maxParticles];
            _particleStartPositions = new Vector3[_maxParticles];
            _particleOffsets = new Vector3[_maxParticles];

            for (int i = 0; i < _maxParticles; i++)
            {
                _particleStartPositions[i] = Random.insideUnitSphere;
                _particleOffsets[i] = new Vector3(Random.Range(0f, 100f), Random.Range(0f, 100f), Random.Range(0f, 100f));
                _particlesArray[i].remainingLifetime = 99999f;
                _particlesArray[i].startLifetime = 99999f;
            }

            _particles.Play();
            UpdateParticles();
        }

        public void Reset()
        {
            _gatherProgress = 0f;
            _textRevealProgress = 0f;
            _letterTargets.Clear();
            UpdateParticles();
        }

        public void SetTextProgress(float gatherProgress, float textProgress)
        {
            _gatherProgress = Mathf.Clamp01(gatherProgress);
            _textRevealProgress = Mathf.Clamp01(textProgress);
            UpdateParticles();
        }

        public void StopAndClear()
        {
            if (_particles != null)
                _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void UpdateParticles()
        {
            if (_particles == null || _particlesArray == null || _particlesArray.Length == 0)
                return;

            float time = Time.time * _driftSpeed;
            int countToDraw = _idleParticlesCount;

            if (_letterTargets.Count > 0 && _gatherProgress > 0.01f)
                countToDraw = Mathf.Min(_maxParticles, Mathf.Max(_idleParticlesCount, _letterTargets.Count));

            float gather = Mathf.SmoothStep(0f, 1f, _gatherProgress);
            float baseSize = _ballRadius * _idleParticleSize;
            float maxRadius = _ballRadius * _cloudRadius;

            for (int i = 0; i < countToDraw; i++)
            {
                Vector3 offset = _particleOffsets[i];
                Vector3 drift = new Vector3(
                    Mathf.Sin(time + offset.x) * 0.08f,
                    Mathf.Cos(time * 0.8f + offset.y) * 0.08f,
                    Mathf.Sin(time * 1.1f + offset.z) * 0.08f
                ) * maxRadius;

                Vector3 idlePos = _particleStartPositions[i] * maxRadius + drift;
                if (idlePos.sqrMagnitude > maxRadius * maxRadius)
                    idlePos = idlePos.normalized * maxRadius;
                Vector3 finalPos = idlePos;
                float size = baseSize;
                Color color = new Color(_wordColor.r, _wordColor.g, _wordColor.b, 0.9f);

                if (i < _letterTargets.Count && _gatherProgress > 0.001f)
                {
                    Vector3 targetPos = _letterTargets[i];
                    finalPos = Vector3.Lerp(idlePos, targetPos, gather);

                    float targetAlpha = Mathf.Lerp(0.9f, 1.0f, gather) * (1f - _textRevealProgress * 0.85f);
                    color = new Color(_wordColor.r, _wordColor.g, _wordColor.b, targetAlpha);
                    size = Mathf.Lerp(baseSize, _ballRadius * _letterParticleSize, gather);
                }
                else if (_gatherProgress > 0.001f)
                {
                    color.a *= (1f - gather * 0.6f);
                }

                _particlesArray[i].position = finalPos;
                _particlesArray[i].startSize = size;
                _particlesArray[i].startColor = color;
            }

            _particles.SetParticles(_particlesArray, countToDraw);
        }

        public void CreateLetterTargets(List<TextMeshPro> wordLabels)
        {
            _letterTargets.Clear();
            if (_particles == null || wordLabels == null || wordLabels.Count == 0)
                return;

            Transform psTransform = _particles.transform;
            float invScale = 1f / Mathf.Max(0.0001f, psTransform.lossyScale.x);

            for (int w = 0; w < wordLabels.Count; w++)
            {
                TextMeshPro label = wordLabels[w];
                if (label == null || string.IsNullOrEmpty(label.text))
                    continue;

                label.ForceMeshUpdate();
                Vector2 renderedSize = label.GetRenderedValues(false);
                if (renderedSize.x <= 0.001f)
                    renderedSize = new Vector2(0.14f, 0.04f);

                float worldScale = label.transform.lossyScale.x;
                float halfW = renderedSize.x * 0.5f * worldScale * invScale;
                float halfH = renderedSize.y * 0.5f * worldScale * invScale;
                float depth = _ballRadius * 0.05f;

                Vector3 localCenter = psTransform.InverseTransformPoint(label.transform.position);
                Quaternion localRot = Quaternion.Inverse(psTransform.rotation) * label.transform.rotation;

                for (int p = 0; p < _pointsPerWord; p++)
                {
                    Vector3 pointOffset = new Vector3(
                        Random.Range(-halfW, halfW),
                        Random.Range(-halfH, halfH),
                        Random.Range(-depth, depth)
                    );

                    Vector3 targetPos = localCenter + localRot * pointOffset;
                    _letterTargets.Add(targetPos);
                }
            }
        }
    }
}
