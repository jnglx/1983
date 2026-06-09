using UnityEngine;

public static class GameSettings
{
    const string KEY_VOLUME = "MasterVolume";
    const string KEY_SENS = "MouseSensitivity";

    public static float MasterVolume => PlayerPrefs.GetFloat(KEY_VOLUME, 1f);
    public static float MouseSensitivity => PlayerPrefs.GetFloat(KEY_SENS, 2f);

    /// <summary>
    /// Вызови это в Start() FPSController чтобы подхватить настройки из меню
    /// </summary>
    public static void ApplyToController(FPSController controller)
    {
        if (controller == null) return;
        controller.mouseSensitivity = MouseSensitivity;
        AudioListener.volume = MasterVolume;
    }
}