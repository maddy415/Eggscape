using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SettingsMenu : MonoBehaviour
{
    private const string KEY_MUSIC = "musicVolume";
    private const string KEY_SFX = "sfxVolume";
    private const string KEY_EXPLOSIONS = "explosionsEnabled";

    [Header("Root (opcional)")]
    [SerializeField] private GameObject root;

    [Header("UI")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle explosionsToggle;
    
    [Header("Labels opcionais")]
    [SerializeField] private TextMeshProUGUI musicLabel;
    [SerializeField] private TextMeshProUGUI sfxLabel;

    private bool canSave = false;
    private float lastMusicValue = 1f;
    private float lastSfxValue = 1f;
    private float lastSfxTestTime = 0f;
    private const float SFX_TEST_COOLDOWN = 0.5f; // 200ms entre cada som

    private void Awake()
    {
        if (root == null) root = gameObject;
        
        // Configura os sliders
        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.wholeNumbers = false;
        }
        
        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.wholeNumbers = false;
        }
    }

    private void OnEnable()
    {
        canSave = false;
        
        // Carrega valores salvos
        float music = PlayerPrefs.GetFloat(KEY_MUSIC, 1f);
        float sfx = PlayerPrefs.GetFloat(KEY_SFX, 1f);
        bool expl = PlayerPrefs.GetInt(KEY_EXPLOSIONS, 1) != 0;

        // Se AudioManager existe, usa os valores dele
        if (AudioManager.audioInstance != null)
        {
            music = AudioManager.audioInstance.musicVolume;
            sfx = AudioManager.audioInstance.sfxVolume;
        }

        // Garante valores válidos
        music = Mathf.Clamp01(music);
        sfx = Mathf.Clamp01(sfx);
        
        lastMusicValue = music;
        lastSfxValue = sfx;

        Debug.Log($"[Settings] Loading: Music={music:F2}, SFX={sfx:F2}, Explosions={expl}");

        // Remove listeners temporariamente pra evitar callbacks durante o setup
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.value = music;
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            UpdateLabel(musicLabel, music);
        }
        
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.value = sfx;
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
            UpdateLabel(sfxLabel, sfx);
        }
        
        if (explosionsToggle != null)
        {
            explosionsToggle.onValueChanged.RemoveAllListeners();
            explosionsToggle.isOn = expl;
            explosionsToggle.onValueChanged.AddListener(OnExplosionsToggleChanged);
        }

        // Espera 1 frame antes de permitir salvar
        StartCoroutine(EnableSavingAfterDelay());
    }

    private IEnumerator EnableSavingAfterDelay()
    {
        yield return null; // espera 1 frame
        canSave = true;
        Debug.Log("[Settings] Ready to save changes");
    }

    public void OnMusicSliderChanged(float rawValue)
    {
        if (!canSave)
        {
            Debug.Log($"[Settings] Music change IGNORED (not ready): {rawValue:F4}");
            return;
        }

        // Arredonda pra evitar valores estranhos
        float value = Mathf.Round(rawValue * 100f) / 100f;
        value = Mathf.Clamp01(value);

        // Ignora mudanças muito pequenas (ruído)
        if (Mathf.Abs(value - lastMusicValue) < 0.01f)
        {
            Debug.Log($"[Settings] Music change too small, ignored: {value:F2}");
            return;
        }

        lastMusicValue = value;

        Debug.Log($"[Settings] Music saved: {value:F2}");

        // Salva
        PlayerPrefs.SetFloat(KEY_MUSIC, value);
        PlayerPrefs.Save();

        // Aplica
        if (AudioManager.audioInstance != null)
        {
            AudioManager.audioInstance.musicVolume = value;
            AudioManager.audioInstance.SetMusicVolume(value);
        }

        UpdateLabel(musicLabel, value);
    }

    public void OnSfxSliderChanged(float rawValue)
    {
        if (!canSave)
        {
            Debug.Log($"[Settings] SFX change IGNORED (not ready): {rawValue:F4}");
            return;
        }

        float value = Mathf.Round(rawValue * 100f) / 100f;
        value = Mathf.Clamp01(value);

        // Ignora mudanças muito pequenas
        if (Mathf.Abs(value - lastSfxValue) < 0.01f)
        {
            Debug.Log($"[Settings] SFX change too small, ignored: {value:F2}");
            return;
        }

        lastSfxValue = value;

        Debug.Log($"[Settings] SFX saved: {value:F2}");

        // Salva
        PlayerPrefs.SetFloat(KEY_SFX, value);
        PlayerPrefs.Save();

        // Aplica
        if (AudioManager.audioInstance != null)
        {
            AudioManager.audioInstance.sfxVolume = value;
            AudioManager.audioInstance.SetSfxVolume(value);
            
            // Toca feedback COM COOLDOWN pra não spammar
            if (value > 0.05f && Time.unscaledTime - lastSfxTestTime > SFX_TEST_COOLDOWN)
            {
                AudioManager.audioInstance.JumpSFX();
                lastSfxTestTime = Time.unscaledTime;
            }
        }

        UpdateLabel(sfxLabel, value);
    }

    public void OnExplosionsToggleChanged(bool value)
    {
        if (!canSave)
        {
            Debug.Log($"[Settings] Explosions change IGNORED (not ready): {value}");
            return;
        }

        Debug.Log($"[Settings] Explosions saved: {value}");

        PlayerPrefs.SetInt(KEY_EXPLOSIONS, value ? 1 : 0);
        PlayerPrefs.Save();

        if (AudioManager.audioInstance != null)
        {
            AudioManager.audioInstance.SetExplosionsEnabled(value);
        }
    }

    public void ResetToDefaults()
    {
        Debug.Log("[Settings] Resetting to defaults...");

        PlayerPrefs.SetFloat(KEY_MUSIC, 1f);
        PlayerPrefs.SetFloat(KEY_SFX, 1f);
        PlayerPrefs.SetInt(KEY_EXPLOSIONS, 1);
        PlayerPrefs.Save();

        if (AudioManager.audioInstance != null)
        {
            AudioManager.audioInstance.musicVolume = 1f;
            AudioManager.audioInstance.sfxVolume = 1f;
            AudioManager.audioInstance.SetMusicVolume(1f);
            AudioManager.audioInstance.SetSfxVolume(1f);
            AudioManager.audioInstance.SetExplosionsEnabled(true);
        }

        OnEnable();
    }

    private void UpdateLabel(TextMeshProUGUI label, float value)
    {
        if (label != null)
        {
            label.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    private void OnDisable()
    {
        // Limpa listeners quando desabilitar pra evitar leaks
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveAllListeners();
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveAllListeners();
        if (explosionsToggle != null)
            explosionsToggle.onValueChanged.RemoveAllListeners();
    }
}