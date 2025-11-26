using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager audioInstance;

    [Header("SFX Clips")]
    public AudioClip jumpSFX;
    public AudioClip logSFX;
    public AudioClip deathSFX;
    public AudioClip explosionSFX;
    public AudioClip parrySFX;

    [Header("Sources (auto-criados se nulos)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Header("Volumes")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;

    private const string KEY_SFX = "sfxVolume";
    private const string KEY_MUSIC = "musicVolume";
    private const string KEY_EXPLOSIONS = "explosionsEnabled";

    public static bool ExplosionsEnabled => PlayerPrefs.GetInt(KEY_EXPLOSIONS, 1) != 0;

    [System.Serializable]
    public class LevelMusic
    {
        public string sceneName;
        public AudioClip musicClip;
        [Range(0f, 1f)] public float targetVolume = 1f;
        public bool loop = true;
    }

    [Header("Level Musics (configurar no Inspector)")]
    public List<LevelMusic> levelMusics = new List<LevelMusic>();
    public bool autoPlayLevelMusicOnLoad = true;

    private LevelMusic currentLevelMusic = null;
    
    // ===== NOVO: Armazena o timestamp da música atual =====
    private float savedMusicTime = 0f;
    private bool preserveMusicTime = false;

    private void Awake()
    {
        if (audioInstance != null && audioInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        audioInstance = this;
        DontDestroyOnLoad(gameObject);

        if (!sfxSource) sfxSource = gameObject.AddComponent<AudioSource>();
        if (!musicSource) musicSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        musicSource.playOnAwake = false;

        sfxSource.clip = null;
        musicSource.clip = null;

        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_SFX, 1f));
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_MUSIC, 1f));

        sfxSource.volume = sfxVolume;
        musicSource.volume = musicVolume;

        if (!PlayerPrefs.HasKey(KEY_EXPLOSIONS))
            PlayerPrefs.SetInt(KEY_EXPLOSIONS, 1);

        SceneManager.sceneLoaded += OnSceneLoaded_AutoPlay;

        Debug.Log($"[AudioManager] Awake: music={musicVolume}, sfx={sfxVolume}, explosions={ExplosionsEnabled}");
    }

    private void Start()
    {
        StartCoroutine(PlayMusicNextFrame());
    }

    private IEnumerator PlayMusicNextFrame()
    {
        yield return null;
        if (autoPlayLevelMusicOnLoad)
        {
            PlayLevelMusic(SceneManager.GetActiveScene().name, 0f);
        }
    }

    private void OnSceneLoaded_AutoPlay(Scene scene, LoadSceneMode mode)
    {
        if (!autoPlayLevelMusicOnLoad) return;

        LevelMusic lm = GetLevelMusicForScene(scene.name);
        if (lm == null) return;

        // ===== MUDANÇA: Verifica se é a mesma música =====
        bool isSameMusic = (currentLevelMusic != null && 
                           currentLevelMusic.musicClip == lm.musicClip &&
                           musicSource.clip == lm.musicClip);

        if (isSameMusic && musicSource.isPlaying)
        {
            // Música já está tocando - apenas ajusta configurações
            Debug.Log($"[AudioManager] Mesma música detectada ({lm.musicClip.name}) - mantendo reprodução contínua");
            musicSource.loop = lm.loop;
            musicSource.volume = Mathf.Clamp01(lm.targetVolume * musicVolume);
            currentLevelMusic = lm;
            
            // ===== NOVO: Restaura o tempo se estava preservando =====
            if (preserveMusicTime && savedMusicTime > 0f)
            {
                musicSource.time = savedMusicTime;
                Debug.Log($"[AudioManager] Tempo da música restaurado: {savedMusicTime:F2}s");
            }
            
            preserveMusicTime = false;
            return;
        }

        // Música diferente - toca normalmente
        PlayLevelMusic(scene.name);
    }

    private LevelMusic GetLevelMusicForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;
        return levelMusics.Find(lm => lm.sceneName == sceneName);
    }

    public bool PlayLevelMusic(string sceneName, float crossfadeTime = 0.5f)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;

        LevelMusic lm = GetLevelMusicForScene(sceneName);
        if (lm == null || lm.musicClip == null) return false;

        // ===== MUDANÇA: Verifica se já é a música atual =====
        if (currentLevelMusic != null && currentLevelMusic.musicClip == lm.musicClip && 
            musicSource.clip == lm.musicClip && musicSource.isPlaying)
        {
            Debug.Log($"[AudioManager] PlayLevelMusic: Já tocando {lm.musicClip.name} - mantendo");
            musicSource.loop = lm.loop;
            musicSource.volume = Mathf.Clamp01(lm.targetVolume * musicVolume);
            currentLevelMusic = lm;
            return true;
        }

        Crossfade(lm.musicClip, crossfadeTime, lm.loop, lm.targetVolume);
        currentLevelMusic = lm;
        return true;
    }

    public void StopLevelMusic(float fadeOutTime = 0.5f)
    {
        currentLevelMusic = null;
        if (fadeOutTime > 0f) FadeOutMusic(fadeOutTime);
        else StopMusic();
    }

    public void PlayMusic(AudioClip clip, bool loop = true, float volume01 = -1f)
    {
        if (!clip) return;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = (volume01 >= 0f ? Mathf.Clamp01(volume01) : musicSource.volume);
        musicSource.Play();
    }

    public void StopMusic() => musicSource.Stop();

    public void PauseMusic(bool pause)
    {
        if (pause) musicSource.Pause();
        else musicSource.UnPause();
    }

    public void SetMusicVolume(float v01)
    {
        musicVolume = Mathf.Clamp01(v01);
        musicSource.volume = musicVolume;
        PlayerPrefs.SetFloat(KEY_MUSIC, musicVolume);
    }

    public void SetSfxVolume(float v01)
    {
        sfxVolume = Mathf.Clamp01(v01);
        sfxSource.volume = sfxVolume;
        PlayerPrefs.SetFloat(KEY_SFX, sfxVolume);
    }

    public void SetExplosionsEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(KEY_EXPLOSIONS, enabled ? 1 : 0);
    }

    public void FadeOutMusic(float time = 0.5f) =>
        StartCoroutine(FadeVolume(musicSource, musicSource.volume, 0f, time));

    private IEnumerator FadeVolume(AudioSource src, float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(from, to, Mathf.Clamp01(t / time));
            yield return null;
        }
        src.volume = to;
    }

    public void Crossfade(AudioClip newClip, float time = 0.5f, bool loop = true, float targetVolume = -1f)
    {
        StartCoroutine(CrossfadeRoutine(newClip, time, loop, targetVolume));
    }

    private IEnumerator CrossfadeRoutine(AudioClip newClip, float time, bool loop, float targetVolume)
    {
        if (!newClip) yield break;

        float startVol = musicSource.volume;
        AudioSource ghost = null;

        if (musicSource.isPlaying && musicSource.clip != null)
        {
            ghost = gameObject.AddComponent<AudioSource>();
            ghost.clip = musicSource.clip;
            ghost.volume = musicSource.volume;
            ghost.loop = false;
            ghost.Play();
        }

        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.volume = 0f;
        musicSource.Play();

        float t = 0f;
        float endVol = (targetVolume >= 0f ? Mathf.Clamp01(targetVolume) : startVol);

        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            musicSource.volume = Mathf.Lerp(0f, endVol, k);
            if (ghost) ghost.volume = Mathf.Lerp(startVol, 0f, k);
            yield return null;
        }

        musicSource.volume = endVol;
        if (ghost) Destroy(ghost);
    }

    // ===== NOVO: Métodos para preservar a posição da música =====
    
    /// <summary>
    /// Salva o tempo atual da música para ser restaurado após reload.
    /// Chame isso ANTES de recarregar a cena (ex: ao morrer).
    /// </summary>
    public void PreserveMusicPosition()
    {
        if (musicSource != null && musicSource.isPlaying && musicSource.clip != null)
        {
            savedMusicTime = musicSource.time;
            preserveMusicTime = true;
            Debug.Log($"[AudioManager] Posição da música preservada: {savedMusicTime:F2}s de {musicSource.clip.length:F2}s");
        }
    }

    /// <summary>
    /// Cancela a preservação da música (útil se quiser resetar a música propositalmente).
    /// </summary>
    public void ClearPreservedMusicPosition()
    {
        savedMusicTime = 0f;
        preserveMusicTime = false;
    }

    // ===== SFX =====
    public void JumpSFX() { if (jumpSFX) sfxSource.PlayOneShot(jumpSFX, sfxVolume); }
    public void LogSFX() { if (logSFX) sfxSource.PlayOneShot(logSFX, sfxVolume); }
    public void DeathSFX() { if (deathSFX) sfxSource.PlayOneShot(deathSFX, sfxVolume); }
    public void ExplodeSFX() { if (ExplosionsEnabled && explosionSFX) sfxSource.PlayOneShot(explosionSFX, sfxVolume); }
    public void BossParrySFX() { if (parrySFX) sfxSource.PlayOneShot(parrySFX, sfxVolume); }
}