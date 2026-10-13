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
///  • Rueda EXTERIOR (fija): letras A-Z, con la A arriba y en sentido horario.
///  • Rueda INTERIOR (gira): letras a-z y sus números 1-26 (siempre verticales).
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
    [SerializeField] private float radioNumeros = 118f;
    [SerializeField] private float radioLineaDesde = 95f;
    [SerializeField] private float radioLineaHasta = 292f;
    [SerializeField] private int tamanioLetraExt = 34;
    [SerializeField] private int tamanioLetraInt = 26;
    [SerializeField] private int tamanioNumero = 22;

    [Header("Paleta vintage")]
    [SerializeField] private Color colorDisco = new Color(0.906f, 0.867f, 0.769f);
    [SerializeField] private Color colorBorde = new Color(0.556f, 0.478f, 0.361f);
    [SerializeField] private Color colorLetraExt = new Color(0.29f, 0.22f, 0.14f);
    [SerializeField] private Color colorLetraInt = new Color(0.29f, 0.22f, 0.14f);
    [SerializeField] private Color colorNumero = new Color(0.45f, 0.35f, 0.20f);
    [SerializeField] private Color colorFondo = new Color(0.05f, 0.05f, 0.07f, 0.92f);
    [SerializeField] private Color colorHoja = new Color(0.965f, 0.945f, 0.875f);
    [SerializeField] private Color colorTinta = new Color(0.20f, 0.16f, 0.10f);

    [Header("Posición")]
    [SerializeField] private float xRueda = -250f;
    [SerializeField] private float xTablero = 580f;

    private Canvas canvas;
    private GameObject panel;
    private RectTransform discoInterior;          // gira (disco visual)
    private RectTransform grupoLetrasInteriores;  // estático, letras verticales
    private Text[] textosInteriores;
    private Text[] textosNumeros;
    private RectTransform[] lineas;               // divisorias completas
    private Text textoDesplazamiento;
    private Text textoEjemplo;
    private InputField campoNotas;

    private int paso;
    private bool arrastrando;
    private float anguloPrevio;
    private float anguloAcumulado;
    private bool hackEstabaAbierto;

    private const float PasoGrados = 360f / 26f;
    private const float AnguloInicial = 90f;   // la A arriba del todo

    private static Font _fuente;
    private static Font Fuente => _fuente ?? (_fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

    private void Awake()
    {
        AsegurarEventSystem();
    }

    private void Update()
    {
        if (LeerTab())
        {
            if (Abierta) Cerrar();
            else Abrir();
            return;
        }

        if (!Abierta) return;
        if (LeerEscape()) { Cerrar(); return; }

        // Teclado
        if (LeerTecla(Key.LeftArrow) || LeerTecla(Key.A)) Girar(-1);
        if (LeerTecla(Key.RightArrow) || LeerTecla(Key.D)) Girar(1);

        // Mouse: arrastrar
        if (MouseAbajo())
        {
            if (MouseSobreRueda())
            {
                arrastrando = true;
                anguloPrevio = AnguloMouse();
                anguloAcumulado = 0f;
            }
        }
        if (arrastrando && MouseArriba())
        {
            arrastrando = false;
            paso = ((Mathf.RoundToInt(-anguloAcumulado / PasoGrados) % 26) + 26) % 26;
            AplicarRotacion();
        }
        if (arrastrando)
        {
            float a = AnguloMouse();
            float delta = Mathf.DeltaAngle(anguloPrevio, a);
            anguloPrevio = a;
            anguloAcumulado += delta;
            int nuevo = ((Mathf.RoundToInt(-anguloAcumulado / PasoGrados) % 26) + 26) % 26;
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

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
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
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Time.timeScale = 1f;
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
        canvasGO.AddComponent<GraphicRaycaster>();

        var fondoGO = NuevoUI(canvasGO.transform, "Fondo", Vector2.zero, new Vector2(1920f, 1080f));
        Estirar(fondoGO);
        fondoGO.AddComponent<Image>().color = colorFondo;

        ConstruirRueda();
        ConstruirTablero();

        AplicarRotacion();
    }

    private void ConstruirRueda()
    {
        panel = NuevoUI(canvasGO_transform(), "PanelRueda", new Vector2(xRueda, 0f), new Vector2(820f, 900f));
        panel.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.09f, 1f);

        CrearTexto(panel.transform, "Titulo", "TABLA DE DESCIFRADO CÉSAR", 28, TextAnchor.UpperCenter,
                   new Vector2(0f, 425f), new Vector2(780f, 40f), colorBorde).fontStyle = FontStyle.Bold;
        CrearTexto(panel.transform, "Ayuda", "Girá el disco interior con el mouse (horario = horario)\n" +
                                             "o con ← → / A D.  Alineá el desplazamiento y leé debajo de cada letra", 18,
                   TextAnchor.UpperCenter, new Vector2(0f, 386f), new Vector2(800f, 54f),
                   new Color(0.62f, 0.52f, 0.36f));

        var centro = NuevoUI(panel.transform, "CentroRueda", new Vector2(0f, 60f), Vector2.zero).transform;

        // Disco exterior (fijo)
        var discoExt = NuevoUI(centro, "DiscoExterior", Vector2.zero,
                               new Vector2(diametroExterior, diametroExterior));
        var imgExt = discoExt.AddComponent<Image>();
        imgExt.sprite = SpriteDisco(diametroExterior, 0.055f, 0.10f, 4242);
        imgExt.color = colorDisco;
        imgExt.raycastTarget = false;

        // Letras exteriores: A arriba, luego en sentido HORARIO
        for (int i = 0; i < 26; i++)
        {
            char letra = (char)('A' + i);
            float ang = (AnguloInicial - i * PasoGrados) * Mathf.Deg2Rad;
            var t = CrearTexto(centro, $"Ext_{i}", letra.ToString(), tamanioLetraExt, TextAnchor.MiddleCenter,
                               new Vector2(Mathf.Cos(ang) * radioLetrasExt, Mathf.Sin(ang) * radioLetrasExt),
                               new Vector2(54f, 44f), colorLetraExt);
            t.fontStyle = FontStyle.Bold;
        }

        // Disco interior (gira solo el visual)
        discoInterior = (RectTransform)NuevoUI(centro, "GrupoInterior", Vector2.zero, Vector2.zero).transform;
        var discoInt = NuevoUI(discoInterior, "DiscoInterior", Vector2.zero,
                               new Vector2(diametroInterior, diametroInterior));
        var imgInt = discoInt.AddComponent<Image>();
        imgInt.sprite = SpriteDisco(diametroInterior, 0.06f, 0.12f, 777);
        imgInt.color = colorDisco;
        imgInt.raycastTarget = false;

        // Letras y números interiores (estáticos, se reposicionan)
        grupoLetrasInteriores = (RectTransform)NuevoUI(centro, "LetrasInterior", Vector2.zero, Vector2.zero).transform;
        textosInteriores = new Text[26];
        textosNumeros = new Text[26];
        for (int i = 0; i < 26; i++)
        {
            char letra = (char)('a' + i);
            textosInteriores[i] = CrearTexto(grupoLetrasInteriores, $"Int_{i}", letra.ToString(),
                tamanioLetraInt, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(44f, 34f), colorLetraInt);
            textosNumeros[i] = CrearTexto(grupoLetrasInteriores, $"Num_{i}", (i + 1).ToString(),
                tamanioNumero, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(40f, 28f), colorNumero);
        }

        // Líneas divisorias COMPLETAS (cubren ambas ruedas)
        lineas = new RectTransform[26];
        for (int i = 0; i < 26; i++)
            lineas[i] = LineaRadial(centro, $"Linea_{i}", radioLineaDesde, radioLineaHasta, colorBorde * 0.85f);

        // Eje central
        var eje = NuevoUI(centro, "Eje", Vector2.zero, new Vector2(26f, 26f));
        eje.AddComponent<Image>().color = colorBorde;

        // Marca superior
        var marca = NuevoUI(centro, "Marca", new Vector2(0f, radioLineaHasta + 12f), new Vector2(34f, 9f));
        marca.AddComponent<Image>().color = colorBorde;

        // Lecturas
        textoDesplazamiento = CrearTexto(panel.transform, "Desplazamiento", "Desplazamiento: 0", 24,
                                         TextAnchor.MiddleCenter, new Vector2(0f, -350f), new Vector2(500f, 36f),
                                         new Color(0.85f, 0.75f, 0.50f));
        textoDesplazamiento.fontStyle = FontStyle.Bold;

        textoEjemplo = CrearTexto(panel.transform, "Ejemplo", "", 20, TextAnchor.MiddleCenter,
                                  new Vector2(0f, -394f), new Vector2(640f, 32f), new Color(0.45f, 0.85f, 0.55f));

        var btnCerrar = CrearBoton(panel.transform, "BotonCerrar", "CERRAR (TAB)", new Vector2(0f, -436f),
                                   new Vector2(260f, 52f));
        btnCerrar.onClick.AddListener(Cerrar);
    }

    private void ConstruirTablero()
    {
        // ── Cuaderno de notas estilo policía ──
        var hoja = NuevoUI(canvasGO_transform(), "Tablero", new Vector2(xTablero, 10f), new Vector2(680f, 780f));

        var imgHoja = hoja.AddComponent<Image>();
        imgHoja.color = colorHoja;
        imgHoja.raycastTarget = false;

        // Borde del cuaderno
        var bordeSup = NuevoUI(hoja.transform, "BordeSup", new Vector2(0f, 388f), new Vector2(680f, 4f));
        bordeSup.AddComponent<Image>().color = colorBorde * 0.9f;

        // Líneas rayadas
        for (int i = 0; i < 14; i++)
        {
            float y = 300f - i * 48f;
            if (y < -360f) break;
            var l = NuevoUI(hoja.transform, $"Linea_{i}", new Vector2(0f, y), new Vector2(600f, 2f));
            l.AddComponent<Image>().color = new Color(0.55f, 0.65f, 0.80f, 0.55f);
        }

        // Margen rojo
        var margen = NuevoUI(hoja.transform, "MargenRojo", new Vector2(-280f, 0f), new Vector2(2f, 700f));
        margen.AddComponent<Image>().color = new Color(0.80f, 0.30f, 0.30f, 0.6f);

        // Anillos espirales arriba
        for (int i = 0; i < 9; i++)
        {
            float x = -270f + i * 67.5f;
            var anillo = NuevoUI(hoja.transform, $"Anillo_{i}", new Vector2(x, 396f), new Vector2(34f, 34f));
            var imgAnillo = anillo.AddComponent<Image>();
            imgAnillo.sprite = SpriteAnillo(64, 10);
            imgAnillo.color = new Color(0.35f, 0.38f, 0.44f);
        }

        // Título del cuaderno
        CrearTexto(hoja.transform, "TituloNotas", "NOTAS DE DECODIFICACIÓN", 26, TextAnchor.MiddleCenter,
                   new Vector2(0f, 340f), new Vector2(600f, 40f), colorTinta).fontStyle = FontStyle.Bold;

        // Área de texto (solo acá se escribe)
        var campoGO = NuevoUI(hoja.transform, "CampoNotas", new Vector2(30f, -40f), new Vector2(580f, 640f));
        var campoImg = campoGO.AddComponent<Image>();
        campoImg.color = new Color(0f, 0f, 0f, 0f);

        campoNotas = campoGO.AddComponent<InputField>();
        campoNotas.lineType = InputField.LineType.MultiLineNewline;

        var texto = CrearTexto(campoGO.transform, "Text", "", 26, TextAnchor.UpperLeft,
                               new Vector2(0f, 0f), new Vector2(580f, 640f), colorTinta);
        texto.lineSpacing = 1.55f;
        campoNotas.textComponent = texto;
        campoNotas.targetGraphic = campoImg;
        campoNotas.selectionColor = new Color(0.4f, 0.6f, 0.9f, 0.4f);
        campoNotas.caretColor = colorTinta;

        // Placeholder
        var ph = CrearTexto(campoGO.transform, "Placeholder", "Escribí aquí la decodificación...", 24,
                            TextAnchor.UpperLeft, Vector2.zero, new Vector2(580f, 640f), new Color(0.45f, 0.42f, 0.36f));
        campoNotas.placeholder = ph;
        campoNotas.text = "";
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

        // Letras y números: se REPOSICIONAN (siempre verticales)
        if (textosInteriores != null && textosNumeros != null)
        {
            for (int i = 0; i < 26; i++)
            {
                float ang = (AnguloInicial - (i + paso) * PasoGrados) * Mathf.Deg2Rad;

                textosInteriores[i].rectTransform.anchoredPosition =
                    new Vector2(Mathf.Cos(ang) * radioLetrasInt, Mathf.Sin(ang) * radioLetrasInt);
                textosInteriores[i].rectTransform.localRotation = Quaternion.identity;

                textosNumeros[i].rectTransform.anchoredPosition =
                    new Vector2(Mathf.Cos(ang) * radioNumeros, Mathf.Sin(ang) * radioNumeros);
                textosNumeros[i].rectTransform.localRotation = Quaternion.identity;
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
            textoDesplazamiento.text = $"Desplazamiento: {paso}";

        if (textoEjemplo != null)
        {
            int cifrada = 2;   // letra "C" del código
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

    private static Sprite SpriteDisco(float diametro, float anchoBorde, float intensidadManchas, int semilla)
    {
        int key = (int)(diametro * 1000);
        if (SpritesDisco.TryGetValue(key, out var s)) return s;

        int size = Mathf.Clamp(Mathf.RoundToInt(diametro), 64, 1024);
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float centro = (size - 1) / 2f;
        float radio = size / 2f - 1f;
        float radioInterno = radio * (1f - anchoBorde);
        var rng = new System.Random(semilla);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - centro;
                float dy = y - centro;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                Color c;
                if (d > radio) c = new Color(0f, 0f, 0f, 0f);
                else if (d > radioInterno) c = Color.white;
                else c = new Color(0.93f, 0.93f, 0.93f, 1f);

                if (d <= radio)
                {
                    float m = (float)rng.NextDouble();
                    if (m < 0.02f) c *= new Color(0.80f, 0.76f, 0.66f, 1f);
                    else if (m < 0.04f) c *= new Color(0.86f, 0.82f, 0.72f, 1f);
                }
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        SpritesDisco[key] = sprite;
        return sprite;
    }

    private static Sprite _spriteAnillo;
    private static Sprite SpriteAnillo(int size, int grosor)
    {
        if (_spriteAnillo != null) return _spriteAnillo;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float centro = (size - 1) / 2f;
        float radioExt = size / 2f - 1f;
        float radioInt = radioExt - grosor;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - centro;
                float dy = y - centro;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                bool anillo = dist <= radioExt && dist >= radioInt;
                tex.SetPixel(x, y, anillo ? Color.white : new Color(0f, 0f, 0f, 0f));
            }
        }
        tex.Apply();
        _spriteAnillo = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _spriteAnillo;
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
        img.color = colorBorde;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        var c = btn.colors;
        c.normalColor = colorBorde;
        c.highlightedColor = new Color(0.32f, 0.62f, 0.72f);
        c.pressedColor = new Color(0.20f, 0.50f, 0.62f);
        c.fadeDuration = 0.06f;
        btn.colors = c;
        CrearTexto(btn.transform, "Texto", etiqueta, 20, TextAnchor.MiddleCenter, Vector2.zero, size, Color.white);
        return btn;
    }

    // ── Input (SOLO nuevo Input System) ───────────────────────────

    private Vector2 PosMouse()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
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
