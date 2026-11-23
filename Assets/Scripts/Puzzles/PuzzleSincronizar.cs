using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Minijuego: sincronizar una señal. Un marcador va y viene sobre una pista y hay que
/// detenerlo dentro de la ZONA. Cada acierto mueve la zona y acelera el marcador;
/// un fallo reinicia el contador.
///
/// Accesibilidad (daltonismo): la zona no depende solo del color verde: tiene borde
/// brillante y la palabra "ZONA" encima, y el marcador es una barra blanca.
/// Controles: botón SINCRONIZAR con ratón (clic), mando (A) o teclado (Espacio o Enter).
/// </summary>
public class PuzzleSincronizar : IPuzzle
{
    private const float AnchoTarjeta = 640f;
    private const float AltoTarjeta = 480f;
    private const float AnchoPista = 560f;
    private const float YPista = 55f;
    private const float AltoPista = 30f;
    private const float AnchoProgreso = 300f;
    private const float DuracionDestello = 0.35f;

    private readonly Color colorPista = new Color(0.07f, 0.11f, 0.15f);
    private readonly Color colorZona = new Color(0.15f, 0.75f, 0.35f, 0.45f);
    private readonly Color colorBordeZona = new Color(0.35f, 1f, 0.55f);
    private readonly Color colorSegmentoVacio = new Color(0.11f, 0.20f, 0.26f);

    private readonly IContextoHack ctx;
    private CajasRedondeadasUI cajas;
    private TarjetaHackUI tarjeta;

    private RectTransform marcador;
    private Image nucleoMarcador;
    private RectTransform zona;
    private Image bordeZona;
    private Text textoContador;
    private Image[] segmentos;
    private Button boton;

    private float tiempo;
    private float velocidad;
    private float anchoZona;
    private int aciertos;
    private int objetivo;
    private bool activo;
    private float tiempoDestello;
    private int frameUltimoIntento = -1;   // Espacio puede llegar dos veces en el mismo frame

    public PuzzleSincronizar(IContextoHack ctx)
    {
        this.ctx = ctx;
    }

    public void Construir()
    {
        cajas = new CajasRedondeadasUI();
        ctx.MostrarMarco(false);
        tarjeta = new TarjetaHackUI(ctx, cajas, AnchoTarjeta, AltoTarjeta, "SINCRONIZACIÓN DE SEÑAL: DETÉN EL MARCADOR EN LA ZONA");

        int dificultad = Mathf.Clamp(ctx.DificultadBase + ctx.IndiceModulo, 0, 5);
        objetivo = 2 + Mathf.Clamp(ctx.DificultadBase + ctx.IndiceModulo / 2, 0, 3);
        aciertos = 0;
        tiempo = 0f;
        velocidad = 380f + dificultad * 65f;
        anchoZona = Mathf.Max(80f, 190f - dificultad * 25f);
        tiempoDestello = 0f;
        activo = true;

        Transform raiz = tarjeta.Raiz;

        // Pista
        cajas.CrearCaja(raiz, "Pista", new Vector2(0f, YPista), new Vector2(AnchoPista, AltoPista),
                        colorPista, tarjeta.ColorBordeTarjeta, 2f);

        // Zona (relleno + borde brillante + la palabra ZONA encima)
        bordeZona = cajas.CrearCaja(raiz, "Zona", new Vector2(0f, YPista), new Vector2(anchoZona, AltoPista),
                                    colorZona, colorBordeZona, 2f);
        zona = (RectTransform)bordeZona.transform.parent;
        HackUI.CrearTexto(zona, "EtiquetaZona", "ZONA", 12, TextAnchor.MiddleCenter, new Vector2(0f, 26f),
                          new Vector2(anchoZona, 18f), colorBordeZona);

        // Marcador: brillo celeste + barra blanca
        marcador = (RectTransform)HackUI.NuevoUI(raiz, "Marcador", new Vector2(0f, YPista), new Vector2(18f, 54f)).transform;
        Color brillo = HackUI.ColAcento;
        brillo.a = 0.35f;
        cajas.CrearRelleno(marcador, "Brillo", Vector2.zero, new Vector2(18f, 54f), brillo, 3f);
        nucleoMarcador = cajas.CrearRelleno(marcador, "Nucleo", Vector2.zero, new Vector2(6f, 48f), Color.white, 6f);

        // Contador y barra de progreso
        textoContador = HackUI.CrearTexto(raiz, "Contador", "", 17, TextAnchor.MiddleCenter,
                                          new Vector2(0f, 2f), new Vector2(500f, 24f), new Color(0.85f, 0.9f, 0.95f));
        CrearProgreso();

        // Botón SINCRONIZAR (A del mando y Espacio/Enter lo pulsan porque queda seleccionado)
        boton = tarjeta.CrearBoton(raiz, "BotonSincronizar", new Vector2(0f, -80f), new Vector2(300f, 60f),
                                   new Color(0.05f, 0.36f, 0.26f), new Color(0.07f, 0.47f, 0.34f),
                                   new Color(0.18f, 0.72f, 0.50f), "SINCRONIZAR", AlPulsarBoton, true);
        Text textoBoton = HackUI.CrearTexto(boton.transform, "Texto", "SINCRONIZAR", 22, TextAnchor.MiddleCenter,
                                            Vector2.zero, new Vector2(300f, 60f), Color.white);
        textoBoton.fontStyle = FontStyle.Bold;

        HackUI.CrearTexto(raiz, "Ayuda", "A en el mando  ·  Espacio o Enter en el teclado  ·  clic con el ratón", 13,
                          TextAnchor.MiddleCenter, new Vector2(0f, -132f), new Vector2(600f, 20f), tarjeta.ColorTextoSuave);

        ActualizarContador();
        tarjeta.MostrarEstadoNormal("PULSA SINCRONIZAR CUANDO EL MARCADOR ESTÉ EN LA ZONA");
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(boton.gameObject);
    }

