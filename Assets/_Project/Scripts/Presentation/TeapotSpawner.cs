using System.Collections;
using TheLastCup.Core;
using UnityEngine;

namespace TheLastCup.Presentation
{
    public class TeapotSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _teapotPrefab;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Vector3 _spawnScale = new Vector3(10f, 10f, 10f);
        [SerializeField] private float _respawnDelay = 0.5f;

        private void OnEnable()
        {
            GameEvents.OnTeapotBroken += OnTeapotBroken;
            GameEvents.OnResetTeaCycle += SpawnTeapot;
        }

        private void OnDisable()
        {
            GameEvents.OnTeapotBroken -= OnTeapotBroken;
            GameEvents.OnResetTeaCycle -= SpawnTeapot;
        }

        private void Start()
        {
            SpawnTeapot();
        }

        private void OnTeapotBroken()
        {
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(_respawnDelay);
            SpawnTeapot();
        }

        private void SpawnTeapot()
        {
            if (!_teapotPrefab || !_spawnPoint) return;

            var teapot = Instantiate(_teapotPrefab, _spawnPoint.position, _spawnPoint.rotation);
            teapot.transform.localScale = _spawnScale;
        }
    }
}
