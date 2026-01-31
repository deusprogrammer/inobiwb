using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelLoader))]
public class LevelLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        LevelLoader levelLoader = (LevelLoader)target;
        
        // Check if a preview exists
        bool previewExists = GameObject.Find("LEVEL_PREVIEW") != null;
        
        // Monitor changes
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        
        // If properties changed and preview exists, auto-refresh
        if (EditorGUI.EndChangeCheck() && previewExists)
        {
            PreviewLevel(levelLoader);
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Preview Level", GUILayout.Height(30)))
        {
            PreviewLevel(levelLoader);
        }
        
        if (GUILayout.Button("Clear Preview", GUILayout.Height(30)))
        {
            ClearPreview();
        }
    }
    
    private void PreviewLevel(LevelLoader levelLoader)
    {
        // Clear any existing preview
        ClearPreview();
        
        if (levelLoader.levelFile == null)
        {
            Debug.LogError("No level file assigned to LevelLoader!");
            return;
        }
        
        // Create a parent object to hold all preview objects
        GameObject previewParent = new GameObject("LEVEL_PREVIEW");
        Undo.RegisterCreatedObjectUndo(previewParent, "Preview Level");
        
        // Get anchor offset (defaults to zero if no anchor)
        Vector3 totalOffset = (levelLoader.anchorObject != null ? levelLoader.anchorObject.position : Vector3.zero) + levelLoader.anchorOffset;
        
        Debug.Log($"Preview anchor: {(levelLoader.anchorObject != null ? levelLoader.anchorObject.name : "None")}, Position: {(levelLoader.anchorObject != null ? levelLoader.anchorObject.position.ToString() : "None")}, Custom Offset: {levelLoader.anchorOffset}, Total: {totalOffset}");
        
        string levelData = levelLoader.levelFile.text;
        string[] lines = levelData.Split('\n');
        
        // Calculate level dimensions
        int maxRows = lines.Length;
        int maxCols = 0;
        foreach (string line in lines)
        {
            maxCols = Mathf.Max(maxCols, line.TrimEnd('\r').Length);
        }
        
        // Create ground plane
        if (levelLoader.spawnGroundPlane && levelLoader.groundPlanePrefab != null)
        {
            float centerX = (maxCols - 1) * levelLoader.gridSize / 2f;
            float centerZ = -(maxRows - 1) * levelLoader.gridSize / 2f;
            Vector3 center = new Vector3(centerX, -0.1f, centerZ) + totalOffset;
            
            GameObject ground = (GameObject)PrefabUtility.InstantiatePrefab(levelLoader.groundPlanePrefab);
            ground.transform.position = center;
            ground.transform.SetParent(previewParent.transform);
            
            float scaleX = (maxCols * levelLoader.gridSize) / 10f;
            float scaleZ = (maxRows * levelLoader.gridSize) / 10f;
            ground.transform.localScale = new Vector3(scaleX, 1, scaleZ);
            
            Undo.RegisterCreatedObjectUndo(ground, "Preview Ground");
        }
        
        // Spawn level objects
        for (int row = 0; row < lines.Length; row++)
        {
            string line = lines[row].TrimEnd('\r');
            
            for (int col = 0; col < line.Length; col++)
            {
                char cell = line[col];
                Vector3 position = new Vector3(col * levelLoader.gridSize, 0, -row * levelLoader.gridSize) + totalOffset;
                
                if (cell == ' ')
                {
                    continue;
                }
                else if (cell == 'p' || cell == 'P')
                {
                    // Player
                    if (levelLoader.playerPrefab != null)
                    {
                        position.y = totalOffset.y + levelLoader.playerScale * 0.5f;
                        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(levelLoader.playerPrefab);
                        player.transform.position = position;
                        player.transform.localScale = Vector3.one * levelLoader.playerScale;
                        player.transform.SetParent(previewParent.transform);
                        
                        // Disable any controllers to prevent runtime behavior
                        foreach (MonoBehaviour component in player.GetComponents<MonoBehaviour>())
                        {
                            component.enabled = false;
                        }
                        
                        Undo.RegisterCreatedObjectUndo(player, "Preview Player");
                    }
                }
                else if (cell >= '1' && cell <= '9')
                {
                    SpawnPreviewBlock(levelLoader, position, cell, false, previewParent.transform, totalOffset.y);
                }
                else if (cell == '*')
                {
                    SpawnPreviewBlock(levelLoader, position, '1', true, previewParent.transform, totalOffset.y);
                }
            }
        }
        
        Debug.Log($"Level preview created: {maxCols}x{maxRows} grid");
    }
    
    private void SpawnPreviewBlock(LevelLoader levelLoader, Vector3 position, char code, bool immovable, Transform parent, float yOffset)
    {
        if (levelLoader.clutterBlockPrefab == null) return;
        
        position.y = yOffset + levelLoader.objectScale * 0.5f;
        
        GameObject block = (GameObject)PrefabUtility.InstantiatePrefab(levelLoader.clutterBlockPrefab);
        block.transform.position = position;
        block.transform.localScale = Vector3.one * levelLoader.objectScale;
        block.transform.SetParent(parent);
        
        ClutterBlockStateController controller = block.GetComponent<ClutterBlockStateController>();
        if (controller != null)
        {
            // Disable the controller to prevent OnStart from running
            controller.enabled = false;
            
            controller.isImmovable = immovable;
            
            int codeValue = code - '0';
            
            if (codeValue >= 1 && codeValue <= 3)
            {
                controller.blockType = ClutterBlockType.Trash;
                controller.level = codeValue;
            }
            else if (codeValue >= 4 && codeValue <= 6)
            {
                controller.blockType = ClutterBlockType.Laundry;
                controller.level = codeValue - 3;
            }
            else if (codeValue >= 7 && codeValue <= 9)
            {
                controller.blockType = ClutterBlockType.Dishes;
                controller.level = codeValue - 6;
            }
            
            // Set color manually since OnStart won't run in edit mode
            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (immovable)
                {
                    renderer.sharedMaterial.color = Color.yellow;
                }
                else
                {
                    switch (controller.blockType)
                    {
                        case ClutterBlockType.Trash:
                            renderer.sharedMaterial.color = Color.black;
                            break;
                        case ClutterBlockType.Laundry:
                            renderer.sharedMaterial.color = new Color(0.5f, 0.8f, 1.0f);
                            break;
                        case ClutterBlockType.Dishes:
                            renderer.sharedMaterial.color = Color.white;
                            break;
                    }
                }
            }
        }
        
        Undo.RegisterCreatedObjectUndo(block, "Preview Block");
    }
    
    private void ClearPreview()
    {
        GameObject preview = GameObject.Find("LEVEL_PREVIEW");
        if (preview != null)
        {
            Undo.DestroyObjectImmediate(preview);
            Debug.Log("Level preview cleared");
        }
        
        // Clean up any GridManager instances that may have been created
        GridManager gridManager = GameObject.FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            Undo.DestroyObjectImmediate(gridManager.gameObject);
            Debug.Log("Cleaned up GridManager instance");
        }
    }
}
