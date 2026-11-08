using UnityEngine;

namespace Core
{
    public static class PlanarMath
    {
        public static float Distance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
