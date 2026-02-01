using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Simple title screen controller that loads the Tutorial scene on any input.
/// </summary>
public class TitleScreenController : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Name of the scene to load when input is detected")]
    private string nextScene = "Tutorial";
    
    [SerializeField]
    [Tooltip("Delay before loading scene (for fade effects, etc.)")]
    private float loadDelay = 0.5f;
    
    private bool isLoading = false;

    void Update()
    {
        // Don't process input if already loading
        if (isLoading)
            return;
        
        // Check for any keyboard key
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            StartSceneTransition();
            return;
        }
        
        // Check for any gamepad button
        if (Gamepad.current != null)
        {
            foreach (var button in Gamepad.current.allControls)
            {
                if (button is UnityEngine.InputSystem.Controls.ButtonControl buttonControl)
                {
                    if (buttonControl.wasPressedThisFrame)
                    {
                        StartSceneTransition();
                        return;
                    }
                }
            }
        }
        
        // Check for mouse click
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartSceneTransition();
        }
    }

    private void StartSceneTransition()
    {
        isLoading = true;
        Debug.Log($"[TitleScreenController] Loading scene: {nextScene}");
        
        if (loadDelay > 0)
        {
            Invoke(nameof(LoadScene), loadDelay);
        }
        else
        {
            LoadScene();
        }
    }

    private void LoadScene()
    {
        try
        {
            SceneManager.LoadScene(nextScene);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TitleScreenController] Failed to load scene '{nextScene}': {e.Message}");
            Debug.LogError($"[TitleScreenController] Make sure '{nextScene}' is added to Build Settings!");
        }
    }
}
