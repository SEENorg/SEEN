using UnityEngine;

/// <summary>
/// HUD del juego SEEN con temática de redes sociales distópicas.
/// Muestra en pantalla:
///   • Vida del jugador ("integridad de red"): panel neón con un bloque por vida,
///     porcentaje grande, destello que recorre la barra, halo que late, sacudida y
///     destello rojo al recibir daño, y bordes rojos en la pantalla cuando la vida es crítica.
///   • (Opcional) Módulos de hackeo resueltos, notificaciones y nivel (Estructuras de Datos).
/// Se dibuja con OnGUI, escalado a una pantalla de referencia de 1920 x 1080.
/// </summary>
public class HUDSistema : MonoBehaviour
{
    private const float DuracionDano = 0.55f;

    [Header("Posición y tamaño (pantalla de 1920 x 1080)")]
    [SerializeField] private float margen = 24f;
    [SerializeField] private float anchoPanel = 460f;
    [SerializeField] private float altoVida = 128f;
    [SerializeField] private bool mostrarPanelesDatos = false;

    [Header("Colores temática SEEN")]
    [SerializeField] private Color colorFondo = new Color(0.02f, 0.04f, 0.06f, 0.88f);
    [SerializeField] private Color colorVidaLlena = new Color(0.15f, 0.95f, 0.55f, 1f);
    [SerializeField] private Color colorVidaMedia = new Color(1f, 0.72f, 0.15f, 1f);
    [SerializeField] private Color colorVidaCritica = new Color(1f, 0.20f, 0.28f, 1f);
    [SerializeField] private Color colorVidaVacia = new Color(0.12f, 0.06f, 0.09f, 0.95f);
    [SerializeField] private Color colorTexto = new Color(0.88f, 0.95f, 1f);
    [SerializeField] private Color colorAcento = new Color(0.15f, 0.85f, 1f);

    private PincelHUD pincel;
    private bool suscritoVida;
    private int ultimaVidaObservada = -1;
    private float vidaVisual = 1f;
    private float vidaFantasma = 1f;
    private float esperaFantasma;
    private float efectoDanoTimer;
    private float efectoCuraTimer;

    private void Awake()
    {
        if (VidaJugador.Instancia != null)
        {
            ultimaVidaObservada = VidaJugador.Instancia.VidaActual;
            vidaVisual = VidaJugador.Instancia.VidaNormalizada;
            vidaFantasma = vidaVisual;
        }
    }

