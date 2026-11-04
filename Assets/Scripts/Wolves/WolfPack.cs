using System.Collections.Generic;
using UnityEngine;

namespace Wolves
{
    public static class WolfPack
    {
        private static readonly List<WolfController> _members = new List<WolfController>();

        public static IReadOnlyList<WolfController> Members => _members;

        public static void Register(WolfController wolf) => _members.Add(wolf);
        public static void Unregister(WolfController wolf) => _members.Remove(wolf);

        public static void Howl(WolfAwareness caller, Vector3 hunterPosition)
        {
            foreach (WolfController wolf in _members)
            {
                if (wolf.Awareness != caller)
                    wolf.Awareness.ReceiveAlert(hunterPosition);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad() => _members.Clear();
    }
}
