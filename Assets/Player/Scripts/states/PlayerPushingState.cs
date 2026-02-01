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
        Debug.Log($"[PlayerPushingState] EnterState called for {controller.gameObject.name}");
        
        // Change cube color to red when entering pushing state
        HomeBoyStateController homeBoyController = (HomeBoyStateController)controller;
        Renderer renderer = homeBoyController.GetComponent<Renderer>();
        if (renderer != null)
        {
            originalColor = renderer.material.color;
            renderer.material.color = Color.red;
            Debug.Log($"[PlayerPushingState] {controller.gameObject.name} - color changed to red");
        }
        
        // Push the targeted block if there is one
        if (homeBoyController.TargetedBlock != null)
        {
            ClutterBlockStateController block = homeBoyController.TargetedBlock;
            Vector3Int direction = homeBoyController.GridDirection;
            // Invert Z for push direction to match world space
            Vector2 pushDirection = new Vector2(direction.x, -direction.z);
            
            // Check if push is valid (target position empty or same type)
            Vector3 blockPosition = block.transform.position;
            Vector3Int blockGridPos = GridManager.WorldToGrid(blockPosition);
            Vector3Int targetGridPos = blockGridPos + direction;
            Vector3 targetPosition = GridManager.GridToWorld(targetGridPos);
            
            Debug.Log($"[Push] Attempting to push block '{block.gameObject.name}' from grid {blockGridPos} to {targetGridPos} (world: {targetPosition}), direction: {direction}");
            
            string blockTypeTarget = block.blockType.ToString().ToLower();
            string actor = homeBoyController.actorLabel;
            
            // Check for immovable furniture
            if (block.isImmovable)
            {
                Debug.Log($"[Push] Cannot push - immovable furniture");
                string furnitureTarget = string.IsNullOrEmpty(block.furnitureType) ? "furniture" : block.furnitureType;
                EventBus.Instance.Publish(new GameEvent(EventNames.FurnitureMoveFailure, actor, furnitureTarget));
            }
            // Check if it's a special furniture type that gives an item
            else if (!string.IsNullOrEmpty(block.furnitureType))
            {
                Debug.Log($"[Push] Special furniture - item collected!");
                block.Push(pushDirection, actor);
                EventBus.Instance.Publish(new GameEvent(EventNames.ItemCollected, actor, block.furnitureType));
            }
            // Check if player is allowed to push this type
            else if (!IsBlockTypeAllowed(homeBoyController, block))
            {
                Debug.Log($"[Push] Cannot push - type not allowed by player");
                EventBus.Instance.Publish(new GameEvent(EventNames.WrongBlockPushed, actor, blockTypeTarget));
            }
            else if (CanPushToPosition(homeBoyController, block, targetPosition))
            {
                Debug.Log($"[Push] Push allowed! Executing push...");
                block.Push(pushDirection, actor);
                EventBus.Instance.Publish(new GameEvent(EventNames.BlockPushed, actor, blockTypeTarget));
            }
            else
            {
                // Push blocked by different block type at target
                ClutterBlockStateController targetBlock = GridManager.Instance.GetBlockAt(targetPosition);
                if (targetBlock != null && targetBlock.blockType != block.blockType)
                {
                    Debug.Log($"[Push] Cannot push - different block type at target");
                    EventBus.Instance.Publish(new GameEvent(EventNames.BlocksCombineFailed, actor, blockTypeTarget));
                }
                else
                {
                    Debug.Log($"[Push] Cannot push - unknown reason");
                }
            }
        }
        else
        {
            Debug.Log("[Push] No targeted block to push");
        }
    }
    
    private bool IsBlockTypeAllowed(HomeBoyStateController player, ClutterBlockStateController block)
    {
        foreach (ClutterBlockType allowedType in player.AllowedPushTypes)
        {
            if (block.blockType == allowedType)
            {
                return true;
            }
        }
        return false;
    }
    
    private bool CanPushToPosition(HomeBoyStateController player, ClutterBlockStateController blockToPush, Vector3 targetPosition)
    {
        // Use GridManager for efficient lookup, passing player's allowed push types
        return GridManager.Instance.CanPushBlock(blockToPush, targetPosition, player.AllowedPushTypes);
    }

    public override void OnComplete(GameObjectStateController controller)
    {
        Debug.Log($"[PlayerPushingState] OnComplete called for {controller.gameObject.name}, transitioning to IDLE");
        controller.ChangeState(HomeBoyStates.IDLE);
    }

    public override void ExitState(GameObjectStateController controller)
    {
        Debug.Log($"[PlayerPushingState] ExitState called for {controller.gameObject.name}");
        // Restore original color when leaving pushing state
        HomeBoyStateController homeBoyController = (HomeBoyStateController)controller;
        Renderer renderer = homeBoyController.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = originalColor;
            Debug.Log($"[PlayerPushingState] {controller.gameObject.name} - color restored");
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