using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Menú de pausa (Esc durante el juego):
///   • CONTINUAR    → vuelve al juego sin perder nada
///   • AJUSTES      → gráficos/resolución/volumen/sensibilidad SIN perder progreso
///   • MENÚ         → vuelve al menú principal (se pierde el progreso, con confirmación)
/// Mientras está abierto, el juego queda en pausa real y el ratón libre.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    /// <summary>True mientras el menú de pausa está abierto.</summary>
    public static bool Pausado { get; private set; }

    private PlayerController pc;

    // Estado de paneles
    private bool panelAbierto;
    private bool enAjustes;
    private bool confirmandoSalida;

    // Estado de ajustes
    private int perfilGrafico;
    private int indiceResolucion;
    private bool pantallaCompleta;
    private float volumen;
    private float sensibilidad;
    private string[] resoluciones;

    // Estilos
    private GUIStyle estTitulo, estBoton, estBotonChico, etiqueta, estTituloAjustes;
    private bool estilosListos;

    private void Awake()
    {
        pc = GetComponent<PlayerController>();
    }

    private void Update()
    {
        // No abrir pausa durante hackeo o pantalla de nivel completado
        if (HackManager.Abierto || GameFeedback.NivelCompletadoVisible)
            return;

        if (LeerEscape())
        {
            if (!panelAbierto)
                AbrirPausa();
            else if (enAjustes)
                enAjustes = false;
            else if (confirmandoSalida)
                confirmandoSalida = false;
            else
                CerrarPausa();
        }
    }

    // ── Apertura / cierre ────────────────────────────────────────

    private void AbrirPausa()
    {
        panelAbierto = true;
        Pausado = true;
        enAjustes = false;
        confirmandoSalida = false;

        // Snapshot de ajustes actuales
        perfilGrafico = AjustesJuego.PerfilGuardado;
        indiceResolucion = AjustesJuego.IndiceResolucionGuardado;
        pantallaCompleta = AjustesJuego.PantallaCompletaGuardada;
        volumen = AjustesJuego.VolumenGuardado;
        sensibilidad = AjustesJuego.SensibilidadGuardada;
        resoluciones = AjustesJuego.ListaResoluciones();
        if (indiceResolucion >= resoluciones.Length) indiceResolucion = 0;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (pc != null) pc.enabled = false;
    }

    private void CerrarPausa()
    {
        panelAbierto = false;
        Pausado = false;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (pc != null) pc.enabled = true;
    }

    // ── Dibujado ─────────────────────────────────────────────────

    private void OnGUI()
    {
        if (!panelAbierto) return;
        InicializarEstilos();

        // Fondo oscuro
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (confirmandoSalida)
        {
            DibujarConfirmacion();
            return;
        }

        float pw = 520f, ph = enAjustes ? 620f : 420f;
        float px = (Screen.width - pw) / 2f;
        float py = (Screen.height - ph) / 2f;

        GUI.Box(new Rect(px, py, pw, ph), GUIContent.none, estBoton);

        if (!enAjustes)
        {
            GUI.Label(new Rect(px, py + 24f, pw, 60f), "PAUSA", estTitulo);

            if (Boton(new Rect(px + 60f, py + 110f, pw - 120f, 56f), "CONTINUAR"))
                CerrarPausa();

            if (Boton(new Rect(px + 60f, py + 182f, pw - 120f, 56f), "AJUSTES"))
                enAjustes = true;

            if (Boton(new Rect(px + 60f, py + 254f, pw - 120f, 56f), "MENÚ PRINCIPAL"))
                confirmandoSalida = true;

            GUI.Label(new Rect(px, py + ph - 50f, pw, 30f), "Esc para reanudar", etiqueta);
        }
        else
        {
            DibujarAjustes(px, py, pw);
        }
    }

    private void DibujarAjustes(float px, float py, float pw)
    {
        GUI.Label(new Rect(px, py + 22f, pw, 50f), "A J U S T E S", estTituloAjustes);
        GUI.Label(new Rect(px, py + 68f, pw, 26f),
                  "Podés ajustar sin perder tu progreso", etiqueta);

        float x = px + 40f;
        float ancho = pw - 80f;

        // ── Calidad gráfica ──
        GUI.Label(new Rect(x, py + 110f, ancho, 26f), "CALIDAD GRÁFICA", etiqueta);
        string[] etiquetas = { "Alta", "Media", "Baja" };
        int nuevoPerfil = GUI.Toolbar(new Rect(x, py + 140f, ancho, 44f), perfilGrafico, etiquetas, estBotonChico);
        if (nuevoPerfil != perfilGrafico)
        {
            perfilGrafico = nuevoPerfil;
            AjustesJuego.AplicarPerfilGrafico(perfilGrafico);
        }

        // ── Resolución ──
        GUI.Label(new Rect(x, py + 200f, ancho, 26f), "RESOLUCIÓN", etiqueta);
        float anchoBoton = 50f;
        bool cambioRes = false;
        // ◀ = resolución más alta (índice menor), ▶ = más baja
        if (Boton(new Rect(x, py + 230f, anchoBoton, 44f), "◀") && indiceResolucion > 0)
        {
            indiceResolucion--;
            cambioRes = true;
        }
        if (Boton(new Rect(x + ancho - anchoBoton, py + 230f, anchoBoton, 44f), "▶") &&
            indiceResolucion < resoluciones.Length - 1)
        {
            indiceResolucion++;
            cambioRes = true;
        }
        GUI.Box(new Rect(x + anchoBoton + 8f, py + 230f, ancho - anchoBoton * 2f - 16f, 44f),
                resoluciones[indiceResolucion], estBotonChico);
        if (cambioRes)
            AjustesJuego.AplicarResolucion(indiceResolucion);

        // ── Pantalla completa ──
        bool nuevaPantalla = GUI.Toggle(new Rect(x, py + 296f, 28f, 28f), pantallaCompleta, "");
        if (nuevaPantalla != pantallaCompleta)
        {
            pantallaCompleta = nuevaPantalla;
            AjustesJuego.AplicarPantallaCompleta(pantallaCompleta);
        }
        GUI.Label(new Rect(x + 40f, py + 294f, ancho - 40f, 28f), "PANTALLA COMPLETA", etiqueta);

        // ── Volumen ──
        GUI.Label(new Rect(x, py + 344f, ancho, 26f),
                  $"VOLUMEN MAESTRO   {Mathf.RoundToInt(volumen * 100)}%", etiqueta);
        float nuevoVol = GUI.HorizontalSlider(new Rect(x, py + 372f, ancho, 26f), volumen, 0f, 1f);
        if (!Mathf.Approximately(nuevoVol, volumen))
        {
            volumen = nuevoVol;
            AjustesJuego.AplicarVolumen(volumen);
        }

        // ── Sensibilidad ──
        GUI.Label(new Rect(x, py + 408f, ancho, 26f),
                  $"SENSIBILIDAD RATÓN   {sensibilidad:0.00}", etiqueta);
        float nuevaSens = GUI.HorizontalSlider(new Rect(x, py + 436f, ancho, 26f), sensibilidad, 0.02f, 0.5f);
        if (!Mathf.Approximately(nuevaSens, sensibilidad))
        {
            sensibilidad = nuevaSens;
            AjustesJuego.AplicarSensibilidad(sensibilidad);
        }

        // ── Volver ──
        if (Boton(new Rect(x, py + 490f, ancho, 52f), "VOLVER AL JUEGO"))
            CerrarPausa();

        if (Boton(new Rect(x, py + 552f, ancho, 46f), "MENÚ PRINCIPAL"))
            confirmandoSalida = true;
    }

    private void DibujarConfirmacion()
    {
        float pw = 480f, ph = 240f;
        float px = (Screen.width - pw) / 2f;
        float py = (Screen.height - ph) / 2f;

        GUI.Box(new Rect(px, py, pw, ph), GUIContent.none, estBoton);
        GUI.Label(new Rect(px, py + 26f, pw, 40f), "¿SALIR AL MENÚ PRINCIPAL?", estTituloAjustes);
        GUI.Label(new Rect(px, py + 74f, pw, 50f),
                  "Perderás el progreso de este nivel.\nLos ajustes se mantienen guardados.", etiqueta);

        if (Boton(new Rect(px + 40f, py + 150f, 180f, 50f), "SÍ, SALIR"))
        {
            Time.timeScale = 1f;
            Pausado = false;
            panelAbierto = false;
            if (pc != null) pc.enabled = true;
            SceneManager.LoadScene("MainMenu");
        }

        if (Boton(new Rect(px + pw - 220f, py + 150f, 180f, 50f), "CANCELAR"))
            confirmandoSalida = false;
    }

    // ── Utilidades ───────────────────────────────────────────────

    private bool Boton(Rect r, string texto)
    {
        bool hover = r.Contains(Event.current.mousePosition);
        var estilo = hover ? estBotonChico : estBoton;
        return GUI.Button(r, texto, estilo);
    }

    private void InicializarEstilos()
    {
        if (estilosListos) return;
        estilosListos = true;

        estTitulo = new GUIStyle(GUI.skin.label)
        {
            fontSize = 44, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter
        };
        estTitulo.normal.textColor = new Color(0.85f, 0.95f, 1f);

        estTituloAjustes = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter
        };
        estTituloAjustes.normal.textColor = new Color(0.15f, 0.85f, 1f);

        etiqueta = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18, alignment = TextAnchor.MiddleCenter
        };
        etiqueta.normal.textColor = new Color(0.75f, 0.85f, 0.95f);

        estBoton = new GUIStyle(GUI.skin.box) { fontSize = 20, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        estBoton.normal.textColor = Color.white;

        estBotonChico = new GUIStyle(GUI.skin.box) { fontSize = 20, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        estBotonChico.normal.textColor = new Color(0.35f, 0.9f, 1f);
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
