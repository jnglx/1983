using UnityEngine;
using System.Collections;

/// <summary>
/// Вешается на объект башни танка.
/// Иерархия объектов:
///
/// TankBase (неподвижный корпус)
/// └── TurretPivot          ← этот скрипт здесь (вращается по Y)
///     └── GunPivot          ← назначь в gunPivot (вращается по X)
///         └── BarrelEnd     ← назначь в barrelEnd (точка вылета снаряда)
///
/// Камера прицела:
/// └── SightCamera           ← назначь в sightCamera (дочерний к GunPivot)
///
/// Игрок садится в башню → его FPSController блокируется, включается этот скрипт.
/// </summary>
public class TurretController : MonoBehaviour
{
    // ── Башня ─────────────────────────────────────────────────────────────
    [Header("Вращение башни")]
    [Tooltip("Скорость горизонтального вращения башни (град/сек)")]
    public float turretRotationSpeed = 40f;

    // ── Орудие ────────────────────────────────────────────────────────────
    [Header("Орудие")]
    public Transform gunPivot;
    [Tooltip("Скорость вертикального подъёма орудия (град/сек)")]
    public float gunElevationSpeed = 25f;
    [Tooltip("Максимальный угол подъёма орудия (вверх)")]
    public float gunMaxElevation = 14f;
    [Tooltip("Максимальный угол снижения орудия (вниз)")]
    public float gunMaxDepression = 6f;

    // ── Выстрел ───────────────────────────────────────────────────────────
    [Header("Выстрел")]
    public Transform barrelEnd;
    public GameObject projectilePrefab;     // prefab снаряда — см. TankProjectile ниже
    [Tooltip("Скорость снаряда (м/с). Для Т-80 реально ~1750, для геймплея хватит 300–600")]
    public float muzzleVelocity = 400f;
    [Tooltip("Минимальная пауза между выстрелами (сек)")]
    public float reloadTime = 4f;

    [Header("Эффекты выстрела")]
    public ParticleSystem muzzleFlash;
    public AudioClip fireSound;
    public AudioClip reloadSound;
    [Tooltip("Сила отдачи камеры (импульс по Y)")]
    public float recoilKick = 3f;
    public float recoilRecovery = 8f;

    // ── Прицел ────────────────────────────────────────────────────────────
    [Header("Прицел")]
    public Camera sightCamera;             // камера внутри прицела
    public Camera playerCamera;            // основная FPS камера (отключается при входе)
    public float sightFov = 8f;            // узкое FOV прицела (zoom)
    public GameObject sightOverlayUI;      // UI с перекрестием прицела

    // ── Вход/выход ────────────────────────────────────────────────────────
    [Header("Вход и выход из башни")]
    public Transform exitPoint;            // куда игрока выбросить при выходе
    public FPSController fpsController;    // ссылка на контроллер игрока

    // ── Приватное ─────────────────────────────────────────────────────────
    private bool _isActive = false;
    private bool _canFire = true;
    private float _currentGunX = 0f;    // текущий угол орудия по X
    private float _recoilOffset = 0f;
    private AudioSource _audio;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _audio.spatialBlend = 0f;

