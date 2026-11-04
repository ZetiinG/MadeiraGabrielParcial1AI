using UnityEngine;
using FSM;
using Navigation;

namespace Wolves
{
    public class WolfPatrolState : State<WolfController, WolfStateId>
    {
        public WolfPatrolState(WolfController owner, StateMachine<WolfStateId> machine) : base(owner, machine) { }

        public override void Enter()
        {
            Owner.Motor.MaxSpeed = Owner.PatrolSpeed;
            Owner.Awareness.ForgetHunter();
            Owner.Path.Clear();
        }

        public override void Tick(float deltaTime)
        {
            if (Owner.Awareness.ConsumeAlert())
            {
                TransitionTo(WolfStateId.Investigate);
                return;
            }

            MoveToWaypoint();
        }

        private void MoveToWaypoint()
        {
            WaypointRoute route = Owner.Route;
            Transform waypoint = route.Current;
            if (waypoint == null)
                return;

            if (route.HasReached(Owner.transform.position))
            {
                route.Advance();
                Owner.Path.Clear();
                return;
            }

            if (Owner.Path.HasPath)
                Owner.Path.Tick();
            else if (Owner.HasClearPathTo(waypoint.position))
                Owner.Motor.Seek(waypoint.position);
            else
                Owner.Path.SetDestination(waypoint.position);
        }
    }
}
