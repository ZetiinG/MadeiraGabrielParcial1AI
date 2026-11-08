using System.Collections;
using UnityEngine;

namespace Boids
{
    [RequireComponent(typeof(BoidHealth))]
    public class BoidVisualFeedback : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Color deadColor = new Color(0.25f, 0.25f, 0.25f);
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private float hitFlashDuration = 0.15f;
        [SerializeField] private Color lowHealthColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private float lowHealthThreshold = 0.5f;

        private BoidHealth _health;
        private MaterialPropertyBlock _propertyBlock;
        private Coroutine _flashRoutine;

        private void Awake()
        {
            _health = GetComponent<BoidHealth>();
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
            _health.Gathered += OnGathered;
            _health.Respawned += OnRespawned;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
            _health.Gathered -= OnGathered;
            _health.Respawned -= OnRespawned;
        }

        private void OnDamaged()
        {
            StopFlash();
            _flashRoutine = StartCoroutine(HitFlash());
        }

        private void OnDied()
        {
            StopFlash();
            SetColor(deadColor);
        }

        private void OnGathered() => bodyRenderer.enabled = false;

        private void OnRespawned()
        {
            RefreshHealthColor();
            bodyRenderer.enabled = true;
        }

        private void StopFlash()
        {
            if (_flashRoutine != null)
                StopCoroutine(_flashRoutine);
        }

        private IEnumerator HitFlash()
        {
            SetColor(hitFlashColor);
            yield return new WaitForSeconds(hitFlashDuration);
            RefreshHealthColor();
        }

        private void RefreshHealthColor()
        {
            if (_health.NormalizedHealth <= lowHealthThreshold)
                SetColor(lowHealthColor);
            else
                bodyRenderer.SetPropertyBlock(null);
        }

        private void SetColor(Color color)
        {
            _propertyBlock.SetColor(BaseColorId, color);
            bodyRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
