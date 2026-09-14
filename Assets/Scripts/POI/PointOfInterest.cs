using UnityEngine;

namespace POI
{
    public class PointOfInterest : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 30f;

        private float _currentHealth;

        private void Awake()
        {
            _currentHealth = maxHealth;
            POIManager.Register(this);
        }

        public void TakeDamage(float amount)
        {
            _currentHealth -= amount;
            if (_currentHealth <= 0f)
                Destroy(gameObject);
        }

        private void OnDestroy() => POIManager.Unregister(this);
    }
}
