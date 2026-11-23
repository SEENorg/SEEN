using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Minijuego: conectar cada cable de ORIGEN (izquierda) con su pareja de DESTINO (derecha).
///
/// Accesibilidad (daltonismo): cada cable tiene un NOMBRE (ALFA, BETA, GAMMA...) y el
/// nombre de su color escrito. Se puede resolver sin distinguir colores: basta con unir
/// los cables que tienen el mismo nombre.
///
/// Controles:
/// - Ratón: clic en un cable de origen y luego en su destino.
/// - Mando: cruceta o stick para moverse, A para elegir, B para cancelar el origen.
/// - Teclado: flechas para moverse, Enter o Espacio para elegir, Retroceso para cancelar.
/// Mientras eliges el destino, una línea de vista previa sale del cable de origen.
/// Si unes dos cables distintos, se marcan en rojo y el puzzle se reinicia con otro orden.
/// </summary>
public class PuzzleCables : IPuzzle
{
    // ── Medidas de la tarjeta (unidades del Canvas de 1920 x 1080) ──
    private const float AnchoTarjeta = 660f;
    private const float AltoTarjeta = 560f;
    private const float YTarjeta = 20f;
    private const float XColumna = 205f;
    private const float AnchoConector = 210f;
    private const float XPuerto = 88f;
    private const float TamanoPuerto = 18f;
    private const float ArribaFilas = 105f;
    private const float AltoZonaFilas = 280f;
    private const float AnchoProgreso = 360f;
    private const float DuracionError = 0.7f;
    private const float DuracionReinicio = 1.1f;   // tiempo en rojo antes de reiniciar
    private const float GrosorCable = 6f;
    private const float GrosorBrillo = 16f;

    private const string Origen = "I";
    private const string Destino = "D";
    private const string ColorTextoSecundario = "#8FA6B5";

    // ── Colores de la tarjeta (igual que PuzzleIngresarCodigo) ──────
    private readonly Color colorTarjeta = new Color(0.035f, 0.055f, 0.075f, 0.98f);
    private readonly Color colorBordeTarjeta = new Color(0.16f, 0.22f, 0.27f);
    private readonly Color colorCaja = new Color(0.06f, 0.13f, 0.18f);
    private readonly Color colorSubtitulo = new Color(0.55f, 0.65f, 0.72f);
    private readonly Color colorPie = new Color(0.50f, 0.58f, 0.65f);
    private readonly Color colorConector = new Color(0.055f, 0.15f, 0.21f);
    private readonly Color colorConectorSeleccion = new Color(0.08f, 0.22f, 0.30f);
    private readonly Color colorBordeConector = new Color(0.11f, 0.24f, 0.31f);
    private readonly Color colorSegmentoVacio = new Color(0.11f, 0.20f, 0.26f);

    // ── Cables: nombre, nombre del color y color (el nombre es lo que cuenta) ──
    private readonly string[] nombres = { "ALFA", "BETA", "GAMMA", "DELTA", "SIGMA", "OMEGA" };
    private readonly string[] nombresColor = { "ROJO", "AZUL", "AMARILLO", "ROSA", "VERDE", "MORADO" };
    private readonly Color[] colores =
    {
        new Color(1f, 0.2f, 0.2f),
        new Color(0.2f, 0.8f, 1f),
        new Color(1f, 0.85f, 0.15f),
        new Color(1f, 0.3f, 0.8f),
        new Color(0.4f, 1f, 0.3f),
        new Color(0.7f, 0.4f, 1f)
    };

    // ── Estado ──────────────────────────────────────────────────────
    private readonly IContextoHack ctx;
    private CajasRedondeadasUI cajas;
    private int cantidad;
    private int[] ordenDer;          // ordenDer[k] = cable que está en la fila k de la derecha
    private bool[] izqConectado;
    private bool[] derConectado;
    private int origen = -1;
    private int resueltos;

    private Button[] botonesIzq;
    private Button[] botonesDer;
    private TeclaCodigoUI[] teclasIzq;
    private TeclaCodigoUI[] teclasDer;
    private Text[] textosIzq;
    private Text[] textosDer;
    private Vector2[] puertosIzq;
    private Vector2[] puertosDer;
    private Image[] puntosIzq;

