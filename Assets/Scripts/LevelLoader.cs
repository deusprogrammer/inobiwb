using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    [Header("Level Configuration")]
    public TextAsset levelFile;
    
    [Header("Prefabs")]
    public GameObject clutterBlockPrefab;
    public GameObject playerPrefab;
    public GameObject groundPlanePrefab;
    
    [Header("Grid Settings")]
    public float gridSize = 1.0f;
    public float objectScale = 0.8f;
    public float playerScale = 0.6f;
    
    [Header("World Position")]
    [Tooltip("Optional anchor object - level will spawn relative to this position. If null, spawns at world origin.")]
    public Transform anchorObject;
    [Tooltip("Additional offset from the anchor position.")]
    public Vector3 anchorOffset = Vector3.zero;
    public bool spawnGroundPlane = true;
    
    void Awake()
    {
        // Create GridManager as a child of this LevelLoader
        GameObject gridManagerObj = new GameObject("GridManager");
        gridManagerObj.transform.SetParent(transform);
        gridManagerObj.AddComponent<GridManager>();
        Debug.Log("GridManager created by LevelLoader");
    }
    
    void Start()
    {
        if (levelFile != null)
        {
            LoadLevel(levelFile.text);
        }
        else
        {
            Debug.LogError("No level file assigned to LevelLoader!");
        }
    }
    
    void LoadLevel(string levelData)
    {
        // Get anchor offset (defaults to zero if no anchor)
        Vector3 totalOffset = (anchorObject != null ? anchorObject.position : Vector3.zero) + anchorOffset;
        
        Debug.Log($"Loading level - Anchor: {(anchorObject != null ? anchorObject.name : "None")}, Position: {(anchorObject != null ? anchorObject.position.ToString() : "None")}, Custom Offset: {anchorOffset}, Total: {totalOffset}");
        
        // Split into lines
        string[] lines = levelData.Split('\n');
        
        // Calculate level dimensions
        int maxRows = lines.Length;
        int maxCols = 0;
        foreach (string line in lines)
        {
            maxCols = Mathf.Max(maxCols, line.TrimEnd('\r').Length);
        }
        
        // Create ground plane
        CreateGroundPlane(maxRows, maxCols, totalOffset);
        
        for (int row = 0; row < lines.Length; row++)
        {
            string line = lines[row].TrimEnd('\r'); // Remove carriage return if present
            
            for (int col = 0; col < line.Length; col++)
            {
                char cell = line[col];
                
                // Calculate world position (flip Z to match text file orientation)
                Vector3 position = new Vector3(col * gridSize, 0, -row * gridSize) + totalOffset;
                
                // Process cell based on character
                if (cell == ' ')
                {
                    // Empty space, do nothing
                    continue;
                }
                else if (cell == 'p' || cell == 'P')
                {
                    // Player starting position
                    if (playerPrefab != null)
                    {
                        // Position at half the scaled height (playerScale * 0.5)
                        position.y = playerScale * 0.5f;
                        GameObject player = Instantiate(playerPrefab, position, Quaternion.identity);
                        player.transform.localScale = Vector3.one * playerScale;
                        player.tag = "Player"; // Tag for camera to find
                        
                        Debug.Log($"Player spawned at {position}");
                        
                        // Ensure rigidbody is kinematic for grid-based movement
                        Rigidbody rb = player.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.isKinematic = true;
                            rb.useGravity = false;
                            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                            Debug.Log("Player Rigidbody: kinematic with continuous collision detection");
                        }
                    }
                    else
                    {
                        Debug.LogError("Player prefab not assigned!");
                    }
                }
                else if (cell >= '1' && cell <= '9')
                {
                    // Clutter block
                    SpawnClutterBlock(position, cell, false);
                }
                else if (cell == '*')
                {
                    // Immovable furniture (chair)
                    SpawnClutterBlock(position, '1', true); // Use level 1 trash model for now
                }
            }
        }
    }
    
    void CreateGroundPlane(int rows, int cols, Vector3 offset)
    {
        if (!spawnGroundPlane)
        {
            Debug.Log("Ground plane creation disabled");
            return;
        }
        
        if (groundPlanePrefab == null)
        {
            Debug.LogWarning("Ground plane prefab not assigned - skipping ground creation");
            return;
        }
        
        // Calculate center of the level
        float centerX = (cols - 1) * gridSize / 2f;
        float centerZ = -(rows - 1) * gridSize / 2f;
        Vector3 center = new Vector3(centerX, -0.1f, centerZ) + offset; // Slightly below y=0
        
        // Instantiate ground plane
        GameObject ground = Instantiate(groundPlanePrefab, center, Quaternion.identity);
        
        // Scale to cover the entire level (assuming default plane is 10x10 units)
        float scaleX = (cols * gridSize) / 10f;
        float scaleZ = (rows * gridSize) / 10f;
        ground.transform.localScale = new Vector3(scaleX, 1, scaleZ);
        
        Debug.Log($"Ground plane created: {cols}x{rows} grid, scale: {scaleX}x{scaleZ}");
    }
    
    void SpawnClutterBlock(Vector3 position, char code, bool immovable = false)
    {
        if (clutterBlockPrefab == null)
        {
            Debug.LogError("ClutterBlock prefab not assigned!");
            return;
        }
        
        // Position at half the scaled height so bottom sits on ground
        position.y = objectScale * 0.5f;
        
        // Instantiate the block
        GameObject block = Instantiate(clutterBlockPrefab, position, Quaternion.identity);
        block.transform.localScale = Vector3.one * objectScale;
        
        // Get the controller and set properties
        ClutterBlockStateController controller = block.GetComponent<ClutterBlockStateController>();
        if (controller != null)
        {
            // Set immovable flag
            controller.isImmovable = immovable;
            
            // Decode the character to type and level
            int codeValue = code - '0'; // Convert char to int (1-9)
            
            if (codeValue >= 1 && codeValue <= 3)
            {
                // Trash (1-3)
                controller.blockType = ClutterBlockType.Trash;
                controller.level = codeValue;
            }
            else if (codeValue >= 4 && codeValue <= 6)
            {
                // Laundry (4-6)
                controller.blockType = ClutterBlockType.Laundry;
                controller.level = codeValue - 3; // 4->1, 5->2, 6->3
            }
            else if (codeValue >= 7 && codeValue <= 9)
            {
                // Dishes (7-9)
                controller.blockType = ClutterBlockType.Dishes;
                controller.level = codeValue - 6; // 7->1, 8->2, 9->3
            }
        }
        
        // Register block with GridManager
        GridManager.Instance.RegisterBlock(controller);
    }
}
