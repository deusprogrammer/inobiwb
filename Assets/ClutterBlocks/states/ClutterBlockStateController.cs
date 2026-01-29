using UnityEngine;

public enum ClutterBlockType
{
    Trash,
    Dishes,
    Laundry
}

public class ClutterBlockStateController : GameObjectStateController
{
    private static int nextID = 1;
    private int blockID;
    
    [Header("Block Properties")]
    public ClutterBlockType blockType = ClutterBlockType.Trash;
    public int level = 1;
    public bool isImmovable = false; // For furniture like chairs that cannot be pushed
    
    [Header("Collision Settings")]
    public Vector3 colliderSize = new Vector3(0.8f, 0.8f, 0.8f);
    
    public Vector2 movementDirection;
    public float movementAmount;
    
    private bool isTargeted = false;
    private Color originalColor = Color.white;
    private Vector3 originalScale;
    private TextMesh countText;

    // Removed singleton Instance - each block is independent

    public override void OnStart()
    {
        // Assign unique ID
        blockID = nextID++;
        gameObject.name = $"ClutterBlock_{blockID}_{blockType}_L{level}";
        Debug.Log($"[Block {blockID}] Initialized at {transform.position}");
        
        // Store original scale
        originalScale = transform.localScale;
        
        // Store original color and set based on type
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Immovable blocks get special color
            if (isImmovable)
            {
                renderer.material.color = Color.yellow;
            }
            else
            {
                // Set color based on block type
                switch (blockType)
                {
                    case ClutterBlockType.Trash:
                        renderer.material.color = Color.black;
                        break;
                    case ClutterBlockType.Laundry:
                        renderer.material.color = new Color(0.5f, 0.8f, 1.0f); // Light blue
                        break;
                    case ClutterBlockType.Dishes:
                        renderer.material.color = Color.white;
                        break;
                }
            }
            originalColor = renderer.material.color;
        }
        
        // Create text display for absorbed count (only for movable blocks)
        if (!isImmovable)
        {
            CreateCountText();
            UpdateCountText();
        }
        
        ChangeState(ClutterBlockStates.IDLE);
    }
    
    private void CreateCountText()
    {
        // Create a child GameObject for the text
        GameObject textObj = new GameObject("CountText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = new Vector3(0, 1.5f, 0); // Position above the block
        
        // Add TextMesh component
        countText = textObj.AddComponent<TextMesh>();
        countText.anchor = TextAnchor.MiddleCenter;
        countText.alignment = TextAlignment.Center;
        countText.fontSize = 50;
        countText.color = Color.white;
        countText.characterSize = 0.1f;
    }
    
    public void UpdateCountText()
    {
        if (countText != null)
        {
            countText.text = level.ToString();
        }
    }

    public override void OnUpdate(float deltaTime)
    {
        
    }

    public override void OnChangeState(string oldStateName, string newStateName)
    {
        
    }

    public void Push(Vector2 direction)
    {
        movementDirection = direction;
        Debug.Log($"[Block {blockID}] Push called - Position: {transform.position}, Direction: {direction}, Current State: {currentState?.GetType().Name}");
        ((ClutterBlockState)currentState)?.OnEvent("push", this);
    }
    
    public void SetTargeted(bool targeted)
    {
        if (isTargeted == targeted) return;
        
        isTargeted = targeted;
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            if (targeted)
            {
                // Make it VERY visible with bright emission and scale increase
                renderer.material.EnableKeyword("_EMISSION");
                // Use bright white emission for high visibility
                renderer.material.SetColor("_EmissionColor", Color.white * 2f);
                
                // Scale up slightly
                transform.localScale = originalScale * 1.15f;
            }
            else
            {
                // Remove emission and restore scale
                renderer.material.DisableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", Color.black);
                transform.localScale = originalScale;
            }
        }
    }
    
    public bool IsTargeted()
    {
        return isTargeted;
    }
    
    void OnDestroy()
    {
        // Unregister from grid when destroyed
        if (GridManager.Instance != null)
        {
            GridManager.Instance.UnregisterBlock(this);
        }
    }
    
    void OnDrawGizmos()
    {
        // Draw the collider bounds in the editor (visible in Scene view)
        Gizmos.color = isTargeted ? Color.yellow : new Color(0.5f, 0.5f, 0.5f, 0.5f);
        Vector3 drawPos = transform.position;
        if (drawPos.y == 0) drawPos.y = 0.4f; // Ensure it's visible if at ground level
        Gizmos.DrawWireCube(drawPos, colliderSize);
    }
}