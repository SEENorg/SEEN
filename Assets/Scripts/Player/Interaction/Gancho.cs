using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Gancho de agarre para el Nivel 4 con movimiento pendular estilo Spider-Man.
/// Al recogerlo (con E / Y o al acercarse) se equipa en primera persona y activa
/// la persecución del Antivirus. Permite columpiarse desde los nodos superiores
/// (AnclajeGancho) o superficies elevadas manteniendo Clic Izquierdo / Derecho / RT.
/// </summary>
public class Gancho : MonoBehaviour, IInteractuable
{
    [Header("Alcance y columpio")]
    [SerializeField] private float alcanceMaximo = 54f;
    [SerializeField] private float anguloCono = 36f;
    [SerializeField] private float longitudMinima = 3.5f;
    [SerializeField] private float retrasoAntesDeAcortar = 0.75f;
    [SerializeField] private float velocidadRecogida = 2.4f;
    [SerializeField] private float aceleracionTangencial = 34f;
    [SerializeField] private float rapidezMaxima = 22f;

    private readonly Color colorCian = new Color(0.15f, 0.9f, 1f);
    private readonly Vector3 posicionEnCamara = new Vector3(0.32f, -0.24f, 0.52f);

    private bool equipada;
    private bool columpiando;
    private bool puntoObjetivoValido;
    private Vector3 objetivoActual;
    private Vector3 puntoAnclaje;
    private float longitudCuerda;
    private float tiempoColgando;
    private float tiempoHabilitado;
    private bool pendienteSoltar;
    private bool pendienteSalto;

    private PlayerController jugador;
    private Rigidbody rbJugador;
    private IPlayerInput entrada;
    private Collider[] collidersPropios;
    private TextMesh etiqueta;

    private Transform puntoDisparo;
    private Transform[] garras;
    private LineRenderer lineaCuerda;
    private Material matOscuro;
    private Material matEnergia;
    private Material matCuerda;
    private Texture2D texturaReticle;
    private readonly List<Renderer> renderersModelo = new List<Renderer>();

    public string TextoPrompt
    {
        get { return "Presione E para recoger gancho"; }
    }

    public bool PuedeInteractuar
    {
        get { return !equipada; }
    }

    public bool EstaEquipado
    {
        get { return equipada; }
    }

    public void Interactuar()
    {
        if (!equipada) Equipar();
    }

    private void Start()
    {
        jugador = Object.FindFirstObjectByType<PlayerController>();
        if (jugador != null)
        {
            entrada = jugador.Entrada;
            rbJugador = jugador.GetComponent<Rigidbody>();
        }
        else
        {
            Debug.LogError("[Gancho] No se encontró PlayerController en la escena.");
        }

        collidersPropios = GetComponentsInChildren<Collider>(true);
        texturaReticle = CrearTexturaAnillo(64, 7);
        ConstruirModelo();
        ConstruirLinea();
        CrearEtiqueta();
    }

    private void OnDestroy()
    {
        CancelarColumpio();
        if (matOscuro != null) Destroy(matOscuro);
        if (matEnergia != null) Destroy(matEnergia);
        if (matCuerda != null) Destroy(matCuerda);
        if (texturaReticle != null) Destroy(texturaReticle);
    }

    private void ConstruirModelo()
    {
        matOscuro = CrearMaterial(new Color(0.10f, 0.12f, 0.16f), Color.black, 0f);
        matEnergia = CrearMaterial(new Color(0.05f, 0.30f, 0.42f), colorCian, 2.6f);

        CrearParte("Chasis", PrimitiveType.Cube, new Vector3(0f, 0.01f, 0f), new Vector3(0.095f, 0.105f, 0.42f), matOscuro, transform);
        CrearParte("Tambor", PrimitiveType.Cylinder, new Vector3(0f, 0.045f, 0.06f), new Vector3(0.09f, 0.09f, 0.09f), matOscuro, transform, Quaternion.Euler(90f, 0f, 0f));
        CrearParte("BobinaCian", PrimitiveType.Cube, new Vector3(0f, 0.078f, 0.04f), new Vector3(0.045f, 0.024f, 0.36f), matEnergia, transform);
        CrearParte("Empunadura", PrimitiveType.Cube, new Vector3(0f, -0.095f, -0.10f), new Vector3(0.072f, 0.17f, 0.095f), matOscuro, transform, Quaternion.Euler(14f, 0f, 0f));

        garras = new Transform[3];
        float[] angulos = { 0f, 120f, 240f };
        for (int i = 0; i < 3; i++)
        {
            GameObject pivote = new GameObject("GarraPivote_" + i);
            pivote.transform.SetParent(transform, false);
            pivote.transform.localPosition = new Vector3(0f, 0.03f, 0.22f);
            pivote.transform.localRotation = Quaternion.Euler(0f, 0f, angulos[i]);
            garras[i] = pivote.transform;

            GameObject pua = CrearParte("Garra_" + i, PrimitiveType.Cube, new Vector3(0f, 0.042f, 0.07f), new Vector3(0.018f, 0.018f, 0.14f), matEnergia, pivote.transform);
            pua.transform.localRotation = Quaternion.Euler(-16f, 0f, 0f);
        }

        GameObject punta = new GameObject("PuntoDisparo");
        punta.transform.SetParent(transform, false);
        punta.transform.localPosition = new Vector3(0f, 0.035f, 0.34f);
        puntoDisparo = punta.transform;
    }

