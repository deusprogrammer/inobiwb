using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMovingState", menuName = "States/Player Moving State")]
public class PlayerMovingState : HomeBoyState
{
    public override void OnEvent(string eventName, GameObjectStateController controller)
    {
        // Handle events for MovingState if needed
    }

    public override void Tick(float deltaTime, GameObjectStateController controller)
    {
        HomeBoyStateController homeBoyController = (HomeBoyStateController)controller;
        Vector2 moveInput = homeBoyController.MoveInput;

        if (moveInput.sqrMagnitude == 0)
        {
            // No movement input, transition to Idle state
            controller.ChangeState(HomeBoyStates.IDLE);
            return;
        }
        
        // Lock movement to 4 cardinal directions (up, down, left, right)
        Vector2 lockedInput;
        if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
        {
            // Horizontal movement dominates
            lockedInput = new Vector2(Mathf.Sign(moveInput.x), 0);
        }
        else
        {
            // Vertical movement dominates
            lockedInput = new Vector2(0, Mathf.Sign(moveInput.y));
        }
        
        // Use world-space movement (not camera-relative) for strict grid alignment
        Vector3 movement = new Vector3(lockedInput.x, 0, lockedInput.y);
        
        // Check if we're trying to cross into a new grid cell
        Vector3 currentPos = homeBoyController.RigidBody.position;
        Vector3 nextPos = currentPos + 5f * deltaTime * movement;
        
        Vector3Int currentGridPos = GridManager.WorldToGrid(currentPos);
        Vector3Int nextGridPos = GridManager.WorldToGrid(nextPos);
        
        // Only check collision if we're moving to a different grid cell
        if (currentGridPos != nextGridPos)
        {
            if (GridManager.Instance != null && GridManager.Instance.IsPositionBlocked(GridManager.GridToWorld(nextGridPos)))
            {
                Debug.Log($"Movement blocked - trying to enter occupied grid cell {nextGridPos}");
                // Stop at grid boundary
                return;
            }
        }
        
        // Apply movement on x and z axis
        homeBoyController.RigidBody.MovePosition(nextPos);
        
        // Apply rotation based on movement direction
        if (movement.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            homeBoyController.RigidBody.MoveRotation(targetRotation);
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