using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Pistola interactuable para el Nivel 2: aparece sobre un pedestal en el salón previo.
/// Al recogerla (con E o al acercarse) se equipa en primera persona y dispara proyectiles
/// de energía que desaparecen al recorrer mucha distancia o chocar contra cualquier objeto,
/// reduciendo la vida de los enemigos (EnemyBase).
/// </summary>
public class Pistola : MonoBehaviour, IInteractuable
{
    [Header("Disparo")]
    [SerializeField] private float cooldownDisparo = 0.22f;
    [SerializeField] private float velocidadProyectil = 38f;
    [SerializeField] private float alcanceMaximo = 45f;
    [SerializeField] private int danoPorDisparo = 1;

    private bool equipada;
    private float tiempoProximoDisparo;
    private Transform puntoDisparo;
    private TextMesh etiqueta;
    private Collider[] collidersPropios;
    private Vector3 posicionEnCamara = new Vector3(0.32f, -0.25f, 0.55f);
    private float retrocesoActual;
    private InputAction attackAction;

    public string TextoPrompt
    {
        get { return "Presione E para recoger pistola"; }
    }

    public bool PuedeInteractuar
    {
        get { return !equipada; }
    }

    private void Start()
    {
        PlayerInput playerInput = Object.FindFirstObjectByType<PlayerInput>();

if (playerInput != null)
{
    attackAction = playerInput.actions["Attack"];
}
else
{
    Debug.LogError("No se encontró PlayerInput para la acción Attack.");
}
        collidersPropios = GetComponentsInChildren<Collider>(true);
        ConstruirModelo();

        var etiquetaGO = new GameObject("PromptPistola");
        etiquetaGO.transform.SetParent(transform, false);
        etiquetaGO.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        etiqueta = etiquetaGO.AddComponent<TextMesh>();
        etiqueta.text = TextoPrompt;
        etiqueta.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        etiqueta.fontSize = 34;
        etiqueta.characterSize = 0.04f;
        etiqueta.anchor = TextAnchor.MiddleCenter;
        etiqueta.alignment = TextAlignment.Center;
        etiqueta.color = new Color(0.15f, 0.85f, 1f);
        etiquetaGO.AddComponent<MeshRenderer>();
    }

