using FSM;

namespace Hunter
{
    public class HunterFleeState : State<HunterController, HunterStateId>
    {
        private float _waitTimer;

        public HunterFleeState(HunterController owner, StateMachine<HunterStateId> machine) : base(owner, machine) { }

        public override void Enter()
        {
            _waitTimer = 0f;
            Owner.Target = null;
            Owner.Path.Clear();
        }

        public override void Tick(float deltaTime)
        {
            HunterNavigator navigator = Owner.Navigator;
            if (!navigator.IsInSafeZone && !Owner.Path.HasPath)
                TryPathToClosestSafeZone();

            Owner.Path.Tick();

            if (!navigator.IsInSafeZone)
                return;

            _waitTimer += deltaTime;
            if (_waitTimer < navigator.SafeZoneWaitTime)
                return;

            TransitionTo(Owner.Gathering.HasRequiredGathers ? HunterStateId.Exit : HunterStateId.Search);
        }

        public override void Exit() => Owner.Navigator.ClearSafeZoneChoice();

        // Se llama desde Tick y no desde Enter: si ninguna zona tiene camino, se reintenta en el próximo frame en vez de quedar sin destino.
        private void TryPathToClosestSafeZone()
        {
            HunterNavigator navigator = Owner.Navigator;
            navigator.ChooseClosestSafeZone();
            if (navigator.ChosenSafeZone != null)
                Owner.Path.SetDestination(navigator.ChosenSafeZone.Center);
        }
    }
}
