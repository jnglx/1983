using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [System.Serializable]
    public class Translation
    {
        public string ru;
        public string en;
        public string de;
        public string fr;
        public string es;
        public string pt;

        public string Get(string lang) => lang switch
        {
            "ru" => ru,
            "en" => en,
            "de" => de,
            "fr" => fr,
            "es" => es,
            "pt" => pt,
            _ => string.IsNullOrEmpty(en) ? ru : en
        };
    }

    [SerializeField] private Translation translation;

    private TMP_Text _text;

    void Awake() => _text = GetComponent<TMP_Text>();

    void OnEnable()
    {
        LocalizationService.OnLanguageChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        LocalizationService.OnLanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        if (_text == null || translation == null) return;
        string result = translation.Get(LocalizationService.CurrentLanguage);
        if (!string.IsNullOrEmpty(result))
            _text.text = result;
    }
}