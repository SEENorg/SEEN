using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Minijuego: descifrar un código César con desplazamiento variable (+1 a +5). Se muestra una
/// palabra cifrada; para descifrarla hay que avanzar cada letra la cantidad indicada.
/// Las palabras las da un IBancoPalabras (por defecto, buen uso de redes sociales).
/// Mientras este puzzle está en pantalla (Activo), la tabla de ayuda se abre con TAB.
///
/// La respuesta se escribe en casillas, una por letra, alineadas con el código:
/// - Teclado: escribe las letras; flechas ← → para moverte, ↑ ↓ para cambiar la letra,
///   Retroceso para borrar y Enter para verificar.
/// - Mando: cruceta (o stick) ← → para moverte y ↑ ↓ para cambiar la letra, A para verificar,
///   B para borrar.
/// - Ratón: clic en una casilla para ir a ella; botones BORRAR y VERIFICAR.
/// </summary>
public class PuzzleDescifrar : IPuzzle
{
    private const float AnchoTarjeta = 680f;
    private const float AltoTarjeta = 540f;
    private const float AnchoMaximoCasillas = 600f;
    private const float SeparacionCasillas = 8f;
    private const float DuracionError = 0.8f;
    private const char Vacia = ' ';

    // Cuántas letras hay que AVANZAR para descifrar (varía entre +1 y +5)
    private static readonly int[] DesplazamientosDisponibles = { 1, 2, 3, 4, 5 };
    private static int ultimoDesplazamiento = 0;
    public static int DesplazamientoActual { get; private set; } = 1;
    private int desplazamiento = 1;

    private readonly Color colorCasilla = new Color(0.06f, 0.13f, 0.18f);
    private readonly Color colorCasillaActual = new Color(0.09f, 0.24f, 0.32f);
    private readonly Color colorVacia = new Color(0.30f, 0.42f, 0.50f);

    private readonly IContextoHack ctx;

    // De dónde salen las palabras (se le puede dar otro banco sin tocar este puzzle)
    private readonly IBancoPalabras banco;

    /// <summary>True solo mientras este puzzle está en pantalla. La tabla del TAB lo consulta.</summary>
    public static bool Activo { get; private set; }

    private CajasRedondeadasUI cajas;
    private TarjetaHackUI tarjeta;
    private string palabra;
    private char[] respuesta;
    private int cursor;
    private Image[] bordesCasillas;
    private Image[] fondosCasillas;
    private Text[] textosCasillas;
    private float tiempoError;
    private bool resuelto;
    private int frameUltimaAccion = -1;

    // La fábrica sigue usando este constructor: por defecto, el banco de redes sociales
    public PuzzleDescifrar(IContextoHack ctx) : this(ctx, new BancoPalabrasRedes()) { }

    // Inyección por constructor: el banco llega desde afuera
    public PuzzleDescifrar(IContextoHack ctx, IBancoPalabras banco)
    {
        this.ctx = ctx;
        this.banco = banco;
    }

