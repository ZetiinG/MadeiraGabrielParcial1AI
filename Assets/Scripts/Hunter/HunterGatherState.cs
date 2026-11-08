using Boids;
using FSM;

namespace Hunter
{
    public class HunterGatherState : State<HunterController, HunterStateId>
    {
        private float _progress;

        public HunterGatherState(HunterController owner, StateMachine<HunterStateId> machine) : base(owner, machine) { }

        public override void Enter()
        {
            _progress = 0f;
            Owner.Path.Clear();
        }

        public override void Tick(float deltaTime)
        {
            BoidAgent target = Owner.Target;
            if (target == null || !target.IsCollectable)
            {
                TransitionTo(HunterStateId.Search);
                return;
            }

            HunterGathering gathering = Owner.Gathering;
            Owner.Motor.Arrive(target.transform.position, gathering.SlowingRadius);

            if (Owner.Perception.DistanceTo(target) > gathering.InteractRadius)
                return;

            Owner.Motor.Stop();
            _progress += deltaTime;
            if (_progress < gathering.GatherDuration)
                return;

            target.Health.Gather();
            gathering.Add();
            TransitionTo(gathering.HasRequiredGathers ? HunterStateId.Exit : HunterStateId.Search);
        }

        public override void Exit() => _progress = 0f;
    }
}
