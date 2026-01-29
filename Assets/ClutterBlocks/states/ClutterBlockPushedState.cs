using UnityEngine;

[CreateAssetMenu(fileName = "ClutterBlockPushedState", menuName = "States/ClutterBlock Pushed State")]
public class ClutterBlockPushedState : ClutterBlockState
{
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float moveProgress = 0f;

    public override void OnEvent(string eventName, GameObjectStateController controller)
    {
        // Handle events if needed
    }

    public override void EnterState(GameObjectStateController controller)
    {
        ClutterBlockStateController blockController = (ClutterBlockStateController)controller;
        Debug.Log($"[Block at {blockController.transform.position}] Entered PUSHED state");
        
        // Use fixed grid size for movement
        blockController.movementAmount = 1.0f;
        
        // Calculate start and target positions
        startPosition = blockController.transform.position;
        Vector3 movement = new Vector3(blockController.movementDirection.x, 0, blockController.movementDirection.y);
        targetPosition = startPosition + movement * blockController.movementAmount;
        
        Debug.Log($"[Block at {startPosition}] Movement setup - Start: {startPosition}, Target: {targetPosition}, Amount: {blockController.movementAmount}, Direction: {blockController.movementDirection}");
        
        moveProgress = 0f;
    }

    public override void Tick(float deltaTime, GameObjectStateController controller)
    {
        ClutterBlockStateController blockController = (ClutterBlockStateController)controller;
        
        Debug.Log($"[Block at {blockController.transform.position}] Tick - Progress: {moveProgress}, DeltaTime: {deltaTime}, Target: {targetPosition}");
        
        // Move towards target position over time
        moveProgress += deltaTime * 2f; // Speed factor of 2 (takes 0.5 seconds to move 1 unit)
        
        if (moveProgress >= 1f)
        {
            // Movement complete
            Vector3 oldPosition = startPosition;
            blockController.transform.position = targetPosition;
            
            // Check for collision BEFORE updating grid (so we can find the other block)
            ClutterBlockStateController otherBlock = GridManager.Instance.GetBlockAt(targetPosition);
            
            // Update GridManager with new position
            GridManager.Instance.MoveBlock(blockController, oldPosition, targetPosition);
            
            // Handle absorption if we found another block
            if (otherBlock != null && otherBlock != blockController && otherBlock.blockType == blockController.blockType)
            {
                // Add levels together (cap at 4)
                int combinedLevel = blockController.level + otherBlock.level;
                blockController.level = Mathf.Min(combinedLevel, 4);
                blockController.UpdateCountText();
                Object.Destroy(otherBlock.gameObject);
            }
            
            // Check if this block should be destroyed (after absorption)
            if (blockController.level >= 4)
            {
                // Transition to fading state
                controller.ChangeState(ClutterBlockStates.FADING);
                return;
            }
            
            controller.ChangeState(ClutterBlockStates.IDLE);
        }
        else
        {
            // Interpolate position
            blockController.transform.position = Vector3.Lerp(startPosition, targetPosition, moveProgress);
        }
    }
}