    private Transform lineas;
    private Image lineaPrevia;
    private Image brilloPrevia;
    private Text textoEstado;
    private Image bordeEstado;
    private Text textoProgreso;
    private Image[] segmentos;
    private GameObject ultimaSeleccion;

    private float tiempoError;
    private bool reiniciarTrasError;   // al fallar, el puzzle se reinicia cuando termina el aviso rojo
    private int errorIzq = -1;
    private int errorDer = -1;

    public PuzzleCables(IContextoHack ctx)
    {
        this.ctx = ctx;
    }

    // ── IPuzzle ─────────────────────────────────────────────────────

    public void Construir()
    {
        Transform contenido = ctx.Contenido;
        cajas = new CajasRedondeadasUI();
        cantidad = Mathf.Clamp(3 + ctx.DificultadBase + ctx.IndiceModulo, 3, 6);
        origen = -1;
        resueltos = 0;
        tiempoError = 0f;
        reiniciarTrasError = false;
        errorIzq = -1;
        errorDer = -1;

        // Orden aleatorio de la columna derecha (Fisher-Yates)
        ordenDer = new int[cantidad];
        for (int i = 0; i < cantidad; i++) ordenDer[i] = i;
        for (int i = cantidad - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temporal = ordenDer[i];
            ordenDer[i] = ordenDer[j];
            ordenDer[j] = temporal;
        }

        izqConectado = new bool[cantidad];
        derConectado = new bool[cantidad];
        botonesIzq = new Button[cantidad];
        botonesDer = new Button[cantidad];
        teclasIzq = new TeclaCodigoUI[cantidad];
        teclasDer = new TeclaCodigoUI[cantidad];
        textosIzq = new Text[cantidad];
        textosDer = new Text[cantidad];
        puertosIzq = new Vector2[cantidad];
        puertosDer = new Vector2[cantidad];
        puntosIzq = new Image[cantidad];

        // El puzzle dibuja su propio marco: se oculta el del hackeo.
        ctx.MostrarMarco(false);

        Image bordeTarjeta = cajas.CrearCaja(contenido, "TarjetaCables", new Vector2(0f, YTarjeta),
                                             new Vector2(AnchoTarjeta, AltoTarjeta),
                                             colorTarjeta, colorBordeTarjeta, 1f);
        Transform tarjeta = bordeTarjeta.transform.parent;

        ConstruirEncabezado(tarjeta);

        // Las líneas van primero para quedar detrás de los conectores
        lineas = HackUI.NuevoUI(tarjeta, "Lineas", Vector2.zero, new Vector2(AnchoTarjeta, AltoTarjeta)).transform;
        brilloPrevia = CrearLinea(lineas, "BrilloPrevia", Vector2.zero, Vector2.zero, GrosorBrillo, Color.clear);
        lineaPrevia = CrearLinea(lineas, "LineaPrevia", Vector2.zero, Vector2.zero, GrosorCable, Color.clear);
        brilloPrevia.enabled = false;
        lineaPrevia.enabled = false;

        ConstruirColumnas(tarjeta);
        ConstruirProgreso(tarjeta);

        // Pie
        string textoModulo = ctx.TextoModulo;
        if (string.IsNullOrEmpty(textoModulo)) textoModulo = "MÓDULO DE ENLACE";
        HackUI.CrearTexto(tarjeta, "Estado", "• SISTEMA ACTIVO", 14, TextAnchor.MiddleLeft,
                          new Vector2(-150f, -255f), new Vector2(300f, 22f), HackUI.ColAcento);
        HackUI.CrearTexto(tarjeta, "Modulo", textoModulo, 14, TextAnchor.MiddleRight,
                          new Vector2(150f, -255f), new Vector2(300f, 22f), colorPie);

        MostrarEstadoNormal();
        ActualizarProgreso();

        ultimaSeleccion = botonesIzq[0].gameObject;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(ultimaSeleccion);
    }

