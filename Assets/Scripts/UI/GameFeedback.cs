using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Sistema de feedback en pantalla del nivel:
///  - Mensajes temporales arriba a la derecha (checkpoints, avisos)
///  - Pantalla de "NIVEL COMPLETADO" al llegar a la meta, que pausa el juego
///  - Pantalla de "GAME OVER" cuando el antivirus atrapa al jugador
/// Las dos pantallas usan el estilo de tarjeta del menú y se manejan con ratón,
/// teclado y mando (el botón seleccionado crece y se ilumina).
///
/// Uso desde otros scripts (API estática, no requiere referencia en el Inspector):
///   GameFeedback.Mensaje("Checkpoint 1 alcanzado");
///   GameFeedback.NivelCompletado();
///   GameFeedback.MostrarGameOver();
/// </summary>
public class GameFeedback : MonoBehaviour
{
    private static GameFeedback instancia;
    private static string nombreNivelActual = "";
    private static string escenaSiguiente = "";
    private static bool nivelCompletadoVisible;
    private static bool gameOverVisible;

    public static GameFeedback Instancia
    {
        get { return instancia; }
        private set { instancia = value; }
    }

    public static string NombreNivelActual
    {
        get { return nombreNivelActual; }
        set { nombreNivelActual = value; }
    }

    public static string EscenaSiguiente
    {
        get { return escenaSiguiente; }
        set { escenaSiguiente = value; }
    }

    public static bool NivelCompletadoVisible
    {
        get { return nivelCompletadoVisible; }
        private set { nivelCompletadoVisible = value; }
    }

    public static bool GameOverVisible
    {
        get { return gameOverVisible; }
        private set { gameOverVisible = value; }
    }

    [Header("Mensajes")]
    [SerializeField] private float duracionMensaje = 3.5f;
    [SerializeField] private int maxMensajes = 4;

    [Header("Transición")]
    [Tooltip("Reproduce la cinemática de glitch al pasar al siguiente nivel.")]
    [SerializeField] private bool cinematicaAlAvanzar = true;

    private class MensajeUI
    {
        public string texto;
        public float tiempoRestante;
        public float aparicion;
    }

    private readonly List<MensajeUI> mensajes = new List<MensajeUI>();
    private bool nivelCompletado;
    private bool gameOver;
    private float tiempoNivel;
    private PincelHUD pincel;

    // Pantallas finales (uGUI)
    private ConstructorMenuJuego constructor;
    private GameObject canvasFinal;
    private Button botonPrincipal;
    private Text tituloFinal;
    private float tiempoPantallaFinal;
    private bool accionHecha;

    private void Awake()
    {
        Instancia = this;
        NivelCompletadoVisible = false;
        GameOverVisible = false;
        nivelCompletado = false;
        gameOver = false;
        tiempoNivel = 0f;
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
        if (pincel != null) pincel.Liberar();
        if (constructor != null) constructor.Liberar();
    }

    private void Update()
    {
        if (!nivelCompletado && !gameOver) tiempoNivel += Time.deltaTime;

        float dt = Time.unscaledDeltaTime;
        for (int i = mensajes.Count - 1; i >= 0; i--)
        {
            mensajes[i].tiempoRestante -= dt;
            mensajes[i].aparicion = Mathf.MoveTowards(mensajes[i].aparicion, 1f, dt * 5f);
            if (mensajes[i].tiempoRestante <= 0f) mensajes.RemoveAt(i);
        }

        if (canvasFinal != null) AnimarPantallaFinal(dt);
    }

    // ── API estática ────────────────────────────────────────────────

    public static void Mensaje(string texto)
    {
        if (Instancia == null)
        {
            Debug.Log("[Feedback] " + texto);
            return;
        }
        Instancia.AgregarMensaje(texto);
    }

    public static void NivelCompletado()
    {
        if (Instancia == null) return;
        Instancia.MostrarPantallaFinal();
    }

    public static void MostrarGameOver()
    {
        if (Instancia == null) return;
        Instancia.MostrarGameOverInterno();
    }

