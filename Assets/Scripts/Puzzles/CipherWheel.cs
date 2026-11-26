using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Tabla (rueda) para descifrar el código César. Se abre con TAB.
///  • Rueda EXTERIOR (fija): letras A-Z (las del código), con la A arriba y en sentido horario.
///  • Rueda INTERIOR (gira): letras a-z (las originales, siempre verticales).
///  • Solo 2 ruedas, sin números. Arranca ya en +1: la B del código es la C original.
///  • Líneas radiales COMPLETAS que delimitan cada espacio (cubren ambas ruedas).
///  • Gira siguiendo el arrastre del mouse (horario = horario).
///  • Tablero de notas a la derecha (estilo cuaderno de policía con anillos)
///    para escribir la decodificación.
/// </summary>
public class CipherWheel : MonoBehaviour
{
    public static bool Abierta { get; private set; }

    [Header("Tamaños (píxeles del panel)")]
    [SerializeField] private float diametroExterior = 620f;
    [SerializeField] private float diametroInterior = 400f;
    [SerializeField] private float radioLetrasExt = 262f;
    [SerializeField] private float radioLetrasInt = 158f;
    [SerializeField] private float radioLineaDesde = 95f;
    [SerializeField] private float radioLineaHasta = 292f;
    [SerializeField] private int tamanioLetraExt = 34;
    [SerializeField] private int tamanioLetraInt = 26;

    [Header("Paleta cyber")]
    [SerializeField] private Color colorFondoPantalla = new Color(0.02f, 0.03f, 0.06f, 0.94f);
    [SerializeField] private Color colorPanel = new Color(0.04f, 0.06f, 0.10f, 1f);
    [SerializeField] private Color colorAcento = new Color(0.15f, 0.85f, 1f);           // cian del juego
    [SerializeField] private Color colorCodigo = new Color(0.45f, 0.92f, 1f);           // letras de AFUERA
    [SerializeField] private Color colorOriginal = new Color(0.30f, 1f, 0.50f);         // letras de ADENTRO
    [SerializeField] private Color colorDiscoExt = new Color(0.06f, 0.09f, 0.14f);
    [SerializeField] private Color colorDiscoInt = new Color(0.04f, 0.13f, 0.11f);
    [SerializeField] private Color colorNotas = new Color(0.03f, 0.05f, 0.08f, 1f);
    [SerializeField] private Color colorTextoNotas = new Color(0.80f, 1f, 0.90f);

    [Header("Tamaño y posición")]
    [Tooltip("Tamaño de todo (1 = grande, 0.8 = recomendado, 0.7 = más pequeño)")]
    [SerializeField] private float escalaPantalla = 0.8f;
    [Tooltip("Posición horizontal de la rueda (negativo = izquierda)")]
    [SerializeField] private float posXRueda = -310f;
    [Tooltip("Posición horizontal del cuaderno de notas")]
    [SerializeField] private float posXCuaderno = 350f;

    private Canvas canvas;
    private GameObject panel;
    private RectTransform discoInterior;          // gira (disco visual)
    private RectTransform grupoLetrasInteriores;  // estático, letras verticales
    private Text[] textosInteriores;
    private RectTransform[] lineas;               // divisorias completas
    private Text textoDesplazamiento;
    private Text textoEjemplo;
    private InputField campoNotas;

    // Posición inicial de la rueda: 25 equivale a +1 (la letra de afuera B → la de adentro c)
    private int paso = 25;
    private bool arrastrando;
    private float anguloPrevio;
    private float anguloAcumulado;
    private int pasoAlEmpezar;
    private bool hackEstabaAbierto;

    private const float PasoGrados = 360f / 26f;
    private const float AnguloInicial = 90f;   // la A arriba del todo

    private static Font _fuente;
    private static Font Fuente
    {
        get
        {
            if (_fuente == null)
                _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _fuente;
        }
    }

    private void Awake()
    {
        AsegurarEventSystem();
    }

