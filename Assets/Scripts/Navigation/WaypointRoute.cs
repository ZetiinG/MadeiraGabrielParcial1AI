using UnityEngine;
using Core;

namespace Navigation
{
    public enum WaypointTraversalMode { Loop, PingPong }

    public class WaypointRoute : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private WaypointTraversalMode traversalMode = WaypointTraversalMode.PingPong;
        [SerializeField] private float arriveThreshold = 1f;

        private int _index;
        private int _direction = 1;

        public Transform Current => waypoints.Length == 0 ? null : waypoints[_index];

        public bool HasReached(Vector3 position)
        {
            Transform waypoint = Current;
            return waypoint != null && PlanarMath.Distance(position, waypoint.position) <= arriveThreshold;
        }

        public void Advance()
        {
            if (waypoints.Length == 0)
                return;

            if (traversalMode == WaypointTraversalMode.Loop)
            {
                _index = (_index + 1) % waypoints.Length;
                return;
            }

            _index += _direction;
            if (_index >= waypoints.Length)
            {
                _index = waypoints.Length - 1;
                _direction = -1;
            }
            else if (_index < 0)
            {
                _index = 0;
                _direction = 1;
            }
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null)
                return;

            Gizmos.color = Color.yellow;
            for (int i = 1; i < waypoints.Length; i++)
            {
                if (waypoints[i - 1] != null && waypoints[i] != null)
                    Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
            }
        }
    }
}
