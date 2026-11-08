using System;

namespace FSM
{
    public abstract class State<TOwner, TStateId> : IState where TStateId : struct, Enum
    {
        private readonly StateMachine<TStateId> _machine;

        protected TOwner Owner { get; }

        protected State(TOwner owner, StateMachine<TStateId> machine)
        {
            Owner = owner;
            _machine = machine;
        }

        public virtual void Enter() { }
        public abstract void Tick(float deltaTime);
        public virtual void Exit() { }

        protected void TransitionTo(TStateId stateId) => _machine.TransitionTo(stateId);
    }
}
