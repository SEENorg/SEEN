using UnityEngine;
using UnityEngine.InputSystem;
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

private IPlayerInput entrada;
public IPlayerInput Entrada => entrada;
    private Rigidbody rb;
    private CapsuleCollider capsule;
    private static PhysicsMaterial _playerPhysMat;

    private Vector2 moveInput;

    // ── Camera Shake ──
    private float shakeTimer = 0f;
    private float shakeMagnitud = 0f;
    private Vector3 camPosOriginal;
    private bool camaraIniciada = false;

    // ───────────────────────────────────────────────────────────── Setup

    private void Awake()
{
    rb = GetComponent<Rigidbody>();
    capsule = GetComponent<CapsuleCollider>();

    PlayerInput playerInput = GetComponent<PlayerInput>();

    if (playerInput == null)
    {
        Debug.LogError("Player Input component not found on Player.");
        enabled = false;
        return;
    }

    entrada = new PlayerInputReader(playerInput);

        ConfigurarFisica();

        mirada.Iniciar(transform);
        suelo.Iniciar(capsule);
        salto.OnDobleSalto += AlHacerDobleSalto;
        salto.Inicializar();

        if (GetComponent<VidaJugador>() == null)
            gameObject.AddComponent<VidaJugador>();
        if (FindFirstObjectByType<HUDSistema>() == null)
            gameObject.AddComponent<HUDSistema>();

        // Primera persona: no ver la cápsula desde adentro
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // ── Integración: Registro de acciones del jugador en Pila (LIFO) ──
        var ed = EstructurasDatosJuego.Instancia;
        if (ed != null) ed.RegistrarAccionJugador("Jugador spawnado en el mundo distópico");
    }

    private void OnEnable()
    {
        // Nos suscribimos a la muerte para hacer el Camera Shake automáticamente
        PlayerKill.OnKill += ReaccionarMuerte;
    }

    private void OnDisable()
    {
        PlayerKill.OnKill -= ReaccionarMuerte;
    }

    private void ConfigurarFisica()
    {
        Physics.defaultSolverIterations = 12;
        Physics.defaultSolverVelocityIterations = 6;

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.angularDamping = 1f;

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

        Vector2 lookInput = entrada.LeerMirada(out bool esMando);
mirada.Mirar(lookInput, esMando);
        mirada.ManejarCursor(entrada.LiberarCursorPresionado(), entrada.CapturarCursorPresionado());

        dash.ActualizarCooldown();
        salto.RegistrarEntrada(!EstaColumpiando && entrada.SaltoPresionado());

        if (entrada.DashPresionado() && dash.PuedeIniciar())
        {
            dash.Iniciar(rb, mirada.Yaw, moveInput);
            // ── Integración: Registrar dash en Pila ──
            var edDash = EstructurasDatosJuego.Instancia;
            if (edDash != null) edDash.RegistrarAccionJugador("Dash ejecutado (impulso de vigilancia)");
        }
    }

    private void FixedUpdate()
    {
        suelo.Detectar(transform);
        salto.ActualizarSaltosRestantes(suelo.EstaEnSuelo, suelo.EstabaEnSuelo);

        // Durante el dash o el columpio con gancho no se camina ni se salta normalmente
        if (!dash.IsDashing && !EstaColumpiando)
        {
            movimiento.Mover(rb, moveInput, mirada.Yaw, suelo.EstaEnSuelo);
            suelo.TransportarConPlataforma(rb);
            salto.IntentarSaltar(rb, transform.position);

            // ── Integración: Registrar salto en Pila ──
            var edJump = EstructurasDatosJuego.Instancia;
            if (edJump != null) edJump.RegistrarAccionJugador("Salto ejecutado (evasión de amenaza)");
        }

        dash.ActualizarFisica();
        dash.AplicarGravedad(rb);

        transform.rotation = Quaternion.Euler(0f, mirada.Yaw, 0f);

        if (rb.angularVelocity.sqrMagnitude > 0f)
            rb.angularVelocity = Vector3.zero;

        suelo.GuardarEstado();
    }

    private void LateUpdate()
    {
        // Aplicar Camera Shake
        if (shakeTimer > 0f)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                // Guardar posición original en el primer frame de vibración
                if (!camaraIniciada)
                {
                    camPosOriginal = cam.transform.localPosition;
                    camaraIniciada = true;
                }

                shakeTimer -= Time.deltaTime;
                
                // Movimiento aleatorio en X e Y
                float offsetX = Random.Range(-1f, 1f) * shakeMagnitud;
                float offsetY = Random.Range(-1f, 1f) * shakeMagnitud;
                cam.transform.localPosition = camPosOriginal + new Vector3(offsetX, offsetY, 0f);

                // Si terminó el tiempo, restaurar posición
                if (shakeTimer <= 0f)
                {
                    cam.transform.localPosition = camPosOriginal;
                    camaraIniciada = false;
                }
            }
        }
    }

    // ─────────────────────────────────────────────────── Reaparición y Muerte

    public void ResetEstado()
    {
        EstaColumpiando = false;
        salto.Reiniciar();
        dash.Reiniciar();
        moveInput = Vector2.zero;

        Gancho g = GetComponentInChildren<Gancho>(true);
        if (g == null) g = Object.FindFirstObjectByType<Gancho>(FindObjectsInactive.Exclude);
        if (g != null) g.CancelarColumpio();
    }

    public bool EstaColumpiando { get; set; }

    public void RestaurarSaltoAereo()
    {
        salto.RestaurarSaltoAereo();
    }

    public void FijarOrientacion(Vector3 posicion, float yaw)
    {
        transform.position = posicion;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        mirada.FijarYaw(yaw);
    }

    private void ReaccionarMuerte()
    {
        // Activar vibración de cámara (duración, magnitud)
        shakeTimer = 0.4f;
        shakeMagnitud = 0.15f; 
    }

    // ─────────────────────────────────────────────────── Eventos

    public event System.Action<Vector3> OnDobleSalto;

    private void AlHacerDobleSalto(Vector3 posicion)
    {
        if (OnDobleSalto != null)
            OnDobleSalto(posicion);
    }

    // ─────────────────────────────────────────────────── Estado derivado

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

    public bool IsGrounded { get { return suelo.EstaEnSuelo; } }
    public bool IsDashing { get { return dash.IsDashing; } }
    public float DashCooldownLeft { get { return dash.DashCooldownLeft; } }
    public float DashCooldown { get { return dash.DashCooldown; } }
    public int JumpsLeft { get { return salto.JumpsLeft; } }
    public int MaxJumps { get { return salto.MaxJumps; } }
    public float MoveSpeed { get { return movimiento.MoveSpeed; } }
    public float AlturaCamara { get { return mirada.AlturaCamara; } }
    public float VelocidadHorizontal { get { return new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude; } }

    public float MouseSensitivity
    {
        get { return mirada.MouseSensitivity; }
        set { mirada.MouseSensitivity = Mathf.Max(0.01f, value); }
    }
}