    public void Construir()
    {
        Activo = true;
        cajas = new CajasRedondeadasUI();
        ctx.MostrarMarco(false);
        // Elegir desplazamiento César aleatorio entre +1 y +5 sin repetir el último usado
        int candidato = DesplazamientosDisponibles[Random.Range(0, DesplazamientosDisponibles.Length)];
        if (candidato == ultimoDesplazamiento && DesplazamientosDisponibles.Length > 1)
        {
            int idx = System.Array.IndexOf(DesplazamientosDisponibles, candidato);
            candidato = DesplazamientosDisponibles[(idx + Random.Range(1, DesplazamientosDisponibles.Length)) % DesplazamientosDisponibles.Length];
        }
        desplazamiento = candidato;
        ultimoDesplazamiento = desplazamiento;
        DesplazamientoActual = desplazamiento;

        char ejDestino = (char)('A' + ((1 + desplazamiento) % 26));
        string txtLugares = desplazamiento == 1 ? "1 LUGAR" : (desplazamiento + " LUGARES");
        string subtitulo = "DESCIFRADO CÉSAR +" + desplazamiento + ": AVANZA CADA LETRA " + txtLugares + " (B → " + ejDestino + ")";

        tarjeta = new TarjetaHackUI(ctx, cajas, AnchoTarjeta, AltoTarjeta, subtitulo);

        // Nivel: tutorial (0) fácil → Nivel 3 (2 o más) difícil; define qué tan larga es la palabra
        int nivel = Mathf.Clamp(ctx.DificultadBase + ctx.IndiceModulo, 0, 5);
        palabra = banco.ObtenerPalabra(nivel);

        // Se cifra RETROCEDIENDO 'desplazamiento' letras; el jugador descifra AVANZANDO ese mismo número
        string codigoCifrado = Desplazar(palabra, -desplazamiento);

        respuesta = new char[palabra.Length];
        for (int i = 0; i < respuesta.Length; i++) respuesta[i] = Vacia;
        cursor = 0;
        tiempoError = 0f;
        resuelto = false;

        Transform raiz = tarjeta.Raiz;
        int largo = palabra.Length;
        float anchoCasilla = Mathf.Min(54f, (AnchoMaximoCasillas - SeparacionCasillas * (largo - 1)) / largo);
        float inicio = -((anchoCasilla + SeparacionCasillas) * (largo - 1)) / 2f;

        // Código interceptado (una casilla por letra)
        HackUI.CrearTexto(raiz, "TituloCodigo", "CÓDIGO INTERCEPTADO", 13, TextAnchor.MiddleCenter,
                          new Vector2(0f, 115f), new Vector2(600f, 20f), tarjeta.ColorTextoSuave);
        for (int i = 0; i < largo; i++)
        {
            float x = inicio + i * (anchoCasilla + SeparacionCasillas);
            Image borde = cajas.CrearCaja(raiz, "Cifrada_" + i, new Vector2(x, 80f), new Vector2(anchoCasilla, 50f),
                                          new Color(0.04f, 0.08f, 0.11f), tarjeta.ColorBordeTarjeta, 2f);
            Text letra = HackUI.CrearTexto(borde.transform.parent, "Letra", codigoCifrado.Substring(i, 1), 30,
                                           TextAnchor.MiddleCenter, Vector2.zero, new Vector2(anchoCasilla, 50f),
                                           HackUI.ColAcento);
            letra.fontStyle = FontStyle.Bold;
        }

        // Respuesta (casillas editables)
        HackUI.CrearTexto(raiz, "TituloRespuesta", "TU RESPUESTA", 13, TextAnchor.MiddleCenter,
                          new Vector2(0f, 40f), new Vector2(600f, 20f), tarjeta.ColorTextoSuave);
        bordesCasillas = new Image[largo];
        fondosCasillas = new Image[largo];
        textosCasillas = new Text[largo];
        for (int i = 0; i < largo; i++)
        {
            float x = inicio + i * (anchoCasilla + SeparacionCasillas);
            bordesCasillas[i] = cajas.CrearCaja(raiz, "Respuesta_" + i, new Vector2(x, 2f),
                                                new Vector2(anchoCasilla, 62f), colorCasilla,
                                                tarjeta.ColorBordeBoton, 2f);
            GameObject go = bordesCasillas[i].transform.parent.gameObject;
            fondosCasillas[i] = go.GetComponent<Image>();

            // Clic en la casilla: el cursor va a ella
            ClicIndiceUI clic = go.AddComponent<ClicIndiceUI>();
            clic.Configurar(i, AlClicCasilla);

            textosCasillas[i] = HackUI.CrearTexto(go.transform, "Letra", "_", 34, TextAnchor.MiddleCenter,
                                                  Vector2.zero, new Vector2(anchoCasilla, 62f), colorVacia);
            textosCasillas[i].fontStyle = FontStyle.Bold;
        }

        // Ayuda de controles
        HackUI.CrearTexto(raiz, "AyudaTeclado",
                          "TECLADO  escribe · ← → moverte · ↑ ↓ cambiar letra · Enter verificar · TAB tabla César",
                          13, TextAnchor.MiddleCenter, new Vector2(0f, -55f), new Vector2(640f, 20f),
                          tarjeta.ColorTextoSuave);
        HackUI.CrearTexto(raiz, "AyudaMando",
                          "MANDO  cruceta ← → moverte · ↑ ↓ cambiar letra · A verificar · B borrar",
                          13, TextAnchor.MiddleCenter, new Vector2(0f, -75f), new Vector2(640f, 20f),
                          tarjeta.ColorTextoSuave);

        // Botones
        Button borrar = tarjeta.CrearBoton(raiz, "BotonBorrar", new Vector2(-115f, -135f), new Vector2(200f, 56f),
                                           new Color(0.34f, 0.12f, 0.16f), new Color(0.45f, 0.16f, 0.21f),
                                           new Color(0.62f, 0.22f, 0.28f), "BORRAR", AlPulsarBoton, true);
        Text textoBorrar = HackUI.CrearTexto(borrar.transform, "Texto", "BORRAR", 19, TextAnchor.MiddleCenter,
                                             Vector2.zero, new Vector2(200f, 56f), Color.white);
        textoBorrar.fontStyle = FontStyle.Bold;

        Button verificar = tarjeta.CrearBoton(raiz, "BotonVerificar", new Vector2(115f, -135f), new Vector2(200f, 56f),
                                              new Color(0.05f, 0.36f, 0.26f), new Color(0.07f, 0.47f, 0.34f),
                                              new Color(0.18f, 0.72f, 0.50f), "VERIFICAR", AlPulsarBoton, true);
        Text textoVerificar = HackUI.CrearTexto(verificar.transform, "Texto", "VERIFICAR", 19, TextAnchor.MiddleCenter,
                                                Vector2.zero, new Vector2(200f, 56f), Color.white);
        textoVerificar.fontStyle = FontStyle.Bold;

        tarjeta.MostrarEstadoNormal("ESCRIBE LA PALABRA ORIGINAL");
        ActualizarCasillas();

        // Este puzzle maneja el teclado y el mando por su cuenta: nada queda seleccionado
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    public void Actualizar()
    {
        if (tiempoError > 0f)
        {
            tiempoError -= Time.unscaledDeltaTime;
            if (tiempoError <= 0f)
            {
                tarjeta.MostrarEstadoNormal("ESCRIBE LA PALABRA ORIGINAL");
                ActualizarCasillas();
            }
        }

        // Si el ratón dejó algo seleccionado, se suelta (A y Enter los lee este puzzle)
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (resuelto || ctx.InputBloqueado || ctx.EnTransicion) return;
        LeerTeclado();
        LeerMando();
    }

    public void Detener()
    {
        Activo = false;
        ctx.MostrarMarco(true);
        if (cajas != null) cajas.Liberar();
        cajas = null;
        tarjeta = null;
        bordesCasillas = null;
        fondosCasillas = null;
        textosCasillas = null;
    }

    // ── Acciones ────────────────────────────────────────────────────

    private void Escribir(char letra)
    {
        respuesta[cursor] = letra;
        if (cursor < respuesta.Length - 1) cursor++;
        ActualizarCasillas();
    }

    private void Borrar()
    {
        // Si la casilla actual tiene letra, se borra; si está vacía, se borra la anterior
        if (respuesta[cursor] == Vacia && cursor > 0) cursor--;
        respuesta[cursor] = Vacia;
        ActualizarCasillas();
    }

    private void Mover(int direccion)
    {
        cursor = Mathf.Clamp(cursor + direccion, 0, respuesta.Length - 1);
        ActualizarCasillas();
    }

    /// <summary>↑ ↓: cambia la letra de la casilla actual (A → B → ... → Z → A).</summary>
    private void CambiarLetra(int direccion)
    {
        int actual;
        if (respuesta[cursor] == Vacia)
        {
            // Empieza desde la letra del código, que es la pista natural
            actual = palabra[cursor] - 'A' - desplazamiento - direccion;
        }
        else
        {
            actual = respuesta[cursor] - 'A';
        }
        int nueva = ((actual + direccion) % 26 + 26) % 26;
        respuesta[cursor] = (char)('A' + nueva);
        ActualizarCasillas();
    }

    private void Verificar()
    {
        if (Time.frameCount == frameUltimaAccion) return;
        frameUltimaAccion = Time.frameCount;

        string escrita = new string(respuesta);
        if (escrita.IndexOf(Vacia) >= 0)
        {
            MostrarError("COMPLETA TODAS LAS LETRAS");
            return;
        }

        if (escrita == palabra)
        {
            resuelto = true;
            tarjeta.MostrarEstado("CÓDIGO DESCIFRADO: " + palabra, HackUI.ColVerde);
            for (int i = 0; i < bordesCasillas.Length; i++) bordesCasillas[i].color = HackUI.ColVerde;
            ctx.CompletarModulo();
        }
        else
        {
            MostrarError("CÓDIGO INCORRECTO: REVISA LAS LETRAS");
            for (int i = 0; i < bordesCasillas.Length; i++) bordesCasillas[i].color = HackUI.ColRojo;
        }
    }

    private void MostrarError(string mensaje)
    {
        tarjeta.MostrarEstado(mensaje, HackUI.ColRojo);
        tiempoError = DuracionError;
    }

    private void AlPulsarBoton(string valor)
    {
        if (resuelto || ctx.InputBloqueado || ctx.EnTransicion) return;
        if (valor == "VERIFICAR") Verificar();
        else Borrar();
    }

    /// <summary>Clic en una casilla: el cursor va a ella.</summary>
    private void AlClicCasilla(int indice)
    {
        if (resuelto || respuesta == null) return;
        cursor = Mathf.Clamp(indice, 0, respuesta.Length - 1);
        ActualizarCasillas();
    }

    // ── Entrada ─────────────────────────────────────────────────────

    private void LeerTeclado()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        // Letras A-Z (son seguidas en el enum Key)
        for (int i = 0; i < 26; i++)
        {
            Key tecla = (Key)((int)Key.A + i);
            if (teclado[tecla].wasPressedThisFrame) Escribir((char)('A' + i));
        }

        if (teclado.backspaceKey.wasPressedThisFrame) Borrar();
        if (teclado.leftArrowKey.wasPressedThisFrame) Mover(-1);
        if (teclado.rightArrowKey.wasPressedThisFrame) Mover(1);
        if (teclado.upArrowKey.wasPressedThisFrame) CambiarLetra(1);
        if (teclado.downArrowKey.wasPressedThisFrame) CambiarLetra(-1);
        if (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame) Verificar();
#else
        for (int i = 0; i < 26; i++)
        {
            if (Input.GetKeyDown(KeyCode.A + i)) Escribir((char)('A' + i));
        }
        if (Input.GetKeyDown(KeyCode.Backspace)) Borrar();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) Mover(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) Mover(1);
        if (Input.GetKeyDown(KeyCode.UpArrow)) CambiarLetra(1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) CambiarLetra(-1);
        if (Input.GetKeyDown(KeyCode.Return)) Verificar();
#endif
    }

    private void LeerMando()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad mando = Gamepad.current;
        if (mando == null) return;

        if (mando.dpad.left.wasPressedThisFrame || mando.leftStick.left.wasPressedThisFrame) Mover(-1);
        if (mando.dpad.right.wasPressedThisFrame || mando.leftStick.right.wasPressedThisFrame) Mover(1);
        if (mando.dpad.up.wasPressedThisFrame || mando.leftStick.up.wasPressedThisFrame) CambiarLetra(1);
        if (mando.dpad.down.wasPressedThisFrame || mando.leftStick.down.wasPressedThisFrame) CambiarLetra(-1);
        if (mando.buttonSouth.wasPressedThisFrame) Verificar();   // A
        if (mando.buttonEast.wasPressedThisFrame) Borrar();       // B