        // По умолчанию прицел выключен
        if (sightCamera) sightCamera.gameObject.SetActive(false);
        if (sightOverlayUI) sightOverlayUI.SetActive(false);
    }

    void Update()
    {
        if (!_isActive) return;

        HandleTurretRotation();
        HandleGunElevation();
        HandleFire();
        ApplyRecoilRecovery();

        // Выход из башни — F или Escape
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
            ExitTurret();
    }

    // ── Вращение башни (A / D) ────────────────────────────────────────────
    void HandleTurretRotation()
    {
        float h = Input.GetAxis("Horizontal"); // A = -1, D = +1
        transform.Rotate(Vector3.up, h * turretRotationSpeed * Time.deltaTime, Space.World);
    }

    // ── Подъём орудия (W / S) ─────────────────────────────────────────────
    void HandleGunElevation()
    {
        if (gunPivot == null) return;

        float v = -Input.GetAxis("Vertical"); // W = вверх, S = вниз
        _currentGunX = Mathf.Clamp(_currentGunX + v * gunElevationSpeed * Time.deltaTime,
                                    -gunMaxElevation, gunMaxDepression);

        gunPivot.localRotation = Quaternion.Euler(_currentGunX + _recoilOffset, 0f, 0f);
    }

    // ── Выстрел ───────────────────────────────────────────────────────────
    void HandleFire()
    {
        if (!_canFire) return;
        if (!Input.GetKeyDown(KeyCode.F)) return;

        StartCoroutine(FireSequence());
    }

    IEnumerator FireSequence()
    {
        _canFire = false;

        // Звук выстрела
        if (fireSound) _audio.PlayOneShot(fireSound);

        // Дульное пламя
        if (muzzleFlash) muzzleFlash.Play();

        // Отдача
        _recoilOffset = recoilKick;

        // Снаряд
        SpawnProjectile();

        // Ожидаем перезарядку
        yield return new WaitForSeconds(reloadTime * 0.6f);
        if (reloadSound) _audio.PlayOneShot(reloadSound);
        yield return new WaitForSeconds(reloadTime * 0.4f);

        _canFire = true;
    }

    void SpawnProjectile()
    {
        if (projectilePrefab == null || barrelEnd == null) return;

        GameObject proj = Instantiate(projectilePrefab, barrelEnd.position, barrelEnd.rotation);
        var rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = barrelEnd.forward * muzzleVelocity;
            // Гравитация на такой скорости почти не заметна на дистанции полигона,
            // но для реализма можно оставить. Для аркадности — rb.useGravity = false;
        }
    }

    // ── Восстановление от отдачи ──────────────────────────────────────────
    void ApplyRecoilRecovery()
    {
        _recoilOffset = Mathf.Lerp(_recoilOffset, 0f, Time.deltaTime * recoilRecovery);
        if (gunPivot != null)
            gunPivot.localRotation = Quaternion.Euler(_currentGunX + _recoilOffset, 0f, 0f);
    }

    [Header("Модель игрока")]
    [Tooltip("Корневой объект модели/меша игрока — скрывается при входе в башню")]
    public GameObject playerVisuals;

    // ── Вход в башню ──────────────────────────────────────────────────────
    /// <summary>
    /// Вызывается из интерактивного объекта ("Сесть в башню" — E).
    /// </summary>
    public void EnterTurret()
    {
        if (_isActive) return;
        _isActive = true;

        // Скрываем модель игрока
        if (playerVisuals) playerVisuals.SetActive(false);

        // Блокируем FPS-контроллер
        if (fpsController != null)
        {
            fpsController.SetMovementLocked(true);
            fpsController.SetLookLocked(true);
            if (playerCamera) playerCamera.gameObject.SetActive(false);
        }

        // Включаем прицел
        if (sightCamera) sightCamera.gameObject.SetActive(true);
        if (sightOverlayUI) sightOverlayUI.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ── Выход из башни ────────────────────────────────────────────────────
    public void ExitTurret()
    {
        if (!_isActive) return;
        _isActive = false;

        // Показываем модель игрока
        if (playerVisuals) playerVisuals.SetActive(true);

        // Восстанавливаем игрока
        if (fpsController != null)
        {
            if (exitPoint != null)
                fpsController.transform.position = exitPoint.position;

            fpsController.SetMovementLocked(false);
            fpsController.SetLookLocked(false);
            if (playerCamera) playerCamera.gameObject.SetActive(true);
        }

        // Выключаем прицел
        if (sightCamera) sightCamera.gameObject.SetActive(false);
        if (sightOverlayUI) sightOverlayUI.SetActive(false);
    }
}