using UnityEngine;

namespace Core
{
    public enum ZoneType { Start, HuntingGround, Safe, Exit }

    public class Zone : MonoBehaviour
    {
        private const float GizmoHeight = 0.1f;

        [SerializeField] private ZoneType type;
        [SerializeField] private Vector2 size = new Vector2(6f, 6f);

        public ZoneType Type => type;
        public Vector3 Center => transform.position;

        public bool Contains(Vector3 position)
        {
            Vector3 offset = position - transform.position;
            return Mathf.Abs(offset.x) <= size.x * 0.5f && Mathf.Abs(offset.z) <= size.y * 0.5f;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = GetGizmoColor();
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, GizmoHeight, size.y));
        }

        private Color GetGizmoColor()
        {
            switch (type)
            {
                case ZoneType.Start: return Color.green;
                case ZoneType.HuntingGround: return new Color(1f, 0.6f, 0f);
                case ZoneType.Safe: return Color.magenta;
                default: return Color.blue;
            }
        }
    }
}
