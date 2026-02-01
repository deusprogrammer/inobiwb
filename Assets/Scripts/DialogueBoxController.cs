using UnityEngine;
using TMPro;

public class DialogueBoxController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The Canvas GameObject to show/hide")]
    public Canvas dialogueCanvas;
    
    [Tooltip("The TextMeshProUGUI component for dialogue text")]
    public TextMeshProUGUI dialogueText;
    
    [Tooltip("Container for character portrait (reserved for future use)")]
    public RectTransform portraitContainer;
    
    private static DialogueBoxController instance;
    public static DialogueBoxController Instance { get { return instance; } }
    
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
    
    /// <summary>
    /// Show the dialogue box with the specified text
    /// </summary>
    public void Show(string text)
    {
        if (dialogueText != null)
        {
            dialogueText.text = text;
        }
        
        if (dialogueCanvas != null)
        {
            dialogueCanvas.gameObject.SetActive(true);
        }
        
        Debug.Log($"[DialogueBox] Showing: {text}");
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
