using System;
using System.Collections.Generic;
using UnityEngine;

public static class LocalizationService
{
    public static event Action OnLanguageChanged;

    public static string CurrentLanguage { get; private set; } = "ru";

    // Все поддерживаемые языки
    public static readonly string[] SupportedLanguages =
        { "ru", "en", "de", "fr", "es", "pt" };

    // Словарь: ключ → (язык → текст)
    private static readonly Dictionary<string, Dictionary<string, string>> _table
        = new Dictionary<string, Dictionary<string, string>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {

        ApplySystemLanguage();
    }

    public static void SetLanguage(string lang)
    {
        if (CurrentLanguage == lang) return;
        CurrentLanguage = lang;
        PlayerPrefs.SetString("Language", lang);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
    }


    public static string Get(string key)
    {
        if (_table.TryGetValue(key, out var langs))
        {
            if (langs.TryGetValue(CurrentLanguage, out var text))
                return text;
            if (langs.TryGetValue("en", out var fallback))
                return fallback;
        }

        Debug.LogWarning($"[Localization] Missing key: {key}");
        return key;
    }

    private static void ApplySystemLanguage()
    {
        // Читаем сохранённый язык
        string saved = PlayerPrefs.GetString("Language", "");
        if (!string.IsNullOrEmpty(saved))
        {
            CurrentLanguage = saved;
            return;
        }

        // Автоопределение по системному языку
        CurrentLanguage = Application.systemLanguage switch
        {
            SystemLanguage.Russian => "ru",
            SystemLanguage.English => "en",
            SystemLanguage.German => "de",
            SystemLanguage.French => "fr",
            SystemLanguage.Spanish => "es",
            SystemLanguage.Portuguese => "pt",
            _ => "en"
        };
    }


    // Хелпер для удобного добавления строк
    private static void Add(string key,
        string ru, string en, string de, string fr, string es, string pt)
    {
        _table[key] = new Dictionary<string, string>
        {
            ["ru"] = ru,
            ["en"] = en,
            ["de"] = de,
            ["fr"] = fr,
            ["es"] = es,
            ["pt"] = pt
        };
    }
}