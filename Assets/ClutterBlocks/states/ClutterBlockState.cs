using UnityEngine;

public class ClutterBlockStates
{
    public static string IDLE = "Idle";
    public static string PUSHED = "Pushed";
    public static string FADING = "Fading";
}

public abstract class ClutterBlockState : GameObjectState
{
    public void Initialize(ClutterBlockStateController stateManager, float duration, int cooldown)
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
}