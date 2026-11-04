using Core;
using FSM;

namespace Boids
{
    public class BoidSeekingPOIState : State<BoidAgent, BoidStateId>
    {
        public BoidSeekingPOIState(BoidAgent owner, StateMachine<BoidStateId> machine) : base(owner, machine) { }

        public override void Tick(float deltaTime)
        {
            if (Owner.TargetPOI == null)
            {
                TransitionTo(BoidStateId.Roaming);
                return;
            }

            Owner.SteerTowardsPOI();

            if (PlanarMath.Distance(Owner.transform.position, Owner.TargetPOI.transform.position) <= Owner.ArriveThreshold)
                TransitionTo(BoidStateId.Working);
        }
    }
}
