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
            Vector3Int direction = homeBoyController.GridDirection;
            // Invert Z for push direction to match world space
            Vector2 pushDirection = new Vector2(direction.x, -direction.z);
            
            // Check if push is valid (target position empty or same type)
            Vector3 blockPosition = homeBoyController.TargetedBlock.transform.position;
            Vector3Int blockGridPos = GridManager.WorldToGrid(blockPosition);
            Vector3Int targetGridPos = blockGridPos + direction;
            Vector3 targetPosition = GridManager.GridToWorld(targetGridPos);
            
            Debug.Log($"[Push] Attempting to push block '{homeBoyController.TargetedBlock.gameObject.name}' from grid {blockGridPos} to {targetGridPos} (world: {targetPosition}), direction: {direction}");
            
            if (CanPushToPosition(homeBoyController.TargetedBlock, targetPosition))
            {
                Debug.Log($"[Push] Push allowed! Executing push...");
                homeBoyController.TargetedBlock.Push(pushDirection);
            }
            else
            {
                Debug.Log($"[Push] Cannot push - target position blocked or immovable");
            }
        }
        else
        {
            Debug.Log("[Push] No targeted block to push");
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