    private void Start()
    {
        SuscribirVida();
        if (VidaJugador.Instancia != null)
        {
            ultimaVidaObservada = VidaJugador.Instancia.VidaActual;
            vidaVisual = VidaJugador.Instancia.VidaNormalizada;
            vidaFantasma = vidaVisual;
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
        if (pincel != null) pincel.Liberar();
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
        {
            efectoDanoTimer = DuracionDano;
            esperaFantasma = 0.35f;
        }
        else if (ultimaVidaObservada >= 0 && vidaActual > ultimaVidaObservada)
        {
            efectoCuraTimer = 0.6f;
        }
        ultimaVidaObservada = vidaActual;
    }

    private void Update()
    {
        if (!suscritoVida) SuscribirVida();

        float dt = Time.unscaledDeltaTime;
        if (efectoDanoTimer > 0f) efectoDanoTimer -= dt;
        if (efectoCuraTimer > 0f) efectoCuraTimer -= dt;

        float objetivo = 1f;
        if (VidaJugador.Instancia != null) objetivo = VidaJugador.Instancia.VidaNormalizada;
        vidaVisual = Mathf.Lerp(vidaVisual, objetivo, dt * 12f);

        // La parte perdida queda un momento en blanco/rojo y luego se va vaciando
        if (esperaFantasma > 0f) esperaFantasma -= dt;
        else vidaFantasma = Mathf.Lerp(vidaFantasma, objetivo, dt * 4f);
        if (vidaFantasma < vidaVisual) vidaFantasma = vidaVisual;
    }

    private void OnGUI()
    {
        if (HackManager.Abierto || PauseMenu.Pausado || CipherWheel.Abierta ||
            GameFeedback.NivelCompletadoVisible || GameFeedback.GameOverVisible || PantallaControles.Visible)
            return;

        if (pincel == null) pincel = new PincelHUD();
        pincel.EmpezarEscala();

        int vidaActual = 3;
        int vidaMaxima = 3;
        if (VidaJugador.Instancia != null)
        {
            vidaActual = VidaJugador.Instancia.VidaActual;
            vidaMaxima = Mathf.Max(1, VidaJugador.Instancia.VidaMaxima);
        }
        float ratio = Mathf.Clamp01((float)vidaActual / vidaMaxima);

        DibujarEfectosPantalla(ratio);
        DibujarPanelVida(vidaActual, vidaMaxima, ratio);

        if (mostrarPanelesDatos)
        {
            float y = margen + altoVida + 14f;
            DibujarPanelModulos(ref y);
            DibujarPanelNotificaciones(ref y);
            DibujarPanelNivel(ref y);
        }

        pincel.TerminarEscala();
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    //  VIDA
    // ═══════════════════════════════════════════════════════════════════════════════════

    private void DibujarPanelVida(int vidaActual, int vidaMaxima, float ratio)
    {
        float ahora = Time.unscaledTime;

        Color colorEstado = colorVidaLlena;
        string textoEstado = "CONEXIÓN SEGURA";
        float velocidadLatido = 2f;
        if (ratio <= 0.35f)
        {
            colorEstado = colorVidaCritica;
            textoEstado = "SEÑAL CRÍTICA";
            velocidadLatido = 7f;
        }
        else if (ratio <= 0.70f)
        {
            colorEstado = colorVidaMedia;
            textoEstado = "ALERTA DE RED";
            velocidadLatido = 4f;
        }

        // Sacudida al recibir daño
        float sacudidaX = 0f;
        float sacudidaY = 0f;
        float fuerzaDano = 0f;
        if (efectoDanoTimer > 0f)
        {
            fuerzaDano = efectoDanoTimer / DuracionDano;
            sacudidaX = Mathf.Sin(ahora * 70f) * fuerzaDano * 9f;
            sacudidaY = Mathf.Cos(ahora * 57f) * fuerzaDano * 4f;
        }

        Rect panel = new Rect(margen + sacudidaX, margen + sacudidaY, anchoPanel, altoVida);
        float latido = 0.5f + 0.5f * Mathf.Sin(ahora * velocidadLatido);

        Color colorBorde = Color.Lerp(colorAcento, colorEstado, 0.5f);
        if (fuerzaDano > 0f) colorBorde = Color.Lerp(colorBorde, colorVidaCritica, fuerzaDano);
        if (efectoCuraTimer > 0f) colorBorde = Color.Lerp(colorBorde, colorVidaLlena, efectoCuraTimer / 0.6f);

        Color colorHalo = colorBorde;
        colorHalo.a = 0.25f + 0.35f * latido + fuerzaDano * 0.4f;
        pincel.Halo(panel, colorHalo, 4);
        pincel.Caja(panel, colorFondo);
        pincel.Borde(panel, colorBorde);

        // Acento rosado en la esquina, estilo terminal
        pincel.Rectangulo(new Rect(panel.x + 18f, panel.y, 70f, 3f), ConstructorMenuJuego.ColorMagenta);

        // Encabezado
        pincel.Texto(new Rect(panel.x + 18f, panel.y + 12f, 300f, 22f), "SEEN // INTEGRIDAD DE RED", 16,
                     colorAcento, TextAnchor.MiddleLeft, true);

        int porcentaje = Mathf.RoundToInt(vidaVisual * 100f);
        Color colorPorcentaje = colorEstado;
        if (fuerzaDano > 0f && Mathf.Repeat(ahora, 0.12f) < 0.06f) colorPorcentaje = Color.white;
        pincel.Texto(new Rect(panel.x + panel.width - 150f, panel.y + 4f, 132f, 40f), porcentaje + "%", 34,
                     colorPorcentaje, TextAnchor.MiddleRight, true);

        // Bloques de vida
        float barraX = panel.x + 18f;
        float barraY = panel.y + 48f;
        float barraAncho = panel.width - 36f;
        float barraAlto = 38f;
        float separacion = 8f;
        float anchoBloque = (barraAncho - separacion * (vidaMaxima - 1)) / vidaMaxima;

        for (int i = 0; i < vidaMaxima; i++)
        {
            Rect bloque = new Rect(barraX + i * (anchoBloque + separacion), barraY, anchoBloque, barraAlto);
            float llenoVisual = Mathf.Clamp01(vidaVisual * vidaMaxima - i);
            float llenoFantasma = Mathf.Clamp01(vidaFantasma * vidaMaxima - i);

            pincel.Caja(bloque, colorVidaVacia);

            // Lo que se acaba de perder: parpadea blanco y se vuelve rojo mientras se vacía
            if (llenoFantasma > llenoVisual)
            {
                Color colorFantasma = Color.Lerp(colorVidaCritica, Color.white, Mathf.Clamp01(esperaFantasma / 0.35f));
                pincel.Caja(new Rect(bloque.x, bloque.y, bloque.width * llenoFantasma, bloque.height), colorFantasma);
            }

            if (llenoVisual > 0.02f)
            {
                Rect relleno = new Rect(bloque.x, bloque.y, bloque.width * llenoVisual, bloque.height);
                pincel.Caja(relleno, colorEstado);
                // Reflejo superior para que se vea "de vidrio"
                Color reflejo = new Color(1f, 1f, 1f, 0.22f);
                pincel.Caja(new Rect(relleno.x + 4f, relleno.y + 4f, Mathf.Max(0f, relleno.width - 8f), relleno.height * 0.32f), reflejo);
                // Rayas diagonales sutiles (marcas de datos)
                for (float rx = relleno.x + 10f; rx < relleno.xMax - 6f; rx += 14f)
                {
                    pincel.Rectangulo(new Rect(rx, relleno.y + relleno.height * 0.55f, 6f, 2f), new Color(0f, 0f, 0f, 0.25f));
                }
            }
            else
            {
                // Bloque vacío: contorno rojo y una X para que no dependa solo del color
                pincel.Borde(bloque, new Color(colorVidaCritica.r, colorVidaCritica.g, colorVidaCritica.b, 0.45f));
                pincel.Texto(bloque, "X", 20, new Color(colorVidaCritica.r, colorVidaCritica.g, colorVidaCritica.b, 0.7f),
                             TextAnchor.MiddleCenter, true);
            }
        }

        // Destello que recorre la barra cada pocos segundos
        float ciclo = Mathf.Repeat(ahora / 2.6f, 1f);
        if (ciclo < 0.45f)
        {
            float avance = ciclo / 0.45f;
            float anchoLlenado = barraAncho * vidaVisual;
            float xBrillo = barraX - 60f + (anchoLlenado + 60f) * avance;
            if (xBrillo < barraX + anchoLlenado)
                pincel.Brillo(new Rect(xBrillo, barraY, 60f, barraAlto), new Color(1f, 1f, 1f, 0.35f));
        }

        // Línea de estado
        float yEstado = barraY + barraAlto + 10f;
        Color colorPunto = colorEstado;
        colorPunto.a = 0.35f + 0.65f * latido;
        pincel.Texto(new Rect(barraX, yEstado, 20f, 22f), "●", 16, colorPunto, TextAnchor.MiddleLeft, false);

        string estadoMostrado = textoEstado;
        if (fuerzaDano > 0f) estadoMostrado = "¡IMPACTO DE AMENAZA!";
        else if (efectoCuraTimer > 0f) estadoMostrado = "SEÑAL RESTAURADA";
        pincel.Texto(new Rect(barraX + 20f, yEstado, 260f, 22f), estadoMostrado, 15, colorEstado, TextAnchor.MiddleLeft, true);

        pincel.Texto(new Rect(barraX + barraAncho - 160f, yEstado, 160f, 22f), "VIDA  " + vidaActual + " / " + vidaMaxima, 15,
                     colorTexto, TextAnchor.MiddleRight, true);
    }

    /// <summary>Destello rojo al recibir daño y bordes rojos que laten con vida crítica.</summary>
    private void DibujarEfectosPantalla(float ratio)
    {
        float ancho = PincelHUD.AnchoReferencia;
        float alto = 1080f;

        if (efectoDanoTimer > 0f)
        {
            float fuerza = efectoDanoTimer / DuracionDano;
            pincel.Rectangulo(new Rect(0f, 0f, ancho, alto), new Color(colorVidaCritica.r, 0f, 0f, 0.22f * fuerza));
        }

        if (ratio <= 0.35f && ratio > 0f)
        {
            float latido = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
            for (int capa = 0; capa < 4; capa++)
            {
                float grosor = 18f + capa * 22f;
                float alfa = (0.16f - capa * 0.035f) * (0.5f + 0.5f * latido);
                Color c = new Color(colorVidaCritica.r, 0f, 0.05f, alfa);
                pincel.Rectangulo(new Rect(0f, 0f, ancho, grosor), c);
                pincel.Rectangulo(new Rect(0f, alto - grosor, ancho, grosor), c);
                pincel.Rectangulo(new Rect(0f, 0f, grosor, alto), c);
                pincel.Rectangulo(new Rect(ancho - grosor, 0f, grosor, alto), c);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    //  PANELES DE ESTRUCTURAS DE DATOS (opcionales)
    // ═══════════════════════════════════════════════════════════════════════════════════

    private void DibujarPanelModulos(ref float y)
    {
        Rect panel = new Rect(margen, y, anchoPanel, 90f);
        pincel.Caja(panel, colorFondo);
        pincel.Borde(panel, new Color(colorAcento.r, colorAcento.g, colorAcento.b, 0.5f));
        pincel.Texto(new Rect(panel.x + 16f, panel.y + 8f, panel.width - 32f, 22f), "MÓDULOS DE HACKEO RESUELTOS", 15,
                     colorAcento, TextAnchor.MiddleLeft, true);

        int total = 0;
        if (EstructurasDatosJuego.Instancia != null) total = EstructurasDatosJuego.Instancia.TotalModulosResueltos;
        pincel.Texto(new Rect(panel.x + 16f, panel.y + 34f, panel.width - 32f, 20f), "Accesos vulnerados: " + total, 14,
                     colorTexto, TextAnchor.MiddleLeft, false);

        if (EstructurasDatosJuego.Instancia != null && total > 0)
        {
            string modulo = EstructurasDatosJuego.Instancia.ObtenerModuloEnPosicion(total - 1);
            if (!string.IsNullOrEmpty(modulo))
            {
                pincel.Texto(new Rect(panel.x + 16f, panel.y + 58f, panel.width - 32f, 20f), "✓ " + modulo, 14,
                             colorVidaLlena, TextAnchor.MiddleLeft, false);
            }
        }
        y += panel.height + 10f;
    }

    private void DibujarPanelNotificaciones(ref float y)
    {
        Rect panel = new Rect(margen, y, anchoPanel, 90f);
        pincel.Caja(panel, colorFondo);
        pincel.Borde(panel, new Color(colorAcento.r, colorAcento.g, colorAcento.b, 0.5f));
        pincel.Texto(new Rect(panel.x + 16f, panel.y + 8f, panel.width - 32f, 22f), "NOTIFICACIONES DEL SISTEMA", 15,
                     colorAcento, TextAnchor.MiddleLeft, true);

        int pendientes = 0;
        string proxima = null;
        if (EstructurasDatosJuego.Instancia != null)
        {
            pendientes = EstructurasDatosJuego.Instancia.NotificacionesPendientes;
            proxima = EstructurasDatosJuego.Instancia.VerProximaNotificacion();
        }
        if (string.IsNullOrEmpty(proxima)) proxima = "Sistema estable. Sin alertas activas.";

        pincel.Texto(new Rect(panel.x + 16f, panel.y + 34f, panel.width - 32f, 20f), "Pendientes: " + pendientes, 14,
                     colorTexto, TextAnchor.MiddleLeft, false);
        pincel.Texto(new Rect(panel.x + 16f, panel.y + 58f, panel.width - 32f, 20f), proxima, 14,
                     colorTexto, TextAnchor.MiddleLeft, false);
        y += panel.height + 10f;
    }

    private void DibujarPanelNivel(ref float y)
    {
        Rect panel = new Rect(margen, y, anchoPanel, 60f);
        pincel.Caja(panel, colorFondo);
        pincel.Borde(panel, new Color(colorAcento.r, colorAcento.g, colorAcento.b, 0.5f));

        string nombreNivel = GameFeedback.NombreNivelActual;
        int dificultad = 0;
        if (EstructurasDatosJuego.Instancia != null)
            dificultad = EstructurasDatosJuego.Instancia.ObtenerDificultadNivel(nombreNivel);

        pincel.Texto(new Rect(panel.x + 16f, panel.y + 6f, panel.width - 32f, 22f), "NIVEL: " + nombreNivel, 15,
                     colorAcento, TextAnchor.MiddleLeft, true);
        pincel.Texto(new Rect(panel.x + 16f, panel.y + 32f, panel.width - 32f, 20f),
                     "AMENAZA: " + ObtenerTipoAmenaza(dificultad) + " (Dificultad " + dificultad + ")", 13,
                     colorTexto, TextAnchor.MiddleLeft, false);
        y += panel.height + 10f;
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
