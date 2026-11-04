using FSM;

namespace Hunter
{
    public class HunterExitState : State<HunterController, HunterStateId>
    {
        public HunterExitState(HunterController owner, StateMachine<HunterStateId> machine) : base(owner, machine) { }

        public override void Enter()
        {
            Owner.Target = null;
            Owner.Path.Clear();
        }

        public override void Tick(float deltaTime)
        {
            if (Owner.Navigator.IsInExitZone)
            {
                Owner.RestartRun();
                TransitionTo(HunterStateId.Search);
                return;
            }

            if (!Owner.Path.HasPath)
                Owner.Path.SetDestination(Owner.Navigator.ExitPoint);

            Owner.Path.Tick();
        }
    }
}
