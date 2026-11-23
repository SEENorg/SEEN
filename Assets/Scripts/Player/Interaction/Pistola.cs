using UnityEngine;

/// <summary>
/// Pistola interactuable para el Nivel 2: aparece sobre un pedestal en el salón previo.
/// Al recogerla (con E / Y o al acercarse) se equipa en primera persona y dispara
/// proyectiles de energía que reducen la vida de los enemigos (EnemyBase).
///
/// Munición: cargador de 10 balas. Se recarga con R (teclado) o RB (mando), o sola al
/// intentar disparar sin balas. La recarga tiene animación: el arma se inclina, el
/// cargador cae y vuelve a entrar, el riel de energía se apaga y vuelve a encenderse.
/// El contador en pantalla lo dibuja HUDMunicion, que lee esta clase como IArmaMunicion.
/// </summary>
public class Pistola : MonoBehaviour, IInteractuable, IArmaMunicion
{
    [Header("Disparo")]
    [SerializeField] private float cooldownDisparo = 0.22f;
    [SerializeField] private float velocidadProyectil = 38f;
    [SerializeField] private float alcanceMaximo = 45f;
    [SerializeField] private int danoPorDisparo = 1;

    [Header("Munición")]
    [SerializeField] private int capacidadCargador = 10;
    [SerializeField] private float duracionRecarga = 1.3f;
    [Tooltip("Si está activo, al intentar disparar sin balas empieza a recargar sola.")]
    [SerializeField] private bool recargaAutomatica = true;

    // Momentos de la animación de recarga (fracción de la duración)
    private const float FinInclinar = 0.18f;
    private const float FinSacarCargador = 0.36f;
    private const float InicioMeterCargador = 0.52f;
    private const float FinMeterCargador = 0.72f;
    private const float InicioEnderezar = 0.82f;
    private const float BajadaCargador = 0.28f;
    private const float EmisionRiel = 2.5f;

    private readonly Color colorEnergia = new Color(0.15f, 0.85f, 1f);
    private readonly Vector3 posicionEnCamara = new Vector3(0.32f, -0.25f, 0.55f);
    private readonly Vector3 posicionCargador = new Vector3(0f, -0.20f, -0.125f);

    private bool equipada;
    private float tiempoProximoDisparo;
    private Transform puntoDisparo;
    private Transform cargador;
    private Material materialEnergia;
    private TextMesh etiqueta;
    private Collider[] collidersPropios;
    private IPlayerInput entrada;
    private PlayerController jugador;

    private int municionActual;
    private bool recargando;
    private float tiempoRecarga;
    private bool cargadorLleno;
    private float retrocesoActual;
    private float golpeCargador;
    private float destelloDisparo;
    private int disparosRealizados;
    private int intentosSinMunicion;

    // ── IInteractuable ──────────────────────────────────────────────

    public string TextoPrompt
    {
        get { return "Presione E para recoger pistola"; }
    }

    public bool PuedeInteractuar
    {
        get { return !equipada; }
    }

    public void Interactuar()
    {
        if (!equipada) Equipar();
    }

    // ── IArmaMunicion ───────────────────────────────────────────────

    public string NombreArma
    {
        get { return "PISTOLA DE PULSOS"; }
    }

    public int MunicionActual
    {
        get { return municionActual; }
    }

    public int Capacidad
    {
        get { return capacidadCargador; }
    }

    public bool Recargando
    {
        get { return recargando; }
    }

    public float ProgresoRecarga
    {
        get
        {
            if (!recargando) return 0f;
            // Las balas "entran" mientras el cargador sube
            float t = tiempoRecarga / duracionRecarga;
            return Mathf.Clamp01((t - InicioMeterCargador) / (FinMeterCargador - InicioMeterCargador));
        }
    }

    public int DisparosRealizados
    {
        get { return disparosRealizados; }
    }

    public int IntentosSinMunicion
    {
        get { return intentosSinMunicion; }
    }

    // ── Inicio ──────────────────────────────────────────────────────

    private void Start()
    {
        jugador = Object.FindFirstObjectByType<PlayerController>();
        if (jugador != null) entrada = jugador.Entrada;
        else Debug.LogError("[Pistola] No se encontró PlayerController para obtener la entrada.");

        collidersPropios = GetComponentsInChildren<Collider>(true);
        ConstruirModelo();
        CrearEtiqueta();
    }

    private void CrearEtiqueta()
    {
        GameObject etiquetaGO = new GameObject("PromptPistola");
        etiquetaGO.transform.SetParent(transform, false);
        etiquetaGO.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        etiqueta = etiquetaGO.AddComponent<TextMesh>();
        etiqueta.text = TextoPrompt;
        etiqueta.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        etiqueta.fontSize = 34;
        etiqueta.characterSize = 0.04f;
        etiqueta.anchor = TextAnchor.MiddleCenter;
        etiqueta.alignment = TextAlignment.Center;
        etiqueta.color = colorEnergia;
        MeshRenderer malla = etiquetaGO.GetComponent<MeshRenderer>();
        if (malla == null) malla = etiquetaGO.AddComponent<MeshRenderer>();
        malla.sharedMaterial = etiqueta.font.material;
    }

