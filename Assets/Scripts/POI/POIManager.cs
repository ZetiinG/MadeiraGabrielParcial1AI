using System.Collections.Generic;
using UnityEngine;

namespace POI
{
    public static class POIManager
    {
        private static readonly List<PointOfInterest> _active = new List<PointOfInterest>();

        public static IReadOnlyList<PointOfInterest> Active => _active;
        public static int Count => _active.Count;

        public static void Register(PointOfInterest poi) => _active.Add(poi);
        public static void Unregister(PointOfInterest poi) => _active.Remove(poi);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad() => _active.Clear();
    }
}
