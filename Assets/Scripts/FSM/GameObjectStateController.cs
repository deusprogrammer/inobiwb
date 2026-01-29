using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

[Serializable]
public struct GameObjectStateStruct
{
    public string name;
    public AnimationClip animation;
    public GameObjectState gameObjectState;
    public PhysicsMaterial physicsMaterial;
    public float duration;
    public int cooldown;
}

public abstract class GameObjectStateController : MonoBehaviour
{
    public static GameObjectStateController Instance;
    public static event Action OnStateChange;

    protected GameObjectState currentState;
    private string currentStateName;
    public string CurrentStateName { get { return currentStateName; } }

    private AnimancerComponent animancerComponent;

    public GameObjectStateStruct[] gameObjectStates;
    private Dictionary<string, GameObjectStateStruct> gameObjectStateMap = new();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        animancerComponent = GetComponent<AnimancerComponent>();

        foreach (GameObjectStateStruct state in gameObjectStates)
        {
            gameObjectStateMap[state.name] = state;
            state.gameObjectState.Initialize(this, state.duration, state.cooldown);
        }

        OnStart();
    }

    void FixedUpdate()
    {
        OnUpdate(Time.deltaTime);
        currentState?.UpdateElapsedTime(Time.deltaTime, this);
        currentState?.Tick(Time.deltaTime, this);
        CooldownManager.Update(Time.deltaTime);
    }

    public GameObjectStateStruct GetGameObjectState(string stateName)
    {
        return gameObjectStateMap[stateName];
    }

    public void ChangeState(string newStateName)
    {
        string oldStateName = currentStateName;
        GameObjectState newState = gameObjectStateMap[newStateName].gameObjectState;
        AnimationClip animation = gameObjectStateMap[newStateName].animation;
        PhysicsMaterial physicsMaterial = gameObjectStateMap[newStateName].physicsMaterial;

        if (currentState != null && (newState == currentState || CooldownManager.IsOnCooldown(newState.ToString())))
        {
            return;
        }

        Debug.Log("STATE  " + currentState + " => " + newState);

        currentState?.ExitState(this);
        currentState?.StartCooldown();

        currentState = newState;
        currentStateName = newStateName;

        if (animation)
        {
            var anim = animancerComponent.Play(animation);
            anim.Events(this).OnEnd = () => currentState.OnAnimationComplete(this);
        }

        currentState?.Reinitialize();
        currentState?.EnterState(this);

        OnStateChange?.Invoke();
        OnChangeState(oldStateName, newStateName);
    }

    public abstract void OnUpdate(float deltaTime);

    public abstract void OnStart();

    public abstract void OnChangeState(string oldStateName, string newStateName);
}