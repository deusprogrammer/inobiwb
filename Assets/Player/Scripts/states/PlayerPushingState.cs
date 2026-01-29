using UnityEngine;

[CreateAssetMenu(fileName = "PlayerPushingState", menuName = "States/Player Pushing State")]
public class PlayerPushingState : HomeBoyState
{
    private Color originalColor;

    public override void OnEvent(string eventName, GameObjectStateController controller)
    {
        // Handle events for PushingState if needed
    }

    public override void EnterState(GameObjectStateController controller)
    {
        // Change cube color to red when entering pushing state
        HomeBoyStateController homeBoyController = (HomeBoyStateController)controller;
        Renderer renderer = homeBoyController.GetComponent<Renderer>();
        if (renderer != null)
        {
            originalColor = renderer.material.color;
            renderer.material.color = Color.red;
            Debug.Log("Entering Pushing State - color changed to red");
        }
        
        // Push the targeted block if there is one
        if (homeBoyController.TargetedBlock != null)
        {
            Vector3 forward = homeBoyController.LastFacingDirection;
            Vector2 pushDirection = new Vector2(forward.x, forward.z).normalized;
            
            // Check if push is valid (target position empty or same type)
            Vector3 blockPosition = homeBoyController.TargetedBlock.transform.position;
            Vector3 targetPosition = blockPosition + new Vector3(pushDirection.x, 0, pushDirection.y) * 1.0f;
            
            if (CanPushToPosition(homeBoyController.TargetedBlock, targetPosition))
            {
                Debug.Log($"Pushing targeted block {homeBoyController.TargetedBlock.gameObject.name} in direction {pushDirection}");
                homeBoyController.TargetedBlock.Push(pushDirection);
            }
            else
            {
                Debug.Log("Cannot push - target position blocked by dissimilar block type");
            }
        }
        else
        {
            Debug.Log("No targeted block to push");
        }
    }
    
    private bool CanPushToPosition(ClutterBlockStateController blockToPush, Vector3 targetPosition)
    {
        // Use GridManager for efficient lookup
        return GridManager.Instance.CanPushBlock(blockToPush, targetPosition);
    }

    public override void OnComplete(GameObjectStateController controller)
    {
        controller.ChangeState(HomeBoyStates.IDLE);
    }

    public override void ExitState(GameObjectStateController controller)
    {
        // Restore original color when leaving pushing state
        HomeBoyStateController homeBoyController = (HomeBoyStateController)controller;
        Renderer renderer = homeBoyController.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = originalColor;
        }
    }

    public override void OnMove()
    {
        // Continue pushing with updated direction
        // Movement applied in HomeBoyStateController.OnUpdate()
    }

    public override void OnPush()
    {
        // Transition back to idle or moving state when push ends
        // State transition handled by HomeBoyStateController
    }
}