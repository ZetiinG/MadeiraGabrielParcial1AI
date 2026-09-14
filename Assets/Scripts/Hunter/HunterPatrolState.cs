namespace Hunter
{
    public class HunterPatrolState : IHunterState
    {
        public void Enter(HunterController ctx)
        {
            ctx.CurrentTarget = null;
            ctx.SetBodyMaterial(ctx.PatrolMaterial);
        }

        public void Tick(HunterController ctx, float deltaTime)
        {
            ctx.TickPOISpawning(deltaTime);

            var waypoint = ctx.CurrentWaypoint;
            if (waypoint != null)
            {
                ctx.ArrivePoint(waypoint.position, 2f);
                if (ctx.HasReachedWaypoint())
                    ctx.AdvanceWaypoint();
            }

            var deadBoid = ctx.FindDeadBoidInRange();
            if (deadBoid != null)
            {
                ctx.CurrentTarget = deadBoid;
                ctx.TransitionTo(ctx.GatherState, "Gather");
                return;
            }

            if (ctx.AttackReady)
            {
                var livingBoid = ctx.FindLivingBoidInRange();
                if (livingBoid != null)
                {
                    ctx.CurrentTarget = livingBoid;
                    ctx.TransitionTo(ctx.AttackState, "Attack");
                }
            }
        }

        public void Exit(HunterController ctx) { }
    }
}
