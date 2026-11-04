using System.Collections.Generic;
using UnityEngine;
using Core;
using Steering;

namespace Navigation
{
    [RequireComponent(typeof(SteeringMotor))]
    public class PathFollower : MonoBehaviour
    {
        [SerializeField] private NavGraph graph;
        [SerializeField] private bool avoidSafeZones;
        [SerializeField] private float pointReachedRadius = 0.8f;
        [SerializeField] private float arriveSlowingRadius = 2f;
        [SerializeField] private LineRenderer pathLine;

        private readonly List<Vector3> _points = new List<Vector3>();
        private int _index;
        private SteeringMotor _motor;

        public bool HasPath => _index < _points.Count;
        public NavGraph Graph => graph;

        private void Awake()
        {
            _motor = GetComponent<SteeringMotor>();
        }

        public bool SetDestination(Vector3 destination)
        {
            Clear();
            destination = AtAgentHeight(destination);

            List<NavNode> path = graph.FindPath(transform.position, destination, IsWalkable);
            if (path == null)
                return false;

            foreach (NavNode node in path)
                _points.Add(node.Position);
            _points.Add(destination);

            // Evita volver hacia atrás al primer nodo cuando el segundo ya está a la vista.
            if (_points.Count > 1 && graph.HasClearPath(transform.position, _points[1]))
                _index = 1;

            RefreshLine();
            return true;
        }

        public float GetPathLength(Vector3 destination)
        {
            destination = AtAgentHeight(destination);
            List<NavNode> path = graph.FindPath(transform.position, destination, IsWalkable);
            if (path == null)
                return float.MaxValue;

            float length = Vector3.Distance(transform.position, path[0].Position);
            for (int i = 1; i < path.Count; i++)
                length += Vector3.Distance(path[i - 1].Position, path[i].Position);

            return length + Vector3.Distance(path[path.Count - 1].Position, destination);
        }

        public Vector3 GetRandomWalkablePointNear(Vector3 center, float radius) => graph.GetRandomPointNear(center, radius, IsWalkable);
        public void Tick()
        {
            if (!HasPath)
                return;

            Vector3 target = _points[_index];
            if (_index == _points.Count - 1)
                _motor.Arrive(target, arriveSlowingRadius);
            else
                _motor.Seek(target);

            if (PlanarMath.Distance(transform.position, target) <= pointReachedRadius)
            {
                _index++;
                if (!HasPath)
                    _motor.Stop();
            }

            RefreshLine();
        }

        public void Clear()
        {
            _points.Clear();
            _index = 0;
            RefreshLine();
        }

        private bool IsWalkable(NavNode node) => !avoidSafeZones || !node.IsSafeZone;

        private Vector3 AtAgentHeight(Vector3 point) => new Vector3(point.x, transform.position.y, point.z);

        private void RefreshLine()
        {
            if (pathLine == null)
                return;

            int remaining = _points.Count - _index;
            pathLine.positionCount = remaining > 0 ? remaining + 1 : 0;
            if (remaining <= 0)
                return;

            pathLine.SetPosition(0, transform.position);
            for (int i = 0; i < remaining; i++)
                pathLine.SetPosition(i + 1, _points[_index + i]);
        }
    }
}
