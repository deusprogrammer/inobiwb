using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMovingState", menuName = "States/Player Moving State")]
public class PlayerMovingState : HomeBoyState
{
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3Int targetGridPos;
    private bool isMoving = false;
    
    public override void OnEvent(string eventName, GameObjectStateController controller)
    {
        // Handle events for MovingState if needed
    }

    public override void OnPush()
    {
        // Block push input while moving - no state transition
        Debug.Log("[PlayerMovingState] Push blocked - cannot push while moving");
        // Do nothing - stay in MOVING state
    }

    public override void EnterState(GameObjectStateController controller)
    {
        HomeBoyStateController homeBoyController = (HomeBoyStateController)controller;
        
        // Calculate target grid position
        Vector3Int currentGridPos = homeBoyController.GridPosition;
        Vector3Int direction = homeBoyController.GridDirection;
        targetGridPos = currentGridPos + direction;
        
        Debug.Log($"[Movement] Attempting to move from {currentGridPos} to {targetGridPos} (direction: {direction})");
        
        // Check if target position is within bounds
        if (!GridManager.IsWithinBounds(targetGridPos))
        {
            Debug.Log($"[Movement] Target position {targetGridPos} is out of bounds");
            controller.ChangeState(HomeBoyStates.IDLE);
            return;
        }
        
        // Check if target position is blocked by a clutter block
        Vector3 targetWorldPos = GridManager.GridToWorld(targetGridPos);
        ClutterBlockStateController blockAtTarget = GridManager.Instance.GetBlockAt(targetWorldPos);
        
        if (blockAtTarget == null)
        {
            // Target is empty, start movement
            startPosition = homeBoyController.transform.position;
            targetPosition = targetWorldPos;
            targetPosition.y = homeBoyController.transform.position.y; // Keep Y position
            
            isMoving = true;
            
            // Update rotation to face movement direction
            if (direction.x != 0 || direction.z != 0)
            {
                Vector3 lookDir = new Vector3(direction.x, 0, direction.z);
                Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                homeBoyController.RigidBody.MoveRotation(targetRotation);
            }
            
            Debug.Log($"[Movement] Starting smooth movement from {startPosition} to {targetPosition}");
        }
        else
        {
            // Blocked, immediately transition back to idle
            Debug.Log($"[Movement] Blocked by {blockAtTarget.gameObject.name} at {targetGridPos}");
            controller.ChangeState(HomeBoyStates.IDLE);
        }
    }
    
    public override void Tick(float deltaTime, GameObjectStateController controller)
    {
        HomeBoyStateController homeBoyController = (HomeBoyStateController)controller;
        
        if (!isMoving)
        {
            // No movement in progress, check input
            Vector2 moveInput = homeBoyController.MoveInput;
            if (moveInput.sqrMagnitude == 0)
            {
                // No input, transition to Idle
                controller.ChangeState(HomeBoyStates.IDLE);
            }
            return;
        }
        
        // Use base class elapsedTime (managed automatically)
        float progress = this.elapsed / duration;
        
        if (progress >= 1f)
        {
            // Movement complete - snap to target
            homeBoyController.RigidBody.MovePosition(targetPosition);
            homeBoyController.UpdateGridPosition(targetGridPos);
            
            Debug.Log($"[Movement] Completed movement to {targetGridPos}");
            
            // Transition back to idle
            controller.ChangeState(HomeBoyStates.IDLE);
        }
        else
        {
            // Interpolate position using ease-out
            float t = 1f - (1f - progress) * (1f - progress);
            
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, t);
            homeBoyController.RigidBody.MovePosition(newPosition);
        }
    }
    
    public override void OnMove()
    {
        Debug.Log($"[PlayerMovingState] OnMove called - already moving, updating direction");
        // Update movement direction
        // Movement applied in Tick()
    }
}