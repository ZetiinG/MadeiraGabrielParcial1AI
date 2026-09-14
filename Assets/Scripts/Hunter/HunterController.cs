using UnityEngine;
using Boids;
using Core;
using Steering;

namespace Hunter
{
    public class HunterController : MonoBehaviour
    {
        public enum WaypointTraversalMode { Loop, PingPong }

        public static HunterController Instance { get; private set; }

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float maxForce = 10f;

        [Header("Patrol")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private WaypointTraversalMode traversalMode = WaypointTraversalMode.Loop;
        [SerializeField] private float waypointArriveThreshold = 1f;
        [SerializeField] private GameObject poiPrefab;
        [SerializeField] private float poiSpawnInterval = 6f;
        [SerializeField] private float poiSpawnRadius = 2f;
        [SerializeField] private ArenaBounds arenaBounds;

        [Header("Detection")]
        [SerializeField] private float visionRange = 10f;
        [SerializeField] private LayerMask boidLayerMask;

        [Header("Attack")]
        [SerializeField] private float tba = 3f;
        [SerializeField] private float rangeAttackRadius = 5f;
        [SerializeField] private float meleeAttackRadius = 1.5f;
        [SerializeField] private float meleeStopRadius = 0.8f;
        [SerializeField] private float meleeDamage = 10f;
        [SerializeField] private float rangedDamage = 10f;
        [SerializeField] private float projectileSpeed = 25f;

        [Header("Gather")]
        [SerializeField] private float gatherDuration = 2f;
        [SerializeField] private float gatherInteractRadius = 1.2f;

        [Header("Feedback")]
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Material patrolMaterial;
        [SerializeField] private Material attackMaterial;
        [SerializeField] private Material gatherMaterial;

        private readonly Collider[] _overlapBuffer = new Collider[32];
        private IHunterState _currentState;
        private HunterPatrolState _patrolState;
        private HunterAttackState _attackState;
        private HunterGatherState _gatherState;

        private int _waypointIndex;
        private int _waypointDirection = 1;
        private float _poiSpawnTimer;
        private float _lastAttackTime = -9999f;
        private Vector3 _velocity;
        private Material _projectileMaterial;

        public float VisionRange => visionRange;
        public float RangeAttackRadius => rangeAttackRadius;
        public float MeleeAttackRadius => meleeAttackRadius;
        public float MeleeStopRadius => meleeStopRadius;
        public float TBA => tba;
        public float MeleeDamage => meleeDamage;
        public float RangedDamage => rangedDamage;
        public float GatherDuration => gatherDuration;
        public float GatherInteractRadius => gatherInteractRadius;
        public Vector3 Velocity => _velocity;
        public BoidAgent CurrentTarget { get; set; }
        public bool AttackReady => Time.time >= _lastAttackTime + tba;
        public string CurrentStateName { get; private set; }

        private void Awake()
        {
            Instance = this;
            _patrolState = new HunterPatrolState();
            _attackState = new HunterAttackState();
            _gatherState = new HunterGatherState();
        }

        private void Start()
        {
            _currentState = _patrolState;
            CurrentStateName = "Patrol";
            _currentState.Enter(this);
        }

        private void Update()
        {
            _currentState.Tick(this, Time.deltaTime);
        }

        public void TransitionTo(IHunterState next, string stateName)
        {
            _currentState.Exit(this);
            _currentState = next;
            CurrentStateName = stateName;
            _currentState.Enter(this);
        }

        public HunterPatrolState PatrolState => _patrolState;
        public HunterAttackState AttackState => _attackState;
        public HunterGatherState GatherState => _gatherState;

        public void ResetAttackTimer() => _lastAttackTime = Time.time;

        public void SetBodyMaterial(Material material)
        {
            if (bodyRenderer != null && material != null)
                bodyRenderer.material = material;
        }

        public Material PatrolMaterial => patrolMaterial;
        public Material AttackMaterial => attackMaterial;
        public Material GatherMaterial => gatherMaterial;

        public Transform CurrentWaypoint => waypoints.Length == 0 ? null : waypoints[_waypointIndex];

        public void AdvanceWaypoint()
        {
            if (waypoints.Length == 0)
                return;

            if (traversalMode == WaypointTraversalMode.Loop)
            {
                _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
                return;
            }

            _waypointIndex += _waypointDirection;
            if (_waypointIndex >= waypoints.Length)
            {
                _waypointIndex = waypoints.Length - 1;
                _waypointDirection = -1;
            }
            else if (_waypointIndex < 0)
            {
                _waypointIndex = 0;
                _waypointDirection = 1;
            }
        }

        public bool HasReachedWaypoint()
        {
            Transform wp = CurrentWaypoint;
            return wp != null && PlanarDistance(transform.position, wp.position) <= waypointArriveThreshold;
        }

        public void TickPOISpawning(float deltaTime)
        {
            if (poiPrefab == null)
                return;

            _poiSpawnTimer += deltaTime;
            if (_poiSpawnTimer < poiSpawnInterval || POI.POIManager.Count >= 5)
                return;

            _poiSpawnTimer = 0f;
            Vector3 offset = new Vector3(Random.Range(-poiSpawnRadius, poiSpawnRadius), 0f, Random.Range(-poiSpawnRadius, poiSpawnRadius));
            Vector3 spawnPoint = transform.position + offset;
            if (arenaBounds != null)
            {
                spawnPoint = arenaBounds.ClampToArena(spawnPoint);
                spawnPoint.y = arenaBounds.transform.position.y;
            }

            Instantiate(poiPrefab, spawnPoint, Quaternion.identity);
        }

        public BoidAgent FindLivingBoidInRange()
        {
            return FindBoidInRange(dead: false);
        }

        public BoidAgent FindDeadBoidInRange()
        {
            return FindBoidInRange(dead: true);
        }

        private BoidAgent FindBoidInRange(bool dead)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, visionRange, _overlapBuffer, boidLayerMask);
            BoidAgent closest = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                BoidAgent boid = _overlapBuffer[i].GetComponent<BoidAgent>();
                if (boid == null || boid.IsDead != dead)
                    continue;