    public static string ObtenerSiguienteNivel()
    {
        if (!string.IsNullOrEmpty(EscenaSiguiente)) return EscenaSiguiente;

        if (Instancia != null)
        {
            LevelInfo info = Object.FindFirstObjectByType<LevelInfo>();
            if (info != null && !string.IsNullOrEmpty(info.escenaSiguiente)) return info.escenaSiguiente;
        }

        string actual = SceneManager.GetActiveScene().name;
        switch (actual)
        {
            case "Juego": return "Nivel2";
            case "Nivel2": return "Nivel3";
            case "Nivel3": return "Nivel4";
            case "Nivel4": return "Nivel5";
            default: return "";
        }
    }

    // ── Mensajes ────────────────────────────────────────────────────

    private void AgregarMensaje(string texto)
    {
        MensajeUI nuevo = new MensajeUI();
        nuevo.texto = texto;
        nuevo.tiempoRestante = duracionMensaje;
        nuevo.aparicion = 0f;
        mensajes.Add(nuevo);
        while (mensajes.Count > maxMensajes) mensajes.RemoveAt(0);

        // Integración: notificación del sistema en la Cola (FIFO)
        EstructurasDatosJuego datos = EstructurasDatosJuego.Instancia;
        if (datos != null) datos.EncolarNotificacion(texto);
    }

    private void OnGUI()
    {
        if (mensajes.Count == 0 || nivelCompletado || gameOver || PantallaControles.Visible) return;
        if (pincel == null) pincel = new PincelHUD();
        pincel.EmpezarEscala();

        float anchoPantalla = PincelHUD.AnchoReferencia;
        float y = 24f;
        for (int i = 0; i < mensajes.Count; i++)
        {
            MensajeUI m = mensajes[i];
            float ancho = Mathf.Max(340f, pincel.AnchoTexto(m.texto, 17, true) + 60f);
            float alto = 48f;
            float alfa = Mathf.Clamp01(m.tiempoRestante / 0.6f) * m.aparicion;
            float entrada = (1f - m.aparicion) * 60f;
            Rect rect = new Rect(anchoPantalla - ancho - 24f + entrada, y, ancho, alto);

            pincel.Caja(rect, new Color(0.02f, 0.04f, 0.06f, 0.88f * alfa));
            pincel.Borde(rect, new Color(0.15f, 0.85f, 1f, 0.6f * alfa));
            pincel.Rectangulo(new Rect(rect.x + 10f, rect.y + 12f, 3f, alto - 24f), new Color(1f, 0.18f, 0.62f, alfa));
            pincel.Texto(new Rect(rect.x + 24f, rect.y, ancho - 36f, alto), m.texto, 17,
                         new Color(0.85f, 0.97f, 1f, alfa), TextAnchor.MiddleLeft, true);
            y += alto + 8f;
        }

        pincel.TerminarEscala();
    }

    // ── Pantallas finales ───────────────────────────────────────────

    private void MostrarPantallaFinal()
    {
        if (nivelCompletado || gameOver) return;
        nivelCompletado = true;
        NivelCompletadoVisible = true;
        PausarJuego();
        ConstruirNivelCompletado();
        Debug.Log("[Feedback] NIVEL COMPLETADO");
    }

    private void MostrarGameOverInterno()
    {
        if (gameOver || nivelCompletado) return;
        gameOver = true;
        GameOverVisible = true;

        Antivirus antivirus = Object.FindFirstObjectByType<Antivirus>();
        if (antivirus != null) antivirus.Detener();

        PausarJuego();
        ConstruirGameOver();
    }

