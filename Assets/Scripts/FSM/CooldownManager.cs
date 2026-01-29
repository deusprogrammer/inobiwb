using System;
using System.Collections.Generic;
using UnityEngine;

public delegate void CooldownTickHandler(float timeRemaining);

public class Cooldown
{
    public string name;
    public float time;
    public event CooldownTickHandler OnCooldownTick;
    public event Action OnCooldownComplete;

    public Cooldown(string name, float time, CooldownTickHandler onCooldownTick, Action onCooldownComplete)
    {
        this.name = name;
        this.time = time;
        this.OnCooldownTick = onCooldownTick;
        this.OnCooldownComplete = onCooldownComplete;
    }

    public void InvokeCooldownTick()
    {
        OnCooldownTick?.Invoke(time);
    }

    public void InvokeCooldownComplete()
    {
        OnCooldownComplete?.Invoke();
    }
}

public class CooldownManager
{
    public static Dictionary<string, Cooldown> cooldowns = new();
    public static void Update(float deltaTime)
    {
        List<string> keysToRemove = new();
        foreach (string key in cooldowns.Keys)
        {
            Cooldown cooldown = cooldowns[key];
            cooldown.time -= deltaTime;

            if (cooldown.time <= 0)
            {
                Debug.Log("Cooldown " + cooldown.name + " complete");
                cooldown.InvokeCooldownComplete();
                keysToRemove.Add(key);
            }
            else
            {
                cooldown.InvokeCooldownTick();
            }
        }
        foreach (string key in keysToRemove)
        {
            cooldowns.Remove(key);
        }
    }

    public static void AddCooldown(string name, float time, CooldownTickHandler onCooldownTick, Action onCooldownComplete)
    {
        Debug.Log("Adding cooldown for " + name);
        Cooldown cooldown = new Cooldown(name, time, onCooldownTick, onCooldownComplete);
        cooldowns[name] = cooldown;
    }

    public static bool IsOnCooldown(string name)
    {
        Debug.Log("Checking cooldowns for: " + name);
        return cooldowns.ContainsKey(name);
    }
}