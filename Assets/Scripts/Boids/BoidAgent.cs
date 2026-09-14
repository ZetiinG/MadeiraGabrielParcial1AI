using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core;
using Hunter;
using POI;
using Steering;

namespace Boids
{
    public class BoidAgent : MonoBehaviour
    {
        private enum BoidTask { Roaming, SeekingPOI, Working, Dead }

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float maxForce = 8f;

        [Header("Flocking radii")]
        [SerializeField] private float separationRadius = 1.2f;
        [SerializeField] private float neighborRadius = 4.5f;
        [SerializeField] private LayerMask boidLayerMask;

        [Header("Flocking weights")]
        [SerializeField] private float separationWeight = 1.6f;
        [SerializeField] private float alignmentWeight = 1f;
        [SerializeField] private float cohesionWeight = 1f;
        [SerializeField] private float arriveWeight = 1.2f;

        [Header("Evade")]
        [SerializeField] private float visionRange = 7f;
        [SerializeField] private float evadeWeight = 2f;
        [SerializeField] private float evadePredictionTime = 0.5f;

        [Header("POI interaction")]
        [SerializeField] private float arriveSlowingRadius = 3f;
        [SerializeField] private float arriveThreshold = 1f;
        [SerializeField] private float workDamagePerTick = 5f;
        [SerializeField] private float workTickInterval = 1f;

        [Header("Health & respawn")]
        [SerializeField] private float maxHealth = 20f;
        [SerializeField] private float respawnDelay = 4f;
        [SerializeField] private ArenaBounds arenaBounds;

        [Header("Materials")]
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Material aliveMaterial;
        [SerializeField] private Material deadMaterial;
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private float hitFlashDuration = 0.15f;
        [SerializeField] private Color lowHealthColor = new Color(1f, 0.85f, 0.85f);
        [SerializeField] private float lowHealthThreshold = 0.5f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private BoidTask _task = BoidTask.Roaming;
        private Vector3 _velocity;
        private float _currentHealth;
        private PointOfInterest _targetPOI;
        private float _workTimer;
        private Collider _collider;
        private MaterialPropertyBlock _propertyBlock;
        private readonly Collider[] _overlapBuffer = new Collider[32];
        private readonly List<BoidAgent> _separationNeighbors = new List<BoidAgent>();
        private readonly List<BoidAgent> _flockNeighbors = new List<BoidAgent>();

        public Vector3 Velocity => _velocity;
        public bool IsDead => _task == BoidTask.Dead;

        private void Awake()
        {
            _currentHealth = maxHealth;
            _collider = GetComponent<Collider>();
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (_task == BoidTask.Dead)
                return;

            if (IsThreatVisible(out Vector3 threatPosition, out Vector3 threatVelocity))
            {
                Steer(SteeringBehaviours.Evade(transform.position, _velocity, threatPosition, threatVelocity, moveSpeed, evadePredictionTime) * evadeWeight);
                Move();
                return;
            }

            switch (_task)
            {
                case BoidTask.Roaming:
                    TickRoaming();
                    break;
                case BoidTask.SeekingPOI:
                    TickSeekingPOI();
                    break;
                case BoidTask.Working:
                    TickWorking();
                    break;
            }

            Move();
        }

        private bool IsThreatVisible(out Vector3 threatPosition, out Vector3 threatVelocity)
        {
            HunterController hunter = HunterController.Instance;
            if (hunter == null)
            {
                threatPosition = Vector3.zero;
                threatVelocity = Vector3.zero;
                return false;
            }

            threatPosition = hunter.transform.position;
            threatVelocity = hunter.Velocity;
            return Vector3.Distance(transform.position, threatPosition) <= visionRange;
        }

        private void TickRoaming()
        {
            GatherNeighbors();
            Vector3 force = SteeringBehaviours.Separation(transform.position, _separationNeighbors, separationRadius) * separationWeight
                + SteeringBehaviours.Alignment(_velocity, _flockNeighbors) * alignmentWeight
                + SteeringBehaviours.Cohesion(transform.position, _flockNeighbors) * cohesionWeight;
            Steer(force);

            _targetPOI = FindClosestPOI();
            if (_targetPOI != null)
                _task = BoidTask.SeekingPOI;
        }

