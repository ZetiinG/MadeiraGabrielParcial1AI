using Boids;
using FSM;

namespace Hunter
{
    public class HunterAttackState : State<HunterController, HunterStateId>
    {
        public HunterAttackState(HunterController owner, StateMachine<HunterStateId> machine) : base(owner, machine) { }

        public override void Enter() => Owner.Path.Clear();

        public override void Tick(float deltaTime)
        {
            BoidAgent target = Owner.Target;
            if (target == null || target.IsDead || !Owner.Perception.CanSee(target))
            {
                TransitionTo(HunterStateId.Search);
                return;
            }

            if (Owner.Combat.TryAttack(target))
            {
                TransitionTo(HunterStateId.Search);
                return;
            }

            Owner.Combat.Pursue(target);
        }
    }
}
