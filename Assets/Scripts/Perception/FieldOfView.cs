using UnityEngine;

namespace Perception
{
    public class FieldOfView : MonoBehaviour
    {
        private const float FullCircle = 360f;
        private const int GizmoArcSegments = 24;

        [SerializeField] private float viewRadius = 10f;
        [SerializeField, Range(0f, FullCircle)] private float viewAngle = 90f;
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private Color gizmoColor = new Color(1f, 0.5f, 0f);

        public float ViewRadius => viewRadius;

        public bool CanSee(Vector3 target)
        {
            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude > viewRadius)
                return false;

            if (viewAngle < FullCircle && Vector3.Angle(transform.forward, toTarget) > viewAngle * 0.5f)
                return false;

            return HasLineOfSight(target);
        }

        public bool HasLineOfSight(Vector3 target) => !Physics.Linecast(transform.position, target, obstacleMask);

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;

            if (viewAngle >= FullCircle)
            {
                Gizmos.DrawWireSphere(transform.position, viewRadius);
                return;
            }

            float halfAngle = viewAngle * 0.5f;
            Vector3 previous = transform.position + DirectionFromAngle(-halfAngle) * viewRadius;
            Gizmos.DrawLine(transform.position, previous);

            for (int i = 1; i <= GizmoArcSegments; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)GizmoArcSegments);
                Vector3 next = transform.position + DirectionFromAngle(angle) * viewRadius;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }

            Gizmos.DrawLine(transform.position, previous);
        }

        private Vector3 DirectionFromAngle(float angle) => Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
    }
}