    /// <summary>Se llama cada frame: mueve el marcador y apaga los destellos.</summary>
    public void Actualizar()
    {
        if (!activo || marcador == null) return;

        float dt = Time.unscaledDeltaTime;
        tiempo += dt;
        float x = Mathf.PingPong(tiempo * velocidad, AnchoPista) - AnchoPista / 2f;
        marcador.anchoredPosition = new Vector2(x, marcador.anchoredPosition.y);

        if (LeerEspacio()) IntentarSincronizar();

        if (tiempoDestello > 0f)
        {
            tiempoDestello -= dt;
            if (tiempoDestello <= 0f) RestaurarColores();
        }

        // El botón siempre queda seleccionado para que A, Espacio y Enter lo pulsen
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && boton != null)
            EventSystem.current.SetSelectedGameObject(boton.gameObject);
    }

    /// <summary>Se llama al cerrar el hackeo o cambiar de módulo.</summary>
    public void Detener()
    {
        activo = false;
        ctx.MostrarMarco(true);
        if (cajas != null) cajas.Liberar();
        cajas = null;
        tarjeta = null;
        marcador = null;
        zona = null;
        boton = null;
    }

    // ── Lógica ──────────────────────────────────────────────────────

    private void AlPulsarBoton(string valor)
    {
        IntentarSincronizar();
    }

    private void IntentarSincronizar()
    {
        if (!activo || marcador == null || zona == null) return;
        if (ctx.InputBloqueado || ctx.EnTransicion) return;
        // Espacio puede pulsar el botón (EventSystem) y también leerse directo: cuenta una sola vez
        if (Time.frameCount == frameUltimoIntento) return;
        frameUltimoIntento = Time.frameCount;

        float distancia = Mathf.Abs(marcador.anchoredPosition.x - zona.anchoredPosition.x);

        if (distancia <= anchoZona / 2f)
        {
            aciertos++;
            velocidad += 90f;
            ActualizarContador();
            Destellar(HackUI.ColVerde, Color.white);

            if (aciertos >= objetivo)
            {
                activo = false;
                tarjeta.MostrarEstado("SEÑAL ESTABLE: SINCRONIZACIÓN COMPLETA", HackUI.ColVerde);
                ctx.CompletarModulo();
                return;
            }

            tarjeta.MostrarEstado("SEÑAL SINCRONIZADA " + aciertos + "/" + objetivo + ": LA ZONA SE MOVIÓ", HackUI.ColVerde);
            float nuevoX = Random.Range(-AnchoPista / 2f + anchoZona / 2f, AnchoPista / 2f - anchoZona / 2f);
            zona.anchoredPosition = new Vector2(nuevoX, zona.anchoredPosition.y);
        }
        else
        {
            aciertos = 0;
            ActualizarContador();
            Destellar(HackUI.ColRojo, HackUI.ColRojo);
            tarjeta.MostrarEstado("SEÑAL PERDIDA: EL CONTADOR VUELVE A 0", HackUI.ColRojo);
        }
    }

    private bool LeerEspacio()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        return teclado != null && teclado.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    // ── Aspecto ─────────────────────────────────────────────────────

    private void Destellar(Color colorMarcador, Color colorBorde)
    {
        if (nucleoMarcador != null) nucleoMarcador.color = colorMarcador;
        if (bordeZona != null) bordeZona.color = colorBorde;
        tiempoDestello = DuracionDestello;
    }

    private void RestaurarColores()
    {
        if (nucleoMarcador != null) nucleoMarcador.color = Color.white;
        if (bordeZona != null) bordeZona.color = colorBordeZona;
    }

    private void CrearProgreso()
    {
        segmentos = new Image[objetivo];
        float separacion = 6f;
        float ancho = (AnchoProgreso - separacion * (objetivo - 1)) / objetivo;
        float inicio = -AnchoProgreso / 2f + ancho / 2f;
        for (int i = 0; i < objetivo; i++)
        {
            float x = inicio + i * (ancho + separacion);
            segmentos[i] = cajas.CrearRelleno(tarjeta.Raiz, "Senal_" + i, new Vector2(x, -22f),
                                              new Vector2(ancho, 8f), colorSegmentoVacio, 4f);
        }
    }

    private void ActualizarContador()
    {
        if (textoContador != null) textoContador.text = "SEÑALES SINCRONIZADAS: " + aciertos + "/" + objetivo;
        if (segmentos == null) return;
        for (int i = 0; i < segmentos.Length; i++)
        {
            if (i < aciertos) segmentos[i].color = HackUI.ColAcento;
            else segmentos[i].color = colorSegmentoVacio;
        }
    }
}
