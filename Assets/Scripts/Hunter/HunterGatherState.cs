namespace Hunter
{
    public class HunterGatherState : IHunterState
    {
        private float _progress;

        public void Enter(HunterController ctx)
        {
            _progress = 0f;
            ctx.SetBodyMaterial(ctx.GatherMaterial);
        }

        public void Tick(HunterController ctx, float deltaTime)
        {
            if (ctx.CurrentTarget == null || !ctx.CurrentTarget.IsDead)
            {
                ctx.TransitionTo(ctx.PatrolState, "Patrol");
                return;
            }

            ctx.ArrivePoint(ctx.CurrentTarget.transform.position, 2f);

            if (ctx.DistanceToTarget() > ctx.GatherInteractRadius)
                return;

            ctx.Stop();
            _progress += deltaTime;
            if (_progress >= ctx.GatherDuration)
            {
                ctx.CurrentTarget.OnGathered();
                ctx.TransitionTo(ctx.PatrolState, "Patrol");
            }
        }

        public void Exit(HunterController ctx) => _progress = 0f;
    }
}
