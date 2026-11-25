using System.Collections.Generic;
using TheLastCup.Core;
using UnityEngine;

namespace TheLastCup.Presentation
{
    public class SnowLandscape : MonoBehaviour
    {
        [SerializeField] private GameObject _groundPrefab;
        [SerializeField] private GameObject[] _treePrefabs;
        [SerializeField] private ParticleSystem _snowParticles;

        public float MoveSpeed = 14f;
        public float TwistSpeedMultiplier = 1.35f;
        public float DawnSpeed = 50f;
        public float SpeedChangeRate = 0.25f;
        public bool MoveLeftToRight = true;

        [SerializeField] private int ChunksCount = 16;
        [SerializeField] private float ChunkLenght = 50f;
        [SerializeField] private float GroundDepth = 55f;
        [SerializeField] private int TreesPerChunk = 28;
        [SerializeField] private float MinTreeDistanceFromTrack = 22f;
        [SerializeField] private int Seed = 42;
        [SerializeField] private float SnowJitter = 4f;

        [SerializeField] private Vector2 TreeScaleRange = new Vector2(0.85f, 1.75f);
        [SerializeField] private Vector3 SnowVelocity = new Vector3(0f, -4f, -18f);

        private readonly List<Transform> _chunks = new List<Transform>();

        private bool _isTwistActive;
        private bool _isEndingActive;
        private float _speed;
        private float _targetSpeed;
        private int _guestStep = -1;

        private void OnEnable()
        {
            GameEvents.OnGuestArrived += OnGuestArrived;
            GameEvents.OnStoryTwistStarted += OnStoryTwistStarted;
            GameEvents.OnSessionEnded += OnSessionEnded;
        }

        private void OnDisable()
        {
            GameEvents.OnGuestArrived -= OnGuestArrived;
            GameEvents.OnStoryTwistStarted -= OnStoryTwistStarted;
            GameEvents.OnSessionEnded -= OnSessionEnded;
        }

        private void Start()
        {
            _speed = MoveSpeed;
            _targetSpeed = MoveSpeed;

            if (_chunks.Count == 0)
                CreateLandscape();

            if (_guestStep < 0)
                SetSnowStage(0);
        }

        private void Update()
        {
            if (_chunks.Count == 0) return;

            if (!_isEndingActive)
                _targetSpeed = MoveSpeed * (_isTwistActive ? TwistSpeedMultiplier : 1f);

            _speed = Mathf.MoveTowards(_speed, _targetSpeed, Mathf.Abs(DawnSpeed) * SpeedChangeRate * Time.deltaTime * 0.25f);
            float dir = MoveLeftToRight ? 1f : -1f;
            float delta = _speed * dir * Time.deltaTime;

            float totalLength = ChunksCount * ChunkLenght;
            float halfLength = totalLength * 0.5f;

            for (int i = 0; i < _chunks.Count; i++)
            {
                Transform chunk = _chunks[i];
                if (!chunk) continue;

                Vector3 pos = chunk.localPosition;
                pos.x += delta;

                if (dir < 0f && pos.x < -halfLength - ChunkLenght * 0.5f)
                    pos.x += totalLength;
                else if (dir > 0f && pos.x > halfLength + ChunkLenght * 0.5f)
                    pos.x -= totalLength;

                chunk.localPosition = pos;
            }
        }

        private void OnGuestArrived(Data.GuestInfo spirit)
        {
            if (spirit == null) return;

            _guestStep = spirit.isTwistGuest ? 3 : Mathf.Clamp(_guestStep + 1, 0, 2);
            SetSnowStage(_guestStep);
        }

        private void OnStoryTwistStarted()
        {
            _isTwistActive = true;
            SetSnowStage(3);
        }