    public void Actualizar()
    {
        float dt = Time.unscaledDeltaTime;

        if (tiempoError > 0f)
        {
            tiempoError -= dt;
            if (tiempoError <= 0f)
            {
                if (reiniciarTrasError)
                {
                    // Vuelve a armar el puzzle con otro orden (destruye esta tarjeta)
                    reiniciarTrasError = false;
                    ctx.Regenerar();
                    return;
                }
                TerminarError();
            }
        }

        MantenerSeleccion();
        ActualizarVistaPrevia();

        if (ctx.InputBloqueado || ctx.EnTransicion) return;
        LeerCancelar();
    }

    public void Detener()
    {
        ctx.MostrarMarco(true);

        if (cajas != null) cajas.Liberar();
        cajas = null;
        botonesIzq = null;
        botonesDer = null;
        teclasIzq = null;
        teclasDer = null;
        lineaPrevia = null;
        brilloPrevia = null;
        textoEstado = null;
        bordeEstado = null;
        ultimaSeleccion = null;
    }

    // ── Construcción ────────────────────────────────────────────────

    private void ConstruirEncabezado(Transform tarjeta)
    {
        string textoTitulo = ctx.TextoAcceso;
        if (string.IsNullOrEmpty(textoTitulo)) textoTitulo = "SEEN // ENLACE DE RED";
        Text titulo = HackUI.CrearTexto(tarjeta, "Titulo", textoTitulo, 28, TextAnchor.MiddleCenter,
                                        new Vector2(0f, 245f), new Vector2(620f, 36f), HackUI.ColAcento);
        titulo.fontStyle = FontStyle.Bold;

        HackUI.CrearTexto(tarjeta, "Subtitulo", "ENRUTAMIENTO DE SEÑAL: UNE CADA CABLE CON SU MISMO NOMBRE", 14,
                          TextAnchor.MiddleCenter, new Vector2(0f, 215f), new Vector2(620f, 22f), colorSubtitulo);

        Color bordeCeleste = HackUI.ColAcento;
        bordeCeleste.a = 0.85f;
        bordeEstado = cajas.CrearCaja(tarjeta, "CajaEstado", new Vector2(0f, 168f), new Vector2(600f, 46f),
                                      colorCaja, bordeCeleste, 1.2f);
        textoEstado = HackUI.CrearTexto(bordeEstado.transform.parent, "TextoEstado", "", 17,
                                        TextAnchor.MiddleCenter, Vector2.zero, new Vector2(580f, 40f),
                                        HackUI.ColAcento);
        textoEstado.fontStyle = FontStyle.Bold;

        HackUI.CrearTexto(tarjeta, "TituloOrigen", "ORIGEN", 13, TextAnchor.MiddleCenter,
                          new Vector2(-XColumna, 127f), new Vector2(AnchoConector, 20f), colorSubtitulo);
        HackUI.CrearTexto(tarjeta, "TituloDestino", "DESTINO", 13, TextAnchor.MiddleCenter,
                          new Vector2(XColumna, 127f), new Vector2(AnchoConector, 20f), colorSubtitulo);
    }

    private void ConstruirColumnas(Transform tarjeta)
    {
        float paso = Mathf.Min(70f, AltoZonaFilas / cantidad);
        float altoConector = Mathf.Clamp(paso - 14f, 34f, 52f);

        for (int fila = 0; fila < cantidad; fila++)
        {
            float y = ArribaFilas - paso * (fila + 0.5f);

            // Izquierda: el cable "fila" está en la fila "fila"
            int cableIzq = fila;
            puertosIzq[cableIzq] = new Vector2(-XPuerto, y);
            botonesIzq[cableIzq] = CrearConector(tarjeta, true, fila, cableIzq, y, altoConector);
            puntosIzq[cableIzq] = CrearPuerto(tarjeta, "PuertoIzq_" + fila, puertosIzq[cableIzq], cableIzq);

            // Derecha: en la fila "fila" está el cable ordenDer[fila]
            int cableDer = ordenDer[fila];
            puertosDer[fila] = new Vector2(XPuerto, y);
            botonesDer[fila] = CrearConector(tarjeta, false, fila, cableDer, y, altoConector);
            CrearPuerto(tarjeta, "PuertoDer_" + fila, puertosDer[fila], cableDer);
        }
    }

