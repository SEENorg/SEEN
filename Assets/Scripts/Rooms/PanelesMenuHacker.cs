using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Le da a los paneles de AJUSTES y NIVELES el mismo estilo limpio del menú principal:
/// todo a la derecha, sin fondo que tape el video, con mando, teclado y ratón.
///
/// - NIVELES: tres tarjetas. La seleccionada crece, enciende su borde y muestra "ENTRAR".
///   Al elegir una se reproduce la cinemática de glitch (CinematicaGlitch) y se carga el nivel.
/// - AJUSTES: cada opción va en una fila; la fila seleccionada crece un poco y se ilumina.
///
/// Reutiliza los objetos de la escena (no cambia SettingsManager ni MainMenuManager).
/// Va en el objeto MenuController. Si no lo agregas, MenuPrincipalHacker lo agrega solo.
/// </summary>
public class PanelesMenuHacker : MonoBehaviour
{
    // ── Medidas (Canvas de 1920 x 1080) ─────────────────────────────
    private const float XColumna = 560f;
    private const float AnchoColumna = 600f;
    private const float AnchoFila = 600f;
    private const float AltoFila = 62f;
    private const float AnchoControl = 260f;
    private const float AnchoTarjeta = 144f;
    private const float AltoTarjeta = 220f;
    private const float SeparacionTarjetas = 160f;

    [Tooltip("Menú cuyos paneles se cambian. Si está vacío, se usa el MainMenuManager de este objeto.")]
    [SerializeField] private MainMenuManager menu;

    [Header("Niveles")]
    [Tooltip("Texto pequeño debajo del número de cada tarjeta.")]
    [SerializeField] private string[] descripcionesNivel = { "SECTOR DE ENTRADA", "RED INTERNA", "NÚCLEO DEL SISTEMA", "ESCAPE DEL ANTIVIRUS" };
    [Tooltip("Si está apagado, el nivel se carga directo, sin cinemática.")]
    [SerializeField] private bool usarCinematica = true;

    [Header("Fondo")]
    [Range(0f, 1f)]
    [SerializeField] private float oscuridadDegradado = 0.55f;

    // ── Colores ─────────────────────────────────────────────────────
    private readonly Color colorCaja = new Color(0.02f, 0.06f, 0.09f, 0.55f);
    private readonly Color colorCajaSeleccion = new Color(0.05f, 0.18f, 0.25f, 0.80f);
    private readonly Color colorBorde = new Color(0.15f, 0.85f, 1f, 0.25f);
    private readonly Color colorMagenta = new Color(1f, 0.18f, 0.62f);
    private readonly Color colorTextoSuave = new Color(0.70f, 0.78f, 0.84f);
    private readonly Color colorControl = new Color(0.04f, 0.10f, 0.14f, 0.9f);

    // ── Estado ──────────────────────────────────────────────────────
    private CajasRedondeadasUI cajas;
    private Texture2D texturaDegradado;
    private Button[] botonesNivel;
    private Text[] textosEntrar;
    private Button botonVolverAjustes;
    private Button botonVolverNiveles;

    private void Awake()
    {
        if (menu == null) menu = GetComponent<MainMenuManager>();
        if (menu == null)
        {
            Debug.LogWarning("[PanelesMenuHacker] No encontré el MainMenuManager.", this);
            enabled = false;
            return;
        }

        cajas = new CajasRedondeadasUI();
        texturaDegradado = CrearTexturaDegradado();

        if (menu.panelNiveles != null) ConstruirNiveles(menu.panelNiveles);
        if (menu.panelAjustes != null) ConstruirAjustes(menu.panelAjustes);
    }

    private void OnDestroy()
    {
        if (cajas != null) cajas.Liberar();
        if (texturaDegradado != null) Destroy(texturaDegradado);
    }

    private void Update()
    {
        AnimarTextosEntrar();

        // Para el mando: si nada quedó seleccionado en el panel abierto, se selecciona el primero
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject != null) return;
        if (CinematicaGlitch.EnCurso) return;

