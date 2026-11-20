using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using static HackUI;

/// <summary>
/// Interfaz de hackeo 2D (Canvas overlay). Abre una cadena de módulos;
/// al resolverlos todos devuelve el control al jugador y avisa al terminal.
/// Mientras está abierta el juego queda en pausa (timeScale 0) y el ratón libre.
/// </summary>
public class HackManager : MonoBehaviour, IContextoHack
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
    private IPuzzle puzzleActual;
    private HackableType tipoActual;

    // Colores
    private readonly Color colFondo = new Color(0.02f, 0.03f, 0.04f, 0.95f);
    private readonly Color colPanel = new Color(0.06f, 0.08f, 0.11f, 1f);
    private readonly Color colAcento = new Color(0.15f, 0.85f, 1f);
    private readonly Color colVerde = new Color(0.15f, 1f, 0.4f);
    private readonly Color colRojo = new Color(1f, 0.25f, 0.25f);


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

        if (puzzleActual != null) puzzleActual.Actualizar();

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
                    if (cb != null) cb();
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

        int cantidad = (mods != null && mods.Length > 0) ? mods.Length : 1;
        if (tutorial)
        {
            if (mods != null && mods.Length > 0)
                modulos = mods;
            else
                modulos = new[] { HackableType.DescifrarCodigo };
        }
        else
        {
            int totalTipos = Enum.GetValues(typeof(HackableType)).Length;
            modulos = new HackableType[cantidad];
            for (int i = 0; i < cantidad; i++)
                modulos[i] = (HackableType)UnityEngine.Random.Range(0, totalTipos);
        }
        onCompletado = onResuelto;
        esTutorial = tutorial;
        indiceModulo = 0;
        tiempoTransicion = 0f;
        finalizando = false;

        // Congelar el juego y liberar el raton para operar la UI
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
        if (puzzleActual != null) puzzleActual.Detener();
        puzzleActual = null;
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

            // ── Integración: Registrar hackeo completado en Pila (LIFO) ──
            var edHackeo = EstructurasDatosJuego.Instancia;
            if (edHackeo != null) edHackeo.RegistrarAccionJugador("Hackeo de cuenta completado (dato privado extraído)");

            return;
        }
        ConstruirModuloActual();
    }

    // ── IContextoHack ────────────────────────────────────────────
    Transform IContextoHack.Contenido => contenido;
    int IContextoHack.DificultadBase => DificultadBase;
    int IContextoHack.IndiceModulo => indiceModulo;
    bool IContextoHack.InputBloqueado => bloqueoInput > 0f;
    bool IContextoHack.EnTransicion => tiempoTransicion > 0f;
    void IContextoHack.MostrarFeedback(string texto, bool exito) => MostrarFeedback(texto, exito);
    void IContextoHack.CompletarModulo() { tiempoTransicion = 0.9f; }
    void IContextoHack.Regenerar() { RegenerarActual(); }
    Coroutine IContextoHack.IniciarCorrutina(IEnumerator rutina) => StartCoroutine(rutina);

    private void RegenerarActual()
    {
        foreach (Transform t in contenido) Destroy(t.gameObject);
        if (puzzleActual != null) puzzleActual.Detener();
        puzzleActual = FabricaPuzzles.Crear(tipoActual, this);
        if (puzzleActual != null) puzzleActual.Construir();
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

        if (puzzleActual != null) puzzleActual.Detener();
        tipoActual = modulos[indiceModulo];

        // ── Integración: Registrar módulo de hackeo en Lista (List<T>) ──
        var edModulo = EstructurasDatosJuego.Instancia;
        if (edModulo != null) edModulo.RegistrarModuloHackeoResuelto(tipoActual.ToString());

        puzzleActual = FabricaPuzzles.Crear(tipoActual, this);
        if (puzzleActual != null)
            puzzleActual.Construir();
        else
            Debug.LogWarning("[HackManager] No hay puzzle para el módulo " + tipoActual);
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
            case HackableType.Escanear: nombreTipo = "ESCANEAR MATRIZ"; break;
            case HackableType.Sincronizar: nombreTipo = "SINCRONIZAR SEÑAL"; break;
            case HackableType.Secuencia: nombreTipo = "SECUENCIA LÓGICA"; break;
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

            case HackableType.Escanear:
                return "Una cuadrícula de glifos idénticos esconde UNO diferente." +
                       "\n\nHaz clic en el glifo distinto para completar el módulo. " +
                       "Si te equivocas, la cuadrícula se reescanea.";

            case HackableType.Sincronizar:
                return "El marcador se mueve por la pista y hay una ZONA VERDE." +
                       "\n\nPresiona SINCRONIZAR (o la barra espaciadora) cuando el " +
                       "marcador esté dentro de la zona. Las señales necesarias y la " +
                       "velocidad aumentan con la dificultad del nivel.";

            case HackableType.Secuencia:
                return "Se muestra una serie con un elemento faltante." +
                       "\n\nDescubre la regla (multiplicación, suma constante, " +
                       "cuadrados o letras con salto) y elige la opción correcta " +
                       "entre las cuatro disponibles.";
            default:
                return "Resuelve el módulo para continuar.";
        }
    }

    // ═══════════════════════ Utilidades de UI ═══════════════════════

    private void MostrarFeedback(string texto, bool exito)
    {
        if (feedbackTexto == null) return;
        feedbackTexto.text = texto;
        if (exito) feedbackTexto.color = colVerde;
        else feedbackTexto.color = colRojo;
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