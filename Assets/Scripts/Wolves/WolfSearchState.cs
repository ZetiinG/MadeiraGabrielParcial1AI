using UnityEngine;
using FSM;

namespace Wolves
{
    public class WolfSearchState : State<WolfController, WolfStateId>
    {
        private float _elapsed;

        public WolfSearchState(WolfController owner, StateMachine<WolfStateId> machine) : base(owner, machine) { }

        public override void Enter()
        {
            _elapsed = 0f;
            Owner.Motor.MaxSpeed = Owner.PatrolSpeed;
            Owner.Path.Clear();
        }

        public override void Tick(float deltaTime)
        {
            if (Owner.Awareness.ConsumeAlert())
            {
                TransitionTo(WolfStateId.Investigate);
                return;
            }

            _elapsed += deltaTime;
            if (_elapsed >= Owner.SearchDuration)
            {
                TransitionTo(WolfStateId.Patrol);
                return;
            }

            if (!Owner.Path.HasPath)
                Owner.Path.SetDestination(GetRandomSearchPoint());

            Owner.Path.Tick();
        }

        private Vector3 GetRandomSearchPoint()
        {
            return Owner.Path.GetRandomWalkablePointNear(Owner.Awareness.LastKnownPosition, Owner.SearchRadius);
        }
    }
}
