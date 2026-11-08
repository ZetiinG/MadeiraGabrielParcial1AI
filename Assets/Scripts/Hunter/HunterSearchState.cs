using Boids;
using FSM;

namespace Hunter
{
    public class HunterSearchState : State<HunterController, HunterStateId>
    {
        public HunterSearchState(HunterController owner, StateMachine<HunterStateId> machine) : base(owner, machine) { }

        public override void Enter() => Owner.Path.Clear();

        public override void Tick(float deltaTime)
        {
            if (TryStartGather())
                return;

            if (Owner.Navigator.IsInHuntingGround)
                Owner.PoiSpawner.Tick(deltaTime);

            BoidAgent livingBoid = FindLivingTarget();
            if (livingBoid == null)
                Explore();
            else if (Owner.Combat.AttackReady)
                StartAttack(livingBoid);
            else
                StalkUntilAttackReady(livingBoid);
        }

        private bool TryStartGather()
        {
            BoidAgent deadBoid = Owner.Perception.FindCollectableBoid();
            if (deadBoid == null)
                return false;

            Owner.Target = deadBoid;
            TransitionTo(HunterStateId.Gather);
            return true;
        }

        // Prioriza al Boid que ya venía atacando para no cambiar de presa después de cada disparo.
        private BoidAgent FindLivingTarget()
        {
            BoidAgent current = Owner.Target;
            if (current != null && !current.IsDead && Owner.Perception.CanSee(current))
                return current;

            return Owner.Perception.FindLivingBoid();
        }

        private void StartAttack(BoidAgent livingBoid)
        {
            Owner.Target = livingBoid;
            TransitionTo(HunterStateId.Attack);
        }

        // Mientras corre el TBA lo acecha en rango de disparo en vez de irse a patrullar, así ataca apenas termina.
        private void StalkUntilAttackReady(BoidAgent livingBoid)
        {
            Owner.Target = livingBoid;
            Owner.Path.Clear();
            Owner.Combat.Stalk(livingBoid);
        }

        private void Explore()
        {
            Owner.Target = null;

            HunterNavigator navigator = Owner.Navigator;
            if (!Owner.Path.HasPath)
                Owner.Path.SetDestination(navigator.IsInHuntingGround ? navigator.GetRandomHuntingGroundPoint() : navigator.HuntingGroundCenter);

            Owner.Path.Tick();
        }
    }
}
