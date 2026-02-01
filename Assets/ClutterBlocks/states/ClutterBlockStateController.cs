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
    public string furnitureType = ""; // Type of furniture (e.g., "chair", "itemChair")
    
    [Header("Visual Models")]
    public GameObject trashModelPrefab;
    public GameObject laundryModelPrefab;
    public GameObject dishesModelPrefab;
    public GameObject chairModelPrefab; // For immovable blocks
    
    [Header("Level Scaling")]
    public float level1Scale = 0.75f;
    public float level2Scale = 1.0f;
    public float level3Scale = 1.25f;
    public float scaleAnimationDuration = 0.3f;
    
    [Header("Collision Settings")]
    public Vector3 colliderSize = new Vector3(0.8f, 0.8f, 0.8f);
    
    public Vector2 movementDirection;
    public float movementAmount;
    
    [HideInInspector]
    public string lastPusher; // Track which player pushed this block
    
    private bool isTargeted = false;
    private Color originalColor = Color.white;
    private Vector3 originalScale;
    private TextMesh countText;
    private GameObject visualModel; // Reference to the spawned visual model
    private Vector3 visualModelOriginalScale; // Store the prefab's original scale
    private Coroutine scaleCoroutine; // Track active scale animation

    // Removed singleton Instance - each block is independent

    public override void OnStart()
    {
        // Assign unique ID
        blockID = nextID++;
        gameObject.name = $"ClutterBlock_{blockID}_{blockType}_L{level}";
        Debug.Log($"[Block {blockID}] Initialized at {transform.position}");
        
        // Store original scale
        originalScale = transform.localScale;
        
        // Instantiate visual model based on block type
        GameObject modelPrefab = null;
        
        if (isImmovable || !string.IsNullOrEmpty(furnitureType))
        {
            modelPrefab = chairModelPrefab;
        }
        else
        {
            modelPrefab = blockType switch
            {
                ClutterBlockType.Trash => trashModelPrefab,
                ClutterBlockType.Laundry => laundryModelPrefab,
                ClutterBlockType.Dishes => dishesModelPrefab,
                _ => null
            };
        }
        
        if (modelPrefab != null)
        {
            visualModel = Instantiate(modelPrefab, transform);
            
            // The parent ClutterBlock is positioned with its center at height objectScale*0.5f
            // This positions the visual model so:
            // - X/Z centered in the grid cell (0, 0 local)
            // - Y offset to put the bottom of the model on the ground
            // Since the parent's center is at objectScale*0.5f, and we want the bottom at y=0,
            // the model should be at local y = -(objectScale*0.5f) if its pivot is at the bottom
            // OR at local y = 0 if its pivot is at the center
            
            // Get the scale from the parent
            float parentScale = transform.localScale.x; // objectScale
            
            // Store the prefab's rotation (already preserved by Instantiate)
            Quaternion prefabRotation = visualModel.transform.localRotation;
            
            // Position the model - assuming pivot is at the bottom of the model
            // Adjust Y to sit on ground: parent center is at height/2, so offset down by -height/2
            float localY = -parentScale * 0.5f;
            visualModel.transform.localPosition = new Vector3(0, localY, 0);
            visualModel.transform.localRotation = prefabRotation; // Restore rotation
            
            // Store the prefab's original scale
            visualModelOriginalScale = visualModel.transform.localScale;
            
            // Apply level scale multiplied by original scale
            float levelScale = GetScaleForLevel(level);
            visualModel.transform.localScale = visualModelOriginalScale * levelScale;
            
            Debug.Log($"[Block {blockID}] Spawned visual model: {modelPrefab.name} at local Y={localY}, scale {visualModel.transform.localScale} (original: {visualModelOriginalScale}, level multiplier: {levelScale})");
        }
        else
        {
            Debug.LogWarning($"[Block {blockID}] No visual model prefab assigned for {(isImmovable ? "chair" : blockType.ToString())}");
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
    
    private float GetScaleForLevel(int lvl)
    {
        return lvl switch
        {
            1 => level1Scale,
            2 => level2Scale,
            3 => level3Scale,
            _ => 1.0f
        };
    }
    
    public void AnimateToLevel(int newLevel)
    {
        if (visualModel == null || isImmovable) return;
        
        // Stop any existing scale animation
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        
        scaleCoroutine = StartCoroutine(ScaleToLevelCoroutine(newLevel));
    }
    
    private System.Collections.IEnumerator ScaleToLevelCoroutine(int targetLevel)
    {
        Vector3 startScale = visualModel.transform.localScale;
        Vector3 targetScale = visualModelOriginalScale * GetScaleForLevel(targetLevel);
        float elapsed = 0f;
        
        while (elapsed < scaleAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scaleAnimationDuration;
            // Ease out curve for smooth deceleration
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);
            visualModel.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            yield return null;
        }
        
        // Ensure we hit the exact target scale
        visualModel.transform.localScale = targetScale;
        scaleCoroutine = null;
    }

    public override void OnUpdate(float deltaTime)
    {
        
    }

    public override void OnChangeState(string oldStateName, string newStateName)
    {
        
    }

    public void Push(Vector2 direction, string actor = null)
    {
        movementDirection = direction;
        lastPusher = actor;
        Debug.Log($"[Block {blockID}] Push called - Position: {transform.position}, Direction: {direction}, Actor: {actor}, Current State: {currentState?.GetType().Name}");
        ((ClutterBlockState)currentState)?.OnEvent("push", this);
    }
    
    public void SetTargeted(bool targeted)
    {
        if (isTargeted == targeted) return;
        
        isTargeted = targeted;
        
        // Apply outline effect to the visual model if it exists
        if (visualModel != null)
        {
            Renderer[] renderers = visualModel.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (targeted)
                {
                    // Add red emission to create outline effect
                    renderer.material.EnableKeyword("_EMISSION");
                    renderer.material.SetColor("_EmissionColor", Color.red * 1.5f);
                }
                else
                {
                    // Remove emission
                    renderer.material.DisableKeyword("_EMISSION");
                    renderer.material.SetColor("_EmissionColor", Color.black);
                }
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