using UnityEngine;
using Core;

namespace Hunter
{
    public class HunterLife : MonoBehaviour
    {
        [SerializeField] private Zone startZone;
        [SerializeField] private float respawnTime = 10f;

        private Collider _body;

        public bool IsAlive { get; private set; } = true;
        public bool WasCaught { get; private set; }
        public float RespawnTime => respawnTime;

        private void Awake()
        {
            _body = GetComponent<Collider>();
        }

        public void NotifyCaught()
        {
            if (IsAlive)
                WasCaught = true;
        }

        public void Despawn()
        {
            WasCaught = false;
            IsAlive = false;
            // Muerto solo se oculta el render: sin esto los lobos chocarían contra un cuerpo invisible.
            _body.enabled = false;
        }

        public void Respawn()
        {
            Vector3 spawnPoint = startZone.Center;
            spawnPoint.y = transform.position.y;
            transform.position = spawnPoint;
            IsAlive = true;
            _body.enabled = true;
        }
    }
}
