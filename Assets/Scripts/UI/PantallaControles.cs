using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Mini pantalla con TODOS los controles (teclado y mando) que aparece al empezar
/// cada nivel. El juego queda en pausa hasta que el jugador presiona A / ENTER / ESPACIO
/// o hace clic en "COMENZAR".
///
/// No hay que ponerla en ninguna escena: se engancha sola con
/// [RuntimeInitializeOnLoadMethod] y aparece en las escenas de nivel (no en el menú
/// ni en la intro). Al reintentar el mismo nivel no vuelve a salir.
/// </summary>
public class PantallaControles : MonoBehaviour
{
    private const float EsperaMinima = 0.6f;

    private static readonly string[] EscenasSinControles = { "MainMenu", "IntroScene", "SampleScene" };

    /// <summary>
    /// Filas de la tabla: acción, tecla(s) de teclado, botón(es) del mando.
    /// Si cambias un control del juego, cámbialo aquí también.
    /// </summary>
    private static readonly string[,] Controles =
    {
        { "Moverse",                 "W A S D",     "Stick izq." },
        { "Mirar / apuntar",         "Ratón",       "Stick der." },
        { "Saltar",                  "Espacio",     "A" },
        { "Dash",                    "Shift izq.",  "L3" },
        { "Interactuar / hackear",   "E",           "Y" },
        { "Subir en el ascensor",    "F",           "Y" },
        { "Disparar (pistola)",      "Clic izq.",   "X" },
        { "Recargar (pistola)",      "R",           "RB" },
        { "Pantalla del sistema",    "Q",           "LB" },
        { "Tabla de cifrado",        "TAB",         "—" },
        { "Pausa",                   "ESC",         "START" }
    };

    private static bool visible;
    private static string ultimaEscenaMostrada = "";

    /// <summary>True mientras la pantalla está abierta (los HUD y la pausa la respetan).</summary>
    public static bool Visible
    {
        get { return visible; }
    }

    private ConstructorMenuJuego constructor;
    private GameObject canvas;
    private Button botonComenzar;
    private Text textoComenzar;
    private RectTransform tarjeta;
    private PlayerController jugador;
    private bool jugadorEstabaActivo;
    private float tiempoAbierta;
    private bool cerrada;

    // ── Enganche automático ─────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Registrar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        visible = false;
        ultimaEscenaMostrada = "";
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (modo != LoadSceneMode.Single) return;

        for (int i = 0; i < EscenasSinControles.Length; i++)
        {
            if (escena.name == EscenasSinControles[i])
            {
                // Al volver al menú, el próximo nivel vuelve a mostrar los controles
                if (escena.name == "MainMenu") ultimaEscenaMostrada = "";
                return;
            }
        }

        // Reintentar el mismo nivel: no se repite
        if (escena.name == ultimaEscenaMostrada) return;
        // Si Boot va a redirigir al menú (Play desde una escena de nivel), no se muestra
        if (Object.FindFirstObjectByType<PlayerController>() == null) return;