    /// <summary>Crea un conector: caja con franja de color, nombre del cable y nombre del color.</summary>
    private Button CrearConector(Transform tarjeta, bool esOrigen, int fila, int cable,
                                 float y, float alto)
    {
        float x = XColumna;
        string prefijo = Destino;
        if (esOrigen)
        {
            x = -XColumna;
            prefijo = Origen;
        }

        Image borde = cajas.CrearCaja(tarjeta, "Conector" + prefijo + "_" + fila, new Vector2(x, y),
                                      new Vector2(AnchoConector, alto), Color.white, colorBordeConector, 1.5f);
        GameObject go = borde.transform.parent.gameObject;
        Image fondo = go.GetComponent<Image>();

        Button boton = go.AddComponent<Button>();
        boton.targetGraphic = fondo;
        boton.transition = Selectable.Transition.ColorTint;
        ColorBlock bloque = boton.colors;
        bloque.normalColor = colorConector;
        bloque.highlightedColor = colorConectorSeleccion;
        bloque.selectedColor = colorConectorSeleccion;
        bloque.pressedColor = Color.Lerp(colorConectorSeleccion, Color.white, 0.25f);
        // "Deshabilitado" = conectado: el fondo toma un poco del color del cable
        bloque.disabledColor = Color.Lerp(colorConector, colores[cable], 0.3f);
        bloque.colorMultiplier = 1f;
        bloque.fadeDuration = 0.06f;
        boton.colors = bloque;

        Navigation navegacion = boton.navigation;
        navegacion.mode = Navigation.Mode.Automatic;
        boton.navigation = navegacion;

        // Franja de color en el lado de afuera
        float xFranja = -AnchoConector / 2f + 12f;
        float xTexto = 8f;
        if (!esOrigen)
        {
            xFranja = AnchoConector / 2f - 12f;
            xTexto = -8f;
        }
        cajas.CrearRelleno(go.transform, "Franja", new Vector2(xFranja, 0f), new Vector2(8f, alto - 14f),
                           colores[cable], 4f);

        Text texto = HackUI.CrearTexto(go.transform, "Nombre", EtiquetaCable(cable, ""), 19,
                                       TextAnchor.MiddleCenter, new Vector2(xTexto, 0f),
                                       new Vector2(AnchoConector - 30f, alto), Color.white);
        texto.supportRichText = true;

        TeclaCodigoUI tecla = go.AddComponent<TeclaCodigoUI>();
        tecla.Configurar(prefijo + fila, borde, colorBordeConector, HackUI.ColAcento, PulsarConector);
        boton.onClick.AddListener(tecla.Presionar);

        if (esOrigen)
        {
            teclasIzq[cable] = tecla;
            textosIzq[cable] = texto;
        }
        else
        {
            teclasDer[fila] = tecla;
            textosDer[fila] = texto;
        }
        return boton;
    }

    private Image CrearPuerto(Transform tarjeta, string nombre, Vector2 posicion, int cable)
    {
        return cajas.CrearRelleno(tarjeta, nombre, posicion, new Vector2(TamanoPuerto, TamanoPuerto),
                                  colores[cable], 1.8f);
    }

    private void ConstruirProgreso(Transform tarjeta)
    {
        textoProgreso = HackUI.CrearTexto(tarjeta, "Progreso", "", 14, TextAnchor.MiddleCenter,
                                          new Vector2(0f, -200f), new Vector2(400f, 22f), colorSubtitulo);

        segmentos = new Image[cantidad];
        float separacion = 6f;
        float anchoSegmento = (AnchoProgreso - separacion * (cantidad - 1)) / cantidad;
        float inicio = -AnchoProgreso / 2f + anchoSegmento / 2f;
        for (int i = 0; i < cantidad; i++)
        {
            float x = inicio + i * (anchoSegmento + separacion);
            segmentos[i] = cajas.CrearRelleno(tarjeta, "Segmento_" + i, new Vector2(x, -222f),
                                              new Vector2(anchoSegmento, 8f), colorSegmentoVacio, 4f);
        }
    }

    // ── Acciones ────────────────────────────────────────────────────

    /// <summary>Lo llaman los conectores. valor = "I2" (origen fila 2) o "D0" (destino fila 0).</summary>
    private void PulsarConector(string valor)
    {
        if (ctx.InputBloqueado || ctx.EnTransicion) return;
        if (reiniciarTrasError) return;   // ya falló: se espera el reinicio
        if (string.IsNullOrEmpty(valor) || valor.Length < 2) return;

        string lado = valor.Substring(0, 1);
        int fila = int.Parse(valor.Substring(1));

        if (lado == Origen) ElegirOrigen(fila);
        else ElegirDestino(fila);
    }

