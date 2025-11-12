using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager audioInstance;

    [Header("SFX Clips")]
    public AudioClip jumpSFX;
    public AudioClip logSFX;
    public AudioClip deathSFX;
    public AudioClip explosionSFX;

    [Header("Sources (auto-criados se nulos)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Header("Volumes")]
    [Range(0f,1f)] public float sfxVolume = 1f;
    [Range(0f,1f)] public float musicVolume = 1f;

    void Awake()
    {
        if (audioInstance != null && audioInstance != this) { Destroy(gameObject); return; }
        audioInstance = this;
        DontDestroyOnLoad(gameObject);

        // Cria/garante as fontes
        if (!sfxSource)   sfxSource   = gameObject.AddComponent<AudioSource>();
        if (!musicSource) musicSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake   = false;
        musicSource.playOnAwake = false;
        musicSource.loop        = true;

        sfxSource.volume   = sfxVolume;
        musicSource.volume = musicVolume;
    }

    // ======= SFX =======
    public void JumpSFX()
    {
        if (jumpSFX) sfxSource.PlayOneShot(jumpSFX, sfxVolume);
    }
    public void LogSFX()
    {
        if (logSFX) sfxSource.PlayOneShot(logSFX, sfxVolume); // corrigido
    }
    public void DeathSFX()
    {
        if (deathSFX) sfxSource.PlayOneShot(deathSFX, sfxVolume); // corrigido
    }
    public void ExplodeSFX()
    {
        if (explosionSFX) sfxSource.PlayOneShot(explosionSFX, sfxVolume);
    }

    // ======= BGM =======
    public void PlayMusic(AudioClip clip, bool loop = true, float volume01 = -1f)
    {
        if (!clip) return;
        musicSource.loop = loop;
        if (volume01 >= 0f) musicSource.volume = Mathf.Clamp01(volume01);
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic() => musicSource.Stop();
    public void PauseMusic(bool pause) { if (pause) musicSource.Pause(); else musicSource.UnPause(); }

    public void SetMusicVolume(float v01) { musicVolume = Mathf.Clamp01(v01); musicSource.volume = musicVolume; }
    public void SetSfxVolume(float v01)   { sfxVolume   = Mathf.Clamp01(v01); sfxSource.volume   = sfxVolume; }

    // Fade out/in da música atual
    public void FadeOutMusic(float time = 0.5f) => StartCoroutine(FadeVolume(musicSource, musicSource.volume, 0f, time));
    public void FadeInMusic(float target = 1f, float time = 0.5f) => StartCoroutine(FadeVolume(musicSource, musicSource.volume, Mathf.Clamp01(target), time));

    // Troca de faixa com crossfade
    public void Crossfade(AudioClip newClip, float time = 0.5f, bool loop = true, float targetVolume = -1f)
    {
        StartCoroutine(CrossfadeRoutine(newClip, time, loop, targetVolume));
    }

    private IEnumerator CrossfadeRoutine(AudioClip newClip, float time, bool loop, float targetVolume)
    {
        if (!newClip) yield break;

        float startVol = musicSource.volume;
        // cria um "ghost" pra fade-out se estiver tocando algo
        AudioSource ghost = null;
        if (musicSource.isPlaying && musicSource.clip != null)
        {
            ghost = gameObject.AddComponent<AudioSource>();
            ghost.clip = musicSource.clip;
            ghost.volume = musicSource.volume;
            ghost.loop = false;
            ghost.Play();
        }

        // prepara a nova
        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.volume = 0f;
        musicSource.Play();

        float t = 0f;
        float endVol = (targetVolume >= 0f ? Mathf.Clamp01(targetVolume) : startVol);
        while (t < time)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / time);
            musicSource.volume = Mathf.Lerp(0f, endVol, k);
            if (ghost) ghost.volume = Mathf.Lerp(startVol, 0f, k);
            yield return null;
        }
        musicSource.volume = endVol;
        if (ghost) Destroy(ghost);
    }

    private IEnumerator FadeVolume(AudioSource src, float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / time);
            src.volume = Mathf.Lerp(from, to, k);
            yield return null;
        }
        src.volume = to;
    }
}
