using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Estilo "terminal de hackeo" para el menú principal, versión limpia para que se vea
/// el fondo animado. Se agrega al objeto MenuController (el que tiene MainMenuManager)
/// y, al empezar, reorganiza el PanelMenu que ya existe:
///
/// - A la DERECHA, sin tarjeta: el título SEEN (con efecto glitch), el subtítulo y los
///   botones JUGAR / AJUSTES / SALIR semitransparentes, con número y borde iluminado.
/// - Un degradado muy suave en el borde derecho para que el texto se lea sobre el video.
///
/// No borra nada de la escena: reutiliza los botones con sus eventos (JUGAR sigue llamando
/// a MainMenuManager.Jugar, etc.). Si quitas este componente, el menú vuelve a verse como antes.
/// Ajustes y Niveles los arregla PanelesMenuHacker (se agrega solo si falta).
/// </summary>
public class MenuPrincipalHacker : MonoBehaviour
{
    // ── Medidas (Canvas de 1920 x 1080) ─────────────────────────────
    private const float XColumna = 560f;
    private const float AnchoColumna = 600f;
    private const float AnchoBoton = 480f;
    private const float AltoBoton = 82f;
    private const float YTitulo = 250f;

    [Tooltip("Menú al que se le cambia el estilo. Si está vacío, se usa el MainMenuManager de este objeto.")]
    [SerializeField] private MainMenuManager menu;

    [Header("Textos (vacío = deja el texto que ya tiene la escena)")]
    [SerializeField] private string subtitulo = "INTRUSIÓN EN EL SISTEMA DE VIGILANCIA";
    [SerializeField] private string textoVersion = "";

    [Header("Descripciones de los botones")]
    [SerializeField] private string descripcionJugar = "INICIAR INTRUSIÓN";
    [SerializeField] private string descripcionAjustes = "CONFIGURAR EL SISTEMA";
    [SerializeField] private string descripcionSalir = "CERRAR LA CONEXIÓN";

    [Header("Fondo")]
    [Tooltip("Qué tan oscuro es el degradado del borde derecho (0 = nada, 1 = negro).")]
    [Range(0f, 1f)]
    [SerializeField] private float oscuridadDegradado = 0.45f;

    // ── Colores ─────────────────────────────────────────────────────
    private readonly Color colorBoton = new Color(0.02f, 0.06f, 0.09f, 0.45f);
    private readonly Color colorBotonSeleccion = new Color(0.05f, 0.18f, 0.25f, 0.75f);
    private readonly Color colorBordeBoton = new Color(0.15f, 0.85f, 1f, 0.25f);
    private readonly Color colorMagenta = new Color(1f, 0.18f, 0.62f);
    private readonly Color colorTextoSuave = new Color(0.70f, 0.78f, 0.84f);

    // ── Estado ──────────────────────────────────────────────────────
    private CajasRedondeadasUI cajas;
    private Texture2D texturaDegradado;
    private GameObject panel;
    private Transform columna;

    private Text titulo;
    private Text tituloCian;
    private Text tituloMagenta;
    private float proximoGlitch;
    private float finGlitch;

    private Button[] botones;
    private Text[] descripciones;
    private Text[] flechas;

    private void Awake()
    {
        if (menu == null) menu = GetComponent<MainMenuManager>();
        if (menu == null || menu.panelMenu == null)
        {
            Debug.LogWarning("[MenuPrincipalHacker] No encontré el MainMenuManager o su PanelMenu.", this);
            enabled = false;
            return;
        }

        panel = menu.panelMenu;
        cajas = new CajasRedondeadasUI();

        ConstruirDegradado();
        ConstruirColumna();

        // Ajustes y Niveles con el mismo estilo (si no lo agregaste a mano, se agrega solo)
        if (GetComponent<PanelesMenuHacker>() == null) gameObject.AddComponent<PanelesMenuHacker>();
    }

    private void OnDestroy()
    {
        if (cajas != null) cajas.Liberar();
        if (texturaDegradado != null) Destroy(texturaDegradado);
    }

