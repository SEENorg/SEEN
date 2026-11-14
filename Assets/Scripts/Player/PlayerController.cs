using UnityEngine;

/// <summary>
/// Jugador de SEEN en PRIMERA PERSONA. Esta clase es el COORDINADORA: configura la física,
/// llama a cada componente en el orden correcto y expone el estado público que usan los
/// demás scripts (HeadBob, SkillIndicators, SpeedLines, JumpRing).
///
/// La lógica está repartida (una responsabilidad por clase):
///   PlayerInputReader → lee teclado y ratón
///   PlayerLook        → cámara, giro del ratón, altura y cursor
///   PlayerGround      → detección de suelo y plataformas móviles
///   PlayerCrouch      → estado de agachado
///   PlayerMovement    → caminar con momentum (y deslizarse agachado)
///   PlayerJump        → salto, doble salto y buffer de salto
///   PlayerDash        → impulso del dash y su recarga
///
/// Controles: WASD mover | Mouse mirar | Espacio saltar (doble) | Shift dash
///            Ctrl/C agacharse | TAB tabla César | Esc pausa
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Componentes (se editan desde aquí)")]
    [SerializeField] private PlayerLook mirada = new PlayerLook();
    [SerializeField] private PlayerGround suelo = new PlayerGround();
    [SerializeField] private PlayerMovement movimiento = new PlayerMovement();
    [SerializeField] private PlayerJump salto = new PlayerJump();
    [SerializeField] private PlayerDash dash = new PlayerDash();

    private PlayerInputReader entrada = new PlayerInputReader();
    private Rigidbody rb;
    private CapsuleCollider capsule;
    private static PhysicsMaterial _playerPhysMat;

    private Vector2 moveInput;

    // ───────────────────────────────────────────────────────────── Setup

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        ConfigurarFisica();

        mirada.Iniciar(transform);
        suelo.Iniciar(capsule);
        salto.OnDobleSalto += AlHacerDobleSalto;

        // Primera persona: no ver la cápsula desde adentro
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ConfigurarFisica()
    {
        // ── Anti-temblor de cámara ─────────────────────────────
        // (no solo del jugador). Se dejan aquí a propósito: el juego tiene un único jugador.
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

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    // ───────────────────────────────────────────────────────────── Loop

    private void Update()
    {
        moveInput = entrada.LeerMovimiento();

        mirada.Mirar(entrada.LeerMirada());
        mirada.ManejarCursor(entrada.LiberarCursorPresionado(), entrada.CapturarCursorPresionado());

        dash.ActualizarCooldown();
        salto.RegistrarEntrada(entrada.SaltoPresionado());

        if (entrada.DashPresionado() && dash.PuedeIniciar())
            dash.Iniciar(rb, mirada.Yaw, moveInput);
    }

    private void FixedUpdate()
    {
        suelo.Detectar(transform);
        salto.ActualizarSaltosRestantes(suelo.EstaEnSuelo, suelo.EstabaEnSuelo);

        // Durante el dash no se camina ni se salta
        if (!dash.IsDashing)
        {
            movimiento.Mover(rb, moveInput, mirada.Yaw, suelo.EstaEnSuelo);
            suelo.TransportarConPlataforma(rb);
            salto.IntentarSaltar(rb, transform.position);
        }

        dash.ActualizarFisica();
        dash.AplicarGravedad(rb);

        transform.rotation = Quaternion.Euler(0f, mirada.Yaw, 0f);

        if (rb.angularVelocity.sqrMagnitude > 0f)
            rb.angularVelocity = Vector3.zero;

        suelo.GuardarEstado();
    }

    // ─────────────────────────────────────────────────── Reaparición

    /// <summary>
    /// Limpia el estado interno (saltos, buffers, dash, agachado) al reaparecer. Lo llama RespawnOnFall.
    /// </summary>
    public void ResetEstado()
    {
        salto.Reiniciar();
        dash.Reiniciar();
        moveInput = Vector2.zero;
    }

    // ─────────────────────────────────────────────────── Eventos

    /// <summary>Se dispara al hacer un doble salto (lo usa JumpRing).</summary>
    public event System.Action<Vector3> OnDobleSalto;

    private void AlHacerDobleSalto(Vector3 posicion)
    {
        if (OnDobleSalto != null)
            OnDobleSalto(posicion);
    }

    // ─────────────────────────────────────────────────── Estado derivado

    /// <summary>
    /// Estado del jugador en este instante, derivado de sus flags.
    /// Prioridad: Dashing > Crouching > Grounded/Airborne.
    /// Es una VISTA calculada (no una FSM): no altera el comportamiento, solo lo expone
    /// para que los consumidores (HeadBob, SpeedLines, Crosshair...) usen switch en lugar
    /// de encadenar booleanos.
    /// </summary>
    public PlayerState Estado
    {
        get
        {
            if (dash.IsDashing) return PlayerState.Dashing;
            if (suelo.EstaEnSuelo) return PlayerState.Grounded;
            return PlayerState.Airborne;
        }
    }

    // ─────────────────────────────────────────────────── Estado público

    public bool IsGrounded
    {
        get { return suelo.EstaEnSuelo; }
    }

    public bool IsDashing
    {
        get { return dash.IsDashing; }
    }

    public float DashCooldownLeft
    {
        get { return dash.DashCooldownLeft; }
    }

    public float DashCooldown
    {
        get { return dash.DashCooldown; }
    }

    public int JumpsLeft
    {
        get { return salto.JumpsLeft; }
    }

    public int MaxJumps
    {
        get { return salto.MaxJumps; }
    }

    public float MoveSpeed
    {
        get { return movimiento.MoveSpeed; }
    }

    /// <summary>Altura actual de la cámara (baja al agacharse).</summary>
    public float AlturaCamara
    {
        get { return mirada.AlturaCamara; }
    }

    /// <summary>Magnitud de la velocidad horizontal actual.</summary>
    public float VelocidadHorizontal
    {
        get { return new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude; }
    }

    public float MouseSensitivity
    {
        get { return mirada.MouseSensitivity; }
        set { mirada.MouseSensitivity = Mathf.Max(0.01f, value); }
    }
}