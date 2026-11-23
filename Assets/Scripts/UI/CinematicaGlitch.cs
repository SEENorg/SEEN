using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Cinemática corta de "glitch" al entrar a un nivel desde el menú.
///
/// 1. Toma una foto de la pantalla y la "rompe": franjas corridas, copias celeste y
///    rosada desplazadas, barras de interferencia y parpadeos.
/// 2. Aparece "ENTRANDO AL NIVEL 0X" y la pantalla se va a negro.
/// 3. Se carga el nivel (MainMenuManager.IrANivel) y la cinemática sigue viva en la
///    escena nueva para terminar el glitch mientras se aclara la imagen.
///
/// Usa tiempo real (unscaled), así que funciona aunque el nivel empiece en pausa.
/// </summary>
public class CinematicaGlitch : MonoBehaviour
{
    private const float DuracionEntrada = 1.1f;
    private const float DuracionNegro = 0.45f;
    private const float DuracionSalida = 0.8f;
    private const int CantidadFranjas = 12;
    private const int CantidadBarras = 16;

    private static bool enCurso;

    /// <summary>True mientras se reproduce (para no lanzarla dos veces).</summary>
    public static bool EnCurso
    {
        get { return enCurso; }
    }

    private MainMenuManager menu;
    private string escenaDestino;
    private int nivel;
    private string descripcion;

    private Texture2D captura;
    private RawImage capturaBase;
    private RawImage capturaCian;
    private RawImage capturaMagenta;
    private RawImage[] franjas;
    private Image[] barras;
    private Image negro;
    private Image destello;
    private Text titulo;
    private Text tituloCian;
    private Text tituloMagenta;
    private Text subtitulo;
    private float proximoCambio;

    private readonly Color colorCian = new Color(0.15f, 0.85f, 1f);
    private readonly Color colorMagenta = new Color(1f, 0.18f, 0.62f);

    /// <summary>Empieza la cinemática y al final carga el nivel indicado.</summary>
    public static void Reproducir(MainMenuManager menu, int nivel, string descripcion)
    {
        if (enCurso || menu == null) return;
        enCurso = true;


        GameObject go = new GameObject("CinematicaGlitch");
        DontDestroyOnLoad(go);
        CinematicaGlitch cinematica = go.AddComponent<CinematicaGlitch>();
        cinematica.menu = menu;
        cinematica.nivel = nivel;
        cinematica.descripcion = descripcion;
        cinematica.StartCoroutine(cinematica.Secuencia());
    }

    /// <summary>Igual, pero carga una escena por su nombre (por ejemplo, "SIGUIENTE NIVEL").</summary>
    public static void ReproducirEscena(string escena, int nivel, string descripcion)
    {
        if (enCurso || string.IsNullOrEmpty(escena)) return;
        enCurso = true;

        GameObject go = new GameObject("CinematicaGlitch");
        DontDestroyOnLoad(go);
        CinematicaGlitch cinematica = go.AddComponent<CinematicaGlitch>();
        cinematica.escenaDestino = escena;
        cinematica.nivel = nivel;
        cinematica.descripcion = descripcion;
        cinematica.StartCoroutine(cinematica.Secuencia());
    }

    /// <summary>Número de nivel de una escena del juego (Juego = 1, Nivel2 = 2…). 0 si no se sabe.</summary>
    public static int NumeroDeEscena(string escena)
    {
        if (escena == "Juego") return 1;
        if (escena != null && escena.StartsWith("Nivel"))
        {
            int numero;
            if (int.TryParse(escena.Substring(5), out numero)) return numero;
        }
        return 0;
    }

    private void OnDestroy()
    {
        enCurso = false;
        if (captura != null) Destroy(captura);
    }

