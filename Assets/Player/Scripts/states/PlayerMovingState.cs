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
        
        // Use Physics checks to prevent clipping through blocks
        Vector3 currentPos = homeBoyController.RigidBody.position;
        Vector3 desiredMovement = 5f * deltaTime * movement;
        Vector3 targetPos = currentPos + desiredMovement;
        
        // Get player's collider size
        BoxCollider playerCollider = homeBoyController.GetComponent<BoxCollider>();
        Vector3 halfExtents = playerCollider.size * 0.5f * homeBoyController.transform.localScale.x;
        Vector3 center = playerCollider.center;
        
        // Check if target position would overlap with anything
        Collider[] overlaps = Physics.OverlapBox(
            targetPos + center,
            halfExtents * 0.95f, // Slightly smaller to avoid edge cases
            Quaternion.identity,
            ~0, // Check all layers
            QueryTriggerInteraction.Ignore
        );
        
        // Filter out self
        bool wouldOverlap = false;
        foreach (Collider col in overlaps)
        {
            if (col != playerCollider)
            {
                wouldOverlap = true;
                break;
            }
        }
        
        if (!wouldOverlap)
        {
            // Safe to move
            homeBoyController.RigidBody.MovePosition(targetPos);
        }
        
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