using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using TMPro;

/// <summary>
/// Вешается на объект SettingsManager в сцене.
/// Все панели (Graphics / Gameplay / Audio / Other) управляются отсюда.
/// Стрелочки — просто две кнопки < > рядом с текстом текущего значения.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    // ── Вкладки ───────────────────────────────────────────────────────────
    [Header("Панели вкладок")]
    public GameObject panelGraphics;
    public GameObject panelGameplay;
    public GameObject panelAudio;
    public GameObject panelOther;

    // ── Графика ───────────────────────────────────────────────────────────
    [Header("Графика — качество")]
    public TextMeshProUGUI qualityValueText;
    private string[] _qualityLevels = { "Низкое", "Среднее", "Высокое", "Ультра" };
    private int _qualityIndex = 1;

    [Header("Графика — разрешение")]
    public TextMeshProUGUI resolutionValueText;
    private Resolution[] _resolutions;
    private int _resolutionIndex;

    [Header("Графика — полноэкранный режим")]
    public TextMeshProUGUI fullscreenValueText;
    private string[] _fullscreenModes = { "Оконный", "Без рамки", "Полный экран" };
    private int _fullscreenIndex = 2;

    [Header("Графика — VSync")]
    public TextMeshProUGUI vsyncValueText;
    private string[] _vsyncModes = { "Выкл", "Вкл" };
    private int _vsyncIndex = 1;

    [Header("Графика — HDRP Volume (опционально)")]
    public Volume globalVolume; // назначь глобальный Volume если хочешь управлять bloom/aa

    // ── Геймплей ──────────────────────────────────────────────────────────
    [Header("Геймплей — чувствительность")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValueText;

    [Header("Геймплей — инверсия Y")]
    public TextMeshProUGUI invertYValueText;
    private string[] _invertYOptions = { "Выкл", "Вкл" };
    private int _invertYIndex = 0;

    // ── Звук ──────────────────────────────────────────────────────────────
    [Header("Звук")]
    public Slider masterVolumeSlider;
    public TextMeshProUGUI masterVolumeText;
    public Slider musicVolumeSlider;
    public TextMeshProUGUI musicVolumeText;
    public Slider sfxVolumeSlider;
    public TextMeshProUGUI sfxVolumeText;

    // ── Остальное ─────────────────────────────────────────────────────────
    [Header("Остальное — язык")]
    public TextMeshProUGUI languageValueText;
    private string[] _languages = { "Русский", "English" };
    private int _languageIndex = 0;

    // ── Ключи PlayerPrefs ─────────────────────────────────────────────────
    const string K_QUALITY = "Quality";
    const string K_RESOLUTION = "Resolution";
    const string K_FULLSCREEN = "Fullscreen";
    const string K_VSYNC = "VSync";
    const string K_SENS = "Sensitivity";
    const string K_INVERTY = "InvertY";
    const string K_VOL_MASTER = "VolMaster";
    const string K_VOL_MUSIC = "VolMusic";
    const string K_VOL_SFX = "VolSFX";
    const string K_LANGUAGE = "Language";

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        // Собираем доступные разрешения
        _resolutions = Screen.resolutions;

        LoadAll();
        ApplyAll();
        UpdateAllUI();
        ShowTab(0);
    }

    // ── Вкладки ───────────────────────────────────────────────────────────
    public void ShowTab(int index)
    {
        panelGraphics.SetActive(index == 0);
        panelGameplay.SetActive(index == 1);
        panelAudio.SetActive(index == 2);
        panelOther.SetActive(index == 3);
    }

    // ── Стрелочки — универсальный метод ──────────────────────────────────
    /// <summary>
    /// direction: +1 = вправо (следующее), -1 = влево (предыдущее)
    /// </summary>
    void Cycle(ref int index, int length, int direction)
    {
        index = (index + direction + length) % length;
    }

    // ── ГРАФИКА ───────────────────────────────────────────────────────────

    public void OnQualityLeft() { Cycle(ref _qualityIndex, _qualityLevels.Length, -1); ApplyQuality(); UpdateGraphicsUI(); }
    public void OnQualityRight() { Cycle(ref _qualityIndex, _qualityLevels.Length, +1); ApplyQuality(); UpdateGraphicsUI(); }

    public void OnResolutionLeft() { Cycle(ref _resolutionIndex, _resolutions.Length, -1); ApplyResolution(); UpdateGraphicsUI(); }
    public void OnResolutionRight() { Cycle(ref _resolutionIndex, _resolutions.Length, +1); ApplyResolution(); UpdateGraphicsUI(); }

    public void OnFullscreenLeft() { Cycle(ref _fullscreenIndex, _fullscreenModes.Length, -1); ApplyFullscreen(); UpdateGraphicsUI(); }
    public void OnFullscreenRight() { Cycle(ref _fullscreenIndex, _fullscreenModes.Length, +1); ApplyFullscreen(); UpdateGraphicsUI(); }

    public void OnVSyncLeft() { Cycle(ref _vsyncIndex, _vsyncModes.Length, -1); ApplyVSync(); UpdateGraphicsUI(); }
    public void OnVSyncRight() { Cycle(ref _vsyncIndex, _vsyncModes.Length, +1); ApplyVSync(); UpdateGraphicsUI(); }

    void ApplyQuality()
    {
        // Unity quality levels: 0=Very Low ... 5=Ultra
        // Маппим наши 4 уровня на стандартные
        int[] mapping = { 0, 2, 4, 5 };
        QualitySettings.SetQualityLevel(mapping[_qualityIndex], true);
    }

    void ApplyResolution()
    {
        if (_resolutions == null || _resolutions.Length == 0) return;
        Resolution r = _resolutions[_resolutionIndex];
        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode);
    }

    void ApplyFullscreen()
    {
        FullScreenMode[] modes = {
            FullScreenMode.Windowed,
            FullScreenMode.FullScreenWindow,
            FullScreenMode.ExclusiveFullScreen
        };
        Screen.fullScreenMode = modes[_fullscreenIndex];
    }

    void ApplyVSync()
    {
        QualitySettings.vSyncCount = _vsyncIndex; // 0 = off, 1 = on
    }

    void UpdateGraphicsUI()
    {
        if (qualityValueText) qualityValueText.text = _qualityLevels[_qualityIndex];
        if (fullscreenValueText) fullscreenValueText.text = _fullscreenModes[_fullscreenIndex];
        if (vsyncValueText) vsyncValueText.text = _vsyncModes[_vsyncIndex];

        if (resolutionValueText && _resolutions != null && _resolutions.Length > 0)
        {
            Resolution r = _resolutions[_resolutionIndex];
            resolutionValueText.text = $"{r.width} × {r.height}";
        }
    }

    // ── ГЕЙМПЛЕЙ ──────────────────────────────────────────────────────────

    public void OnSensitivityChanged(float value)
    {
        
        if (sensitivityValueText) sensitivityValueText.text = value.ToString("F1");

    }

    public void OnInvertYLeft() { Cycle(ref _invertYIndex, _invertYOptions.Length, -1); UpdateGameplayUI(); }
    public void OnInvertYRight() { Cycle(ref _invertYIndex, _invertYOptions.Length, +1); UpdateGameplayUI(); }

    void UpdateGameplayUI()
    {
        if (invertYValueText) invertYValueText.text = _invertYOptions[_invertYIndex];
        if (sensitivityValueText && sensitivitySlider)
            sensitivityValueText.text = sensitivitySlider.value.ToString("F1");
    }

    // ── ЗВУК ──────────────────────────────────────────────────────────────

    public void OnMasterVolumeChanged(float v)
    {
        // Передаємо чисте значення v (від 0 до 1) напряму у Wwise
        AkUnitySoundEngine.SetRTPCValue("Vol_Master", v);

        AkUnitySoundEngine.PostEvent("Play_Ui_click_toombler", gameObject);
        AkUnitySoundEngine.SetRTPCValue("Ui_Toombler", v);
        
        // Оновлюємо текст на екрані (тут множимо на 100 лише для того, щоб показати "50%", а не "0.5%")
        if (masterVolumeText) masterVolumeText.text = Mathf.RoundToInt(v * 100) + "%";
    }

    public void OnMusicVolumeChanged(float v)
    {
        AkUnitySoundEngine.SetRTPCValue("Vol_Music", v);

        AkUnitySoundEngine.PostEvent("Play_Ui_click_toombler", gameObject);
        AkUnitySoundEngine.SetRTPCValue("Ui_Toombler", v);
        
        if (musicVolumeText) musicVolumeText.text = Mathf.RoundToInt(v * 100) + "%";
    }

    public void OnSFXVolumeChanged(float v)
    {
        AkUnitySoundEngine.SetRTPCValue("Vol_SFX", v);

        AkUnitySoundEngine.PostEvent("Play_Ui_click_toombler", gameObject);
        AkUnitySoundEngine.SetRTPCValue("Ui_Toombler", v);
        
        if (sfxVolumeText) sfxVolumeText.text = Mathf.RoundToInt(v * 100) + "%";
    }

    // ── ОСТАЛЬНОЕ ─────────────────────────────────────────────────────────

    public void OnLanguageLeft()
    {
        int idx = System.Array.IndexOf(LocalizationService.SupportedLanguages,
                                       LocalizationService.CurrentLanguage);
        idx = (idx - 1 + LocalizationService.SupportedLanguages.Length)
              % LocalizationService.SupportedLanguages.Length;
        LocalizationService.SetLanguage(LocalizationService.SupportedLanguages[idx]);
        UpdateOtherUI();
    }

    public void OnLanguageRight()
    {
        int idx = System.Array.IndexOf(LocalizationService.SupportedLanguages,
                                       LocalizationService.CurrentLanguage);
        idx = (idx + 1) % LocalizationService.SupportedLanguages.Length;
        LocalizationService.SetLanguage(LocalizationService.SupportedLanguages[idx]);
        UpdateOtherUI();
    }

    void UpdateOtherUI()
    {
        if (languageValueText) languageValueText.text = _languages[_languageIndex];
    }

    // ── Сохранение / загрузка ─────────────────────────────────────────────

    public void SaveAll()
    {
        PlayerPrefs.SetInt(K_QUALITY, _qualityIndex);
        PlayerPrefs.SetInt(K_RESOLUTION, _resolutionIndex);
        PlayerPrefs.SetInt(K_FULLSCREEN, _fullscreenIndex);
        PlayerPrefs.SetInt(K_VSYNC, _vsyncIndex);
        PlayerPrefs.SetFloat(K_SENS, sensitivitySlider ? sensitivitySlider.value : 2f);
        PlayerPrefs.SetInt(K_INVERTY, _invertYIndex);
        PlayerPrefs.SetFloat(K_VOL_MASTER, masterVolumeSlider ? masterVolumeSlider.value : 1f);
        PlayerPrefs.SetFloat(K_VOL_MUSIC, musicVolumeSlider ? musicVolumeSlider.value : 1f);
        PlayerPrefs.SetFloat(K_VOL_SFX, sfxVolumeSlider ? sfxVolumeSlider.value : 1f);
        PlayerPrefs.SetInt(K_LANGUAGE, _languageIndex);
        PlayerPrefs.Save();
    }

    void LoadAll()
    {
        _qualityIndex = PlayerPrefs.GetInt(K_QUALITY, 1);
        _resolutionIndex = PlayerPrefs.GetInt(K_RESOLUTION, GetDefaultResolutionIndex());
        _fullscreenIndex = PlayerPrefs.GetInt(K_FULLSCREEN, 2);
        _vsyncIndex = PlayerPrefs.GetInt(K_VSYNC, 1);
        _invertYIndex = PlayerPrefs.GetInt(K_INVERTY, 0);
        _languageIndex = PlayerPrefs.GetInt(K_LANGUAGE, 0);

        if (sensitivitySlider) sensitivitySlider.value = PlayerPrefs.GetFloat(K_SENS, 2f);
        if (masterVolumeSlider) masterVolumeSlider.value = PlayerPrefs.GetFloat(K_VOL_MASTER, 1f);
        if (musicVolumeSlider) musicVolumeSlider.value = PlayerPrefs.GetFloat(K_VOL_MUSIC, 1f);
        if (sfxVolumeSlider) sfxVolumeSlider.value = PlayerPrefs.GetFloat(K_VOL_SFX, 1f);
    }

    void ApplyAll()
    {
        ApplyQuality();
        ApplyResolution();
        ApplyFullscreen();
        ApplyVSync();
        if (masterVolumeSlider) AudioListener.volume = masterVolumeSlider.value;
    }

    void UpdateAllUI()
    {
        UpdateGraphicsUI();
        UpdateGameplayUI();
        UpdateOtherUI();
        if (masterVolumeText && masterVolumeSlider)
            masterVolumeText.text = Mathf.RoundToInt(masterVolumeSlider.value * 100) + "%";
        if (musicVolumeText && musicVolumeSlider)
            musicVolumeText.text = Mathf.RoundToInt(musicVolumeSlider.value * 100) + "%";
        if (sfxVolumeText && sfxVolumeSlider)
            sfxVolumeText.text = Mathf.RoundToInt(sfxVolumeSlider.value * 100) + "%";
    }

    int GetDefaultResolutionIndex()
    {
        if (_resolutions == null) return 0;
        for (int i = 0; i < _resolutions.Length; i++)
        {
            if (_resolutions[i].width == Screen.currentResolution.width &&
                _resolutions[i].height == Screen.currentResolution.height)
                return i;
        }
        return _resolutions.Length - 1;
    }

    // Вызывай при закрытии настроек
    public void OnClose() => SaveAll();
}