    private IEnumerator Secuencia()
    {
        // La foto se toma al final del cuadro, cuando la pantalla ya está dibujada
        yield return new WaitForEndOfFrame();
        captura = ScreenCapture.CaptureScreenshotAsTexture();
        HacerOpaca(captura);
        Construir();

        // ── 1. La imagen se rompe cada vez más ──
        float t = 0f;
        while (t < DuracionEntrada)
        {
            t += Time.unscaledDeltaTime;
            float intensidad = Mathf.Clamp01(t / DuracionEntrada);
            AnimarCaptura(intensidad);
            AnimarBarras(intensidad);
            AnimarTitulo(intensidad, t > DuracionEntrada * 0.3f);
            Escribir(t / DuracionEntrada);

            float alfaNegro = intensidad * 0.55f;
            if (Random.value < intensidad * 0.25f) alfaNegro = Random.Range(0.2f, 0.9f);
            negro.color = new Color(0f, 0f, 0f, alfaNegro);
            destello.color = new Color(1f, 1f, 1f, ParpadeoDestello(intensidad * 0.18f));
            yield return null;
        }

        // ── 2. Corte a negro ──
        t = 0f;
        while (t < DuracionNegro)
        {
            t += Time.unscaledDeltaTime;
            float avance = Mathf.Clamp01(t / DuracionNegro);
            AnimarCaptura(1f);
            AnimarBarras(1f - avance * 0.5f);
            AnimarTitulo(1f, true);
            subtitulo.text = "> ACCESO CONCEDIDO";
            subtitulo.color = new Color(0.15f, 1f, 0.4f);

            float alfaNegro = Mathf.Lerp(0.55f, 1f, avance);
            if (Random.value < 0.2f) alfaNegro = Random.Range(0.6f, 1f);
            negro.color = new Color(0f, 0f, 0f, alfaNegro);
            destello.color = new Color(1f, 1f, 1f, ParpadeoDestello(0.12f));
            yield return null;
        }
        negro.color = Color.black;
        destello.color = Color.clear;
        MostrarCaptura(false);
        ApagarBarras();

        // ── 3. Se carga el nivel (la foto del menú ya no sirve) ──
        yield return null;
        Time.timeScale = 1f;
        if (menu != null)
        {
            menu.IrANivel(nivel);
        }
        else
        {
            Boot.PermitirInicioEscena = true;
            SceneManager.LoadScene(escenaDestino);
        }
        for (int i = 0; i < 3; i++) yield return null;

        // ── 4. El nivel aparece entre interferencias ──
        t = 0f;
        while (t < DuracionSalida)
        {
            t += Time.unscaledDeltaTime;
            float avance = Mathf.Clamp01(t / DuracionSalida);
            float restante = 1f - avance;
            AnimarBarras(restante);

            float alfaNegro = restante * restante;
            if (Random.value < restante * 0.3f) alfaNegro = Mathf.Min(1f, alfaNegro + Random.Range(0.2f, 0.5f));
            negro.color = new Color(0f, 0f, 0f, alfaNegro);
            destello.color = new Color(1f, 1f, 1f, ParpadeoDestello(restante * 0.15f));

            AnimarTitulo(restante, true);
            Color colorTitulo = titulo.color;
            colorTitulo.a = restante;
            titulo.color = colorTitulo;
            Color colorSub = subtitulo.color;
            colorSub.a = restante;
            subtitulo.color = colorSub;
            yield return null;
        }

        Destroy(gameObject);
    }

    // ── Construcción ────────────────────────────────────────────────

    private void Construir()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        CanvasScaler escalador = gameObject.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        // Bloquea clics mientras dura la cinemática
        Image bloqueo = CrearImagen("Bloqueo", new Color(0f, 0f, 0f, 0f));
        bloqueo.raycastTarget = true;

        capturaBase = CrearCaptura("Captura", Color.white);
        capturaCian = CrearCaptura("CapturaCian", new Color(colorCian.r, colorCian.g, colorCian.b, 0.45f));
        capturaMagenta = CrearCaptura("CapturaMagenta", new Color(colorMagenta.r, colorMagenta.g, colorMagenta.b, 0.45f));

        franjas = new RawImage[CantidadFranjas];
        for (int i = 0; i < CantidadFranjas; i++)
        {
            GameObject go = HackUI.NuevoUI(transform, "Franja" + i, Vector2.zero, new Vector2(1920f, 40f));
            RawImage franja = go.AddComponent<RawImage>();
            franja.texture = captura;
            franja.raycastTarget = false;
            franja.enabled = false;
            franjas[i] = franja;
        }

        negro = CrearImagen("Negro", new Color(0f, 0f, 0f, 0f));

        barras = new Image[CantidadBarras];
        for (int i = 0; i < CantidadBarras; i++)
        {
            GameObject go = HackUI.NuevoUI(transform, "Barra" + i, Vector2.zero, new Vector2(1920f, 10f));
            Image barra = go.AddComponent<Image>();
            barra.raycastTarget = false;
            barra.enabled = false;
            barras[i] = barra;
        }

