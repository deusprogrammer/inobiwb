using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    [Header("Level Configuration")]
    public TextAsset levelFile;
    
    [Header("Prefabs")]
    public GameObject clutterBlockPrefab;
    public GameObject playerPrefab;
    public GameObject partnerPrefab;
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
    public bool createBoundaryWalls = true;
    
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
        
        // Store anchor offset in GridManager so coordinate conversions work correctly
        GridManager.AnchorOffset = totalOffset;
        
        // Split into lines
        string[] lines = levelData.Split('\n');
        
        // Calculate level dimensions
        int maxRows = lines.Length;
        int maxCols = 0;
        foreach (string line in lines)
        {
            maxCols = Mathf.Max(maxCols, line.TrimEnd('\r').Length);
        }
        
        // Store grid dimensions for bounds checking
        GridManager.GridRows = maxRows;
        GridManager.GridCols = maxCols;
        
        Debug.Log($"Loading level - Anchor: {(anchorObject != null ? anchorObject.name : "None")}, Position: {(anchorObject != null ? anchorObject.position.ToString() : "None")}, Custom Offset: {anchorOffset}, Total: {totalOffset}, Size: {maxCols}x{maxRows}");
        
        // Create ground plane
        CreateGroundPlane(maxRows, maxCols, totalOffset);
        
        // Track spawned players for partner linking
        GameObject mainPlayer = null;
        GameObject partner = null;
        
        for (int row = 0; row < lines.Length; row++)
        {
            string line = lines[row].TrimEnd('\r'); // Remove carriage return if present
            
            for (int col = 0; col < line.Length; col++)
            {
                char cell = line[col];
                
                // Calculate world position (flip Z to match text file orientation)
                // Add 0.5 to center objects in grid cells instead of at intersections
                Vector3 position = new Vector3((col + 0.5f) * gridSize, 0, -(row + 0.5f) * gridSize) + totalOffset;
                
                // Process cell based on character
                if (cell == ' ')
                {
                    // Empty space, do nothing
                    continue;
                }
                else if (cell == 'p' || cell == 'P')
                {
                    // Player starting position - P = main (active), p = partner (inactive)
                    bool isMainPlayer = cell == 'P';
                    GameObject prefab = isMainPlayer ? playerPrefab : partnerPrefab;
                    
                    Debug.Log($"Spawning {(isMainPlayer ? "Main" : "Partner")} player. Character: '{cell}', Prefab: {(prefab != null ? prefab.name : "NULL")}, PlayerPrefab: {(playerPrefab != null ? playerPrefab.name : "NULL")}, PartnerPrefab: {(partnerPrefab != null ? partnerPrefab.name : "NULL")}");
                    
                    if (prefab != null)
                    {
                        // Position at half the scaled height (playerScale * 0.5) to center on grid
                        position.y = playerScale * 0.5f;
                        GameObject player = Instantiate(prefab, position, Quaternion.identity);
                        player.transform.localScale = Vector3.one * playerScale;
                        
                        // Only tag main player so camera finds it first
                        if (isMainPlayer)
                        {
                            player.tag = "Player";
                        }
                        
                        // Get controller and set active state
                        HomeBoyStateController controller = player.GetComponent<HomeBoyStateController>();
                        if (controller != null)
                        {
                            controller.isActive = isMainPlayer;
                        }
                        
                        // Track for partner linking
                        if (isMainPlayer)
                        {
                            mainPlayer = player;
                        }
                        else
                        {
                            partner = player;
                        }
                        
                        Debug.Log($"{(isMainPlayer ? "Main player" : "Partner")} spawned at {position}");
                        
                        // Ensure rigidbody is kinematic for grid-based movement
                        Rigidbody rb = player.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.isKinematic = true;
                            rb.useGravity = false;
                            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                        }
                    }
                    else
                    {
                        Debug.LogError($"{(isMainPlayer ? "Player" : "Partner")} prefab not assigned!");
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
        
        // Link partners together and notify PlayerInputManager
        if (mainPlayer != null && partner != null)
        {
            HomeBoyStateController mainController = mainPlayer.GetComponent<HomeBoyStateController>();
            HomeBoyStateController partnerController = partner.GetComponent<HomeBoyStateController>();
            
            if (mainController != null && partnerController != null)
            {
                // Notify PlayerInputManager about the spawned players
                PlayerInputManager inputManager = FindFirstObjectByType<PlayerInputManager>();
                if (inputManager != null)
                {
                    inputManager.SetPlayers(mainController, partnerController);
                    inputManager.Activate();
                    Debug.Log("Players registered with PlayerInputManager and input activated");
                }
                else
                {
                    Debug.LogError("PlayerInputManager not found in scene! Input will not work.");
                }
            }
        }
        
        // Create boundary walls around the level
        CreateBoundaryWalls(maxRows, maxCols, totalOffset);
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
        // Objects are positioned at col+0.5, row+0.5, so the center is at the midpoint
        float centerX = cols * gridSize / 2f;
        float centerZ = -rows * gridSize / 2f;
        Vector3 center = new Vector3(centerX, -0.1f, centerZ) + offset; // Slightly below y=0
        
        // Instantiate ground plane
        GameObject ground = Instantiate(groundPlanePrefab, center, Quaternion.identity);
        
        // Scale to cover the entire level (assuming default plane is 10x10 units)
        float scaleX = (cols * gridSize) / 10f;
        float scaleZ = (rows * gridSize) / 10f;
        ground.transform.localScale = new Vector3(scaleX, 1, scaleZ);
        
        // Apply muted rug material to ground plane (dingy apartment rug color)
        MeshRenderer renderer = ground.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material rugMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            // Muted brownish-red, like an old dingy apartment rug
            rugMaterial.color = new Color(0.45f, 0.25f, 0.22f, 1f);
            renderer.material = rugMaterial;
        }
        
        Debug.Log($"Ground plane created: {cols}x{rows} grid, scale: {scaleX}x{scaleZ}");
        
        // Draw grid cells for debugging (disabled)
        // DrawGridCells(rows, cols, offset);
    }
    
    void DrawGridCells(int rows, int cols, Vector3 offset)
    {
        GameObject gridCellsParent = new GameObject("GridCells");
        gridCellsParent.transform.SetParent(transform);
        
        // Create material for grid cells
        Material cellMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        cellMaterial.color = new Color(1f, 1f, 1f, 0.15f); // White, very transparent
        
        // Enable transparency
        cellMaterial.SetFloat("_Surface", 1); // Transparent
        cellMaterial.SetFloat("_Blend", 0); // Alpha blend
        cellMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        cellMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        cellMaterial.SetInt("_ZWrite", 0);
        cellMaterial.renderQueue = 3000;
        
        float cellHeight = 0.005f; // Slightly above ground to avoid z-fighting
        
        // Draw a quad for each grid cell
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cell.name = $"GridCell_{col}_{row}";
                cell.transform.SetParent(gridCellsParent.transform);
                Destroy(cell.GetComponent<Collider>()); // Remove collider
                
                // Position at the center of the grid cell
                float worldX = (col + 0.5f) * gridSize + offset.x;
                float worldZ = -(row + 0.5f) * gridSize + offset.z;
                
                cell.transform.position = new Vector3(worldX, cellHeight, worldZ);
                cell.transform.rotation = Quaternion.Euler(90, 0, 0); // Flat on ground
                cell.transform.localScale = new Vector3(gridSize * 0.95f, gridSize * 0.95f, 1f); // Slightly smaller to show gaps
                
                cell.GetComponent<Renderer>().material = cellMaterial;
                
                // Store grid position as a component for easy lookup
                GridCellMarker marker = cell.AddComponent<GridCellMarker>();
                marker.gridPosition = new Vector3Int(col, 0, row);
            }
        }
        
        Debug.Log($"Grid cells drawn: {rows}x{cols} = {rows * cols} cells");
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
        
        // Ensure the block has a BoxCollider (grid-sized for collision detection)
        BoxCollider boxCollider = block.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = block.AddComponent<BoxCollider>();
            Debug.Log($"Added BoxCollider to clutter block at {position}");
        }
        boxCollider.size = Vector3.one; // Grid-sized collider
        
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
    
    void CreateBoundaryWalls(int rows, int cols, Vector3 offset)
    {
        if (!createBoundaryWalls)
        {
            Debug.Log("Boundary wall creation disabled");
            return;
        }
        
        // Create a parent object for all walls
        GameObject wallsParent = new GameObject("Level_Boundaries");
        wallsParent.transform.position = offset;
        
        float wallHeight = 3f; // Height of invisible walls
        float wallThickness = 0.5f;
        
        // Calculate level bounds
        float minX = -wallThickness;
        float maxX = (cols - 1) * gridSize + wallThickness;
        float minZ = -(rows - 1) * gridSize - wallThickness;
        float maxZ = wallThickness;
        float centerY = wallHeight * 0.5f;
        
        // North wall (positive Z)
        CreateWall("North_Wall", wallsParent.transform, 
            new Vector3((maxX + minX) * 0.5f, centerY, maxZ), 
            new Vector3(maxX - minX + wallThickness, wallHeight, wallThickness));
        
        // South wall (negative Z)
        CreateWall("South_Wall", wallsParent.transform, 
            new Vector3((maxX + minX) * 0.5f, centerY, minZ), 
            new Vector3(maxX - minX + wallThickness, wallHeight, wallThickness));
        
        // East wall (positive X)
        CreateWall("East_Wall", wallsParent.transform, 
            new Vector3(maxX, centerY, (maxZ + minZ) * 0.5f), 
            new Vector3(wallThickness, wallHeight, maxZ - minZ));
        
        // West wall (negative X)
        CreateWall("West_Wall", wallsParent.transform, 
            new Vector3(minX, centerY, (maxZ + minZ) * 0.5f), 
            new Vector3(wallThickness, wallHeight, maxZ - minZ));
        
        Debug.Log($"Created boundary walls for {cols}x{rows} level");
    }
    
    void CreateWall(string name, Transform parent, Vector3 localPosition, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.localPosition = localPosition;
        
        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;
        
        // Optional: Add a visible mesh for debugging
        // Uncomment these lines if you want to see the walls in the editor
        // MeshRenderer renderer = wall.AddComponent<MeshRenderer>();
        // MeshFilter filter = wall.AddComponent<MeshFilter>();
        // filter.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
    }
}

// Helper component to store grid position on cell GameObjects
public class GridCellMarker : MonoBehaviour
{
    public Vector3Int gridPosition;
}