    private void Update()
    {
        if (LeerTab())
        {
            if (Abierta) Cerrar();
            else if (PuzzleDescifrar.Activo) Abrir();   // solo sirve dentro del puzzle de descifrar
            return;
        }

        if (!Abierta) return;
        if (LeerEscape()) { Cerrar(); return; }

        // Teclado: solo gira si NO se está escribiendo en el cuaderno
        // (si no, las letras A / D y las flechas moverían la rueda sin querer)
        bool escribiendo = campoNotas != null && campoNotas.isFocused;
        if (!escribiendo)
        {
            if (LeerTecla(Key.LeftArrow) || LeerTecla(Key.A)) Girar(-1);
            if (LeerTecla(Key.RightArrow) || LeerTecla(Key.D)) Girar(1);
        }

        // Mouse: arrastrar
        if (MouseAbajo())
        {
            if (MouseSobreRueda())
            {
                arrastrando = true;
                anguloPrevio = AnguloMouse();
                anguloAcumulado = 0f;
                pasoAlEmpezar = paso;
            }
        }
        if (arrastrando && MouseArriba())
        {
            arrastrando = false;
            paso = (((pasoAlEmpezar + Mathf.RoundToInt(-anguloAcumulado / PasoGrados)) % 26) + 26) % 26;
            AplicarRotacion();
        }
        if (arrastrando)
        {
            float a = AnguloMouse();
            float delta = Mathf.DeltaAngle(anguloPrevio, a);
            anguloPrevio = a;
            anguloAcumulado += delta;
            int nuevo = (((pasoAlEmpezar + Mathf.RoundToInt(-anguloAcumulado / PasoGrados)) % 26) + 26) % 26;
            if (nuevo != paso) { paso = nuevo; AplicarRotacion(); }
        }
    }

    // ── Abrir / cerrar ───────────────────────────────────────────

    private bool desactiveJugador;

    private void Abrir()
    {
        if (canvas == null) ConstruirCanvas();
        Abierta = true;
        canvas.gameObject.SetActive(true);

        // Si ya estábamos hackeando, mantener el juego en pausa y seguir el hackeo
        hackEstabaAbierto = HackManager.Abierto;

        // Si venimos de un hackeo, el juego ya está congelado
        if (!HackManager.Abierto) Time.timeScale = 0f;
        Cursor.visible = true;

        var pc = Object.FindFirstObjectByType<PlayerController>();
        desactiveJugador = pc != null && pc.enabled;
        if (pc != null) pc.enabled = false;
    }

    private void Cerrar()
    {
        Abierta = false;
        if (canvas != null) canvas.gameObject.SetActive(false);

        if (hackEstabaAbierto)
        {
            // Volver EXACTamente como estábamos: hackeo en pausa, ratón libre
        // Si venimos de un hackeo, el juego ya está congelado
        if (!HackManager.Abierto) Time.timeScale = 0f;
            Cursor.visible = true;
            return;
        }

        if (!HackManager.Abierto) Time.timeScale = 1f;   // no descongelar si seguimos hackeando
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (desactiveJugador)
        {
            var pc = Object.FindFirstObjectByType<PlayerController>();
            if (pc != null) pc.enabled = true;
        }
    }

    // ── Construcción de la UI ────────────────────────────────────

