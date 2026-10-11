#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Construye el menú principal de SEEN como una escena nueva (Assets/Scenes/MainMenu.unity)
/// con Canvas en Screen Space Overlay: título, botones JUGAR / AJUSTES / SALIR y un
/// panel de Ajustes con calidad gráfica, resolución, pantalla completa, volumen y
/// sensibilidad del ratón. Además registra las escenas en Build Settings.
///
/// Menú: <b>SEEN → Construir Menú Principal</b>
/// </summary>
public static class UIMenuBuilder
{
    // Paleta distópica
    static readonly Color colFondo      = new Color(0.030f, 0.035f, 0.050f);
    static readonly Color colAcento     = new Color(0.150f, 0.750f, 0.950f); // cyan
    static readonly Color colMagenta    = new Color(1.000f, 0.180f, 0.620f);
    static readonly Color colBoton      = new Color(0.090f, 0.100f, 0.130f);
    static readonly Color colBotonHover = new Color(0.150f, 0.180f, 0.230f);
    static readonly Color colTexto      = new Color(0.900f, 0.940f, 1.000f);
    static readonly Color colTextoSec   = new Color(0.550f, 0.620f, 0.700f);

    static Font _font;
    static Font Font
    {
        get
        {
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _font;
        }
    }

    [MenuItem("SEEN/Construir Menú Principal")]
    static void Build()
    {
        if (!EditorUtility.DisplayDialog("Construir Menú Principal — SEEN",
                "Se creará una escena nueva <b>MainMenu</b> con:\n\n" +
                "• Título del juego\n• Botones JUGAR / AJUSTES / SALIR\n" +
                "• Panel de Ajustes (gráficos, resolución, volumen, sensibilidad)\n\n" +
                "ADVERTENCIA: se reemplazará la escena abierta actual.\n¿Continuar?",
                "Sí, construir", "Cancelar")) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // EventSystem (módulo del nuevo Input System, obligatorio para la UI)
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>()?.gameObject.SetActive(true);
        var inputModule = es.AddComponent<InputSystemUIInputModule>();

        // ── Canvas ──
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Controlador ──
        var controlador = new GameObject("MenuController");
        var menu = controlador.AddComponent<MainMenuManager>();
        var ajustes = controlador.AddComponent<SettingsManager>();
        menu.ajustes = ajustes;

        // ── Panel Menú Principal ──
        var panelMenu = CrearPanel(canvasGO.transform, "PanelMenu", colFondo);
        menu.panelMenu = panelMenu;

        var titulo = CrearTexto(panelMenu.transform, "Titulo", "SEEN", 130, TextAnchor.MiddleCenter,
                                new Vector2(0f, 240f), new Vector2(1200f, 180f), colTexto);
        titulo.fontStyle = FontStyle.Bold;

        CrearTexto(panelMenu.transform, "Subtitulo", "P U N T O   D E   I N F E C C I Ó N", 26,
                   TextAnchor.MiddleCenter, new Vector2(0f, 150f), new Vector2(1000f, 40f), colAcento);

        CrearLinea(panelMenu.transform, new Vector2(0f, 110f), new Vector2(560f, 3f), colMagenta);

        var btnJugar = CrearBoton(panelMenu.transform, "BotonJugar", "JUGAR",
                                  new Vector2(0f, 20f), new Vector2(340f, 68f));
        var btnAjustes = CrearBoton(panelMenu.transform, "BotonAjustes", "AJUSTES",
                                    new Vector2(0f, -70f), new Vector2(340f, 68f));
        var btnSalir = CrearBoton(panelMenu.transform, "BotonSalir", "SALIR",
                                  new Vector2(0f, -160f), new Vector2(340f, 68f));

        CrearTexto(panelMenu.transform, "Version", "v0.1 — Prototipo jugable", 18,
                   TextAnchor.MiddleCenter, new Vector2(0f, -320f), new Vector2(600f, 30f), colTextoSec);

        // Listeners persistentes (sobreviven al guardar la escena)
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnJugar.onClick, menu.Jugar);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnAjustes.onClick, menu.MostrarAjustes);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnSalir.onClick, menu.SalirDelJuego);

        // ── Panel Ajustes ──
        var panelAjustes = CrearPanel(canvasGO.transform, "PanelAjustes", new Color(0.045f, 0.050f, 0.070f, 0.985f));
        menu.panelAjustes = panelAjustes;

        CrearTexto(panelAjustes.transform, "TituloAjustes", "A J U S T E S", 48, TextAnchor.MiddleCenter,
                   new Vector2(0f, 280f), new Vector2(900f, 70f), colTexto);
        CrearLinea(panelAjustes.transform, new Vector2(0f, 240f), new Vector2(420f, 2f), colAcento);

        // Fila: Calidad gráfica
        CrearTexto(panelAjustes.transform, "LblCalidad", "CALIDAD GRÁFICA", 24, TextAnchor.MiddleLeft,
                   new Vector2(-210f, 185f), new Vector2(330f, 40f), colTexto);
        var ddCalidad = CrearDropdown(panelAjustes.transform, "DropdownCalidad",
                                      new Vector2(170f, 185f), new Vector2(340f, 54f));

        // Fila: Resolución
        CrearTexto(panelAjustes.transform, "LblResolucion", "RESOLUCIÓN", 24, TextAnchor.MiddleLeft,
                   new Vector2(-210f, 115f), new Vector2(330f, 40f), colTexto);
        var ddResolucion = CrearDropdown(panelAjustes.transform, "DropdownResolucion",
                                         new Vector2(170f, 115f), new Vector2(340f, 54f));

        // Fila: Pantalla completa
        CrearTexto(panelAjustes.transform, "LblPantalla", "PANTALLA COMPLETA", 24, TextAnchor.MiddleLeft,
                   new Vector2(-210f, 45f), new Vector2(330f, 40f), colTexto);
        var tgPantalla = CrearToggle(panelAjustes.transform, "TogglePantallaCompleta",
                                     new Vector2(280f, 45f), new Vector2(48f, 48f));

        // Fila: Volumen
        CrearTexto(panelAjustes.transform, "LblVolumen", "VOLUMEN MAESTRO", 24, TextAnchor.MiddleLeft,
                   new Vector2(-210f, -25f), new Vector2(330f, 40f), colTexto);
        var slVolumen = CrearSlider(panelAjustes.transform, "SliderVolumen",
                                    new Vector2(170f, -25f), new Vector2(340f, 34f), 0f, 1f, 1f);

        // Fila: Sensibilidad
        CrearTexto(panelAjustes.transform, "LblSensibilidad", "SENSIBILIDAD RATÓN", 24, TextAnchor.MiddleLeft,
                   new Vector2(-210f, -95f), new Vector2(330f, 40f), colTexto);
        var slSens = CrearSlider(panelAjustes.transform, "SliderSensibilidad",
                                 new Vector2(170f, -95f), new Vector2(340f, 34f), 0.02f, 0.5f, 0.15f);

        // Botón volver
        var btnVolver = CrearBoton(panelAjustes.transform, "BotonVolver", "VOLVER",
                                   new Vector2(0f, -230f), new Vector2(280f, 62f));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnVolver.onClick, menu.MostrarMenuPrincipal);

        CrearTexto(panelAjustes.transform, "NotaAjustes", "Los cambios se guardan automáticamente", 18,
                   TextAnchor.MiddleCenter, new Vector2(0f, -290f), new Vector2(600f, 30f), colTextoSec);

        // Conectar ajustes
        ajustes.calidadDropdown = ddCalidad;
        ajustes.resolucionDropdown = ddResolucion;
        ajustes.pantallaCompletaToggle = tgPantalla;
        ajustes.volumenSlider = slVolumen;
        ajustes.sensibilidadSlider = slSens;

        // Empezar mostrando el menú principal
        panelAjustes.SetActive(false);

        // ── Guardar escena y Build Settings ──
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");

        var buildScenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Juego.unity", true)
        };
        EditorBuildSettings.scenes = buildScenes;
        AssetDatabase.SaveAssets();

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>("Assets/Scenes/MainMenu.unity");
        Debug.Log("[UIMenuBuilder] ¡Menú construido! Escena 'MainMenu' guardada y añadida al Build Settings. " +
                  "Presioná Play y usá el ratón (el EventSystem usa el nuevo Input System).");
    }

    // ───────────────────────────────────────────────────────── Helpers

    static GameObject CrearPanel(Transform parent, string nombre, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return go;
    }

    static void CrearLinea(Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject("Linea", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    static Text CrearTexto(Transform parent, string nombre, string contenido, int fontSize,
                           TextAnchor alignment, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var txt = go.AddComponent<Text>();
        txt.text = contenido;
        txt.font = Font;
        txt.fontSize = fontSize;
        txt.alignment = alignment;
        txt.color = color;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;
        return txt;
    }

    static Button CrearBoton(Transform parent, string nombre, string etiqueta, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = colBoton;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        var colors = btn.colors;
        colors.normalColor = colBoton;
        colors.highlightedColor = colBotonHover;
        colors.pressedColor = new Color(0.10f, 0.55f, 0.75f);
        colors.selectedColor = colBotonHover;
        colors.disabledColor = new Color(0.15f, 0.15f, 0.15f);
        colors.fadeDuration = 0.08f;
        colors.colorMultiplier = 1f;
        btn.colors = colors;

        var txt = CrearTexto(btn.transform, "Texto", etiqueta, 30, TextAnchor.MiddleCenter,
                             Vector2.zero, size, colTexto);
        txt.fontStyle = FontStyle.Bold;
        return btn;
    }

    static Toggle CrearToggle(Transform parent, string nombre, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = colBoton;

        var tgl = go.AddComponent<Toggle>();
        tgl.targetGraphic = img;
        tgl.transition = Selectable.Transition.ColorTint;
        tgl.isOn = true;

        var mark = new GameObject("Checkmark", typeof(RectTransform));
        mark.transform.SetParent(go.transform, false);
        var mRt = (RectTransform)mark.transform;
        mRt.anchorMin = Vector2.zero;
        mRt.anchorMax = Vector2.one;
        mRt.offsetMin = new Vector2(10f, 10f);
        mRt.offsetMax = new Vector2(-10f, -10f);
        var mImg = mark.AddComponent<Image>();
        mImg.color = colAcento;
        tgl.graphic = mImg;

        return tgl;
    }

    static Slider CrearSlider(Transform parent, string nombre, Vector2 pos, Vector2 size,
                              float min, float max, float value)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.09f, 0.10f, 0.13f);

        var slider = go.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.targetGraphic = img;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;

        // Fill (barra de progreso)
        var fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(go.transform, false);
        var fRt = (RectTransform)fill.transform;
        fRt.anchorMin = new Vector2(0f, 0.5f);
        fRt.anchorMax = new Vector2(0f, 0.5f);
        fRt.pivot = new Vector2(0f, 0.5f);
        fRt.sizeDelta = new Vector2(size.x, size.y * 0.75f);
        fRt.anchoredPosition = Vector2.zero;
        var fImg = fill.AddComponent<Image>();
        fImg.color = colAcento;
        fImg.raycastTarget = false;
        slider.fillRect = fRt;

        // Handle (perilla)
        var handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(go.transform, false);
        var hRt = (RectTransform)handle.transform;
        hRt.anchorMin = new Vector2(0.5f, 0.5f);
        hRt.anchorMax = new Vector2(0.5f, 0.5f);
        hRt.pivot = new Vector2(0.5f, 0.5f);
        hRt.sizeDelta = new Vector2(26f, size.y + 10f);
        hRt.anchoredPosition = Vector2.zero;
        var hImg = handle.AddComponent<Image>();
        hImg.color = new Color(0.85f, 0.95f, 1f);
        hImg.raycastTarget = false;
        slider.handleRect = hRt;

        // Navegación para que el ratón lo tome
        var nav = slider.navigation;
        nav.mode = Navigation.Mode.Automatic;
        slider.navigation = nav;

        return slider;
    }

    static Dropdown CrearDropdown(Transform parent, string nombre, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.09f, 0.10f, 0.13f);

        var dd = go.AddComponent<Dropdown>();
        dd.targetGraphic = img;
        dd.transition = Selectable.Transition.ColorTint;

        // Valor actual
        var label = CrearTexto(dd.transform, "Label", "—", 24, TextAnchor.MiddleLeft,
                               new Vector2(14f, 0f), new Vector2(size.x - 40f, size.y), colTexto);
        dd.captionText = label;

        // Flecha
        CrearTexto(dd.transform, "Arrow", "▼", 16, TextAnchor.MiddleRight,
                   new Vector2(-12f, 0f), new Vector2(28f, size.y), colAcento);

        // Template
        var templateGO = new GameObject("Template", typeof(RectTransform));
        templateGO.transform.SetParent(go.transform, false);
        var tRt = (RectTransform)templateGO.transform;
        tRt.anchorMin = new Vector2(0f, 0f);
        tRt.anchorMax = new Vector2(1f, 0f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, 4f);
        tRt.sizeDelta = new Vector2(0f, 180f);
        var tImg = templateGO.AddComponent<Image>();
        tImg.color = new Color(0.07f, 0.08f, 0.11f);
        tImg.raycastTarget = false;
        var scroll = templateGO.AddComponent<ScrollRect>();

        // Viewport
        var viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(templateGO.transform, false);
        var vRt = (RectTransform)viewport.transform;
        vRt.anchorMin = Vector2.zero;
        vRt.anchorMax = Vector2.one;
        vRt.offsetMin = Vector2.zero;
        vRt.offsetMax = Vector2.zero;
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = vRt;

        // Content
        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var cRt = (RectTransform)content.transform;
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.sizeDelta = new Vector2(0f, 180f);
        scroll.content = cRt;

        // Item
        var itemGO = new GameObject("Item", typeof(RectTransform));
        itemGO.transform.SetParent(content.transform, false);
        var iRt = (RectTransform)itemGO.transform;
        iRt.anchorMin = new Vector2(0f, 0.5f);
        iRt.anchorMax = new Vector2(1f, 0.5f);
        iRt.pivot = new Vector2(0.5f, 0.5f);
        iRt.sizeDelta = new Vector2(0f, 44f);
        var iImg = itemGO.AddComponent<Image>();
        iImg.color = new Color(0.12f, 0.13f, 0.17f);
        var iToggle = itemGO.AddComponent<Toggle>();
        iToggle.targetGraphic = iImg;
        iToggle.transition = Selectable.Transition.None;

        var itemBg = new GameObject("Item Background", typeof(RectTransform));
        itemBg.transform.SetParent(itemGO.transform, false);
        var ibRt = (RectTransform)itemBg.transform;
        ibRt.anchorMin = Vector2.zero;
        ibRt.anchorMax = Vector2.one;
        ibRt.offsetMin = Vector2.zero;
        ibRt.offsetMax = Vector2.zero;
        var ibImg = itemBg.AddComponent<Image>();
        ibImg.color = colAcento;
        iToggle.graphic = ibImg;

        var itemLabel = CrearTexto(itemGO.transform, "Item Label", "—", 22, TextAnchor.MiddleLeft,
                                   new Vector2(12f, 0f), new Vector2(size.x - 30f, 44f), colTexto);
        dd.itemText = itemLabel;

        dd.template = tRt;
        dd.value = 0;
        dd.RefreshShownValue();

        // Navegación automática con ratón
        var nav = dd.navigation;
        nav.mode = Navigation.Mode.Automatic;
        dd.navigation = nav;

        return dd;
    }
}
#endif
