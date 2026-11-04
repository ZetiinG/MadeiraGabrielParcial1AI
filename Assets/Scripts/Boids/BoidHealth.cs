using System;
using System.Collections;
using UnityEngine;
using Core;

namespace Boids
{
    public class BoidHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 20f;
        [SerializeField] private float respawnDelay = 4f;
        [SerializeField] private ArenaBounds arenaBounds;

        private float _currentHealth;
        private Collider _collider;

        public bool IsDead { get; private set; }
        public bool IsCollectable => IsDead && _collider.enabled;
        public float NormalizedHealth => _currentHealth / maxHealth;

        public event Action Damaged;
        public event Action Died;
        public event Action Gathered;
        public event Action Respawned;

        private void Awake()
        {
            _currentHealth = maxHealth;
            _collider = GetComponent<Collider>();
        }

        public void TakeDamage(float amount)
        {
            if (IsDead)
                return;

            _currentHealth -= amount;
            if (_currentHealth > 0f)
            {
                Damaged?.Invoke();
                return;
            }

            IsDead = true;
            Died?.Invoke();
        }

        public void Gather()
        {
            _collider.enabled = false;
            Gathered?.Invoke();
            StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);

            Vector3 respawnPoint = arenaBounds.GetRandomPoint();
            respawnPoint.y = transform.position.y;
            transform.position = respawnPoint;

            _currentHealth = maxHealth;
            IsDead = false;
            _collider.enabled = true;
            Respawned?.Invoke();
        }
    }
}
