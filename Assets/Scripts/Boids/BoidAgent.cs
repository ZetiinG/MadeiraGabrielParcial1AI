using System.Collections.Generic;
using UnityEngine;
using Core;
using FSM;
using Hunter;
using POI;
using Steering;

namespace Boids
{
    [RequireComponent(typeof(SteeringMotor), typeof(BoidFlocking), typeof(BoidHealth))]
    public class BoidAgent : MonoBehaviour
    {
        [Header("Evade")]
        [SerializeField] private float visionRange = 7f;
        // Mayor que visionRange: con el mismo radio para entrar y salir, el Boid deja de huir apenas cruza el borde y vuelve a entrar enseguida.
        [SerializeField] private float evadeExitRange = 11f;
        [SerializeField] private float evadeWeight = 2f;
        [SerializeField] private float evadeSpeed = 4.5f;
        [SerializeField] private float evadePredictionTime = 0.5f;
        // Mayor que wallMargin: empieza a deslizarse antes de que la contención lo frene contra la pared.
        [SerializeField] private float wallSlideMargin = 3f;

        [Header("Wander")]
        [SerializeField] private float wanderWeight = 0.8f;
        [SerializeField] private float wanderCircleDistance = 2f;
        [SerializeField] private float wanderCircleRadius = 1f;
        [SerializeField] private float wanderJitter = 360f;

        [Header("POI interaction")]
        [SerializeField] private float arriveWeight = 1.2f;
        [SerializeField] private float arriveSlowingRadius = 3f;
        [SerializeField] private float arriveThreshold = 1f;
        [SerializeField] private float workDamagePerTick = 5f;
        [SerializeField] private float workTickInterval = 1f;

        [Header("Containment")]
        [SerializeField] private float wallMargin = 1.5f;
        [SerializeField] private float containmentWeight = 3f;

        [Header("Scene")]
        [SerializeField] private ArenaBounds arenaBounds;
        [SerializeField] private HunterController hunter;

        private StateMachine<BoidStateId> _stateMachine;

        public SteeringMotor Motor { get; private set; }
        public BoidFlocking Flocking { get; private set; }
        public BoidHealth Health { get; private set; }
        public PointOfInterest TargetPOI { get; set; }

        public float CruiseSpeed { get; private set; }

        public float EvadeWeight => evadeWeight;
        public float EvadeSpeed => evadeSpeed;
        public float EvadePredictionTime => evadePredictionTime;
        public float WanderWeight => wanderWeight;
        public float WanderCircleDistance => wanderCircleDistance;
        public float WanderCircleRadius => wanderCircleRadius;
        public float WanderJitter => wanderJitter;
        public float ArriveThreshold => arriveThreshold;
        public float WorkDamagePerTick => workDamagePerTick;
        public float WorkTickInterval => workTickInterval;

        public Vector3 Velocity => Motor.Velocity;
        public bool IsDead => Health.IsDead;
        public bool IsCollectable => Health.IsCollectable;

        public Vector3 ThreatPosition => hunter.transform.position;
        public Vector3 ThreatVelocity => hunter.Velocity;
        public bool SeesThreat => IsThreatWithin(transform.position, visionRange);
        public bool IsSafeFromThreat => !IsThreatWithin(transform.position, evadeExitRange);

        private void Awake()
        {
            Motor = GetComponent<SteeringMotor>();
            Flocking = GetComponent<BoidFlocking>();
            Health = GetComponent<BoidHealth>();
            CruiseSpeed = Motor.MaxSpeed;
            BuildStateMachine();
        }

        private void Start() => _stateMachine.Start(BoidStateId.Roaming);

        private void Update() => _stateMachine.Tick(Time.deltaTime);

        private void BuildStateMachine()
        {
            _stateMachine = new StateMachine<BoidStateId>();
            _stateMachine.AddState(BoidStateId.Roaming, new BoidRoamingState(this, _stateMachine));
            _stateMachine.AddState(BoidStateId.SeekingPOI, new BoidSeekingPOIState(this, _stateMachine));
            _stateMachine.AddState(BoidStateId.Working, new BoidWorkingState(this, _stateMachine));
            _stateMachine.AddState(BoidStateId.Evading, new BoidEvadingState(this, _stateMachine));
            _stateMachine.AddState(BoidStateId.Dead, new BoidDeadState(this, _stateMachine));

            _stateMachine.AddAnyStateTransition(BoidStateId.Dead, () => Health.IsDead);
            _stateMachine.AddAnyStateTransition(BoidStateId.Evading, () => !Health.IsDead && SeesThreat);
        }

        private bool IsThreatWithin(Vector3 position, float range)
        {
            return hunter.IsAlive && PlanarMath.Distance(position, hunter.transform.position) <= range;
        }

        public void Steer(Vector3 force)
        {
            Motor.ApplyForce(force + Containment() * containmentWeight);
            transform.position = arenaBounds.ClampToArena(transform.position);
        }

        public void SteerTowardsPOI()
        {
            Vector3 arriveForce = SteeringBehaviours.Arrive(transform.position, Velocity, TargetPOI.transform.position, Motor.MaxSpeed, arriveSlowingRadius);
            Steer(Flocking.GetSeparationForce() + arriveForce * arriveWeight);
        }

        public Vector3 GetNearbyWallsInward() => arenaBounds.GetInwardDirection(transform.position, wallSlideMargin);

        // Los bordes del coto son paredes: cerca de ellos se empuja al Boid hacia adentro y el clamp impide que las cruce, incluso por las puertas.
        private Vector3 Containment()
        {
            return arenaBounds.GetInwardDirection(transform.position, wallMargin) * Motor.MaxSpeed;
        }

        // El Cazador spawnea los POIs a su lado: sin este filtro el Boid deja de huir y vuelve derecho hacia él.
        public PointOfInterest FindClosestSafePOI()
        {
            PointOfInterest closest = null;
            float closestDistance = float.MaxValue;

            IReadOnlyList<PointOfInterest> active = POIManager.Active;
            for (int i = 0; i < active.Count; i++)
            {
                if (IsThreatWithin(active[i].transform.position, visionRange))
                    continue;

                float distance = PlanarMath.Distance(transform.position, active[i].transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = active[i];
                }
            }

            return closest;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, visionRange);
        }
    }
}
