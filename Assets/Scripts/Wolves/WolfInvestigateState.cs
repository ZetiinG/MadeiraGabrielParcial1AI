using FSM;

namespace Wolves
{
    public class WolfInvestigateState : State<WolfController, WolfStateId>
    {
        public WolfInvestigateState(WolfController owner, StateMachine<WolfStateId> machine) : base(owner, machine) { }

        public override void Enter()
        {
            Owner.Motor.MaxSpeed = Owner.ChaseSpeed;
            Owner.Path.SetDestination(Owner.Awareness.LastKnownPosition);
        }

        public override void Tick(float deltaTime)
        {
            if (Owner.Awareness.ConsumeAlert())
                Owner.Path.SetDestination(Owner.Awareness.LastKnownPosition);

            if (!Owner.Path.HasPath)
            {
                TransitionTo(WolfStateId.Search);
                return;
            }

            Owner.Path.Tick();
        }
    }
}