    private void PausarJuego()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.enabled = false;
    }

    private void ConstruirNivelCompletado()
    {
        PrepararCanvas(new Color(0f, 0.02f, 0.01f, 0.8f));

        string nombre = NombreNivelActual;
        if (string.IsNullOrEmpty(nombre)) nombre = SceneManager.GetActiveScene().name;

        Text subtitulo;
        Image borde;
        Vector2 tamano = new Vector2(640f, 640f);
        Transform t = constructor.CrearTarjeta(canvasFinal.transform, "TarjetaFinal", Vector2.zero, tamano,
                                               "[ ACCESO TOTAL CONSEGUIDO ]", "NIVEL COMPLETADO",
                                               ConstructorMenuJuego.ColorVerde, nombre.ToUpper(),
                                               out tituloFinal, out subtitulo, out borde);
        borde.color = new Color(0.2f, 1f, 0.5f, 0.55f);

        // Estadísticas
        int modulos = 0;
        if (EstructurasDatosJuego.Instancia != null) modulos = EstructurasDatosJuego.Instancia.TotalModulosResueltos;
        CrearDato(t, "DatoTiempo", "TIEMPO", FormatearTiempo(tiempoNivel), new Vector2(-130f, 80f));
        CrearDato(t, "DatoModulos", "MÓDULOS HACKEADOS", modulos.ToString(), new Vector2(130f, 80f));

        // Botones
        string siguiente = ObtenerSiguienteNivel();
        bool haySiguiente = !string.IsNullOrEmpty(siguiente) && Application.CanStreamedLevelBeLoaded(siguiente);
        string textoSiguiente = "SIGUIENTE NIVEL  ▶";
        if (!haySiguiente) textoSiguiente = "FINALIZAR";

        Vector2 tamanoBoton = new Vector2(420f, 64f);
        Button botonSiguiente = constructor.CrearBoton(t, "BotonSiguiente", textoSiguiente, new Vector2(0f, -30f), tamanoBoton,
                                                       ConstructorMenuJuego.ColorVerde);
        Button botonReintentar = constructor.CrearBoton(t, "BotonReintentar", "REINTENTAR", new Vector2(0f, -108f), tamanoBoton,
                                                        HackUI.ColAcento);
        Button botonMenu = constructor.CrearBoton(t, "BotonMenu", "MENÚ PRINCIPAL", new Vector2(0f, -186f), tamanoBoton,
                                                  ConstructorMenuJuego.ColorMagenta);
        botonSiguiente.onClick.AddListener(AlSiguienteNivel);
        botonReintentar.onClick.AddListener(AlReintentar);
        botonMenu.onClick.AddListener(AlMenuPrincipal);

        Selectable[] orden = { botonSiguiente, botonReintentar, botonMenu };
        ConstructorMenuJuego.ConectarVertical(orden);
        botonPrincipal = botonSiguiente;

        CrearAyuda(t, tamano);
        SeleccionarPrincipal();
    }

    private void ConstruirGameOver()
    {
        PrepararCanvas(new Color(0.18f, 0f, 0.02f, 0.82f));

        Text subtitulo;
        Image borde;
        Vector2 tamano = new Vector2(620f, 480f);
        Transform t = constructor.CrearTarjeta(canvasFinal.transform, "TarjetaFinal", Vector2.zero, tamano,
                                               "[ CONEXIÓN PERDIDA ]", "GAME OVER", ConstructorMenuJuego.ColorRojo,
                                               "El antivirus te alcanzó", out tituloFinal, out subtitulo, out borde);
        tituloFinal.fontSize = 64;
        borde.color = new Color(1f, 0.25f, 0.3f, 0.6f);

        Vector2 tamanoBoton = new Vector2(400f, 64f);
        Button botonReintentar = constructor.CrearBoton(t, "BotonReintentar", "REINTENTAR", new Vector2(0f, -30f), tamanoBoton,
                                                        HackUI.ColAcento);
        Button botonMenu = constructor.CrearBoton(t, "BotonMenu", "MENÚ PRINCIPAL", new Vector2(0f, -108f), tamanoBoton,
                                                  ConstructorMenuJuego.ColorMagenta);
        botonReintentar.onClick.AddListener(AlReintentar);
        botonMenu.onClick.AddListener(AlMenuPrincipal);

        Selectable[] orden = { botonReintentar, botonMenu };
        ConstructorMenuJuego.ConectarVertical(orden);
        botonPrincipal = botonReintentar;

        CrearAyuda(t, tamano);
        SeleccionarPrincipal();
    }

    private void PrepararCanvas(Color colorVelo)
    {
        if (constructor == null) constructor = new ConstructorMenuJuego();
        if (canvasFinal != null) Destroy(canvasFinal);
        canvasFinal = ConstructorMenuJuego.CrearCanvas(null, "CanvasFinNivel", 30500);
        ConstructorMenuJuego.CrearVelo(canvasFinal.transform, colorVelo);
        tiempoPantallaFinal = 0f;
        accionHecha = false;
    }

    /// <summary>Caja con un dato grande (tiempo, módulos).</summary>
    private void CrearDato(Transform padre, string nombre, string etiqueta, string valor, Vector2 posicion)
    {
        Image borde = constructor.Cajas.CrearCaja(padre, nombre, posicion, new Vector2(230f, 96f),
                                                  new Color(0.02f, 0.07f, 0.06f, 0.9f), new Color(0.2f, 1f, 0.5f, 0.35f), 1.4f);
        Transform caja = borde.transform.parent;
        caja.GetComponent<Image>().raycastTarget = false;
        HackUI.CrearTexto(caja, "Etiqueta", etiqueta, 13, TextAnchor.MiddleCenter, new Vector2(0f, 28f), new Vector2(220f, 20f),
                          ConstructorMenuJuego.ColorTextoSuave);
        Text texto = HackUI.CrearTexto(caja, "Valor", valor, 40, TextAnchor.MiddleCenter, new Vector2(0f, -10f),
                                       new Vector2(220f, 50f), Color.white);
        texto.fontStyle = FontStyle.Bold;
    }

    private void CrearAyuda(Transform padre, Vector2 tamano)
    {
        HackUI.CrearTexto(padre, "Ayuda", "↑ ↓ elegir      A / ENTER / clic: aceptar", 14, TextAnchor.MiddleCenter,
                          new Vector2(0f, -tamano.y / 2f + 30f), new Vector2(tamano.x - 40f, 20f),
                          ConstructorMenuJuego.ColorTextoSuave);
    }

    private void SeleccionarPrincipal()
    {
        if (EventSystem.current == null || botonPrincipal == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(botonPrincipal.gameObject);
    }

    /// <summary>La tarjeta entra creciendo, el título late y la selección no se pierde (mando).</summary>
    private void AnimarPantallaFinal(float dt)
    {
        tiempoPantallaFinal += dt;
        if (tituloFinal != null)
        {
            Transform tarjeta = tituloFinal.transform.parent;
            float aparicion = Mathf.Clamp01(tiempoPantallaFinal / 0.35f);
            float escala = Mathf.Lerp(0.9f, 1f, 1f - (1f - aparicion) * (1f - aparicion));
            tarjeta.localScale = new Vector3(escala, escala, 1f);

            // GAME OVER tiembla de vez en cuando, como una señal que se corta
            float salto = 0f;
            if (gameOver && Random.value < 0.04f) salto = Random.Range(-8f, 8f);
            RectTransform rt = (RectTransform)tituloFinal.transform;
            rt.anchoredPosition = new Vector2(salto, rt.anchoredPosition.y);
        }

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            SeleccionarPrincipal();
    }

    // ── Botones ─────────────────────────────────────────────────────

    private bool PuedeActuar()
    {
        // Evita que el mismo clic o botón que terminó el nivel active un botón
        if (accionHecha || tiempoPantallaFinal < 0.4f) return false;
        accionHecha = true;
        return true;
    }

    private void AlSiguienteNivel()
    {
        if (!PuedeActuar()) return;
        string siguiente = ObtenerSiguienteNivel();

        if (!string.IsNullOrEmpty(siguiente) && Application.CanStreamedLevelBeLoaded(siguiente))
        {
            Debug.Log("[Feedback] Avanzando de '" + SceneManager.GetActiveScene().name + "' a '" + siguiente + "'");
            if (cinematicaAlAvanzar)
            {
                CinematicaGlitch.ReproducirEscena(siguiente, CinematicaGlitch.NumeroDeEscena(siguiente), "");
            }
            else
            {
                Time.timeScale = 1f;
                Boot.PermitirInicioEscena = true;
                SceneManager.LoadScene(siguiente);
            }
            return;
        }

        Debug.LogWarning("[Feedback] No hay nivel siguiente desde '" + SceneManager.GetActiveScene().name + "'. Volviendo al menú.");
        IrAlMenu();
    }

    private void AlReintentar()
    {
        if (!PuedeActuar()) return;
        Time.timeScale = 1f;
        Boot.PermitirInicioEscena = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void AlMenuPrincipal()
    {
        if (!PuedeActuar()) return;
        IrAlMenu();
    }

    private void IrAlMenu()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
        {
            Boot.PermitirInicioEscena = true;
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            Debug.LogWarning("[Feedback] No existe la escena 'MainMenu'. Ejecuta SEEN / Construir Menu Principal.");
            accionHecha = false;
        }
    }

    private string FormatearTiempo(float t)
    {
        int minutos = Mathf.FloorToInt(t / 60f);
        int segundos = Mathf.FloorToInt(t % 60f);
        return minutos.ToString("00") + ":" + segundos.ToString("00");
    }
}
