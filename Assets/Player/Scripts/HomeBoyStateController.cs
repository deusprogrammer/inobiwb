using UnityEngine;
using UnityEngine.InputSystem;

public class HomeBoyStates
{
    public static string IDLE = "Idle";
    public static string MOVING = "Moving";
    public static string PUSHING = "Pushing";    
}

public class HomeBoyStateController : GameObjectStateController
{
    private Vector2 moveInput;
    public Vector2 MoveInput { get { return moveInput; } }

    private int currentDirection = 0;
    public int CurrentDirection { get { return currentDirection; } }
    
    private Vector3 lastFacingDirection = Vector3.forward;
    public Vector3 LastFacingDirection { get { return lastFacingDirection; } }

    private new Collider collider;
    public Collider Collider { get { return collider; } }
    private Rigidbody rigidBody;
    public Rigidbody RigidBody { get { return rigidBody; } }
    
    private ClutterBlockStateController targetedBlock = null;
    public ClutterBlockStateController TargetedBlock { get { return targetedBlock; } }

    public new static HomeBoyStateController Instance { get; private set; }

    public override void OnStart()
    {
        collider = GetComponent<Collider>();
        rigidBody = GetComponent<Rigidbody>();
        ChangeState(HomeBoyStates.IDLE);
    }

    public override void OnUpdate(float deltaTime)
    {
        // Update last facing direction based on current rotation
        lastFacingDirection = transform.forward;
        
        // Update targeted block
        UpdateTargetedBlock();
    }
    
    private void UpdateTargetedBlock()
    {
        // Get player's grid position and facing direction
        Vector3Int playerGridPos = GridManager.WorldToGrid(transform.position);
        Vector3 forward = lastFacingDirection;
        
        // Calculate target grid position (one space in front)
        Vector3Int targetGridPos = playerGridPos + new Vector3Int(
            Mathf.RoundToInt(forward.x),
            0,
            Mathf.RoundToInt(forward.z)
        );
        
        // Get block at target grid position
        ClutterBlockStateController newTargetBlock = GridManager.Instance.GetBlockAt(GridManager.GridToWorld(targetGridPos));
        
        // Update targeting
        if (targetedBlock != newTargetBlock)
        {
            // Untarget old block
            if (targetedBlock != null)
            {
                targetedBlock.SetTargeted(false);
            }
            
            // Target new block
            targetedBlock = newTargetBlock;
            if (targetedBlock != null)
            {
                targetedBlock.SetTargeted(true);
            }
        }
    }

    public override void OnChangeState(string oldStateName, string newStateName)
    {
        
    }

    void OnMove(InputValue inputValue) 
    {
        moveInput = inputValue.Get<Vector2>();

        if (moveInput.x < 0)
        {
            currentDirection = -1;
        }
        else if (moveInput.x > 0)
        {
            currentDirection = 1;
        }
        else
        {
            currentDirection = 0;
        }

        ((HomeBoyState)currentState)?.OnMove();
    }

    void OnPush()
    {
        ((HomeBoyState)currentState)?.OnPush();
    }
    
    void OnDrawGizmos()
    {
        if (GridManager.Instance == null) return;
        
        // Draw player grid position and facing direction
        Gizmos.color = Color.green;
        Vector3Int gridPos = GridManager.WorldToGrid(transform.position);
        Gizmos.DrawWireCube(GridManager.GridToWorld(gridPos), Vector3.one * 0.95f);
        
        // Draw target grid position
        if (lastFacingDirection.magnitude > 0.1f)
        {
            Vector3Int targetGridPos = gridPos + new Vector3Int(
                Mathf.RoundToInt(lastFacingDirection.x),
                0,
                Mathf.RoundToInt(lastFacingDirection.z)
            );
            Gizmos.color = targetedBlock != null ? Color.yellow : Color.cyan;
            Gizmos.DrawWireCube(GridManager.GridToWorld(targetGridPos), Vector3.one * 0.9f);
        }
    }
}
