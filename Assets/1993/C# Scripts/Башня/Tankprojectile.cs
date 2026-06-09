using UnityEngine;

/// <summary>
/// Вешается на prefab снаряда.
/// Prefab нужен: Rigidbody + Collider (сфера или капсула, маленькая).
/// Скорость задаётся снаружи через Rigidbody.velocity в TurretController.
/// </summary>
public class TankProjectile : MonoBehaviour
{
    [Header("Параметры")]
    [Tooltip("Максимальная дистанция до самоуничтожения (метры)")]
    public float maxRange = 1500f;

    [Tooltip("Время жизни снаряда если не попал (сек) — запасной вариант")]
    public float lifetime = 8f;

    [Header("Эффекты попадания")]
    public GameObject impactFX;        // ParticleSystem взрыва/пыли
    public AudioClip impactSound;

    [Header("Урон")]
    public float blastRadius = 3f;     // радиус взрыва (0 = точечный урон)
    public float damage = 100f;

    private Vector3 _startPos;
    private AudioSource _audio;

    void Start()
    {
        _startPos = transform.position;
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.spatialBlend = 1f;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Самоуничтожение по дальности
        if (Vector3.Distance(_startPos, transform.position) >= maxRange)
            DestroyProjectile(transform.position, Vector3.up);

        // Поворачиваем снаряд по вектору скорости (выглядит реалистично)
        var rb = GetComponent<Rigidbody>();
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.1f)
            transform.forward = rb.linearVelocity.normalized;
    }

    void OnCollisionEnter(Collision col)
    {
        // Точка попадания и нормаль поверхности
        ContactPoint contact = col.GetContact(0);
        DestroyProjectile(contact.point, contact.normal);
    }

    void DestroyProjectile(Vector3 point, Vector3 normal)
    {
        // Эффект попадания
        if (impactFX)
        {
            var fx = Instantiate(impactFX, point, Quaternion.LookRotation(normal));
            Destroy(fx, 4f);
        }

        // Звук попадания
        if (impactSound)
            AudioSource.PlayClipAtPoint(impactSound, point, 1f);

        // Урон по радиусу — если нужны разрушаемые мишени
        if (blastRadius > 0f)
        {
            Collider[] hits = Physics.OverlapSphere(point, blastRadius);
            foreach (var hit in hits)
            {
                // Ищем интерфейс IDamageable — вешай на мишени полигона
                var damageable = hit.GetComponent<IDamageable>();
                damageable?.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        // Показывает радиус взрыва в редакторе
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, blastRadius);
    }
}

/// <summary>
/// Интерфейс для мишеней на полигоне — вешай на любой разрушаемый объект.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount);
}