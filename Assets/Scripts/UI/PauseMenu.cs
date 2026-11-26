using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Menú de pausa (ESC en el teclado o START en el mando):
///   • CONTINUAR      → vuelve al juego sin perder nada
///   • AJUSTES        → gráficos, resolución, pantalla completa, volumen y sensibilidad
///   • MENÚ PRINCIPAL → vuelve al menú (pide confirmación, se pierde el progreso del nivel)
/// Mientras está abierto, el juego queda en pausa real y el ratón libre.
/// Se maneja con ratón, teclado y mando: el elemento seleccionado crece y se ilumina.
/// B / ESC vuelve atrás (o cierra la pausa desde la pantalla principal).
/// </summary>
public class PauseMenu : MonoBehaviour
{
    private static bool pausado;

    public static bool Pausado
    {
        get { return pausado; }
        private set { pausado = value; }
    }

    private enum Pantalla { Cerrada, Principal, Ajustes, Confirmar }

    private readonly Vector2 tamanoFila = new Vector2(640f, 60f);

    private PlayerController pc;
    private Pantalla pantalla = Pantalla.Cerrada;
    private int frameApertura = -10;

    private ConstructorMenuJuego constructor;
    private GameObject canvas;
    private GameObject panelPrincipal;
    private GameObject panelAjustes;
    private GameObject panelConfirmar;
    private Button botonContinuar;
    private Button botonAjustes;
    private Button botonSalir;
    private Button botonCancelarSalida;
    private SelectorOpcionUI selectorCalidad;
    private SelectorOpcionUI selectorResolucion;
    private SelectorOpcionUI selectorPantalla;
    private Slider sliderVolumen;
    private Slider sliderSensibilidad;
    private Text textoVolumen;
    private Text textoSensibilidad;
    private Button botonVolverAjustes;
    private bool cargandoValores;

    private void Awake()
    {
        pc = GetComponent<PlayerController>();
        Pausado = false;
    }

    private void OnDestroy()
    {
        if (pantalla != Pantalla.Cerrada)
        {
            Pausado = false;
            Time.timeScale = 1f;
        }
        if (canvas != null) Destroy(canvas);
        if (constructor != null) constructor.Liberar();
    }

    private void Update()
    {
        if (HackManager.Abierto || GameFeedback.NivelCompletadoVisible || GameFeedback.GameOverVisible ||
            PantallaControles.Visible || CipherWheel.Abierta)
            return;

        if (pantalla == Pantalla.Cerrada)
        {
            if (LeerPausa()) AbrirPausa();
            return;
        }

        Time.timeScale = 0f;
        MantenerSeleccion();

        if (Time.frameCount == frameApertura) return;
        if (LeerPausa() && pantalla == Pantalla.Principal)
        {
            CerrarPausa();
            return;
        }
        if (LeerAtras()) Atras();
    }

    // ── Abrir / cerrar ──────────────────────────────────────────────

    private void AbrirPausa()
    {
        if (canvas == null) Construir();

        Pausado = true;
        frameApertura = Time.frameCount;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (pc != null) pc.enabled = false;

        canvas.SetActive(true);
        MostrarPantalla(Pantalla.Principal);
    }

    private void CerrarPausa()
    {
        pantalla = Pantalla.Cerrada;
        Pausado = false;
        if (canvas != null) canvas.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (pc != null) pc.enabled = true;
    }

    private void Atras()
    {
        if (pantalla == Pantalla.Principal) CerrarPausa();
        else if (pantalla == Pantalla.Ajustes) MostrarPantalla(Pantalla.Principal);
        else if (pantalla == Pantalla.Confirmar) MostrarPantalla(Pantalla.Principal);
    }

    private void MostrarPantalla(Pantalla nueva)
    {
        Pantalla anterior = pantalla;
        pantalla = nueva;
        panelPrincipal.SetActive(nueva == Pantalla.Principal);
        panelAjustes.SetActive(nueva == Pantalla.Ajustes);
        panelConfirmar.SetActive(nueva == Pantalla.Confirmar);

        GameObject seleccion = null;
        if (nueva == Pantalla.Principal)
        {
            seleccion = botonContinuar.gameObject;
            if (anterior == Pantalla.Ajustes) seleccion = botonAjustes.gameObject;
            if (anterior == Pantalla.Confirmar) seleccion = botonSalir.gameObject;
        }
        else if (nueva == Pantalla.Ajustes)
        {
            CargarValores();
            seleccion = selectorCalidad.gameObject;
        }
        else if (nueva == Pantalla.Confirmar)
        {
            seleccion = botonCancelarSalida.gameObject;
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(seleccion);
        }
    }

