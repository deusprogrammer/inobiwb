using UnityEngine;

/// <summary>
/// Debug listener that logs all events to the console.
/// Attach this to any GameObject to see event traffic.
/// </summary>
public class DebugEventListener : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("Which events to listen for (leave empty for all)")]
    public string[] eventFilter = new string[0];

    void Start()
    {
        // Subscribe to all event types
        if (eventFilter.Length == 0)
        {
            // Listen to all events defined in EventNames
            SubscribeToEvent(EventNames.LevelStart);
            SubscribeToEvent(EventNames.LevelEnd);
            SubscribeToEvent(EventNames.BlockPushed);
            SubscribeToEvent(EventNames.WrongBlockPushed);
            SubscribeToEvent(EventNames.BlockCleared);
            SubscribeToEvent(EventNames.BlocksCombined);
            SubscribeToEvent(EventNames.BlocksCombineFailed);
            SubscribeToEvent(EventNames.ItemCollected);
            SubscribeToEvent(EventNames.FurnitureMoved);
            SubscribeToEvent(EventNames.FurnitureMoveFailure);
            
            Debug.Log("[DebugEventListener] Listening to all events");
        }
        else
        {
            // Listen only to specified events
            foreach (string eventName in eventFilter)
            {
                SubscribeToEvent(eventName);
            }
            
            Debug.Log($"[DebugEventListener] Listening to {eventFilter.Length} specific events");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from all events
        if (eventFilter.Length == 0)
        {
            UnsubscribeFromEvent(EventNames.LevelStart);
            UnsubscribeFromEvent(EventNames.LevelEnd);
            UnsubscribeFromEvent(EventNames.BlockPushed);
            UnsubscribeFromEvent(EventNames.WrongBlockPushed);
            UnsubscribeFromEvent(EventNames.BlockCleared);
            UnsubscribeFromEvent(EventNames.BlocksCombined);
            UnsubscribeFromEvent(EventNames.BlocksCombineFailed);
            UnsubscribeFromEvent(EventNames.ItemCollected);
            UnsubscribeFromEvent(EventNames.FurnitureMoved);
            UnsubscribeFromEvent(EventNames.FurnitureMoveFailure);
        }
        else
        {
            foreach (string eventName in eventFilter)
            {
                UnsubscribeFromEvent(eventName);
            }
        }
    }

    private void SubscribeToEvent(string eventName)
    {
        EventBus.Instance.Subscribe(eventName, OnGameEvent);
    }

    private void UnsubscribeFromEvent(string eventName)
    {
        EventBus.Instance.Unsubscribe(eventName, OnGameEvent);
    }

    private void OnGameEvent(GameEvent gameEvent)
    {
        Debug.Log($"[DebugEventListener] Received: {gameEvent}");
    }
}
