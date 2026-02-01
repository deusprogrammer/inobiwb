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

    [Header("Trigger Configuration")]
    [SerializeField] private TextAsset triggersJson;
    
    private DialogueTriggerData triggerData;
    private Dictionary<string, bool> gameFlags = new Dictionary<string, bool>();
    private Queue<DialogueLine> dialogueQueue = new Queue<DialogueLine>();
    private bool isShowingDialogue = false;
    private bool controlsFrozen = false;

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

        LoadTriggers();
        SubscribeToEvents();
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            UnsubscribeFromEvents();
        }
    }

    private void LoadTriggers()
    {
        if (triggersJson == null)
        {
            Debug.LogError("[DialogueManager] No triggers.json assigned!");
            return;
        }

        try
        {
            triggerData = JsonUtility.FromJson<DialogueTriggerData>(triggersJson.text);
            Debug.Log($"[DialogueManager] Loaded {triggerData.triggers.Count} triggers");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DialogueManager] Failed to parse triggers.json: {ex.Message}");
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
            return;
        }

        DialogueLine line = dialogueQueue.Dequeue();
        isShowingDialogue = true;

        Debug.Log($"[DialogueManager] Showing dialogue - Speaker: {line.speaker}, Text: {line.text}");

        // Format dialogue with speaker name
        string displayText = string.IsNullOrEmpty(line.speaker) 
            ? line.text 
            : $"<b>{line.speaker}:</b> {line.text}";

        // Add continue prompt if controls are frozen
        if (controlsFrozen)
        {
            displayText += "\n\n(Press [A] to continue)";
        }

        Debug.Log($"[DialogueManager] Calling DialogueBoxController.Show with text length: {displayText.Length}");
        DialogueBoxController.Instance?.Show(displayText, !controlsFrozen);
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
