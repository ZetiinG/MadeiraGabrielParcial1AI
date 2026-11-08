using UnityEngine;

namespace Steering
{
    public class SteeringMotor : MonoBehaviour
    {
        private const float MinSqrSpeed = 0.0001f;
        private const float SkinWidth = 0.05f;
        private const int MaxSlideIterations = 2;

        [SerializeField] private float maxSpeed = 5f;
        [SerializeField] private float maxForce = 10f;
        // Reynolds: aceleración = fuerza / masa. Con masa baja el agente corrige el rumbo casi al instante en vez de irse de largo.
        [SerializeField] private float mass = 1f;

        [Header("Obstacles")]
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private float bodyRadius = 0.5f;

        private Vector3 _velocity;

        public Vector3 Velocity => _velocity;

        public float MaxSpeed
        {
            get => maxSpeed;
            set => maxSpeed = value;
        }

        public void Seek(Vector3 point)
        {
            ApplyForce(SteeringBehaviours.Seek(transform.position, _velocity, point, maxSpeed));
        }

        public void Arrive(Vector3 point, float slowingRadius)
        {
            ApplyForce(SteeringBehaviours.Arrive(transform.position, _velocity, point, maxSpeed, slowingRadius));
        }

        public void Pursue(Vector3 targetPosition, Vector3 targetVelocity, float predictionTime)
        {
            ApplyForce(SteeringBehaviours.Pursue(transform.position, _velocity, targetPosition, targetVelocity, maxSpeed, predictionTime));
        }

        public void ApplyForce(Vector3 force)
        {
            Steer(force);
            Move();
        }

        public void Stop() => _velocity = Vector3.zero;

        private void Steer(Vector3 force)
        {
            force.y = 0f;
            force = Vector3.ClampMagnitude(force, maxForce);
            _velocity = Vector3.ClampMagnitude(_velocity + force / mass * Time.deltaTime, maxSpeed);
            _velocity.y = 0f;
        }

        private void Move()
        {
            if (_velocity.sqrMagnitude < MinSqrSpeed)
                return;

            Vector3 displacement = _velocity * Time.deltaTime;
            if (obstacleMask.value != 0)
                displacement = SlideAlongObstacles(displacement);

            transform.position += displacement;
            if (_velocity.sqrMagnitude >= MinSqrSpeed)
                transform.forward = _velocity.normalized;
        }

        // Sin Rigidbody nada frena al agente contra las paredes: las fuerzas de steering tienen inercia y cortan esquinas o empujan a través de ellas.
        private Vector3 SlideAlongObstacles(Vector3 displacement)
        {
            Vector3 moved = Vector3.zero;

            // Con autoSyncTransforms apagado el cast ve a los demás agentes donde estaban en el último paso de física:
            // se llegan a solapar y SphereCast ignora los colliders que ya arranca tocando, así que se atravesarían.
            Physics.SyncTransforms();

            for (int i = 0; i < MaxSlideIterations; i++)
            {
                float distance = displacement.magnitude;
                if (distance < Mathf.Epsilon)
                    break;

                Vector3 direction = displacement / distance;
                if (!Physics.SphereCast(transform.position + moved, bodyRadius, direction, out RaycastHit hit, distance + SkinWidth, obstacleMask, QueryTriggerInteraction.Ignore))
                    return moved + displacement;

                Vector3 normal = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
                Vector3 allowed = direction * Mathf.Max(0f, hit.distance - SkinWidth);
                moved += allowed;
                displacement = Vector3.ProjectOnPlane(displacement - allowed, normal);
                _velocity = Vector3.ProjectOnPlane(_velocity, normal);
            }

            return moved;
        }
    }
}