                float distance = PlanarDistance(transform.position, boid.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = boid;
                }
            }

            return closest;
        }

        public float DistanceToTarget()
        {
            return CurrentTarget == null ? float.MaxValue : PlanarDistance(transform.position, CurrentTarget.transform.position);
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        public void SeekPoint(Vector3 point)
        {
            Steer(SteeringBehaviours.Seek(transform.position, _velocity, point, moveSpeed));
            Move();
        }

        public void ArrivePoint(Vector3 point, float slowingRadius)
        {
            Steer(SteeringBehaviours.Arrive(transform.position, _velocity, point, moveSpeed, slowingRadius));
            Move();
        }

        public void PursueTarget(BoidAgent target)
        {
            Steer(SteeringBehaviours.Pursue(transform.position, _velocity, target.transform.position, target.Velocity, moveSpeed, 0.5f));
            Move();
        }

        public void Stop() => _velocity = Vector3.zero;

        public void FireRangedAttack(BoidAgent target)
        {
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.transform.position = new Vector3(transform.position.x, target.transform.position.y, transform.position.z);
            projectile.transform.localScale = Vector3.one * 0.25f;
            projectile.GetComponent<Renderer>().material = GetProjectileMaterial();
            Destroy(projectile.GetComponent<SphereCollider>());

            projectile.AddComponent<HunterProjectile>().Initialize(target, projectileSpeed, rangedDamage);
        }

        private Material GetProjectileMaterial()
        {
            if (_projectileMaterial != null)
                return _projectileMaterial;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            _projectileMaterial = new Material(shader);
            _projectileMaterial.color = Color.red;
            return _projectileMaterial;
        }

        private void Steer(Vector3 force)
        {
            force.y = 0f;
            force = Vector3.ClampMagnitude(force, maxForce);
            _velocity = Vector3.ClampMagnitude(_velocity + force * Time.deltaTime, moveSpeed);
            _velocity.y = 0f;
        }

        private void Move()
        {
            if (_velocity.sqrMagnitude < 0.0001f)
                return;

            transform.position += _velocity * Time.deltaTime;
            transform.forward = _velocity.normalized;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, visionRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);
        }
    }
}
