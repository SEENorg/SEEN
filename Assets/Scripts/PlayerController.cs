using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Jugador de SEEN en PRIMERA PERSONA con sistema de MOMENTUM:
///   • Agacharse (Ctrl o C) =Slide: conserva el momentum casi sin fricción.
///     Es la forma rápida de moverse.
///   • De pie también conserva momentum, pero con más fricción.
///   • El DASH es un impulso que suma velocidad: después hay que dejarse
///     frenar por la fricción (agachado = más distancia).
/// Controles: WASD mover | Mouse mirar | Espacio saltar (doble) | Shift dash
///            Ctrl/C agacharse | TAB tabla César | Esc pausa
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField, Min(1)] private int maxJumps = 2;

    [Header("Momentum / fricción")]
    [Tooltip("Aceleración al caminar (unidades/s²)")]
    [SerializeField] private float aceleracion = 55f;
    [Tooltip("Fricción en el suelo estando de pie (GRIP: frena al instante)")]
    [SerializeField] private float friccionSuelo = 20f;
    [Tooltip("Fricción en el suelo AGACHADO (slide: se desliza conservando momentum)")]
    [SerializeField] private float friccionAgachado = 2.5f;
    [Tooltip("Fricción en el aire")]
    [SerializeField] private float friccionAire = 1.2f;
    [Tooltip("Multiplicador de velocidad máxima al estar agachado")]
    [SerializeField] private float bonusAgachado = 1.08f;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField, Range(0.5f, 1f)] private float doubleJumpMultiplier = 0.9f;

    [Header("Agacharse")]
    [Tooltip("Cuánto baja la cámara al agacharse")]
    [SerializeField] private float alturaAgachado = 0.55f;
    [SerializeField] private float velocidadAgacharse = 12f;

    [Header("Dash (impulso)")]
    [SerializeField] private float dashForce = 22f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private bool dashCancelsGravity = true;

    [Header("Cámara")]
    [Tooltip("Cámara hija del jugador (se crea sola si no la asignás)")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float cameraHeight = 0.65f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Detección de suelo")]
    [Tooltip("Capas de suelo. Si lo dejás en Nothing, detecta contra todo.")]
    [SerializeField] private LayerMask groundMask;
    [Tooltip("Margen extra bajo los pies para considerar que está en suelo")]
    [SerializeField] private float groundSkin = 0.12f;

    [Header("Game Feel")]
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float jumpGroundGuard = 0.12f;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private static PhysicsMaterial _playerPhysMat;

    private bool isGrounded;
    private bool wasGrounded;
    private MovingPlatform groundPlatform;
    private int jumpsLeft;
    private float jumpBufferTimer;
    private float groundGuardTimer;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;

    // Agacharse
    private bool agachado;
    private float alturaActual;

    private float yaw;
    private float pitch;
    private Vector2 moveInput;

    // ───────────────────────────────────────────────────────────── Setup

    private void Awake()
    {
        // La sensibilidad se puede cambiar desde el menú de Ajustes
        mouseSensitivity = PlayerPrefs.GetFloat("sensibilidad", mouseSensitivity);

        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        // ── Anti-temblor de cámara ─────────────────────────────
        Physics.defaultSolverIterations = 12;
        Physics.defaultSolverVelocityIterations = 6;

        // CONGELAR TODA la rotación (los 3 ejes): el yaw lo asigna este script
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.angularDamping = 1f;

        // Fricción baja en la cápsula: al rozar un borde, desliza en vez de trabarse
        if (_playerPhysMat == null)
        {
            _playerPhysMat = new PhysicsMaterial("PlayerPhysMat");
            _playerPhysMat.dynamicFriction = 0.25f;
            _playerPhysMat.staticFriction = 0.25f;
            _playerPhysMat.frictionCombine = PhysicsMaterialCombine.Minimum;
            _playerPhysMat.bounciness = 0f;
            _playerPhysMat.bounceCombine = PhysicsMaterialCombine.Minimum;
        }
        capsule.material = _playerPhysMat;
        // ────────────────────────────────────────────────────────

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        alturaActual = cameraHeight;
        yaw = transform.eulerAngles.y;

        EnsureCamera();

        // Primera persona: no ver la cápsula desde adentro
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void EnsureCamera()
    {
        if (playerCamera == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam != null) playerCamera = cam.transform;
        }

        if (playerCamera == null) return;

        if (playerCamera.parent != transform)
            playerCamera.SetParent(transform, false);
        playerCamera.localPosition = new Vector3(0f, cameraHeight, 0f);
        playerCamera.localRotation = Quaternion.identity;
    }

    // ───────────────────────────────────────────────────────────── Loop

    private void Update()
    {
        ReadMoveInput();
        HandleLook();
        HandleCursor();
        HandleCrouch();

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        // Buffer de salto
        if (ReadJumpPressed())
            jumpBufferTimer = jumpBufferTime;
        else if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;

        if (ReadDashPressed() && !isDashing && dashCooldownTimer <= 0f)
            StartDash();
    }

    private void FixedUpdate()
    {
        CheckGround();
        UpdateJumpsLeft();
        HandleMovement();
        HandleJump();
        UpdateDash();
        ApplyGravity();

        // El yaw lo asigna directamente este script
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (rb.angularVelocity.sqrMagnitude > 0f)
            rb.angularVelocity = Vector3.zero;

        wasGrounded = isGrounded;
    }

    // ─────────────────────────────────────────────────── Movimiento

    private void HandleMovement()
    {
        if (isDashing) return;

        Vector3 wish = Quaternion.Euler(0f, yaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y);
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        // ── Velocidad horizontal actual ──
        Vector3 v = rb.linearVelocity;
        Vector3 hVel = new Vector3(v.x, 0f, v.z);

        if (wish.sqrMagnitude > 0.01f)
        {
            float objetivo = moveSpeed * (agachado ? bonusAgachado : 1f);
            float accel = aceleracion * (agachado ? 0.55f : 1f);

            if (agachado)
            {
                // AGACHADO = deslizar: solo acelera si vamos MÁS LENTOS que el
                // objetivo. Si venimos con velocidad (dash), NO se frena: se desliza.
                float velocidadActual = Vector3.Dot(hVel, wish);
                if (velocidadActual < objetivo)
                {
                    float falta = objetivo - velocidadActual;
                    hVel += wish * Mathf.Min(accel * Time.fixedDeltaTime, falta);
                }
            }
            else
            {
                // DE PIE = GRIP normal: la velocidad se acerca al objetivo
                // (incluye frenar el exceso, como un controlador clásico).
                hVel = Vector3.MoveTowards(hVel, wish * objetivo, accel * Time.fixedDeltaTime);
            }
        }
        else
        {
            // Sin input: frenar por fricción (agachado = casi no frena)
            float friccion = agachado ? friccionAgachado : (isGrounded ? friccionSuelo : friccionAire);
            hVel = Vector3.MoveTowards(hVel, Vector3.zero, friccion * Time.fixedDeltaTime);
        }

        rb.linearVelocity = new Vector3(hVel.x, v.y, hVel.z);

        // ── Transporte con plataforma móvil ──
        // Se mueve al jugador POR POSICIÓN con la plataforma (no se le suma
        // velocidad, porque eso lo disparaba al desplazarse).
        if (groundPlatform != null)
        {
            Vector3 delta = groundPlatform.Velocity * Time.fixedDeltaTime;
            if (delta.sqrMagnitude > 0.0001f)
                rb.MovePosition(rb.position + delta);

            // Anti-lanzamiento: si la plataforma sube, el jugador no la supera
            float subida = groundPlatform.Velocity.y;
            if (subida > 0f && rb.linearVelocity.y > subida)
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, subida, rb.linearVelocity.z);
        }

        // Girar al jugador hacia donde camina
        if (wish.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(wish);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                                                  Time.fixedDeltaTime * rotationSpeed);
        }
    }

    private void ApplyGravity()
    {
        if (isDashing && dashCancelsGravity)
        {
            Vector3 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;
        }
    }

    // ─────────────────────────────────────────────────── Salto

    private void CheckGround()
    {
        float bottomCenter = transform.position.y - (capsule.height * 0.5f - capsule.radius);
        Vector3 origin = new Vector3(transform.position.x, bottomCenter, transform.position.z);
        float dist = capsule.radius + groundSkin;

        int mask = groundMask.value != 0 ? groundMask.value : ~0;
        isGrounded = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, dist, mask, QueryTriggerInteraction.Ignore);

        if (isGrounded && hit.collider != null)
            groundPlatform = hit.collider.GetComponentInParent<MovingPlatform>();
        else
            groundPlatform = null;
    }

    private void UpdateJumpsLeft()
    {
        if (groundGuardTimer > 0f)
            groundGuardTimer -= Time.fixedDeltaTime;

        if (isGrounded)
        {
            if (groundGuardTimer <= 0f)
                jumpsLeft = maxJumps;
        }
        else if (wasGrounded && groundGuardTimer <= 0f)
        {
            // Salió del suelo sin saltar (se cayó de un borde): consume el salto "de suelo"
            if (jumpsLeft >= maxJumps)
                jumpsLeft = maxJumps - 1;
        }
    }

    private void HandleJump()
    {
        if (isDashing) return;
        if (jumpBufferTimer <= 0f || jumpsLeft <= 0) return;

        bool isDoubleJump = jumpsLeft < maxJumps;
        float force = isDoubleJump ? jumpForce * doubleJumpMultiplier : jumpForce;

        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;
        rb.AddForce(Vector3.up * force, ForceMode.Impulse);

        jumpsLeft--;
        jumpBufferTimer = 0f;
        groundGuardTimer = jumpGroundGuard;

        if (isDoubleJump)
            OnDobleSalto?.Invoke(transform.position);
    }

    // ─────────────────────────────────────────────────── Dash

    private void StartDash()
    {
        Vector3 fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

        // Impulso en la dirección del movimiento (o hacia adelante)
        Vector3 dashDir = moveInput.sqrMagnitude > 0.01f
            ? (fwd * moveInput.y + right * moveInput.x).normalized
            : fwd;

        Vector3 v = rb.linearVelocity;
        if (dashCancelsGravity)
            v.y = 0f;

        // IMPULSO: suma velocidad, no reemplaza el movimiento
        rb.linearVelocity = v + dashDir * dashForce;

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
    }

    private void UpdateDash()
    {
        if (!isDashing) return;

        dashTimer -= Time.fixedDeltaTime;
        if (dashTimer > 0f) return;

        // Fin del dash: NO se clampea la velocidad — la fricción se encarga
        // de disipar el momentum (agachado = más distancia recorrida).
        isDashing = false;
    }

    // ─────────────────────────────────────────────────── Agacharse

    private void HandleCrouch()
    {
        bool quiere = ReadCrouchHeld();
        agachado = quiere && isGrounded;

        // Altura de cámara suavizada
        float objetivo = cameraHeight - (agachado ? alturaAgachado : 0f);
        alturaActual = Mathf.Lerp(alturaActual, objetivo, Time.deltaTime * velocidadAgacharse);

        if (playerCamera != null)
        {
            Vector3 p = playerCamera.localPosition;
            p.y = alturaActual;
            playerCamera.localPosition = p;
        }
    }

    // ─────────────────────────────────────────────────── Cámara

    private void HandleLook()
    {
        if (playerCamera == null) return;

        Vector2 look = ReadLookInput() * mouseSensitivity;
        yaw += look.x;
        pitch -= look.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleCursor()
    {
        if (ReadUnlockCursorPressed())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (ReadLockCursorPressed() && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // ─────────────────────────────────────────────────── Estado público

    public bool IsGrounded => isGrounded;
    public bool IsDashing => isDashing;
    public bool Agachado => agachado;
    public float DashCooldownLeft => dashCooldownTimer;
    public float DashCooldown => dashCooldown;
    public int JumpsLeft => jumpsLeft;
    public int MaxJumps => maxJumps;
    public float MoveSpeed => moveSpeed;

    /// <summary>Altura actual de la cámara (baja al agacharse).</summary>
    public float AlturaCamara => alturaActual;

    /// <summary>Magnitud de la velocidad horizontal actual.</summary>
    public float VelocidadHorizontal => new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude;

    public event System.Action<Vector3> OnDobleSalto;

    public float MouseSensitivity
    {
        get => mouseSensitivity;
        set => mouseSensitivity = Mathf.Max(0.01f, value);
    }

    // ─────────────────────────────────────────────────── Input

    private void ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 move = Vector2.zero;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
        }
        moveInput = Vector2.ClampMagnitude(move, 1f);
#else
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
    }

    private Vector2 ReadLookInput()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }

    private bool ReadJumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.spaceKey.wasPressedThisFrame;
#else
        return Input.GetButtonDown("Jump");
#endif
    }

    private bool ReadDashPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        bool shift = kb != null && (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame);
        bool rmb = mouse != null && mouse.rightButton.wasPressedThisFrame;
        return shift || rmb;
#else
        return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetMouseButtonDown(1);
#endif
    }

    private bool ReadCrouchHeld()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return false;
        return kb.leftCtrlKey.isPressed || kb.cKey.isPressed;
#else
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
#endif
    }

    private bool LeerCipherWheel()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.tabKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Tab);
#endif
    }

    private bool ReadUnlockCursorPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private bool ReadLockCursorPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }
}