#endif
    }

    // ── Aspecto ─────────────────────────────────────────────────────

    private void ActualizarCasillas()
    {
        if (textosCasillas == null) return;
        bool enError = tiempoError > 0f;

        for (int i = 0; i < textosCasillas.Length; i++)
        {
            if (respuesta[i] == Vacia)
            {
                textosCasillas[i].text = "_";
                textosCasillas[i].color = colorVacia;
            }
            else
            {
                textosCasillas[i].text = respuesta[i].ToString();
                textosCasillas[i].color = Color.white;
            }

            bool actual = i == cursor;
            if (actual) fondosCasillas[i].color = colorCasillaActual;
            else fondosCasillas[i].color = colorCasilla;

            if (enError) continue;   // durante el error los bordes quedan rojos
            if (actual) bordesCasillas[i].color = HackUI.ColAcento;
            else bordesCasillas[i].color = tarjeta.ColorBordeBoton;
        }
    }

    // ── Texto ───────────────────────────────────────────────────────

    /// <summary>Desplaza cada letra n posiciones (n puede ser negativo).</summary>
    private string Desplazar(string texto, int n)
    {
        char[] letras = texto.ToCharArray();
        for (int i = 0; i < letras.Length; i++)
        {
            if (letras[i] >= 'A' && letras[i] <= 'Z')
                letras[i] = (char)('A' + (((letras[i] - 'A' + n) % 26) + 26) % 26);
        }
        return new string(letras);
    }
}
