using System;
using UnityEngine;
using Core;
using FSM;
using Hunter;
using Navigation;
using Steering;

namespace Wolves
{
    [RequireComponent(typeof(SteeringMotor), typeof(PathFollower), typeof(WolfAwareness))]
    [RequireComponent(typeof(WaypointRoute))]
    public class WolfController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float patrolSpeed = 2.5f;
        [SerializeField] private float chaseSpeed = 4f;
        [SerializeField] private float pursuitPredictionTime = 0.5f;

        [Header("Hunt")]
        [SerializeField] private float captureRadius = 1.2f;
        [SerializeField] private float searchDuration = 6f;
        [SerializeField] private float searchRadius = 8f;

        private StateMachine<WolfStateId> _stateMachine;

        public SteeringMotor Motor { get; private set; }
        public PathFollower Path { get; private set; }
        public WolfAwareness Awareness { get; private set; }
        public WaypointRoute Route { get; private set; }

        public float PatrolSpeed => patrolSpeed;
        public float ChaseSpeed => chaseSpeed;
        public float SearchDuration => searchDuration;
        public float SearchRadius => searchRadius;
        public WolfStateId CurrentState => _stateMachine.CurrentStateId;

        public event Action<WolfStateId> StateChanged;

        private void Awake()
        {
            Motor = GetComponent<SteeringMotor>();
            Path = GetComponent<PathFollower>();
            Awareness = GetComponent<WolfAwareness>();
            Route = GetComponent<WaypointRoute>();
            BuildStateMachine();
        }

        private void OnEnable() => WolfPack.Register(this);
        private void OnDisable() => WolfPack.Unregister(this);

        private void Start() => _stateMachine.Start(WolfStateId.Patrol);

        private void Update() => _stateMachine.Tick(Time.deltaTime);

        private void BuildStateMachine()
        {
            _stateMachine = new StateMachine<WolfStateId>();
            _stateMachine.AddState(WolfStateId.Patrol, new WolfPatrolState(this, _stateMachine));
            _stateMachine.AddState(WolfStateId.Chase, new WolfChaseState(this, _stateMachine));
            _stateMachine.AddState(WolfStateId.Investigate, new WolfInvestigateState(this, _stateMachine));
            _stateMachine.AddState(WolfStateId.Search, new WolfSearchState(this, _stateMachine));

            // Si el Cazador llega a una zona segura la manada abandona la caza; si lo ven, lo persiguen desde cualquier estado.
            _stateMachine.AddAnyStateTransition(WolfStateId.Patrol, Awareness.IsHunterInSafeZone);
            _stateMachine.AddAnyStateTransition(WolfStateId.Chase, Awareness.CanSeeHunter);

            _stateMachine.StateChanged += stateId => StateChanged?.Invoke(stateId);
        }

        public void ChaseHunter()
        {
            HunterController hunter = Awareness.Hunter;
            Motor.Pursue(hunter.transform.position, hunter.Velocity, pursuitPredictionTime);
        }

        public bool TryCatchHunter()
        {
            HunterController hunter = Awareness.Hunter;
            if (PlanarMath.Distance(transform.position, hunter.transform.position) > captureRadius)
                return false;

            hunter.Life.NotifyCaught();
            return true;
        }

        public bool HasClearPathTo(Vector3 point) => Path.Graph.HasClearPath(transform.position, point);

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, captureRadius);
        }
    }
}
