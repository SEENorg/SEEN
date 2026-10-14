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
    public static GameFeedback Instancia { get; private set; }

    /// <summary>Nombre del nivel actual (lo escribe LevelInfo al arrancar).</summary>
    public static string NombreNivelActual { get; set; } = "";
    /// <summary>Escena siguiente a cargar (vacía = nivel final).</summary>
    public static string EscenaSiguiente { get; set; } = "";
    /// <summary>True mientras se muestra la pantalla de nivel completado.</summary>
    public static bool NivelCompletadoVisible { get; private set; }
    /// <summary>True mientras se muestra la pantalla de GAME OVER.</summary>
    public static bool GameOverVisible { get; private set; }

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
        Time.timeScale = 1f;   // por si venimos de una escena pausada
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    private void Update()
    {
        if (!nivelCompletado)
            tiempoNivel += Time.deltaTime;

        // Desvanecer mensajes
        for (int i = mensajes.Count - 1; i >= 0; i--)
        {
            mensajes[i].tiempoRestante -= Time.unscaledDeltaTime;
            if (mensajes[i].tiempoRestante <= 0f)
                mensajes.RemoveAt(i);
        }
    }

    // ── API estatica ─────────────────────────────────────────────

    /// <summary>Muestra un mensaje temporal arriba a la derecha.</summary>
    public static void Mensaje(string texto)
    {
        if (Instancia == null)
        {
            Debug.Log("[Feedback] " + texto);
            return;
        }
        Instancia.AgregarMensaje(texto);
    }

    /// <summary>Muestra la pantalla de nivel completado y pausa el juego.</summary>
    public static void NivelCompletado()
    {
        if (Instancia == null) return;
        Instancia.MostrarPantallaFinal();
    }

    /// <summary>Muestra la pantalla de GAME OVER (el antivirus atrapo al jugador).</summary>
    public static void MostrarGameOver()
    {
        if (Instancia == null) return;
        Instancia.MostrarGameOverInterno();
    }

    /// <summary>
    /// Obtiene la escena siguiente de forma robusta:
    ///  1. El valor estático (lo escribe LevelInfo al arrancar)
    ///  2. El LevelInfo presente en la escena
    ///  3. Derivado del nombre de la escena actual
    /// Devuelve "" si no hay siguiente.
    /// </summary>
    public static string ObtenerSiguienteNivel()
    {
        if (!string.IsNullOrEmpty(EscenaSiguiente))
            return EscenaSiguiente;

        // Buscar el LevelInfo activo en la escena
        if (Instancia != null)
        {
            var info = Object.FindFirstObjectByType<LevelInfo>();
            if (info != null && !string.IsNullOrEmpty(info.escenaSiguiente))
                return info.escenaSiguiente;
        }

        // Derivar del nombre de la escena actual (último recurso)
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

    // ── Interno ──────────────────────────────────────────────────

    private void AgregarMensaje(string texto)
    {
        mensajes.Add(new MsgUI { texto = texto, tiempoRestante = duracionMensaje });
        while (mensajes.Count > maxMensajes)
            mensajes.RemoveAt(0);
    }

    private void MostrarPantallaFinal()
    {
        if (nivelCompletado) return;
        nivelCompletado = true;
        NivelCompletadoVisible = true;

        Time.timeScale = 0f;                    // el juego se detiene
        Cursor.lockState = CursorLockMode.None; // liberar el raton
        Cursor.visible = true;

        // CONGELAR POR COMPLETO la camara y el movimiento del jugador:
        // desactivamos el controlador (nada se mueve salvo el mouse en los botones)
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.enabled = false;

        Debug.Log("[Feedback] NIVEL COMPLETADO");
    }

    private void MostrarGameOverInterno()
    {
        if (gameOverVisible) return;
        gameOverVisible = true;
        GameOverVisible = true;

        // Detener al antivirus y congelar la escena
        var av = Object.FindFirstObjectByType<Antivirus>();
        if (av != null) av.Detener();

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.enabled = false;
    }

    // ── Dibujado ─────────────────────────────────────────────────

    private void OnGUI()
    {
        InicializarEstilos();

        // Mensajes arriba a la derecha
        float y = 12f;
        foreach (var m in mensajes)
        {
            float ancho = Mathf.Max(320f, estiloMensaje.CalcSize(new GUIContent(m.texto)).x + 44f);
            float alto = 46f;
            var rect = new Rect(Screen.width - ancho - 12f, y, ancho, alto);

            Color c = GUI.color;
            float alpha = Mathf.Clamp01(m.tiempoRestante / 1f);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Box(rect, m.texto, estiloMensaje);
            GUI.color = c;

            y += alto + 6f;
        }

        // Pantalla de GAME OVER
        if (gameOverVisible)
        {
            var gFondo = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.color = new Color(0.25f, 0f, 0f, 0.88f);
            GUI.DrawTexture(gFondo, Texture2D.whiteTexture);
            GUI.color = Color.white;

            float gpw = 560f, gph = 320f;
            float gpx = (Screen.width - gpw) / 2f;
            float gpy = (Screen.height - gph) / 2f;
            GUI.Box(new Rect(gpx, gpy, gpw, gph), GUIContent.none, estiloPanel);

            GUI.Label(new Rect(gpx, gpy + 30f, gpw, 70f), "GAME OVER", estiloGameOver);
            GUI.Label(new Rect(gpx, gpy + 104f, gpw, 30f), "El antivirus te alcanzó", estiloMensaje);

            float gbw = 280f, gbh = 56f;
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

        // Pantalla de nivel completado
        if (!nivelCompletado) return;

        // Fondo oscuro semitransparente
        var fondo = new Rect(0f, 0f, Screen.width, Screen.height);
        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(fondo, Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Panel central
        float pw = 560f, ph = 420f;
        float px = (Screen.width - pw) / 2f;
        float py = (Screen.height - ph) / 2f;
        GUI.Box(new Rect(px, py, pw, ph), GUIContent.none, estiloPanel);

        GUI.Label(new Rect(px, py + 30f, pw, 70f), "NIVEL COMPLETADO", estiloTitulo);

        string subtitulo = string.IsNullOrEmpty(NombreNivelActual) ? "" : "   |   " + NombreNivelActual;
        GUI.Label(new Rect(px, py + 104f, pw, 30f),
                  "Tiempo: " + FormatearTiempo(tiempoNivel) + subtitulo, estiloMensaje);

        float bw = 280f, bh = 56f;
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
                Debug.LogWarning("[Feedback] No hay nivel siguiente. Escena actual: '" +
                                 SceneManager.GetActiveScene().name + "'. EscenaSiguiente estatica: '" +
                                 (EscenaSiguiente ?? "null") + "'. Derivado: '" + siguiente + "'");
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
        return GUI.Button(r, texto, hover ? estiloBotonHover : estiloBoton);
    }

    private void InicializarEstiloHover()
    {
        if (estiloBotonHover != null) return;
        estiloBotonHover = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
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
        estiloGameOver = new GUIStyle(GUI.skin.label)
        {
            fontSize = 52,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        estiloGameOver.normal.textColor = new Color(1f, 0.15f, 0.15f);

        // El estilo hover se crea AQUÍ (antes solo se creaba en la ruta de
        // "nivel completado", por eso los botones de GAME OVER se rompían al pasar el mouse)
        estiloBotonHover = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
        estiloBotonHover.normal.textColor = new Color(0.15f, 0.85f, 1f);
        estiloBotonHover.hover.textColor = new Color(0.15f, 0.85f, 1f);

        if (estiloMensaje != null) return;

        estiloMensaje = new GUIStyle(GUI.skin.box)
        {
            fontSize = 17,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };
        estiloMensaje.normal.textColor = new Color(0.75f, 0.95f, 1f);

        estiloPanel = new GUIStyle(GUI.skin.box) { fontSize = 18 };

        estiloTitulo = new GUIStyle(GUI.skin.label)
        {
            fontSize = 44,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        estiloTitulo.normal.textColor = new Color(0.20f, 0.90f, 0.45f);

        estiloBoton = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
    }
}
