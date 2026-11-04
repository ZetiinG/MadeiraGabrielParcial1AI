using UnityEngine;

namespace Core
{
    public class Billboard : MonoBehaviour
    {
        private Transform _camera;

        private void Awake()
        {
            _camera = Camera.main.transform;
        }

        private void LateUpdate() => transform.forward = _camera.forward;
    }
}
