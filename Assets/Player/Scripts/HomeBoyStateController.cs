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
    [Header("Partner System")]
    public HomeBoyStateController partnerController;
    public bool isActive = true;
    
    [Header("Push Restrictions")]
    [Tooltip("Which clutter block types this player can push")]
    public ClutterBlockType[] allowedPushTypes = { ClutterBlockType.Trash, ClutterBlockType.Laundry, ClutterBlockType.Dishes };
    
    private PlayerInput playerInput;
    private Vector2 moveInput;
    public Vector2 MoveInput { get { return moveInput; } }

    // Grid-based position and direction
    private Vector3Int gridPosition;
    public Vector3Int GridPosition { get { return gridPosition; } }
    
    private Vector3Int gridDirection = new Vector3Int(0, 0, 1); // Start facing forward (positive Z)
    public Vector3Int GridDirection { get { return gridDirection; } }

    private new Collider collider;
    public Collider Collider { get { return collider; } }
    private Rigidbody rigidBody;
    public Rigidbody RigidBody { get { return rigidBody; } }
    
    private ClutterBlockStateController targetedBlock = null;
    public ClutterBlockStateController TargetedBlock { get { return targetedBlock; } }
    
    public ClutterBlockType[] AllowedPushTypes { get { return allowedPushTypes; } }

    // Visual indicator for targeted grid cell
    private GameObject targetIndicator;
    private Vector3Int currentTargetGridPos;

    // Note: Removed singleton Instance - each player is independent
    
    void Awake()
    {
        // Override base class Awake to prevent singleton behavior
        // Each player needs to be independent
        
        // CRITICAL: Instantiate copies of ScriptableObject states so each player has its own
        // Otherwise both players share the same state objects and stateManager gets overwritten
        for (int i = 0; i < gameObjectStates.Length; i++)
        {
            gameObjectStates[i].gameObjectState = Instantiate(gameObjectStates[i].gameObjectState);
        }
        
        Debug.Log($"[{gameObject.name}] Created unique state instances");
    }

    public override void OnStart()
    {
        collider = GetComponent<Collider>();
        rigidBody = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
        
        // Keep PlayerInput always enabled - we gate input with isActive flag instead
        
        // Initialize grid position from world position
        gridPosition = GridManager.WorldToGrid(transform.position);
        Debug.Log($"[{gameObject.name}] Starting at grid position: {gridPosition}, isActive: {isActive}");
        
        CreateTargetIndicator();
        ChangeState(HomeBoyStates.IDLE);
    }
    
    void OnDestroy()
    {
        if (targetIndicator != null)
        {
            Destroy(targetIndicator);
        }
    }
    
    private void CreateTargetIndicator()
    {
        // Create a quad on the ground to show targeted grid cell
        targetIndicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
        targetIndicator.name = "TargetIndicator";
        
        // Remove the collider so it doesn't interfere
        Destroy(targetIndicator.GetComponent<Collider>());
        
        // Rotate to lie flat on the ground (pointing up)
        targetIndicator.transform.rotation = Quaternion.Euler(90, 0, 0);
        targetIndicator.transform.localScale = new Vector3(0.9f, 0.9f, 1f); // Slightly smaller than grid cell
        
        // Set material color (semi-transparent yellow)
        Renderer renderer = targetIndicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(1f, 1f, 0f, 0.5f); // Yellow, semi-transparent
            
            // Enable transparency
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0); // Alpha blend
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3001;
            
            renderer.material = mat;
        }
        
        // Start hidden
        targetIndicator.SetActive(false);
    }

    public override void OnUpdate(float deltaTime)
    {
        // Only update targeting for active player
        if (isActive)
        {
            UpdateTargetedBlock();
        }
    }
    
    private void UpdateTargetedBlock()
    {
        // Safety check
        if (GridManager.Instance == null)
        {
            return;
        }
        
        // Calculate target grid position (one space in current direction)
        Vector3Int targetGridPos = gridPosition + gridDirection;
        Vector3 targetWorldPos = GridManager.GridToWorld(targetGridPos);
        
        Debug.Log($"[Targeting] Player at grid: {gridPosition}, facing direction: {gridDirection}, target grid: {targetGridPos}, target world: {targetWorldPos}");
        
        // Update visual indicator position (disabled)
        currentTargetGridPos = targetGridPos;
        /*
        if (targetIndicator != null)
        {
            targetIndicator.SetActive(true);
            // Position at ground level (y=0.01 to avoid z-fighting)
            targetIndicator.transform.position = new Vector3(targetWorldPos.x, 0.01f, targetWorldPos.z);
        }
        */
        
        // Get block at target grid position
        ClutterBlockStateController newTargetBlock = GridManager.Instance.GetBlockAt(targetWorldPos);
        Debug.Log($"[Targeting] Block at target: {(newTargetBlock != null ? newTargetBlock.gameObject.name : "NULL")}");
        
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

    public void UpdateGridPosition(Vector3Int newGridPos)
    {
        gridPosition = newGridPos;
        Debug.Log($"[Player] Moved to grid position: {gridPosition}");
    }
    
    public override void OnChangeState(string oldStateName, string newStateName)
    {
        
    }

    public void HandleMove(InputValue inputValue) 
    {
        Debug.Log($"[{gameObject.name}] HandleMove called, isActive: {isActive}");
        
        if (!isActive)
        {
            Debug.Log($"[{gameObject.name}] HandleMove BLOCKED - not active");
            return;
        }
        
        moveInput = inputValue.Get<Vector2>();
        Debug.Log($"[{gameObject.name}] HandleMove processing input: {moveInput}, currentState: {CurrentStateName}");

        // Update grid direction based on input (lock to cardinal directions)
        if (moveInput.sqrMagnitude > 0)
        {
            if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
            {
                // Horizontal movement (left/right)
                gridDirection = new Vector3Int((int)Mathf.Sign(moveInput.x), 0, 0);
            }
            else
            {
                // Vertical movement (up/down)
                // Input Y positive = up = negative Z (rows decrease)
                // Input Y negative = down = positive Z (rows increase)
                gridDirection = new Vector3Int(0, 0, -(int)Mathf.Sign(moveInput.y));
            }
        }

        ((HomeBoyState)currentState)?.OnMove();
    }

    public void HandlePush()
    {
        Debug.Log($"[{gameObject.name}] HandlePush called, isActive: {isActive}");
        
        if (!isActive)
        {
            Debug.Log($"[{gameObject.name}] HandlePush BLOCKED - not active");
            return;
        }
        
        ((HomeBoyState)currentState)?.OnPush();
    }
    
    public void HandleLook(InputValue inputValue)
    {
        if (!isActive) return;
        
        Vector2 lookInput = inputValue.Get<Vector2>();
        
        // Find and tell the camera to rotate
        CameraController camera = FindFirstObjectByType<CameraController>();
        if (camera != null)
        {
            camera.RotateCamera(lookInput);
        }
    }
    
    void OnCycleCamera()
    {
        // Removed - no longer using camera cycling
    }
    
    void OnDrawGizmos()
    {
        if (GridManager.Instance == null) return;
        
        // Draw player grid position
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(GridManager.GridToWorld(gridPosition), Vector3.one * 0.95f);
        
        // Draw target grid position based on current direction
        Vector3Int targetGridPos = gridPosition + gridDirection;
        Gizmos.color = targetedBlock != null ? Color.yellow : Color.cyan;
        Gizmos.DrawWireCube(GridManager.GridToWorld(targetGridPos), Vector3.one * 0.9f);
    }
}
