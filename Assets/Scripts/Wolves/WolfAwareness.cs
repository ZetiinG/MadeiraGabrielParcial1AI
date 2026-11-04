using System;
using UnityEngine;
using Hunter;
using Perception;

namespace Wolves
{
    [RequireComponent(typeof(FieldOfView))]
    public class WolfAwareness : MonoBehaviour
    {
        private const float GizmoMarkerRadius = 0.5f;

        [SerializeField] private HunterController hunter;

        private FieldOfView _fieldOfView;
        private bool _alertPending;

        public HunterController Hunter => hunter;
        public bool HasLastKnownPosition { get; private set; }
        public Vector3 LastKnownPosition { get; private set; }

        public event Action Howled;
        public event Action AlertReceived;

        private void Awake()
        {
            _fieldOfView = GetComponent<FieldOfView>();
        }

        public bool CanSeeHunter()
        {
            return hunter.IsAlive && !hunter.IsInSafeZone && _fieldOfView.CanSee(hunter.transform.position);
        }

        public bool IsHunterInSafeZone() => hunter.IsInSafeZone;

        public void RememberHunterPosition()
        {
            LastKnownPosition = hunter.transform.position;
            HasLastKnownPosition = true;
        }

        public void Howl()
        {
            WolfPack.Howl(this, LastKnownPosition);
            Howled?.Invoke();
        }

        public void ReceiveAlert(Vector3 hunterPosition)
        {
            LastKnownPosition = hunterPosition;
            HasLastKnownPosition = true;
            _alertPending = true;
            AlertReceived?.Invoke();
        }

        public bool ConsumeAlert()
        {
            bool pending = _alertPending;
            _alertPending = false;
            return pending;
        }

        public void ForgetHunter()
        {
            HasLastKnownPosition = false;
            _alertPending = false;
        }

        private void OnDrawGizmos()
        {
            if (!HasLastKnownPosition)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, LastKnownPosition);
            Gizmos.DrawSphere(LastKnownPosition, GizmoMarkerRadius);
        }
    }
}
