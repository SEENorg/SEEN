using UnityEngine;

/// <summary>
/// HUD del juego SEEN con temática de redes sociales distópicas.
/// Muestra en pantalla:
///   • Vida del jugador (integridad del sistema, barra segmentada con reacción al daño)
///   • Módulos de hackeo resueltos (Lista de Estructuras de Datos)
///   • Notificaciones del sistema (Cola de Estructuras de Datos)
///   • Nivel actual y dificultad (Diccionario de Estructuras de Datos)
/// Se dibuja con OnGUI para mantener coherencia con el resto de la UI del proyecto.
/// </summary>
public class HUDSistema : MonoBehaviour
{
    [Header("Posición y tamaño")]
    [SerializeField] private float margen = 14f;
    [SerializeField] private float anchoPanel = 340f;
    [SerializeField] private float altoVida = 82f;
    [SerializeField] private float altoModulos = 70f;
    [SerializeField] private float altoNotificaciones = 120f;
    [SerializeField] private bool mostrarPanelesDatos = false;

    [Header("Colores temática SEEN")]
    [SerializeField] private Color colorFondo = new Color(0.04f, 0.05f, 0.08f, 0.90f);
    [SerializeField] private Color colorVidaLlena = new Color(0.15f, 0.95f, 0.55f, 1f);
    [SerializeField] private Color colorVidaMedia = new Color(1f, 0.72f, 0.15f, 1f);
    [SerializeField] private Color colorVidaCritica = new Color(1f, 0.20f, 0.28f, 1f);
    [SerializeField] private Color colorVidaVacia = new Color(0.18f, 0.10f, 0.14f, 0.85f);
    [SerializeField] private Color colorTexto = new Color(0.88f, 0.95f, 1f);
    [SerializeField] private Color colorAcento = new Color(0.15f, 0.85f, 1f);

    private GUIStyle estiloFondo;
    private GUIStyle estiloTexto;
    private GUIStyle estiloTitulo;
    private GUIStyle estiloEstado;
    private Texture2D texturaBlanca;

    private bool suscritoVida;
    private int ultimaVidaObservada = -1;
    private float vidaVisual = 1f;
    private float efectoDañoTimer = 0f;

    private void Awake()
    {
        texturaBlanca = Texture2D.whiteTexture;
        if (VidaJugador.Instancia != null)
        {
            ultimaVidaObservada = VidaJugador.Instancia.VidaActual;
            vidaVisual = VidaJugador.Instancia.VidaNormalizada;
        }
    }

    private void Start()
    {
        SuscribirVida();
        if (VidaJugador.Instancia != null)
        {
            ultimaVidaObservada = VidaJugador.Instancia.VidaActual;
            vidaVisual = VidaJugador.Instancia.VidaNormalizada;
        }
    }

    private void OnEnable()
    {
        SuscribirVida();
    }

    private void OnDisable()
    {
        DesuscribirVida();
    }

    private void OnDestroy()
    {
        DesuscribirVida();
    }

    private void SuscribirVida()
    {
        if (suscritoVida || VidaJugador.Instancia == null) return;
        VidaJugador.Instancia.OnVidaCambiada += OnVidaCambiada;
        suscritoVida = true;
    }

    private void DesuscribirVida()
    {
        if (!suscritoVida || VidaJugador.Instancia == null) return;
        VidaJugador.Instancia.OnVidaCambiada -= OnVidaCambiada;
        suscritoVida = false;
    }

    private void OnVidaCambiada(int vidaActual, int vidaMaxima)
    {
        if (ultimaVidaObservada >= 0 && vidaActual < ultimaVidaObservada)
            efectoDañoTimer = 0.45f;

        ultimaVidaObservada = vidaActual;
        Debug.Log($"[HUD] Vida: {vidaActual}/{vidaMaxima}");
    }

    private void Update()
    {
        if (!suscritoVida) SuscribirVida();

        if (efectoDañoTimer > 0f)
            efectoDañoTimer -= Time.unscaledDeltaTime;

        float objetivo = VidaJugador.Instancia != null ? VidaJugador.Instancia.VidaNormalizada : 1f;
        vidaVisual = Mathf.Lerp(vidaVisual, objetivo, Time.unscaledDeltaTime * 10f);
    }