    private void ElegirOrigen(int cable)
    {
        if (izqConectado[cable]) return;
        if (tiempoError > 0f) TerminarError();

        // Si había otro origen, vuelve a su aspecto normal
        if (origen >= 0 && origen != cable) MarcarOrigen(origen, false);

        origen = cable;
        MarcarOrigen(origen, true);
        MostrarEstado("ORIGEN " + nombres[origen] + ": ELIGE SU DESTINO", HackUI.ColAcento);

        // Lleva la selección a la columna de destino, en la misma fila si se puede
        int destino = DestinoLibreMasCercano(cable);
        if (destino >= 0) Seleccionar(botonesDer[destino].gameObject);
    }

    private void ElegirDestino(int fila)
    {
        if (derConectado[fila]) return;

        if (origen < 0)
        {
            MostrarEstado("PRIMERO ELIGE UN CABLE DE ORIGEN", HackUI.ColRojo);
            tiempoError = DuracionError;
            return;
        }

        int cableDestino = ordenDer[fila];
        if (cableDestino == origen)
        {
            Conectar(origen, fila);
            return;
        }

        // Error: los dos se marcan en rojo un momento y luego el puzzle se reinicia
        MostrarEstado(nombres[origen] + " NO COINCIDE CON " + nombres[cableDestino] + ": REINICIANDO",
                      HackUI.ColRojo);
        reiniciarTrasError = true;
        errorIzq = origen;
        errorDer = fila;
        teclasIzq[errorIzq].CambiarColoresBorde(HackUI.ColRojo, HackUI.ColRojo);
        teclasDer[errorDer].CambiarColoresBorde(HackUI.ColRojo, HackUI.ColRojo);
        tiempoError = DuracionReinicio;
        lineaPrevia.enabled = false;
        brilloPrevia.enabled = false;

        int origenFallido = origen;
        MarcarOrigen(origen, false);
        origen = -1;
        // MarcarOrigen restauró el borde: se vuelve a poner rojo mientras dura el error
        teclasIzq[errorIzq].CambiarColoresBorde(HackUI.ColRojo, HackUI.ColRojo);
        Seleccionar(botonesIzq[origenFallido].gameObject);
    }

    private void Conectar(int cable, int fila)
    {
        izqConectado[cable] = true;
        derConectado[fila] = true;
        origen = -1;
        resueltos++;

        // Cable definitivo: un brillo ancho y una línea fina encima
        Color brillo = colores[cable];
        brillo.a = 0.25f;
        CrearLinea(lineas, "Brillo_" + nombres[cable], puertosIzq[cable], puertosDer[fila], GrosorBrillo, brillo);
        CrearLinea(lineas, "Cable_" + nombres[cable], puertosIzq[cable], puertosDer[fila], GrosorCable, colores[cable]);
        lineaPrevia.enabled = false;
        brilloPrevia.enabled = false;
        puntosIzq[cable].transform.localScale = Vector3.one;

        // Los dos conectores quedan fijos, con borde del color del cable y la marca ENLAZADO
        teclasIzq[cable].CambiarColoresBorde(colores[cable], colores[cable]);
        teclasDer[fila].CambiarColoresBorde(colores[cable], colores[cable]);
        textosIzq[cable].text = EtiquetaCable(cable, "ENLAZADO");
        textosDer[fila].text = EtiquetaCable(cable, "ENLAZADO");
        botonesIzq[cable].interactable = false;
        botonesDer[fila].interactable = false;

        ActualizarProgreso();

        if (resueltos >= cantidad)
        {
            MostrarEstado("TODOS LOS ENLACES ESTABLECIDOS", HackUI.ColVerde);
            bordeEstado.color = HackUI.ColVerde;
            tiempoError = 0f;
            ctx.CompletarModulo();
            return;
        }

        MostrarEstado("ENLACE " + nombres[cable] + " ESTABLECIDO", HackUI.ColVerde);
        tiempoError = DuracionError;

        int siguiente = OrigenLibre();
        if (siguiente >= 0) Seleccionar(botonesIzq[siguiente].gameObject);
    }

