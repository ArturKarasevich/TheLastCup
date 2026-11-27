using System.Collections;
using UnityEngine;

namespace TheLastCup.Presentation
{
    public class BallBreaker : MonoBehaviour
    {
        public float DefaultThrowDistance = 3.5f;
        public float ThrowDuration = 0.35f;
        public int ShardsCount = 14;
        public Vector3 DefaultThrowDirection = new Vector3(0f, 0.25f, -1f);

        private Coroutine _throwCoroutine;

        public void StartBreak(AudioSource audioSource, AudioClip breakSound)
        {
            if (_throwCoroutine != null)
                StopCoroutine(_throwCoroutine);

            _throwCoroutine = StartCoroutine(ThrowAndBreakRoutine(audioSource, breakSound));
        }

        public void Cancel()
        {
            if (_throwCoroutine != null)
            {
                StopCoroutine(_throwCoroutine);
                _throwCoroutine = null;
            }
        }

        private IEnumerator ThrowAndBreakRoutine(AudioSource audioSource, AudioClip breakSound)
        {
            Vector3 startPos = transform.position;
            Vector3 dir = DefaultThrowDirection.normalized;
            float maxDist = DefaultThrowDistance;

            Vector3 impactPos = startPos + dir * maxDist;
            Vector3 impactNormal = -dir;

            RaycastHit[] hits = Physics.RaycastAll(startPos, dir, maxDist, ~0, QueryTriggerInteraction.Ignore);
            float closestDist = float.MaxValue;
            foreach (var hit in hits)
            {
                if (!hit.collider || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.distance < closestDist)
                {
                    closestDist = hit.distance;
                    impactPos = hit.point;
                    impactNormal = hit.normal;
                }
            }

            float duration = Mathf.Max(0.1f, ThrowDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = t * t;
                Vector3 arc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.24f);
                transform.position = Vector3.Lerp(startPos, impactPos, ease) + arc;
                transform.Rotate(new Vector3(420f, 680f, 310f) * Time.deltaTime, Space.World);
                yield return null;
            }

            transform.position = impactPos;
            BreakAtPoint(impactPos, impactNormal, audioSource, breakSound);
            _throwCoroutine = null;
        }

        private void BreakAtPoint(Vector3 hitPoint, Vector3 hitNormal, AudioSource audioSource, AudioClip breakSound)
        {
            if (audioSource && breakSound)
                audioSource.PlayOneShot(breakSound);

            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = false;

            foreach (var mr in GetComponentsInChildren<MeshRenderer>())
                mr.enabled = false;

            foreach (var ps in GetComponentsInChildren<ParticleSystem>())
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            SpawnShards(hitPoint, hitNormal);
        }

        private void SpawnShards(Vector3 hitPoint, Vector3 hitNormal)
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr == null) mr = GetComponentInChildren<MeshRenderer>();
            Material mat = mr != null ? mr.sharedMaterial : null;

            for (int i = 0; i < ShardsCount; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = $"BallShard_{i}";
                shard.transform.position = hitPoint + hitNormal * 0.05f + Random.insideUnitSphere * 0.06f;
                shard.transform.rotation = Random.rotation;
                shard.transform.localScale = new Vector3(
                    Random.Range(0.015f, 0.045f),
                    Random.Range(0.008f, 0.025f),
                    Random.Range(0.015f, 0.045f));

                MeshRenderer shardRenderer;
                if (mat && shard.TryGetComponent<MeshRenderer>(out shardRenderer))
                    shardRenderer.sharedMaterial = mat;

                var rb = shard.AddComponent<Rigidbody>();
                rb.mass = 0.05f;
                rb.AddForce(hitNormal * Random.Range(1.2f, 3.2f) + Random.insideUnitSphere * 1.5f, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);

                Destroy(shard, 10f);
            }
        }
    }
}