    private void Update()
    {
        if (panel == null || !panel.activeInHierarchy) return;

        AnimarGlitch();
        AnimarBotones();

        // Para el mando: si nada quedó seleccionado, se selecciona JUGAR
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null
            && botones != null && botones[0] != null)
        {
            EventSystem.current.SetSelectedGameObject(botones[0].gameObject);
        }
    }

    // ── Construcción ────────────────────────────────────────────────

    /// <summary>Degradado de transparente (centro) a oscuro (borde derecho), solo detrás del menú.</summary>
    private void ConstruirDegradado()
    {
        texturaDegradado = new Texture2D(64, 1, TextureFormat.RGBA32, false);
        texturaDegradado.wrapMode = TextureWrapMode.Clamp;
        texturaDegradado.filterMode = FilterMode.Bilinear;
        for (int x = 0; x < 64; x++)
        {
            float t = x / 63f;
            texturaDegradado.SetPixel(x, 0, new Color(0f, 0f, 0f, t * t));
        }
        texturaDegradado.Apply();

        GameObject go = HackUI.NuevoUI(panel.transform, "DegradadoDerecho", new Vector2(480f, 0f), new Vector2(960f, 1080f));
        RawImage imagen = go.AddComponent<RawImage>();
        imagen.texture = texturaDegradado;
        imagen.color = new Color(1f, 1f, 1f, oscuridadDegradado);
        imagen.raycastTarget = false;
        go.transform.SetAsFirstSibling();
    }

    /// <summary>Columna invisible a la derecha donde se acomodan título, subtítulo y botones.</summary>
    private void ConstruirColumna()
    {
        GameObject go = HackUI.NuevoUI(panel.transform, "ColumnaMenu", new Vector2(XColumna, 0f), new Vector2(AnchoColumna, 1000f));
        columna = go.transform;

        // Título: se reutiliza el de la escena, con dos copias de color para el glitch
        Transform tituloEscena = panel.transform.Find("Titulo");
        tituloMagenta = HackUI.CrearTexto(columna, "TituloMagenta", "SEEN", 150, TextAnchor.MiddleCenter,
                                          new Vector2(0f, YTitulo), new Vector2(AnchoColumna, 180f), colorMagenta);
        tituloCian = HackUI.CrearTexto(columna, "TituloCian", "SEEN", 150, TextAnchor.MiddleCenter,
                                       new Vector2(0f, YTitulo), new Vector2(AnchoColumna, 180f), HackUI.ColAcento);
        if (tituloEscena != null)
        {
            titulo = tituloEscena.GetComponent<Text>();
            ColocarEn(tituloEscena, columna, new Vector2(0f, YTitulo), new Vector2(AnchoColumna, 180f));
        }
        if (titulo == null)
        {
            titulo = HackUI.CrearTexto(columna, "Titulo", "SEEN", 150, TextAnchor.MiddleCenter,
                                       new Vector2(0f, YTitulo), new Vector2(AnchoColumna, 180f), Color.white);
        }
        titulo.fontSize = 150;
        titulo.fontStyle = FontStyle.Bold;
        titulo.alignment = TextAnchor.MiddleCenter;
        titulo.color = Color.white;
        AgregarSombra(titulo.gameObject);
        tituloMagenta.fontStyle = FontStyle.Bold;
        tituloCian.fontStyle = FontStyle.Bold;
        tituloMagenta.text = titulo.text;
        tituloCian.text = titulo.text;
        tituloMagenta.enabled = false;
        tituloCian.enabled = false;
        proximoGlitch = Time.unscaledTime + 2f;

        // Subtítulo
        Transform subtituloEscena = panel.transform.Find("Subtitulo");
        if (subtituloEscena != null)
        {
            ColocarEn(subtituloEscena, columna, new Vector2(0f, YTitulo - 100f), new Vector2(AnchoColumna, 30f));
            Text texto = subtituloEscena.GetComponent<Text>();
            if (texto != null)
            {
                if (!string.IsNullOrEmpty(subtitulo)) texto.text = subtitulo;
                texto.fontSize = 18;
                texto.alignment = TextAnchor.MiddleCenter;
                texto.color = HackUI.ColAcento;
                AgregarSombra(texto.gameObject);
            }
        }

        Transform lineaVieja = panel.transform.Find("Linea");
        if (lineaVieja != null) lineaVieja.gameObject.SetActive(false);
        GameObject separador = HackUI.NuevoUI(columna, "Separador", new Vector2(0f, YTitulo - 128f), new Vector2(120f, 3f));
        Image imagenSeparador = separador.AddComponent<Image>();
        imagenSeparador.color = colorMagenta;
        imagenSeparador.raycastTarget = false;

        // Botones (los mismos de la escena, con sus eventos)
        botones = new Button[3];
        descripciones = new Text[3];
        flechas = new Text[3];
        PrepararBoton(0, "BotonJugar", "01", descripcionJugar, 20f, HackUI.ColAcento);
        PrepararBoton(1, "BotonAjustes", "02", descripcionAjustes, -80f, HackUI.ColAcento);
        PrepararBoton(2, "BotonSalir", "03", descripcionSalir, -180f, colorMagenta);
        ConectarNavegacionMenu();

        // Ayuda de controles y versión, pequeñas
        Text controles = HackUI.CrearTexto(columna, "Controles", "↑ ↓  moverse      A / ENTER  aceptar      B / ESC  volver", 13,
                                           TextAnchor.MiddleCenter, new Vector2(0f, -265f), new Vector2(AnchoColumna, 20f),
                                           colorTextoSuave);
        AgregarSombra(controles.gameObject);

        Transform version = panel.transform.Find("Version");
        if (version != null)
        {
            ColocarEn(version, columna, new Vector2(0f, -440f), new Vector2(AnchoColumna, 24f));
            Text texto = version.GetComponent<Text>();
            if (texto != null)
            {
                if (!string.IsNullOrEmpty(textoVersion)) texto.text = textoVersion;
                texto.fontSize = 13;
                texto.alignment = TextAnchor.MiddleCenter;
                texto.color = new Color(colorTextoSuave.r, colorTextoSuave.g, colorTextoSuave.b, 0.6f);
            }
        }
    }

    /// <summary>Le da al botón de la escena el estilo nuevo, sin tocar sus eventos.</summary>
    private void PrepararBoton(int indice, string nombre, string numero, string descripcion, float y, Color acento)
    {
        Transform t = panel.transform.Find(nombre);
        if (t == null) return;
        Button boton = t.GetComponent<Button>();
        if (boton == null) return;
        botones[indice] = boton;

        ColocarEn(t, columna, new Vector2(0f, y), new Vector2(AnchoBoton, AltoBoton));

        // Fondo redondeado semitransparente + borde (el borde se ilumina con TeclaCodigoUI)
        Image fondo = boton.GetComponent<Image>();
        if (fondo == null) fondo = t.gameObject.AddComponent<Image>();
        Image borde = cajas.Redondear(fondo, colorBordeBoton, 1.4f);
        fondo.color = Color.white;   // el color lo pone el tinte del Button
        boton.targetGraphic = fondo;

        boton.transition = Selectable.Transition.ColorTint;
        ColorBlock colores = boton.colors;
        colores.normalColor = colorBoton;
        colores.highlightedColor = colorBotonSeleccion;
        colores.selectedColor = colorBotonSeleccion;
        colores.pressedColor = Color.Lerp(colorBotonSeleccion, Color.white, 0.25f);
        colores.colorMultiplier = 1f;
        colores.fadeDuration = 0.06f;
        boton.colors = colores;

        TeclaCodigoUI iluminacion = t.gameObject.AddComponent<TeclaCodigoUI>();
        iluminacion.Configurar(nombre, borde, colorBordeBoton, acento, null);

        // Número a la izquierda
        Text numeroTexto = HackUI.CrearTexto(t, "Numero", numero, 20, TextAnchor.MiddleCenter,
                                             new Vector2(-AnchoBoton / 2f + 38f, 0f), new Vector2(44f, 40f), acento);
        numeroTexto.fontStyle = FontStyle.Bold;

        // Texto principal: empieza después del número, sin encimarse
        Transform textoBoton = t.Find("Texto");
        if (textoBoton != null)
        {
            Text texto = textoBoton.GetComponent<Text>();
            RectTransform rt = (RectTransform)textoBoton;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(320f, 40f);
            rt.anchoredPosition = new Vector2(-15f, 11f);
            if (texto != null)
            {
                texto.fontSize = 28;
                texto.fontStyle = FontStyle.Bold;
                texto.alignment = TextAnchor.MiddleLeft;
                texto.color = Color.white;
            }
            textoBoton.SetAsLastSibling();
        }

        descripciones[indice] = HackUI.CrearTexto(t, "Descripcion", descripcion, 13, TextAnchor.MiddleLeft,
                                                  new Vector2(-15f, -19f), new Vector2(320f, 20f), colorTextoSuave);
        flechas[indice] = HackUI.CrearTexto(t, "Flecha", ">>", 24, TextAnchor.MiddleRight,
                                            new Vector2(AnchoBoton / 2f - 36f, 0f), new Vector2(50f, 40f), acento);
        flechas[indice].fontStyle = FontStyle.Bold;
    }

    /// <summary>Sombra suave para que el texto se lea sobre el video sin poner cajas.</summary>
    private void AgregarSombra(GameObject go)
    {
        Shadow sombra = go.GetComponent<Shadow>();
        if (sombra == null) sombra = go.AddComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.8f);
        sombra.effectDistance = new Vector2(2f, -2f);
    }

    // ── Mando ───────────────────────────────────────────────────────

    /// <summary>JUGAR ↔ AJUSTES ↔ SALIR con la cruceta (y de SALIR vuelve a JUGAR).</summary>
    private void ConectarNavegacionMenu()
    {
        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i] == null) continue;
            int arriba = (i + botones.Length - 1) % botones.Length;
            int abajo = (i + 1) % botones.Length;

            Navigation navegacion = new Navigation();
            navegacion.mode = Navigation.Mode.Explicit;
            navegacion.selectOnUp = botones[arriba];
            navegacion.selectOnDown = botones[abajo];
            botones[i].navigation = navegacion;
        }
    }

    // ── Animaciones ─────────────────────────────────────────────────

    /// <summary>Cada pocos segundos el título se "rompe" un instante (copias de color corridas).</summary>
    private void AnimarGlitch()
    {
        if (titulo == null) return;
        float ahora = Time.unscaledTime;

        if (ahora >= proximoGlitch)
        {
            finGlitch = ahora + Random.Range(0.08f, 0.2f);
            proximoGlitch = ahora + Random.Range(2.5f, 5f);
        }

        bool enGlitch = ahora < finGlitch;
        tituloCian.enabled = enGlitch;
        tituloMagenta.enabled = enGlitch;
        if (enGlitch)
        {
            float salto = Random.Range(4f, 10f);
            ((RectTransform)tituloCian.transform).anchoredPosition = new Vector2(-salto, YTitulo + Random.Range(-2f, 2f));
            ((RectTransform)tituloMagenta.transform).anchoredPosition = new Vector2(salto, YTitulo + Random.Range(-2f, 2f));
        }
    }

    /// <summary>El botón seleccionado muestra su flecha parpadeando y su descripción en celeste.</summary>
    private void AnimarBotones()
    {
        if (botones == null) return;
        GameObject actual = null;
        if (EventSystem.current != null) actual = EventSystem.current.currentSelectedGameObject;
        bool parpadeo = Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.5f;

        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i] == null) continue;
            bool seleccionado = botones[i].gameObject == actual;
            if (flechas[i] != null) flechas[i].enabled = seleccionado && parpadeo;
            if (descripciones[i] != null)
            {
                if (seleccionado) descripciones[i].color = HackUI.ColAcento;
                else descripciones[i].color = colorTextoSuave;
            }
        }
    }

    // ── Ayudantes ───────────────────────────────────────────────────

    /// <summary>Mueve un elemento de la escena a la columna y lo coloca.</summary>
    private void ColocarEn(Transform elemento, Transform padre, Vector2 posicion, Vector2 tamano)
    {
        elemento.SetParent(padre, false);
        RectTransform rt = (RectTransform)elemento;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = tamano;
        rt.anchoredPosition = posicion;
    }
}