    private void CancelarOrigen()
    {
        if (origen < 0 || reiniciarTrasError) return;
        int anterior = origen;
        MarcarOrigen(origen, false);
        origen = -1;
        MostrarEstadoNormal();
        Seleccionar(botonesIzq[anterior].gameObject);
    }

    // ── Aspecto ─────────────────────────────────────────────────────

    /// <summary>Un origen elegido muestra "&gt;" delante y el borde del color de su cable.</summary>
    private void MarcarOrigen(int cable, bool elegido)
    {
        if (elegido)
        {
            teclasIzq[cable].CambiarColoresBorde(colores[cable], HackUI.ColAcento);
            textosIzq[cable].text = "<b>> </b>" + EtiquetaCable(cable, "");
        }
        else
        {
            teclasIzq[cable].CambiarColoresBorde(colorBordeConector, HackUI.ColAcento);
            textosIzq[cable].text = EtiquetaCable(cable, "");
            puntosIzq[cable].transform.localScale = Vector3.one;   // deja de latir
        }
    }

    /// <summary>"ALFA  ROJO" (o "ALFA  ENLAZADO"): el nombre en grande y el detalle en pequeño.</summary>
    private string EtiquetaCable(int cable, string detalle)
    {
        string segundo = nombresColor[cable];
        if (!string.IsNullOrEmpty(detalle)) segundo = detalle;
        return "<b>" + nombres[cable] + "</b>  <size=12><color=" + ColorTextoSecundario + ">"
             + segundo + "</color></size>";
    }

    private void ActualizarProgreso()
    {
        if (textoProgreso != null) textoProgreso.text = "ENLACES " + resueltos + "/" + cantidad;
        if (segmentos == null) return;
        for (int i = 0; i < segmentos.Length; i++)
        {
            if (i < resueltos) segmentos[i].color = HackUI.ColAcento;
            else segmentos[i].color = colorSegmentoVacio;
        }
    }

    private void MostrarEstado(string mensaje, Color color)
    {
        if (textoEstado != null)
        {
            textoEstado.text = mensaje;
            textoEstado.color = color;
        }
        if (bordeEstado != null) bordeEstado.color = color;
    }

    private void MostrarEstadoNormal()
    {
        Color bordeCeleste = HackUI.ColAcento;
        bordeCeleste.a = 0.85f;
        if (origen >= 0)
        {
            MostrarEstado("ORIGEN " + nombres[origen] + ": ELIGE SU DESTINO", HackUI.ColAcento);
        }
        else
        {
            MostrarEstado("ELIGE UN CABLE DE ORIGEN", HackUI.ColAcento);
        }
        if (bordeEstado != null) bordeEstado.color = bordeCeleste;
    }

    private void TerminarError()
    {
        tiempoError = 0f;
        if (errorIzq >= 0 && !izqConectado[errorIzq])
        {
            if (errorIzq == origen) MarcarOrigen(errorIzq, true);
            else MarcarOrigen(errorIzq, false);
        }
        if (errorDer >= 0 && !derConectado[errorDer])
            teclasDer[errorDer].CambiarColoresBorde(colorBordeConector, HackUI.ColAcento);
        errorIzq = -1;
        errorDer = -1;
        if (resueltos < cantidad) MostrarEstadoNormal();
    }

    /// <summary>Línea de vista previa entre el origen elegido y el destino seleccionado.</summary>
    private void ActualizarVistaPrevia()
    {
        if (lineaPrevia == null) return;

        int filaDestino = DestinoSeleccionado();
        if (origen < 0 || filaDestino < 0)
        {
            lineaPrevia.enabled = false;
            brilloPrevia.enabled = false;
            if (origen >= 0) Latir(puntosIzq[origen]);
            return;
        }

        float pulso = 0.55f + 0.35f * Mathf.Sin(Time.unscaledTime * 8f);
        Color color = colores[origen];
        color.a = pulso;
        Color brillo = colores[origen];
        brillo.a = pulso * 0.3f;

        lineaPrevia.enabled = true;
        brilloPrevia.enabled = true;
        lineaPrevia.color = color;
        brilloPrevia.color = brillo;
        ColocarLinea((RectTransform)lineaPrevia.transform, puertosIzq[origen], puertosDer[filaDestino], GrosorCable);
        ColocarLinea((RectTransform)brilloPrevia.transform, puertosIzq[origen], puertosDer[filaDestino], GrosorBrillo);
        Latir(puntosIzq[origen]);
    }

