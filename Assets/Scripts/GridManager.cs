using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    private static GridManager instance;
    public static GridManager Instance 
    { 
        get 
        {
            if (instance == null)
            {
                Debug.LogError("GridManager.Instance accessed before initialization! LevelLoader should create it first.");
            }
            return instance;
        }
    }
    
    // Dictionary mapping Vector3Int positions to ClutterBlocks
    private Dictionary<Vector3Int, ClutterBlockStateController> grid = new Dictionary<Vector3Int, ClutterBlockStateController>();
    
    // Anchor offset for the level (set by LevelLoader)
    private static Vector3 anchorOffset = Vector3.zero;
    public static Vector3 AnchorOffset { get { return anchorOffset; } set { anchorOffset = value; } }
    
    // Grid dimensions (set by LevelLoader)
    private static int gridRows = 0;
    private static int gridCols = 0;
    public static int GridRows { get { return gridRows; } set { gridRows = value; } }
    public static int GridCols { get { return gridCols; } set { gridCols = value; } }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Debug.Log("GridManager initialized");
        }
        else if (instance != this)
        {
            Debug.LogWarning("Multiple GridManager instances detected! Destroying duplicate.");
            Destroy(gameObject);
        }
    }
    
    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            Debug.Log("GridManager destroyed");
        }
    }
    
    public static Vector3Int WorldToGrid(Vector3 worldPosition)
    {
        float gridSize = 1.0f;
        
        // Remove anchor offset first to get position relative to grid origin
        Vector3 relativePos = worldPosition - anchorOffset;
        
        int gridX = Mathf.FloorToInt(relativePos.x / gridSize);
        int gridY = Mathf.FloorToInt(relativePos.y / gridSize);
        
        // Handle negative Z: convert to positive row index
        int gridZ;
        if (relativePos.z < 0)
        {
            gridZ = Mathf.FloorToInt(-relativePos.z / gridSize);
        }
        else
        {
            gridZ = Mathf.FloorToInt(relativePos.z / gridSize);
        }
        
        return new Vector3Int(gridX, gridY, gridZ);
    }
    
    public static Vector3 GridToWorld(Vector3Int gridPosition)
    {
        float gridSize = 1.0f;
        
        // Convert grid to relative position, then add anchor offset
        Vector3 relativePos = new Vector3(
            (gridPosition.x + 0.5f) * gridSize,  // Center in grid cell
            gridPosition.y, 
            -(gridPosition.z + 0.5f) * gridSize  // Center in grid cell
        );
        
        return relativePos + anchorOffset;
    }
    
    public static bool IsWithinBounds(Vector3Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < gridCols &&
               gridPosition.z >= 0 && gridPosition.z < gridRows;
    }
    
    public void RegisterBlock(ClutterBlockStateController block)
    {
        Vector3Int gridPos = WorldToGrid(block.transform.position);
        grid[gridPos] = block;
        Debug.Log($"Registered block at grid position {gridPos}");
    }
    
    public void UnregisterBlock(ClutterBlockStateController block)
    {
        Vector3Int gridPos = WorldToGrid(block.transform.position);
        if (grid.ContainsKey(gridPos) && grid[gridPos] == block)
        {
            grid.Remove(gridPos);
            Debug.Log($"Unregistered block at grid position {gridPos}");
        }
    }
    
    public void MoveBlock(ClutterBlockStateController block, Vector3 oldPosition, Vector3 newPosition)
    {
        Vector3Int oldGridPos = WorldToGrid(oldPosition);
        Vector3Int newGridPos = WorldToGrid(newPosition);
        
        // Remove from old position
        if (grid.ContainsKey(oldGridPos) && grid[oldGridPos] == block)
        {
            grid.Remove(oldGridPos);
        }
        
        // Add to new position
        grid[newGridPos] = block;
        Debug.Log($"Moved block from {oldGridPos} to {newGridPos}");
    }
    
    public ClutterBlockStateController GetBlockAt(Vector3 worldPosition)
    {
        Vector3Int gridPos = WorldToGrid(worldPosition);
        ClutterBlockStateController block = grid.ContainsKey(gridPos) ? grid[gridPos] : null;
        Debug.Log($"[GridManager] GetBlockAt world:{worldPosition} grid:{gridPos} -> {(block != null ? block.gameObject.name : "NULL")} (total blocks: {grid.Count})");
        return block;
    }
    
    public bool IsPositionBlocked(Vector3 worldPosition)
    {
        return GetBlockAt(worldPosition) != null;
    }
    
    public bool CanPushBlock(ClutterBlockStateController blockToPush, Vector3 targetWorldPosition, ClutterBlockType[] allowedTypes)
    {
        // Cannot push immovable blocks
        if (blockToPush.isImmovable)
        {
            Debug.Log($"[GridManager] CanPushBlock: Block '{blockToPush.gameObject.name}' is immovable");
            return false;
        }
        
        // Check if player is allowed to push this block type
        bool isAllowed = false;
        foreach (ClutterBlockType allowedType in allowedTypes)
        {
            if (blockToPush.blockType == allowedType)
            {
                isAllowed = true;
                break;
            }
        }
        
        if (!isAllowed)
        {
            Debug.Log($"[GridManager] CanPushBlock: Block type '{blockToPush.blockType}' not in allowed push types");
            return false;
        }
        
        ClutterBlockStateController targetBlock = GetBlockAt(targetWorldPosition);
        
        if (targetBlock == null)
        {
            Debug.Log($"[GridManager] CanPushBlock: Target position empty - can push");
            return true; // Empty space, can push
        }
        
        // Can only push into same type
        bool canPush = targetBlock.blockType == blockToPush.blockType;
        Debug.Log($"[GridManager] CanPushBlock: Target has block '{targetBlock.gameObject.name}' (type: {targetBlock.blockType}) vs pushing '{blockToPush.gameObject.name}' (type: {blockToPush.blockType}) -> {canPush}");
        return canPush;
    }
}
