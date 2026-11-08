using FSM;

namespace Hunter
{
    public class HunterDeadState : State<HunterController, HunterStateId>
    {
        private float _elapsed;

        public HunterDeadState(HunterController owner, StateMachine<HunterStateId> machine) : base(owner, machine) { }

        public override void Enter()
        {
            _elapsed = 0f;
            Owner.Despawn();
        }

        public override void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            if (_elapsed < Owner.Life.RespawnTime)
                return;

            Owner.RestartRun();
            TransitionTo(HunterStateId.Search);
        }
    }
}
