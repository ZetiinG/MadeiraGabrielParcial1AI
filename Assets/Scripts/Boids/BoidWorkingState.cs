using FSM;

namespace Boids
{
    public class BoidWorkingState : State<BoidAgent, BoidStateId>
    {
        private float _workTimer;

        public BoidWorkingState(BoidAgent owner, StateMachine<BoidStateId> machine) : base(owner, machine) { }

        public override void Enter()
        {
            _workTimer = 0f;
            Owner.Motor.Stop();
        }

        public override void Tick(float deltaTime)
        {
            if (Owner.TargetPOI == null)
            {
                TransitionTo(BoidStateId.Roaming);
                return;
            }

            // Sigue anclado al POI pero con separación, si no los que llegan después se apilan sobre los que ya están trabajando.
            Owner.SteerTowardsPOI();

            _workTimer += deltaTime;
            if (_workTimer < Owner.WorkTickInterval)
                return;

            _workTimer = 0f;
            Owner.TargetPOI.TakeDamage(Owner.WorkDamagePerTick);
        }
    }
}
