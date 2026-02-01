using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMovingState", menuName = "States/Player Moving State")]
public class PlayerMovingState : HomeBoyState
{
    public float movementDuration = 0.15f; // Duration of movement between grid cells
    private float elapsedTime = 0f;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3Int targetGridPos;
    private bool isMoving = false;
    
    public override void OnEvent(string eventName, GameObjectStateController controller)
    {
        // Handle events for MovingState if needed
    }

    public override void EnterState(GameObjectStateController controller)
    {
        HomeBoyStateController homeBoyController = (HomeBoyStateController)controller;
        
        // Calculate target grid position
        Vector3Int currentGridPos = homeBoyController.GridPosition;
        Vector3Int direction = homeBoyController.GridDirection;
        targetGridPos = currentGridPos + direction;
        
        Debug.Log($"[Movement] Attempting to move from {currentGridPos} to {targetGridPos} (direction: {direction})");
        
        // Check if target position is blocked by a clutter block
        Vector3 targetWorldPos = GridManager.GridToWorld(targetGridPos);
        ClutterBlockStateController blockAtTarget = GridManager.Instance.GetBlockAt(targetWorldPos);
        
        if (blockAtTarget == null)
        {
            // Target is empty, start movement
            startPosition = homeBoyController.transform.position;
            targetPosition = targetWorldPos;
            targetPosition.y = homeBoyController.transform.position.y; // Keep Y position
            
            elapsedTime = 0f;
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
        
        // Update elapsed time
        elapsedTime += deltaTime;
        
        if (elapsedTime >= movementDuration)
        {
            // Movement complete
            homeBoyController.RigidBody.MovePosition(targetPosition);
            homeBoyController.UpdateGridPosition(targetGridPos);
            
            Debug.Log($"[Movement] Completed movement to {targetGridPos}");
            
            // Transition back to idle
            controller.ChangeState(HomeBoyStates.IDLE);
        }
        else
        {
            // Interpolate position
            float t = elapsedTime / movementDuration;
            // Use ease-out for smoother feel
            t = 1f - (1f - t) * (1f - t);
            
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, t);
            homeBoyController.RigidBody.MovePosition(newPosition);
        }
    }
    
    public override void OnMove()
    {
        // Update movement direction
        // Movement applied in Tick()
    }

    public override void OnPush()
    {
        this.stateManager.ChangeState(HomeBoyStates.PUSHING);
    }
}