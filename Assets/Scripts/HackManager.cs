using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Interfaz de hackeo 2D (Canvas overlay). Abre una cadena de módulos;
/// al resolverlos todos devuelve el control al jugador y avisa al terminal.
/// Mientras está abierta el juego queda en pausa (timeScale 0) y el ratón libre.
/// </summary>
public class HackManager : MonoBehaviour
{
    public static HackManager Instancia { get; private set; }
    public static bool Abierto => Instancia != null && Instancia.activo;

    /// <summary>
    /// Dificultad base de los puzzles según el nivel:
    /// 0 = Nivel 1 (tutorial, fácil), 1 = Nivel 2, 2 = Nivel 3 (difícil).
    /// La escribe LevelInfo al cargar el nivel.
    /// </summary>
    public static int DificultadBase { get; set; }

    // Estado del puzzle actual
    private Canvas canvas;
    private GameObject panel;
    private Text tituloTexto;
    private Text moduloTexto;
    private Text feedbackTexto;
    private Transform contenido;

    // Instrucciones (tutorial)
    private GameObject panelInstrucciones;
    private Text textoInstrucciones;
    private Text textoTipoModulo;

    private HackableType[] modulos;
    private System.Action onCompletado;
    private int indiceModulo;
    private bool activo;
    private bool esTutorial;
    private bool finalizando;
    private float bloqueoInput;            // ignora clics un instante tras abrir un módulo
    private float tiempoTransicion;   // para avance entre módulos (tiempo real)

    // Colores
    private readonly Color colFondo = new Color(0.02f, 0.03f, 0.04f, 0.95f);
    private readonly Color colPanel = new Color(0.06f, 0.08f, 0.11f, 1f);
    private readonly Color colAcento = new Color(0.15f, 0.85f, 1f);
    private readonly Color colVerde = new Color(0.15f, 1f, 0.4f);
    private readonly Color colRojo = new Color(1f, 0.25f, 0.25f);
    private readonly Color colBoton = new Color(0.10f, 0.13f, 0.17f);

