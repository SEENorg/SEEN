using UnityEngine;

/// <summary>
/// Base común de todos los enemigos. Gestiona vida, daño, muerte, referencia
/// al jugador, el estado de buff y el cambio de color de la luz y el material.
/// </summary>
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Identidad")]
    [SerializeField] protected EnemyType tipo;

    [Header("Vida")]
    [SerializeField] protected int vidaMaxima = 3;
    [SerializeField] protected int vidaActual = 3;

    [Header("Daño al jugador")]
    [Tooltip("Si es true, al tocar al jugador lo mata (usa PlayerKill).")]
    [SerializeField] protected bool mataAlTocar = true;

    [Header("Luz y Material")]
    [Tooltip("Esfera de luz del enemigo")]
    [SerializeField] protected Light luzPropia;
    [Tooltip("El Renderer de la esfera (para cambiar el color del material)")]
    [SerializeField] protected Renderer esferaRenderer;
    
    [SerializeField] protected Color colorLuzNormal = Color.green;
    [SerializeField] protected Color colorLuzBuffeado = new Color(1f, 0.4f, 0f); // Naranja

    [Header("Contacto con el jugador")]
    [Tooltip("Distancia extra alrededor del enemigo que cuenta como tocar al jugador")]
    [SerializeField] protected float margenContacto = 0.3f;
    [Tooltip("Segundos mínimos entre dos muertes causadas por este enemigo")]
    [SerializeField] protected float cooldownContacto = 1f;

    [Header("Colisión con el entorno")]
    [Tooltip("Capas que el enemigo no puede atravesar (paredes, suelo, plataformas)")]
    [SerializeField] protected LayerMask capasObstaculo = ~0;
    [Tooltip("Radio con el que se comprueba el choque contra el entorno")]
    [SerializeField] protected float radioColision = 0.6f;

    [Header("Activación por puerta")]
    [Tooltip("Si se asigna una puerta, el enemigo no se mueve hasta que esa puerta se abra")]
    [SerializeField] protected DoorController puertaActivadora;

    protected Transform jugador;
    protected bool muerto;
    protected bool buffeado;
    protected Vector3 posicionOrigen;

    private Material materialInstancia; // Para no afectar a otros enemigos
    private Collider jugadorCollider;
    private float tiempoUltimoContacto = -10f;

    public EnemyType Tipo { get { return tipo; } }
    public int VidaActual { get { return vidaActual; } }
    public int VidaMaxima { get { return vidaMaxima; } }
    public bool Muerto { get { return muerto; } }
    public bool Buffeado { get { return buffeado; } }
    public bool PuedeMoverse { get { return puertaActivadora == null || puertaActivadora.Abierta; } }

    protected virtual void Start()
    {
        posicionOrigen = transform.position;
        vidaActual = vidaMaxima;
        BuscarJugador();

        // Clonamos el material para que el cambio de color sea único para este enemigo
        if (esferaRenderer != null)
        {
            materialInstancia = esferaRenderer.material;
        }

        ActualizarColorLuz();
    }

    public virtual void ReiniciarPosicion()
    {
        transform.position = posicionOrigen;
    }

    protected virtual void Update()
    {
        if (jugador == null && !muerto)
            BuscarJugador();

        // Contacto por cercanía: funciona aunque el collider del enemigo no sea trigger
        // o esté en un objeto hijo (en esos casos OnTriggerEnter nunca se dispara).
        if (!muerto && mataAlTocar && TocandoJugador())
            IntentarMatarJugador();
    }

    protected void BuscarJugador()
    {
        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            jugador = pc.transform;
            jugadorCollider = pc.GetComponent<Collider>();
        }
    }

    public virtual void RecibirDaño(int cantidad, DamageType tipoDaño)
    {
        if (muerto) return;
        vidaActual -= cantidad;

        if (vidaActual <= 0)
            Morir();
    }

    protected virtual void Morir()
    {
        if (muerto) return;
        muerto = true;
        OnMuerte();
        Destroy(gameObject);
    }

    protected virtual void OnMuerte() { }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (muerto) return;
        if (!mataAlTocar) return;

        if (other.CompareTag("Player"))
            IntentarMatarJugador();
    }

    /// <summary>
    /// Mata al jugador (RespawnOnFall escucha PlayerKill.OnKill y lo reaparece).
    /// El cooldown evita matarlo dos veces seguidas por el mismo contacto.
    /// </summary>
    protected void IntentarMatarJugador()
    {
        if (muerto || !mataAlTocar) return;
        if (Time.time - tiempoUltimoContacto < cooldownContacto) return;

        tiempoUltimoContacto = Time.time;
        PlayerKill.KillPlayer();
    }

    /// <summary>
    /// True si el collider del jugador toca (o casi toca) al enemigo. Compara las cajas
    /// envolventes, así que no depende de triggers, Rigidbody ni de dónde esté el collider.
    /// </summary>
    protected bool TocandoJugador()
    {
        if (jugadorCollider == null) return false;
        if (Time.deltaTime <= 0f) return false;   // juego en pausa (hackeo, menú...)

        Bounds limites;
        if (esferaRenderer != null)
            limites = esferaRenderer.bounds;
        else
            limites = new Bounds(transform.position, Vector3.one);
        limites.Expand(margenContacto * 2f);

        return jugadorCollider.bounds.Intersects(limites);
    }

    /// <summary>
    /// Mueve al enemigo hacia el destino SIN atravesar el entorno.
    /// Devuelve false si una pared lo bloqueó (no se mueve ese frame).
    /// </summary>
    protected bool MoverHacia(Vector3 destino, float velocidad)
    {
        if (!PuedeMoverse) return false;
        Vector3 haciaDestino = destino - transform.position;
        float distanciaDestino = haciaDestino.magnitude;
        if (distanciaDestino < 0.001f) return true;

        Vector3 direccion = haciaDestino / distanciaDestino;
        float paso = velocidad * Time.deltaTime;
        if (paso > distanciaDestino) paso = distanciaDestino;

        if (HayObstaculo(direccion, paso))
            return false;

        transform.position += direccion * paso;
        return true;
    }

    /// <summary>Comprueba si hay algo sólido en el camino (ignora al propio enemigo, a otros enemigos y al jugador).</summary>
    protected bool HayObstaculo(Vector3 direccion, float distancia)
    {
        RaycastHit[] golpes = Physics.SphereCastAll(transform.position, radioColision, direccion,
                                                    distancia + 0.02f, capasObstaculo,
                                                    QueryTriggerInteraction.Ignore);

        bool movimientoHorizontal = Mathf.Abs(direccion.y) < 0.2f;

        for (int i = 0; i < golpes.Length; i++)
        {
            Collider c = golpes[i].collider;
            if (c.transform.IsChildOf(transform)) continue;          // sus propios cubos y colliders
            if (c.CompareTag("Player")) continue;
            if (c.GetComponentInParent<EnemyBase>() != null) continue; // otros enemigos

            // Al moverse en horizontal, el suelo y el techo no lo frenan
            bool superficieHorizontal = Mathf.Abs(Vector3.Dot(golpes[i].normal, Vector3.up)) > 0.9f;
            if (movimientoHorizontal && superficieHorizontal) continue;

            return true;
        }
        return false;
    }

    public void AplicarBuff(bool aplicar)
    {
        if (buffeado == aplicar) return;
        buffeado = aplicar;
        ActualizarColorLuz();
    }

    protected void ActualizarColorLuz()
    {
        Color colorObjetivo = colorLuzNormal;
        if (buffeado) colorObjetivo = colorLuzBuffeado;

        // 1. Cambiar la luz
        if (luzPropia != null)
        {
            luzPropia.color = colorObjetivo;
        }

        // 2. Cambiar la emisión del material de la esfera
        if (materialInstancia != null)
        {
            materialInstancia.EnableKeyword("_EMISSION");
            
            // Multiplicamos el color por intensidad para que brille
            materialInstancia.SetColor("_EmissionColor", colorObjetivo * 2.5f);
            
            // Si es URP, actualizamos también el color base
            if (materialInstancia.HasProperty("_BaseColor"))
            {
                materialInstancia.SetColor("_BaseColor", colorObjetivo * 0.5f);
            }
        }
    }
}