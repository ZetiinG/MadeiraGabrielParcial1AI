using System.Collections;
using UnityEngine;

namespace Wolves
{
    public class WolfVisualFeedback : MonoBehaviour
    {
        private const float FullCircleRadians = Mathf.PI * 2f;
        private const float MarkerHeight = 0.1f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private WolfController wolf;
        [SerializeField] private TextMesh label;
        [SerializeField] private Transform lastKnownMarker;

        [Header("State colors")]
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Color patrolColor = new Color(0.55f, 0.55f, 0.6f);
        [SerializeField] private Color chaseColor = new Color(0.55f, 0.05f, 0.1f);
        [SerializeField] private Color investigateColor = new Color(1f, 0.55f, 0.1f);
        [SerializeField] private Color searchColor = new Color(0.95f, 0.85f, 0.2f);

        [Header("Signals")]
        [SerializeField] private LineRenderer signalRing;
        [SerializeField] private float signalDuration = 1.5f;
        [SerializeField] private float howlRingRadius = 12f;
        [SerializeField] private float alertRingRadius = 3f;
        [SerializeField] private int ringSegments = 32;
        [SerializeField] private Color howlColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private Color alertColor = new Color(1f, 0.85f, 0.2f);

        private WolfAwareness _awareness;
        private MaterialPropertyBlock _propertyBlock;
        private string _signalText;
        private float _signalEndTime;
        private Coroutine _ringRoutine;

        private void Awake()
        {
            _awareness = wolf.GetComponent<WolfAwareness>();
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            wolf.StateChanged += OnStateChanged;
            _awareness.Howled += OnHowled;
            _awareness.AlertReceived += OnAlertReceived;
        }

        private void OnDisable()
        {
            wolf.StateChanged -= OnStateChanged;
            _awareness.Howled -= OnHowled;
            _awareness.AlertReceived -= OnAlertReceived;
        }

        private void LateUpdate()
        {
            label.text = BuildLabelText();
            RefreshLastKnownMarker();
        }

        private void OnStateChanged(WolfStateId stateId)
        {
            _propertyBlock.SetColor(BaseColorId, GetColor(stateId));
            bodyRenderer.SetPropertyBlock(_propertyBlock);
        }

        private Color GetColor(WolfStateId stateId)
        {
            switch (stateId)
            {
                case WolfStateId.Chase: return chaseColor;
                case WolfStateId.Investigate: return investigateColor;
                case WolfStateId.Search: return searchColor;
                default: return patrolColor;
            }
        }

        private string BuildLabelText()
        {
            string text = $"{wolf.name} {wolf.CurrentState}";
            if (wolf.Path.HasPath)
                text += " [A*]";
            if (Time.time < _signalEndTime)
                text += "\n" + _signalText;
            return text;
        }

        private void RefreshLastKnownMarker()
        {
            lastKnownMarker.gameObject.SetActive(_awareness.HasLastKnownPosition);
            if (!_awareness.HasLastKnownPosition)
                return;

            Vector3 markerPosition = _awareness.LastKnownPosition;
            markerPosition.y = MarkerHeight;
            lastKnownMarker.position = markerPosition;
        }

        private void OnHowled() => ShowSignal("¡AULLIDO!", howlColor, howlRingRadius);

        private void OnAlertReceived() => ShowSignal("ALERTA RECIBIDA", alertColor, alertRingRadius);

        private void ShowSignal(string text, Color color, float ringRadius)
        {
            _signalText = text;
            _signalEndTime = Time.time + signalDuration;

            if (_ringRoutine != null)
                StopCoroutine(_ringRoutine);
            _ringRoutine = StartCoroutine(ExpandRing(color, ringRadius));
        }

        private IEnumerator ExpandRing(Color color, float maxRadius)
        {
            signalRing.startColor = color;
            signalRing.endColor = color;
            signalRing.positionCount = ringSegments;

            for (float elapsed = 0f; elapsed < signalDuration; elapsed += Time.deltaTime)
            {
                SetRingRadius(maxRadius * elapsed / signalDuration);
                yield return null;
            }

            signalRing.positionCount = 0;
        }

        private void SetRingRadius(float radius)
        {
            for (int i = 0; i < ringSegments; i++)
            {
                float angle = FullCircleRadians * i / ringSegments;
                signalRing.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }
        }
    }
}
