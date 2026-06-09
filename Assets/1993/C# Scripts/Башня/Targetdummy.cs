using UnityEngine;
using System.Collections;

/// <summary>
/// Мишень на полигоне. Вешается на любой объект.
/// При получении урона — падает, меняет цвет или разрушается.
/// </summary>
public class TargetDummy : MonoBehaviour, IDamageable
{
    [Header("Параметры")]
    public float maxHealth = 100f;

    [Header("Реакция на попадание")]
    [Tooltip("Объект падает на бок при уничтожении")]
    public bool fallOnDeath = true;
    public float fallDuration = 0.8f;

    [Tooltip("Вспышка цвета при попадании")]
    public Color hitColor = Color.red;
    public float hitColorDuration = 0.15f;

    [Header("Возрождение")]
    public bool respawn = true;
    public float respawnDelay = 5f;

    private float _health;
    private bool _dead = false;
    private Renderer[] _renderers;
    private Color[] _originalColors;
    private Quaternion _originalRotation;
    private Vector3 _originalPosition;

    void Start()
    {
        _health = maxHealth;
        _originalRotation = transform.rotation;
        _originalPosition = transform.position;

        _renderers = GetComponentsInChildren<Renderer>();
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
            _originalColors[i] = _renderers[i].material.color;
    }

    public void TakeDamage(float amount)
    {
        if (_dead) return;

        _health -= amount;
        StartCoroutine(FlashHitColor());

        if (_health <= 0f)
            StartCoroutine(Die());
    }

    IEnumerator FlashHitColor()
    {
        foreach (var r in _renderers)
            r.material.color = hitColor;

        yield return new WaitForSeconds(hitColorDuration);

        for (int i = 0; i < _renderers.Length; i++)
            _renderers[i].material.color = _originalColors[i];
    }

    IEnumerator Die()
    {
        _dead = true;

        if (fallOnDeath)
        {
            // Плавно заваливаем мишень на 90 градусов
            float elapsed = 0f;
            Quaternion from = transform.rotation;
            Quaternion to = from * Quaternion.Euler(0f, 0f, 90f);

            while (elapsed < fallDuration)
            {
                transform.rotation = Quaternion.Lerp(from, to, elapsed / fallDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.rotation = to;
        }

        if (respawn)
        {
            yield return new WaitForSeconds(respawnDelay);
            ResetTarget();
        }
    }

    void ResetTarget()
    {
        _health = maxHealth;
        _dead = false;
        transform.rotation = _originalRotation;
        transform.position = _originalPosition;

        for (int i = 0; i < _renderers.Length; i++)
            _renderers[i].material.color = _originalColors[i];
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one);
    }
}