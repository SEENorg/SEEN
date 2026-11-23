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
    private Image fondoPanel;   // fondo del panel (se oculta si el puzzle dibuja su propio marco)
    private GameObject marco;   // contenedor del borde azul del panel
    private Text tituloTexto;
    private Text moduloTexto;
    private Text feedbackTexto;
    private Transform contenido;

    // Instrucciones (tutorial)
    private GameObject panelInstrucciones;
    private Text textoInstrucciones;
    private Text textoTipoModulo;
    private Text textoModuloInstrucciones;
    private Text textoControles;
    private Button botonEmpezar;
    private CajasRedondeadasUI cajasInstrucciones;   // esquinas redondeadas de la tarjeta de instrucciones

    // Colores de la tarjeta de instrucciones (los mismos de los puzzles con tarjeta)
    private readonly Color colTarjeta = new Color(0.035f, 0.055f, 0.075f, 0.98f);
    private readonly Color colBordeTarjeta = new Color(0.16f, 0.22f, 0.27f);
    private readonly Color colCaja = new Color(0.06f, 0.13f, 0.18f);
    private readonly Color colTextoSuave = new Color(0.55f, 0.65f, 0.72f);
    private readonly Color colBotonEmpezar = new Color(0.05f, 0.36f, 0.26f);
    private readonly Color colBotonEmpezarSeleccion = new Color(0.07f, 0.47f, 0.34f);
    private readonly Color colBordeBotonEmpezar = new Color(0.18f, 0.72f, 0.50f);

    private HackableType[] modulos;
    private System.Action onCompletado;
    private int indiceModulo;
    private bool activo;
    private bool esTutorial;
    private bool finalizando;
    private int frameApertura = -1;    // el mismo botón Y que abrió no debe cerrar en ese frame
    private int frameCierre = -10;     // ni reabrir justo después de cerrar
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

        // Cerrar con Escape (teclado) o Y (mando, el mismo botón que abre el hackeo)
        if (LeerEscape() || LeerBotonSalirMando()) { Cerrar(); return; }

        if (panelInstrucciones != null && panelInstrucciones.activeSelf) MantenerSeleccionInstrucciones();

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
        if (cajasInstrucciones != null) cajasInstrucciones.Liberar();
        // Restaurar el juego por si se destruye estando abierto
        Time.timeScale = 1f;
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.enabled = true;
    }

    /// <summary>
    /// True si la lista trae el módulo CodigoSecreto. En ese caso se usan exactamente los módulos
    /// indicados en el terminal (en los demás casos se eligen al azar).
    /// </summary>
    private static bool ContieneCodigoSecreto(HackableType[] mods)
    {
        if (mods == null) return false;
        foreach (var m in mods)
            if (m == HackableType.CodigoSecreto) return true;
        return false;
    }

    private void Iniciar(HackableType[] mods, string nombreAcceso, Action onResuelto, bool tutorial = false)
    {
        if (activo) return;
        // Si se acaba de cerrar con Y, ese mismo botón no lo vuelve a abrir
        if (Time.frameCount <= frameCierre + 1) return;
        frameApertura = Time.frameCount;

        int cantidad = (mods != null && mods.Length > 0) ? mods.Length : 1;
        if (tutorial || ContieneCodigoSecreto(mods))
        {
            if (mods != null && mods.Length > 0)
                modulos = mods;
            else
                modulos = new[] { HackableType.DescifrarCodigo };
        }
        else
        {
            // CodigoSecreto no sale al azar: solo aparece si el terminal lo trae en su lista
            var disponibles = new List<HackableType>();
            foreach (HackableType t in Enum.GetValues(typeof(HackableType)))
                if (t != HackableType.CodigoSecreto) disponibles.Add(t);

            modulos = new HackableType[cantidad];
            for (int i = 0; i < cantidad; i++)
                modulos[i] = disponibles[UnityEngine.Random.Range(0, disponibles.Count)];
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
        frameCierre = Time.frameCount;
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

    string IContextoHack.TextoAcceso
    {
        get
        {
            if (tituloTexto == null) return "";
            return tituloTexto.text;
        }
    }

    string IContextoHack.TextoModulo
    {
        get
        {
            if (moduloTexto == null) return "";
            return moduloTexto.text;
        }
    }

    void IContextoHack.MostrarMarco(bool visible)
    {
        MostrarMarcoInterno(visible);
    }

    /// <summary>Muestra u oculta el fondo del panel, el borde azul, el título, el contador de módulos y el mensaje de abajo.</summary>
    private void MostrarMarcoInterno(bool visible)
    {
        if (fondoPanel != null) fondoPanel.enabled = visible;
        if (marco != null) marco.SetActive(visible);
        if (tituloTexto != null) tituloTexto.enabled = visible;
        if (moduloTexto != null) moduloTexto.enabled = visible;
        if (feedbackTexto != null) feedbackTexto.enabled = visible;
    }

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
            fondoPanel = pImg;

            // El borde va dentro de "Marco" para poder ocultarlo de una vez (MostrarMarco)
            marco = NuevoUI(panel.transform, "Marco", Vector2.zero, new Vector2(920f, 640f));
            Contorno(marco.transform, colAcento);

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
        MostrarMarcoInterno(true);
        tituloTexto.text = $"ACCESO: {nombreAcceso}";
        moduloTexto.text = "";
        feedbackTexto.text = "";
    }

    private void ConstruirModuloActual(bool forzarModulo = false)
    {
        // Limpiar contenido anterior
        foreach (Transform t in contenido) Destroy(t.gameObject);
        // Cada módulo empieza con el marco visible (las instrucciones del tutorial lo usan)
        MostrarMarcoInterno(true);

        moduloTexto.text = $"MÓDULO {indiceModulo + 1}/{modulos.Length}";
        feedbackTexto.text = "";

        // Tutorial (Nivel 1): primero las instrucciones, después el módulo
        // (CodigoSecreto se salta las instrucciones: va directo a pedir el código)
        if (esTutorial && !forzarModulo && modulos[indiceModulo] != HackableType.CodigoSecreto)
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

    /// <summary>
    /// Tarjeta "CÓMO HACKEAR" del tutorial, con el mismo estilo que los puzzles:
    /// encabezado con el tipo de módulo, caja con las instrucciones, caja con los
    /// controles y botón EMPEZAR HACKEO (se ilumina y funciona con el mando: botón A).
    /// </summary>
    private void ConstruirPanelInstrucciones()
    {
        cajasInstrucciones = new CajasRedondeadasUI();

        panelInstrucciones = NuevoUI(panel.transform, "Instrucciones", Vector2.zero, new Vector2(920f, 640f));

        Image bordeTarjeta = cajasInstrucciones.CrearCaja(panelInstrucciones.transform, "TarjetaInstrucciones",
                                                          Vector2.zero, new Vector2(760f, 560f),
                                                          colTarjeta, colBordeTarjeta, 1f);
        Transform tarjeta = bordeTarjeta.transform.parent;

        // Encabezado
        CrearTexto(tarjeta, "Guia", "SEEN // CÓMO HACKEAR", 15, TextAnchor.MiddleCenter,
                   new Vector2(0f, 245f), new Vector2(700f, 22f), colVerde);

        textoTipoModulo = CrearTexto(tarjeta, "TipoModulo", "", 32, TextAnchor.MiddleCenter,
                                     new Vector2(0f, 212f), new Vector2(700f, 40f), colAcento);
        textoTipoModulo.fontStyle = FontStyle.Bold;

        textoModuloInstrucciones = CrearTexto(tarjeta, "ModuloInst", "", 15, TextAnchor.MiddleCenter,
                                              new Vector2(0f, 180f), new Vector2(700f, 22f), colTextoSuave);

        // Caja de instrucciones
        Image bordeTexto = cajasInstrucciones.CrearCaja(tarjeta, "CajaInstrucciones", new Vector2(0f, 30f),
                                                        new Vector2(700f, 270f), colCaja,
                                                        new Color(colAcento.r, colAcento.g, colAcento.b, 0.6f), 1.2f);
        textoInstrucciones = CrearTexto(bordeTexto.transform.parent, "TextoInstrucciones", "", 21,
                                        TextAnchor.MiddleLeft, Vector2.zero, new Vector2(650f, 240f),
                                        new Color(0.85f, 0.9f, 0.95f));
        textoInstrucciones.horizontalOverflow = HorizontalWrapMode.Wrap;
        textoInstrucciones.verticalOverflow = VerticalWrapMode.Truncate;
        textoInstrucciones.lineSpacing = 1.2f;
        // Si un texto es largo, la letra se achica sola para que quepa en la caja
        textoInstrucciones.resizeTextForBestFit = true;
        textoInstrucciones.resizeTextMinSize = 14;
        textoInstrucciones.resizeTextMaxSize = 21;

        // Caja de controles
        Image bordeControles = cajasInstrucciones.CrearCaja(tarjeta, "CajaControles", new Vector2(0f, -140f),
                                                            new Vector2(700f, 58f), colCaja, colBordeTarjeta, 1.2f);
        textoControles = CrearTexto(bordeControles.transform.parent, "TextoControles", "", 15,
                                    TextAnchor.MiddleCenter, Vector2.zero, new Vector2(680f, 54f), colTextoSuave);
        textoControles.supportRichText = true;

        // Botón EMPEZAR HACKEO
        Image bordeBoton = cajasInstrucciones.CrearCaja(tarjeta, "BotonContinuar", new Vector2(0f, -218f),
                                                        new Vector2(320f, 58f), Color.white, colBordeBotonEmpezar, 1.3f);
        GameObject goBoton = bordeBoton.transform.parent.gameObject;
        Image fondoBoton = goBoton.GetComponent<Image>();

        botonEmpezar = goBoton.AddComponent<Button>();
        botonEmpezar.targetGraphic = fondoBoton;
        botonEmpezar.transition = Selectable.Transition.ColorTint;
        ColorBlock colores = botonEmpezar.colors;
        colores.normalColor = colBotonEmpezar;
        colores.highlightedColor = colBotonEmpezarSeleccion;
        colores.selectedColor = colBotonEmpezarSeleccion;
        colores.pressedColor = Color.Lerp(colBotonEmpezarSeleccion, Color.white, 0.25f);
        colores.colorMultiplier = 1f;
        colores.fadeDuration = 0.06f;
        botonEmpezar.colors = colores;

        Text textoBoton = CrearTexto(goBoton.transform, "Texto", "EMPEZAR HACKEO", 21, TextAnchor.MiddleCenter,
                                     Vector2.zero, new Vector2(320f, 58f), Color.white);
        textoBoton.fontStyle = FontStyle.Bold;

        TeclaCodigoUI iluminacion = goBoton.AddComponent<TeclaCodigoUI>();
        iluminacion.Configurar("EMPEZAR", bordeBoton, colBordeBotonEmpezar, colAcento, AlEmpezarHackeo);
        botonEmpezar.onClick.AddListener(iluminacion.Presionar);

        panelInstrucciones.SetActive(false);
    }

    /// <summary>Botón EMPEZAR HACKEO: cierra las instrucciones y arma el puzzle.</summary>
    private void AlEmpezarHackeo(string valor)
    {
        panelInstrucciones.SetActive(false);
        contenido.gameObject.SetActive(true);
        ConstruirModuloActual(true);   // directo al puzzle, sin volver a las instrucciones
    }

    /// <summary>Mientras se ven las instrucciones, el botón siempre queda seleccionado (para el mando).</summary>
    private void MantenerSeleccionInstrucciones()
    {
        if (botonEmpezar == null || EventSystem.current == null) return;
        if (EventSystem.current.currentSelectedGameObject == null)
            EventSystem.current.SetSelectedGameObject(botonEmpezar.gameObject);
    }

    private void MostrarInstrucciones(HackableType tipo)
    {
        contenido.gameObject.SetActive(false);
        panelInstrucciones.SetActive(true);
        // La tarjeta de instrucciones tiene su propio marco: se oculta el del panel
        MostrarMarcoInterno(false);

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

        textoTipoModulo.text = nombreTipo;
        textoModuloInstrucciones.text = "MÓDULO " + (indiceModulo + 1) + "/" + modulos.Length;
        textoInstrucciones.text = TextoInstrucciones(tipo);
        textoControles.text = TextoControles(tipo);

        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(botonEmpezar.gameObject);
    }

    /// <summary>
    /// Línea de controles debajo de las instrucciones. Solo nombra los controles que
    /// ese módulo de verdad acepta (los que no tienen soporte de mando dicen solo ratón).
    /// </summary>
    private string TextoControles(HackableType tipo)
    {
        string raton = "<color=#26D9FF>RATÓN</color>  clic";
        string salir = "\n<color=#26D9FF>Y</color> (mando) o <color=#26D9FF>ESC</color> (teclado): salir del hackeo";
        switch (tipo)
        {
            case HackableType.UnirCables:
                return raton + "      <color=#26D9FF>MANDO</color>  cruceta + A · B cancela"
                     + "      <color=#26D9FF>TECLADO</color>  flechas + Enter" + salir;
            case HackableType.Patron:
            case HackableType.Secuencia:
                return raton + "      <color=#26D9FF>MANDO</color>  cruceta + A"
                     + "      <color=#26D9FF>TECLADO</color>  flechas + Enter" + salir;
            case HackableType.DescifrarCodigo:
                return raton + "      <color=#26D9FF>TECLADO</color>  escribe la palabra"
                     + "      <color=#26D9FF>TAB</color>  tabla César" + salir;
            default:
                return raton + " en los botones del módulo" + salir;
        }
    }

    private static string TextoInstrucciones(HackableType tipo)
    {
        switch (tipo)
        {
            case HackableType.DescifrarCodigo:
                return "El código interceptado está cifrado con un desplazamiento César muy simple: +1.\n\n" +
                       "Cada letra del código está UNA letra antes de la original. Para recuperar " +
                       "la palabra, avanza cada letra 1 lugar en el abecedario.\n\n" +
                       "Ejemplo:  'B'  =  'C'     'M'  =  'N'\n\n" +
                       "Puedes abrir la tabla de ayuda con TAB.\n" +
                       "Escribe la palabra original en el campo de texto y presiona VERIFICAR.";

            case HackableType.UnirCables:
                return "Cada cable de ORIGEN (izquierda) tiene su pareja en DESTINO (derecha): " +
                       "tienen el mismo NOMBRE y el mismo color.\n\n" +
                       "1. Elige un cable de ORIGEN.\n" +
                       "2. Elige el cable de DESTINO con el mismo nombre.\n" +
                       "3. Si aciertas, queda enlazado. Si te equivocas, el puzzle se reinicia con otro orden.\n\n" +
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

    /// <summary>Botón Y del mando (Interact): vuelve a pulsarlo para salir de cualquier puzzle.</summary>
    private bool LeerBotonSalirMando()
    {
        if (Time.frameCount == frameApertura) return false;
#if ENABLE_INPUT_SYSTEM
        Gamepad mando = Gamepad.current;
        if (mando == null) return false;
        return mando.buttonNorth.wasPressedThisFrame;
#else
        return false;
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