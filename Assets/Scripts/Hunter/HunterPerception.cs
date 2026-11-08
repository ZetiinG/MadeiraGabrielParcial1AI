using System;
using System.Collections.Generic;
using UnityEngine;
using Boids;
using Core;
using Perception;
using Wolves;

namespace Hunter
{
    [RequireComponent(typeof(FieldOfView))]
    public class HunterPerception : MonoBehaviour
    {
        [SerializeField] private float boidDetectionRange = 10f;
        [SerializeField] private BoidSensor boidSensor;

        private FieldOfView _fieldOfView;

        public WolfController VisibleWolf { get; private set; }

        private void Awake()
        {
            _fieldOfView = GetComponent<FieldOfView>();
        }

        private void Start()
        {
            boidSensor.SetWorldRadius(boidDetectionRange);
        }

        public void Refresh() => VisibleWolf = FindVisibleWolf();

        public void Clear() => VisibleWolf = null;

        public BoidAgent FindLivingBoid() => FindClosestVisibleBoid(boid => !boid.IsDead);

        public BoidAgent FindCollectableBoid() => FindClosestVisibleBoid(boid => boid.IsCollectable);

        public bool CanSee(BoidAgent boid)
        {
            return DistanceTo(boid) <= boidDetectionRange && _fieldOfView.HasLineOfSight(boid.transform.position);
        }

        public float DistanceTo(Component target) => PlanarMath.Distance(transform.position, target.transform.position);

        private BoidAgent FindClosestVisibleBoid(Predicate<BoidAgent> filter)
        {
            BoidAgent closest = null;
            float closestDistance = float.MaxValue;

            IReadOnlyList<BoidAgent> detected = boidSensor.Detected;
            for (int i = 0; i < detected.Count; i++)
            {
                BoidAgent boid = detected[i];
                if (!filter(boid) || !CanSee(boid))
                    continue;

                float distance = DistanceTo(boid);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = boid;
                }
            }

            return closest;
        }

        private WolfController FindVisibleWolf()
        {
            WolfController closest = null;
            float closestDistance = float.MaxValue;

            IReadOnlyList<WolfController> wolves = WolfPack.Members;
            for (int i = 0; i < wolves.Count; i++)
            {
                float distance = DistanceTo(wolves[i]);
                if (distance < closestDistance && _fieldOfView.CanSee(wolves[i].transform.position))
                {
                    closestDistance = distance;
                    closest = wolves[i];
                }
            }

            return closest;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, boidDetectionRange);
        }
    }
}
