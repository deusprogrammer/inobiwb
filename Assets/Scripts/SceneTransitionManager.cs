using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles scene transitions triggered by game events.
/// Attach to a GameObject in your first scene.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager instance;
    
    [Header("Transition Settings")]
    [Tooltip("Delay before loading next scene (for fade-out effects, etc.)")]
    public float transitionDelay = 1f;
    
    private bool isTransitioning = false;
    private string nextSceneName = "";
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Subscribe to level complete event
        EventBus.Instance.Subscribe(EventNames.LevelComplete, OnLevelComplete);
        Debug.Log("[SceneTransitionManager] Ready - listening for level complete events");
    }
    
    void OnDestroy()
    {
        if (instance == this && EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe(EventNames.LevelComplete, OnLevelComplete);
        }
    }
    
    private void OnLevelComplete(GameEvent evt)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[SceneTransitionManager] Already transitioning, ignoring event");
            return;
        }
        
        string sceneName = evt.target;
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneTransitionManager] LevelComplete event has no target scene!");
            return;
        }
        
        Debug.Log($"[SceneTransitionManager] Level complete - transitioning to '{sceneName}' in {transitionDelay}s");
        isTransitioning = true;
        nextSceneName = sceneName;
        
        // Load scene after delay
        Invoke(nameof(LoadNextScene), transitionDelay);
    }
    
    private void LoadNextScene()
    {
        LoadScene(nextSceneName);
    }
    
    /// <summary>
    /// Load a scene by name. Can be called directly or via events.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneTransitionManager] Cannot load scene - name is empty!");
            return;
        }
        
        Debug.Log($"[SceneTransitionManager] Loading scene: {sceneName}");
        
        try
        {
            SceneManager.LoadScene(sceneName);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SceneTransitionManager] Failed to load scene '{sceneName}': {ex.Message}");
            isTransitioning = false;
        }
    }
}