        destello = CrearImagen("Destello", Color.clear);

        string textoTitulo = "ENTRANDO AL NIVEL 0" + nivel;
        if (nivel <= 0) textoTitulo = "CONECTANDO…";
        tituloMagenta = HackUI.CrearTexto(transform, "TituloMagenta", textoTitulo, 72, TextAnchor.MiddleCenter,
                                          new Vector2(0f, 20f), new Vector2(1600f, 110f), colorMagenta);
        tituloCian = HackUI.CrearTexto(transform, "TituloCian", textoTitulo, 72, TextAnchor.MiddleCenter,
                                       new Vector2(0f, 20f), new Vector2(1600f, 110f), colorCian);
        titulo = HackUI.CrearTexto(transform, "Titulo", textoTitulo, 72, TextAnchor.MiddleCenter,
                                   new Vector2(0f, 20f), new Vector2(1600f, 110f), Color.white);
        titulo.fontStyle = FontStyle.Bold;
        tituloCian.fontStyle = FontStyle.Bold;
        tituloMagenta.fontStyle = FontStyle.Bold;

        subtitulo = HackUI.CrearTexto(transform, "Subtitulo", "", 22, TextAnchor.MiddleCenter,
                                      new Vector2(0f, -60f), new Vector2(1600f, 34f), colorCian);