        private void SetSnowStage(int stage)
        {
            if (!_snowParticles) return;

            float rate = 1200f;
            float velScale = 1.9f;
            switch (stage)
            {
                case 0: rate = 85f; velScale = 0.65f; break;
                case 1: rate = 240f; velScale = 1.0f; break;
                case 2: rate = 520f; velScale = 1.35f; break;
                case 3: rate = 950f; velScale = 1.7f; break;
            }

            var emission = _snowParticles.emission;
            emission.rateOverTime = rate;

            var vel = _snowParticles.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(
                (SnowVelocity.x - SnowJitter) * velScale,
                (SnowVelocity.x + SnowJitter) * velScale);
            vel.y = new ParticleSystem.MinMaxCurve(
                (SnowVelocity.y - 1.5f) * velScale,
                (SnowVelocity.y + 1f) * velScale);
            vel.z = new ParticleSystem.MinMaxCurve(
                (SnowVelocity.z - SnowJitter) * velScale,
                (SnowVelocity.z + SnowJitter) * velScale);

            if (!_snowParticles.isPlaying)
                _snowParticles.Play();
        }

        private void OnSessionEnded(SessionSummary summary)
        {
            if (summary == null) return;
            _isEndingActive = true;

            if (summary.Rank == EndingRank.GoldenDawn)
            {
                _targetSpeed = DawnSpeed;
                if (_snowParticles)
                    _snowParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            else if (summary.Rank == EndingRank.PaleDawn)
            {
                _targetSpeed = MoveSpeed;
                if (_snowParticles)
                    _snowParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            else
            {
                _targetSpeed = 0f;
                if (_snowParticles)
                    _snowParticles.Play();
            }
        }

        private void CreateLandscape()
        {
            var rng = new System.Random(Seed);
            float totalLength = ChunksCount * ChunkLenght;
            float startX = -totalLength * 0.5f + ChunkLenght * 0.5f;

            for (int c = 0; c < ChunksCount; c++)
            {
                GameObject chunkGo = _groundPrefab
                    ? Instantiate(_groundPrefab, transform, false)
                    : new GameObject($"TerrainChunk_{c:00}");

                chunkGo.name = $"TerrainChunk_{c:00}";
                chunkGo.transform.SetParent(transform, false);
                chunkGo.transform.localPosition = new Vector3(startX + c * ChunkLenght, 0f, 0f);

                SpawnTrees(chunkGo.transform, totalLength, rng);
                _chunks.Add(chunkGo.transform);
            }
        }

        private void SpawnTrees(Transform chunkTransform, float totalLength, System.Random rng)
        {
            if (_treePrefabs == null || _treePrefabs.Length == 0) return;

            float halfChunk = ChunkLenght * 0.5f;
            float minZ = Mathf.Clamp(MinTreeDistanceFromTrack, 0.5f, GroundDepth - 1f);

            for (int i = 0; i < TreesPerChunk; i++)
            {
                GameObject prefab = _treePrefabs[rng.Next(_treePrefabs.Length)];
                if (!prefab) continue;

                float localX = Mathf.Lerp(-halfChunk, halfChunk, (float)rng.NextDouble());
                float localZ = Mathf.Lerp(minZ, GroundDepth - 1f, (float)rng.NextDouble());
                float y = GetGroundHeight(localX, localZ, totalLength);
                float scale = Mathf.Lerp(TreeScaleRange.x, TreeScaleRange.y, (float)rng.NextDouble());
                float yRot = (float)rng.NextDouble() * 360f;

                GameObject tree = Instantiate(prefab, chunkTransform, false);
                tree.transform.localPosition = new Vector3(localX, y - 0.1f, localZ);
                tree.transform.localRotation = Quaternion.Euler(0f, yRot, 0f) * tree.transform.localRotation;
                tree.transform.localScale = tree.transform.localScale * scale;
            }
        }

        private float GetGroundHeight(float localX, float localZ, float totalLength)
        {
            float angle = (localX / Mathf.Max(1f, totalLength)) * Mathf.PI * 2f;
            float seedOffset = (Seed % 10000) * 1.37f;
            float nx = Mathf.Cos(angle) * 2.2f + seedOffset;
            float ny = Mathf.Sin(angle) * 2.2f + localZ * 0.08f;

            float noise = Mathf.PerlinNoise(nx, ny);
            float distFactor = Mathf.Clamp01((localZ - 4f) / Mathf.Max(1f, GroundDepth - 4f));
            float smoothDist = distFactor * distFactor * (3f - 2f * distFactor);

            return (noise * 1f + 7f * distFactor) * smoothDist;
        }
    }
}
