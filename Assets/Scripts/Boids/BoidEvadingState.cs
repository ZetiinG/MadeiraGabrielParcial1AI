using UnityEngine;
using FSM;
using Steering;

namespace Boids
{
    public class BoidEvadingState : State<BoidAgent, BoidStateId>
    {
        private static readonly Vector3[] WallSlideCandidates = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

        private Vector3 _wallSlideDirection;

        public BoidEvadingState(BoidAgent owner, StateMachine<BoidStateId> machine) : base(owner, machine) { }

        public override void Enter()
        {
            _wallSlideDirection = Vector3.zero;
            Owner.Motor.MaxSpeed = Owner.EvadeSpeed;
        }

        public override void Exit() => Owner.Motor.MaxSpeed = Owner.CruiseSpeed;

        public override void Tick(float deltaTime)
        {
            if (Owner.IsSafeFromThreat)
            {
                TransitionTo(BoidStateId.Roaming);
                return;
            }

            Owner.Steer(Owner.Flocking.GetSeparationForce() + EscapeForce() * Owner.EvadeWeight);
        }

        private Vector3 EscapeForce()
        {
            Vector3 position = Owner.transform.position;
            if (TryGetWallSlide(out Vector3 slideDirection))
                return SteeringBehaviours.Seek(position, Owner.Velocity, position + slideDirection, Owner.Motor.MaxSpeed);

            return SteeringBehaviours.Evade(position, Owner.Velocity, Owner.ThreatPosition, Owner.ThreatVelocity,
                Owner.Motor.MaxSpeed, Owner.EvadePredictionTime);
        }

        // Huir en línea recta contra una pared se cancela con la contención y el Boid queda quieto: en su lugar corre a lo largo de ella.
        // La dirección se mantiene hasta que deja de servir (llega a una esquina) para no oscilar entre los dos lados.
        private bool TryGetWallSlide(out Vector3 slideDirection)
        {
            slideDirection = Vector3.zero;
            Vector3 wallsInward = Owner.GetNearbyWallsInward();
            if (wallsInward == Vector3.zero)
            {
                _wallSlideDirection = Vector3.zero;
                return false;
            }

            Vector3 away = AwayFromThreat();
            if (!PointsIntoWall(away, wallsInward))
                return false;

            if (_wallSlideDirection == Vector3.zero || PointsIntoWall(_wallSlideDirection, wallsInward))
                _wallSlideDirection = ChooseWallSlide(away, wallsInward);

            slideDirection = _wallSlideDirection;
            return true;
        }

        private Vector3 AwayFromThreat()
        {
            Vector3 predictedThreat = Owner.ThreatPosition + Owner.ThreatVelocity * Owner.EvadePredictionTime;
            Vector3 away = Owner.transform.position - predictedThreat;
            away.y = 0f;
            return away.normalized;
        }

        private static bool PointsIntoWall(Vector3 direction, Vector3 wallsInward)
        {
            return direction.x * wallsInward.x < 0f || direction.z * wallsInward.z < 0f;
        }

        // En una esquina sólo quedan las direcciones hacia adentro: gana la que menos se acerca al Cazador, o sea, escapa por su costado.
        private static Vector3 ChooseWallSlide(Vector3 away, Vector3 wallsInward)
        {
            Vector3 best = Vector3.zero;
            float bestAlignment = float.NegativeInfinity;
            foreach (Vector3 candidate in WallSlideCandidates)
            {
                if (PointsIntoWall(candidate, wallsInward))
                    continue;

                float alignment = Vector3.Dot(candidate, away);
                if (alignment > bestAlignment)
                {
                    best = candidate;
                    bestAlignment = alignment;
                }
            }

            return best;
        }
    }
}
