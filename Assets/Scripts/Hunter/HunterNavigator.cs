using System.Collections.Generic;
using UnityEngine;
using Core;
using Navigation;

namespace Hunter
{
    [RequireComponent(typeof(PathFollower))]
    public class HunterNavigator : MonoBehaviour
    {
        [SerializeField] private Zone huntingGround;
        [SerializeField] private Zone exitZone;
        [SerializeField] private Zone[] safeZones;
        [SerializeField] private float safeZoneWaitTime = 4f;

        private readonly List<NavNode> _huntingGroundNodes = new List<NavNode>();
        private PathFollower _path;

        public Zone ChosenSafeZone { get; private set; }
        public float SafeZoneWaitTime => safeZoneWaitTime;
        public Vector3 HuntingGroundCenter => huntingGround.Center;
        public Vector3 ExitPoint => exitZone.Center;
        public bool IsInHuntingGround => huntingGround.Contains(transform.position);
        public bool IsInExitZone => exitZone.Contains(transform.position);
        public bool IsInSafeZone => FindSafeZoneContaining(transform.position) != null;

        private void Awake()
        {
            _path = GetComponent<PathFollower>();
        }

        private void Start()
        {
            foreach (NavNode node in _path.Graph.Nodes)
            {
                if (node.Zone == huntingGround)
                    _huntingGroundNodes.Add(node);
            }
        }

        public Vector3 GetRandomHuntingGroundPoint() => _huntingGroundNodes[Random.Range(0, _huntingGroundNodes.Count)].Position;

        public void ChooseClosestSafeZone()
        {
            ChosenSafeZone = null;
            float shortestPath = float.MaxValue;

            foreach (Zone zone in safeZones)
            {
                float pathLength = _path.GetPathLength(zone.Center);
                if (pathLength < shortestPath)
                {
                    shortestPath = pathLength;
                    ChosenSafeZone = zone;
                }
            }
        }

        public void ClearSafeZoneChoice() => ChosenSafeZone = null;

        private Zone FindSafeZoneContaining(Vector3 position)
        {
            foreach (Zone zone in safeZones)
            {
                if (zone.Contains(position))
                    return zone;
            }

            return null;
        }
    }
}