        private void TickSeekingPOI()
        {
            if (_targetPOI == null)
            {
                _task = BoidTask.Roaming;
                return;
            }

            GatherNeighbors();
            Vector3 force = SteeringBehaviours.Separation(transform.position, _separationNeighbors, separationRadius) * separationWeight
                + SteeringBehaviours.Arrive(transform.position, _velocity, _targetPOI.transform.position, moveSpeed, arriveSlowingRadius) * arriveWeight;
            Steer(force);

            if (Vector3.Distance(transform.position, _targetPOI.transform.position) <= arriveThreshold)
            {
                _task = BoidTask.Working;
                _workTimer = 0f;
                _velocity = Vector3.zero;
            }
        }

        private void TickWorking()
        {
            if (_targetPOI == null)
            {
                _task = BoidTask.Roaming;
                return;
            }

            _workTimer += Time.deltaTime;
            if (_workTimer >= workTickInterval)
            {
                _workTimer = 0f;
                _targetPOI.TakeDamage(workDamagePerTick);
            }
        }

        private PointOfInterest FindClosestPOI()
        {
            PointOfInterest closest = null;
            float closestDistance = float.MaxValue;

            IReadOnlyList<PointOfInterest> active = POIManager.Active;
            for (int i = 0; i < active.Count; i++)
            {
                float distance = Vector3.Distance(transform.position, active[i].transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = active[i];
                }
            }

            return closest;
        }

        private void GatherNeighbors()
        {
            _separationNeighbors.Clear();
            _flockNeighbors.Clear();

            int count = Physics.OverlapSphereNonAlloc(transform.position, neighborRadius, _overlapBuffer, boidLayerMask);
            for (int i = 0; i < count; i++)
            {
                BoidAgent other = _overlapBuffer[i].GetComponent<BoidAgent>();
                if (other == null || other == this || other.IsDead)
                    continue;

                _flockNeighbors.Add(other);
                if (Vector3.Distance(transform.position, other.transform.position) <= separationRadius)
                    _separationNeighbors.Add(other);
            }
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

            if (arenaBounds != null)
                transform.position = arenaBounds.Wrap(transform.position);
        }

        public void TakeDamage(float amount)
        {
            if (_task == BoidTask.Dead)
                return;

            _currentHealth -= amount;
            if (_currentHealth <= 0f)
                Die();
            else
                StartCoroutine(HitFlash());
        }

        private IEnumerator HitFlash()
        {
            _propertyBlock.SetColor(BaseColorId, hitFlashColor);
            bodyRenderer.SetPropertyBlock(_propertyBlock);
            yield return new WaitForSeconds(hitFlashDuration);
            RefreshHealthColor();
        }

        private void RefreshHealthColor()
        {
            if (_currentHealth <= maxHealth * lowHealthThreshold)
            {
                _propertyBlock.SetColor(BaseColorId, lowHealthColor);
                bodyRenderer.SetPropertyBlock(_propertyBlock);
            }
            else
            {
                bodyRenderer.SetPropertyBlock(null);
            }
        }

        private void Die()
        {
            _task = BoidTask.Dead;
            StopAllCoroutines();
            bodyRenderer.SetPropertyBlock(null);
            _velocity = Vector3.zero;
            _targetPOI = null;
            if (bodyRenderer != null && deadMaterial != null)
                bodyRenderer.material = deadMaterial;
        }

        public void OnGathered()
        {
            SetVisible(false);
            StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);

            Vector3 respawnPoint = arenaBounds.GetRandomPoint();
            respawnPoint.y = transform.position.y;
            transform.position = respawnPoint;
            _currentHealth = maxHealth;
            _velocity = Vector3.zero;
            _task = BoidTask.Roaming;
            if (bodyRenderer != null && aliveMaterial != null)
                bodyRenderer.material = aliveMaterial;
            RefreshHealthColor();

            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            if (bodyRenderer != null)
                bodyRenderer.enabled = visible;
            if (_collider != null)
                _collider.enabled = visible;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, separationRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, neighborRadius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, visionRange);
        }
    }
}
