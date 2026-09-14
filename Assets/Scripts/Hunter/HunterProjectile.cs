using UnityEngine;
using Boids;

namespace Hunter
{
    public class HunterProjectile : MonoBehaviour
    {
        private BoidAgent _target;
        private float _speed;
        private float _damage;

        public void Initialize(BoidAgent target, float speed, float damage)
        {
            _target = target;
            _speed = speed;
            _damage = damage;
        }

        private void Update()
        {
            if (_target == null || _target.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 targetPosition = _target.transform.position;
            float step = _speed * Time.deltaTime;

            if (Vector3.Distance(transform.position, targetPosition) <= step)
            {
                _target.TakeDamage(_damage);
                Destroy(gameObject);
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
        }
    }
}
