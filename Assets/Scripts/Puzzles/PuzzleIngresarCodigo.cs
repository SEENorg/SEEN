using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Minijuego: pide el código que muestra la pantalla de la sala (PuzzleCodigo).
/// Se ve como una tarjeta "SEEN // SECURITY ACCESS" con un teclado numérico de 4 x 3.
///
/// Controles:
/// - Mando: cruceta o stick para moverse, A para pulsar, B para borrar, Y para salir.
/// - Teclado: flechas para moverse, Enter o Espacio para pulsar; también se pueden
///   escribir los números directamente y borrar con Retroceso.
/// - Ratón: pasar por encima ilumina la tecla y el clic la pulsa.
/// La tecla seleccionada siempre se ilumina con un borde celeste.
/// </summary>
public class PuzzleIngresarCodigo : IPuzzle
{
    // ── Medidas (unidades del Canvas de 1920 x 1080) ──────────────
    private const float AnchoTarjeta = 460f;
    private const float AltoTarjeta = 450f;
    private const float AnchoTecla = 132f;
    private const float AltoTecla = 54f;
    private const float Separacion = 12f;
    private const float YPrimeraFila = 28f;
    private const int Columnas = 3;
    private const int Filas = 4;
    private const int IndiceBorrar = 9;
    private const int IndiceVerificar = 11;
    private const float DuracionAviso = 1.2f;
    // Tamaño de la tarjeta: 1 = tamaño base, 1.2 = 20 % más grande.
    private const float Escala = 1.2f;
    // Posición vertical de la tarjeta dentro del panel del hackeo.
    private const float YTarjeta = 20f;

    private const string ValorBorrar = "BORRAR";
    private const string ValorVerificar = "VERIFICAR";
    private const string ColorHueco = "#2E4A5C";

    // ── Colores ───────────────────────────────────────────────────
    private readonly Color colorTarjeta = new Color(0.035f, 0.055f, 0.075f, 0.98f);
    private readonly Color colorBordeTarjeta = new Color(0.16f, 0.22f, 0.27f);
    private readonly Color colorPantalla = new Color(0.06f, 0.13f, 0.18f);
    private readonly Color colorSubtitulo = new Color(0.55f, 0.65f, 0.72f);
    private readonly Color colorPie = new Color(0.50f, 0.58f, 0.65f);

    private readonly Color colorTecla = new Color(0.055f, 0.15f, 0.21f);
    private readonly Color colorTeclaSeleccion = new Color(0.08f, 0.22f, 0.30f);
    private readonly Color colorBordeTecla = new Color(0.11f, 0.24f, 0.31f);

    private readonly Color colorBorrar = new Color(0.34f, 0.12f, 0.16f);
    private readonly Color colorBorrarSeleccion = new Color(0.45f, 0.16f, 0.21f);
    private readonly Color colorBordeBorrar = new Color(0.62f, 0.22f, 0.28f);

    private readonly Color colorVerificar = new Color(0.05f, 0.36f, 0.26f);
    private readonly Color colorVerificarSeleccion = new Color(0.07f, 0.47f, 0.34f);
    private readonly Color colorBordeVerificar = new Color(0.18f, 0.72f, 0.50f);

    // ── Estado ────────────────────────────────────────────────────
    private readonly IContextoHack ctx;
    private IPuzzleCodigo pantalla;
    private string ingresado = "";
    private int largoMaximo = 8;
    private float tiempoAviso;

    private Text textoPuntos;
    private Text textoAviso;
    private Image bordePantalla;
    private Button[] teclas;
    private GameObject ultimaSeleccion;

    private CajasRedondeadasUI cajas;   // crea las cajas con esquinas redondeadas

    public PuzzleIngresarCodigo(IContextoHack ctx)
    {
        this.ctx = ctx;
    }

    // ── IPuzzle ───────────────────────────────────────────────────

