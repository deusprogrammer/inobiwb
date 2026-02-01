using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Central event bus for publish/subscribe pattern.
/// All gameplay events flow through here.
/// </summary>
public class EventBus : MonoBehaviour
{
    private static EventBus instance;
    public static EventBus Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("EventBus");
                instance = go.AddComponent<EventBus>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    // Event name -> List of subscribers
    private Dictionary<string, List<Action<GameEvent>>> subscribers = new Dictionary<string, List<Action<GameEvent>>>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // Removed DontDestroyOnLoad - EventBus is now per-scene
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Publish an event to all subscribers
    /// </summary>
    public void Publish(GameEvent gameEvent)
    {
        if (gameEvent == null)
        {
            Debug.LogError("[EventBus] Cannot publish null event");
            return;
        }

        Debug.Log($"[EventBus] Publishing: {gameEvent}");

        if (subscribers.ContainsKey(gameEvent.eventName))
        {
            // Create a copy of the list to avoid modification during iteration
            List<Action<GameEvent>> handlers = new List<Action<GameEvent>>(subscribers[gameEvent.eventName]);
            foreach (var handler in handlers)
            {
                try
                {
                    handler?.Invoke(gameEvent);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Error in event handler for {gameEvent.eventName}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Subscribe to an event by name
    /// </summary>
    public void Subscribe(string eventName, Action<GameEvent> handler)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogError("[EventBus] Cannot subscribe to null or empty event name");
            return;
        }

        if (handler == null)
        {
            Debug.LogError("[EventBus] Cannot subscribe with null handler");
            return;
        }

        if (!subscribers.ContainsKey(eventName))
        {
            subscribers[eventName] = new List<Action<GameEvent>>();
        }

        if (!subscribers[eventName].Contains(handler))
        {
            subscribers[eventName].Add(handler);
            Debug.Log($"[EventBus] Subscribed to event: {eventName}");
        }
    }

    /// <summary>
    /// Unsubscribe from an event
    /// </summary>
    public void Unsubscribe(string eventName, Action<GameEvent> handler)
    {
        if (subscribers.ContainsKey(eventName))
        {
            subscribers[eventName].Remove(handler);
            Debug.Log($"[EventBus] Unsubscribed from event: {eventName}");
        }
    }

    /// <summary>
    /// Clear all subscribers for a specific event
    /// </summary>
    public void ClearEvent(string eventName)
    {
        if (subscribers.ContainsKey(eventName))
        {
            subscribers.Remove(eventName);
            Debug.Log($"[EventBus] Cleared all subscribers for event: {eventName}");
        }
    }

    /// <summary>
    /// Clear all subscribers
    /// </summary>
    public void ClearAll()
    {
        subscribers.Clear();
        Debug.Log("[EventBus] Cleared all event subscriptions");
    }
}
