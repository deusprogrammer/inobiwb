using UnityEngine;
using TMPro;

public class DialogueBoxController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The Canvas GameObject to show/hide")]
    public Canvas dialogueCanvas;
    
    [Tooltip("The TextMeshProUGUI component for dialogue text")]
    public TextMeshProUGUI dialogueText;
    
    [Tooltip("The TextMeshProUGUI component for continue prompt")]
    public TextMeshProUGUI continuePromptText;
    
    [Tooltip("Container for character portrait (reserved for future use)")]
    public RectTransform portraitContainer;
    
    [Header("Auto-Hide Settings")]
    [Tooltip("Duration in seconds before dialogue auto-hides (0 = no auto-hide)")]
    public float autoHideDuration = 3f;
    
    private static DialogueBoxController instance;
    public static DialogueBoxController Instance { get { return instance; } }
    
    private float autoHideTimer = 0f;
    private bool isAutoHideEnabled = false;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple DialogueBoxController instances found!");
            Destroy(gameObject);
            return;
        }
        
        // Start hidden
        Hide();
    }
    
    void Update()
    {
        // Handle auto-hide timer
        if (isAutoHideEnabled && autoHideTimer > 0f)
        {
            autoHideTimer -= Time.deltaTime;
            if (autoHideTimer <= 0f)
            {
                Hide();
                
                // Auto-advance to next dialogue
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.AdvanceDialogue();
                }
            }
        }
    }
    
    /// <summary>
    /// Show the dialogue box with the specified text
    /// </summary>
    public void Show(string text, bool enableAutoHide = false)
    {
        if (dialogueText != null)
        {
            dialogueText.text = text;
        }
        isAutoHideEnabled = false;
        autoHideTimer = 0f;
        
        
        if (dialogueCanvas != null)
        {
            dialogueCanvas.gameObject.SetActive(true);
        }
        
        // Setup auto-hide timer
        isAutoHideEnabled = enableAutoHide;
        if (isAutoHideEnabled && autoHideDuration > 0f)
        {
            autoHideTimer = autoHideDuration;
        }
        
        Debug.Log($"[DialogueBox] Showing: {text} (auto-hide: {enableAutoHide})");
    }
    
    /// <summary>
    /// Hide the dialogue box
    /// </summary>
    public void Hide()
    {
        if (dialogueCanvas != null)
        {
            dialogueCanvas.gameObject.SetActive(false);
        }
        
        Debug.Log("[DialogueBox] Hidden");
    }
    
    /// <summary>
    /// Update the dialogue text without showing/hiding
    /// </summary>
    public void SetText(string text)
    {
        if (dialogueText != null)
        {
            dialogueText.text = text;
        }
    }
}
