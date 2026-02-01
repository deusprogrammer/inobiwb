using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Single input manager that forwards input to the currently active player
/// </summary>
public class PlayerInputManager : MonoBehaviour
{
    [Header("Player References")]
    [Tooltip("Main player (starts active). Set by LevelLoader at runtime.")]
    public HomeBoyStateController mainPlayer;
    
    [Tooltip("Partner player (starts inactive). Set by LevelLoader at runtime.")]
    public HomeBoyStateController partner;
    
    private HomeBoyStateController activePlayer;
    private bool isActive = false;
    
    private static PlayerInputManager instance;
    public static PlayerInputManager Instance { get { return instance; } }

    void Awake()
    {
        instance = this;
        Debug.Log("[PlayerInputManager] Initialized, waiting for LevelLoader to activate");
    }
    
    public void SetPlayers(HomeBoyStateController main, HomeBoyStateController partnerPlayer)
    {
        mainPlayer = main;
        partner = partnerPlayer;
        activePlayer = mainPlayer;
        
        // Link them together
        if (mainPlayer != null && partner != null)
        {
            mainPlayer.partnerController = partner;
            partner.partnerController = mainPlayer;
            Debug.Log("[PlayerInputManager] Players linked");
        }
        
        Debug.Log($"[PlayerInputManager] Players set - Main: {mainPlayer?.gameObject.name}, Partner: {partner?.gameObject.name}, Active: {activePlayer?.gameObject.name}");
    }
    
    public void Activate()
    {
        isActive = true;
        Debug.Log("[PlayerInputManager] Activated - input enabled");
    }

    void OnMove(InputValue inputValue)
    {
        if (!isActive)
        {
            Debug.Log("[PlayerInputManager] OnMove blocked - not active yet");
            return;
        }
        
        Debug.Log($"[PlayerInputManager] OnMove called, activePlayer: {(activePlayer != null ? activePlayer.gameObject.name : "NULL")}, inputValue: {inputValue.Get<Vector2>()}");
        
        if (activePlayer != null)
        {
            activePlayer.HandleMove(inputValue);
        }
        else
        {
            Debug.LogError("[PlayerInputManager] OnMove called but activePlayer is NULL!");
        }
    }

    void OnPush()
    {
        if (!isActive) return;
        
        Debug.Log($"[PlayerInputManager] OnPush called, activePlayer: {(activePlayer != null ? activePlayer.gameObject.name : "NULL")}");
        
        if (activePlayer != null)
        {
            activePlayer.HandlePush();
        }
        else
        {
            Debug.LogError("[PlayerInputManager] OnPush called but activePlayer is NULL!");
        }
    }

    void OnLook(InputValue inputValue)
    {
        if (!isActive) return;
        
        Debug.Log($"[PlayerInputManager] OnLook called, activePlayer: {(activePlayer != null ? activePlayer.gameObject.name : "NULL")}, inputValue: {inputValue.Get<Vector2>()}");
        
        if (activePlayer != null)
        {
            activePlayer.HandleLook(inputValue);
        }
        else
        {
            Debug.LogError("[PlayerInputManager] OnLook called but activePlayer is NULL!");
        }
    }

    void OnPartnerSwitch()
    {
        if (!isActive) return;
        
        if (activePlayer != null)
        {
            // Toggle between players
            HomeBoyStateController newActive = (activePlayer == mainPlayer) ? partner : mainPlayer;
            
            if (newActive != null)
            {
                activePlayer.isActive = false;
                newActive.isActive = true;
                activePlayer = newActive;
                
                // Update camera
                CameraController camera = FindFirstObjectByType<CameraController>();
                if (camera != null)
                {
                    camera.SetTarget(newActive.transform);
                }
                
                Debug.Log($"[PlayerInputManager] Switched to {newActive.gameObject.name}");
            }
        }
    }
}
