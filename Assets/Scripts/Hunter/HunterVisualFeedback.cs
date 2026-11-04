using UnityEngine;
using Wolves;

namespace Hunter
{
    public class HunterVisualFeedback : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private HunterController hunter;
        [SerializeField] private TextMesh label;
        [SerializeField] private LineRenderer wolfLine;

        [Header("State colors")]
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Color searchColor = new Color(0.15f, 0.8f, 0.2f);
        [SerializeField] private Color attackColor = new Color(0.9f, 0.15f, 0.15f);
        [SerializeField] private Color gatherColor = new Color(0.95f, 0.85f, 0.1f);
        [SerializeField] private Color fleeColor = new Color(0.1f, 0.9f, 0.9f);
        [SerializeField] private Color exitColor = new Color(0.9f, 0.2f, 0.8f);

        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable() => hunter.StateChanged += OnStateChanged;
        private void OnDisable() => hunter.StateChanged -= OnStateChanged;

        private void OnStateChanged(HunterStateId stateId)
        {
            bool visible = stateId != HunterStateId.Dead;
            bodyRenderer.enabled = visible;
            label.gameObject.SetActive(visible);

            if (!visible)
                return;

            _propertyBlock.SetColor(BaseColorId, GetColor(stateId));
            bodyRenderer.SetPropertyBlock(_propertyBlock);
        }

        private Color GetColor(HunterStateId stateId)
        {
            switch (stateId)
            {
                case HunterStateId.Attack: return attackColor;
                case HunterStateId.Gather: return gatherColor;
                case HunterStateId.Flee: return fleeColor;
                case HunterStateId.Exit: return exitColor;
                default: return searchColor;
            }
        }

        private void LateUpdate()
        {
            label.text = BuildLabelText();
            RefreshWolfLine();
        }

        private string BuildLabelText()
        {
            HunterGathering gathering = hunter.Gathering;
            string text = $"{hunter.CurrentState} {gathering.Count}/{gathering.RequiredGathers}";
            if (hunter.Path.HasPath)
                text += " [A*]";
            if (hunter.Perception.VisibleWolf != null)
                text += "\n¡LOBO!";
            if (hunter.Navigator.ChosenSafeZone != null)
                text += $"\n→ {hunter.Navigator.ChosenSafeZone.name}";
            return text;
        }

        private void RefreshWolfLine()
        {
            WolfController visibleWolf = hunter.Perception.VisibleWolf;
            if (visibleWolf == null)
            {
                wolfLine.positionCount = 0;
                return;
            }

            wolfLine.positionCount = 2;
            wolfLine.SetPosition(0, transform.position);
            wolfLine.SetPosition(1, visibleWolf.transform.position);
        }
    }
}
