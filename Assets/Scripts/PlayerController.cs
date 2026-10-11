using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Jugador de SEEN en PRIMERA PERSONA.
///
///   WASD / Flechas = mover | Mouse = mirar | Espacio = saltar (doble salto)
///   Shift der/iz o clic derecho = DASH
///   Esc libera el mouse, clic izq lo vuelve a capturar
///
/// El DASH fue tomado del comportamiento del proyecto Jueguinho:
///   - Se aplica AL INSTANTE sumando velocidad (rb.linearVelocity += dashDir * dashSpeed),
///     no empuja con fuerzas ni usa corrutinas.
///   - Durante dashDuration segundos no se aplica el movimiento normal.
///   - Al terminar, la velocidad horizontal se clampea suavemente a moveSpeed * exitMult
///     (frenada progresiva, no un corte brusco).
///
/// Extras heredados de la versión anterior de SEEN:
///   - Jump buffer (el salto presionado antes de aterrizar no se pierde)
///   - Al caer de un borde SIN saltar solo queda 1 salto (no 2)
///   - Ground check por Raycast desde la base de la cápsula (no detecta el propio collider)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField, Min(1)] private int maxJumps = 2;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 7f;
    [Tooltip("El segundo salto es un poco más flojo que el primero")]
    [SerializeField, Range(0.5f, 1f)] private float doubleJumpMultiplier = 0.9f;

    [Header("Dash (comportamiento Jueguinho)")]
    [SerializeField] private float dashSpeed = 22f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1f;
    [Tooltip("Si está activo, el dash anula la gravedad durante su duración")]
    [SerializeField] private bool dashCancelsGravity = true;
    [Tooltip("Velocidad horizontal máxima al salir del dash (x moveSpeed)")]
    [SerializeField] private float dashExitMultiplier = 1.8f;

    [Header("Cámara")]
    [Tooltip("Cámara hija del jugador (se crea sola si no la asignás)")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float cameraHeight = 0.65f;

    [Header("Detección de suelo")]
    [Tooltip("Capas de suelo. Si lo dejás en Nothing, detecta contra todo.")]
    [SerializeField] private LayerMask groundMask;
    [Tooltip("Margen extra bajo los pies para considerar que está en suelo")]
    [SerializeField] private float groundSkin = 0.12f;

    [Header("Game Feel")]
    [Tooltip("Cuánto se 'recuerda' el salto presionado antes de tocar el suelo")]
    [SerializeField] private float jumpBufferTime = 0.15f;
    [Tooltip("Tras saltar, ignorar el ground check este tiempo para no recargar saltos")]
    [SerializeField] private float jumpGroundGuard = 0.12f;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private static PhysicsMaterial _playerPhysMat;

    private bool isGrounded;
    private bool wasGrounded;
    private int jumpsLeft;
    private float jumpBufferTimer;
    private float groundGuardTimer;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;

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
        // 1) Más iteraciones del solver: contactos en esquinas/bordes
        //    mucho más estables (menos penetración alternada = menos jitter)
        Physics.defaultSolverIterations = 12;
        Physics.defaultSolverVelocityIterations = 6;

        // 2) Fricción baja en la cápsula: al rozar un borde, desliza
        //    en vez de trabarse (stick-slip)
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

        // Congelar inclinación (X/Z) pero permitir el yaw (Y) que maneja el mouse
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        yaw = transform.eulerAngles.y;

        EnsureCamera();

        // Primera persona: no ver la cápsula desde adentro
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Si no asignaste una cámara hija, busca Camera.main y la engancha al jugador.
    /// </summary>
    private void EnsureCamera()
    {
        if (playerCamera == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam != null) playerCamera = cam.transform;
        }

        if (playerCamera == null) return;

        // Asegurar que sea hija del jugador y quede a la altura de la cabeza
        if (playerCamera.parent != transform)
            playerCamera.SetParent(transform, false);
        playerCamera.localPosition = new Vector3(0f, cameraHeight, 0f);
        playerCamera.localRotation = Quaternion.identity;
    }

    // ───────────────────────────────────────────────────────────── Loop

    /// <summary>
    /// ANTI-TEMBLOR: cuando la cápsula roza una pared o el borde de una
    /// plataforma, anulamos la velocidad HORIZONTAL que entra en la superficie.
    /// Así el solver de física no tiene que empujar la cápsula hacia afuera
    /// cada frame (esa pelea es la que hacía temblar la cámara).
    /// El eje vertical NO se toca, para no romper la gravedad ni los saltos.
    /// </summary>
    private void OnCollisionStay(Collision collision)
    {
        Vector3 v = rb.linearVelocity;
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);
        if (horizontal.sqrMagnitude < 0.0001f) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 n = collision.GetContact(i).normal;

            // Solo paredes/objetos: si la normal es casi vertical (suelo/techo), no tocar
            Vector3 wallNormal = new Vector3(n.x, 0f, n.z);
            if (wallNormal.sqrMagnitude < 0.05f) continue;
            wallNormal.Normalize();

            float into = Vector3.Dot(horizontal, wallNormal);
            if (into < 0f)
                horizontal -= wallNormal * into;
        }

        if (horizontal != new Vector3(v.x, 0f, v.z))
            rb.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);
    }

    private void Update()
    {
        ReadMoveInput();
        HandleLook();
        HandleCursor();

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        // Buffer de salto: guarda el input aunque se presione un instante antes del aterrizaje
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
        if (!isDashing) HandleMovement();
        HandleJump();
        UpdateDash();
        ApplyGravity();

        // El yaw del cuerpo se aplica por física (como en Jueguinho)
        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));

        wasGrounded = isGrounded;
    }

    // ─────────────────────────────────────────────────── Movimiento

    private void HandleMovement()
    {
        Vector3 fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

        Vector3 move = fwd * moveInput.y + right * moveInput.x;
        if (move.sqrMagnitude > 1f) move.Normalize();
        move *= moveSpeed;

        // Se mantiene la velocidad vertical (gravedad / salto activos)
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }

    private void ApplyGravity()
    {
        // Gravedad estándar de Unity ya aplicada al Rigidbody;
        // en el dash con dashCancelsGravity se anula un frame.
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
        // Raycast desde el centro de la esfera inferior de la cápsula.
        // Al originarse DENTRO del collider propio, nunca se detecta a sí mismo.
        float bottomCenter = transform.position.y - (capsule.height * 0.5f - capsule.radius);
        Vector3 origin = new Vector3(transform.position.x, bottomCenter, transform.position.z);
        float dist = capsule.radius + groundSkin;

        int mask = groundMask.value != 0 ? groundMask.value : ~0;
        isGrounded = Physics.Raycast(origin, Vector3.down, dist, mask, QueryTriggerInteraction.Ignore);
    }

    private void UpdateJumpsLeft()
    {
        if (groundGuardTimer > 0f)
            groundGuardTimer -= Time.fixedDeltaTime;

        if (isGrounded)
        {
            // En el suelo siempre se recargan los saltos (salvo justo después de saltar)
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
    }

    // ─────────────────────────────────────────────────── Dash

    private void StartDash()
    {
        Vector3 fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

        // Dirección: hacia donde te movés; si no hay input, hacia adelante
        Vector3 dashDir = moveInput.sqrMagnitude > 0.01f
            ? (fwd * moveInput.y + right * moveInput.x).normalized
            : fwd;

        Vector3 v = rb.linearVelocity;
        if (dashCancelsGravity)
            v.y = 0f;
        rb.linearVelocity = v + dashDir * dashSpeed;

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
    }

    private void UpdateDash()
    {
        if (!isDashing) return;

        dashTimer -= Time.fixedDeltaTime;
        if (dashTimer > 0f) return;

        // Fin del dash: clampeo suave de la velocidad horizontal (sin frenada brusca)
        isDashing = false;
        Vector3 v = rb.linearVelocity;
        Vector2 h = new Vector2(v.x, v.z);
        float maxH = moveSpeed * dashExitMultiplier;
        if (h.magnitude > maxH)
        {
            h = h.normalized * maxH;
            rb.linearVelocity = new Vector3(h.x, v.y, h.y);
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

        // Solo el pitch va en la cámara; el yaw rota el cuerpo del jugador
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

    // ─────────────────────────────────────────────────── API pública

    public bool IsGrounded => isGrounded;
    public bool IsDashing => isDashing;
    public float DashCooldownLeft => dashCooldownTimer;
    public int JumpsLeft => jumpsLeft;

    /// <summary>Sensibilidad del ratón (la ajusta el menú de Ajustes vía PlayerPrefs).</summary>
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

    private Vector2 ReadLookInput()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
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

    // ─────────────────────────────────────────────────── Debug

    private void OnDrawGizmosSelected()
    {
        if (capsule == null) capsule = GetComponent<CapsuleCollider>();
        if (capsule == null) return;

        float bottomCenter = transform.position.y - (capsule.height * 0.5f - capsule.radius);
        Vector3 origin = new Vector3(transform.position.x, bottomCenter, transform.position.z);

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(origin, Vector3.down * (capsule.radius + groundSkin));
        Gizmos.DrawWireSphere(origin, 0.06f);
    }
}
