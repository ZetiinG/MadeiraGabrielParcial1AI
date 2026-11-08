using UnityEngine;
using Boids;
using Core;
using Steering;

namespace Hunter
{
    [RequireComponent(typeof(SteeringMotor))]
    public class HunterCombat : MonoBehaviour
    {
        [SerializeField] private float attackCooldown = 3f;
        [SerializeField] private float rangeAttackRadius = 5f;
        [SerializeField] private float meleeAttackRadius = 1.5f;
        [SerializeField] private float meleeDamage = 10f;
        [SerializeField] private float rangedDamage = 10f;
        [SerializeField] private float projectileSpeed = 25f;
        [SerializeField] private HunterProjectile projectilePrefab;
        [SerializeField] private float pursuitPredictionTime = 0.5f;
        [SerializeField] private float stalkDistance = 3f;
        [SerializeField] private float stalkSlowingRadius = 2f;

        private SteeringMotor _motor;
        private float _lastAttackTime = float.NegativeInfinity;

        public bool AttackReady => Time.time >= _lastAttackTime + attackCooldown;

        private void Awake()
        {
            _motor = GetComponent<SteeringMotor>();
        }

        public bool TryAttack(BoidAgent target)
        {
            float distance = PlanarMath.Distance(transform.position, target.transform.position);

            if (distance <= meleeAttackRadius)
                target.Health.TakeDamage(meleeDamage);
            else if (distance <= rangeAttackRadius)
                FireProjectile(target);
            else
                return false;

            _lastAttackTime = Time.time;
            return true;
        }

        public void Pursue(BoidAgent target)
        {
            _motor.Pursue(target.transform.position, target.Velocity, pursuitPredictionTime);
        }

        // Se mantiene a distancia de disparo: perseguirlo hasta el contacto le tapa la huida con el cuerpo y lo termina empujando.
        public void Stalk(BoidAgent target)
        {
            Vector3 fromTarget = transform.position - target.transform.position;
            fromTarget.y = 0f;

            Vector3 stalkPoint = target.transform.position + fromTarget.normalized * stalkDistance;
            stalkPoint.y = transform.position.y;
            _motor.Arrive(stalkPoint, stalkSlowingRadius);
        }

        private void FireProjectile(BoidAgent target)
        {
            Vector3 spawnPoint = new Vector3(transform.position.x, target.transform.position.y, transform.position.z);
            HunterProjectile projectile = Instantiate(projectilePrefab, spawnPoint, Quaternion.identity);
            projectile.Initialize(target.Health, projectileSpeed, rangedDamage);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);
        }
    }
}
