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
        return new Vector3Int(
            Mathf.RoundToInt(worldPosition.x),
            Mathf.RoundToInt(worldPosition.y),
            Mathf.RoundToInt(worldPosition.z)
        );
    }
    
    public static Vector3 GridToWorld(Vector3Int gridPosition)
    {
        return new Vector3(gridPosition.x, gridPosition.y, gridPosition.z);
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
        return grid.ContainsKey(gridPos) ? grid[gridPos] : null;
    }
    
    public bool IsPositionBlocked(Vector3 worldPosition)
    {
        return GetBlockAt(worldPosition) != null;
    }
    
    public bool CanPushBlock(ClutterBlockStateController blockToPush, Vector3 targetWorldPosition)
    {
        // Cannot push immovable blocks
        if (blockToPush.isImmovable)
        {
            return false;
        }
        
        ClutterBlockStateController targetBlock = GetBlockAt(targetWorldPosition);
        
        if (targetBlock == null)
        {
            return true; // Empty space, can push
        }
        
        // Can only push into same type
        return targetBlock.blockType == blockToPush.blockType;
    }
}
