using UnityEngine;
using POI;

namespace Hunter
{
    public class HunterVisualFeedback : MonoBehaviour
    {
        [SerializeField] private HunterController hunter;
        [SerializeField] private TextMesh label;

        private void LateUpdate()
        {
            if (Camera.main != null)
                label.transform.forward = Camera.main.transform.forward;

            string targetName = hunter.CurrentTarget != null ? hunter.CurrentTarget.name : "-";
            label.text = $"{hunter.CurrentStateName}\nTarget: {targetName}";
        }

        private void OnGUI()
        {
            string targetName = hunter.CurrentTarget != null ? hunter.CurrentTarget.name : "-";
            GUI.Box(new Rect(10, 10, 220, 90), "Hunter Debug");
            GUI.Label(new Rect(20, 35, 200, 20), $"Estado: {hunter.CurrentStateName}");
            GUI.Label(new Rect(20, 55, 200, 20), $"Objetivo: {targetName}");
            GUI.Label(new Rect(20, 75, 200, 20), $"POIs activos: {POIManager.Count}");
        }
    }
}
