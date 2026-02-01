using System;
using System.Collections.Generic;

/// <summary>
/// Data structures for deserializing triggers.json
/// </summary>
[Serializable]
public class DialogueTriggerData
{
    public List<DialogueTrigger> triggers;
}

[Serializable]
public class DialogueTrigger
{
    public string id;
    public string eventName;
    public string actor;
    public string target;
    public bool freezeControls;
    public int maxCount;
    public Dictionary<string, bool> condition;
    public List<string> setFlags;
    public List<DialogueLine> dialogue;
    
    [NonSerialized]
    public int fireCount = 0;
}

[Serializable]
public class DialogueLine
{
    public string speaker;
    public string expression;
    public string text;
    public int count; // Optional: which trigger repetition this line applies to
}
