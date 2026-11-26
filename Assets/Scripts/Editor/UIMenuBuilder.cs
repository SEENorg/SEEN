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
/// panel de Ajustes con calidad gráfica (Alta/Media/Baja), resolución (de mayor a
/// menor), pantalla completa, volumen y sensibilidad del ratón.
///
/// Menú: <b>SEEN → Construir Menú Principal</b>
/// Reparar sin reconstruir: <b>SEEN → Reparar Input del Menú</b>
/// </summary>
public static class UIMenuBuilder
{
    // Paleta distópica
    static readonly Color colFondo        = new Color(0.030f, 0.035f, 0.050f);
    static readonly Color colAcento       = new Color(0.150f, 0.750f, 0.950f); // cyan
    static readonly Color colMagenta      = new Color(1.000f, 0.180f, 0.620f);
    static readonly Color colBoton        = new Color(0.075f, 0.085f, 0.110f);
    static readonly Color colBotonHover   = new Color(0.130f, 0.400f, 0.530f); // cyan marcado
    static readonly Color colBotonHover2  = new Color(0.600f, 0.200f, 0.500f); // magenta (botón salir)
    static readonly Color colTexto        = new Color(0.900f, 0.940f, 1.000f);
    static readonly Color colTextoSec     = new Color(0.550f, 0.620f, 0.700f);
    static readonly Color colControl      = new Color(0.090f, 0.100f, 0.130f);

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
                "ADVERTENCIA: se reemplazará la escena abierta actual\n" +
                "(si personalizaste textos del menú, tendrás que rehacerlos).\n\n" +
                "¿Continuar?", "Sí, construir", "Cancelar")) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // EventSystem con el módulo NUEVO de Input System.
        // NO se usa StandaloneInputModule: lee el Input clásico y revienta con
        // "Active Input Handling = Input System Package".
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        var inputModule = es.AddComponent<InputSystemUIInputModule>();

        // Cámara de fondo (el Canvas Overlay se dibuja encima igual)
        if (Camera.main == null)
        {
            var camGO = new GameObject("MenuCamera");
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.030f, 0.035f, 0.050f);
            camGO.AddComponent<AudioListener>();
        }

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

        var btnJugar   = CrearBoton(panelMenu.transform, "BotonJugar",   "JUGAR",
                                    new Vector2(0f, 20f),  new Vector2(340f, 68f), colAcento);
        var btnAjustes = CrearBoton(panelMenu.transform, "BotonAjustes", "AJUSTES",
                                    new Vector2(0f, -70f), new Vector2(340f, 68f), colAcento);
        var btnSalir   = CrearBoton(panelMenu.transform, "BotonSalir",   "SALIR",
                                    new Vector2(0f, -160f), new Vector2(340f, 68f), colMagenta);

        CrearTexto(panelMenu.transform, "Version", "v0.1 — Prototipo jugable", 18,
                   TextAnchor.MiddleCenter, new Vector2(0f, -320f), new Vector2(600f, 30f), colTextoSec);

        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnJugar.onClick, menu.Jugar);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnAjustes.onClick, menu.MostrarAjustes);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnSalir.onClick, menu.SalirDelJuego);

        // ── Panel Ajustes ──
        var panelAjustes = CrearPanel(canvasGO.transform, "PanelAjustes", new Color(0.045f, 0.050f, 0.070f, 0.985f));
        menu.panelAjustes = panelAjustes;
        CrearTexto(panelAjustes.transform, "TituloAjustes", "A J U S T E S", 48, TextAnchor.MiddleCenter,
                   new Vector2(0f, 280f), new Vector2(900f, 70f), colTexto);
        CrearLinea(panelAjustes.transform, new Vector2(0f, 240f), new Vector2(420f, 2f), colAcento);

        // Calidad gráfica (Alta / Media / Baja)
        CrearTexto(panelAjustes.transform, "LblCalidad", "CALIDAD GRÁFICA", 24, TextAnchor.MiddleLeft,
                   new Vector2(-210f, 185f), new Vector2(330f, 40f), colTexto);
        var ddCalidad = CrearDropdown(panelAjustes.transform, "DropdownCalidad",
                                      new Vector2(170f, 185f), new Vector2(340f, 54f));
        ddCalidad.ClearOptions();
        ddCalidad.AddOptions(new System.Collections.Generic.List<string> { "Alta", "Media", "Baja" });
        ddCalidad.value = 0;
        ddCalidad.RefreshShownValue();

        // Resolución (se rellena en orden descendente desde SettingsManager)
        CrearTexto(panelAjustes.transform, "LblResolucion", "RESOLUCIÓN", 24, TextAnchor.MiddleLeft,
                   new Vector2(-210f, 115f), new Vector2(330f, 40f), colTexto);
        var ddResolucion = CrearDropdown(panelAjustes.transform, "DropdownResolucion",
                                         new Vector2(170f, 115f), new Vector2(340f, 54f));

        // Volumen
        CrearTexto(panelAjustes.transform, "LblVolumen", "VOLUMEN MAESTRO", 24, TextAnchor.MiddleLeft,
                   new Vector2(-210f, 45f), new Vector2(330f, 40f), colTexto);
        var slVolumen = CrearSlider(panelAjustes.transform, "SliderVolumen",
                                    new Vector2(170f, 45f), new Vector2(340f, 40f), 0f, 1f, 1f);

        // Sensibilidad
        CrearTexto(panelAjustes.transform, "LblSensibilidad", "SENSIBILIDAD RATÓN", 24, TextAnchor.MiddleLeft,
                   new Vector2(-210f, -25f), new Vector2(330f, 40f), colTexto);
        var slSens = CrearSlider(panelAjustes.transform, "SliderSensibilidad",
                                 new Vector2(170f, -25f), new Vector2(340f, 40f), 0.02f, 0.5f, 0.15f);

        var btnVolver = CrearBoton(panelAjustes.transform, "BotonVolver", "VOLVER",
                                   new Vector2(0f, -160f), new Vector2(280f, 62f), colAcento);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnVolver.onClick, menu.MostrarMenuPrincipal);

        CrearTexto(panelAjustes.transform, "NotaAjustes", "Los cambios se guardan automáticamente", 18,
                   TextAnchor.MiddleCenter, new Vector2(0f, -220f), new Vector2(600f, 30f), colTextoSec);

        ajustes.calidadDropdown = ddCalidad;
        ajustes.resolucionDropdown = ddResolucion;
        ajustes.volumenSlider = slVolumen;
        ajustes.sensibilidadSlider = slSens;

        panelAjustes.SetActive(false);

        // ── Panel NIVELES (selector) ──
        var panelNiveles = CrearPanel(canvasGO.transform, "PanelNiveles", new Color(0.045f, 0.050f, 0.070f, 0.985f));
        menu.panelNiveles = panelNiveles;

        var tituloNiveles = CrearTexto(panelNiveles.transform, "TituloNiveles", "NIVELES", 54, TextAnchor.MiddleCenter,
                                       new Vector2(0f, 250f), new Vector2(800f, 70f), colTexto);
        tituloNiveles.fontStyle = FontStyle.Bold;
        CrearLinea(panelNiveles.transform, new Vector2(0f, 200f), new Vector2(560f, 3f), colMagenta);

        var botonesNivel = new Button[3];
        string[] nombresNivel = { "1", "2", "3" };
        for (int i = 0; i < 3; i++)
        {
            int num = i + 1;
            float x = -160f + i * 160f;
            botonesNivel[i] = CrearBoton(panelNiveles.transform, "BotonNivel" + num, nombresNivel[i],
                                         new Vector2(x, 80f), new Vector2(120f, 120f), colAcento);

            // Listener PERSISTENTE (sobrevive al guardar la escena)
            switch (num)
            {
                case 1: UnityEditor.Events.UnityEventTools.AddPersistentListener(botonesNivel[i].onClick, menu.CargarNivel1); break;
                case 2: UnityEditor.Events.UnityEventTools.AddPersistentListener(botonesNivel[i].onClick, menu.CargarNivel2); break;
                case 3: UnityEditor.Events.UnityEventTools.AddPersistentListener(botonesNivel[i].onClick, menu.CargarNivel3); break;
            }
        }

        menu.botonNivel1 = botonesNivel[0];
        menu.botonNivel2 = botonesNivel[1];
        menu.botonNivel3 = botonesNivel[2];

        var btnVolverNiveles = CrearBoton(panelNiveles.transform, "BotonVolverNiveles", "VOLVER",
                                          new Vector2(0f, -80f), new Vector2(280f, 62f), colMagenta);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnVolverNiveles.onClick, menu.MostrarMenuPrincipal);

        CrearTexto(panelNiveles.transform, "NotaNiveles", "Elige un nivel para empezar", 20,
                   TextAnchor.MiddleCenter, new Vector2(0f, -160f), new Vector2(700f, 30f), colTextoSec);

        panelNiveles.SetActive(false);

        // ── Guardar escena y Build Settings ──
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");

        RegistrarEscenasBuild();   // preserva los niveles existentes + añade menú y Juego

        AssetDatabase.SaveAssets();

        // Play siempre arranca en el menú
        var menuAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MainMenu.unity");
        if (menuAsset != null)
            EditorSceneManager.playModeStartScene = menuAsset;

        AssetDatabase.SaveAssets();

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>("Assets/Scenes/MainMenu.unity");
        Debug.Log("[UIMenuBuilder] ¡Menú construido! Play arranca en el menú y usa el nuevo Input System.");
    }

    // ── Reparación del menú existente ────────────────────────────

    /// <summary>
    /// Repara la escena del menú SIN reconstruirla (conserva tus personalizaciones):
    /// elimina el StandaloneInputModule (input clásico, incompatible con el proyecto),
    /// asegura el InputSystemUIInputModule y añade una cámara si falta.
    /// </summary>
    [MenuItem("SEEN/Reparar Input del Menú")]
    static void FixMenuInput()
    {
        if (!AssetDatabase.LoadAssetAtPath<Object>("Assets/Scenes/MainMenu.unity"))
        {
            Debug.LogError("[SEENFix] No existe la escena MainMenu. Ejecutá primero 'Construir Menú Principal'.");
            return;
        }

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

        int eliminados = 0;
        foreach (var legacy in Object.FindObjectsByType<StandaloneInputModule>(FindObjectsSortMode.None))
        {
            Undo.DestroyObjectImmediate(legacy);
            eliminados++;
        }
        if (eliminados > 0)
            Debug.Log($"[SEENFix] {eliminados} StandaloneInputModule eliminado(s) (eran la causa de los errores).");

        var es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            var esGO = new GameObject("EventSystem");
            es = esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }
        if (es.GetComponent<InputSystemUIInputModule>() == null)
            Undo.AddComponent<InputSystemUIInputModule>(es.gameObject);

        var cam = Camera.main;
        if (cam == null)
        {
            var camGO = new GameObject("MenuCamera");
            cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.030f, 0.035f, 0.050f);
            camGO.AddComponent<AudioListener>();
            Debug.Log("[SEENFix] Cámara del menú creada.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SEENFix] ¡Menú reparado! Volvé a dar Play.");
    }

    // ───────────────────────────────────────────────────────── Helpers

    /// <summary>
    /// Registra menú y Juego en Build Settings preservando todas las escenas
    /// existentes (antes el builder borraba los Niveles 2-5).
    /// </summary>
    static void RegistrarEscenasBuild()
    {
        var escenas = new System.Collections.Generic.List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Juego.unity", true)
        };

        // Preservar todo lo que ya estuviera registrado (Nivel2..Nivel5)
        foreach (var existente in EditorBuildSettings.scenes)
        {
            if (existente == null || string.IsNullOrEmpty(existente.path)) continue;
            if (!escenas.Exists(e => e.path == existente.path))
                escenas.Add(existente);
        }

        EditorBuildSettings.scenes = escenas.ToArray();
    }
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

    static Button CrearBoton(Transform parent, string nombre, string etiqueta, Vector2 pos, Vector2 size, Color colorHighlight)
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
        colors.highlightedColor = colorHighlight;      // hover bien visible
        colors.pressedColor = Color.Lerp(colorHighlight, Color.black, 0.35f);
        colors.selectedColor = colorHighlight;
        colors.disabledColor = new Color(0.15f, 0.15f, 0.15f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        btn.colors = colors;

        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;              // el ratón manda, sin auto-selección rara
        btn.navigation = nav;

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
        img.color = colControl;

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
        mImg.raycastTarget = false;
        tgl.graphic = mImg;

        return tgl;
    }

    /// <summary>
    /// Slider con línea FIJA y perilla móvil: la pista no crece, solo se mueve el handle.
    /// Toda la barra es cliqueable/arrastrable.
    /// </summary>
    static Slider CrearSlider(Transform parent, string nombre, Vector2 pos, Vector2 size,
                              float min, float max, float value)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        // Zona de arrastre: transparente pero interactiva
        var zona = go.AddComponent<Image>();
        zona.color = new Color(0f, 0f, 0f, 0f);
        zona.raycastTarget = true;

        var slider = go.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.targetGraphic = zona;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;

        // Fill oculto (solo para que Slider no haga NRE; no se ve)
        var fill = new GameObject("FillHidden", typeof(RectTransform));
        fill.transform.SetParent(go.transform, false);
        var fRt = (RectTransform)fill.transform;
        fRt.anchorMin = new Vector2(0f, 0.5f);
        fRt.anchorMax = new Vector2(0f, 0.5f);
        fRt.sizeDelta = Vector2.zero;
        var fImg = fill.AddComponent<Image>();
        fImg.color = new Color(0f, 0f, 0f, 0f);
        fImg.raycastTarget = false;
        slider.fillRect = fRt;

        // Línea fija (la pista, visible)
        var linea = new GameObject("Linea", typeof(RectTransform));
        linea.transform.SetParent(go.transform, false);
        var lRt = (RectTransform)linea.transform;
        lRt.anchorMin = new Vector2(0f, 0.5f);
        lRt.anchorMax = new Vector2(1f, 0.5f);
        lRt.pivot = new Vector2(0.5f, 0.5f);
        lRt.sizeDelta = new Vector2(0f, 5f);
        lRt.anchoredPosition = Vector2.zero;
        var lImg = linea.AddComponent<Image>();
        lImg.color = new Color(0.30f, 0.36f, 0.42f);   // gris fija, NO crece
        lImg.raycastTarget = false;

        // Perilla (se mueve con el valor)
        var handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(go.transform, false);
        var hRt = (RectTransform)handle.transform;
        hRt.anchorMin = new Vector2(0f, 0.5f);
        hRt.anchorMax = new Vector2(0f, 0.5f);
        hRt.pivot = new Vector2(0.5f, 0.5f);
        hRt.sizeDelta = new Vector2(26f, size.y);
        hRt.anchoredPosition = Vector2.zero;
        var hImg = handle.AddComponent<Image>();
        hImg.color = colAcento;
        hImg.raycastTarget = false;
        slider.handleRect = hRt;

        var nav = slider.navigation;
        nav.mode = Navigation.Mode.None;
        slider.navigation = nav;

        return slider;
    }

    /// <summary>
    /// Dropdown con la lista saliendo PEGADA a la caja (sin espacio fantasma)
    /// y con VerticalLayoutGroup para que las opciones se apilen correctamente.
    /// </summary>
    static Dropdown CrearDropdown(Transform parent, string nombre, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = colControl;

        var dd = go.AddComponent<Dropdown>();
        dd.targetGraphic = img;
        dd.transition = Selectable.Transition.ColorTint;

        var label = CrearTexto(dd.transform, "Label", "—", 24, TextAnchor.MiddleLeft,
                               new Vector2(14f, 0f), new Vector2(size.x - 44f, size.y), colTexto);
        dd.captionText = label;

        CrearTexto(dd.transform, "Arrow", "▼", 16, TextAnchor.MiddleRight,
                   new Vector2(-12f, 0f), new Vector2(28f, size.y), colAcento);

        // ── Template (pegado abajo, pivote arriba) ──
        var templateGO = new GameObject("Template", typeof(RectTransform));
        templateGO.transform.SetParent(go.transform, false);
        var tRt = (RectTransform)templateGO.transform;
        tRt.anchorMin = new Vector2(0f, 0f);
        tRt.anchorMax = new Vector2(1f, 0f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, 2f);    // 2px de separación, sin hueco grande
        tRt.sizeDelta = new Vector2(0f, 220f);
        var tImg = templateGO.AddComponent<Image>();
        tImg.color = new Color(0.07f, 0.08f, 0.11f);
        tImg.raycastTarget = false;

        var scroll = templateGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        // Viewport (stretch)
        var viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(templateGO.transform, false);
        var vRt = (RectTransform)viewport.transform;
        vRt.anchorMin = Vector2.zero;
        vRt.anchorMax = Vector2.one;
        vRt.offsetMin = Vector2.zero;
        vRt.offsetMax = Vector2.zero;
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = vRt;

        // Content (anclado arriba, auto-altura)
        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var cRt = (RectTransform)content.transform;
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.sizeDelta = new Vector2(0f, 40f);

        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.spacing = 3f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = cRt;

        // Item
        var itemGO = new GameObject("Item", typeof(RectTransform));
        itemGO.transform.SetParent(content.transform, false);
        var iRt = (RectTransform)itemGO.transform;
        iRt.anchorMin = new Vector2(0f, 1f);
        iRt.anchorMax = new Vector2(1f, 1f);
        iRt.pivot = new Vector2(0.5f, 1f);
        iRt.sizeDelta = new Vector2(0f, 42f);

        var iElem = itemGO.AddComponent<LayoutElement>();
        iElem.preferredHeight = 42f;
        iElem.minHeight = 42f;

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
        ibImg.raycastTarget = false;
        iToggle.graphic = ibImg;

        var itemLabel = CrearTexto(itemGO.transform, "Item Label", "—", 22, TextAnchor.MiddleLeft,
                                   new Vector2(12f, 0f), new Vector2(size.x - 30f, 42f), colTexto);
        dd.itemText = itemLabel;

        dd.template = tRt;
        dd.value = 0;
        dd.RefreshShownValue();

        var nav = dd.navigation;
        nav.mode = Navigation.Mode.None;
        dd.navigation = nav;

        return dd;
    }
}
#endif
