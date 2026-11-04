using System;
using System.Collections.Generic;

namespace FSM
{
    public class StateMachine<TStateId> where TStateId : struct, Enum
    {
        private readonly struct AnyStateTransition
        {
            public readonly TStateId Target;
            public readonly Func<bool> Condition;

            public AnyStateTransition(TStateId target, Func<bool> condition)
            {
                Target = target;
                Condition = condition;
            }
        }

        private static readonly EqualityComparer<TStateId> IdComparer = EqualityComparer<TStateId>.Default;

        private readonly Dictionary<TStateId, IState> _states = new Dictionary<TStateId, IState>();
        private readonly List<AnyStateTransition> _anyStateTransitions = new List<AnyStateTransition>();
        private IState _current;

        public TStateId CurrentStateId { get; private set; }

        public event Action<TStateId> StateChanged;

        public void AddState(TStateId stateId, IState state) => _states.Add(stateId, state);

        // Se evalúan en el orden en que se agregan, así el orden define la prioridad.
        public void AddAnyStateTransition(TStateId target, Func<bool> condition)
        {
            _anyStateTransitions.Add(new AnyStateTransition(target, condition));
        }

        public void Start(TStateId initialState) => ChangeState(initialState);

        public void Tick(float deltaTime)
        {
            if (TryAnyStateTransition())
                return;

            _current.Tick(deltaTime);
        }

        public void TransitionTo(TStateId stateId)
        {
            _current.Exit();
            ChangeState(stateId);
        }

        private bool TryAnyStateTransition()
        {
            foreach (AnyStateTransition transition in _anyStateTransitions)
            {
                if (IdComparer.Equals(transition.Target, CurrentStateId) || !transition.Condition())
                    continue;

                TransitionTo(transition.Target);
                return true;
            }

            return false;
        }

        private void ChangeState(TStateId stateId)
        {
            CurrentStateId = stateId;
            _current = _states[stateId];
            _current.Enter();
            StateChanged?.Invoke(stateId);
        }
    }
}