    private void ConstruirLinea()
    {
        matCuerda = CrearMaterial(colorCian, colorCian, 3.2f);
        lineaCuerda = gameObject.AddComponent<LineRenderer>();
        lineaCuerda.positionCount = 2;
        lineaCuerda.startWidth = 0.055f;
        lineaCuerda.endWidth = 0.035f;
        lineaCuerda.useWorldSpace = true;
        lineaCuerda.sharedMaterial = matCuerda;
        lineaCuerda.enabled = false;
    }

    private GameObject CrearParte(string nombre, PrimitiveType tipo, Vector3 pos, Vector3 escala, Material mat, Transform padre, Quaternion? rot = null)
    {
        GameObject go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        if (rot.HasValue) go.transform.localRotation = rot.Value;
        Object.Destroy(go.GetComponent<Collider>());
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sharedMaterial = mat;
            renderersModelo.Add(mr);
        }
        return go;
    }

    private static Material CrearMaterial(Color baseCol, Color emision, float intensidad)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(sh);
        mat.color = baseCol;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseCol);
        if (intensidad > 0f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emision * intensidad);
        }
        return mat;
    }

    private void CrearEtiqueta()
    {
        GameObject go = new GameObject("PromptGancho");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        etiqueta = go.AddComponent<TextMesh>();
        etiqueta.text = TextoPrompt;
        etiqueta.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        etiqueta.fontSize = 34;
        etiqueta.characterSize = 0.04f;
        etiqueta.anchor = TextAnchor.MiddleCenter;
        etiqueta.alignment = TextAlignment.Center;
        etiqueta.color = colorCian;
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr == null) mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = etiqueta.font.material;
    }

    private void Equipar()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        equipada = true;
        tiempoHabilitado = Time.unscaledTime + 0.2f;

        if (etiqueta != null) etiqueta.gameObject.SetActive(false);
        if (collidersPropios != null)
        {
            for (int i = 0; i < collidersPropios.Length; i++)
                if (collidersPropios[i] != null) collidersPropios[i].enabled = false;
        }

        transform.SetParent(cam.transform, false);
        transform.localPosition = posicionEnCamara;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        Antivirus av = Object.FindFirstObjectByType<Antivirus>(FindObjectsInactive.Exclude);
        if (av != null) av.ActivarPersecucion();

        GameFeedback.Mensaje("GANCHO EQUIPADO — Mantén Clic Izquierdo o RT hacia los nodos superiores para columpiarte y escapar del Antivirus");
    }

    private void Update()
    {
        if (!equipada)
        {
            AnimarEnPedestal();
            RevisarRecogidaCercana();
            return;
        }

        if (Time.timeScale <= 0f || HackManager.Abierto || PauseMenu.Pausado || CipherWheel.Abierta ||
            GameFeedback.NivelCompletadoVisible || GameFeedback.GameOverVisible || PantallaControles.Visible)
        {
            if (columpiando) CancelarColumpio();
            puntoObjetivoValido = false;
            return;
        }

        AnimarPosePrimeraPersona();

        if (!columpiando)
        {
            puntoObjetivoValido = BuscarMejorPuntoAnclaje(out objetivoActual);
            if (Time.unscaledTime >= tiempoHabilitado && LeerDisparoPresionado() && puntoObjetivoValido)
                IniciarColumpio(objetivoActual);
        }
        else
        {
            if (LeerSaltoDirecto())
            {
                pendienteSoltar = true;
                pendienteSalto = true;
            }
            else if (!LeerDisparoMantenido())
            {
                pendienteSoltar = true;
                pendienteSalto = false;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!equipada || !columpiando || rbJugador == null) return;

        if (pendienteSoltar)
        {
            bool conSalto = pendienteSalto;
            pendienteSoltar = false;
            pendienteSalto = false;
            SoltarColumpio(conSalto);
            return;
        }

        Vector3 radial = rbJugador.position - puntoAnclaje;
        float dist = radial.magnitude;
        Vector3 dirRadial = radial / Mathf.Max(0.001f, dist);

        bool estaColgando = rbJugador.position.y < puntoAnclaje.y - 1.2f && dist >= longitudCuerda - 0.35f;
        if (estaColgando)
        {
            tiempoColgando += Time.fixedDeltaTime;
            if (tiempoColgando >= retrasoAntesDeAcortar)
                longitudCuerda = Mathf.MoveTowards(longitudCuerda, longitudMinima, velocidadRecogida * Time.fixedDeltaTime);
        }
        else
        {
            tiempoColgando = 0f;
        }

        if (dist < longitudCuerda)
        {
            longitudCuerda = Mathf.Max(longitudMinima, dist);
        }
        else
        {
            float rapidezPrevia = rbJugador.linearVelocity.magnitude;
            float velRadial = Vector3.Dot(rbJugador.linearVelocity, dirRadial);
            if (velRadial > 0f)
            {
                Vector3 velTangencial = rbJugador.linearVelocity - dirRadial * velRadial;
                if (velTangencial.sqrMagnitude > 0.0001f)
                    rbJugador.linearVelocity = velTangencial.normalized * rapidezPrevia;
                else
                    rbJugador.linearVelocity = velTangencial;
            }

            if (dist > longitudCuerda)
                rbJugador.position = puntoAnclaje + dirRadial * longitudCuerda;
        }

        Camera cam = Camera.main;
        Vector3 camFwd = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up) : jugador.transform.forward;
        Vector3 camRight = cam != null ? Vector3.ProjectOnPlane(cam.transform.right, Vector3.up) : jugador.transform.right;
        if (camFwd.sqrMagnitude > 0.001f) camFwd.Normalize();
        if (camRight.sqrMagnitude > 0.001f) camRight.Normalize();

        Vector2 mov = entrada != null ? entrada.LeerMovimiento() : Vector2.zero;
        Vector3 dirDeseada = camFwd * mov.y + camRight * mov.x;
        if (dirDeseada.sqrMagnitude > 0.001f)
        {
            dirDeseada.Normalize();
            float velEnDireccion = Vector3.Dot(rbJugador.linearVelocity, dirDeseada);
            if (velEnDireccion < rapidezMaxima)
            {
                float factor = Mathf.Clamp01((rapidezMaxima - velEnDireccion) / Mathf.Max(1f, rapidezMaxima * 0.35f));
                rbJugador.AddForce(dirDeseada * (aceleracionTangencial * factor), ForceMode.Acceleration);
            }
        }
    }

    private void LateUpdate()
    {
        if (lineaCuerda == null) return;
        if (!equipada || !columpiando || puntoDisparo == null)
        {
            if (lineaCuerda.enabled) lineaCuerda.enabled = false;
            return;
        }

        lineaCuerda.enabled = true;
        lineaCuerda.SetPosition(0, puntoDisparo.position);
        lineaCuerda.SetPosition(1, puntoAnclaje);
    }

    private void OnGUI()
    {
        if (!equipada || texturaReticle == null || HackManager.Abierto || EstadoMenus.HayMenuAbierto) return;
        if (!columpiando && !puntoObjetivoValido) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 objetivoMundo = columpiando ? puntoAnclaje : objetivoActual;
        Vector3 pantalla = cam.WorldToScreenPoint(objetivoMundo);
        if (pantalla.z <= 0f) return;

        float tamano = columpiando ? 34f : 26f;
        float x = pantalla.x - tamano * 0.5f;
        float y = (Screen.height - pantalla.y) - tamano * 0.5f;

        Color previa = GUI.color;
        GUI.color = columpiando ? new Color(0.25f, 1f, 0.6f, 0.95f) : new Color(0.15f, 0.9f, 1f, 0.92f);
        GUI.DrawTexture(new Rect(x, y, tamano, tamano), texturaReticle);
        GUI.color = previa;
    }

    private void AnimarEnPedestal()
    {
        transform.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World);
        Camera cam = Camera.main;
        if (etiqueta != null && cam != null)
            etiqueta.transform.rotation = Quaternion.LookRotation(etiqueta.transform.position - cam.transform.position);
    }

    private void RevisarRecogidaCercana()
    {
        if (jugador == null || Time.timeScale <= 0f) return;
        if (Vector3.Distance(jugador.transform.position, transform.position) < 1.65f)
            Equipar();
    }

    private void AnimarPosePrimeraPersona()
    {
        float objetivoPitch = columpiando ? -10f : 0f;
        Quaternion rotObj = Quaternion.Euler(objetivoPitch, 0f, 0f);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, rotObj, Time.deltaTime * 12f);

        Vector3 posObj = columpiando ? posicionEnCamara + new Vector3(0f, 0.03f, 0.05f) : posicionEnCamara;
        transform.localPosition = Vector3.Lerp(transform.localPosition, posObj, Time.deltaTime * 12f);

        if (garras != null)
        {
            float apertura = columpiando ? -0.025f : 0f;
            for (int i = 0; i < garras.Length; i++)
            {
                if (garras[i] != null)
                    garras[i].localPosition = Vector3.Lerp(garras[i].localPosition, new Vector3(0f, 0.03f, 0.22f + apertura), Time.deltaTime * 15f);
            }
        }
    }

    private bool BuscarMejorPuntoAnclaje(out Vector3 punto)
    {
        punto = Vector3.zero;
        Camera cam = Camera.main;
        if (cam == null || jugador == null) return false;

        Vector3 origen = cam.transform.position;
        Vector3 adelante = cam.transform.forward;
        float mejorPuntuacion = float.MaxValue;
        bool encontrado = false;

        for (int i = 0; i < AnclajeGancho.Todos.Count; i++)
        {
            AnclajeGancho anclaje = AnclajeGancho.Todos[i];
            if (anclaje == null || !anclaje.isActiveAndEnabled) continue;

            Vector3 pos = anclaje.PuntoEnganche;
            Vector3 delta = pos - origen;
            float dist = delta.magnitude;
            if (dist < 2.2f || dist > alcanceMaximo) continue;

            Vector3 dir = delta / dist;
            float angulo = Vector3.Angle(adelante, dir);
            if (angulo > anguloCono) continue;

            if (Physics.Linecast(origen + dir * 0.4f, pos, out RaycastHit bloque, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!EsDelJugador(bloque.collider) && Vector3.Distance(bloque.point, pos) > 2.5f)
                    continue;
            }

            float bonusAlto = pos.y > jugador.transform.position.y + 1.2f ? -6f : 12f;
            float puntuacion = angulo + dist * 0.22f + bonusAlto;
            if (puntuacion < mejorPuntuacion)
            {
                mejorPuntuacion = puntuacion;
                punto = pos;
                encontrado = true;
            }
        }

        if (encontrado) return true;

        RaycastHit[] hits = Physics.SphereCastAll(origen + adelante * 0.5f, 0.5f, adelante, alcanceMaximo, ~0, QueryTriggerInteraction.Ignore);
        Pistola.OrdenarPorDistancia(hits);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i].collider;
            if (c == null || EsDelJugador(c)) continue;
            if (hits[i].distance < 2.2f) continue;

            if (hits[i].point.y > jugador.transform.position.y + 1.5f)
            {
                punto = hits[i].point;
                return true;
            }
            break;
        }

        return false;
    }

    private void IniciarColumpio(Vector3 punto)
    {
        if (rbJugador == null || jugador == null) return;

        columpiando = true;
        pendienteSoltar = false;
        pendienteSalto = false;
        puntoAnclaje = punto;
        jugador.EstaColumpiando = true;

        float distActual = Vector3.Distance(rbJugador.position, puntoAnclaje);
        longitudCuerda = Mathf.Max(1.5f, distActual);
        tiempoColgando = 0f;

        Vector3 v = rbJugador.linearVelocity;
        float rapidez = v.magnitude;
        if (rapidez > 0.001f)
        {
            Vector3 radial = rbJugador.position - puntoAnclaje;
            float dist = radial.magnitude;
            if (dist > 0.001f)
            {
                Vector3 dirRadial = radial / dist;
                float velRadial = Vector3.Dot(v, dirRadial);
                if (velRadial > 0f)
                {
                    Vector3 vTangencial = v - dirRadial * velRadial;
                    if (vTangencial.sqrMagnitude > 0.0001f)
                        rbJugador.linearVelocity = vTangencial.normalized * rapidez;
                }
            }
        }
    }

    private void SoltarColumpio(bool conImpulsoSalto)
    {
        if (!columpiando) return;
        columpiando = false;
        tiempoColgando = 0f;
        if (lineaCuerda != null) lineaCuerda.enabled = false;
        if (jugador != null)
        {
            jugador.EstaColumpiando = false;
            jugador.RestaurarSaltoAereo();
        }

        if (rbJugador != null)
        {
            Vector3 v = rbJugador.linearVelocity;
            float rapidezInicial = v.magnitude;

            if (conImpulsoSalto)
            {
                Camera cam = Camera.main;
                Vector3 fwd = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up) : jugador.transform.forward;
                if (fwd.sqrMagnitude > 0.001f)
                {
                    fwd.Normalize();
                    if (Vector3.Dot(v, fwd) >= 0f)
                        v += fwd * 2.5f;
                }
                v.y = Mathf.Max(v.y + 4.0f, 7.2f);
            }
            else if (rapidezInicial > 0.001f && v.y > -2f)
            {
                v.y += 1.2f;
            }

            if (v.magnitude < rapidezInicial && v.sqrMagnitude > 0.0001f)
                v = v.normalized * rapidezInicial;

            rbJugador.linearVelocity = v;
        }
    }

    public void CancelarColumpio()
    {
        columpiando = false;
        tiempoColgando = 0f;
        pendienteSoltar = false;
        pendienteSalto = false;
        if (lineaCuerda != null) lineaCuerda.enabled = false;
        if (jugador != null) jugador.EstaColumpiando = false;
    }

    private bool LeerDisparoPresionado()
    {
        if (entrada != null && entrada.AtaquePresionado()) return true;
#if ENABLE_INPUT_SYSTEM
        Mouse raton = Mouse.current;
        if (raton != null && (raton.leftButton.wasPressedThisFrame || raton.rightButton.wasPressedThisFrame))
            return true;
        Gamepad mando = Gamepad.current;
        if (mando != null && mando.rightTrigger.wasPressedThisFrame)
            return true;
#else
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) return true;
#endif
        return false;
    }

    private bool LeerDisparoMantenido()
    {
        if (entrada != null && entrada.AtaqueMantenido()) return true;
#if ENABLE_INPUT_SYSTEM
        Mouse raton = Mouse.current;
        if (raton != null && (raton.leftButton.isPressed || raton.rightButton.isPressed))
            return true;
        Gamepad mando = Gamepad.current;
        if (mando != null && mando.rightTrigger.isPressed)
            return true;
#else
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1)) return true;
#endif
        return false;
    }

    private bool LeerSaltoDirecto()
    {
        if (entrada != null && entrada.SaltoPresionado()) return true;
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;
        Gamepad mando = Gamepad.current;
        if (mando != null && mando.buttonSouth.wasPressedThisFrame) return true;
#else
        if (Input.GetKeyDown(KeyCode.Space)) return true;
#endif
        return false;
    }

    private static bool EsDelJugador(Collider c)
    {
        if (c == null) return false;
        if (c.CompareTag("Player")) return true;
        if (c.GetComponentInParent<PlayerController>() != null) return true;
        return c.GetComponentInParent<Gancho>() != null;
    }

    private static Texture2D CrearTexturaAnillo(int tamano, int grosor)
    {
        Texture2D tex = new Texture2D(tamano, tamano, TextureFormat.RGBA32, false);
        float centro = (tamano - 1) * 0.5f;
        float rExt = centro - 1f;
        float rInt = rExt - grosor;

        for (int y = 0; y < tamano; y++)
        {
            for (int x = 0; x < tamano; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(centro, centro));
                float a = 0f;
                if (d >= rInt && d <= rExt)
                    a = Mathf.Clamp01(1f - Mathf.Abs(d - (rInt + grosor * 0.5f)) / (grosor * 0.5f + 0.5f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return tex;
    }
}
