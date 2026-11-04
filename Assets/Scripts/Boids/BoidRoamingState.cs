using UnityEngine;
using FSM;
using POI;
using Steering;

namespace Boids
{
    public class BoidRoamingState : State<BoidAgent, BoidStateId>
    {
        private float _wanderAngle;

        public BoidRoamingState(BoidAgent owner, StateMachine<BoidStateId> machine) : base(owner, machine) { }

        // Sin vecinos el flocking da fuerza cero: el wander evita que un Boid aislado (ej. recién respawneado) quede congelado lejos del grupo.
        public override void Tick(float deltaTime)
        {
            Owner.Steer(Owner.Flocking.GetFlockingForce() + WanderForce(deltaTime) * Owner.WanderWeight);

            PointOfInterest poi = Owner.FindClosestSafePOI();
            if (poi == null)
                return;

            Owner.TargetPOI = poi;
            TransitionTo(BoidStateId.SeekingPOI);
        }

        private Vector3 WanderForce(float deltaTime)
        {
            _wanderAngle += Random.Range(-Owner.WanderJitter, Owner.WanderJitter) * deltaTime;
            Transform transform = Owner.transform;
            return SteeringBehaviours.Wander(transform.position, Owner.Velocity, transform.forward, _wanderAngle,
                Owner.WanderCircleDistance, Owner.WanderCircleRadius, Owner.Motor.MaxSpeed);
        }
    }
}
