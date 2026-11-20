using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sistema de feedback en pantalla del nivel:
///  - Mensajes temporales arriba a la derecha (checkpoints, avisos)
///  - Pantalla de "NIVEL COMPLETADO" al llegar a la meta, que pausa el juego
///
/// Uso desde otros scripts (API estatica, no requiere referencia en el Inspector):
///   GameFeedback.Mensaje("Checkpoint 1 alcanzado");
///   GameFeedback.NivelCompletado();
/// </summary>
public class GameFeedback : MonoBehaviour
{
    private static GameFeedback _instancia;

    public static GameFeedback Instancia
    {
        get { return _instancia; }
        private set { _instancia = value; }
    }

    private static string _nombreNivelActual = "";
    public static string NombreNivelActual
    {
        get { return _nombreNivelActual; }
        set { _nombreNivelActual = value; }
    }

    private static string _escenaSiguiente = "";
    public static string EscenaSiguiente
    {
        get { return _escenaSiguiente; }
        set { _escenaSiguiente = value; }
    }

    private static bool _nivelCompletadoVisible;
    public static bool NivelCompletadoVisible
    {
        get { return _nivelCompletadoVisible; }
        private set { _nivelCompletadoVisible = value; }
    }

    private static bool _gameOverVisible;
    public static bool GameOverVisible
    {
        get { return _gameOverVisible; }
        private set { _gameOverVisible = value; }
    }

    [Header("Mensajes")]
    [SerializeField] private float duracionMensaje = 3.5f;
    [SerializeField] private int maxMensajes = 4;

    private class MsgUI
    {
        public string texto;
        public float tiempoRestante;
    }

    private readonly List<MsgUI> mensajes = new List<MsgUI>();
    private bool nivelCompletado;
    private float tiempoNivel;
    private bool gameOverVisible;
    // Estilos cacheados
    private GUIStyle estiloMensaje;
    private GUIStyle estiloPanel;
    private GUIStyle estiloTitulo;
    private GUIStyle estiloBoton;
    private GUIStyle estiloBotonHover;
    private GUIStyle estiloGameOver;

