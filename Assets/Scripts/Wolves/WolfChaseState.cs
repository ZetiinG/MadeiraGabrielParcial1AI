using FSM;

namespace Wolves
{
    public class WolfChaseState : State<WolfController, WolfStateId>
    {
        public WolfChaseState(WolfController owner, StateMachine<WolfStateId> machine) : base(owner, machine) { }

        public override void Enter()
        {
            Owner.Motor.MaxSpeed = Owner.ChaseSpeed;
            Owner.Path.Clear();
            Owner.Awareness.RememberHunterPosition();
            Owner.Awareness.Howl();
        }

        public override void Tick(float deltaTime)
        {
            WolfAwareness awareness = Owner.Awareness;
            if (!awareness.CanSeeHunter())
            {
                TransitionTo(WolfStateId.Investigate);
                return;
            }

            // Lo está viendo: su propia posición es más reciente que cualquier alerta de la manada.
            awareness.ConsumeAlert();
            awareness.RememberHunterPosition();

            if (Owner.TryCatchHunter())
            {
                TransitionTo(WolfStateId.Patrol);
                return;
            }

            Owner.ChaseHunter();
        }
    }
}
