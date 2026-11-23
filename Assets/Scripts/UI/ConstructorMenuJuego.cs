using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ayudante para armar los menús que salen DURANTE el juego (pausa, nivel completado,
/// game over, pantalla de controles) con el mismo estilo del menú principal:
/// tarjeta oscura redondeada, título grande, botones con borde que se ilumina y
/// crece al seleccionarlo, y navegación con mando, teclado y ratón.
///
/// No es un MonoBehaviour: cada menú crea uno, lo usa para construir su Canvas y
/// llama a Liberar() en su OnDestroy.
/// </summary>
public class ConstructorMenuJuego
{
    public static readonly Color ColorTarjeta = new Color(0.03f, 0.05f, 0.07f, 0.92f);
    public static readonly Color ColorBordeTarjeta = new Color(0.15f, 0.85f, 1f, 0.45f);
    public static readonly Color ColorBoton = new Color(0.05f, 0.10f, 0.14f, 0.95f);
    public static readonly Color ColorBotonSeleccion = new Color(0.07f, 0.22f, 0.30f, 1f);
    public static readonly Color ColorBordeBoton = new Color(0.15f, 0.85f, 1f, 0.25f);
    public static readonly Color ColorMagenta = new Color(1f, 0.18f, 0.62f);
    public static readonly Color ColorTextoSuave = new Color(0.70f, 0.78f, 0.84f);
    public static readonly Color ColorVerde = new Color(0.20f, 1f, 0.50f);
    public static readonly Color ColorRojo = new Color(1f, 0.25f, 0.30f);

    private readonly CajasRedondeadasUI cajas;

    public ConstructorMenuJuego()
    {
        cajas = new CajasRedondeadasUI();
    }

    public CajasRedondeadasUI Cajas
    {
        get { return cajas; }
    }

    public void Liberar()
    {
        cajas.Liberar();
    }

    /// <summary>Canvas a pantalla completa (1920 x 1080 de referencia) encima del juego.</summary>
    public static GameObject CrearCanvas(Transform padre, string nombre, int orden)
    {
        GameObject go = new GameObject(nombre);
        if (padre != null) go.transform.SetParent(padre, false);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = orden;
        CanvasScaler escalador = go.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        HackUI.AsegurarEventSystem();
        return go;
    }

    /// <summary>Oscurece toda la pantalla (y bloquea los clics al juego).</summary>
    public static Image CrearVelo(Transform padre, Color color)
    {
        GameObject go = HackUI.NuevoUI(padre, "Velo", Vector2.zero, Vector2.zero);
        HackUI.Estirar(go);
        Image velo = go.AddComponent<Image>();
        velo.color = color;
        velo.raycastTarget = true;
        return velo;
    }

    /// <summary>
    /// Tarjeta centrada con etiqueta superior, título y subtítulo. Devuelve el transform
    /// de la tarjeta (el relleno), donde se ponen los botones. El título queda en "titulo".
    /// </summary>
    public Transform CrearTarjeta(Transform padre, string nombre, Vector2 posicion, Vector2 tamano,
                                  string etiqueta, string textoTitulo, Color colorTitulo, string textoSubtitulo,
                                  out Text titulo, out Text subtitulo, out Image borde)
    {
        borde = cajas.CrearCaja(padre, nombre, posicion, tamano, ColorTarjeta, ColorBordeTarjeta, 1f);
        Transform tarjeta = borde.transform.parent;
        tarjeta.GetComponent<Image>().raycastTarget = false;

        float arriba = tamano.y / 2f;
        HackUI.CrearTexto(tarjeta, "Etiqueta", etiqueta, 15, TextAnchor.MiddleCenter,
                          new Vector2(0f, arriba - 32f), new Vector2(tamano.x - 40f, 22f), HackUI.ColAcento);

        titulo = HackUI.CrearTexto(tarjeta, "Titulo", textoTitulo, 54, TextAnchor.MiddleCenter,
                                   new Vector2(0f, arriba - 82f), new Vector2(tamano.x - 40f, 70f), colorTitulo);
        titulo.fontStyle = FontStyle.Bold;

        GameObject linea = HackUI.NuevoUI(tarjeta, "Separador", new Vector2(0f, arriba - 124f), new Vector2(120f, 3f));
        Image imagenLinea = linea.AddComponent<Image>();
        imagenLinea.color = ColorMagenta;
        imagenLinea.raycastTarget = false;

        subtitulo = HackUI.CrearTexto(tarjeta, "Subtitulo", textoSubtitulo, 18, TextAnchor.MiddleCenter,
                                      new Vector2(0f, arriba - 152f), new Vector2(tamano.x - 40f, 28f), ColorTextoSuave);
        return tarjeta;
    }

    /// <summary>Botón redondeado con número opcional, que crece y enciende su borde al seleccionarlo.</summary>
    public Button CrearBoton(Transform padre, string nombre, string texto, Vector2 posicion, Vector2 tamano, Color acento)
    {
        GameObject go = HackUI.NuevoUI(padre, nombre, posicion, tamano);
        Image fondo = go.AddComponent<Image>();
        Image borde = cajas.Redondear(fondo, ColorBordeBoton, 1.4f);
        fondo.color = Color.white;

        Button boton = go.AddComponent<Button>();
        boton.targetGraphic = fondo;
        AplicarTinte(boton);

        Text etiqueta = HackUI.CrearTexto(go.transform, "Texto", texto, 24, TextAnchor.MiddleCenter,
                                          Vector2.zero, tamano, Color.white);
        etiqueta.fontStyle = FontStyle.Bold;

        ElementoMenuUI elemento = go.AddComponent<ElementoMenuUI>();
        elemento.Configurar(go.transform, 1.06f, borde, ColorBordeBoton, acento, etiqueta, Color.white, acento);
        return boton;
    }

    /// <summary>
    /// Fila de ajustes: caja redondeada con el nombre a la izquierda. Devuelve la fila,
    /// su borde y la etiqueta, para que el control se ponga a la derecha.
    /// </summary>
    public Transform CrearFila(Transform padre, string nombre, string texto, Vector2 posicion, Vector2 tamano,
                               out Image borde, out Text etiqueta)
    {
        borde = cajas.CrearCaja(padre, nombre, posicion, tamano, new Color(0.02f, 0.06f, 0.09f, 0.75f), ColorBordeBoton, 1.4f);
        Transform fila = borde.transform.parent;
        fila.GetComponent<Image>().raycastTarget = false;
        etiqueta = HackUI.CrearTexto(fila, "Etiqueta", texto, 19, TextAnchor.MiddleLeft,
                                     new Vector2(-tamano.x / 4f + 10f, 0f), new Vector2(tamano.x / 2f - 20f, 30f), Color.white);
        return fila;
    }

    /// <summary>Fila con selector "◀ valor ▶" (izquierda/derecha o clic para cambiar).</summary>
    public SelectorOpcionUI CrearFilaSelector(Transform padre, string nombre, string texto, Vector2 posicion, Vector2 tamano,
                                              string[] opciones, int inicial, bool circular, System.Action<int> alCambiar)
    {
        Image bordeFila;
        Text etiqueta;
        Transform fila = CrearFila(padre, nombre, texto, posicion, tamano, out bordeFila, out etiqueta);

        Vector2 tamanoControl = new Vector2(tamano.x / 2f - 30f, tamano.y - 16f);
        GameObject go = HackUI.NuevoUI(fila, "Selector", new Vector2(tamano.x / 4f - 5f, 0f), tamanoControl);
        Image fondo = go.AddComponent<Image>();
        cajas.Redondear(fondo, new Color(0.15f, 0.85f, 1f, 0.35f), 2f);
        fondo.color = Color.white;
        Button boton = go.AddComponent<Button>();
        boton.targetGraphic = fondo;
        AplicarTinte(boton);

        Text valor = HackUI.CrearTexto(go.transform, "Valor", "", 18, TextAnchor.MiddleCenter, Vector2.zero, tamanoControl, Color.white);
        valor.fontStyle = FontStyle.Bold;

        SelectorOpcionUI selector = go.AddComponent<SelectorOpcionUI>();
        selector.Configurar(opciones, inicial, valor, circular, alCambiar);

        ElementoMenuUI elemento = go.AddComponent<ElementoMenuUI>();
        elemento.Configurar(fila, 1.03f, bordeFila, ColorBordeBoton, HackUI.ColAcento, etiqueta, Color.white, HackUI.ColAcento);
        return selector;
    }

    /// <summary>Fila con un slider (izquierda/derecha lo mueve) y el valor escrito al lado.</summary>
    public Slider CrearFilaSlider(Transform padre, string nombre, string texto, Vector2 posicion, Vector2 tamano,
                                  float minimo, float maximo, float valorInicial, out Text textoValor)
    {
        Image bordeFila;
        Text etiqueta;
        Transform fila = CrearFila(padre, nombre, texto, posicion, tamano, out bordeFila, out etiqueta);

        float anchoSlider = tamano.x / 2f - 110f;
        GameObject go = HackUI.NuevoUI(fila, "Slider", new Vector2(tamano.x / 4f - 45f, 0f), new Vector2(anchoSlider, 32f));
        Image zona = go.AddComponent<Image>();
        zona.color = new Color(0f, 0f, 0f, 0f);

        GameObject pista = HackUI.NuevoUI(go.transform, "Pista", Vector2.zero, new Vector2(anchoSlider, 6f));
        Image imagenPista = pista.AddComponent<Image>();
        imagenPista.color = new Color(0.15f, 0.85f, 1f, 0.2f);
        imagenPista.raycastTarget = false;

        GameObject areaRelleno = HackUI.NuevoUI(go.transform, "AreaRelleno", Vector2.zero, new Vector2(anchoSlider, 6f));
        GameObject relleno = HackUI.NuevoUI(areaRelleno.transform, "Relleno", Vector2.zero, Vector2.zero);
        Image imagenRelleno = relleno.AddComponent<Image>();
        imagenRelleno.color = HackUI.ColAcento;
        imagenRelleno.raycastTarget = false;
        RectTransform rtRelleno = (RectTransform)relleno.transform;
        rtRelleno.anchorMin = Vector2.zero;
        rtRelleno.anchorMax = new Vector2(0f, 1f);
        rtRelleno.sizeDelta = Vector2.zero;

        GameObject areaPerilla = HackUI.NuevoUI(go.transform, "AreaPerilla", Vector2.zero, new Vector2(anchoSlider, 32f));
        GameObject perilla = HackUI.NuevoUI(areaPerilla.transform, "Perilla", Vector2.zero, new Vector2(18f, 30f));
        Image imagenPerilla = perilla.AddComponent<Image>();
        cajas.Redondear(imagenPerilla, Color.clear, 4f);
        imagenPerilla.color = Color.white;
        RectTransform rtPerilla = (RectTransform)perilla.transform;
        rtPerilla.anchorMin = new Vector2(0f, 0.5f);
        rtPerilla.anchorMax = new Vector2(0f, 0.5f);

        Slider slider = go.AddComponent<Slider>();
        slider.fillRect = rtRelleno;
        slider.handleRect = rtPerilla;
        slider.targetGraphic = imagenPerilla;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = minimo;
        slider.maxValue = maximo;
        slider.value = valorInicial;
        slider.transition = Selectable.Transition.ColorTint;
        ColorBlock colores = slider.colors;
        colores.normalColor = new Color(0.75f, 0.9f, 1f);
        colores.highlightedColor = HackUI.ColAcento;
        colores.selectedColor = HackUI.ColAcento;
        colores.pressedColor = Color.white;
        slider.colors = colores;

        textoValor = HackUI.CrearTexto(fila, "Valor", "", 18, TextAnchor.MiddleRight,
                                       new Vector2(tamano.x / 2f - 60f, 0f), new Vector2(80f, 30f), HackUI.ColAcento);
        textoValor.fontStyle = FontStyle.Bold;

        ElementoMenuUI elemento = go.AddComponent<ElementoMenuUI>();
        elemento.Configurar(fila, 1.03f, bordeFila, ColorBordeBoton, HackUI.ColAcento, etiqueta, Color.white, HackUI.ColAcento);
        return slider;
    }

    public static void AplicarTinte(Selectable elemento)
    {
        elemento.transition = Selectable.Transition.ColorTint;
        ColorBlock colores = elemento.colors;
        colores.normalColor = ColorBoton;
        colores.highlightedColor = ColorBotonSeleccion;
        colores.selectedColor = ColorBotonSeleccion;
        colores.pressedColor = Color.Lerp(ColorBotonSeleccion, Color.white, 0.25f);
        colores.disabledColor = new Color(0.08f, 0.09f, 0.1f, 0.6f);
        colores.colorMultiplier = 1f;
        colores.fadeDuration = 0.06f;
        elemento.colors = colores;
    }

    /// <summary>Arriba / abajo recorre la lista (con vuelta). Izquierda / derecha quedan libres.</summary>
    public static void ConectarVertical(Selectable[] orden)
    {
        int cantidad = orden.Length;
        for (int i = 0; i < cantidad; i++)
        {
            if (orden[i] == null) continue;
            Navigation navegacion = new Navigation();
            navegacion.mode = Navigation.Mode.Explicit;
            navegacion.selectOnUp = Vecino(orden, i, -1);
            navegacion.selectOnDown = Vecino(orden, i, 1);
            orden[i].navigation = navegacion;
        }
    }

    private static Selectable Vecino(Selectable[] orden, int desde, int paso)
    {
        int indice = desde;
        for (int intentos = 0; intentos < orden.Length; intentos++)
        {
            indice = (indice + paso + orden.Length) % orden.Length;
            if (orden[indice] != null && indice != desde) return orden[indice];
        }
        return null;
    }

    public static void AgregarSombra(GameObject go)
    {
        Shadow sombra = go.GetComponent<Shadow>();
        if (sombra == null) sombra = go.AddComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.8f);
        sombra.effectDistance = new Vector2(2f, -2f);
    }
}