    /// <summary>El puerto del origen elegido late para que se vea de dónde sale el cable.</summary>
    private void Latir(Image puerto)
    {
        if (puerto == null) return;
        float escala = 1f + 0.25f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f));
        puerto.transform.localScale = Vector3.one * escala;
    }

    // ── Líneas ──────────────────────────────────────────────────────

    private Image CrearLinea(Transform padre, string nombre, Vector2 desde, Vector2 hasta, float grosor, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        Image imagen = go.AddComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;
        ColocarLinea((RectTransform)go.transform, desde, hasta, grosor);
        return imagen;
    }

    private void ColocarLinea(RectTransform rt, Vector2 desde, Vector2 hasta, float grosor)
    {
        Vector2 delta = hasta - desde;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(delta.magnitude, grosor);
        rt.anchoredPosition = (desde + hasta) / 2f;
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    // ── Entrada y selección ─────────────────────────────────────────

    /// <summary>B del mando o Retroceso: cancela el origen elegido.</summary>
    private void LeerCancelar()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad mando = Gamepad.current;
        if (mando != null && mando.buttonEast.wasPressedThisFrame) CancelarOrigen();
        Keyboard teclado = Keyboard.current;
        if (teclado != null && teclado.backspaceKey.wasPressedThisFrame) CancelarOrigen();
#else
        if (Input.GetKeyDown(KeyCode.Backspace)) CancelarOrigen();
#endif
    }

    /// <summary>Si un clic fuera deja sin selección, vuelve a la última (para el mando).</summary>
    private void MantenerSeleccion()
    {
        if (EventSystem.current == null || botonesIzq == null) return;

        GameObject actual = EventSystem.current.currentSelectedGameObject;
        if (actual != null && EsConectorPropio(actual))
        {
            ultimaSeleccion = actual;
            return;
        }

        if (actual == null && ultimaSeleccion != null)
        {
            Button boton = ultimaSeleccion.GetComponent<Button>();
            if (boton != null && boton.interactable) EventSystem.current.SetSelectedGameObject(ultimaSeleccion);
            else
            {
                int libre = OrigenLibre();
                if (libre >= 0) EventSystem.current.SetSelectedGameObject(botonesIzq[libre].gameObject);
            }
        }
    }

    private bool EsConectorPropio(GameObject objeto)
    {
        for (int i = 0; i < cantidad; i++)
        {
            if (botonesIzq[i] != null && botonesIzq[i].gameObject == objeto) return true;
            if (botonesDer[i] != null && botonesDer[i].gameObject == objeto) return true;
        }
        return false;
    }

    /// <summary>Fila de destino seleccionada ahora mismo (o -1).</summary>
    private int DestinoSeleccionado()
    {
        if (EventSystem.current == null || botonesDer == null) return -1;
        GameObject actual = EventSystem.current.currentSelectedGameObject;
        if (actual == null) return -1;
        for (int i = 0; i < cantidad; i++)
        {
            if (botonesDer[i] != null && botonesDer[i].gameObject == actual && !derConectado[i]) return i;
        }
        return -1;
    }

    private int DestinoLibreMasCercano(int fila)
    {
        int mejor = -1;
        int mejorDistancia = int.MaxValue;
        for (int i = 0; i < cantidad; i++)
        {
            if (derConectado[i]) continue;
            int distancia = Mathf.Abs(i - fila);
            if (distancia < mejorDistancia)
            {
                mejorDistancia = distancia;
                mejor = i;
            }
        }
        return mejor;
    }

    private int OrigenLibre()
    {
        for (int i = 0; i < cantidad; i++)
        {
            if (!izqConectado[i]) return i;
        }
        return -1;
    }

    private void Seleccionar(GameObject objeto)
    {
        ultimaSeleccion = objeto;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(objeto);
    }
}
