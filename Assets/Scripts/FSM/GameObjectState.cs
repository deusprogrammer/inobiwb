using UnityEngine;

public abstract class GameObjectState : ScriptableObject
{
    protected float duration = 0;
    protected float elapsed = 0;
    protected int cooldown = 0;

    protected PhysicsMaterial physicsMaterial;
    protected GameObjectStateController stateManager;

    public void Initialize(GameObjectStateController stateManager, float duration, int cooldown)
    {
        this.stateManager = stateManager;
        this.duration = duration;
        this.cooldown = cooldown;
    }

    public void Reinitialize()
    {
        elapsed = 0;
    }

    public virtual void EnterState(GameObjectStateController controller) { }
    public virtual void ExitState(GameObjectStateController controller) { }
    public void StartCooldown()
    {
        if (cooldown > 0)
        {
            CooldownManager.AddCooldown(this.ToString(), cooldown, OnCooldownTick, OnCooldownComplete);
        }
    }
    public void UpdateElapsedTime(float deltaTime, GameObjectStateController controller)
    {
        if (duration > 0 && elapsed < duration)
        {
            elapsed += deltaTime;
        }

        if (duration > 0 && elapsed >= duration)
        {
            OnComplete(controller);
        }
    }
    public virtual void Tick(float deltaTime, GameObjectStateController controller) { }
    public virtual void OnComplete(GameObjectStateController controller) { }
    public virtual void OnAnimationComplete(GameObjectStateController controller) { }
    public virtual void OnCooldownTick(float deltaTime) { }
    public virtual void OnCooldownComplete() { }

    public abstract void OnEvent(string eventName, GameObjectStateController controller);
}