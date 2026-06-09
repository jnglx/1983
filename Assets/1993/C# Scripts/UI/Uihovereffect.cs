using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Вешается на любой интерактивный UI элемент.
/// При наведении — звук + плавная подсветка через дочерний Image (highlightImage).
/// При клике — отдельный звук.
///
/// Иерархия объекта:
///   Button (этот скрипт здесь)
///   └── HighlightImage  ← отдельный Image с твоей формой подсветки
///                          по умолчанию alpha = 0, скрипт плавно показывает его
/// </summary>
public class UIHoverEffect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Подсветка")]
    [Tooltip("Дочерний Image с формой подсветки — рисуй любую форму, хоть закруглённую")]
    public Image highlightImage;

    [Tooltip("Цвет подсветки при наведении")]
    public Color highlightColor = new Color(1f, 1f, 1f, 0.12f);

    [Tooltip("Цвет при клике (чуть ярче)")]
    public Color pressedColor = new Color(1f, 1f, 1f, 0.25f);

    [Tooltip("Скорость появления / исчезновения подсветки")]
    public float fadeSpeed = 8f;

    [Header("Звук")]
    [Tooltip("Звук при наведении курсора")]
    public AudioClip hoverSound;

    [Tooltip("Звук при клике")]
    public AudioClip clickSound;

    [Tooltip("Громкость звуков (0–1)")]
    [Range(0f, 1f)]
    public float volume = 0.5f;

    [Header("Масштаб (опционально)")]
    [Tooltip("Чуть увеличивать элемент при наведении")]
    public bool scaleOnHover = false;
    public float hoverScale = 1.03f;
    public float scaleSpeed = 10f;

    // ── Приватное ─────────────────────────────────────────────────────────
    private Color _targetColor;
    private Vector3 _baseScale;
    private float _targetScale;
    private bool _isHovered;
    private static AudioSource _sharedAudio; // один AudioSource на всю сцену

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _baseScale = transform.localScale;
        _targetScale = 1f;

        // Инициализируем highlightImage в прозрачное состояние
        if (highlightImage != null)
        {
            _targetColor = Color.clear;
            highlightImage.color = Color.clear;
            highlightImage.raycastTarget = false; // не перехватывает клики
        }

        // Один общий AudioSource для всех кнопок
        if (_sharedAudio == null)
        {
            var go = new GameObject("UI_AudioSource");
            DontDestroyOnLoad(go);
            _sharedAudio = go.AddComponent<AudioSource>();
            _sharedAudio.spatialBlend = 0f;
            _sharedAudio.playOnAwake = false;
        }
    }

    void Update()
    {
        // Плавная анимация подсветки
        if (highlightImage != null)
            highlightImage.color = Color.Lerp(
                highlightImage.color, _targetColor, Time.unscaledDeltaTime * fadeSpeed);

        // Плавный масштаб
        if (scaleOnHover)
        {
            float s = Mathf.Lerp(
                transform.localScale.x / _baseScale.x,
                _targetScale,
                Time.unscaledDeltaTime * scaleSpeed);
            transform.localScale = _baseScale * s;
        }
    }

    // ── События наведения ─────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData _)
    {
        _isHovered = true;
        _targetColor = highlightColor;
        _targetScale = hoverScale;

        if (hoverSound != null)
            _sharedAudio.PlayOneShot(hoverSound, volume);
    }

    public void OnPointerExit(PointerEventData _)
    {
        _isHovered = false;
        _targetColor = Color.clear;
        _targetScale = 1f;
    }

    public void OnPointerDown(PointerEventData _)
    {
        _targetColor = pressedColor;

        if (clickSound != null)
            _sharedAudio.PlayOneShot(clickSound, volume);
    }

    public void OnPointerUp(PointerEventData _)
    {
        // Возвращаем hover-цвет если курсор ещё на кнопке
        _targetColor = _isHovered ? highlightColor : Color.clear;
    }
}