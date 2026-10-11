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

    // Estilos cacheados
    private GUIStyle estiloMensaje;
    private GUIStyle estiloPanel;
    private GUIStyle estiloTitulo;
    private GUIStyle estiloBoton;
    private GUIStyle estiloBotonHover;

    private void Awake()
    {
        NivelCompletadoVisible = false;
        Instancia = this;
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

        InicializarEstiloHover();

        if (BotonConHover(new Rect(bx, py + 160f, bw, bh), "SIGUIENTE NIVEL ▶"))
        {
            Time.timeScale = 1f;
            if (!string.IsNullOrEmpty(EscenaSiguiente) && Application.CanStreamedLevelBeLoaded(EscenaSiguiente))
                SceneManager.LoadScene(EscenaSiguiente);
            else
            {
                Debug.LogWarning("[Feedback] No hay nivel siguiente disponible.");
                Time.timeScale = 1f;
                if (Application.CanStreamedLevelBeLoaded("MainMenu"))
                    SceneManager.LoadScene("MainMenu");
            }
        }

        if (BotonConHover(new Rect(bx, py + 228f, bw, bh), "REINTENTAR"))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (BotonConHover(new Rect(bx, py + 296f, bw, bh), "MENU PRINCIPAL"))
        {
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
                SceneManager.LoadScene("MainMenu");
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