    private void Awake()
    {
        Instancia = this;
        NivelCompletadoVisible = false;
        GameOverVisible = false;
        gameOverVisible = false;
        nivelCompletado = false;
        tiempoNivel = 0f;
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    private void Update()
    {
        if (!nivelCompletado)
            tiempoNivel += Time.deltaTime;

        for (int i = mensajes.Count - 1; i >= 0; i--)
        {
            mensajes[i].tiempoRestante -= Time.unscaledDeltaTime;
            if (mensajes[i].tiempoRestante <= 0f)
                mensajes.RemoveAt(i);
        }
    }

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
        if (!string.IsNullOrEmpty(EscenaSiguiente))
            return EscenaSiguiente;

        if (Instancia != null)
        {
            LevelInfo info = Object.FindFirstObjectByType<LevelInfo>();
            if (info != null && !string.IsNullOrEmpty(info.escenaSiguiente))
                return info.escenaSiguiente;
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

    private void AgregarMensaje(string texto)
    {
        MsgUI nuevo = new MsgUI();
        nuevo.texto = texto;
        nuevo.tiempoRestante = duracionMensaje;
        mensajes.Add(nuevo);
        while (mensajes.Count > maxMensajes)
            mensajes.RemoveAt(0);

        // ── Integración: Encolar notificación del sistema en Cola (FIFO) ──
        var edNotif = EstructurasDatosJuego.Instancia;
        if (edNotif != null) edNotif.EncolarNotificacion(texto);
    }

    private void MostrarPantallaFinal()
    {
        if (nivelCompletado) return;
        nivelCompletado = true;
        NivelCompletadoVisible = true;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.enabled = false;

        Debug.Log("[Feedback] NIVEL COMPLETADO");
    }

    private void MostrarGameOverInterno()
    {
        if (gameOverVisible) return;
        gameOverVisible = true;
        GameOverVisible = true;

        Antivirus av = Object.FindFirstObjectByType<Antivirus>();
        if (av != null) av.Detener();

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.enabled = false;
    }

    private void OnGUI()
    {
        InicializarEstilos();

        float y = 12f;
        foreach (MsgUI m in mensajes)
        {
            float ancho = Mathf.Max(320f, estiloMensaje.CalcSize(new GUIContent(m.texto)).x + 44f);
            float alto = 46f;
            Rect rect = new Rect(Screen.width - ancho - 12f, y, ancho, alto);

            Color c = GUI.color;
            float alpha = Mathf.Clamp01(m.tiempoRestante / 1f);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Box(rect, m.texto, estiloMensaje);
            GUI.color = c;

            y += alto + 6f;
        }

        if (gameOverVisible)
        {
            Rect gFondo = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.color = new Color(0.25f, 0f, 0f, 0.88f);
            GUI.DrawTexture(gFondo, Texture2D.whiteTexture);
            GUI.color = Color.white;

            float gpw = 560f;
            float gph = 320f;
            float gpx = (Screen.width - gpw) / 2f;
            float gpy = (Screen.height - gph) / 2f;
            GUI.Box(new Rect(gpx, gpy, gpw, gph), GUIContent.none, estiloPanel);

            GUI.Label(new Rect(gpx, gpy + 30f, gpw, 70f), "GAME OVER", estiloGameOver);
            GUI.Label(new Rect(gpx, gpy + 104f, gpw, 30f), "El antivirus te alcanzó", estiloMensaje);

            float gbw = 280f;
            float gbh = 56f;
            float gbx = gpx + (gpw - gbw) / 2f;

            if (BotonConHover(new Rect(gbx, gpy + 156f, gbw, gbh), "REINTENTAR"))
            {
                Time.timeScale = 1f;
                Boot.PermitirInicioEscena = true;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            if (BotonConHover(new Rect(gbx, gpy + 224f, gbw, gbh), "MENÚ PRINCIPAL"))
            {
                Time.timeScale = 1f;
                Boot.PermitirInicioEscena = true;
                if (Application.CanStreamedLevelBeLoaded("MainMenu"))
                    SceneManager.LoadScene("MainMenu");
            }
            return;
        }

        if (!nivelCompletado) return;

        Rect fondo = new Rect(0f, 0f, Screen.width, Screen.height);
        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(fondo, Texture2D.whiteTexture);
        GUI.color = Color.white;

        float pw = 560f;
        float ph = 420f;
        float px = (Screen.width - pw) / 2f;
        float py = (Screen.height - ph) / 2f;
        GUI.Box(new Rect(px, py, pw, ph), GUIContent.none, estiloPanel);

        GUI.Label(new Rect(px, py + 30f, pw, 70f), "NIVEL COMPLETADO", estiloTitulo);

        string subtitulo;
        if (string.IsNullOrEmpty(NombreNivelActual))
            subtitulo = "";
        else
            subtitulo = "   |   " + NombreNivelActual;

        GUI.Label(new Rect(px, py + 104f, pw, 30f),
                  "Tiempo: " + FormatearTiempo(tiempoNivel) + subtitulo, estiloMensaje);

        float bw = 280f;
        float bh = 56f;
        float bx = px + (pw - bw) / 2f;

        if (BotonConHover(new Rect(bx, py + 160f, bw, bh), "SIGUIENTE NIVEL ▶"))
        {
            Time.timeScale = 1f;
            string siguiente = ObtenerSiguienteNivel();

            if (!string.IsNullOrEmpty(siguiente) && Application.CanStreamedLevelBeLoaded(siguiente))
            {
                Debug.Log("[Feedback] Avanzando de '" + SceneManager.GetActiveScene().name + "' a '" + siguiente + "'");
                Boot.PermitirInicioEscena = true;
                SceneManager.LoadScene(siguiente);
            }
            else
            {
                string escenaSiguienteTexto;
                if (EscenaSiguiente != null)
                    escenaSiguienteTexto = EscenaSiguiente;
                else
                    escenaSiguienteTexto = "null";

                Debug.LogWarning("[Feedback] No hay nivel siguiente. Escena actual: '" +
                                 SceneManager.GetActiveScene().name + "'. EscenaSiguiente estatica: '" +
                                 escenaSiguienteTexto + "'. Derivado: '" + siguiente + "'");
                Time.timeScale = 1f;
                if (Application.CanStreamedLevelBeLoaded("MainMenu"))
                {
                    Boot.PermitirInicioEscena = true;
                    SceneManager.LoadScene("MainMenu");
                }
            }
        }

        if (BotonConHover(new Rect(bx, py + 228f, bw, bh), "REINTENTAR"))
        {
            Time.timeScale = 1f;
            Boot.PermitirInicioEscena = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (BotonConHover(new Rect(bx, py + 296f, bw, bh), "MENU PRINCIPAL"))
        {
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            {
                Boot.PermitirInicioEscena = true;
                SceneManager.LoadScene("MainMenu");
            }
            else
                Debug.LogWarning("[Feedback] No existe la escena 'MainMenu'. " +
                                 "Ejecuta SEEN / Construir Menu Principal.");
        }
    }

    /// <summary>Botón OnGUI con feedback visual al pasar el mouse.</summary>
    private bool BotonConHover(Rect r, string texto)
    {
        bool hover = r.Contains(Event.current.mousePosition);
        if (hover)
            return GUI.Button(r, texto, estiloBotonHover);
        else
            return GUI.Button(r, texto, estiloBoton);
    }

    private void InicializarEstiloHover()
    {
        if (estiloBotonHover != null) return;
        estiloBotonHover = new GUIStyle(GUI.skin.button);
        estiloBotonHover.fontSize = 20;
        estiloBotonHover.fontStyle = FontStyle.Bold;
        estiloBotonHover.normal.textColor = new Color(0.15f, 0.85f, 1f);
        estiloBotonHover.hover.textColor = new Color(0.15f, 0.85f, 1f);
    }

    private string FormatearTiempo(float t)
    {
        int min = Mathf.FloorToInt(t / 60f);
        int seg = Mathf.FloorToInt(t % 60f);
        return string.Format("{0:00}:{1:00}", min, seg);
    }

    private void InicializarEstilos()
    {
        estiloGameOver = new GUIStyle(GUI.skin.label);
        estiloGameOver.fontSize = 52;
        estiloGameOver.fontStyle = FontStyle.Bold;
        estiloGameOver.alignment = TextAnchor.MiddleCenter;
        estiloGameOver.normal.textColor = new Color(1f, 0.15f, 0.15f);

        estiloBotonHover = new GUIStyle(GUI.skin.button);
        estiloBotonHover.fontSize = 20;
        estiloBotonHover.fontStyle = FontStyle.Bold;
        estiloBotonHover.normal.textColor = new Color(0.15f, 0.85f, 1f);
        estiloBotonHover.hover.textColor = new Color(0.15f, 0.85f, 1f);

        if (estiloMensaje != null) return;

        estiloMensaje = new GUIStyle(GUI.skin.box);
        estiloMensaje.fontSize = 17;
        estiloMensaje.alignment = TextAnchor.MiddleCenter;
        estiloMensaje.fontStyle = FontStyle.Bold;
        estiloMensaje.wordWrap = true;
        estiloMensaje.normal.textColor = new Color(0.75f, 0.95f, 1f);

        estiloPanel = new GUIStyle(GUI.skin.box);
        estiloPanel.fontSize = 18;

        estiloTitulo = new GUIStyle(GUI.skin.label);
        estiloTitulo.fontSize = 44;
        estiloTitulo.fontStyle = FontStyle.Bold;
        estiloTitulo.alignment = TextAnchor.MiddleCenter;
        estiloTitulo.normal.textColor = new Color(0.20f, 0.90f, 0.45f);

        estiloBoton = new GUIStyle(GUI.skin.button);
        estiloBoton.fontSize = 20;
        estiloBoton.fontStyle = FontStyle.Bold;
    }
}