        ultimaEscenaMostrada = escena.name;
        GameObject go = new GameObject("PantallaControles");
        go.AddComponent<PantallaControles>();
    }

    // ── Ciclo de vida ───────────────────────────────────────────────

    private void Start()
    {
        visible = true;
        tiempoAbierta = 0f;

        jugador = Object.FindFirstObjectByType<PlayerController>();
        if (jugador != null)
        {
            jugadorEstabaActivo = jugador.enabled;
            jugador.enabled = false;
        }
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Construir();
    }

    private void OnDestroy()
    {
        if (!cerrada)
        {
            visible = false;
            Time.timeScale = 1f;
        }
        if (constructor != null) constructor.Liberar();
    }

    private void Update()
    {
        if (cerrada) return;
        tiempoAbierta += Time.unscaledDeltaTime;

        // Otro script (p. ej. GameFeedback.Awake) podría reanudar el tiempo: se mantiene la pausa
        Time.timeScale = 0f;

        // Entrada suave de la tarjeta
        float aparicion = Mathf.Clamp01(tiempoAbierta / 0.35f);
        float escala = Mathf.Lerp(0.92f, 1f, 1f - (1f - aparicion) * (1f - aparicion));
        if (tarjeta != null) tarjeta.localScale = new Vector3(escala, escala, 1f);

        // "COMENZAR" parpadea suave
        if (textoComenzar != null)
        {
            float latido = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 4f);
            Color color = textoComenzar.color;
            color.a = latido;
            textoComenzar.color = color;
        }

        // El botón siempre queda seleccionado para el mando
        if (EventSystem.current != null && botonComenzar != null &&
            EventSystem.current.currentSelectedGameObject != botonComenzar.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(botonComenzar.gameObject);
        }

        if (tiempoAbierta >= EsperaMinima && LeerComenzar()) Comenzar();
    }

    private void AlPresionarComenzar()
    {
        if (tiempoAbierta < EsperaMinima) return;
        Comenzar();
    }

    private void Comenzar()
    {
        if (cerrada) return;
        cerrada = true;
        visible = false;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (jugador != null && jugadorEstabaActivo) jugador.enabled = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        Destroy(gameObject);
    }

    /// <summary>A, ENTER o ESPACIO también comienzan (por si el botón no tiene el foco).</summary>
    private bool LeerComenzar()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        if (teclado != null)
        {
            if (teclado.enterKey.wasPressedThisFrame) return true;
            if (teclado.numpadEnterKey.wasPressedThisFrame) return true;
            if (teclado.spaceKey.wasPressedThisFrame) return true;
        }
        Gamepad mando = Gamepad.current;
        if (mando != null)
        {
            if (mando.buttonSouth.wasPressedThisFrame) return true;
            if (mando.startButton.wasPressedThisFrame) return true;
        }
        return false;
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private static bool HayMando()
    {
#if ENABLE_INPUT_SYSTEM
        return Gamepad.current != null;
#else
        return false;
#endif
    }

    // ── Construcción ────────────────────────────────────────────────

    private void Construir()
    {
        constructor = new ConstructorMenuJuego();
        canvas = ConstructorMenuJuego.CrearCanvas(transform, "CanvasControles", 31000);
        ConstructorMenuJuego.CrearVelo(canvas.transform, new Color(0f, 0.01f, 0.02f, 0.82f));

        string nombreNivel = GameFeedback.NombreNivelActual;
        if (string.IsNullOrEmpty(nombreNivel)) nombreNivel = SceneManager.GetActiveScene().name;

        Text titulo;
        Text subtitulo;
        Image borde;
        Vector2 tamano = new Vector2(1000f, 800f);
        Transform t = constructor.CrearTarjeta(canvas.transform, "TarjetaControles", Vector2.zero, tamano,
                                               "[ SEEN // MANUAL DEL OPERADOR ]", "CONTROLES", Color.white,
                                               nombreNivel.ToUpper(), out titulo, out subtitulo, out borde);
        tarjeta = (RectTransform)t;
        subtitulo.color = HackUI.ColAcento;

        ConstruirTabla(t, tamano);

        botonComenzar = constructor.CrearBoton(t, "BotonComenzar", "COMENZAR", new Vector2(0f, -tamano.y / 2f + 78f),
                                               new Vector2(340f, 64f), ConstructorMenuJuego.ColorVerde);
        botonComenzar.onClick.AddListener(AlPresionarComenzar);
        Navigation sinNavegacion = new Navigation();
        sinNavegacion.mode = Navigation.Mode.None;
        botonComenzar.navigation = sinNavegacion;
        Transform textoBoton = botonComenzar.transform.Find("Texto");
        if (textoBoton != null) textoComenzar = textoBoton.GetComponent<Text>();

        HackUI.CrearTexto(t, "Ayuda", "A / ENTER / ESPACIO para comenzar", 14, TextAnchor.MiddleCenter,
                          new Vector2(0f, -tamano.y / 2f + 30f), new Vector2(600f, 20f), ConstructorMenuJuego.ColorTextoSuave);
    }

    private void ConstruirTabla(Transform t, Vector2 tamano)
    {
        float xAccion = -tamano.x / 2f + 60f;
        float xTeclado = 90f;
        float xMando = 320f;
        float yInicio = tamano.y / 2f - 232f;
        float altoFila = 41f;
        bool mando = HayMando();

        Color colorTeclado = HackUI.ColAcento;
        Color colorMando = ConstructorMenuJuego.ColorMagenta;

        // Encabezados (el del mando se resalta si hay uno conectado)
        CrearTextoIzquierda(t, "EncAccion", "ACCIÓN", 14, new Vector2(xAccion, yInicio + 34f), 300f, ConstructorMenuJuego.ColorTextoSuave);
        HackUI.CrearTexto(t, "EncTeclado", "TECLADO", 14, TextAnchor.MiddleCenter,
                          new Vector2(xTeclado, yInicio + 34f), new Vector2(200f, 20f), colorTeclado);
        string textoMando = "MANDO";
        if (mando) textoMando = "MANDO  ● CONECTADO";
        HackUI.CrearTexto(t, "EncMando", textoMando, 14, TextAnchor.MiddleCenter,
                          new Vector2(xMando, yInicio + 34f), new Vector2(240f, 20f), colorMando);

        int filas = Controles.GetLength(0);
        for (int i = 0; i < filas; i++)
        {
            float y = yInicio - i * altoFila;

            // Fila con fondo alterno para que se lea fácil
            if (i % 2 == 0)
            {
                Image fondoFila = constructor.Cajas.CrearRelleno(t, "FondoFila" + i, new Vector2(0f, y),
                                                                 new Vector2(tamano.x - 80f, altoFila - 4f),
                                                                 new Color(1f, 1f, 1f, 0.035f), 2f);
                fondoFila.raycastTarget = false;
            }

            CrearTextoIzquierda(t, "Accion" + i, Controles[i, 0], 19, new Vector2(xAccion, y), 360f, Color.white);
            CrearTecla(t, "Teclado" + i, Controles[i, 1], new Vector2(xTeclado, y), colorTeclado);
            CrearTecla(t, "Mando" + i, Controles[i, 2], new Vector2(xMando, y), colorMando);
        }
    }

    /// <summary>Una "tecla" redondeada con el nombre del control dentro.</summary>
    private void CrearTecla(Transform padre, string nombre, string texto, Vector2 posicion, Color color)
    {
        if (texto == "—")
        {
            HackUI.CrearTexto(padre, nombre, "—", 18, TextAnchor.MiddleCenter, posicion, new Vector2(60f, 30f),
                              ConstructorMenuJuego.ColorTextoSuave);
            return;
        }

        float ancho = Mathf.Max(56f, texto.Length * 11f + 30f);
        Color relleno = new Color(color.r * 0.15f, color.g * 0.15f, color.b * 0.15f, 0.95f);
        Color bordeColor = new Color(color.r, color.g, color.b, 0.7f);
        Image borde = constructor.Cajas.CrearCaja(padre, nombre, posicion, new Vector2(ancho, 32f), relleno, bordeColor, 2f);
        Transform caja = borde.transform.parent;
        caja.GetComponent<Image>().raycastTarget = false;
        Text etiqueta = HackUI.CrearTexto(caja, "Texto", texto, 16, TextAnchor.MiddleCenter, Vector2.zero,
                                          new Vector2(ancho, 32f), Color.white);
        etiqueta.fontStyle = FontStyle.Bold;
    }

    private void CrearTextoIzquierda(Transform padre, string nombre, string texto, int tamano, Vector2 posicionIzquierda,
                                     float ancho, Color color)
    {
        Vector2 centro = new Vector2(posicionIzquierda.x + ancho / 2f, posicionIzquierda.y);
        HackUI.CrearTexto(padre, nombre, texto, tamano, TextAnchor.MiddleLeft, centro, new Vector2(ancho, 30f), color);
    }
}
