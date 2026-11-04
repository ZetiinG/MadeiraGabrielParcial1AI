using System.Collections.Generic;
using UnityEngine;
using Core;

namespace Navigation
{
    public class NavNode : MonoBehaviour
    {
        [SerializeField] private Zone zone;
        [SerializeField] private List<NavNode> neighbors = new List<NavNode>();

        public Zone Zone => zone;
        public bool IsSafeZone => zone != null && zone.Type == ZoneType.Safe;
        public IReadOnlyList<NavNode> Neighbors => neighbors;
        public Vector3 Position => transform.position;

        public void SetZone(Zone value) => zone = value;
        public void ClearNeighbors() => neighbors.Clear();
        public void AddNeighbor(NavNode node) => neighbors.Add(node);
    }
}
