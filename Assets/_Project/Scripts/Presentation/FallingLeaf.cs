using TheLastCup.Core;
using TheLastCup.Data;
using UnityEngine;

namespace TheLastCup.Presentation
{
    public class FallingLeaf : MonoBehaviour
    {
        private const float MissedLifetime = 2.0f;

        private float _timer;
        private bool _caught;

        public HerbInfo HerbData { get; private set; }

        private void OnEnable()
        {
            GameEvents.OnTeapotBroken += OnTeapotCleared;
            GameEvents.OnResetTeaCycle += OnTeapotCleared;
            GameEvents.OnTeaDumped += OnTeapotCleared;
        }

        private void OnDisable()
        {
            GameEvents.OnTeapotBroken -= OnTeapotCleared;
            GameEvents.OnResetTeaCycle -= OnTeapotCleared;
            GameEvents.OnTeaDumped -= OnTeapotCleared;
        }

        public void Setup(HerbInfo data)
        {
            HerbData = data;
            _timer = 0f;
            _caught = false;
        }

        public void MarkCaught(Transform teapotParent = null)
        {
            if (_caught) return;
            _caught = true;

            if (teapotParent != null)
                transform.SetParent(teapotParent, true);

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            Collider[] cols = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
                cols[i].enabled = false;
        }

        private void Update()
        {
            if (_caught) return;

            _timer += Time.deltaTime;
            if (_timer >= MissedLifetime)
                Destroy(gameObject);
        }

        private void OnTeapotCleared()
        {
            if (_caught)
                Destroy(gameObject);
        }
    }
}
