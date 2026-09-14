using System.Collections.Generic;
using UnityEngine;
using Boids;

namespace Steering
{
    public static class SteeringBehaviours
    {
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
            if (distance < 0.0001f)
                return -velocity;

            float speed = distance < slowingRadius ? maxSpeed * (distance / slowingRadius) : maxSpeed;
            Vector3 desired = toTarget.normalized * speed;
            return desired - velocity;
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

        public static Vector3 Separation(Vector3 position, IReadOnlyList<BoidAgent> neighbors, float separationRadius)
        {
            Vector3 force = Vector3.zero;
            int count = 0;

            for (int i = 0; i < neighbors.Count; i++)
            {
                BoidAgent other = neighbors[i];
                Vector3 offset = position - other.transform.position;
                float distance = offset.magnitude;
                if (distance > 0.0001f && distance < separationRadius)
                {
                    force += offset.normalized / distance;
                    count++;
                }
            }

            return count > 0 ? force / count : Vector3.zero;
        }

        public static Vector3 Alignment(Vector3 velocity, IReadOnlyList<BoidAgent> neighbors)
        {
            if (neighbors.Count == 0)
                return Vector3.zero;

            Vector3 averageVelocity = Vector3.zero;
            for (int i = 0; i < neighbors.Count; i++)
                averageVelocity += neighbors[i].Velocity;

            averageVelocity /= neighbors.Count;
            return averageVelocity - velocity;
        }

        public static Vector3 Cohesion(Vector3 position, IReadOnlyList<BoidAgent> neighbors)
        {
            if (neighbors.Count == 0)
                return Vector3.zero;

            Vector3 centerOfMass = Vector3.zero;
            for (int i = 0; i < neighbors.Count; i++)
                centerOfMass += neighbors[i].transform.position;

            centerOfMass /= neighbors.Count;
            return (centerOfMass - position).normalized;
        }
    }
}