    private void ConstruirModelo()
    {
        Material matOscuro = CrearMaterial(new Color(0.11f, 0.13f, 0.17f), Color.black, 0f);
        materialEnergia = CrearMaterial(new Color(0.05f, 0.3f, 0.4f), colorEnergia, EmisionRiel);

        CrearParte("Cuerpo", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(0.09f, 0.11f, 0.42f), matOscuro, transform);
        CrearParte("Canon", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0.24f), new Vector3(0.06f, 0.06f, 0.22f), matOscuro, transform);
        CrearParte("RielEnergia", PrimitiveType.Cube, new Vector3(0f, 0.085f, 0.06f), new Vector3(0.04f, 0.025f, 0.44f), materialEnergia, transform);
        GameObject empunadura = CrearParte("Empunadura", PrimitiveType.Cube, new Vector3(0f, -0.10f, -0.11f),
                                           new Vector3(0.075f, 0.18f, 0.10f), matOscuro, transform);
        empunadura.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);

        // Cargador: sale por abajo de la empuñadura al recargar
        GameObject goCargador = new GameObject("Cargador");
        goCargador.transform.SetParent(transform, false);
        goCargador.transform.localPosition = posicionCargador;
        goCargador.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);
        cargador = goCargador.transform;
        CrearParte("CuerpoCargador", PrimitiveType.Cube, Vector3.zero, new Vector3(0.062f, 0.07f, 0.082f), matOscuro, cargador);
        CrearParte("LuzCargador", PrimitiveType.Cube, new Vector3(0f, 0f, 0.042f), new Vector3(0.045f, 0.05f, 0.006f), materialEnergia, cargador);

        GameObject boca = new GameObject("PuntoDisparo");
        boca.transform.SetParent(transform, false);
        boca.transform.localPosition = new Vector3(0f, 0.055f, 0.38f);
        puntoDisparo = boca.transform;
    }

    private GameObject CrearParte(string nombre, PrimitiveType tipo, Vector3 posicion, Vector3 escala, Material material, Transform padre)
    {
        GameObject go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;
        go.transform.localScale = escala;
        Object.Destroy(go.GetComponent<Collider>());
        MeshRenderer malla = go.GetComponent<MeshRenderer>();
        if (malla != null) malla.sharedMaterial = material;
        return go;
    }

    private static Material CrearMaterial(Color colorBase, Color emision, float intensidad)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.color = colorBase;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colorBase);
        if (intensidad > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emision * intensidad);
        }
        return material;
    }

    private void Equipar()
    {
        Camera camara = Camera.main;
        if (camara == null) return;

        equipada = true;
        municionActual = capacidadCargador;
        tiempoProximoDisparo = Time.unscaledTime + 0.25f;

        if (etiqueta != null) etiqueta.gameObject.SetActive(false);
        if (collidersPropios != null)
        {
            for (int i = 0; i < collidersPropios.Length; i++)
            {
                if (collidersPropios[i] != null) collidersPropios[i].enabled = false;
            }
        }

        transform.SetParent(camara.transform, false);
        transform.localPosition = posicionEnCamara;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        HUDMunicion hud = GetComponent<HUDMunicion>();
        if (hud == null) hud = gameObject.AddComponent<HUDMunicion>();
        hud.Configurar(this);

        GameFeedback.Mensaje("Pistola equipada — Clic izquierdo / X para disparar, R / RB para recargar");
    }

    // ── Cada frame ──────────────────────────────────────────────────

    private void Update()
    {
        if (!equipada)
        {
            AnimarEnPedestal();
            return;
        }

        float dt = Time.deltaTime;
        retrocesoActual = Mathf.Lerp(retrocesoActual, 0f, dt * 14f);
        golpeCargador = Mathf.Lerp(golpeCargador, 0f, dt * 12f);
        destelloDisparo = Mathf.MoveTowards(destelloDisparo, 0f, dt * 8f);

        if (recargando) AvanzarRecarga(dt);
        AplicarPose();
        AplicarEnergia();

        if (Time.timeScale <= 0f || HackManager.Abierto || PauseMenu.Pausado || CipherWheel.Abierta ||
            GameFeedback.NivelCompletadoVisible || GameFeedback.GameOverVisible || PantallaControles.Visible)
            return;
        if (entrada == null) return;

        if (entrada.RecargaPresionada()) EmpezarRecarga();

        if (entrada.AtaquePresionado() && Time.unscaledTime >= tiempoProximoDisparo && !recargando)
        {
            tiempoProximoDisparo = Time.unscaledTime + cooldownDisparo;
            if (municionActual > 0)
            {
                Disparar();
            }
            else
            {
                intentosSinMunicion++;
                if (recargaAutomatica) EmpezarRecarga();
            }
        }
    }

    private void AnimarEnPedestal()
    {
        transform.Rotate(0f, 55f * Time.deltaTime, 0f, Space.World);

        Camera camara = Camera.main;
        if (etiqueta != null && camara != null)
            etiqueta.transform.rotation = Quaternion.LookRotation(etiqueta.transform.position - camara.transform.position);

        // Recoger también al acercarse
        if (jugador == null) jugador = Object.FindFirstObjectByType<PlayerController>();
        if (jugador != null && Vector3.Distance(jugador.transform.position, transform.position) <= 1.35f)
            Equipar();
    }

    // ── Recarga ─────────────────────────────────────────────────────

    private void EmpezarRecarga()
    {
        if (recargando || municionActual >= capacidadCargador) return;
        recargando = true;
        tiempoRecarga = 0f;
        cargadorLleno = false;
    }

    private void AvanzarRecarga(float dt)
    {
        tiempoRecarga += dt;
        float t = tiempoRecarga / duracionRecarga;

        // Cuando el cargador termina de entrar: "clac", balas llenas
        if (!cargadorLleno && t >= FinMeterCargador)
        {
            cargadorLleno = true;
            municionActual = capacidadCargador;
            golpeCargador = 1f;
        }

        if (t >= 1f)
        {
            recargando = false;
            tiempoRecarga = 0f;
        }
    }

    /// <summary>Posición y giro del arma y del cargador según el momento de la recarga.</summary>
    private void AplicarPose()
    {
        float inclinacion = 0f;
        float bajadaCargador = 0f;

        if (recargando)
        {
            float t = tiempoRecarga / duracionRecarga;

            if (t < FinInclinar) inclinacion = Suave(t / FinInclinar);
            else if (t < InicioEnderezar) inclinacion = 1f;
            else inclinacion = 1f - Suave((t - InicioEnderezar) / (1f - InicioEnderezar));

            if (t >= FinInclinar && t < FinSacarCargador)
                bajadaCargador = Suave((t - FinInclinar) / (FinSacarCargador - FinInclinar));
            else if (t >= FinSacarCargador && t < InicioMeterCargador)
                bajadaCargador = 1f;
            else if (t >= InicioMeterCargador && t < FinMeterCargador)
                bajadaCargador = 1f - Suave((t - InicioMeterCargador) / (FinMeterCargador - InicioMeterCargador));
        }

        Vector3 posicion = posicionEnCamara;
        posicion += new Vector3(-0.04f * inclinacion, -0.07f * inclinacion + 0.015f * golpeCargador, -retrocesoActual);
        transform.localPosition = posicion;
        transform.localRotation = Quaternion.Euler(-18f * inclinacion - 6f * golpeCargador - retrocesoActual * 60f,
                                                   -8f * inclinacion, 32f * inclinacion);

        if (cargador != null)
        {
            Vector3 abajo = Quaternion.Euler(14f, 0f, 0f) * Vector3.down;
            cargador.localPosition = posicionCargador + abajo * (BajadaCargador * bajadaCargador);
            cargador.localRotation = Quaternion.Euler(14f + 25f * bajadaCargador, 0f, 0f);
        }
    }

    /// <summary>El riel brilla al disparar, se apaga sin balas o recargando, y destella al quedar cargado.</summary>
    private void AplicarEnergia()
    {
        if (materialEnergia == null) return;

        float intensidad = EmisionRiel;
        if (recargando && !cargadorLleno)
        {
            intensidad = 0.35f + 0.25f * Mathf.Sin(Time.time * 30f);
        }
        else if (municionActual <= 0)
        {
            intensidad = 0.25f;
        }
        else if ((float)municionActual / capacidadCargador <= 0.3f)
        {
            intensidad = 1.2f + 0.8f * Mathf.Sin(Time.time * 8f);
        }

        intensidad += golpeCargador * 6f + destelloDisparo * 3f;
        materialEnergia.SetColor("_EmissionColor", colorEnergia * intensidad);
    }

    private static float Suave(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    // ── Disparo ─────────────────────────────────────────────────────

    private void Disparar()
    {
        Camera camara = Camera.main;
        if (camara == null || municionActual <= 0) return;

        municionActual--;
        disparosRealizados++;
        retrocesoActual = 0.08f;
        destelloDisparo = 1f;

        Vector3 origenCamara = camara.transform.position;
        Vector3 direccionCamara = camara.transform.forward;
        Vector3 puntoObjetivo = origenCamara + direccionCamara * alcanceMaximo;
        float distanciaPrimerImpacto = alcanceMaximo;

        RaycastHit[] impactos = Physics.RaycastAll(origenCamara, direccionCamara, alcanceMaximo, ~0, QueryTriggerInteraction.Ignore);
        OrdenarPorDistancia(impactos);

        for (int i = 0; i < impactos.Length; i++)
        {
            Collider c = impactos[i].collider;
            if (c == null || EsDelJugador(c)) continue;
            puntoObjetivo = impactos[i].point;
            distanciaPrimerImpacto = impactos[i].distance;
            break;
        }

        Vector3 origenBoca = origenCamara + direccionCamara * 0.5f;
        if (puntoDisparo != null) origenBoca = puntoDisparo.position;
        float distanciaCamaraABoca = Vector3.Distance(origenCamara, origenBoca);

        // Si el objetivo está a quemarropa, el proyectil sale desde la cámara para no traspasarlo
        Vector3 origenProyectil = origenBoca;
        if (distanciaPrimerImpacto < distanciaCamaraABoca + 0.25f)
            origenProyectil = origenCamara + direccionCamara * 0.15f;

        Vector3 direccionProyectil = (puntoObjetivo - origenProyectil).normalized;
        if (direccionProyectil.sqrMagnitude < 0.001f) direccionProyectil = direccionCamara;

        GameObject proyectilGO = new GameObject("ProyectilPistola");
        proyectilGO.transform.position = origenProyectil;
        proyectilGO.transform.rotation = Quaternion.LookRotation(direccionProyectil);
        ProyectilPistola proyectil = proyectilGO.AddComponent<ProyectilPistola>();
        proyectil.Inicializar(direccionProyectil, velocidadProyectil, alcanceMaximo, danoPorDisparo);
    }

    private static bool EsDelJugador(Collider c)
    {
        if (c.CompareTag("Player")) return true;
        if (c.GetComponentInParent<PlayerController>() != null) return true;
        return c.GetComponentInParent<Pistola>() != null;
    }

    /// <summary>Ordena los impactos del más cercano al más lejano (inserción, sin lambdas).</summary>
    public static void OrdenarPorDistancia(RaycastHit[] impactos)
    {
        for (int i = 1; i < impactos.Length; i++)
        {
            RaycastHit actual = impactos[i];
            int j = i - 1;
            while (j >= 0 && impactos[j].distance > actual.distance)
            {
                impactos[j + 1] = impactos[j];
                j--;
            }
            impactos[j + 1] = actual;
        }
    }
}

