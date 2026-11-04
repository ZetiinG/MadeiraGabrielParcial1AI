using System.Collections.Generic;
using UnityEngine;
using Core;

namespace Boids
{
    public class BoidFlocking : MonoBehaviour
    {
        private const float MinSeparationDistance = 0.0001f;

        [Header("Radii")]
        [SerializeField] private float separationRadius = 0.9f;
        [SerializeField] private float neighborRadius = 4.5f;
        [SerializeField] private BoidSensor neighborSensor;

        [Header("Weights")]
        [SerializeField] private float separationWeight = 1.6f;
        [SerializeField] private float alignmentWeight = 1f;
        [SerializeField] private float cohesionWeight = 1f;

        private readonly List<BoidAgent> _separationNeighbors = new List<BoidAgent>();
        private readonly List<BoidAgent> _flockNeighbors = new List<BoidAgent>();
        private BoidAgent _self;

        private void Awake()
        {
            _self = GetComponent<BoidAgent>();
        }

        private void Start()
        {
            neighborSensor.SetWorldRadius(neighborRadius);
        }

        public Vector3 GetFlockingForce()
        {
            RefreshNeighbors();
            return Separation() * separationWeight
                + Alignment() * alignmentWeight
                + Cohesion() * cohesionWeight;
        }

        public Vector3 GetSeparationForce()
        {
            RefreshNeighbors();
            return Separation() * separationWeight;
        }

        private void RefreshNeighbors()
        {
            _separationNeighbors.Clear();
            _flockNeighbors.Clear();

            IReadOnlyList<BoidAgent> detected = neighborSensor.Detected;
            for (int i = 0; i < detected.Count; i++)
            {
                BoidAgent other = detected[i];
                if (other == _self || other.IsDead)
                    continue;

                _flockNeighbors.Add(other);
                if (PlanarMath.Distance(transform.position, other.transform.position) <= separationRadius)
                    _separationNeighbors.Add(other);
            }
        }

        // En unidades de velocidad como las demás fuerzas. Se promedia: sumado, varios vecinos expulsan al Boid del borde fuera del radio de cohesión.
        private Vector3 Separation()
        {
            if (_separationNeighbors.Count == 0)
                return Vector3.zero;

            Vector3 push = Vector3.zero;
            for (int i = 0; i < _separationNeighbors.Count; i++)
            {
                Vector3 offset = transform.position - _separationNeighbors[i].transform.position;
                offset.y = 0f;
                float distance = offset.magnitude;
                Vector3 away = distance > MinSeparationDistance ? offset / distance : RandomPlanarDirection();
                push += away * (1f - distance / separationRadius);
            }

            return push / _separationNeighbors.Count * _self.Motor.MaxSpeed;
        }

        // Dos Boids en el mismo punto (ej. aplastados en una esquina por el clamp) no tienen dirección de escape; cada uno sortea la suya para despegarse.
        private static Vector3 RandomPlanarDirection()
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            return new Vector3(direction.x, 0f, direction.y);
        }

        private Vector3 Alignment()
        {
            if (_flockNeighbors.Count == 0)
                return Vector3.zero;

            Vector3 averageVelocity = Vector3.zero;
            for (int i = 0; i < _flockNeighbors.Count; i++)
                averageVelocity += _flockNeighbors[i].Velocity;

            averageVelocity /= _flockNeighbors.Count;
            return averageVelocity - _self.Velocity;
        }

        private Vector3 Cohesion()
        {
            if (_flockNeighbors.Count == 0)
                return Vector3.zero;

            Vector3 centerOfMass = Vector3.zero;
            for (int i = 0; i < _flockNeighbors.Count; i++)
                centerOfMass += _flockNeighbors[i].transform.position;

            centerOfMass /= _flockNeighbors.Count;
            return (centerOfMass - transform.position).normalized;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, separationRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, neighborRadius);
        }
    }
}
