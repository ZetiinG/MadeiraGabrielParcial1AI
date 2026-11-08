using UnityEngine;

namespace Steering
{
    public static class SteeringBehaviours
    {
        private const float ArrivedDistance = 0.0001f;

        public static Vector3 Seek(Vector3 position, Vector3 velocity, Vector3 targetPosition, float maxSpeed)
        {
            Vector3 desired = (targetPosition - position).normalized * maxSpeed;
            return desired - velocity;
        }

        public static Vector3 Flee(Vector3 position, Vector3 velocity, Vector3 threatPosition, float maxSpeed)
        {
            Vector3 desired = (position - threatPosition).normalized * maxSpeed;
            return desired - velocity;
        }

        public static Vector3 Arrive(Vector3 position, Vector3 velocity, Vector3 targetPosition, float maxSpeed, float slowingRadius)
        {
            Vector3 toTarget = targetPosition - position;
            float distance = toTarget.magnitude;
            if (distance < ArrivedDistance)
                return -velocity;

            float speed = distance < slowingRadius ? maxSpeed * (distance / slowingRadius) : maxSpeed;
            Vector3 desired = toTarget.normalized * speed;
            return desired - velocity;
        }

        // Reynolds: persigue un punto que se mueve sobre un círculo proyectado delante del agente, así el rumbo cambia de forma suave.
        public static Vector3 Wander(Vector3 position, Vector3 velocity, Vector3 forward, float wanderAngle, float circleDistance, float circleRadius, float maxSpeed)
        {
            Vector3 circleCenter = position + forward * circleDistance;
            Vector3 offset = Quaternion.AngleAxis(wanderAngle, Vector3.up) * Vector3.forward * circleRadius;
            return Seek(position, velocity, circleCenter + offset, maxSpeed);
        }

        public static Vector3 Pursue(Vector3 position, Vector3 velocity, Vector3 targetPosition, Vector3 targetVelocity, float maxSpeed, float predictionTime)
        {
            Vector3 predictedPosition = targetPosition + targetVelocity * predictionTime;
            return Seek(position, velocity, predictedPosition, maxSpeed);
        }

        public static Vector3 Evade(Vector3 position, Vector3 velocity, Vector3 threatPosition, Vector3 threatVelocity, float maxSpeed, float predictionTime)
        {
            Vector3 predictedPosition = threatPosition + threatVelocity * predictionTime;
            return Flee(position, velocity, predictedPosition, maxSpeed);
        }
    }
}
