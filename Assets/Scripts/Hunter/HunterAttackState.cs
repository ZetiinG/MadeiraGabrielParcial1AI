namespace Hunter
{
    public class HunterAttackState : IHunterState
    {
        public void Enter(HunterController ctx)
        {
            ctx.SetBodyMaterial(ctx.AttackMaterial);
        }

        public void Tick(HunterController ctx, float deltaTime)
        {
            if (ctx.CurrentTarget != null && ctx.CurrentTarget.IsDead)
            {
                ctx.TransitionTo(ctx.GatherState, "Gather");
                return;
            }

            if (ctx.CurrentTarget == null || ctx.DistanceToTarget() > ctx.VisionRange)
                ctx.CurrentTarget = ctx.FindLivingBoidInRange();

            if (ctx.CurrentTarget == null)
            {
                ctx.TransitionTo(ctx.PatrolState, "Patrol");
                return;
            }

            float distance = ctx.DistanceToTarget();

            if (distance > ctx.MeleeStopRadius)
                ctx.PursueTarget(ctx.CurrentTarget);
            else
                ctx.Stop();

            if (!ctx.AttackReady)
                return;

            if (distance <= ctx.MeleeAttackRadius)
                ctx.CurrentTarget.TakeDamage(ctx.MeleeDamage);
            else if (distance <= ctx.RangeAttackRadius)
                ctx.FireRangedAttack(ctx.CurrentTarget);
            else
                return;

            ctx.ResetAttackTimer();

            if (ctx.CurrentTarget.IsDead)
                ctx.TransitionTo(ctx.GatherState, "Gather");
        }

        public void Exit(HunterController ctx) { }
    }
}