    private void ConstruirCanvas()
    {
        var canvasGO = new GameObject("CipherWheelCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;   // ajusta por ALTO: lo de arriba nunca se corta
        canvasGO.AddComponent<GraphicRaycaster>();

        var fondoGO = NuevoUI(canvasGO.transform, "Fondo", Vector2.zero, new Vector2(1920f, 1080f));
        Estirar(fondoGO);
        fondoGO.AddComponent<Image>().color = colorFondoPantalla;

        ConstruirRueda();
        ConstruirTablero();

        AplicarRotacion();
    }

    private void ConstruirRueda()
    {
        panel = NuevoUI(canvasGO_transform(), "PanelRueda", new Vector2(posXRueda, 0f), new Vector2(820f, 900f));
        panel.transform.localScale = Vector3.one * escalaPantalla;
        panel.AddComponent<Image>().color = colorPanel;
        Marco(panel.transform, new Vector2(820f, 900f), colorAcento * 0.7f);

        CrearTexto(panel.transform, "Titulo", "> TABLA DE DESCIFRADO", 28, TextAnchor.UpperCenter,
                   new Vector2(0f, 420f), new Vector2(780f, 40f), colorAcento).fontStyle = FontStyle.Bold;
        CrearTexto(panel.transform, "Ayuda", "Gira el disco con el mouse o con ← → / A D.\n" +
                                             "Busca la letra del código afuera y lee su pareja adentro.", 18,
                   TextAnchor.UpperCenter, new Vector2(0f, 382f), new Vector2(800f, 54f),
                   new Color(0.60f, 0.72f, 0.80f));

        var centro = NuevoUI(panel.transform, "CentroRueda", new Vector2(0f, 60f), Vector2.zero).transform;

        // Disco exterior (fijo): borde cian
        var discoExt = NuevoUI(centro, "DiscoExterior", Vector2.zero,
                               new Vector2(diametroExterior, diametroExterior));
        var imgExt = discoExt.AddComponent<Image>();
        imgExt.sprite = SpriteDisco(diametroExterior, 5f, colorDiscoExt, colorAcento);
        imgExt.raycastTarget = false;

        // Letras exteriores (código): A arriba, luego en sentido HORARIO
        for (int i = 0; i < 26; i++)
        {
            char letra = (char)('A' + i);
            float ang = (AnguloInicial - i * PasoGrados) * Mathf.Deg2Rad;
            var t = CrearTexto(centro, $"Ext_{i}", letra.ToString(), tamanioLetraExt, TextAnchor.MiddleCenter,
                               new Vector2(Mathf.Cos(ang) * radioLetrasExt, Mathf.Sin(ang) * radioLetrasExt),
                               new Vector2(54f, 44f), colorCodigo);
            t.fontStyle = FontStyle.Bold;
        }

        // Disco interior (gira solo el visual): borde verde
        discoInterior = (RectTransform)NuevoUI(centro, "GrupoInterior", Vector2.zero, Vector2.zero).transform;
        var discoInt = NuevoUI(discoInterior, "DiscoInterior", Vector2.zero,
                               new Vector2(diametroInterior, diametroInterior));
        var imgInt = discoInt.AddComponent<Image>();
        imgInt.sprite = SpriteDisco(diametroInterior, 4f, colorDiscoInt, colorOriginal * 0.8f);
        imgInt.raycastTarget = false;

        // Letras interiores (originales, siempre verticales, se reposicionan)
        grupoLetrasInteriores = (RectTransform)NuevoUI(centro, "LetrasInterior", Vector2.zero, Vector2.zero).transform;
        textosInteriores = new Text[26];
        for (int i = 0; i < 26; i++)
        {
            char letra = (char)('a' + i);
            textosInteriores[i] = CrearTexto(grupoLetrasInteriores, $"Int_{i}", letra.ToString(),
                tamanioLetraInt, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(44f, 34f), colorOriginal);
            textosInteriores[i].fontStyle = FontStyle.Bold;
        }

        // Líneas divisorias COMPLETAS (cubren ambas ruedas)
        lineas = new RectTransform[26];
        for (int i = 0; i < 26; i++)
            lineas[i] = LineaRadial(centro, $"Linea_{i}", radioLineaDesde, radioLineaHasta,
                                    new Color(colorAcento.r, colorAcento.g, colorAcento.b, 0.30f));

        // Eje central
        var eje = NuevoUI(centro, "Eje", Vector2.zero, new Vector2(22f, 22f));
        eje.AddComponent<Image>().color = colorAcento;

        // Marca superior (indica dónde está la A)
        var marca = NuevoUI(centro, "Marca", new Vector2(0f, radioLineaHasta + 12f), new Vector2(34f, 9f));
        marca.AddComponent<Image>().color = colorAcento;

        // Leyenda: qué es cada anillo
        CrearTexto(panel.transform, "LeyendaAfuera", "AFUERA = CÓDIGO", 20, TextAnchor.MiddleCenter,
                   new Vector2(-170f, -300f), new Vector2(300f, 30f), colorCodigo).fontStyle = FontStyle.Bold;
        CrearTexto(panel.transform, "LeyendaAdentro", "ADENTRO = ORIGINAL", 20, TextAnchor.MiddleCenter,
                   new Vector2(170f, -300f), new Vector2(300f, 30f), colorOriginal).fontStyle = FontStyle.Bold;

        // Lecturas
        textoDesplazamiento = CrearTexto(panel.transform, "Desplazamiento", "Desplazamiento: +1", 24,
                                         TextAnchor.MiddleCenter, new Vector2(0f, -346f), new Vector2(500f, 36f),
                                         Color.white);
        textoDesplazamiento.fontStyle = FontStyle.Bold;

        textoEjemplo = CrearTexto(panel.transform, "Ejemplo", "", 20, TextAnchor.MiddleCenter,
                                  new Vector2(0f, -390f), new Vector2(640f, 32f), colorOriginal);

        var btnCerrar = CrearBoton(panel.transform, "BotonCerrar", "CERRAR (TAB)", new Vector2(0f, -436f),
                                   new Vector2(260f, 48f));
        btnCerrar.onClick.AddListener(Cerrar);
    }

    private void ConstruirTablero()
    {
        // ── Terminal de notas ──
        var hoja = NuevoUI(canvasGO_transform(), "Tablero", new Vector2(posXCuaderno, 10f), new Vector2(680f, 780f));
        hoja.transform.localScale = Vector3.one * escalaPantalla;

        var imgHoja = hoja.AddComponent<Image>();
        imgHoja.color = colorNotas;
        imgHoja.raycastTarget = false;
        Marco(hoja.transform, new Vector2(680f, 780f), colorAcento * 0.7f);

        // Barra superior con título
        var barra = NuevoUI(hoja.transform, "BarraSup", new Vector2(0f, 351f), new Vector2(680f, 78f));
        var imgBarra = barra.AddComponent<Image>();
        imgBarra.color = new Color(colorAcento.r, colorAcento.g, colorAcento.b, 0.12f);
        imgBarra.raycastTarget = false;

        CrearTexto(hoja.transform, "TituloNotas", "> NOTAS DE DECODIFICACIÓN", 26, TextAnchor.MiddleCenter,
                   new Vector2(0f, 366f), new Vector2(640f, 36f), colorAcento).fontStyle = FontStyle.Bold;
        CrearTexto(hoja.transform, "SubtituloNotas", "Escribe aquí tu traducción, letra por letra", 18,
                   TextAnchor.MiddleCenter, new Vector2(0f, 334f), new Vector2(640f, 26f),
                   new Color(0.60f, 0.72f, 0.80f));

        // Margen verde
        var margen = NuevoUI(hoja.transform, "MargenVerde", new Vector2(-290f, -40f), new Vector2(2f, 640f));
        var imgM = margen.AddComponent<Image>();
        imgM.color = new Color(colorOriginal.r, colorOriginal.g, colorOriginal.b, 0.45f);
        imgM.raycastTarget = false;

        // Área de texto (solo acá se escribe)
        var campoGO = NuevoUI(hoja.transform, "CampoNotas", new Vector2(15f, -40f), new Vector2(580f, 620f));
        var campoImg = campoGO.AddComponent<Image>();
        campoImg.color = new Color(0f, 0f, 0f, 0f);

        var texto = CrearTexto(campoGO.transform, "Text", "", 28, TextAnchor.UpperLeft,
                               new Vector2(0f, 0f), new Vector2(580f, 620f), colorTextoNotas);
        texto.lineSpacing = 1.3f;

        // Mide cuánto ocupa de alto UNA línea de texto, para dibujar las rayas justo ahí
        texto.text = "A";
        float alto1 = texto.preferredHeight;
        texto.text = "A\nA";
        float alto2 = texto.preferredHeight;
        texto.text = "";
        float altoLinea = Mathf.Max(20f, alto2 - alto1);

        // Rayas de la libreta: una debajo de cada renglón de texto
        float arribaCampo = -40f + 620f / 2f;
        int cantidad = Mathf.FloorToInt(620f / altoLinea);
        for (int i = 0; i < cantidad; i++)
        {
            float y = arribaCampo - altoLinea * (i + 1) + 5f;
            var l = NuevoUI(hoja.transform, $"Raya_{i}", new Vector2(15f, y), new Vector2(580f, 1.5f));
            var imgL = l.AddComponent<Image>();
            imgL.color = new Color(colorAcento.r, colorAcento.g, colorAcento.b, 0.16f);
            imgL.raycastTarget = false;
        }
        campoGO.transform.SetAsLastSibling();   // el campo queda por encima de las rayas

        campoNotas = campoGO.AddComponent<InputField>();
        campoNotas.lineType = InputField.LineType.MultiLineNewline;
        campoNotas.textComponent = texto;
        campoNotas.targetGraphic = campoImg;
        campoNotas.selectionColor = new Color(colorAcento.r, colorAcento.g, colorAcento.b, 0.35f);

        // Cursor (la "rayita"): grueso y brillante para que se vea siempre
        campoNotas.customCaretColor = true;
        campoNotas.caretColor = Color.white;
        campoNotas.caretWidth = 3;
        campoNotas.caretBlinkRate = 1.2f;

        // Placeholder con un ejemplo para que se entienda cómo usarlo
        var ph = CrearTexto(campoGO.transform, "Placeholder", "Ejemplo:\nB → C\nM → N\n\n(haz clic aquí y escribe)", 28,
                            TextAnchor.UpperLeft, Vector2.zero, new Vector2(580f, 620f),
                            new Color(0.40f, 0.55f, 0.62f));
        ph.lineSpacing = 1.3f;
        campoNotas.placeholder = ph;
        campoNotas.text = "";
    }

    // Marco fino de 4 lados (para dar el contorno cian a los paneles)
    private void Marco(Transform parent, Vector2 tamano, Color color)
    {
        float t = 2f;
        BordeUI(parent, "MarcoSup", new Vector2(0f, tamano.y / 2f), new Vector2(tamano.x, t), color);
        BordeUI(parent, "MarcoInf", new Vector2(0f, -tamano.y / 2f), new Vector2(tamano.x, t), color);
        BordeUI(parent, "MarcoIzq", new Vector2(-tamano.x / 2f, 0f), new Vector2(t, tamano.y), color);
        BordeUI(parent, "MarcoDer", new Vector2(tamano.x / 2f, 0f), new Vector2(t, tamano.y), color);
    }

    private void BordeUI(Transform parent, string nombre, Vector2 pos, Vector2 size, Color color)
    {
        var go = NuevoUI(parent, nombre, pos, size);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    // ── Lógica de rotación ───────────────────────────────────────

    private void Girar(int dir)
    {
        paso = ((paso + dir) % 26 + 26) % 26;
        AplicarRotacion();
    }

    private void AplicarRotacion()
    {
        // El disco visual gira en sentido horario cuando paso aumenta
        if (discoInterior != null)
            discoInterior.localRotation = Quaternion.Euler(0f, 0f, -paso * PasoGrados);

        // Letras: se REPOSICIONAN (siempre verticales)
        if (textosInteriores != null)
        {
            for (int i = 0; i < 26; i++)
            {
                float ang = (AnguloInicial - (i + paso) * PasoGrados) * Mathf.Deg2Rad;

                textosInteriores[i].rectTransform.anchoredPosition =
                    new Vector2(Mathf.Cos(ang) * radioLetrasInt, Mathf.Sin(ang) * radioLetrasInt);
                textosInteriores[i].rectTransform.localRotation = Quaternion.identity;
            }
        }

        // Líneas divisorias: también se mueven (mitad entre letra y letra)
        if (lineas != null)
        {
            for (int i = 0; i < 26; i++)
            {
                float ang = (AnguloInicial - (i + 0.5f + paso) * PasoGrados) * Mathf.Deg2Rad;
                PosicionarLinea(lineas[i], ang);
            }
        }

        if (textoDesplazamiento != null)
        {
            // Para descifrar se AVANZA 'avance' letras (paso 25 → +1)
            int avance = ((-paso + 13) % 26 + 26) % 26 - 13;
            string signo = avance > 0 ? "+" : "";
            textoDesplazamiento.text = $"Desplazamiento rueda: {signo}{avance}  (Código: +{PuzzleDescifrar.DesplazamientoActual})";
        }

        if (textoEjemplo != null)
        {
            int cifrada = 1;   // letra "B" del código
            int original = ((cifrada - paso) % 26 + 26) % 26;
            textoEjemplo.text = $"Ejemplo: la letra «{(char)('A' + cifrada)}» del código es la «{(char)('A' + original)}»";
        }
    }

    // ── Líneas radiales ──────────────────────────────────────────

    private RectTransform LineaRadial(Transform parent, string nombre, float rDesde, float rHasta, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        PosicionarLinea(rt, AnguloInicial * Mathf.Deg2Rad);
        return rt;
    }

    private void PosicionarLinea(RectTransform rt, float angRad)
    {
        float dx = Mathf.Cos(angRad);
        float dy = Mathf.Sin(angRad);
        Vector2 inicio = new Vector2(dx * radioLineaDesde, dy * radioLineaDesde);
        Vector2 fin = new Vector2(dx * radioLineaHasta, dy * radioLineaHasta);

        float dist = (fin - inicio).magnitude;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(dist, 3f);
        rt.anchoredPosition = (inicio + fin) / 2f;
        rt.localRotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(fin.y - inicio.y, fin.x - inicio.x) * Mathf.Rad2Deg);
    }

    // ── Mouse ─────────────────────────────────────────────────────

    private Transform canvasGO_transform()
    {
        if (canvas == null) ConstruirCanvas();
        return canvas.transform;
    }

    private Vector2 MouseLocal()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)panel.transform, PosMouse(), null, out Vector2 local);
        return local;
    }

    private Vector2 CentroRuedaLocal() => new Vector2(0f, 60f);

    private bool MouseSobreRueda()
    {
        if (panel == null) return false;
        return Vector2.Distance(MouseLocal(), CentroRuedaLocal()) <= radioLineaHasta + 10f;
    }

    private float AnguloMouse()
    {
        Vector2 d = MouseLocal() - CentroRuedaLocal();
        return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
    }

    // ── Texturas ──────────────────────────────────────────────────

    private static readonly Dictionary<int, Sprite> SpritesDisco = new Dictionary<int, Sprite>();

    /// <summary>Disco liso con borde de color y bordes suaves (sin ruido, estilo digital).</summary>
    private static Sprite SpriteDisco(float diametro, float bordePx, Color relleno, Color borde)
    {
        int key = (int)(diametro * 1000);
        if (SpritesDisco.TryGetValue(key, out var s)) return s;

        int size = Mathf.Clamp(Mathf.RoundToInt(diametro), 64, 1024);
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float centro = (size - 1) / 2f;
        float radio = size / 2f - 1.5f;
        float radioInterno = radio - bordePx;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - centro;
                float dy = y - centro;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                // Borde exterior suave (1.5 px de difuminado)
                float alfa = Mathf.Clamp01((radio + 1f - d) / 1.5f);
                // Transición relleno → borde
                float t = Mathf.Clamp01((d - radioInterno) / 1.5f);
                Color c = Color.Lerp(relleno, borde, t);
                c.a = alfa;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        SpritesDisco[key] = sprite;
        return sprite;
    }

    // ── Helpers de UI ─────────────────────────────────────────────

    private void AsegurarEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    private GameObject NuevoUI(Transform parent, string nombre, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return go;
    }

    private void Estirar(GameObject go)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private Text CrearTexto(Transform parent, string nombre, string contenido, int size,
                            TextAnchor align, Vector2 pos, Vector2 dimension, Color color)
    {
        var go = NuevoUI(parent, nombre, pos, dimension);
        var txt = go.AddComponent<Text>();
        txt.text = contenido;
        txt.font = Fuente;
        txt.fontSize = size;
        txt.alignment = align;
        txt.color = color;
        txt.raycastTarget = false;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        return txt;
    }

    private Button CrearBoton(Transform parent, string nombre, string etiqueta, Vector2 pos, Vector2 size)
    {
        var go = NuevoUI(parent, nombre, pos, size);
        var img = go.AddComponent<Image>();
        Color baseColor = new Color(0.08f, 0.14f, 0.20f);
        img.color = baseColor;
        BordeUI(go.transform, "BordeSup", new Vector2(0f, size.y / 2f), new Vector2(size.x, 2f), colorAcento);
        BordeUI(go.transform, "BordeInf", new Vector2(0f, -size.y / 2f), new Vector2(size.x, 2f), colorAcento);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        var c = btn.colors;
        c.normalColor = baseColor;
        c.highlightedColor = Color.Lerp(baseColor, colorAcento, 0.35f);
        c.pressedColor = Color.Lerp(baseColor, colorAcento, 0.6f);
        c.fadeDuration = 0.06f;
        btn.colors = c;
        CrearTexto(btn.transform, "Texto", etiqueta, 20, TextAnchor.MiddleCenter, Vector2.zero, size, colorAcento)
            .fontStyle = FontStyle.Bold;
        return btn;
    }

    // ── Input (SOLO nuevo Input System) ───────────────────────────

    private Vector2 PosMouse()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
        return Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    private bool MouseAbajo()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private bool MouseArriba()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
        return Input.GetMouseButtonUp(0);
#endif
    }

    private bool LeerTab()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.tabKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Tab);
#endif
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

    private bool LeerTecla(Key tecla)
    {
        var kb = Keyboard.current;
        return kb != null && kb[tecla].wasPressedThisFrame;
    }
}