    public void Construir()
    {
        Transform contenido = ctx.Contenido;
        cajas = new CajasRedondeadasUI();

        pantalla = BuscarPantallaCodigo();
        largoMaximo = 8;
        if (pantalla != null && !string.IsNullOrEmpty(pantalla.Codigo)) largoMaximo = pantalla.Codigo.Length;
        ingresado = "";
        tiempoAviso = 0f;

        // Tarjeta de fondo
        // El puzzle dibuja su propio marco: se oculta el del hackeo (fondo, borde azul,
        // título, contador y mensaje de abajo). Los mensajes de correcto o incorrecto
        // salen dentro de la caja del código. Detener() lo vuelve a mostrar para los otros módulos.
        ctx.MostrarMarco(false);

        Image bordeTarjeta = cajas.CrearCaja(contenido, "TarjetaAcceso", new Vector2(0f, YTarjeta),
                                       V(AnchoTarjeta, AltoTarjeta),
                                       colorTarjeta, colorBordeTarjeta, 1f / Escala);
        Transform tarjeta = bordeTarjeta.transform.parent;

        // Encabezado
        // Título: el nombre del acceso que da el hackeo (ej: "ACCESO: PUERTA 1")
        string textoTitulo = ctx.TextoAcceso;
        if (string.IsNullOrEmpty(textoTitulo)) textoTitulo = "SEEN // SECURITY ACCESS";
        Text titulo = Texto(tarjeta, "Titulo", textoTitulo, 26,
                            TextAnchor.MiddleCenter, V(0f, 192f),
                            V(440f, 34f), HackUI.ColAcento);
        titulo.fontStyle = FontStyle.Bold;

        Texto(tarjeta, "Subtitulo", "AUTENTICACIÓN REQUERIDA", 14,
              TextAnchor.MiddleCenter, V(0f, 164f),
              V(440f, 22f), colorSubtitulo);

        // Pantalla del código
        Color bordeCeleste = HackUI.ColAcento;
        bordeCeleste.a = 0.85f;
        bordePantalla = cajas.CrearCaja(tarjeta, "PantallaCodigo", V(0f, 108f),
                                  V(420f, 84f), colorPantalla, bordeCeleste, 1.2f / Escala);
        Transform cajaPantalla = bordePantalla.transform.parent;

        textoPuntos = Texto(cajaPantalla, "Puntos", "", 36, TextAnchor.MiddleCenter,
                            V(0f, 12f), V(400f, 44f), Color.white);
        textoPuntos.fontStyle = FontStyle.Bold;
        textoPuntos.supportRichText = true;

        textoAviso = Texto(cajaPantalla, "Aviso", "", 14, TextAnchor.MiddleCenter,
                           V(0f, -24f), V(400f, 20f), HackUI.ColAcento);

        // Teclado numérico
        string[] etiquetas =
        {
            "1", "2", "3",
            "4", "5", "6",
            "7", "8", "9",
            ValorBorrar, "0", ValorVerificar
        };

        teclas = new Button[etiquetas.Length];
        for (int i = 0; i < etiquetas.Length; i++)
        {
            teclas[i] = CrearTecla(tarjeta, i, etiquetas[i]);
        }
        ConectarNavegacion();

        // Pie de la tarjeta
        // Pie: estado a la izquierda y cuántos módulos van (ej: "MÓDULO 1/3") a la derecha
        string textoModulo = ctx.TextoModulo;
        if (string.IsNullOrEmpty(textoModulo)) textoModulo = "MÓDULO DE ACCESO";
        Texto(tarjeta, "Estado", "• SISTEMA ACTIVO", 13, TextAnchor.MiddleLeft,
              V(-105f, -212f), V(210f, 20f), HackUI.ColAcento);
        Texto(tarjeta, "Modulo", textoModulo, 13, TextAnchor.MiddleRight,
              V(105f, -212f), V(210f, 20f), colorPie);

        RestaurarAviso();
        ActualizarPuntos();

        // Empieza con la tecla 1 seleccionada (iluminada)
        ultimaSeleccion = teclas[0].gameObject;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(ultimaSeleccion);
    }

    public void Actualizar()
    {
        if (tiempoAviso > 0f)
        {
            tiempoAviso -= Time.unscaledDeltaTime;
            if (tiempoAviso <= 0f) RestaurarAviso();
        }

        MantenerSeleccion();

        if (ctx.InputBloqueado || ctx.EnTransicion) return;
        LeerTecladoFisico();
        LeerMando();
    }

    public void Detener()
    {
        // Devuelve el marco del hackeo para el siguiente módulo
        ctx.MostrarMarco(true);

        teclas = null;
        textoPuntos = null;
        textoAviso = null;
        bordePantalla = null;
        ultimaSeleccion = null;

        if (cajas != null) cajas.Liberar();
        cajas = null;
    }

    // ── Acciones ──────────────────────────────────────────────────

    /// <summary>Lo llaman las teclas en pantalla al pulsarse.</summary>
    private void PulsarTecla(string valor)
    {
        if (ctx.InputBloqueado || ctx.EnTransicion) return;

        if (valor == ValorBorrar) BorrarNumero();
        else if (valor == ValorVerificar) Verificar();
        else AgregarNumero(valor);
    }

    private void AgregarNumero(string numero)
    {
        if (ingresado.Length >= largoMaximo) return;
        ingresado += numero;
        ActualizarPuntos();
    }

    private void BorrarNumero()
    {
        if (ingresado.Length == 0) return;
        ingresado = ingresado.Substring(0, ingresado.Length - 1);
        ActualizarPuntos();
    }