/// <summary>
/// Proyectil de energía disparado por Pistola.
/// Desaparece al exceder su distancia máxima o al chocar con cualquier sólido,
/// aplicando daño si impacta a un EnemyBase.
/// </summary>
public class ProyectilPistola : MonoBehaviour
{
    private Vector3 direccion = Vector3.forward;
    private float velocidad = 38f;
    private float distanciaMaxima = 45f;
    private float radio = 0.12f;
    private int dano = 1;
    private float distanciaRecorrida;

    public void Inicializar(Vector3 direccionInicial, float velocidadInicial, float maximaDistancia, int cantidadDano)
    {
        direccion = direccionInicial.normalized;
        velocidad = velocidadInicial;
        distanciaMaxima = maximaDistancia;
        dano = cantidadDano;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Pulso";
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = new Vector3(0.14f, 0.14f, 0.42f);
        Object.Destroy(visual.GetComponent<Collider>());

        MeshRenderer malla = visual.GetComponent<MeshRenderer>();
        if (malla != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader);
            Color cian = new Color(0.2f, 0.9f, 1f);
            material.color = cian;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", cian);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", cian * 4f);
            malla.sharedMaterial = material;
        }
    }

    private void Update()
    {
        float paso = velocidad * Time.deltaTime;
        if (paso <= 0f) return;

        RaycastHit[] impactos = Physics.SphereCastAll(transform.position, radio, direccion, paso, ~0, QueryTriggerInteraction.Ignore);
        Pistola.OrdenarPorDistancia(impactos);

        for (int i = 0; i < impactos.Length; i++)
        {
            Collider c = impactos[i].collider;
            if (c == null) continue;
            if (c.CompareTag("Player") || c.GetComponentInParent<PlayerController>() != null) continue;
            if (c.GetComponentInParent<Pistola>() != null || c.GetComponentInParent<ProyectilPistola>() != null) continue;

            EnemyBase enemigo = c.GetComponentInParent<EnemyBase>();
            if (enemigo != null) enemigo.RecibirDaño(dano, DamageType.Normal);

            Destroy(gameObject);
            return;
        }

        transform.position += direccion * paso;
        distanciaRecorrida += paso;
        if (distanciaRecorrida >= distanciaMaxima) Destroy(gameObject);
    }
}