        if (menu.panelNiveles != null && menu.panelNiveles.activeInHierarchy && botonesNivel != null && botonesNivel[0] != null)
        {
            EventSystem.current.SetSelectedGameObject(botonesNivel[0].gameObject);
        }
        else if (menu.panelAjustes != null && menu.panelAjustes.activeInHierarchy && botonVolverAjustes != null)
        {
            Selectable primero = menu.panelAjustes.GetComponentInChildren<Selectable>();
            if (primero != null) EventSystem.current.SetSelectedGameObject(primero.gameObject);
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  NIVELES
    // ══════════════════════════════════════════════════════════════

    private void ConstruirNiveles(GameObject panel)
    {
        PrepararFondo(panel);
        Transform columna = CrearColumna(panel);

        Encabezado(panel, columna, "TituloNiveles", "NIVELES", "[ SELECCIONA UN NODO DE ACCESO ]");

        botonesNivel = new Button[4];
        textosEntrar = new Text[4];
        Button[] deEscena = { menu.botonNivel1, menu.botonNivel2, menu.botonNivel3, menu.botonNivel4 };
        for (int i = 0; i < 4; i++)
        {
            Button boton = deEscena[i];
            if (boton == null)
            {
                Transform buscado = panel.transform.Find("BotonNivel" + (i + 1));
                if (buscado != null) boton = buscado.GetComponent<Button>();
            }
            if (boton == null && i == 3)
            {
                GameObject go4 = new GameObject("BotonNivel4", typeof(RectTransform), typeof(Image), typeof(Button));
                go4.transform.SetParent(panel.transform, false);
                boton = go4.GetComponent<Button>();
                menu.botonNivel4 = boton;
            }
            if (boton == null) continue;
            botonesNivel[i] = boton;
            float x = (i - 1.5f) * SeparacionTarjetas;
            PrepararTarjetaNivel(boton, i, columna, new Vector2(x, 40f));
        }

        // El clic ya no carga la escena directo: primero pasa por la cinemática
        if (botonesNivel[0] != null) ReemplazarClic(botonesNivel[0], EntrarNivel1);
        if (botonesNivel[1] != null) ReemplazarClic(botonesNivel[1], EntrarNivel2);
        if (botonesNivel[2] != null) ReemplazarClic(botonesNivel[2], EntrarNivel3);
        if (botonesNivel[3] != null) ReemplazarClic(botonesNivel[3], EntrarNivel4);

        Transform volver = panel.transform.Find("BotonVolverNiveles");
        if (volver != null) botonVolverNiveles = PrepararBotonVolver(volver, columna, new Vector2(0f, -170f));

        ConectarNavegacionNiveles();

        Transform nota = panel.transform.Find("NotaNiveles");
        if (nota != null) PrepararNota(nota, columna, new Vector2(0f, -240f));
    }

    private void PrepararTarjetaNivel(Button boton, int indice, Transform columna, Vector2 posicion)
    {
        Transform t = boton.transform;
        Colocar(t, columna, posicion, new Vector2(AnchoTarjeta, AltoTarjeta));

        Image fondo = boton.GetComponent<Image>();
        if (fondo == null) fondo = t.gameObject.AddComponent<Image>();
        Image borde = cajas.Redondear(fondo, colorBorde, 1.2f);
        fondo.color = Color.white;
        boton.targetGraphic = fondo;
        AplicarTinte(boton);

        string numero = "0" + (indice + 1);

        HackUI.CrearTexto(t, "Etiqueta", "NIVEL", 15, TextAnchor.MiddleCenter,
                          new Vector2(0f, 82f), new Vector2(AnchoTarjeta, 22f), colorTextoSuave);

        Text textoNumero = null;
        Transform textoEscena = t.Find("Texto");
        if (textoEscena != null) textoNumero = textoEscena.GetComponent<Text>();
        if (textoNumero == null)
        {
            textoNumero = HackUI.CrearTexto(t, "Texto", numero, 72, TextAnchor.MiddleCenter,
                                            Vector2.zero, Vector2.zero, Color.white);
        }
        RectTransform rtNumero = (RectTransform)textoNumero.transform;
        rtNumero.anchorMin = new Vector2(0.5f, 0.5f);
        rtNumero.anchorMax = new Vector2(0.5f, 0.5f);
        rtNumero.pivot = new Vector2(0.5f, 0.5f);
        rtNumero.sizeDelta = new Vector2(AnchoTarjeta, 90f);
        rtNumero.anchoredPosition = new Vector2(0f, 20f);
        textoNumero.text = numero;
        textoNumero.fontSize = 72;
        textoNumero.fontStyle = FontStyle.Bold;
        textoNumero.alignment = TextAnchor.MiddleCenter;
        textoNumero.transform.SetAsLastSibling();

        GameObject linea = HackUI.NuevoUI(t, "Linea", new Vector2(0f, -32f), new Vector2(60f, 2f));
        Image imagenLinea = linea.AddComponent<Image>();
        imagenLinea.color = colorMagenta;
        imagenLinea.raycastTarget = false;

        string descripcion = ObtenerDescripcionNivel(indice);
        Text textoDescripcion = HackUI.CrearTexto(t, "Descripcion", descripcion, 13, TextAnchor.MiddleCenter,
                                                  new Vector2(0f, -55f), new Vector2(AnchoTarjeta - 16f, 34f), colorTextoSuave);
        textoDescripcion.horizontalOverflow = HorizontalWrapMode.Wrap;

        textosEntrar[indice] = HackUI.CrearTexto(t, "Entrar", "> ENTRAR", 14, TextAnchor.MiddleCenter,
                                                 new Vector2(0f, -90f), new Vector2(AnchoTarjeta, 20f), HackUI.ColAcento);
        textosEntrar[indice].fontStyle = FontStyle.Bold;
        textosEntrar[indice].enabled = false;

        ElementoMenuUI elemento = t.gameObject.AddComponent<ElementoMenuUI>();
        elemento.Configurar(t, 1.18f, borde, colorBorde, HackUI.ColAcento, textoNumero, Color.white, HackUI.ColAcento);
    }

    private string ObtenerDescripcionNivel(int indice)
    {
        if (descripcionesNivel != null && indice >= 0 && indice < descripcionesNivel.Length && !string.IsNullOrEmpty(descripcionesNivel[indice]))
            return descripcionesNivel[indice];
        switch (indice)
        {
            case 0: return "SECTOR DE ENTRADA";
            case 1: return "RED INTERNA";
            case 2: return "NÚCLEO DEL SISTEMA";
            case 3: return "ESCAPE DEL ANTIVIRUS";
            default: return "";
        }
    }

    /// <summary>1 ↔ 2 ↔ 3 ↔ 4 con izquierda/derecha; abajo va a VOLVER y de VOLVER se sube a la tarjeta 2.</summary>
    private void ConectarNavegacionNiveles()
    {
        for (int i = 0; i < 4; i++)
        {
            if (botonesNivel[i] == null) continue;
            Navigation navegacion = new Navigation();
            navegacion.mode = Navigation.Mode.Explicit;
            navegacion.selectOnLeft = botonesNivel[(i + 3) % 4];
            navegacion.selectOnRight = botonesNivel[(i + 1) % 4];
            navegacion.selectOnDown = botonVolverNiveles;
            botonesNivel[i].navigation = navegacion;
        }

        if (botonVolverNiveles != null)
        {
            Navigation navegacionVolver = new Navigation();
            navegacionVolver.mode = Navigation.Mode.Explicit;
            navegacionVolver.selectOnUp = botonesNivel[1] != null ? botonesNivel[1] : botonesNivel[0];
            botonVolverNiveles.navigation = navegacionVolver;
        }
    }

    private void ReemplazarClic(Button boton, UnityEngine.Events.UnityAction accion)
    {
        boton.onClick = new Button.ButtonClickedEvent();
        boton.onClick.AddListener(accion);
    }

    private void EntrarNivel1() { Entrar(1); }
    private void EntrarNivel2() { Entrar(2); }
    private void EntrarNivel3() { Entrar(3); }
    private void EntrarNivel4() { Entrar(4); }

    private void Entrar(int numero)
    {
        if (CinematicaGlitch.EnCurso) return;
        if (!usarCinematica)
        {
            menu.IrANivel(numero);
            return;
        }

        string descripcion = ObtenerDescripcionNivel(numero - 1);
        CinematicaGlitch.Reproducir(menu, numero, descripcion);
    }

    /// <summary>"> ENTRAR" parpadea solo en la tarjeta seleccionada.</summary>
    private void AnimarTextosEntrar()
    {
        if (botonesNivel == null || textosEntrar == null) return;
        GameObject actual = null;
        if (EventSystem.current != null) actual = EventSystem.current.currentSelectedGameObject;
        bool parpadeo = Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.55f;

        for (int i = 0; i < botonesNivel.Length && i < textosEntrar.Length; i++)
        {
            if (botonesNivel[i] == null || textosEntrar[i] == null) continue;
            textosEntrar[i].enabled = botonesNivel[i].gameObject == actual && parpadeo;
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  AJUSTES
    // ══════════════════════════════════════════════════════════════

    private void ConstruirAjustes(GameObject panel)
    {
        PrepararFondo(panel);
        Transform columna = CrearColumna(panel);

        Encabezado(panel, columna, "TituloAjustes", "AJUSTES", "[ CONFIGURACIÓN DEL SISTEMA ]");

        Selectable[] controles = new Selectable[5];
        controles[0] = Fila(panel, columna, 0, "LblCalidad", "DropdownCalidad", 175f);
        controles[1] = Fila(panel, columna, 1, "LblResolucion", "DropdownResolucion", 100f);
        controles[2] = Fila(panel, columna, 2, "LblPantalla", "TogglePantallaCompleta", 25f);
        controles[3] = Fila(panel, columna, 3, "LblVolumen", "SliderVolumen", -50f);
        controles[4] = Fila(panel, columna, 4, "LblSensibilidad", "SliderSensibilidad", -125f);

        Transform volver = panel.transform.Find("BotonVolver");
        if (volver != null) botonVolverAjustes = PrepararBotonVolver(volver, columna, new Vector2(0f, -225f));

        // Arriba / abajo recorre las filas; izquierda / derecha mueve los sliders
        Selectable[] orden = new Selectable[6];
        for (int i = 0; i < 5; i++) orden[i] = controles[i];
        orden[5] = botonVolverAjustes;
        ConectarVertical(orden);

        Transform nota = panel.transform.Find("NotaAjustes");
        if (nota != null) PrepararNota(nota, columna, new Vector2(0f, -290f));
    }

    /// <summary>Mete la etiqueta y el control en una fila redondeada que se ilumina al seleccionarse.</summary>
    private Selectable Fila(GameObject panel, Transform columna, int indice, string nombreEtiqueta,
                            string nombreControl, float y)
    {
        Image bordeFila = cajas.CrearCaja(columna, "Fila" + indice, new Vector2(0f, y), new Vector2(AnchoFila, AltoFila),
                                          colorCaja, colorBorde, 1.4f);
        Transform fila = bordeFila.transform.parent;
        fila.GetComponent<Image>().raycastTarget = false;

        Text etiqueta = null;
        Transform tEtiqueta = panel.transform.Find(nombreEtiqueta);
        if (tEtiqueta != null)
        {
            Colocar(tEtiqueta, fila, new Vector2(-140f, 0f), new Vector2(280f, 40f));
            etiqueta = tEtiqueta.GetComponent<Text>();
            if (etiqueta != null)
            {
                etiqueta.fontSize = 20;
                etiqueta.alignment = TextAnchor.MiddleLeft;
                etiqueta.color = Color.white;
            }
        }

        Transform tControl = panel.transform.Find(nombreControl);
        if (tControl == null) return null;
        Selectable control = tControl.GetComponent<Selectable>();
        if (control == null) return null;

        Dropdown dropdown = tControl.GetComponent<Dropdown>();
        Toggle toggle = tControl.GetComponent<Toggle>();
        Slider slider = tControl.GetComponent<Slider>();

        if (dropdown != null) PrepararDropdown(dropdown, fila);
        else if (toggle != null) PrepararToggle(toggle, fila);
        else if (slider != null) PrepararSlider(slider, fila);

        ElementoMenuUI elemento = tControl.gameObject.AddComponent<ElementoMenuUI>();
        elemento.Configurar(fila, 1.04f, bordeFila, colorBorde, HackUI.ColAcento, etiqueta, Color.white, HackUI.ColAcento);
        return control;
    }

    private void PrepararDropdown(Dropdown dropdown, Transform fila)
    {
        Transform t = dropdown.transform;
        Colocar(t, fila, new Vector2(150f, 0f), new Vector2(AnchoControl, 44f));

        Image fondo = dropdown.GetComponent<Image>();
        if (fondo != null)
        {
            cajas.Redondear(fondo, new Color(0.15f, 0.85f, 1f, 0.35f), 2f);
            fondo.color = colorControl;
        }

        // Texto y flecha ajustados al nuevo ancho (antes la flecha quedaba encima del texto)
        Transform label = t.Find("Label");
        if (label != null)
        {
            RectTransform rt = (RectTransform)label;
            rt.sizeDelta = new Vector2(AnchoControl - 50f, 44f);
            rt.anchoredPosition = new Vector2(-12f, 0f);
            Text texto = label.GetComponent<Text>();
            if (texto != null) texto.fontSize = 20;
        }
        Transform flecha = t.Find("Arrow");
        if (flecha != null)
        {
            RectTransform rt = (RectTransform)flecha;
            rt.sizeDelta = new Vector2(28f, 44f);
            rt.anchoredPosition = new Vector2(AnchoControl / 2f - 22f, 0f);
            flecha.SetAsLastSibling();
        }
    }

    private void PrepararToggle(Toggle toggle, Transform fila)
    {
        Transform t = toggle.transform;
        Colocar(t, fila, new Vector2(AnchoFila / 2f - 50f, 0f), new Vector2(40f, 40f));

        Image fondo = toggle.GetComponent<Image>();
        if (fondo != null)
        {
            cajas.Redondear(fondo, new Color(0.15f, 0.85f, 1f, 0.5f), 2.5f);
            fondo.color = colorControl;
        }
        if (toggle.graphic != null)
        {
            Image marca = toggle.graphic as Image;
            if (marca != null)
            {
                cajas.Redondear(marca, Color.clear, 3f);
                marca.color = HackUI.ColAcento;
            }
        }

        // Texto "SÍ / NO" para no depender solo del color
        Text estado = HackUI.CrearTexto(fila, "EstadoToggle", "", 16, TextAnchor.MiddleRight,
                                        new Vector2(AnchoFila / 2f - 130f, 0f), new Vector2(90f, 30f), colorTextoSuave);
        IndicadorToggleUI indicador = toggle.gameObject.AddComponent<IndicadorToggleUI>();
        indicador.Configurar(toggle, estado);
    }

    private void PrepararSlider(Slider slider, Transform fila)
    {
        Transform t = slider.transform;
        Colocar(t, fila, new Vector2(150f, 0f), new Vector2(AnchoControl, 36f));

        Transform pista = t.Find("Linea");
        if (pista != null)
        {
            Image imagen = pista.GetComponent<Image>();
            if (imagen != null) imagen.color = new Color(0.15f, 0.85f, 1f, 0.3f);
            RectTransform rt = (RectTransform)pista;
            rt.sizeDelta = new Vector2(0f, 4f);
        }
        if (slider.handleRect != null)
        {
            slider.handleRect.sizeDelta = new Vector2(16f, 30f);
            Image perilla = slider.handleRect.GetComponent<Image>();
            if (perilla != null)
            {
                cajas.Redondear(perilla, Color.clear, 4f);
                perilla.color = HackUI.ColAcento;
            }
        }
    }

    private void ConectarVertical(Selectable[] orden)
    {
        for (int i = 0; i < orden.Length; i++)
        {
            if (orden[i] == null) continue;
            Selectable arriba = BuscarVecino(orden, i, -1);
            Selectable abajo = BuscarVecino(orden, i, 1);

            Navigation navegacion = new Navigation();
            navegacion.mode = Navigation.Mode.Explicit;
            navegacion.selectOnUp = arriba;
            navegacion.selectOnDown = abajo;
            orden[i].navigation = navegacion;
        }
    }

    /// <summary>El siguiente control que exista hacia arriba o abajo (con vuelta al otro extremo).</summary>
    private Selectable BuscarVecino(Selectable[] orden, int desde, int paso)
    {
        int indice = desde;
        for (int intentos = 0; intentos < orden.Length; intentos++)
        {
            indice = (indice + paso + orden.Length) % orden.Length;
            if (orden[indice] != null && indice != desde) return orden[indice];
        }
        return null;
    }

    // ══════════════════════════════════════════════════════════════
    //  Comunes
    // ══════════════════════════════════════════════════════════════

    /// <summary>Quita el fondo casi negro del panel y pone el mismo degradado suave del menú.</summary>
    private void PrepararFondo(GameObject panel)
    {
        Image fondoPanel = panel.GetComponent<Image>();
        if (fondoPanel != null) fondoPanel.color = new Color(0f, 0f, 0f, 0f);

        // Las líneas viejas debajo de los títulos sobran
        for (int i = 0; i < panel.transform.childCount; i++)
        {
            Transform hijo = panel.transform.GetChild(i);
            if (hijo.name == "Linea") hijo.gameObject.SetActive(false);
        }

        GameObject go = HackUI.NuevoUI(panel.transform, "DegradadoDerecho", new Vector2(480f, 0f), new Vector2(960f, 1080f));
        RawImage imagen = go.AddComponent<RawImage>();
        imagen.texture = texturaDegradado;
        imagen.color = new Color(1f, 1f, 1f, oscuridadDegradado);
        imagen.raycastTarget = false;
        go.transform.SetAsFirstSibling();
    }

    private Transform CrearColumna(GameObject panel)
    {
        GameObject go = HackUI.NuevoUI(panel.transform, "Columna", new Vector2(XColumna, 0f), new Vector2(AnchoColumna, 1000f));
        return go.transform;
    }

    private void Encabezado(GameObject panel, Transform columna, string nombreTitulo, string texto, string etiqueta)
    {
        Text textoEtiqueta = HackUI.CrearTexto(columna, "Etiqueta", etiqueta, 15, TextAnchor.MiddleCenter,
                                               new Vector2(0f, 350f), new Vector2(AnchoColumna, 22f), HackUI.ColAcento);
        AgregarSombra(textoEtiqueta.gameObject);

        Text titulo = null;
        Transform tTitulo = panel.transform.Find(nombreTitulo);
        if (tTitulo != null)
        {
            Colocar(tTitulo, columna, new Vector2(0f, 295f), new Vector2(AnchoColumna, 80f));
            titulo = tTitulo.GetComponent<Text>();
        }
        if (titulo == null)
        {
            titulo = HackUI.CrearTexto(columna, nombreTitulo, texto, 64, TextAnchor.MiddleCenter,
                                       new Vector2(0f, 295f), new Vector2(AnchoColumna, 80f), Color.white);
        }
        titulo.text = texto;
        titulo.fontSize = 64;
        titulo.fontStyle = FontStyle.Bold;
        titulo.alignment = TextAnchor.MiddleCenter;
        titulo.color = Color.white;
        AgregarSombra(titulo.gameObject);

        GameObject separador = HackUI.NuevoUI(columna, "Separador", new Vector2(0f, 250f), new Vector2(120f, 3f));
        Image imagen = separador.AddComponent<Image>();
        imagen.color = colorMagenta;
        imagen.raycastTarget = false;
    }

    private Button PrepararBotonVolver(Transform t, Transform columna, Vector2 posicion)
    {
        Button boton = t.GetComponent<Button>();
        if (boton == null) return null;
        Colocar(t, columna, posicion, new Vector2(300f, 60f));

        Image fondo = boton.GetComponent<Image>();
        if (fondo == null) fondo = t.gameObject.AddComponent<Image>();
        Image borde = cajas.Redondear(fondo, colorBorde, 1.4f);
        fondo.color = Color.white;
        boton.targetGraphic = fondo;
        AplicarTinte(boton);

        Text texto = null;
        Transform tTexto = t.Find("Texto");
        if (tTexto != null)
        {
            RectTransform rt = (RectTransform)tTexto;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(300f, 60f);
            rt.anchoredPosition = Vector2.zero;
            texto = tTexto.GetComponent<Text>();
            if (texto != null)
            {
                texto.text = "< VOLVER";
                texto.fontSize = 24;
                texto.alignment = TextAnchor.MiddleCenter;
            }
            tTexto.SetAsLastSibling();
        }

        ElementoMenuUI elemento = t.gameObject.AddComponent<ElementoMenuUI>();
        elemento.Configurar(t, 1.08f, borde, colorBorde, colorMagenta, texto, Color.white, Color.white);
        return boton;
    }

    private void PrepararNota(Transform nota, Transform columna, Vector2 posicion)
    {
        Colocar(nota, columna, posicion, new Vector2(AnchoColumna, 24f));
        Text texto = nota.GetComponent<Text>();
        if (texto == null) return;
        texto.fontSize = 14;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = colorTextoSuave;
        AgregarSombra(texto.gameObject);
    }

    private void AplicarTinte(Selectable elemento)
    {
        elemento.transition = Selectable.Transition.ColorTint;
        ColorBlock colores = elemento.colors;
        colores.normalColor = colorCaja;
        colores.highlightedColor = colorCajaSeleccion;
        colores.selectedColor = colorCajaSeleccion;
        colores.pressedColor = Color.Lerp(colorCajaSeleccion, Color.white, 0.25f);
        colores.colorMultiplier = 1f;
        colores.fadeDuration = 0.06f;
        elemento.colors = colores;
    }

    private void AgregarSombra(GameObject go)
    {
        Shadow sombra = go.GetComponent<Shadow>();
        if (sombra == null) sombra = go.AddComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.8f);
        sombra.effectDistance = new Vector2(2f, -2f);
    }

    private Texture2D CrearTexturaDegradado()
    {
        Texture2D textura = new Texture2D(64, 1, TextureFormat.RGBA32, false);
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.filterMode = FilterMode.Bilinear;
        for (int x = 0; x < 64; x++)
        {
            float t = x / 63f;
            textura.SetPixel(x, 0, new Color(0f, 0f, 0f, t * t));
        }
        textura.Apply();
        return textura;
    }

    private void Colocar(Transform elemento, Transform padre, Vector2 posicion, Vector2 tamano)
    {
        elemento.SetParent(padre, false);
        RectTransform rt = (RectTransform)elemento;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = tamano;
        rt.anchoredPosition = posicion;
        elemento.localScale = Vector3.one;
    }
}
