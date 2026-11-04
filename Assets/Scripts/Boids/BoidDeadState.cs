using FSM;

namespace Boids
{
    public class BoidDeadState : State<BoidAgent, BoidStateId>
    {
        public BoidDeadState(BoidAgent owner, StateMachine<BoidStateId> machine) : base(owner, machine) { }

        public override void Enter()
        {
            Owner.Motor.Stop();
            Owner.TargetPOI = null;
        }

        public override void Tick(float deltaTime)
        {
            if (!Owner.IsDead)
                TransitionTo(BoidStateId.Roaming);
        }
    }
}
