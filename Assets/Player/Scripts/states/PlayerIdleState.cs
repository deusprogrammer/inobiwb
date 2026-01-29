using UnityEngine;

[CreateAssetMenu(fileName = "PlayerIdleState", menuName = "States/Player Idle State")]
public class PlayerIdleState : HomeBoyState
{
    public override void OnEvent(string eventName, GameObjectStateController controller)
    {
        // Handle events if needed
    }
    
    public override void Tick(float deltaTime, GameObjectStateController controller)
    {
        // Targeting now handled by HomeBoyStateController.UpdateTargetedBlock()
    }
    
    public override void OnMove()
    {
        // Transition to moving state
        // State transition handled by HomeBoyStateController
        // Note: stateManager is still set in Initialize, safe to use for state transitions
        this.stateManager.ChangeState(HomeBoyStates.MOVING);
    }

    public override void OnPush()
    {
        // Transition to pushing state
        // State transition handled by HomeBoyStateController
        this.stateManager.ChangeState(HomeBoyStates.PUSHING);
    }
}