    private void ConstruirModelo()
    {
        Material matOscuro = CrearMaterial(new Color(0.11f, 0.13f, 0.17f), Color.black, 0f);
        Material matCian = CrearMaterial(new Color(0.05f, 0.3f, 0.4f), new Color(0.15f, 0.85f, 1f), 2.5f);

        // Cuerpo principal
        CrearParte("Cuerpo", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(0.09f, 0.11f, 0.42f), matOscuro);
        // Cañón superior
        CrearParte("Canon", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0.24f), new Vector3(0.06f, 0.06f, 0.22f), matOscuro);
        // Riel de energía cian
        CrearParte("RielEnergia", PrimitiveType.Cube, new Vector3(0f, 0.085f, 0.06f), new Vector3(0.04f, 0.025f, 0.44f), matCian);
        // Empuñadura
        var emp = CrearParte("Empunadura", PrimitiveType.Cube, new Vector3(0f, -0.10f, -0.11f), new Vector3(0.075f, 0.18f, 0.10f), matOscuro);
        emp.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);

        var boca = new GameObject("PuntoDisparo");
        boca.transform.SetParent(transform, false);
        boca.transform.localPosition = new Vector3(0f, 0.055f, 0.38f);
        puntoDisparo = boca.transform;
    }

    private GameObject CrearParte(string nombre, PrimitiveType tipo, Vector3 pos, Vector3 escala, Material mat)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        Object.Destroy(go.GetComponent<Collider>());
        var r = go.GetComponent<MeshRenderer>();
        if (r != null) r.sharedMaterial = mat;
        return go;
    }

    private static Material CrearMaterial(Color baseCol, Color emision, float intensidad)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material m = new Material(sh);
        m.color = baseCol;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseCol);
        if (intensidad > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emision * intensidad);
        }
        return m;
    }

    public void Interactuar()
    {
        if (!equipada)
            Equipar();
    }

    private void Equipar()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        equipada = true;
        tiempoProximoDisparo = Time.unscaledTime + 0.25f;

        if (etiqueta != null)
            etiqueta.gameObject.SetActive(false);

        if (collidersPropios != null)
        {
            for (int i = 0; i < collidersPropios.Length; i++)
                if (collidersPropios[i] != null) collidersPropios[i].enabled = false;
        }

        transform.SetParent(cam.transform, false);
        transform.localPosition = posicionEnCamara;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        GameFeedback.Mensaje("Pistola equipada — Clic izquierdo para disparar");
    }

    private void Update()
    {
        if (!equipada)
        {
            transform.Rotate(0f, 55f * Time.deltaTime, 0f, Space.World);

            Camera cam = Camera.main;
            if (etiqueta != null && cam != null)
                etiqueta.transform.rotation = Quaternion.LookRotation(etiqueta.transform.position - cam.transform.position);

            // Recoger también al acercarse
            PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
            if (pc != null && Vector3.Distance(pc.transform.position, transform.position) <= 1.35f)
                Equipar();

            return;
        }

        // Retroceso suave en primera persona
        retrocesoActual = Mathf.Lerp(retrocesoActual, 0f, Time.deltaTime * 14f);
        transform.localPosition = posicionEnCamara + new Vector3(0f, 0f, -retrocesoActual);

        if (Time.timeScale <= 0f || HackManager.Abierto || PauseMenu.Pausado ||
            CipherWheel.Abierta || GameFeedback.NivelCompletadoVisible || GameFeedback.GameOverVisible)
            return;

        if (LeerDisparoPresionado() && Time.unscaledTime >= tiempoProximoDisparo)
        {
            tiempoProximoDisparo = Time.unscaledTime + cooldownDisparo;
            Disparar();
        }
    }

    private void Disparar()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        retrocesoActual = 0.08f;

        Vector3 origenCam = cam.transform.position;
        Vector3 dirCam = cam.transform.forward;
        Vector3 puntoObjetivo = origenCam + dirCam * alcanceMaximo;
        float distPrimerImpacto = alcanceMaximo;

        RaycastHit[] hits = Physics.RaycastAll(origenCam, dirCam, alcanceMaximo, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i].collider;
            if (c == null || EsDelJugador(c)) continue;
            puntoObjetivo = hits[i].point;
            distPrimerImpacto = hits[i].distance;
            break;
        }

        Vector3 origenBoca = puntoDisparo != null ? puntoDisparo.position : origenCam + dirCam * 0.5f;
        float distCamABoca = Vector3.Distance(origenCam, origenBoca);

        // Si el objetivo está muy cerca (a quemarropa), salir desde la cámara para no traspasarlo
        Vector3 origenProyectil = (distPrimerImpacto < distCamABoca + 0.25f)
            ? origenCam + dirCam * 0.15f
            : origenBoca;

        Vector3 dirProyectil = (puntoObjetivo - origenProyectil).normalized;
        if (dirProyectil.sqrMagnitude < 0.001f) dirProyectil = dirCam;

        var proyGO = new GameObject("ProyectilPistola");
        proyGO.transform.position = origenProyectil;
        proyGO.transform.rotation = Quaternion.LookRotation(dirProyectil);

        var proy = proyGO.AddComponent<ProyectilPistola>();
        proy.Inicializar(dirProyectil, velocidadProyectil, alcanceMaximo, danoPorDisparo);
    }

    private static bool EsDelJugador(Collider c)
    {
        return c.CompareTag("Player")
            || c.GetComponentInParent<PlayerController>() != null
            || c.GetComponentInParent<Pistola>() != null;
    }

    private bool LeerDisparoPresionado()
{
    return attackAction != null && attackAction.WasPressedThisFrame();
}
}

/// <summary>
/// Proyectil de energía disparado por Pistola.
/// Desaparece al exceder su distancia máxima o al colisionar con cualquier sólido,
/// aplicando daño si impacta a un EnemyBase.
/// </summary>
public class ProyectilPistola : MonoBehaviour
{
    private Vector3 direccion = Vector3.forward;
    private float velocidad = 38f;
    private float distanciaMaxima = 45f;
    private float radio = 0.12f;
    private int daño = 1;
    private float distanciaRecorrida;

    public void Inicializar(Vector3 dir, float vel, float maxDist, int cantDaño)
    {
        direccion = dir.normalized;
        velocidad = vel;
        distanciaMaxima = maxDist;
        daño = cantDaño;

        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Pulso";
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = new Vector3(0.14f, 0.14f, 0.42f);
        Object.Destroy(visual.GetComponent<Collider>());

        var rend = visual.GetComponent<MeshRenderer>();
        if (rend != null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            Material mat = new Material(sh);
            Color cian = new Color(0.2f, 0.9f, 1f);
            mat.color = cian;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", cian);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", cian * 4f);
            rend.sharedMaterial = mat;
        }
    }

    private void Update()
    {
        float paso = velocidad * Time.deltaTime;
        if (paso <= 0f) return;

        RaycastHit[] hits = Physics.SphereCastAll(transform.position, radio, direccion, paso, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i].collider;
            if (c == null) continue;
            if (c.CompareTag("Player") || c.GetComponentInParent<PlayerController>() != null) continue;
            if (c.GetComponentInParent<Pistola>() != null || c.GetComponentInParent<ProyectilPistola>() != null) continue;

            EnemyBase enemigo = c.GetComponentInParent<EnemyBase>();
            if (enemigo != null)
                enemigo.RecibirDaño(daño, DamageType.Normal);

            Destroy(gameObject);
            return;
        }

        transform.position += direccion * paso;
        distanciaRecorrida += paso;

        if (distanciaRecorrida >= distanciaMaxima)
            Destroy(gameObject);
    }
}
