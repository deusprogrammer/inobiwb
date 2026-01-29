public class HomeBoyEvents
{
    public const string MOVE = "move";
    public const string PUSH = "push";
}

public abstract class HomeBoyState : GameObjectState
{
    public void Initialize(HomeBoyStateController stateManager, float duration, int cooldown)
    {
        this.stateManager = stateManager;
        this.duration = duration;
        this.cooldown = cooldown;
    }
    
    // Override from base - default implementation
    public override void OnEvent(string eventName, GameObjectStateController controller)
    {
        // Default implementation - can be overridden
    }

    public abstract void OnMove();
    public abstract void OnPush();
}