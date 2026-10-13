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
/// Dos discos concéntricos de estilo vintage:
///   • Disco EXTERIOR (fijo): letras A-Z
///   • Disco INTERIOR (gira): letras a-z y sus números 1-26
/// Se gira arrastrando con el mouse o con las flechas ← → / A D.
/// Con el desplazamiento k alineado, cada letra del código (exterior) tiene su
/// original justo debajo, en el interior.
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
    [SerializeField] private int tamanioLetraExt = 34;
    [SerializeField] private int tamanioLetraInt = 26;
    [SerializeField] private int tamanioNumero = 22;

    [Header("Paleta vintage")]
    [SerializeField] private Color colorDisco = new Color(0.906f, 0.867f, 0.769f);     // pergamino
    [SerializeField] private Color colorBorde = new Color(0.556f, 0.478f, 0.361f);     // borde oscuro
    [SerializeField] private Color colorLetraExt = new Color(0.29f, 0.22f, 0.14f);      // marrón tinta
    [SerializeField] private Color colorLetraInt = new Color(0.29f, 0.22f, 0.14f);
    [SerializeField] private Color colorNumero = new Color(0.45f, 0.35f, 0.20f);
    [SerializeField] private Color colorFondo = new Color(0.05f, 0.05f, 0.07f, 0.92f);

    private Canvas canvas;
    private GameObject panel;
    private RectTransform discoInterior;
    private RectTransform discoInteriorLetras;
    private Text[] textosInteriores;
    private Text[] textosNumeros;
    private Text[] textosExteriores;
    private Text textoDesplazamiento;
    private Text textoEjemplo;

    private int paso;
    private bool arrastrando;
    private float anguloPrevio;
    private float anguloAcumulado;

    private const float PasoGrados = 360f / 26f;

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

        // Mouse
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
            int pasos = Mathf.RoundToInt(-anguloAcumulado / PasoGrados);
            paso = ((pasos % 26) + 26) % 26;
            AplicarRotacion();
        }
        if (arrastrando)
        {
            float a = AnguloMouse();
            float delta = Mathf.DeltaAngle(anguloPrevio, a);
            anguloPrevio = a;
            anguloAcumulado += delta;
            int pasos = Mathf.RoundToInt(-anguloAcumulado / PasoGrados);
            int nuevo = ((pasos % 26) + 26) % 26;
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
        canvas.sortingOrder = 50;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGO.AddComponent<GraphicRaycaster>();

        var fondoGO = NuevoUI(canvasGO.transform, "Fondo", Vector2.zero, new Vector2(1920f, 1080f));
        Estirar(fondoGO);
        fondoGO.AddComponent<Image>().color = colorFondo;

        panel = NuevoUI(canvasGO.transform, "Panel", Vector2.zero, new Vector2(860f, 900f));
        panel.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.09f, 1f);

        CrearTexto(panel.transform, "Titulo", "TABLA DE DESCIFRADO CÉSAR", 30, TextAnchor.UpperCenter,
                   new Vector2(0f, 425f), new Vector2(800f, 40f), colorBorde).fontStyle = FontStyle.Bold;
        CrearTexto(panel.transform, "Ayuda", "Girá el disco interior con el mouse o con ← →\n" +
                                             "Alineá el desplazamiento y leé debajo de cada letra", 20,
                   TextAnchor.UpperCenter, new Vector2(0f, 385f), new Vector2(800f, 56f),
                   new Color(0.62f, 0.52f, 0.36f));

        // ── Discos ──
        var centro = NuevoUI(panel.transform, "CentroRueda", new Vector2(0f, 60f), Vector2.zero).transform;

        // Disco exterior (fijo)
        var discoExt = NuevoUI(centro, "DiscoExterior", Vector2.zero,
                               new Vector2(diametroExterior, diametroExterior));
        var imgExt = discoExt.AddComponent<Image>();
        imgExt.sprite = SpriteDisco(diametroExterior, 0.055f, 0.10f, 4242);
        imgExt.color = colorDisco;
        imgExt.raycastTarget = false;

        // Letras exteriores (SIEMPRE verticales)
        textosExteriores = new Text[26];
        for (int i = 0; i < 26; i++)
        {
            char letra = (char)('A' + i);
            float ang = i * PasoGrados * Mathf.Deg2Rad;
            var t = CrearTexto(centro, $"Ext_{i}", letra.ToString(), tamanioLetraExt, TextAnchor.MiddleCenter,
                               new Vector2(Mathf.Cos(ang) * radioLetrasExt, Mathf.Sin(ang) * radioLetrasExt),
                               new Vector2(54f, 44f), colorLetraExt);
            t.fontStyle = FontStyle.Bold;
            textosExteriores[i] = t;
        }

        // Líneas divisorias exteriores (marcan cada espacio de letra)
        for (int i = 0; i < 26; i++)
        {
            float ang = (i - 0.5f) * PasoGrados * Mathf.Deg2Rad;
            DivisionRadial(centro, $"DivExt_{i}", ang, radioLetrasExt - 34f, 46f, colorBorde * 0.9f);
        }

        // ── Disco interior ──
        // El GRUPO gira (disco visual + divisorias), pero las letras quedan
        // en un grupo aparte y se REPOSICIONAN para mantenerse siempre verticales.
        discoInterior = (RectTransform)NuevoUI(centro, "GrupoInterior", Vector2.zero, Vector2.zero).transform;
        var discoInt = NuevoUI(discoInterior, "DiscoInterior", Vector2.zero,
                               new Vector2(diametroInterior, diametroInterior));
        var imgInt = discoInt.AddComponent<Image>();
        imgInt.sprite = SpriteDisco(diametroInterior, 0.06f, 0.12f, 777);
        imgInt.color = colorDisco;
        imgInt.raycastTarget = false;

        // Divisorias del disco interior (giran con el disco)
        for (int i = 0; i < 26; i++)
        {
            float ang = (i - 0.5f) * PasoGrados * Mathf.Deg2Rad;
            DivisionRadial(discoInterior, $"DivInt_{i}", ang, radioNumeros - 26f, 40f, colorBorde * 0.8f);
        }

        // Letras y números interiores (se mueven, no giran)
        discoInteriorLetras = (RectTransform)NuevoUI(centro, "LetrasInterior", Vector2.zero, Vector2.zero).transform;
        textosInteriores = new Text[26];
        textosNumeros = new Text[26];

        for (int i = 0; i < 26; i++)
        {
            char letra = (char)('a' + i);
            textosInteriores[i] = CrearTexto(discoInteriorLetras, $"Int_{i}", letra.ToString(),
                tamanioLetraInt, TextAnchor.MiddleCenter, Vector2.zero,
                new Vector2(44f, 34f), colorLetraInt);

            textosNumeros[i] = CrearTexto(discoInteriorLetras, $"Num_{i}", (i + 1).ToString(),
                tamanioNumero, TextAnchor.MiddleCenter, Vector2.zero,
                new Vector2(40f, 28f), colorNumero);
        }

        // Eje central
        var eje = NuevoUI(centro, "Eje", Vector2.zero, new Vector2(26f, 26f));
        eje.AddComponent<Image>().color = colorBorde;

        // Marca superior (alineación de lectura)
        var marca = NuevoUI(centro, "Marca", new Vector2(0f, radioLetrasExt + 22f), new Vector2(30f, 8f));
        marca.AddComponent<Image>().color = colorBorde;

        // Lecturas
        textoDesplazamiento = CrearTexto(panel.transform, "Desplazamiento", "Desplazamiento: 0", 26,
                                         TextAnchor.MiddleCenter, new Vector2(0f, -330f), new Vector2(520f, 38f),
                                         new Color(0.85f, 0.75f, 0.50f));
        textoDesplazamiento.fontStyle = FontStyle.Bold;

        textoEjemplo = CrearTexto(panel.transform, "Ejemplo", "", 22, TextAnchor.MiddleCenter,
                                  new Vector2(0f, -378f), new Vector2(660f, 34f), new Color(0.45f, 0.85f, 0.55f));

        var btnCerrar = CrearBoton(panel.transform, "BotonCerrar", "CERRAR (TAB)", new Vector2(0f, -425f),
                                   new Vector2(280f, 54f));
        btnCerrar.onClick.AddListener(Cerrar);

        AplicarRotacion();
    }

    // ── Lógica ────────────────────────────────────────────────────

    private void Girar(int dir)
    {
        paso = ((paso + dir) % 26 + 26) % 26;
        AplicarRotacion();
    }

    private void AplicarRotacion()
    {
        // El DISCO gira (visual + divisorias)
        if (discoInterior != null)
            discoInterior.localRotation = Quaternion.Euler(0f, 0f, paso * PasoGrados);

        // Las letras y números INTERIORES se REPOSICIONAN (nunca se inclinan)
        if (textosInteriores != null && textosNumeros != null)
        {
            for (int i = 0; i < 26; i++)
            {
                float ang = (i + paso) * PasoGrados * Mathf.Deg2Rad;

                if (textosInteriores[i] != null)
                {
                    textosInteriores[i].rectTransform.anchoredPosition =
                        new Vector2(Mathf.Cos(ang) * radioLetrasInt, Mathf.Sin(ang) * radioLetrasInt);
                    textosInteriores[i].rectTransform.localRotation = Quaternion.identity;
                }
                if (textosNumeros[i] != null)
                {
                    textosNumeros[i].rectTransform.anchoredPosition =
                        new Vector2(Mathf.Cos(ang) * radioNumeros, Mathf.Sin(ang) * radioNumeros);
                    textosNumeros[i].rectTransform.localRotation = Quaternion.identity;
                }
            }
        }

        if (textoDesplazamiento != null)
            textoDesplazamiento.text = $"Desplazamiento: {paso}";

        if (textoEjemplo != null)
        {
            int cifrada = 2;                                            // la letra "C" del código
            int original = ((cifrada - paso) % 26 + 26) % 26;
            textoEjemplo.text = $"Ejemplo: la letra «{(char)('A' + cifrada)}» del código es la «{(char)('A' + original)}»";
        }
    }

    /// <summary>Línea radial que delimita el espacio de una letra.</summary>
    private void DivisionRadial(Transform parent, string nombre, float angRad, float radioDesde, float largo, Color color)
    {
        var dx = Mathf.Cos(angRad);
        var dy = Mathf.Sin(angRad);
        Vector2 inicio = new Vector2(dx * radioDesde, dy * radioDesde);
        Vector2 fin = new Vector2(dx * (radioDesde + largo), dy * (radioDesde + largo));

        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        float dist = (fin - inicio).magnitude;
        rt.sizeDelta = new Vector2(dist, 3f);
        rt.anchoredPosition = (inicio + fin) / 2f;
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(fin.y - inicio.y, fin.x - inicio.x) * Mathf.Rad2Deg);

        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    // ── Mouse sobre la rueda ──────────────────────────────────────

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
        return Vector2.Distance(MouseLocal(), CentroRuedaLocal()) <= diametroExterior / 2f;
    }

    private float AnguloMouse()
    {
        Vector2 d = MouseLocal() - CentroRuedaLocal();
        return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
    }

    // ── Texturas de disco (pergamino con borde) ───────────────────

    private static readonly Dictionary<int, Sprite> SpritesDisco = new Dictionary<int, Sprite>();

    private static Sprite SpriteDisco(float diametro, float anchoBorde, float NoiseSemilla, int semilla)
    {
        int key = (int)(diametro * 1000) + anchoBorde.GetHashCode();
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
                else if (d > radioInterno) c = Color.white;                 // borde (se tinta)
                else c = new Color(0.93f, 0.93f, 0.93f, 1f);                // cara del disco

                // Manchas vintage suaves
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
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
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
