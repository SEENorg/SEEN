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
    private float tiempoUltimoDañoRecibido = -10f;
    private Renderer[] renderersVisuales;
    private Collider[] collidersPropios;

    private static GUIStyle estiloNombreBarra;

    public EnemyType Tipo { get { return tipo; } }
    public int VidaActual { get { return vidaActual; } }
    public int VidaMaxima { get { return vidaMaxima; } }
    public bool Muerto { get { return muerto; } }
    public bool Buffeado { get { return buffeado; } }
    public bool PuedeMoverse { get { return puertaActivadora == null || puertaActivadora.Abierta; } }

    protected virtual void Start()
    {
        posicionOrigen = transform.position;
        if (vidaMaxima <= 0) vidaMaxima = 3;
        vidaActual = vidaMaxima;

        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rbs.Length; i++)
        {
            rbs[i].isKinematic = true;
            rbs[i].useGravity = false;
        }

        renderersVisuales = GetComponentsInChildren<Renderer>(true);
        collidersPropios = GetComponentsInChildren<Collider>(true);

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
        muerto = false;
        vidaActual = vidaMaxima;
        transform.position = posicionOrigen;
        gameObject.SetActive(true);
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
        tiempoUltimoDañoRecibido = Time.unscaledTime;
        vidaActual = Mathf.Max(0, vidaActual - cantidad);

        if (vidaActual <= 0)
            Morir();
    }

    protected virtual void Morir()
    {
        if (muerto) return;
        muerto = true;
        OnMuerte();
        gameObject.SetActive(false);
    }

    protected virtual void OnMuerte() { }

    protected Bounds ObtenerLimitesVisuales()
    {
        bool tiene = false;
        Bounds b = default;

        if (renderersVisuales != null)
        {
            for (int i = 0; i < renderersVisuales.Length; i++)
            {
                Renderer r = renderersVisuales[i];
                if (r == null || !r.enabled) continue;
                if (!tiene) { b = r.bounds; tiene = true; }
                else b.Encapsulate(r.bounds);
            }
        }

        if (!tiene && collidersPropios != null)
        {
            for (int i = 0; i < collidersPropios.Length; i++)
            {
                Collider c = collidersPropios[i];
                if (c == null || !c.enabled || c.isTrigger) continue;
                if (!tiene) { b = c.bounds; tiene = true; }
                else b.Encapsulate(c.bounds);
            }
        }

        if (!tiene) b = new Bounds(transform.position, Vector3.one);
        return b;
    }

    protected virtual void OnGUI()
    {
        if (muerto) return;
        if (HackManager.Abierto || PauseMenu.Pausado || CipherWheel.Abierta ||
            GameFeedback.NivelCompletadoVisible || GameFeedback.GameOverVisible)
            return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Bounds b = ObtenerLimitesVisuales();
        if (Vector3.Distance(cam.transform.position, b.center) > 35f) return;

        Vector3 posBarra = new Vector3(b.center.x, b.max.y + 0.45f, b.center.z);
        Vector3 sp = cam.WorldToScreenPoint(posBarra);
        if (sp.z <= 0f) return;

        if (estiloNombreBarra == null)
        {
            estiloNombreBarra = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        float ancho = 112f;
        float altoBarra = 10f;
        float altoTotal = 28f;
        float x = sp.x - ancho * 0.5f;
        float y = (Screen.height - sp.y) - altoTotal;

        int maxV = Mathf.Max(1, vidaMaxima);
        float ratio = Mathf.Clamp01((float)vidaActual / maxV);

        Color colorRelleno = new Color(0.15f, 0.95f, 0.45f, 0.95f);
        if (ratio <= 0.35f) colorRelleno = new Color(1f, 0.22f, 0.28f, 0.95f);
        else if (ratio <= 0.70f) colorRelleno = new Color(1f, 0.70f, 0.15f, 0.95f);

        bool impactoReciente = (Time.unscaledTime - tiempoUltimoDañoRecibido) < 0.22f;
        Color colorBorde = impactoReciente
            ? new Color(1f, 0.25f, 0.30f, 1f)
            : new Color(0.15f, 0.85f, 1f, 0.9f);

        Color previo = GUI.color;

        // Fondo y borde estilo SEEN
        GUI.color = new Color(0.03f, 0.04f, 0.07f, 0.88f);
        GUI.DrawTexture(new Rect(x - 4f, y, ancho + 8f, altoTotal), Texture2D.whiteTexture);

        GUI.color = colorBorde;
        GUI.DrawTexture(new Rect(x - 4f, y, ancho + 8f, 1.5f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - 4f, y + altoTotal - 1.5f, ancho + 8f, 1.5f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - 4f, y, 1.5f, altoTotal), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + ancho + 2.5f, y, 1.5f, altoTotal), Texture2D.whiteTexture);

        // Nombre del enemigo
        estiloNombreBarra.normal.textColor = new Color(0.85f, 0.95f, 1f, 0.95f);
        GUI.color = Color.white;
        GUI.Label(new Rect(x - 4f, y + 1f, ancho + 8f, 14f), ObtenerNombreBarra(), estiloNombreBarra);

        // Fondo de barra
        float barY = y + 15f;
        GUI.color = new Color(0.22f, 0.08f, 0.10f, 0.9f);
        GUI.DrawTexture(new Rect(x, barY, ancho, altoBarra), Texture2D.whiteTexture);

        // Relleno de vida
        if (ratio > 0f)
        {
            GUI.color = impactoReciente ? Color.white : colorRelleno;
            GUI.DrawTexture(new Rect(x, barY, ancho * ratio, altoBarra), Texture2D.whiteTexture);
        }

        // Separadores por segmento
        float seg = ancho / maxV;
        GUI.color = new Color(0.03f, 0.04f, 0.07f, 0.95f);
        for (int i = 1; i < maxV; i++)
            GUI.DrawTexture(new Rect(x + seg * i - 0.75f, barY, 1.5f, altoBarra), Texture2D.whiteTexture);

        GUI.color = previo;
    }

    private string ObtenerNombreBarra()
    {
        switch (tipo)
        {
            case EnemyType.Hashtag: return "HASHTAG";
            case EnemyType.Repost: return "REPOST";
            case EnemyType.Notificacion: return "NOTIFICACIÓN";
            case EnemyType.Algoritmo: return "ALGORITMO";
            default: return "AMENAZA";
        }
    }

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