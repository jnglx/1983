using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Одна точка модификации на башне.
/// Вешается на коллайдер точки взаимодействия (невидимый триггер рядом с деталью).
/// </summary>
public class TankModPoint : MonoBehaviour, IInteractable
{
    [Header("Описание")]
    [SerializeField] private string promptText = "Установить ДЗ «Контакт-1»";
    [SerializeField] private string lockedText = "Сначала завершите предыдущий шаг";

    [Header("Визуал")]
    [SerializeField] private GameObject beforeObject;  // деталь ДО модификации (старая позиция гранатомётов и т.д.)
    [SerializeField] private GameObject afterObject;   // деталь ПОСЛЕ
    [SerializeField] private ParticleSystem installFX; // опционально — искры/пыль

    [Header("Звук")]
    [SerializeField] private AudioClip installSound;

    private bool _done = false;
    private bool _locked = true;
    private AudioSource _audio;

    public string PromptText => _locked ? lockedText : promptText;
    public bool IsAvailable => !_done; // скрываем подсказку после выполнения

    void Awake()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();

        // Начальное состояние: показываем "до"
        if (beforeObject) beforeObject.SetActive(true);
        if (afterObject) afterObject.SetActive(false);
    }

    public void SetLocked(bool locked) => _locked = locked;

    public void Interact()
    {
        if (_done || _locked) return;
        StartCoroutine(DoInstall());
    }

    IEnumerator DoInstall()
    {
        _done = true;

        // Звук
        if (installSound) _audio.PlayOneShot(installSound);

        // Эффект
        if (installFX) installFX.Play();

        yield return new WaitForSeconds(0.4f);

        // Меняем объекты
        if (beforeObject) beforeObject.SetActive(false);
        if (afterObject) afterObject.SetActive(true);

        yield return new WaitForSeconds(0.3f);

        // Сообщаем менеджеру
        TankModificationManager.Instance.OnModCompleted(this);
    }
}