    /// <summary>Para el mando: si se pierde la selección (p. ej. clic en el fondo), se recupera.</summary>
    private void MantenerSeleccion()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject != null) return;
        if (pantalla == Pantalla.Principal) EventSystem.current.SetSelectedGameObject(botonContinuar.gameObject);
        else if (pantalla == Pantalla.Ajustes) EventSystem.current.SetSelectedGameObject(selectorCalidad.gameObject);
        else if (pantalla == Pantalla.Confirmar) EventSystem.current.SetSelectedGameObject(botonCancelarSalida.gameObject);
    }

    // ── Botones ─────────────────────────────────────────────────────

    private void AlContinuar()
    {
        CerrarPausa();
    }

    private void AlAjustes()
    {
        MostrarPantalla(Pantalla.Ajustes);
    }

    private void AlMenuPrincipal()
    {
        MostrarPantalla(Pantalla.Confirmar);
    }

    private void AlVolverAjustes()
    {
        MostrarPantalla(Pantalla.Principal);
    }

    private void AlConfirmarSalida()
    {
        Time.timeScale = 1f;
        Pausado = false;
        pantalla = Pantalla.Cerrada;
        if (pc != null) pc.enabled = true;
        Boot.PermitirInicioEscena = true;
        SceneManager.LoadScene("MainMenu");
    }

    private void AlCancelarSalida()
    {
        MostrarPantalla(Pantalla.Principal);
    }

    // ── Ajustes ─────────────────────────────────────────────────────

    private void CargarValores()
    {
        cargandoValores = true;
        selectorCalidad.PonerIndice(AjustesJuego.PerfilGuardado);
        selectorResolucion.PonerIndice(AjustesJuego.IndiceResolucionGuardado);
        if (AjustesJuego.PantallaCompletaGuardada) selectorPantalla.PonerIndice(0);
        else selectorPantalla.PonerIndice(1);
        sliderVolumen.value = AjustesJuego.VolumenGuardado;
        sliderSensibilidad.value = AjustesJuego.SensibilidadGuardada;
        ActualizarTextoVolumen(sliderVolumen.value);
        ActualizarTextoSensibilidad(sliderSensibilidad.value);
        cargandoValores = false;
    }

    private void AlCambiarCalidad(int indice)
    {
        AjustesJuego.AplicarPerfilGrafico(indice);
    }

    private void AlCambiarResolucion(int indice)
    {
        AjustesJuego.AplicarResolucion(indice);
    }

    private void AlCambiarPantalla(int indice)
    {
        AjustesJuego.AplicarPantallaCompleta(indice == 0);
    }

    private void AlCambiarVolumen(float valor)
    {
        ActualizarTextoVolumen(valor);
        if (!cargandoValores) AjustesJuego.AplicarVolumen(valor);
    }

    private void AlCambiarSensibilidad(float valor)
    {
        ActualizarTextoSensibilidad(valor);
        if (!cargandoValores) AjustesJuego.AplicarSensibilidad(valor);
    }

    private void ActualizarTextoVolumen(float valor)
    {
        if (textoVolumen != null) textoVolumen.text = Mathf.RoundToInt(valor * 100f) + "%";
    }

    private void ActualizarTextoSensibilidad(float valor)
    {
        if (textoSensibilidad != null) textoSensibilidad.text = valor.ToString("0.00");
    }

    // ── Construcción ────────────────────────────────────────────────

    private void Construir()
    {
        constructor = new ConstructorMenuJuego();
        canvas = ConstructorMenuJuego.CrearCanvas(null, "CanvasPausa", 30000);
        ConstructorMenuJuego.CrearVelo(canvas.transform, new Color(0f, 0.01f, 0.02f, 0.78f));

        ConstruirPrincipal();
        ConstruirAjustes();
        ConstruirConfirmar();
        canvas.SetActive(false);
    }

    private void ConstruirPrincipal()
    {
        panelPrincipal = NuevoPanel("PanelPrincipal");
        Text titulo;
        Text subtitulo;
        Image borde;
        Vector2 tamano = new Vector2(560f, 560f);
        Transform t = constructor.CrearTarjeta(panelPrincipal.transform, "Tarjeta", Vector2.zero, tamano,
                                               "[ CONEXIÓN EN ESPERA ]", "PAUSA", Color.white,
                                               NombreNivel(), out titulo, out subtitulo, out borde);

        Vector2 tamanoBoton = new Vector2(420f, 68f);
        botonContinuar = constructor.CrearBoton(t, "BotonContinuar", "CONTINUAR", new Vector2(0f, 50f), tamanoBoton, HackUI.ColAcento);
        botonAjustes = constructor.CrearBoton(t, "BotonAjustes", "AJUSTES", new Vector2(0f, -32f), tamanoBoton, HackUI.ColAcento);
        botonSalir = constructor.CrearBoton(t, "BotonSalir", "MENÚ PRINCIPAL", new Vector2(0f, -114f), tamanoBoton,
                                            ConstructorMenuJuego.ColorMagenta);
        botonContinuar.onClick.AddListener(AlContinuar);
        botonAjustes.onClick.AddListener(AlAjustes);
        botonSalir.onClick.AddListener(AlMenuPrincipal);

        Selectable[] orden = { botonContinuar, botonAjustes, botonSalir };
        ConstructorMenuJuego.ConectarVertical(orden);

        HackUI.CrearTexto(t, "Ayuda", "ESC / START: continuar      A / ENTER: aceptar      B: atrás", 14,
                          TextAnchor.MiddleCenter, new Vector2(0f, -tamano.y / 2f + 34f), new Vector2(520f, 20f),
                          ConstructorMenuJuego.ColorTextoSuave);
    }

    private void ConstruirAjustes()
    {
        panelAjustes = NuevoPanel("PanelAjustes");
        Text titulo;
        Text subtitulo;
        Image borde;
        Vector2 tamano = new Vector2(760f, 760f);
        Transform t = constructor.CrearTarjeta(panelAjustes.transform, "Tarjeta", Vector2.zero, tamano,
                                               "[ CONFIGURACIÓN DEL SISTEMA ]", "AJUSTES", Color.white,
                                               "Puedes cambiarlos sin perder tu progreso", out titulo, out subtitulo, out borde);

        string[] calidades = { "Alta", "Media", "Baja" };
        string[] siNo = { "Activada", "Desactivada" };
        string[] resoluciones = AjustesJuego.ListaResoluciones();
        if (resoluciones == null || resoluciones.Length == 0) resoluciones = new string[] { "—" };

        float y = 160f;
        float paso = 72f;
        selectorCalidad = constructor.CrearFilaSelector(t, "FilaCalidad", "CALIDAD GRÁFICA", new Vector2(0f, y), tamanoFila,
                                                        calidades, 0, true, AlCambiarCalidad);
        selectorResolucion = constructor.CrearFilaSelector(t, "FilaResolucion", "RESOLUCIÓN", new Vector2(0f, y - paso), tamanoFila,
                                                           resoluciones, 0, false, AlCambiarResolucion);
        selectorPantalla = constructor.CrearFilaSelector(t, "FilaPantalla", "PANTALLA COMPLETA", new Vector2(0f, y - paso * 2f),
                                                         tamanoFila, siNo, 0, true, AlCambiarPantalla);
        sliderVolumen = constructor.CrearFilaSlider(t, "FilaVolumen", "VOLUMEN MAESTRO", new Vector2(0f, y - paso * 3f), tamanoFila,
                                                    0f, 1f, 1f, out textoVolumen);
        sliderSensibilidad = constructor.CrearFilaSlider(t, "FilaSensibilidad", "SENSIBILIDAD", new Vector2(0f, y - paso * 4f),
                                                         tamanoFila, 0.02f, 0.5f, 0.15f, out textoSensibilidad);
        sliderVolumen.onValueChanged.AddListener(AlCambiarVolumen);
        sliderSensibilidad.onValueChanged.AddListener(AlCambiarSensibilidad);

        botonVolverAjustes = constructor.CrearBoton(t, "BotonVolver", "< VOLVER", new Vector2(0f, y - paso * 5f - 20f),
                                                    new Vector2(300f, 60f), ConstructorMenuJuego.ColorMagenta);
        botonVolverAjustes.onClick.AddListener(AlVolverAjustes);

        Selectable[] orden =
        {
            selectorCalidad.GetComponent<Selectable>(), selectorResolucion.GetComponent<Selectable>(),
            selectorPantalla.GetComponent<Selectable>(), sliderVolumen, sliderSensibilidad, botonVolverAjustes
        };
        ConstructorMenuJuego.ConectarVertical(orden);

        HackUI.CrearTexto(t, "Ayuda", "↑ ↓ elegir opción      ◀ ▶ cambiar valor      B / ESC: volver", 14,
                          TextAnchor.MiddleCenter, new Vector2(0f, -tamano.y / 2f + 30f), new Vector2(700f, 20f),
                          ConstructorMenuJuego.ColorTextoSuave);
    }

    private void ConstruirConfirmar()
    {
        panelConfirmar = NuevoPanel("PanelConfirmar");
        Text titulo;
        Text subtitulo;
        Image borde;
        Vector2 tamano = new Vector2(620f, 380f);
        Transform t = constructor.CrearTarjeta(panelConfirmar.transform, "Tarjeta", Vector2.zero, tamano,
                                               "[ ADVERTENCIA ]", "¿SALIR AL MENÚ?", ConstructorMenuJuego.ColorMagenta,
                                               "Perderás el progreso de este nivel. Los ajustes se mantienen.",
                                               out titulo, out subtitulo, out borde);
        titulo.fontSize = 42;
        borde.color = new Color(1f, 0.18f, 0.62f, 0.5f);

        Button botonSi = constructor.CrearBoton(t, "BotonSi", "SÍ, SALIR", new Vector2(-140f, -90f), new Vector2(240f, 64f),
                                                ConstructorMenuJuego.ColorMagenta);
        botonCancelarSalida = constructor.CrearBoton(t, "BotonCancelar", "CANCELAR", new Vector2(140f, -90f), new Vector2(240f, 64f),
                                                     HackUI.ColAcento);
        botonSi.onClick.AddListener(AlConfirmarSalida);
        botonCancelarSalida.onClick.AddListener(AlCancelarSalida);

        Navigation navegacionSi = new Navigation();
        navegacionSi.mode = Navigation.Mode.Explicit;
        navegacionSi.selectOnRight = botonCancelarSalida;
        navegacionSi.selectOnLeft = botonCancelarSalida;
        botonSi.navigation = navegacionSi;
        Navigation navegacionNo = new Navigation();
        navegacionNo.mode = Navigation.Mode.Explicit;
        navegacionNo.selectOnLeft = botonSi;
        navegacionNo.selectOnRight = botonSi;
        botonCancelarSalida.navigation = navegacionNo;
    }

    private GameObject NuevoPanel(string nombre)
    {
        GameObject go = HackUI.NuevoUI(canvas.transform, nombre, Vector2.zero, Vector2.zero);
        HackUI.Estirar(go);
        return go;
    }

    private static string NombreNivel()
    {
        string nombre = GameFeedback.NombreNivelActual;
        if (string.IsNullOrEmpty(nombre)) nombre = SceneManager.GetActiveScene().name;
        return nombre.ToUpper();
    }

    // ── Entrada ─────────────────────────────────────────────────────

    /// <summary>ESC (teclado) o START (mando).</summary>
    private bool LeerPausa()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        if (teclado != null && teclado.escapeKey.wasPressedThisFrame) return true;
        Gamepad mando = Gamepad.current;
        if (mando != null && mando.startButton.wasPressedThisFrame) return true;
        return false;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    /// <summary>B (mando) o ESC fuera de la pantalla principal.</summary>
    private bool LeerAtras()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad mando = Gamepad.current;
        if (mando != null && mando.buttonEast.wasPressedThisFrame) return true;
        Keyboard teclado = Keyboard.current;
        if (teclado != null && teclado.escapeKey.wasPressedThisFrame) return true;
        return false;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
