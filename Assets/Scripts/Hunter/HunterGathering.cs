using UnityEngine;

namespace Hunter
{
    public class HunterGathering : MonoBehaviour
    {
        [SerializeField] private int requiredGathers = 3;
        [SerializeField] private float gatherDuration = 2f;
        [SerializeField] private float interactRadius = 1.2f;
        [SerializeField] private float slowingRadius = 2f;

        public int Count { get; private set; }
        public int RequiredGathers => requiredGathers;
        public bool HasRequiredGathers => Count >= requiredGathers;
        public float GatherDuration => gatherDuration;
        public float InteractRadius => interactRadius;
        public float SlowingRadius => slowingRadius;

        public void Add() => Count++;
        public void ResetCount() => Count = 0;
    }
}
