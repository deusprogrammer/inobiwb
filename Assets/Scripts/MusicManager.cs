using UnityEngine;

/// <summary>
/// Simple music manager for playing background music in each scene.
/// Persists across scene loads and handles transitions between different tracks.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    [Header("Music Settings")]
    [SerializeField]
    [Tooltip("The music track to play in this scene")]
    private AudioClip musicClip;
    
    [SerializeField]
    [Tooltip("Whether the music should loop continuously")]
    private bool loop = true;
    
    [SerializeField]
    [Tooltip("Volume of the music (0-1)")]
    [Range(0f, 1f)]
    private float volume = 1f;
    
    private AudioSource audioSource;
    private static MusicManager instance;
    
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        Debug.Log($"[MusicManager] Awake on GameObject: {gameObject.name}");
        
        // If this is the first MusicManager, keep it alive across scenes
        if (instance == null)
        {
            instance = this;
            
            // Create a dedicated persistent GameObject for the music system
            GameObject musicContainer = new GameObject("MusicManager (Persistent)");
            DontDestroyOnLoad(musicContainer);
            
            // Stop and copy AudioSource to the new container
            AudioSource source = GetComponent<AudioSource>();
            source.Stop(); // Stop the original audio source!
            
            AudioSource newSource = musicContainer.AddComponent<AudioSource>();
            newSource.clip = source.clip;
            newSource.volume = source.volume;
            newSource.loop = source.loop;
            newSource.playOnAwake = false;
            audioSource = newSource;
            
            // Move MusicManager component
            MusicManager newManager = musicContainer.AddComponent<MusicManager>();
            newManager.musicClip = this.musicClip;
            newManager.loop = this.loop;
            newManager.volume = this.volume;
            newManager.audioSource = newSource;
            instance = newManager;
            
            Debug.Log($"[MusicManager] Created persistent container - original GameObject can be destroyed");
            newManager.PlayMusic();
            
            // Destroy this component (original scene object can now be destroyed normally)
            Destroy(this);
        }
        else
        {
            Debug.Log($"[MusicManager] Duplicate found - instance is on: {instance.gameObject.name}");
            
            // Another MusicManager exists - check if we should switch tracks
            if (instance.musicClip != musicClip)
            {
                // Different music - switch to this one
                instance.SwitchMusic(musicClip, loop, volume);
            }
            else
            {
                // Same music - update loop/volume settings in case they changed
                instance.UpdateSettings(loop, volume);
            }
            
            // Destroy this duplicate MusicManager component only (not the entire GameObject!)
            Debug.Log($"[MusicManager] Destroying duplicate component on GameObject: {gameObject.name}");
            Destroy(this);
        }
    }
    
    private void PlayMusic()
    {
        if (musicClip == null)
        {
            Debug.LogWarning("[MusicManager] No music clip assigned!");
            return;
        }
        
        audioSource.clip = musicClip;
        audioSource.loop = loop;
        audioSource.volume = volume;
        audioSource.Play();
        
        Debug.Log($"[MusicManager] Playing music: {musicClip.name}, Loop: {loop}");
    }
    
    private void SwitchMusic(AudioClip newClip, bool newLoop, float newVolume)
    {
        if (newClip == null)
        {
            Debug.Log("[MusicManager] No clip provided - stopping music");
            audioSource.Stop();
            audioSource.clip = null;
            return;
        }
        
        // Don't restart if it's the same clip already playing
        if (audioSource.clip == newClip && audioSource.isPlaying)
        {
            Debug.Log($"[MusicManager] Already playing {newClip.name}, just updating settings");
            audioSource.loop = newLoop;
            audioSource.volume = newVolume;
            return;
        }
        
        Debug.Log($"[MusicManager] Switching to music: {newClip.name}, Loop: {newLoop}");
        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.loop = newLoop;
        audioSource.volume = newVolume;
        audioSource.Play();
    }
    
    private void UpdateSettings(bool newLoop, float newVolume)
    {
        audioSource.loop = newLoop;
        audioSource.volume = newVolume;
    }
}
