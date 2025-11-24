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

    // ==== PlayerPrefs Keys ====
    private const string KEY_SFX = "sfxVolume";
    private const string KEY_MUSIC = "musicVolume";
    private const string KEY_EXPLOSIONS = "explosionsEnabled";

    // Flag global pra outros scripts checarem
    public static bool ExplosionsEnabled
    {
        get { return PlayerPrefs.GetInt(KEY_EXPLOSIONS, 1) != 0; }
    }

    // ===========================
    // NOVO: MÚSICAS POR FASE
    // ===========================
    [System.Serializable]
    public class LevelMusic
    {
        [Tooltip("Nome da cena como em Build Settings (ex: 'Level_1').")]
        public string sceneName;
        public AudioClip musicClip;
        [Range(0f, 1f)] public float targetVolume = 1f;
        public bool loop = true;
    }

    [Header("Level Musics (configurar no Inspector)")]
    [Tooltip("Lista de músicas associadas a nomes de cena (use o mesmo nome que está em Build Settings).")]
    public List<LevelMusic> levelMusics = new List<LevelMusic>();

    [Tooltip("Se true, o AudioManager tentará tocar a música configurada para a cena automaticamente no carregamento.")]
    public bool autoPlayLevelMusicOnLoad = true;

    // referência para a música de nível atualmente ativa (pode ser usada para evitar replays)
    private LevelMusic currentLevelMusic = null;

    // ===========================

    void Awake()
    {
        if (audioInstance != null && audioInstance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        audioInstance = this;
        DontDestroyOnLoad(gameObject);

        // Cria/garante as fontes
        if (!sfxSource)   sfxSource   = gameObject.AddComponent<AudioSource>();
        if (!musicSource) musicSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake   = false;
        musicSource.playOnAwake = false;
        musicSource.loop        = true;

        // ====== Carrega volumes salvos de forma segura ======

        

        sfxVolume = PlayerPrefs.GetFloat(KEY_SFX, 1f);
        musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC, 1f);

        sfxVolume = Mathf.Clamp01(sfxVolume);
        musicVolume = Mathf.Clamp01(musicVolume);

        if (!PlayerPrefs.HasKey(KEY_EXPLOSIONS))
        {
            PlayerPrefs.SetInt(KEY_EXPLOSIONS, 1);
        }

        sfxSource.volume   = sfxVolume;
        musicSource.volume = musicVolume;

        Debug.Log($"[AudioManager] music={musicVolume}, sfx={sfxVolume}, explosions={ExplosionsEnabled}");
    }

    private void OnEnable()
    {
        if (autoPlayLevelMusicOnLoad)
            SceneManager.sceneLoaded += OnSceneLoaded_AutoPlay;
    }

    private void OnDisable()
    {
        if (autoPlayLevelMusicOnLoad)
            SceneManager.sceneLoaded -= OnSceneLoaded_AutoPlay;
    }

    private void OnSceneLoaded_AutoPlay(Scene scene, LoadSceneMode mode)
    {
        // ALTERAÇÃO: verifica se já está tocando a música correta antes de tentar tocar
        LevelMusic lm = GetLevelMusicForScene(scene.name);
        
        // Se já está tocando a música desta cena, não faz nada (mantém tocando)
        if (lm != null && 
            currentLevelMusic != null && 
            currentLevelMusic.sceneName == lm.sceneName && 
            musicSource.isPlaying && 
            musicSource.clip == lm.musicClip)
        {
            Debug.Log($"[AudioManager] Música da cena '{scene.name}' já está tocando. Continuando sem reiniciar.");
            return;
        }
        
        // Caso contrário, toca a música configurada para a cena
        PlayLevelMusic(scene.name);
    }

    // ======= SFX =======
    public void JumpSFX()
    {
        if (jumpSFX) sfxSource.PlayOneShot(jumpSFX, sfxVolume);
    }

    public void LogSFX()
    {
        if (logSFX) sfxSource.PlayOneShot(logSFX, sfxVolume);
    }

    public void DeathSFX()
    {
        if (deathSFX) sfxSource.PlayOneShot(deathSFX, sfxVolume);
    }

    public void ExplodeSFX()
    {
        if (!ExplosionsEnabled) return;
        if (explosionSFX) sfxSource.PlayOneShot(explosionSFX, sfxVolume);
    }

    public void BossParrySFX()
    {
        if (parrySFX) sfxSource.PlayOneShot(parrySFX, sfxVolume);
    }

    // ======= BGM / Música de fase =======

    /// <summary>
    /// Toca a música configurada para um nome de cena (se existir).
    /// Se não encontrar, não faz nada (mantém música atual).
    /// </summary>
    public bool PlayLevelMusic(string sceneName, float crossfadeTime = 0.5f)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;

        LevelMusic lm = GetLevelMusicForScene(sceneName);
        if (lm == null || lm.musicClip == null) return false;

        // já está tocando essa música? atualiza volume/loop e retorna
        if (currentLevelMusic != null && currentLevelMusic.sceneName == lm.sceneName && musicSource.clip == lm.musicClip)
        {
            musicSource.loop = lm.loop;
            musicSource.volume = Mathf.Clamp01(lm.targetVolume);
            currentLevelMusic = lm;
            return true;
        }

        // crossfade para a música de nível
        Crossfade(lm.musicClip, crossfadeTime, lm.loop, lm.targetVolume);
        currentLevelMusic = lm;
        return true;
    }

    /// <summary>
    /// Play por buildIndex (pega o nome da cena do BuildSettings)
    /// </summary>
    public bool PlayLevelMusic(int buildIndex, float crossfadeTime = 0.5f)
    {
        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings) return false;
        string path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
        return PlayLevelMusic(sceneName, crossfadeTime);
    }

    /// <summary>
    /// Para a música de nível atual (opcional crossfade out)
    /// </summary>
    public void StopLevelMusic(float fadeOutTime = 0.5f)
    {
        currentLevelMusic = null;
        if (fadeOutTime > 0f) FadeOutMusic(fadeOutTime);
        else StopMusic();
    }

    /// <summary>
    /// Crossfade para a música passada (utilitário que preserva compatibilidade).
    /// </summary>
    public void CrossfadeToLevelMusic(string sceneName, float time = 0.5f)
    {
        LevelMusic lm = GetLevelMusicForScene(sceneName);
        if (lm != null && lm.musicClip != null)
            Crossfade(lm.musicClip, time, lm.loop, lm.targetVolume);
    }

    private LevelMusic GetLevelMusicForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;
        for (int i = 0; i < levelMusics.Count; i++)
        {
            if (!string.IsNullOrEmpty(levelMusics[i].sceneName) &&
                levelMusics[i].sceneName == sceneName)
            {
                return levelMusics[i];
            }
        }
        return null;
    }

    // Mantive seus métodos públicos originais para retrocompatibilidade
    public void PlayMusic(AudioClip clip, bool loop = true, float volume01 = -1f)
    {
        if (!clip) return;
        musicSource.loop = loop;
        if (volume01 >= 0f) musicSource.volume = Mathf.Clamp01(volume01);
        musicSource.clip = clip;
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

    // Fade out/in da música atual
    public void FadeOutMusic(float time = 0.5f) =>
        StartCoroutine(FadeVolume(musicSource, musicSource.volume, 0f, time));

    public void FadeInMusic(float target = 1f, float time = 0.5f) =>
        StartCoroutine(FadeVolume(musicSource, musicSource.volume, Mathf.Clamp01(target), time));

    // Crossfade público (usa rotina interna)
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