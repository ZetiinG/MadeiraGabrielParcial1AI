using UnityEngine;
using Core;

namespace POI
{
    public class POISpawner : MonoBehaviour
    {
        [SerializeField] private GameObject poiPrefab;
        [SerializeField] private float spawnInterval = 6f;
        [SerializeField] private float spawnRadius = 4f;
        [SerializeField] private int maxActive = 5;
        // Mayor que el wallMargin de los Boids: dentro de esa franja la contención les gana al Arrive y un POI ahí queda inalcanzable.
        [SerializeField] private float edgeMargin = 2f;
        [SerializeField] private ArenaBounds arenaBounds;

        private float _timer;

        public void Tick(float deltaTime)
        {
            _timer += deltaTime;
            if (_timer < spawnInterval || POIManager.Count >= maxActive)
                return;

            _timer = 0f;
            Instantiate(poiPrefab, GetSpawnPoint(), Quaternion.identity);
        }

        private Vector3 GetSpawnPoint()
        {
            Vector3 offset = new Vector3(Random.Range(-spawnRadius, spawnRadius), 0f, Random.Range(-spawnRadius, spawnRadius));
            Vector3 spawnPoint = arenaBounds.ClampToArena(transform.position + offset, edgeMargin);
            spawnPoint.y = arenaBounds.transform.position.y;
            return spawnPoint;
        }
    }
}
