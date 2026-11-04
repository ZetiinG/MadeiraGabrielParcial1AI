using System.Collections.Generic;
using UnityEngine;

namespace Perception
{
    // Reemplaza las consultas OverlapSphere por frame: el trigger mantiene la lista de lo que está en rango por eventos de física.
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public abstract class TriggerSensor<T> : MonoBehaviour where T : Component
    {
        [SerializeField] private LayerMask detectionMask;

        private readonly List<Collider> _colliders = new List<Collider>();
        private readonly List<T> _detected = new List<T>();
        private SphereCollider _trigger;

        public IReadOnlyList<T> Detected => _detected;

        protected void Awake()
        {
            _trigger = GetComponent<SphereCollider>();
            _trigger.isTrigger = true;
            _trigger.excludeLayers = ~detectionMask;

            Rigidbody body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        public void SetWorldRadius(float radius)
        {
            Vector3 scale = transform.lossyScale;
            _trigger.radius = radius / Mathf.Max(scale.x, scale.y, scale.z);
        }

        protected void OnTriggerEnter(Collider other)
        {
            if (_colliders.Contains(other) || !other.TryGetComponent(out T target))
                return;

            _colliders.Add(other);
            _detected.Add(target);
        }

        protected void OnTriggerExit(Collider other)
        {
            int index = _colliders.IndexOf(other);
            if (index >= 0)
                RemoveAt(index);
        }

        // Unity no envía OnTriggerExit cuando el otro collider se desactiva (ej. un Boid recolectado), por eso se limpian acá.
        protected void FixedUpdate()
        {
            for (int i = _colliders.Count - 1; i >= 0; i--)
            {
                Collider other = _colliders[i];
                if (other == null || !other.enabled || !other.gameObject.activeInHierarchy)
                    RemoveAt(i);
            }
        }

        private void RemoveAt(int index)
        {
            _colliders.RemoveAt(index);
            _detected.RemoveAt(index);
        }
    }
}
