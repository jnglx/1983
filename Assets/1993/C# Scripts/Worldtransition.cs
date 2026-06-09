using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;

/// <summary>
/// Вешается на любой объект-триггер (или вызывается напрямую).
/// Проигрывает эффект потери сознания, потом переключает HDRP Volume и окружение.
/// </summary>
public class WorldTransition : MonoBehaviour
{
    [Header("HDRP Volumes")]
    [SerializeField] private Volume factoryVolume;   // "холодный завод"
    [SerializeField] private Volume dreamVolume;     // "детский мир"

    [Header("Объекты миров")]
    [SerializeField] private GameObject factoryWorld; // родительский объект всей геометрии завода
    [SerializeField] private GameObject dreamWorld;   // родительский объект детского мира

    [Header("Эффект потери сознания")]
    [SerializeField] private Volume fxVolume;         // отдельный Volume только для FX (Vignette, ChromaticAberration)
    [SerializeField] private float blackoutDuration = 2.5f;
    [SerializeField] private AudioClip heartbeatSound;
    [SerializeField] private AudioClip ambientFactory; // эмбиент завода
    [SerializeField] private AudioClip ambientDream;   // эмбиент детского мира

    [Header("Звук")]
    [SerializeField] private AudioSource musicSource;

    private Vignette _vignette;
    private ChromaticAberration _chroma;
    private bool _transitioning = false;

    void Start()
    {
        // Достаём компоненты из FX Volume
        fxVolume.profile.TryGet(out _vignette);
        fxVolume.profile.TryGet(out _chroma);

        // Стартовое состояние — завод активен
        SetWorld(WorldState.Factory);
    }

    /// <summary>
    /// Вызови это при триггере потери сознания (например, из аниматора или другого скрипта).
    /// </summary>
    public void TriggerBlackout()
    {
        if (!_transitioning)
            StartCoroutine(BlackoutSequence());
    }

    IEnumerator BlackoutSequence()
    {
        _transitioning = true;

        // --- Фаза 1: нарастающее ухудшение ---
        float elapsed = 0f;
        float phaseDur = blackoutDuration * 0.4f;

        if (heartbeatSound && musicSource) musicSource.PlayOneShot(heartbeatSound);

        while (elapsed < phaseDur)
        {
            float t = elapsed / phaseDur;
            SetFXIntensity(t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- Фаза 2: полный чёрный экран ---
        SetFXIntensity(1f);
        yield return new WaitForSeconds(blackoutDuration * 0.3f);

        // --- Переключаем мир пока экран чёрный ---
        SetWorld(WorldState.Dream);

        // --- Фаза 3: плавное появление нового мира ---
        elapsed = 0f;
        float fadeInDur = blackoutDuration * 0.3f;

        // Начинаем проигрывать эмбиент детского мира
        if (musicSource && ambientDream)
        {
            musicSource.clip = ambientDream;
            musicSource.Play();
        }

        while (elapsed < fadeInDur)
        {
            float t = 1f - (elapsed / fadeInDur);
            SetFXIntensity(t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetFXIntensity(0f);
        _transitioning = false;
    }

    void SetFXIntensity(float t)
    {
        // Vignette: от 0.35 до 1.0
        if (_vignette != null)
            _vignette.intensity.value = Mathf.Lerp(0.35f, 1f, t);

        // Chromatic Aberration: от 0 до 1
        if (_chroma != null)
            _chroma.intensity.value = Mathf.Lerp(0f, 1f, t);

        // Плавное затемнение самого Volume через weight
        if (fxVolume != null)
            fxVolume.weight = Mathf.Lerp(0f, 1f, t);
    }

    void SetWorld(WorldState state)
    {
        bool isFactory = state == WorldState.Factory;

        // Геометрия
        if (factoryWorld) factoryWorld.SetActive(isFactory);
        if (dreamWorld) dreamWorld.SetActive(!isFactory);

        // Lighting volumes (weight, не enabled — чтобы не ломать стек)
        if (factoryVolume) factoryVolume.weight = isFactory ? 1f : 0f;
        if (dreamVolume) dreamVolume.weight = isFactory ? 0f : 1f;
    }

    // Можно также повесить на OnTriggerEnter, если у тебя есть зона-триггер
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            TriggerBlackout();
    }

    enum WorldState { Factory, Dream }
}