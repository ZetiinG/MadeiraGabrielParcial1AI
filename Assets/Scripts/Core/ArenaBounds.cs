using UnityEngine;

namespace Core
{
    public class ArenaBounds : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(30f, 30f);

        public Vector3 GetRandomPoint()
        {
            float x = Random.Range(-size.x * 0.5f, size.x * 0.5f);
            float z = Random.Range(-size.y * 0.5f, size.y * 0.5f);
            return transform.position + new Vector3(x, 0f, z);
        }

        public Vector3 Wrap(Vector3 position)
        {
            Vector3 center = transform.position;
            float halfX = size.x * 0.5f;
            float halfZ = size.y * 0.5f;

            float x = position.x;
            float z = position.z;

            if (x > center.x + halfX) x -= size.x;
            else if (x < center.x - halfX) x += size.x;

            if (z > center.z + halfZ) z -= size.y;
            else if (z < center.z - halfZ) z += size.y;

            return new Vector3(x, position.y, z);
        }

        public Vector3 ClampToArena(Vector3 position)
        {
            Vector3 center = transform.position;
            float halfX = size.x * 0.5f;
            float halfZ = size.y * 0.5f;

            float x = Mathf.Clamp(position.x, center.x - halfX, center.x + halfX);
            float z = Mathf.Clamp(position.z, center.z - halfZ, center.z + halfZ);

            return new Vector3(x, position.y, z);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, 0.1f, size.y));
        }
    }
}