    private void OnGUI()
    {
        if (HackManager.Abierto || PauseMenu.Pausado || CipherWheel.Abierta ||
            GameFeedback.NivelCompletadoVisible || GameFeedback.GameOverVisible)
            return;

        if (estiloFondo == null)
        {
            estiloFondo = new GUIStyle(GUI.skin.box);
            estiloTexto = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = colorTexto },
                wordWrap = true
            };
            estiloTitulo = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = colorAcento }
            };
            estiloEstado = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = colorTexto }
            };
        }

        float y = margen;

        // 1. VIDA DEL JUGADOR (Integridad de conexión)
        DibujarPanelVida(ref y);
        y += altoVida + 10f;

        if (!mostrarPanelesDatos) return;

        // 2. MÓDULOS DE HACKEO RESUELTOS (Lista de Estructuras de Datos)
        DibujarPanelModulos(ref y);
        y += altoModulos + 10f;

        // 3. NOTIFICACIONES DEL SISTEMA (Cola de Estructuras de Datos)
        DibujarPanelNotificaciones(ref y);
        y += altoNotificaciones + 10f;

        // 4. NIVEL Y DIFICULTAD (Diccionario de Estructuras de Datos)
        DibujarPanelNivel(ref y);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    //  PANEL: VIDA DEL JUGADOR
    // ═══════════════════════════════════════════════════════════════════════════════════
    private void DibujarPanelVida(ref float y)
    {
        float sacudidaX = 0f;
        float sacudidaY = 0f;
        if (efectoDañoTimer > 0f)
        {
            float intensidad = (efectoDañoTimer / 0.45f) * 6f;
            sacudidaX = Mathf.Sin(Time.unscaledTime * 65f) * intensidad;
            sacudidaY = Mathf.Cos(Time.unscaledTime * 55f) * (intensidad * 0.5f);
        }

        float x = margen + sacudidaX;
        float posY = y + sacudidaY;

        int vidaActual = VidaJugador.Instancia != null ? VidaJugador.Instancia.VidaActual : 3;
        int vidaMaxima = VidaJugador.Instancia != null ? Mathf.Max(1, VidaJugador.Instancia.VidaMaxima) : 3;
        float ratio = Mathf.Clamp01((float)vidaActual / vidaMaxima);

        Color colorBarra = colorVidaLlena;
        string etiquetaEstado = "CONEXIÓN SEGURA";
        if (ratio <= 0.35f)
        {
            colorBarra = colorVidaCritica;
            etiquetaEstado = "SEÑAL CRÍTICA";
        }
        else if (ratio <= 0.70f)
        {
            colorBarra = colorVidaMedia;
            etiquetaEstado = "ALERTA DE RED";
        }

        Color colorBorde = efectoDañoTimer > 0f
            ? Color.Lerp(colorAcento, colorVidaCritica, efectoDañoTimer / 0.45f)
            : colorAcento;

        // Marco exterior neón y fondo oscuro
        DibujarRect(new Rect(x, posY, anchoPanel, altoVida), colorFondo);
        DibujarBorde(new Rect(x, posY, anchoPanel, altoVida), colorBorde, 2f);

        // Encabezado estilo terminal SEEN
        GUI.Label(new Rect(x + 12f, posY + 6f, anchoPanel - 24f, 22f), "SEEN // INTEGRIDAD DE RED", estiloTitulo);

        if (efectoDañoTimer > 0f)
        {
            Color prev = estiloEstado.normal.textColor;
            estiloEstado.normal.textColor = colorVidaCritica;
            GUI.Label(new Rect(x + 12f, posY + 6f, anchoPanel - 24f, 22f), "¡IMPACTO DE AMENAZA!", estiloEstado);
            estiloEstado.normal.textColor = prev;
        }

        // Barra continua suavizada + división en segmentos por cada vida
        float barraX = x + 12f;
        float barraY = posY + 34f;
        float barraAncho = anchoPanel - 24f;
        float barraAlto = 18f;

        DibujarRect(new Rect(barraX, barraY, barraAncho, barraAlto), colorVidaVacia);

        if (vidaVisual > 0.001f)
            DibujarRect(new Rect(barraX, barraY, barraAncho * Mathf.Clamp01(vidaVisual), barraAlto), colorBarra);

        // Separadores de segmentos sobre la barra
        float anchoSegmento = barraAncho / vidaMaxima;
        for (int i = 1; i < vidaMaxima; i++)
        {
            float sepX = barraX + anchoSegmento * i - 1.5f;
            DibujarRect(new Rect(sepX, barraY, 3f, barraAlto), colorFondo);
        }

        // Texto inferior de estado
        GUI.Label(new Rect(barraX, barraY + barraAlto + 4f, barraAncho * 0.5f, 20f),
            $"VIDA: {vidaActual} / {vidaMaxima}", estiloTexto);

        Color colorPrev = estiloEstado.normal.textColor;
        estiloEstado.normal.textColor = colorBarra;
        GUI.Label(new Rect(barraX + barraAncho * 0.45f, barraY + barraAlto + 4f, barraAncho * 0.55f, 20f),
            etiquetaEstado, estiloEstado);
        estiloEstado.normal.textColor = colorPrev;
    }

    private void DibujarRect(Rect r, Color c)
    {
        Color previo = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, texturaBlanca);
        GUI.color = previo;
    }

    private void DibujarBorde(Rect r, Color c, float grosor)
    {
        DibujarRect(new Rect(r.x, r.y, r.width, grosor), c);
        DibujarRect(new Rect(r.x, r.yMax - grosor, r.width, grosor), c);
        DibujarRect(new Rect(r.x, r.y, grosor, r.height), c);
        DibujarRect(new Rect(r.xMax - grosor, r.y, grosor, r.height), c);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    //  PANEL: MÓDULOS DE HACKEO RESUELTOS (Lista de ED)
    // ═══════════════════════════════════════════════════════════════════════════════════
    private void DibujarPanelModulos(ref float y)
    {
        float x = margen;

        GUI.color = colorFondo;
        GUI.Box(new Rect(x, y, anchoPanel, altoModulos), GUIContent.none, estiloFondo);
        GUI.color = Color.white;

        GUI.Label(new Rect(x + 10f, y + 6f, anchoPanel - 20f, 24f), "MÓDULOS DE HACKEO RESUELTOS", estiloTitulo);

        int total = EstructurasDatosJuego.Instancia != null
            ? EstructurasDatosJuego.Instancia.TotalModulosResueltos
            : 0;

        GUI.Label(new Rect(x + 10f, y + 34f, anchoPanel - 20f, 24f),
            $"Accesos vulnerados: {total}", estiloTexto);

        // Lista de módulos resueltos (máximo 4 visibles)
        if (EstructurasDatosJuego.Instancia != null)
        {
            float yy = y + 58f;
            for (int i = 0; i < Mathf.Min(4, total); i++)
            {
                string modulo = EstructurasDatosJuego.Instancia.ObtenerModuloEnPosicion(i);
                if (!string.IsNullOrEmpty(modulo))
                {
                    GUI.Label(new Rect(x + 16f, yy, anchoPanel - 32f, 18f),
                        $"✓ {modulo}", estiloTexto);
                    yy += 18f;
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    //  PANEL: NOTIFICACIONES DEL SISTEMA (Cola de ED)
    // ═══════════════════════════════════════════════════════════════════════════════════
    private void DibujarPanelNotificaciones(ref float y)
    {
        float x = margen;

        GUI.color = colorFondo;
        GUI.Box(new Rect(x, y, anchoPanel, altoNotificaciones), GUIContent.none, estiloFondo);
        GUI.color = Color.white;

        GUI.Label(new Rect(x + 10f, y + 6f, anchoPanel - 20f, 24f), "NOTIFICACIONES DEL SISTEMA", estiloTitulo);

        int pendientes = EstructurasDatosJuego.Instancia != null
            ? EstructurasDatosJuego.Instancia.NotificacionesPendientes
            : 0;

        GUI.Label(new Rect(x + 10f, y + 34f, anchoPanel - 20f, 24f),
            $"Pendientes: {pendientes}", estiloTexto);

        string proxima = EstructurasDatosJuego.Instancia != null
            ? EstructurasDatosJuego.Instancia.VerProximaNotificacion()
            : null;

        if (!string.IsNullOrEmpty(proxima))
        {
            GUI.Label(new Rect(x + 10f, y + 58f, anchoPanel - 20f, 56f),
                proxima, estiloTexto);
        }
        else
        {
            GUI.Label(new Rect(x + 10f, y + 58f, anchoPanel - 20f, 56f),
                "Sistema estable. Sin alertas activas.", estiloTexto);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    //  PANEL: NIVEL Y DIFICULTAD (Diccionario de ED)
    // ═══════════════════════════════════════════════════════════════════════════════════
    private void DibujarPanelNivel(ref float y)
    {
        float x = margen;

        GUI.color = colorFondo;
        GUI.Box(new Rect(x, y, anchoPanel, 44f), GUIContent.none, estiloFondo);
        GUI.color = Color.white;

        string nombreNivel = GameFeedback.NombreNivelActual;
        int dificultad = EstructurasDatosJuego.Instancia != null
            ? EstructurasDatosJuego.Instancia.ObtenerDificultadNivel(nombreNivel)
            : 0;

        string tipoAmenaza = ObtenerTipoAmenaza(dificultad);

        GUI.Label(new Rect(x + 10f, y + 6f, anchoPanel - 20f, 20f),
            $"NIVEL: {nombreNivel}", estiloTitulo);
        GUI.Label(new Rect(x + 10f, y + 26f, anchoPanel - 20f, 16f),
            $"AMENAZA: {tipoAmenaza} (Dificultad {dificultad})", estiloTexto);
    }

    private string ObtenerTipoAmenaza(int dificultad)
    {
        switch (dificultad)
        {
            case 0: return "Vigilancia básica (perfil público)";
            case 1: return "Comparación social (likes y métricas)";
            case 2: return "Vigilancia normalizada (metadatos)";
            default: return "Sistema desconocido";
        }
    }
}
