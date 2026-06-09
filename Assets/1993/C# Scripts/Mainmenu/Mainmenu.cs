using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// ─────────────────────────────────────────────────────────────────────────────
// MainMenu.cs — вешается на объект в сцене главного меню
// ─────────────────────────────────────────────────────────────────────────────
public class MainMenu : MonoBehaviour
{
    [Header("Панели")]
    public GameObject mainPanel;        // кнопки Играть / Настройки / Выход
    public GameObject settingsPanel;    // панель настроек

    [Header("Настройки — звук")]
    public Slider masterVolumeSlider;
    public TextMeshProUGUI masterVolumeLabel;

    [Header("Настройки — мышь")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityLabel;

    [Header("Загрузка")]
    public string gameSceneName = "GameScene"; // имя сцены с игрой
    public GameObject loadingScreen;           // экран загрузки с авторами
    public Slider loadingBar;                  // опционально

    // Ключи для PlayerPrefs
    const string KEY_VOLUME = "MasterVolume";
    const string KEY_SENS = "MouseSensitivity";

    void Start()
    {
        // Загружаем сохранённые настройки
        masterVolumeSlider.value = PlayerPrefs.GetFloat(KEY_VOLUME, 1f);
        sensitivitySlider.value = PlayerPrefs.GetFloat(KEY_SENS, 2f);

        ApplyVolume(masterVolumeSlider.value);
        UpdateLabels();

        // Слушаем слайдеры
        masterVolumeSlider.onValueChanged.AddListener(v => { ApplyVolume(v); UpdateLabels(); });
        sensitivitySlider.onValueChanged.AddListener(v => { UpdateLabels(); });

        ShowMain();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ── Кнопки главного меню ─────────────────────────────────────────────────

    public void OnPlayPressed()
    {
        SaveSettings();
        StartCoroutine(LoadGameAsync());
    }

    public void OnSettingsPressed()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void OnQuitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Кнопки панели настроек ───────────────────────────────────────────────

    public void OnSettingsBack()
    {
        SaveSettings();
        ShowMain();
    }

    // ── Приватные методы ─────────────────────────────────────────────────────

    void ShowMain()
    {
        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    void ApplyVolume(float value)
    {
        // AudioListener.volume работает глобально — просто и надёжно
        AudioListener.volume = value;
    }

    void UpdateLabels()
    {
        if (masterVolumeLabel)
            masterVolumeLabel.text = Mathf.RoundToInt(masterVolumeSlider.value * 100f) + "%";
        if (sensitivityLabel)
            sensitivityLabel.text = sensitivitySlider.value.ToString("F1");
    }

    void SaveSettings()
    {
        PlayerPrefs.SetFloat(KEY_VOLUME, masterVolumeSlider.value);
        PlayerPrefs.SetFloat(KEY_SENS, sensitivitySlider.value);
        PlayerPrefs.Save();
    }

    IEnumerator LoadGameAsync()
    {
        // Показываем экран загрузки с авторами
        if (loadingScreen) loadingScreen.SetActive(true);
        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);

        // Минимальное время показа авторов — 2 секунды
        float minShowTime = 2f;
        float elapsed = 0f;

        AsyncOperation op = SceneManager.LoadSceneAsync(gameSceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(op.progress / 0.9f);

            if (loadingBar) loadingBar.value = progress;

            // Ждём пока сцена готова И прошло минимальное время
            if (op.progress >= 0.9f && elapsed >= minShowTime)
                op.allowSceneActivation = true;

            yield return null;
        }
    }
}

