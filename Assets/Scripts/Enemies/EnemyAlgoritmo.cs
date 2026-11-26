using UnityEngine;

/// <summary>
/// ALGORITMO: flota con un escudo orbital de cubos protectores.
/// - Con escudo activo, los disparos normales rebotan.
/// - Disparo sobrecargado rompe el escudo de inmediato y lo aturde.
/// - También se vuelve vulnerable periódicamente en su ventana expuesta.
/// - Lanza orbes de datos hacia el jugador que se pueden esquivar o destruir de un tiro.
/// </summary>
public class EnemyAlgoritmo : EnemyBase
{
    [Header("Protección")]
    [SerializeField] private GameObject cubosProtectores;
    [SerializeField] private float tiempoProtegido = 4f;
    [SerializeField] private float tiempoExpuesto = 2.5f;

    [Header("Movimiento Flotante")]
    [SerializeField] private float velocidadVuelo = 2f;
    [SerializeField] private float radioVuelo = 4f;
    [SerializeField] private float alturaVuelo = 1.5f;
    [Tooltip("Velocidad de rotación sobre su propio eje")]
    [SerializeField] private float velocidadRotacion = 50f;

    [Header("Ataque con Orbes")]
    [SerializeField] private float intervaloDisparo = 2.4f;
    [SerializeField] private float alcanceDisparo = 18f;
    [SerializeField] private float velocidadOrbe = 8.5f;

    private bool expuesto;
    private float temporizador;
    private float temporizadorDisparo = 1.2f;
    private Vector3 posicionInicial;
    private Vector3 destinoActual;

    public bool Expuesto { get { return expuesto; } }

    protected override void Start()
    {
        base.Start();
        posicionInicial = transform.position;
        temporizador = 0f;
        expuesto = false;
        ElegirNuevoDestino();
        ActualizarCubos();
    }

    public override void ReiniciarPosicion()
    {
        expuesto = false;
        temporizador = 0f;
        temporizadorDisparo = 1.2f;
        ActualizarCubos();
        base.ReiniciarPosicion();
    }

    protected override void Update()
    {
        base.Update();
        if (muerto || !PuedeMoverse) return;

        // 1. Rotación constante sobre su propio eje
        transform.Rotate(Vector3.up * velocidadRotacion * Time.deltaTime, Space.Self);

        // 2. Movimiento suave hacia el destino, sin atravesar paredes
        bool avanzo = MoverHacia(destinoActual, velocidadVuelo);

        if (!avanzo || Vector3.Distance(transform.position, destinoActual) < 0.1f)
        {
            ElegirNuevoDestino();
        }

        // 3. Ciclo de protección
        temporizador += Time.deltaTime;
        if (!expuesto && temporizador >= tiempoProtegido)
        {
            expuesto = true;
            temporizador = 0f;
            ActualizarCubos();
        }
        else if (expuesto && temporizador >= tiempoExpuesto)
        {
            expuesto = false;
            temporizador = 0f;
            ActualizarCubos();
        }

        // 4. Ataque con orbes destructibles
        if (jugador != null && Vector3.Distance(transform.position, jugador.position) <= alcanceDisparo)
        {
            temporizadorDisparo -= Time.deltaTime;
            if (temporizadorDisparo <= 0.45f)
            {
                estadoMecanico = "[DISPARANDO]";
            }
            if (temporizadorDisparo <= 0f)
            {
                temporizadorDisparo = intervaloDisparo;
                LanzarOrbe();
            }
        }
        else
        {
            ActualizarEstadoTexto();
        }
    }

    private void ActualizarEstadoTexto()
    {
        estadoMecanico = expuesto ? "[VULNERABLE]" : "[ESCUDO]";
    }

    private void LanzarOrbe()
    {
        if (jugador == null) return;
        Vector3 origen = ObtenerLimitesVisuales().center;
        Vector3 objetivo = jugador.position + Vector3.up * 0.9f;
        Vector3 dir = (objetivo - origen).normalized;

        GameObject go = new GameObject("OrbeAlgoritmo");
        go.transform.position = origen + dir * 0.9f;
        OrbeAlgoritmo orbe = go.AddComponent<OrbeAlgoritmo>();
        orbe.Inicializar(dir, velocidadOrbe);
    }

