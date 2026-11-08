using System;
using System.Collections.Generic;
using UnityEngine;
using Core;
using Random = UnityEngine.Random;

namespace Navigation
{
    public class NavGraph : MonoBehaviour
    {
        private const float NodeGizmoRadius = 0.25f;

        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private float connectionRadius = 8.6f;
        [SerializeField] private float agentRadius = 0.5f;
        [SerializeField] private List<NavNode> nodes = new List<NavNode>();

        private readonly List<NavNode> _candidates = new List<NavNode>();

        public IReadOnlyList<NavNode> Nodes => nodes;

        public List<NavNode> FindPath(Vector3 from, Vector3 to, Predicate<NavNode> isWalkable)
        {
            NavNode start = GetClosestVisibleNode(from, isWalkable);
            NavNode goal = GetClosestVisibleNode(to, isWalkable);
            if (start == null || goal == null)
                return null;

            return AStar.FindPath(start, goal, isWalkable);
        }

        // El SphereCast ignora paredes que ya solapa en el origen; el Linecast cubre ese caso cuando el agente roza una pared.
        public bool HasClearPath(Vector3 from, Vector3 to)
        {
            Vector3 offset = to - from;
            float distance = offset.magnitude;
            if (distance < Mathf.Epsilon)
                return true;

            return !Physics.Linecast(from, to, obstacleMask)
                && !Physics.SphereCast(from, agentRadius, offset / distance, out _, distance, obstacleMask);
        }

        public Vector3 GetRandomPointNear(Vector3 center, float radius, Predicate<NavNode> isWalkable)
        {
            _candidates.Clear();
            foreach (NavNode node in nodes)
            {
                if (isWalkable(node) && PlanarMath.Distance(node.Position, center) <= radius)
                    _candidates.Add(node);
            }

            return _candidates.Count == 0 ? center : _candidates[Random.Range(0, _candidates.Count)].Position;
        }

        private NavNode GetClosestVisibleNode(Vector3 position, Predicate<NavNode> isWalkable)
        {
            NavNode closest = null;
            float closestDistance = float.MaxValue;

            foreach (NavNode node in nodes)
            {
                if (!isWalkable(node))
                    continue;

                float distance = Vector3.Distance(position, node.Position);
                if (distance < closestDistance && HasClearPath(position, node.Position))
                {
                    closestDistance = distance;
                    closest = node;
                }
            }

            return closest;
        }

        [ContextMenu("Bake")]
        public void Bake()
        {
            nodes.Clear();
            GetComponentsInChildren(nodes);
            Zone[] zones = FindObjectsByType<Zone>(FindObjectsSortMode.None);
            Physics.SyncTransforms();

            foreach (NavNode node in nodes)
            {
                node.SetZone(FindZoneContaining(zones, node.Position));
                node.ClearNeighbors();
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                    TryConnect(nodes[i], nodes[j]);
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            foreach (NavNode node in nodes)
                UnityEditor.EditorUtility.SetDirty(node);
#endif
        }

        private void TryConnect(NavNode a, NavNode b)
        {
            if (Vector3.Distance(a.Position, b.Position) > connectionRadius || !HasClearPath(a.Position, b.Position))
                return;

            a.AddNeighbor(b);
            b.AddNeighbor(a);
        }

        private static Zone FindZoneContaining(Zone[] zones, Vector3 position)
        {
            foreach (Zone zone in zones)
            {
                if (zone.Contains(position))
                    return zone;
            }

            return null;
        }

        private void OnDrawGizmos()
        {
            foreach (NavNode node in nodes)
            {
                if (node == null)
                    continue;

                Gizmos.color = node.IsSafeZone ? Color.magenta : Color.white;
                Gizmos.DrawSphere(node.Position, NodeGizmoRadius);

                Gizmos.color = Color.gray;
                foreach (NavNode neighbor in node.Neighbors)
                    Gizmos.DrawLine(node.Position, neighbor.Position);
            }
        }
    }
}
