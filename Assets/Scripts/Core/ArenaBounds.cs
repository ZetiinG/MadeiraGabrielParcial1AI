using UnityEngine;

namespace Core
{
    public class ArenaBounds : MonoBehaviour
    {
        private const float GizmoHeight = 0.1f;

        [SerializeField] private Vector2 size = new Vector2(30f, 30f);

        public Vector3 GetRandomPoint()
        {
            float x = Random.Range(-size.x * 0.5f, size.x * 0.5f);
            float z = Random.Range(-size.y * 0.5f, size.y * 0.5f);
            return transform.position + new Vector3(x, 0f, z);
        }

        // Dirección hacia el interior desde los bordes que están a menos de margin; cero si la posición está lejos de todos.
        public Vector3 GetInwardDirection(Vector3 position, float margin)
        {
            Vector3 offset = position - transform.position;
            float limitX = size.x * 0.5f - margin;
            float limitZ = size.y * 0.5f - margin;

            Vector3 inward = Vector3.zero;
            if (offset.x > limitX) inward.x = -1f;
            else if (offset.x < -limitX) inward.x = 1f;

            if (offset.z > limitZ) inward.z = -1f;
            else if (offset.z < -limitZ) inward.z = 1f;

            return inward.normalized;
        }

        public Vector3 ClampToArena(Vector3 position, float margin = 0f)
        {
            Vector3 center = transform.position;
            float halfX = size.x * 0.5f - margin;
            float halfZ = size.y * 0.5f - margin;

            float x = Mathf.Clamp(position.x, center.x - halfX, center.x + halfX);
            float z = Mathf.Clamp(position.z, center.z - halfZ, center.z + halfZ);

            return new Vector3(x, position.y, z);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, GizmoHeight, size.y));
        }
    }
}
