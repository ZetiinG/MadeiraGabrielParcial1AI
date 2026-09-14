namespace Hunter
{
    public interface IHunterState
    {
        void Enter(HunterController ctx);
        void Tick(HunterController ctx, float deltaTime);
        void Exit(HunterController ctx);
    }
}