        MostrarTitulo(false);
        MostrarCaptura(captura != null);
    }

    private Image CrearImagen(string nombre, Color color)
    {
        GameObject go = HackUI.NuevoUI(transform, nombre, Vector2.zero, Vector2.zero);
        HackUI.Estirar(go);
        Image imagen = go.AddComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;
        return imagen;
    }

    private RawImage CrearCaptura(string nombre, Color color)
    {
        GameObject go = HackUI.NuevoUI(transform, nombre, Vector2.zero, Vector2.zero);
        HackUI.Estirar(go);
        RawImage imagen = go.AddComponent<RawImage>();
        imagen.texture = captura;
        imagen.color = color;
        imagen.raycastTarget = false;
        return imagen;
    }

    // ── Animación ───────────────────────────────────────────────────

    /// <summary>Copias de color corridas y franjas de la imagen desplazadas hacia los lados.</summary>
    private void AnimarCaptura(float intensidad)
    {
        if (captura == null) return;

        float separacion = intensidad * 18f + Random.Range(0f, intensidad * 10f);
        ((RectTransform)capturaCian.transform).anchoredPosition = new Vector2(-separacion, 0f);
        ((RectTransform)capturaMagenta.transform).anchoredPosition = new Vector2(separacion, Random.Range(-2f, 2f));
        ((RectTransform)capturaBase.transform).anchoredPosition = new Vector2(Random.Range(-1f, 1f) * intensidad * 6f, 0f);

        if (Time.unscaledTime < proximoCambio) return;
        proximoCambio = Time.unscaledTime + Random.Range(0.03f, 0.07f);

        Rect pantalla = ((RectTransform)transform).rect;
        float anchoPantalla = pantalla.width;
        float altoPantalla = pantalla.height;

        for (int i = 0; i < franjas.Length; i++)
        {
            RawImage franja = franjas[i];
            franja.enabled = Random.value < intensidad * 0.8f;
            if (!franja.enabled) continue;

            float alto = Random.Range(8f, 90f);
            float y = Random.Range(-altoPantalla / 2f, altoPantalla / 2f);
            float corrimiento = Random.Range(-1f, 1f) * (40f + intensidad * 220f);
            RectTransform rt = (RectTransform)franja.transform;
            rt.sizeDelta = new Vector2(anchoPantalla, alto);
            rt.anchoredPosition = new Vector2(corrimiento, y);

            // La franja muestra el mismo pedazo de imagen que tapa, pero corrido
            float v = (y + altoPantalla / 2f - alto / 2f) / altoPantalla;
            franja.uvRect = new Rect(0f, v, 1f, alto / altoPantalla);

            float tono = Random.value;
            if (tono < 0.2f) franja.color = new Color(colorCian.r, colorCian.g, colorCian.b, 1f);
            else if (tono < 0.4f) franja.color = new Color(colorMagenta.r, colorMagenta.g, colorMagenta.b, 1f);
            else franja.color = Color.white;
        }
    }

    /// <summary>Barras sólidas de interferencia (celeste, rosado, blanco y negro).</summary>
    private void AnimarBarras(float intensidad)
    {
        if (Random.value > 0.5f) return;   // cambian cada pocos cuadros, no en todos

        for (int i = 0; i < barras.Length; i++)
        {
            Image barra = barras[i];
            barra.enabled = Random.value < intensidad * 0.6f;
            if (!barra.enabled) continue;

            float ancho = Random.Range(200f, 1920f);
            float alto = Random.Range(2f, 26f);
            RectTransform rt = (RectTransform)barra.transform;
            rt.sizeDelta = new Vector2(ancho, alto);
            rt.anchoredPosition = new Vector2(Random.Range(-860f, 860f), Random.Range(-540f, 540f));

            float tono = Random.value;
            float alfa = Random.Range(0.25f, 0.8f);
            if (tono < 0.35f) barra.color = new Color(colorCian.r, colorCian.g, colorCian.b, alfa);
            else if (tono < 0.65f) barra.color = new Color(colorMagenta.r, colorMagenta.g, colorMagenta.b, alfa);
            else if (tono < 0.8f) barra.color = new Color(1f, 1f, 1f, alfa);
            else barra.color = new Color(0f, 0f, 0f, alfa);
        }
    }

    private void AnimarTitulo(float intensidad, bool visible)
    {
        MostrarTitulo(visible);
        if (!visible) return;

        // A veces el título "salta" de lado; las copias de color tiemblan siempre un poco
        float salto = 0f;
        if (Random.value < intensidad * 0.2f) salto = Random.Range(-30f, 30f);
        float separacion = 3f + intensidad * Random.Range(2f, 10f);

        ((RectTransform)titulo.transform).anchoredPosition = new Vector2(salto, 20f);
        ((RectTransform)tituloCian.transform).anchoredPosition = new Vector2(salto - separacion, 20f + Random.Range(-2f, 2f));
        ((RectTransform)tituloMagenta.transform).anchoredPosition = new Vector2(salto + separacion, 20f + Random.Range(-2f, 2f));

        // Parpadeo corto del texto blanco
        titulo.enabled = Random.value > intensidad * 0.08f;
    }

    /// <summary>El subtítulo se escribe letra por letra.</summary>
    private void Escribir(float avance)
    {
        string completo = "> CONECTANDO CON EL NODO 0" + nivel;
        if (!string.IsNullOrEmpty(descripcion)) completo += "  //  " + descripcion;

        float desde = 0.3f;
        float porcentaje = Mathf.Clamp01((avance - desde) / (1f - desde));
        int letras = Mathf.RoundToInt(completo.Length * porcentaje);
        string texto = completo.Substring(0, letras);
        if (Mathf.Repeat(Time.unscaledTime, 0.4f) < 0.2f) texto += "_";
        subtitulo.text = texto;
    }

    /// <summary>
    /// En algunos proyectos URP la foto sale con transparencia; se fuerza opaca
    /// para que se vea la imagen y no un hueco.
    /// </summary>
    private void HacerOpaca(Texture2D textura)
    {
        if (textura == null) return;
        Color32[] pixeles = textura.GetPixels32();
        for (int i = 0; i < pixeles.Length; i++) pixeles[i].a = 255;
        textura.SetPixels32(pixeles);
        textura.Apply(false);
    }

    private float ParpadeoDestello(float probabilidad)
    {
        if (Random.value < probabilidad) return Random.Range(0.05f, 0.25f);
        return 0f;
    }

    private void MostrarTitulo(bool visible)
    {
        titulo.enabled = visible;
        tituloCian.enabled = visible;
        tituloMagenta.enabled = visible;
        subtitulo.enabled = visible;
    }

    private void MostrarCaptura(bool visible)
    {
        if (capturaBase == null) return;
        bool hayCaptura = visible && captura != null;
        capturaBase.enabled = hayCaptura;
        capturaCian.enabled = hayCaptura;
        capturaMagenta.enabled = hayCaptura;
        for (int i = 0; i < franjas.Length; i++)
        {
            if (!hayCaptura) franjas[i].enabled = false;
        }
    }

    private void ApagarBarras()
    {
        for (int i = 0; i < barras.Length; i++) barras[i].enabled = false;
    }
}