    private void ElegirNuevoDestino()
    {
        for (int intento = 0; intento < 8; intento++)
        {
            Vector2 circulo = Random.insideUnitCircle * radioVuelo;
            Vector3 candidato = posicionInicial + new Vector3(circulo.x, alturaVuelo, circulo.y);

            Vector3 haciaCandidato = candidato - transform.position;
            float distancia = haciaCandidato.magnitude;
            if (distancia < 0.5f) continue;

            if (!HayObstaculo(haciaCandidato / distancia, distancia))
            {
                destinoActual = candidato;
                return;
            }
        }

        destinoActual = transform.position;
    }

    private void ActualizarCubos()
    {
        if (cubosProtectores != null)
            cubosProtectores.SetActive(!expuesto);
        ActualizarEstadoTexto();
    }

    public override void RecibirDaño(int cantidad, DamageType tipoDaño)
    {
        if (muerto) return;

        if (!expuesto)
        {
            if (tipoDaño != DamageType.Cargado)
            {
                NotificarEscudoBloqueo();
                return;
            }

            // El disparo sobrecargado rompe el escudo orbital de inmediato
            expuesto = true;
            temporizador = 0f;
            ActualizarCubos();
            Aturdir(1.8f);
            base.RecibirDaño(1, tipoDaño);
            return;
        }

        base.RecibirDaño(cantidad, tipoDaño);
    }
}

/// <summary>Proyectil flotante disparado por EnemyAlgoritmo. El jugador puede esquivarlo o destruirlo disparándole.</summary>
public class OrbeAlgoritmo : MonoBehaviour
{
    private Vector3 direccion = Vector3.forward;
    private float velocidad = 8.5f;
    private float vida = 4.5f;
    private Transform jugador;
    private Material mat;

    public void Inicializar(Vector3 dir, float vel)
    {
        direccion = dir.normalized;
        velocidad = vel;

        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null) jugador = pc.transform;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "MallaOrbe";
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = Vector3.one * 0.55f;
        Collider visCol = visual.GetComponent<Collider>();
        if (visCol != null) Object.Destroy(visCol);

        SphereCollider sc = gameObject.AddComponent<SphereCollider>();
        sc.radius = 0.42f;
        sc.isTrigger = false;

        MeshRenderer mr = visual.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            mat = new Material(s);
            Color rojo = new Color(1f, 0.22f, 0.15f);
            mat.color = rojo;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", rojo);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", rojo * 3.5f);
            mr.sharedMaterial = mat;
        }
    }

    private void OnDestroy()
    {
        if (mat != null) Destroy(mat);
    }

    public void DestruirPorDisparo()
    {
        Destroy(gameObject);
    }

    private void Update()
    {
        vida -= Time.deltaTime;
        if (vida <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float paso = velocidad * Time.deltaTime;
        RaycastHit[] golpes = Physics.SphereCastAll(transform.position, 0.28f, direccion, paso, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < golpes.Length; i++)
        {
            Collider c = golpes[i].collider;
            if (c == null || c.transform.IsChildOf(transform)) continue;
            if (c.GetComponentInParent<EnemyBase>() != null || c.GetComponentInParent<OrbeAlgoritmo>() != null) continue;
            if (c.GetComponentInParent<ProyectilPistola>() != null) continue;

            if (c.CompareTag("Player") || c.GetComponentInParent<PlayerController>() != null)
            {
                PlayerKill.KillPlayer();
                Destroy(gameObject);
                return;
            }

            Destroy(gameObject);
            return;
        }

        transform.position += direccion * paso;

        if (jugador != null && Vector3.Distance(transform.position, jugador.position + Vector3.up * 0.9f) <= 0.78f)
        {
            PlayerKill.KillPlayer();
            Destroy(gameObject);
        }
    }
}