    private static Font _fuente;
    private static Font Fuente => _fuente ?? (_fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

    // ── API estática ─────────────────────────────────────────────

    /// <summary>Abre la interfaz de hackeo con la cadena de módulos dada.</summary>
    public static void Abrir(HackableType[] mods, string nombreAcceso, Action onResuelto, bool tutorial = false)
    {
        if (Instancia == null)
        {
            var go = new GameObject("HackManager");
            Instancia = go.AddComponent<HackManager>();
        }
        Instancia.Iniciar(mods, nombreAcceso, onResuelto, tutorial);
    }

    private static string textoPromptActual;
    private static float promptTimer;

    /// <summary>Muestra el prompt "Presione E para hackear" (pantalla).</summary>
    public static void MostrarPrompt(string texto)
    {
        if (Instancia == null)
        {
            var go = new GameObject("HackManager");
            Instancia = go.AddComponent<HackManager>();
        }
        textoPromptActual = texto;
        promptTimer = 2.5f;
    }

    private void Update()
    {
        if (bloqueoInput > 0f) bloqueoInput -= Time.unscaledDeltaTime;

        if (!activo) { if (promptTimer > 0f) promptTimer -= Time.unscaledDeltaTime; return; }

        // Si la tabla César está abierta, no procesamos Esc (lo usa la tabla)
        if (CipherWheel.Abierta) return;

        // Cerrar con Escape
        if (LeerEscape()) { Cerrar(); return; }

        // Avance entre módulos (tiempo real, el juego está pausado)
        if (tiempoTransicion > 0f)
        {
            tiempoTransicion -= Time.unscaledDeltaTime;
            if (tiempoTransicion <= 0f)
            {
                if (finalizando)
                {
                    finalizando = false;
                    var cb = onCompletado;
                    Cerrar();
                    cb?.Invoke();
                }
                else
                {
                    SiguienteModulo();
                }
            }
        }
    }

    // ── Prompt en pantalla (OnGUI, igual que el feedback del nivel) ──
    private void OnGUI()
    {
        if (activo || promptTimer <= 0f || string.IsNullOrEmpty(textoPromptActual)) return;

        var estilo = new GUIStyle(GUI.skin.box)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        estilo.normal.textColor = new Color(0.3f, 1f, 0.45f);

        float w = 420f, h = 46f;
        GUI.Box(new Rect((Screen.width - w) / 2f, Screen.height - h - 40f, w, h),
                textoPromptActual, estilo);
    }

    // ── Ciclo de vida ────────────────────────────────────────────

    private void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(this); return; }
        Instancia = this;
        AsegurarEventSystem();
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
        // Restaurar el juego por si se destruye estando abierto
        Time.timeScale = 1f;
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.enabled = true;
    }

    private void Iniciar(HackableType[] mods, string nombreAcceso, Action onResuelto, bool tutorial = false)
    {
        if (activo) return;

        modulos = (mods != null && mods.Length > 0) ? mods : new[] { HackableType.DescifrarCodigo };
        onCompletado = onResuelto;
        esTutorial = tutorial;
        indiceModulo = 0;
        tiempoTransicion = 0f;
        finalizando = false;

        // Pausa y ratón libre
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.enabled = false;

        ConstruirCanvas(nombreAcceso);
        activo = true;
        ConstruirModuloActual();
    }

    private void Cerrar()
    {
        activo = false;
        if (canvas != null) canvas.gameObject.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.enabled = true;
    }

    private void SiguienteModulo()
    {
        indiceModulo++;
        if (indiceModulo >= modulos.Length)
        {
            // ¡Todos los módulos resueltos! Mostramos el mensaje y cerramos tras una pausa.
            MostrarFeedback("ACCESO CONCEDIDO", true);
            finalizando = true;
            tiempoTransicion = 1.2f;
            return;
        }
        ConstruirModuloActual();
    }

    // ── Construcción de UI ───────────────────────────────────────

    private void ConstruirCanvas(string nombreAcceso)
    {
        if (canvas == null)
        {
            var canvasGO = new GameObject("HackCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Fondo
            var fondoGO = NuevoUI(canvasGO.transform, "Fondo", Vector2.zero, new Vector2(1920f, 1080f));
            Estirar(fondoGO);
            var fImg = fondoGO.AddComponent<Image>();
            fImg.color = colFondo;

            // Panel central (anclas centradas + posición 0 = centro exacto de pantalla)
            panel = NuevoUI(canvasGO.transform, "Panel", Vector2.zero, new Vector2(920f, 640f));
            var pImg = panel.AddComponent<Image>();
            pImg.color = colPanel;

            Contorno(panel.transform, colAcento);

            // Título arriba a la IZQUIERDA (para que no se pise con nada)
            tituloTexto = CrearTexto(panel.transform, "Titulo", "HACKEO", 34, TextAnchor.UpperLeft,
                                     new Vector2(-180f, -285f), new Vector2(560f, 50f), colAcento);
            tituloTexto.fontStyle = FontStyle.Bold;

            // Módulo en la esquina SUPERIOR DERECHA del panel
            moduloTexto = CrearTexto(panel.transform, "Modulo", "", 22, TextAnchor.UpperRight,
                                     new Vector2(360f, -285f), new Vector2(200f, 40f), new Color(0.7f, 0.8f, 0.9f));

            // Contenido del módulo (debajo del título)
            contenido = NuevoUI(panel.transform, "Contenido", new Vector2(0f, -20f), new Vector2(860f, 400f)).transform;

            // Feedback abajo
            feedbackTexto = CrearTexto(panel.transform, "Feedback", "", 24, TextAnchor.MiddleCenter,
                                       new Vector2(0f, -290f), new Vector2(860f, 40f), Color.white);

            // ── Panel de instrucciones (tutorial del Nivel 1) ──
            ConstruirPanelInstrucciones();
        }

        canvas.gameObject.SetActive(true);
        tituloTexto.text = $"ACCESO: {nombreAcceso}";
        moduloTexto.text = "";
        feedbackTexto.text = "";
    }

    private void ConstruirModuloActual(bool forzarModulo = false)
    {
        // Limpiar contenido anterior
        foreach (Transform t in contenido) Destroy(t.gameObject);

        moduloTexto.text = $"MÓDULO {indiceModulo + 1}/{modulos.Length}";
        feedbackTexto.text = "";

        // Tutorial (Nivel 1): primero las instrucciones, después el módulo
        if (esTutorial && !forzarModulo)
        {
            MostrarInstrucciones(modulos[indiceModulo]);
            return;
        }

        contenido.gameObject.SetActive(true);
        panelInstrucciones.SetActive(false);
        bloqueoInput = 0.25f;   // evita que el clic que cerró las instrucciones se "coma" el primer clic

        switch (modulos[indiceModulo])
        {
            case HackableType.DescifrarCodigo: PuzzleDescifrar(); break;
            case HackableType.UnirCables: PuzzleCables(); break;
            case HackableType.Patron: PuzzlePatron(); break;
        }
    }

    // ═══════════════════ INSTRUCCIONES (tutorial) ═══════════════════

    private void ConstruirPanelInstrucciones()
    {
        panelInstrucciones = NuevoUI(panel.transform, "Instrucciones", Vector2.zero, new Vector2(920f, 640f));
        var img = panelInstrucciones.AddComponent<Image>();
        img.color = colPanel;

        var tituloInst = CrearTexto(panelInstrucciones.transform, "TituloInst", "CÓMO HACKEAR", 34,
                                    TextAnchor.UpperLeft, new Vector2(-180f, -285f),
                                    new Vector2(560f, 50f), colVerde);
        tituloInst.fontStyle = FontStyle.Bold;

        textoTipoModulo = CrearTexto(panelInstrucciones.transform, "TipoModulo", "", 24,
                                     TextAnchor.UpperRight, new Vector2(360f, -285f),
                                     new Vector2(200f, 40f), colAcento);

        textoInstrucciones = CrearTexto(panelInstrucciones.transform, "TextoInstrucciones", "", 26,
                                        TextAnchor.MiddleCenter, new Vector2(0f, 15f),
                                        new Vector2(800f, 380f), new Color(0.85f, 0.9f, 0.95f));
        textoInstrucciones.horizontalOverflow = HorizontalWrapMode.Wrap;
        textoInstrucciones.verticalOverflow = VerticalWrapMode.Overflow;
        textoInstrucciones.lineSpacing = 1.5f;

        var btn = CrearBoton(panelInstrucciones.transform, "BotonContinuar", "EMPEZAR HACKEO",
                             new Vector2(0f, -265f), new Vector2(320f, 62f));
        btn.onClick.AddListener(() =>
        {
            panelInstrucciones.SetActive(false);
            contenido.gameObject.SetActive(true);
            ConstruirModuloActual(true);   // directo al puzzle, sin volver a las instrucciones
        });

        panelInstrucciones.SetActive(false);
    }

    private void MostrarInstrucciones(HackableType tipo)
    {
        contenido.gameObject.SetActive(false);
        panelInstrucciones.SetActive(true);

        string nombreTipo;
        switch (tipo)
        {
            case HackableType.DescifrarCodigo: nombreTipo = "DESCIFRAR CÓDIGO"; break;
            case HackableType.UnirCables: nombreTipo = "UNIR CABLES"; break;
            case HackableType.Patron: nombreTipo = "REPETIR PATRÓN"; break;
            default: nombreTipo = "HACKEO"; break;
        }

        textoTipoModulo.text = $"MÓDULO {indiceModulo + 1}/{modulos.Length} · {nombreTipo}";
        textoInstrucciones.text = TextoInstrucciones(tipo);
    }

    private static string TextoInstrucciones(HackableType tipo)
    {
        switch (tipo)
        {
            case HackableType.DescifrarCodigo:
                return "El código interceptado está cifrado con un desplazamiento César.\n\n" +
                       "Si el desplazamiento es -3, cada letra se movió 3 lugares hacia adelante " +
                       "en el abecedario. Para recuperar la palabra original, mueve cada letra " +
                       "hacia atrás esa misma cantidad.\n\n" +
                       "Ejemplo:  'D' con desplazamiento -3  =  'A'\n\n" +
                       "Escribe la palabra original en el campo de texto y presiona VERIFICAR.";

            case HackableType.UnirCables:
                return "Cada cable de la columna izquierda tiene un par del mismo color en la derecha.\n\n" +
                       "1. Haz clic en un cable de la IZQUIERDA.\n" +
                       "2. Haz clic en el cable del MISMO COLOR de la derecha.\n" +
                       "3. Si aciertas, el cable queda conectado. Si te equivocas, la selección se descarta.\n\n" +
                       "Conecta todos los cables para completar el módulo.";

            case HackableType.Patron:
                return "El sistema muestra una secuencia de luces que debes memorizar.\n\n" +
                       "1. Observa el orden en que se encienden los botones.\n" +
                       "2. Repite la secuencia haciendo clic en el mismo orden.\n" +
                       "3. Si te equivocas, la secuencia se reinicia desde el principio.\n\n" +
                       "Cada módulo agrega un paso más a la secuencia.";

            default:
                return "Resuelve el módulo para continuar.";
        }
    }

    // ═══════════════════════ MÓDULO 1: DESCIFRAR CÓDIGO ═══════════════════════

    private string[] palabras = { "PUERTA", "DATOS", "SISTEMA", "RED", "CLAVE", "HACKER", "SEEN", "NUCLEO", "BOTS", "SENAL", "VIRUS", "CONEXION" };
    private string respuestaActual;

    private void PuzzleDescifrar()
    {
        // Dificultad: tutorial (0) fácil → Nivel 3 (2) difícil
        int dificultad = Mathf.Clamp(DificultadBase + indiceModulo, 0, 5);
        int desplazamiento = 3 + dificultad;                       // 3..8
        int idx = UnityEngine.Random.Range(0, palabras.Length);
        string palabra = palabras[idx];
        respuestaActual = Desplazar(palabra, desplazamiento);

        CrearTexto(contenido, "Lbl1", "CÓDIGO INTERCEPTADO", 24, TextAnchor.MiddleCenter,
                   new Vector2(0f, 180f), new Vector2(700f, 36f), new Color(0.7f, 0.8f, 0.9f));

        var codigo = CrearTexto(contenido, "Codigo", respuestaActual, 54, TextAnchor.MiddleCenter,
                                new Vector2(0f, 115f), new Vector2(700f, 72f), colAcento);
        codigo.fontStyle = FontStyle.Bold;

        CrearTexto(contenido, "Lbl2", $"Desplazamiento César: -{desplazamiento}", 20, TextAnchor.MiddleCenter,
                   new Vector2(0f, 58f), new Vector2(700f, 32f), new Color(0.6f, 0.7f, 0.8f));
        CrearTexto(contenido, "Lbl3", "Escribe el código original y presiona VERIFICAR", 20, TextAnchor.MiddleCenter,
                   new Vector2(0f, 20f), new Vector2(700f, 32f), new Color(0.6f, 0.7f, 0.8f));

        // Campo de texto
        var inputGO = NuevoUI(contenido, "Input", new Vector2(-80f, -50f), new Vector2(380f, 62f));
        var inputImg = inputGO.AddComponent<Image>();
        inputImg.color = colBoton;
        var input = inputGO.AddComponent<InputField>();
        input.textComponent = CrearTexto(inputGO.transform, "Text", "", 30, TextAnchor.MiddleLeft,
                                         new Vector2(12f, 0f), new Vector2(360f, 56f), Color.white);
        input.targetGraphic = inputImg;

        var btn = CrearBoton(contenido, "BtnVerificar", "VERIFICAR", new Vector2(200f, -50f),
                             new Vector2(200f, 62f));
        btn.onClick.AddListener(() =>
        {
            string valor = (input.text ?? "").Trim().ToUpperInvariant();
            if (valor == palabra)
            {
                MostrarFeedback("MÓDULO COMPLETADO", true);
                tiempoTransicion = 0.9f;
            }
            else
            {
                MostrarFeedback("CÓDIGO INCORRECTO", false);
            }
        });
    }

    private string Desplazar(string texto, int n)
    {
        char[] c = texto.ToCharArray();
        for (int i = 0; i < c.Length; i++)
        {
            if (c[i] >= 'A' && c[i] <= 'Z')
                c[i] = (char)('A' + ((c[i] - 'A' + n) % 26));
        }
        return new string(c);
    }

    // ═══════════════════════ MÓDULO 2: UNIR CABLES ═══════════════════════

    private void PuzzleCables()
    {
        int cantidad = Mathf.Clamp(3 + DificultadBase + indiceModulo, 3, 6);
        CrearTexto(contenido, "LblCables", "CONECTA LOS CABLES DEL MISMO COLOR", 26, TextAnchor.MiddleCenter,
                   new Vector2(0f, 195f), new Vector2(760f, 40f), colAcento);

        Color[] colores = {
            new Color(1f, 0.2f, 0.2f), new Color(0.2f, 0.8f, 1f), new Color(1f, 0.85f, 0.15f),
            new Color(1f, 0.3f, 0.8f), new Color(0.4f, 1f, 0.3f), new Color(0.7f, 0.4f, 1f)
        };

        // Columnas izquierda/derecha
        var izq = NuevoUI(contenido, "Izq", new Vector2(-220f, 0f), new Vector2(160f, 300f)).transform;
        var der = NuevoUI(contenido, "Der", new Vector2(220f, 0f), new Vector2(160f, 300f)).transform;

        var ordenDer = new List<int>();
        for (int i = 0; i < cantidad; i++) ordenDer.Add(i);
        // Mezclar (Fisher-Yates)
        for (int i = ordenDer.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (ordenDer[i], ordenDer[j]) = (ordenDer[j], ordenDer[i]);
        }

        int resueltos = 0;
        int seleccionado = -1;

        float alto = 300f / cantidad;
        for (int i = 0; i < cantidad; i++)
        {
            float y = 150f - alto * (i + 0.5f);
            int indiceIzq = i;
            var bIzq = CrearBoton(izq, $"Cable_{i}", $"C{i + 1}", new Vector2(0f, y),
                                  new Vector2(120f, Mathf.Min(46f, alto - 12f)),
                                  colores[i]);
            bIzq.image.color = colores[i];
            bIzq.onClick.AddListener(() => seleccionado = indiceIzq);
        }

        for (int k = 0; k < cantidad; k++)
        {
            int colorIdx = ordenDer[k];
            int indiceDer = k;
            float y = 150f - alto * (k + 0.5f);
            var bDer = CrearBoton(der, $"CableR_{colorIdx}", $"C{colorIdx + 1}", new Vector2(0f, y),
                                  new Vector2(120f, Mathf.Min(46f, alto - 12f)),
                                  colores[colorIdx]);
            bDer.image.color = colores[colorIdx];
            bDer.onClick.AddListener(() =>
            {
                if (bloqueoInput > 0f) return;
                if (seleccionado < 0)
                {
                    MostrarFeedback("SELECCIONA UN CABLE DE LA IZQUIERDA", false);
                    return;
                }
                if (colorIdx == seleccionado)
                {
                    // Conexión correcta
                    DibujarCable(contenido, izq.GetChild(seleccionado) as RectTransform,
                                 der.GetChild(indiceDer) as RectTransform, colores[colorIdx]);
                    izq.GetChild(seleccionado).GetComponent<Button>().interactable = false;
                    bDer.interactable = false;
                    seleccionado = -1;
                    resueltos++;
                    if (resueltos >= cantidad)
                    {
                        MostrarFeedback("MÓDULO COMPLETADO", true);
                        tiempoTransicion = 0.9f;
                    }
                }
                else
                {
                    MostrarFeedback("CABLE INCORRECTO", false);
                    seleccionado = -1;
                }
            });
        }
    }

    private void DibujarCable(Transform parent, RectTransform a, RectTransform b, Color color)
    {
        var cableGO = new GameObject("Linea", typeof(RectTransform));
        cableGO.transform.SetParent(parent, false);
        var rt = (RectTransform)cableGO.transform;

        // Posiciones locales al contenedor (los botones cuelgan de columnas hijas)
        Vector2 pa = LocalAlContenedor(a, (RectTransform)parent);
        Vector2 pb = LocalAlContenedor(b, (RectTransform)parent);
        Vector2 delta = pb - pa;
        float dist = delta.magnitude;

        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(dist, 8f);
        rt.anchoredPosition = (pa + pb) / 2f;
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        var img = cableGO.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    private Vector2 LocalAlContenedor(RectTransform hijo, RectTransform contenedor)
    {
        Vector3 mundo = hijo.TransformPoint(hijo.rect.center);
        return (Vector2)contenedor.InverseTransformPoint(mundo);
    }

    // ═══════════════════════ MÓDULO 3: PATRÓN ═══════════════════════

    private List<int> secuencia = new List<int>();
    private int indiceJugador;
    private bool mostrandoSecuencia;
    private Button[] botonesPatron;

    private void PuzzlePatron()
    {
        int largo = Mathf.Clamp(3 + DificultadBase + indiceModulo, 3, 8);
        CrearTexto(contenido, "LblPatron", "REPITE LA SECUENCIA DE LUCES", 26, TextAnchor.MiddleCenter,
                   new Vector2(0f, 195f), new Vector2(760f, 40f), colAcento);

        Color[] colores = {
            new Color(1f, 0.25f, 0.25f), new Color(0.25f, 0.8f, 1f),
            new Color(1f, 0.85f, 0.2f), new Color(0.4f, 1f, 0.35f)
        };
        Color[] apagados = {
            new Color(0.3f, 0.1f, 0.1f), new Color(0.1f, 0.25f, 0.3f),
            new Color(0.3f, 0.26f, 0.08f), new Color(0.12f, 0.3f, 0.13f)
        };

        var grid = NuevoUI(contenido, "Grid", new Vector2(0f, -10f), new Vector2(360f, 260f)).transform;
        botonesPatron = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0) ? -90f : 90f;
            float y = (i < 2) ? 65f : -65f;
            var b = CrearBoton(grid, $"Boton_{i}", "", new Vector2(x, y), new Vector2(170f, 120f), apagados[i]);
            b.image.color = apagados[i];
            int idx = i;
            b.onClick.AddListener(() => ClickPatron(idx, colores, apagados));
            botonesPatron[i] = b;
        }

        // Generar y mostrar secuencia
        secuencia.Clear();
        for (int i = 0; i < largo; i++) secuencia.Add(UnityEngine.Random.Range(0, 4));
        indiceJugador = 0;
        mostrandoSecuencia = true;
        StartCoroutine(MostrarSecuencia(colores, apagados));
    }

    private IEnumerator MostrarSecuencia(Color[] encendidos, Color[] apagados)
    {
        MostrarFeedback("OBSERVA LA SECUENCIA...", false);
        yield return new WaitForSecondsRealtime(0.7f);

        foreach (int idx in secuencia)
        {
            if (botonesPatron[idx] != null) botonesPatron[idx].image.color = encendidos[idx];
            yield return new WaitForSecondsRealtime(0.45f);
            if (botonesPatron[idx] != null) botonesPatron[idx].image.color = apagados[idx];
            yield return new WaitForSecondsRealtime(0.22f);
        }

        mostrandoSecuencia = false;
        MostrarFeedback("REPITE LA SECUENCIA", false);
    }

    private void ClickPatron(int idx, Color[] encendidos, Color[] apagados)
    {
        if (mostrandoSecuencia || tiempoTransicion > 0f || bloqueoInput > 0f) return;

        // Flash del botón
        StartCoroutine(FlashBoton(idx, encendidos[idx], apagados[idx]));

        if (secuencia[indiceJugador] == idx)
        {
            indiceJugador++;
            if (indiceJugador >= secuencia.Count)
            {
                MostrarFeedback("MÓDULO COMPLETADO", true);
                tiempoTransicion = 0.9f;
            }
        }
        else
        {
            MostrarFeedback("SECUENCIA INCORRECTA", false);
            indiceJugador = 0;
            StartCoroutine(ReiniciarSecuencia());
        }
    }

    private IEnumerator FlashBoton(int idx, Color encendido, Color apagado)
    {
        if (botonesPatron[idx] != null) botonesPatron[idx].image.color = encendido;
        yield return new WaitForSecondsRealtime(0.18f);
        if (botonesPatron[idx] != null) botonesPatron[idx].image.color = apagado;
    }

    private IEnumerator ReiniciarSecuencia()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        if (!activo) yield break;
        mostrandoSecuencia = true;
        yield return StartCoroutine(MostrarSecuencia(
            new[] { new Color(1f, 0.25f, 0.25f), new Color(0.25f, 0.8f, 1f), new Color(1f, 0.85f, 0.2f), new Color(0.4f, 1f, 0.35f) },
            new[] { new Color(0.3f, 0.1f, 0.1f), new Color(0.1f, 0.25f, 0.3f), new Color(0.3f, 0.26f, 0.08f), new Color(0.12f, 0.3f, 0.13f) }));
    }

    // ═══════════════════════ Utilidades de UI ═══════════════════════

    private void MostrarFeedback(string texto, bool exito)
    {
        if (feedbackTexto == null) return;
        feedbackTexto.text = texto;
        feedbackTexto.color = exito ? colVerde : colRojo;
    }

    private GameObject NuevoUI(Transform parent, string nombre, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return go;
    }

    private void Estirar(GameObject go)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void Contorno(Transform parent, Color color)
    {
        float w = 920f, h = 640f, t = 3f;
        var sup = NuevoUI(parent, "BordeSup", new Vector2(0f, h / 2f), new Vector2(w, t));
        sup.AddComponent<Image>().color = color;
        var inf = NuevoUI(parent, "BordeInf", new Vector2(0f, -h / 2f), new Vector2(w, t));
        inf.AddComponent<Image>().color = color;
        var izq = NuevoUI(parent, "BordeIzq", new Vector2(-w / 2f, 0f), new Vector2(t, h));
        izq.AddComponent<Image>().color = color;
        var der = NuevoUI(parent, "BordeDer", new Vector2(w / 2f, 0f), new Vector2(t, h));
        der.AddComponent<Image>().color = color;
    }

    private Text CrearTexto(Transform parent, string nombre, string contenido, int size,
                            TextAnchor align, Vector2 pos, Vector2 dimension, Color color)
    {
        var go = NuevoUI(parent, nombre, pos, dimension);
        var txt = go.AddComponent<Text>();
        txt.text = contenido;
        txt.font = Fuente;
        txt.fontSize = size;
        txt.alignment = align;
        txt.color = color;
        txt.raycastTarget = false;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        return txt;
    }

    private Button CrearBoton(Transform parent, string nombre, string etiqueta, Vector2 pos, Vector2 size, Color? colorBase = null)
    {
        Color baseColor = colorBase ?? colBoton;

        var go = NuevoUI(parent, nombre, pos, size);
        var img = go.AddComponent<Image>();
        img.color = baseColor;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;

        // El tint REEMPLAZA el color del gráfico: por eso los colores del tint
        // deben ser el color del propio botón (así se ven los cables/patrones de color)
        var c = btn.colors;
        c.normalColor = baseColor;
        c.highlightedColor = Color.Lerp(baseColor, Color.white, 0.22f);
        c.pressedColor = Color.Lerp(baseColor, Color.white, 0.45f);
        c.selectedColor = baseColor;
        c.disabledColor = Color.Lerp(baseColor, Color.black, 0.4f);
        c.fadeDuration = 0.05f;
        c.colorMultiplier = 1f;
        btn.colors = c;
        btn.image.color = baseColor;

        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        if (!string.IsNullOrEmpty(etiqueta))
        {
            var lbl = CrearTexto(btn.transform, "Texto", etiqueta, 22, TextAnchor.MiddleCenter,
                                 Vector2.zero, size, Color.white);
            lbl.fontStyle = FontStyle.Bold;
        }

        return btn;
    }

    private void AsegurarEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    private bool LeerEscape()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