    private void Verificar()
    {
        if (pantalla == null) pantalla = BuscarPantallaCodigo();
        if (pantalla == null)
        {
            MostrarAviso("SIN PANTALLA DE CÓDIGO", HackUI.ColRojo);
            return;
        }

        if (ingresado == pantalla.Codigo)
        {
            MostrarAviso("ACCESO CONCEDIDO", HackUI.ColVerde);
            tiempoAviso = 0f;   // el mensaje verde se queda hasta que se cierre el módulo
            ctx.CompletarModulo();
        }
        else
        {
            MostrarAviso("CÓDIGO INCORRECTO", HackUI.ColRojo);
            ingresado = "";
            ActualizarPuntos();
        }
    }

    // ── Entrada directa (además de las teclas en pantalla) ────────

    private void LeerTecladoFisico()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        // Fila de números (1 a 9 son seguidos en el enum Key; el 0 va aparte)
        for (int i = 1; i <= 9; i++)
        {
            Key tecla = (Key)((int)Key.Digit1 + i - 1);
            if (teclado[tecla].wasPressedThisFrame) AgregarNumero(i.ToString());
        }
        if (teclado.digit0Key.wasPressedThisFrame) AgregarNumero("0");

        // Teclado numérico (Numpad0 a Numpad9 son seguidos)
        for (int i = 0; i <= 9; i++)
        {
            Key tecla = (Key)((int)Key.Numpad0 + i);
            if (teclado[tecla].wasPressedThisFrame) AgregarNumero(i.ToString());
        }

        if (teclado.backspaceKey.wasPressedThisFrame) BorrarNumero();
        // Enter no se lee aquí: ya pulsa la tecla seleccionada (lo hace el EventSystem).
#else
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i)) AgregarNumero(i.ToString());
            if (Input.GetKeyDown(KeyCode.Keypad0 + i)) AgregarNumero(i.ToString());
        }
        if (Input.GetKeyDown(KeyCode.Backspace)) BorrarNumero();
#endif
    }

    private void LeerMando()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad mando = Gamepad.current;
        if (mando == null) return;

        // A ya pulsa la tecla seleccionada (lo hace el EventSystem). Atajo extra:
        // (Y no se usa aquí: HackManager lo usa para salir del hackeo)
        if (mando.buttonEast.wasPressedThisFrame) BorrarNumero();   // B
