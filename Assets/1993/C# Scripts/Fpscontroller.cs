using UnityEngine;
using TMPro;

/// <summary>
/// FPS-контроллер для Unity 6 HDRP.
/// Основан на SimpleHorrorFPSController, очищен от хоррор-зависимостей.
/// Требует: CharacterController, AudioSource на том же объекте.
/// Камера должна быть дочерним объектом — назначь cameraTransform в инспекторе.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class FPSController : MonoBehaviour
{
    // ── Движение ──────────────────────────────────────────────────────────
    [Header("Movement")]
    public float walkSpeed = 2.8f;
    public float sprintSpeed = 4.5f;
    public float gravity = 20f;

    // ── Мышь ──────────────────────────────────────────────────────────────
    [Header("Mouse Look")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public bool invertY = false;
    public float verticalLookLimit = 80f;

    // ── Bob камеры ────────────────────────────────────────────────────────
    [Header("Camera Bob")]
    public bool enableCameraBob = true;
    [Min(0f)] public float bobWalkAmount = 0.03f;
    [Min(0f)] public float bobSprintAmount = 0.05f;
    [Min(0.1f)] public float bobWalkSpeed = 7f;
    [Min(0.1f)] public float bobSprintSpeed = 11f;
    [Min(0.1f)] public float bobSmoothing = 12f;

    // ── Tilt камеры ───────────────────────────────────────────────────────
    [Header("Camera Tilt")]
    [Tooltip("Пустой объект между игроком и камерой — создай его в иерархии")]
    public Transform cameraNeck;
    public float tiltAmount = 3f;
    public float tiltSpeed = 6f;
    public float forwardTiltAmount = 2f;

    // ── Sprint FOV ────────────────────────────────────────────────────────
    [Header("Sprint FOV")]
    public float sprintFovBoost = 5f;
    public float fovSmoothing = 8f;

    // ── Шаги ──────────────────────────────────────────────────────────────
    [Header("Footsteps")]
    public AudioClip walkLoop;
    public AudioClip sprintLoop;   // опционально — если нет, ускоряем pitch

    // ── Взаимодействие ────────────────────────────────────────────────────
    [Header("Interaction")]
    public float interactionRange = 2.5f;
    public LayerMask interactLayer;
    [Tooltip("Canvas с TextMeshPro для подсказки '[E] ...'")]
    public GameObject promptUI;
    public TextMeshProUGUI promptText;

    // ── Приватное ─────────────────────────────────────────────────────────
    private CharacterController _cc;
    private AudioSource _audio;
    private Camera _cam;
    private float _baseFov;
    private float _xRotation;
    private float _verticalVelocity;
    private float _bobTimer;
    private Vector3 _camBaseLocal;
    private bool _isSprinting;
    private float _moveAmount;

    private bool _movementLocked;
    private bool _lookLocked;

    private IInteractable _currentInteractable;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        _cc = GetComponent<CharacterController>();
        _audio = GetComponent<AudioSource>();
        _audio.loop = true;
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f; // шаги — не пространственные

        if (cameraTransform != null)
        {
            _camBaseLocal = cameraTransform.localPosition;
            _cam = cameraTransform.GetComponentInChildren<Camera>();
            if (_cam != null) _baseFov = _cam.fieldOfView;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (promptUI) promptUI.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!_lookLocked) HandleMouseLook();
        if (!_movementLocked) HandleMovement();
        else StopMovement();

        ApplyCameraBob();
        ApplyCameraTilt();
        ApplySprintFov();
        HandleInteraction();
    }

    // ── Мышь ──────────────────────────────────────────────────────────────
    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 100f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 100f * Time.deltaTime;
        if (invertY) mouseY = -mouseY;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -verticalLookLimit, verticalLookLimit);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    // ── Движение ──────────────────────────────────────────────────────────
    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        _isSprinting = Input.GetKey(KeyCode.LeftShift) && v > 0f;
        float speed = _isSprinting ? sprintSpeed : walkSpeed;

        Vector3 move = (transform.right * h + transform.forward * v);
        if (move.sqrMagnitude > 1f) move.Normalize();
        _moveAmount = move.magnitude;

        if (_cc.isGrounded)
        {
            if (_verticalVelocity < 0f) _verticalVelocity = -2f;
        }
        else
        {
            _verticalVelocity -= gravity * Time.deltaTime;
        }

        Vector3 velocity = move * speed;
        velocity.y = _verticalVelocity;
        _cc.Move(velocity * Time.deltaTime);

        HandleFootsteps(_moveAmount);
    }

    void StopMovement()
    {
        _moveAmount = 0f;
        _isSprinting = false;
        _verticalVelocity = 0f;
        if (_audio.isPlaying) _audio.Stop();
    }

    // ── Шаги ──────────────────────────────────────────────────────────────
    void HandleFootsteps(float moveAmount)
    {
        bool isMoving = moveAmount > 0.1f && _cc.isGrounded;

        if (isMoving)
        {
            AudioClip clip = (_isSprinting && sprintLoop != null) ? sprintLoop : walkLoop;
            if (_audio.clip != clip)
            {
                _audio.clip = clip;
                _audio.Play();
            }
            else if (!_audio.isPlaying) _audio.Play();

            _audio.pitch = (_isSprinting && sprintLoop == null) ? 1.55f : 1f;
        }
        else
        {
            if (_audio.isPlaying) _audio.Stop();
        }
    }

    // ── Camera Bob ────────────────────────────────────────────────────────
    void ApplyCameraBob()
    {
        if (cameraTransform == null) return;

        bool isMoving = _moveAmount > 0.1f && _cc != null && _cc.isGrounded && !_movementLocked;

        if (!enableCameraBob || !isMoving)
        {
            // Плавный возврат позиции и вращения в ноль
            _bobTimer = 0f;
            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition,
                _camBaseLocal,
                Time.deltaTime * bobSmoothing);
            cameraTransform.localRotation = Quaternion.Lerp(
                cameraTransform.localRotation,
                Quaternion.Euler(_xRotation, 0f, 0f),
                Time.deltaTime * bobSmoothing);
            return;
        }

        float amount = _isSprinting ? bobSprintAmount : bobWalkAmount;
        float speed = _isSprinting ? bobSprintSpeed : bobWalkSpeed;
        _bobTimer += Time.deltaTime * speed;

        // ── Позиция ───────────────────────────────────────────────────────
        // Y: два пика за цикл (два шага) — Abs(Sin) даёт именно это
        float bobY = Mathf.Abs(Mathf.Sin(_bobTimer)) * amount;
        // X: один цикл влево-вправо — половина амплитуды от Y
        float bobX = Mathf.Sin(_bobTimer) * amount * 0.4f;

        Vector3 targetPos = _camBaseLocal + new Vector3(bobX, bobY, 0f);
        cameraTransform.localPosition = Vector3.Lerp(
            cameraTransform.localPosition,
            targetPos,
            Time.deltaTime * bobSmoothing);

        // ── Вращение ──────────────────────────────────────────────────────
        // Z (roll): голова наклоняется в сторону шага — в такт с X
        float rollZ = -Mathf.Sin(_bobTimer) * amount * 1.8f;

        // X (pitch): лёгкий кивок вперёд при шаге вниз
        float pitchX = _xRotation + Mathf.Sin(_bobTimer * 2f) * amount * 0.8f;

        Quaternion targetRot = Quaternion.Euler(pitchX, 0f, rollZ);
        cameraTransform.localRotation = Quaternion.Lerp(
            cameraTransform.localRotation,
            targetRot,
            Time.deltaTime * bobSmoothing);
    }

    // ── Camera Tilt ───────────────────────────────────────────────────────
    void ApplyCameraTilt()
    {
        if (cameraNeck == null) return;

        float targetRoll = 0f;
        float targetPitch = 0f;

        if (!_movementLocked && !_lookLocked)
        {
            float h = Input.GetAxis("Horizontal");
            targetRoll = -h * tiltAmount;
            if (_isSprinting) targetPitch = forwardTiltAmount;
        }

        cameraNeck.localRotation = Quaternion.Lerp(
            cameraNeck.localRotation,
            Quaternion.Euler(targetPitch, 0f, targetRoll),
            Time.deltaTime * tiltSpeed);
    }

    // ── Sprint FOV ────────────────────────────────────────────────────────
    void ApplySprintFov()
    {
        if (_cam == null) return;
        float target = _isSprinting ? _baseFov + sprintFovBoost : _baseFov;
        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, target, Time.deltaTime * fovSmoothing);
    }

    // ── Взаимодействие ────────────────────────────────────────────────────
    void HandleInteraction()
    {
        DetectInteractable();

        if (_currentInteractable != null && Input.GetKeyDown(KeyCode.E))
            _currentInteractable.Interact();
    }

    void DetectInteractable()
    {
        if (cameraTransform == null) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactLayer))
        {
            var interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null && interactable.IsAvailable)
            {
                if (_currentInteractable != interactable)
                {
                    _currentInteractable = interactable;
                    ShowPrompt(interactable.PromptText);
                }
                return;
            }
        }

        if (_currentInteractable != null)
        {
            _currentInteractable = null;
            HidePrompt();
        }
    }

    void ShowPrompt(string text)
    {
        if (promptText) promptText.text = $"[E]  {text}";
        if (promptUI) promptUI.SetActive(true);
    }

    void HidePrompt()
    {
        if (promptUI) promptUI.SetActive(false);
    }

    // ── Публичное API ─────────────────────────────────────────────────────

    /// <summary>Заблокировать движение (для катсцен, потери сознания)</summary>
    public void SetMovementLocked(bool locked) => _movementLocked = locked;

    /// <summary>Заблокировать поворот камеры</summary>
    public void SetLookLocked(bool locked) => _lookLocked = locked;

    /// <summary>Синхронизировать pitch после внешнего поворота камеры</summary>
    public void SyncCameraPitch()
    {
        if (cameraTransform == null) return;
        _xRotation = Mathf.DeltaAngle(0f, cameraTransform.localEulerAngles.x);
        _xRotation = Mathf.Clamp(_xRotation, -verticalLookLimit, verticalLookLimit);
    }
}