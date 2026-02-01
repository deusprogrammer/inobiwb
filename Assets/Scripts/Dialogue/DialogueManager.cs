using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Manages dialogue triggers, evaluates conditions, and queues dialogue.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    private static DialogueManager instance;
    public static DialogueManager Instance => instance;

    [Header("Character Portraits")]
    [SerializeField] private List<CharacterPortrait> characterPortraits = new List<CharacterPortrait>();
    
    private Dictionary<string, CharacterPortrait> portraitLookup = new Dictionary<string, CharacterPortrait>();
    private DialogueTriggerData triggerData;
    private Dictionary<string, bool> gameFlags = new Dictionary<string, bool>();
    private Queue<DialogueLine> dialogueQueue = new Queue<DialogueLine>();
    private bool isShowingDialogue = false;
    private bool controlsFrozen = false;
    private DialogueTrigger currentTrigger = null; // Track which trigger's dialogue is showing

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // Removed DontDestroyOnLoad - DialogueManager is now per-scene
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadTriggersForCurrentScene();
        BuildPortraitLookup();
        SubscribeToEvents();
    }
    
    private void LoadTriggersForCurrentScene()
    {
        // Load JSON file matching scene name (e.g., "Tutorial" -> "tutorial.json")
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string jsonFileName = sceneName.ToLower();
        
        TextAsset sceneJson = Resources.Load<TextAsset>(jsonFileName);
        
        if (sceneJson != null)
        {
            Debug.Log($"[DialogueManager] Loading dialogue for scene: {sceneName} from {jsonFileName}.json");
            triggerData = JsonUtility.FromJson<DialogueTriggerData>(sceneJson.text);
            
            if (triggerData.triggers != null)
            {
                Debug.Log($"[DialogueManager] Loaded {triggerData.triggers.Count} triggers for {sceneName}");
            }
        }
        else
        {
            Debug.LogWarning($"[DialogueManager] No dialogue file found for scene '{sceneName}' (looking for Resources/{jsonFileName}.json)");
            triggerData = new DialogueTriggerData();
        }
    }
    
    private void BuildPortraitLookup()
    {
        portraitLookup.Clear();
        foreach (CharacterPortrait portrait in characterPortraits)
        {
            if (portrait != null && !string.IsNullOrEmpty(portrait.characterName))
            {
                portraitLookup[portrait.characterName] = portrait;
                Debug.Log($"[DialogueManager] Registered portrait for '{portrait.characterName}' with {portrait.expressions.Count} expressions");
            }
        }
    }
    
    /// <summary>
    /// Get portrait sprite for a specific character and expression.
    /// </summary>
    public Sprite GetPortraitSprite(string characterName, string expressionName)
    {
        if (portraitLookup.TryGetValue(characterName, out CharacterPortrait portrait))
        {
            return portrait.GetExpression(expressionName);
        }
        
        Debug.LogWarning($"[DialogueManager] No portrait data found for character '{characterName}'");
        return null;
    }
    
    /// <summary>
    /// Get full body portrait sprite for a specific character and pose.
    /// </summary>
    public Sprite GetFullBodyPortrait(string characterName, string portraitName)
    {
        if (portraitLookup.TryGetValue(characterName, out CharacterPortrait portrait))
        {
            return portrait.GetFullBodyPortrait(portraitName);
        }
        
        Debug.LogWarning($"[DialogueManager] No portrait data found for character '{characterName}'");
        return null;
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            UnsubscribeFromEvents();
            instance = null;
        }
    }

    private void SubscribeToEvents()
    {
        EventBus.Instance.Subscribe(EventNames.LevelStart, OnGameEvent);
        EventBus.Instance.Subscribe(EventNames.LevelEnd, OnGameEvent);
        EventBus.Instance.Subscribe(EventNames.LevelEndPerfect, OnGameEvent);
        EventBus.Instance.Subscribe(EventNames.BlockPushed, OnGameEvent);
        EventBus.Instance.Subscribe(EventNames.WrongBlockPushed, OnGameEvent);
        EventBus.Instance.Subscribe(EventNames.BlockCleared, OnGameEvent);
        EventBus.Instance.Subscribe(EventNames.BlocksCombined, OnGameEvent);
        EventBus.Instance.Subscribe(EventNames.BlocksCombineFailed, OnGameEvent);
        EventBus.Instance.Subscribe(EventNames.ItemCollected, OnGameEvent);
        EventBus.Instance.Subscribe(EventNames.FurnitureMoved, OnGameEvent);
        EventBus.Instance.Subscribe(EventNames.FurnitureMoveFailure, OnGameEvent);
        EventBus.Instance.Subscribe(EventNames.Hug, OnGameEvent);
        
        Debug.Log("[DialogueManager] Subscribed to all events");
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Instance.Unsubscribe(EventNames.LevelStart, OnGameEvent);
        EventBus.Instance.Unsubscribe(EventNames.LevelEnd, OnGameEvent);
        EventBus.Instance.Unsubscribe(EventNames.LevelEndPerfect, OnGameEvent);
        EventBus.Instance.Unsubscribe(EventNames.BlockPushed, OnGameEvent);
        EventBus.Instance.Unsubscribe(EventNames.WrongBlockPushed, OnGameEvent);
        EventBus.Instance.Unsubscribe(EventNames.BlockCleared, OnGameEvent);
        EventBus.Instance.Unsubscribe(EventNames.BlocksCombined, OnGameEvent);
        EventBus.Instance.Unsubscribe(EventNames.BlocksCombineFailed, OnGameEvent);
        EventBus.Instance.Unsubscribe(EventNames.ItemCollected, OnGameEvent);
        EventBus.Instance.Unsubscribe(EventNames.FurnitureMoved, OnGameEvent);
        EventBus.Instance.Unsubscribe(EventNames.FurnitureMoveFailure, OnGameEvent);
        EventBus.Instance.Unsubscribe(EventNames.Hug, OnGameEvent);
    }

    private void OnGameEvent(GameEvent evt)
    {
        Debug.Log($"[DialogueManager] Received event: {evt}");
        
        if (triggerData == null || triggerData.triggers == null) return;

        // Evaluate all triggers for this event
        foreach (DialogueTrigger trigger in triggerData.triggers)
        {
            if (EvaluateTrigger(trigger, evt))
            {
                FireTrigger(trigger);
                break; // Only fire one trigger per event for now
            }
        }
    }

    private bool EvaluateTrigger(DialogueTrigger trigger, GameEvent evt)
    {
        // Event name must match
        if (trigger.eventName != evt.eventName)
            return false;

        // Check fire count
        if (trigger.maxCount > 0 && trigger.fireCount >= trigger.maxCount)
            return false;

        // Check actor filter (if specified)
        if (!string.IsNullOrEmpty(trigger.actor) && trigger.actor != evt.actor)
            return false;

        // Check target filter (if specified)
        if (!string.IsNullOrEmpty(trigger.target) && trigger.target != evt.target)
            return false;

        // Check conditions (if specified)
        if (trigger.condition != null)
        {
            foreach (var kvp in trigger.condition)
            {
                if (!gameFlags.ContainsKey(kvp.Key) || gameFlags[kvp.Key] != kvp.Value)
                    return false;
            }
        }

        return true;
    }

    private void FireTrigger(DialogueTrigger trigger)
    {
        Debug.Log($"[DialogueManager] Firing trigger: {trigger.id}");

        // Increment fire count
        trigger.fireCount++;

        // Set flags
        if (trigger.setFlags != null)
        {
            foreach (string flag in trigger.setFlags)
            {
                gameFlags[flag] = true;
                Debug.Log($"[DialogueManager] Set flag: {flag}");
            }
        }

        // Queue dialogue
        if (trigger.dialogue != null && trigger.dialogue.Count > 0)
        {
            foreach (DialogueLine line in trigger.dialogue)
            {
                dialogueQueue.Enqueue(line);
            }

            controlsFrozen = trigger.freezeControls;
            currentTrigger = trigger; // Track this trigger
            ShowNextDialogue();
        }
    }

    private void ShowNextDialogue()
    {
        Debug.Log($"[DialogueManager] ShowNextDialogue called, queue count: {dialogueQueue.Count}");
        
        if (dialogueQueue.Count == 0)
        {
            isShowingDialogue = false;
            controlsFrozen = false;
            DialogueBoxController.Instance?.Hide();
            Debug.Log("[DialogueManager] Dialogue queue empty, hiding box");
            
            // Check if we should transition to next scene
            if (currentTrigger != null && !string.IsNullOrEmpty(currentTrigger.nextScene))
            {
                Debug.Log($"[DialogueManager] Trigger complete with nextScene: {currentTrigger.nextScene}");
                EventBus.Instance.Publish(new GameEvent(
                    EventNames.LevelComplete,
                    target: currentTrigger.nextScene
                ));
            }
            
            currentTrigger = null;
            return;
        }

        DialogueLine line = dialogueQueue.Dequeue();
        isShowingDialogue = true;

        Debug.Log($"[DialogueManager] Showing dialogue - Speaker: {line.speaker}, Expression: {line.expression}, FullBody: {line.fullBody}, Text: {line.text}");

        // Get portrait sprite for this speaker and expression
        Sprite portraitSprite = GetPortraitSprite(line.speaker, line.expression);
        
        // Get full body portrait if specified
        Sprite fullBodySprite = null;
        if (!string.IsNullOrEmpty(line.fullBody))
        {
            fullBodySprite = GetFullBodyPortrait(line.speaker, line.fullBody);
        }

        // Format dialogue with speaker name
        string displayText = string.IsNullOrEmpty(line.speaker) 
            ? line.text 
            : $"<b>{line.speaker}:</b> {line.text}";

        // Add continue prompt if controls are frozen
        if (controlsFrozen)
        {
            displayText += "\n\n(Press [A] to continue)";
        }

        Debug.Log($"[DialogueManager] Calling DialogueBoxController.Show with text length: {displayText.Length}, portrait: {(portraitSprite != null ? "YES" : "NO")}, fullBody: {(fullBodySprite != null ? "YES" : "NO")}");
        DialogueBoxController.Instance?.Show(displayText, portraitSprite, fullBodySprite, !controlsFrozen);
    }

    /// <summary>
    /// Advance to next dialogue line (called by push button when controls frozen)
    /// </summary>
    public void AdvanceDialogue()
    {
        if (isShowingDialogue && controlsFrozen)
        {
            ShowNextDialogue();
        }
    }

    public bool AreControlsFrozen()
    {
        return controlsFrozen;
    }
}
