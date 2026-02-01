using System.Collections.Generic;

/// <summary>
/// Represents a gameplay event with contextual data
/// </summary>
public class GameEvent
{
    public string eventName;
    public string actor;      // Who caused the event (e.g., "homeBoy", "homeGirl")
    public string target;     // What was interacted with (e.g., "laundry", "dishes")
    public Dictionary<string, object> metadata; // Optional additional context

    public GameEvent(string eventName, string actor = null, string target = null, Dictionary<string, object> metadata = null)
    {
        this.eventName = eventName;
        this.actor = actor;
        this.target = target;
        this.metadata = metadata ?? new Dictionary<string, object>();
    }

    public override string ToString()
    {
        string result = $"[Event: {eventName}]";
        if (!string.IsNullOrEmpty(actor)) result += $" Actor: {actor}";
        if (!string.IsNullOrEmpty(target)) result += $" Target: {target}";
        if (metadata.Count > 0)
        {
            result += " Metadata: {";
            foreach (var kvp in metadata)
            {
                result += $" {kvp.Key}={kvp.Value}";
            }
            result += " }";
        }
        return result;
    }
}
