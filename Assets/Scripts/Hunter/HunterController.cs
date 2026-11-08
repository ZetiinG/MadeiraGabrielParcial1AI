using System;
using UnityEngine;
using Boids;
using FSM;
using Navigation;
using POI;
using Steering;

namespace Hunter
{
    [RequireComponent(typeof(SteeringMotor), typeof(PathFollower), typeof(HunterPerception))]
    [RequireComponent(typeof(HunterCombat), typeof(HunterGathering), typeof(HunterNavigator))]
    [RequireComponent(typeof(HunterLife), typeof(POISpawner))]
    public class HunterController : MonoBehaviour
    {
        private StateMachine<HunterStateId> _stateMachine;

        public SteeringMotor Motor { get; private set; }
        public PathFollower Path { get; private set; }
        public HunterPerception Perception { get; private set; }
        public HunterCombat Combat { get; private set; }
        public HunterGathering Gathering { get; private set; }
        public HunterNavigator Navigator { get; private set; }
        public HunterLife Life { get; private set; }
        public POISpawner PoiSpawner { get; private set; }

        public BoidAgent Target { get; set; }
        public HunterStateId CurrentState => _stateMachine.CurrentStateId;

        public event Action<HunterStateId> StateChanged;

        public Vector3 Velocity => Motor.Velocity;
        public bool IsAlive => Life.IsAlive;
        public bool IsInSafeZone => Navigator.IsInSafeZone;

        private void Awake()
        {
            Motor = GetComponent<SteeringMotor>();
            Path = GetComponent<PathFollower>();
            Perception = GetComponent<HunterPerception>();
            Combat = GetComponent<HunterCombat>();
            Gathering = GetComponent<HunterGathering>();
            Navigator = GetComponent<HunterNavigator>();
            Life = GetComponent<HunterLife>();
            PoiSpawner = GetComponent<POISpawner>();
            BuildStateMachine();
        }

        private void Start() => _stateMachine.Start(HunterStateId.Search);

        private void Update()
        {
            if (Life.IsAlive)
                Perception.Refresh();

            _stateMachine.Tick(Time.deltaTime);
        }

        private void BuildStateMachine()
        {
            _stateMachine = new StateMachine<HunterStateId>();
            _stateMachine.AddState(HunterStateId.Search, new HunterSearchState(this, _stateMachine));
            _stateMachine.AddState(HunterStateId.Attack, new HunterAttackState(this, _stateMachine));
            _stateMachine.AddState(HunterStateId.Gather, new HunterGatherState(this, _stateMachine));
            _stateMachine.AddState(HunterStateId.Flee, new HunterFleeState(this, _stateMachine));
            _stateMachine.AddState(HunterStateId.Exit, new HunterExitState(this, _stateMachine));
            _stateMachine.AddState(HunterStateId.Dead, new HunterDeadState(this, _stateMachine));

            // Dead y Flee son las de mayor prioridad y se alcanzan desde cualquier estado.
            _stateMachine.AddAnyStateTransition(HunterStateId.Dead, () => Life.WasCaught);
            _stateMachine.AddAnyStateTransition(HunterStateId.Flee, () => Life.IsAlive && Perception.VisibleWolf != null);

            _stateMachine.StateChanged += stateId => StateChanged?.Invoke(stateId);
        }

        public void Despawn()
        {
            Life.Despawn();
            Target = null;
            Perception.Clear();
            Motor.Stop();
            Path.Clear();
        }

        public void RestartRun()
        {
            Life.Respawn();
            Gathering.ResetCount();
            Motor.Stop();
            Path.Clear();
        }
    }
}