#endif
    }

    /// <summary>
    /// Si un clic fuera de las teclas deja sin selección, se vuelve a seleccionar
    /// la última tecla. Así el mando siempre tiene una tecla iluminada desde donde moverse.
    /// </summary>
    private void MantenerSeleccion()
    {
        if (EventSystem.current == null || teclas == null) return;

        GameObject actual = EventSystem.current.currentSelectedGameObject;
        if (actual != null && EsTeclaPropia(actual))
        {
            ultimaSeleccion = actual;
            return;
        }

        if (actual == null && ultimaSeleccion != null)
            EventSystem.current.SetSelectedGameObject(ultimaSeleccion);
    }

    private bool EsTeclaPropia(GameObject objeto)
    {
        for (int i = 0; i < teclas.Length; i++)
        {
            if (teclas[i] != null && teclas[i].gameObject == objeto) return true;
        }
        return false;
    }

    // ── Pantalla del código ───────────────────────────────────────

    /// <summary>Muestra un punto por número escrito y un guion bajo por cada hueco.</summary>
    private void ActualizarPuntos()
    {
        if (textoPuntos == null) return;

        string texto = "";
        for (int i = 0; i < largoMaximo; i++)
        {
            if (i > 0) texto += " ";
            if (i < ingresado.Length) texto += "•";
            else texto += "<color=" + ColorHueco + ">_</color>";
        }
        textoPuntos.text = texto;
    }

    private void MostrarAviso(string mensaje, Color color)
    {
        if (textoAviso != null)
        {
            textoAviso.text = mensaje;
            textoAviso.color = color;
        }
        if (bordePantalla != null) bordePantalla.color = color;
        tiempoAviso = DuracionAviso;
    }

    private void RestaurarAviso()
    {
        tiempoAviso = 0f;
        if (textoAviso != null)
        {
            textoAviso.text = "INTRODUCE TU CÓDIGO";
            textoAviso.color = HackUI.ColAcento;
        }
        if (bordePantalla != null)
        {
            Color bordeCeleste = HackUI.ColAcento;
            bordeCeleste.a = 0.85f;
            bordePantalla.color = bordeCeleste;
        }
    }

    // ── Construcción de las teclas ────────────────────────────────

    private Button CrearTecla(Transform padre, int indice, string etiqueta)
    {
        int columna = indice % Columnas;
        int fila = indice / Columnas;
        Vector2 posicion = V((columna - 1) * (AnchoTecla + Separacion),
                             YPrimeraFila - fila * (AltoTecla + Separacion));

        Color relleno = colorTecla;
        Color rellenoSeleccion = colorTeclaSeleccion;
        Color borde = colorBordeTecla;
        int tamanoLetra = 24;

        if (indice == IndiceBorrar)
        {
            relleno = colorBorrar;
            rellenoSeleccion = colorBorrarSeleccion;
            borde = colorBordeBorrar;
            tamanoLetra = 18;
        }
        else if (indice == IndiceVerificar)
        {
            relleno = colorVerificar;
            rellenoSeleccion = colorVerificarSeleccion;
            borde = colorBordeVerificar;
            tamanoLetra = 18;
        }

        Image bordeTecla = cajas.CrearCaja(padre, "Tecla_" + etiqueta, posicion,
                                     V(AnchoTecla, AltoTecla), Color.white, borde, 1.5f / Escala);
        GameObject go = bordeTecla.transform.parent.gameObject;
        Image fondo = go.GetComponent<Image>();

        // El color final sale del tinte del Button (el sprite es blanco).
        Button boton = go.AddComponent<Button>();
        boton.targetGraphic = fondo;
        boton.transition = Selectable.Transition.ColorTint;
        ColorBlock colores = boton.colors;
        colores.normalColor = relleno;
        colores.highlightedColor = rellenoSeleccion;
        colores.selectedColor = rellenoSeleccion;
        colores.pressedColor = Color.Lerp(rellenoSeleccion, Color.white, 0.25f);
        colores.disabledColor = Color.Lerp(relleno, Color.black, 0.4f);
        colores.colorMultiplier = 1f;
        colores.fadeDuration = 0.06f;
        boton.colors = colores;

        Text texto = Texto(go.transform, "Texto", etiqueta, tamanoLetra,
                           TextAnchor.MiddleCenter, Vector2.zero,
                           V(AnchoTecla, AltoTecla), Color.white);
        texto.fontStyle = FontStyle.Bold;

        TeclaCodigoUI tecla = go.AddComponent<TeclaCodigoUI>();
        tecla.Configurar(etiqueta, bordeTecla, borde, HackUI.ColAcento, PulsarTecla);
        boton.onClick.AddListener(tecla.Presionar);

        return boton;
    }

    /// <summary>
    /// Navegación explícita en cuadrícula de 4 x 3. Al salir por un borde
    /// aparece por el lado contrario.
    /// </summary>
    private void ConectarNavegacion()
    {
        for (int i = 0; i < teclas.Length; i++)
        {
            int columna = i % Columnas;
            int fila = i / Columnas;

            int filaArriba = fila - 1;
            if (filaArriba < 0) filaArriba = Filas - 1;
            int filaAbajo = fila + 1;
            if (filaAbajo >= Filas) filaAbajo = 0;
            int columnaIzquierda = columna - 1;
            if (columnaIzquierda < 0) columnaIzquierda = Columnas - 1;
            int columnaDerecha = columna + 1;
            if (columnaDerecha >= Columnas) columnaDerecha = 0;

            Navigation navegacion = new Navigation();
            navegacion.mode = Navigation.Mode.Explicit;
            navegacion.selectOnUp = teclas[filaArriba * Columnas + columna];
            navegacion.selectOnDown = teclas[filaAbajo * Columnas + columna];
            navegacion.selectOnLeft = teclas[fila * Columnas + columnaIzquierda];
            navegacion.selectOnRight = teclas[fila * Columnas + columnaDerecha];
            teclas[i].navigation = navegacion;
        }
    }

    // ── Ayudantes de tamaño ───────────────────────────────────────

    /// <summary>Vector2 multiplicado por la escala de la tarjeta.</summary>
    private Vector2 V(float x, float y)
    {
        return new Vector2(x * Escala, y * Escala);
    }

    /// <summary>HackUI.CrearTexto con el tamaño de letra multiplicado por la escala.</summary>
    private Text Texto(Transform padre, string nombre, string contenido, int tamano,
                       TextAnchor alineacion, Vector2 posicion, Vector2 dimension, Color color)
    {
        int tamanoEscalado = Mathf.RoundToInt(tamano * Escala);
        return HackUI.CrearTexto(padre, nombre, contenido, tamanoEscalado, alineacion, posicion, dimension, color);
    }

    // ── Búsqueda de la pantalla del código ────────────────────────

    /// <summary>Busca en la escena el script que implementa IPuzzleCodigo.</summary>
    private IPuzzleCodigo BuscarPantallaCodigo()
    {
        MonoBehaviour[] todos = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (MonoBehaviour mb in todos)
        {
            IPuzzleCodigo encontrada = mb as IPuzzleCodigo;
            if (encontrada != null) return encontrada;
        }
        return null;
    }
}
