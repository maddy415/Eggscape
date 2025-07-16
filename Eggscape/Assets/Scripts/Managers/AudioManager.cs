using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager audioInstance;
    public AudioClip jumpSFX;
    public AudioClip logSFX;
    public AudioClip deathSFX;
    public AudioClip explosionSFX;

    
    
    
    public AudioSource audioSource;
    
    void Awake()
    {
        if (audioInstance == null)
        {
            audioInstance = this; 
        }
        else
        {
            Destroy(gameObject); 
        }
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }


    public void JumpSFX()
    {
        if (jumpSFX != null)
        {
            audioSource.PlayOneShot(jumpSFX);   
        }
    }
    
    public void LogSFX()
    {
        if (jumpSFX != null)
        {
            audioSource.PlayOneShot(logSFX);   
        }
    }
    
    public void DeathSFX()
    {
        if (jumpSFX != null)
        {
            audioSource.PlayOneShot(deathSFX);   
        }
    }

    public void ExplodeSFX()
    {
        if (explosionSFX != null)
        {
            audioSource.PlayOneShot(explosionSFX);   
        }
    }
}
