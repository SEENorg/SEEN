using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Minijuego: encontrar el glifo distinto (◈) entre todos los iguales (◆) de una cuadrícula.
/// La diferencia es de FORMA, no de color, así que sirve para personas con daltonismo.
///
/// Controles: ratón (clic), mando (cruceta + A) o teclado (flechas + Enter).
/// Si eliges un glifo equivocado, la celda se marca en rojo y la cuadrícula se vuelve a generar.
/// </summary>
public class PuzzleEscanear : IPuzzle
{
    private const float AnchoTarjeta = 560f;
    private const float AltoTarjeta = 560f;
    private const float LadoCuadricula = 340f;
    private const float YCuadricula = -32f;
    private const float DuracionError = 0.8f;
    private const string GlifoIgual = "◆";
    private const string GlifoDistinto = "◈";

    private readonly IContextoHack ctx;
    private CajasRedondeadasUI cajas;
    private TarjetaHackUI tarjeta;
    private Transform cuadricula;
    private Button[] celdas;
    private GameObject ultimaSeleccion;
    private int filas;
    private int indiceDistinto;
    private int indiceAnterior;
    private float tiempoError;
    private bool resuelto;

    public PuzzleEscanear(IContextoHack ctx)
    {
        this.ctx = ctx;
    }

    public void Construir()
    {
        cajas = new CajasRedondeadasUI();
        ctx.MostrarMarco(false);
        tarjeta = new TarjetaHackUI(ctx, cajas, AnchoTarjeta, AltoTarjeta, "ESCANEO DE MATRIZ: ENCUENTRA EL GLIFO DIFERENTE");

        filas = 4 + Mathf.Clamp(ctx.DificultadBase + ctx.IndiceModulo, 0, 2);
        tiempoError = 0f;
        resuelto = false;
        indiceAnterior = 0;

        ConstruirCuadricula();
    }

    public void Actualizar()
    {
        if (tiempoError > 0f)
        {
            tiempoError -= Time.unscaledDeltaTime;
            if (tiempoError <= 0f) ConstruirCuadricula();   // reescaneo
        }
        MantenerSeleccion();
    }

    public void Detener()
    {
        ctx.MostrarMarco(true);
        if (cajas != null) cajas.Liberar();
        cajas = null;
        tarjeta = null;
        celdas = null;
        cuadricula = null;
        ultimaSeleccion = null;
    }

    // ── Cuadrícula ──────────────────────────────────────────────────

    private void ConstruirCuadricula()
    {
        if (tarjeta == null) return;
        if (cuadricula != null) Object.Destroy(cuadricula.gameObject);

        cuadricula = HackUI.NuevoUI(tarjeta.Raiz, "Cuadricula", new Vector2(0f, YCuadricula),
                                    new Vector2(LadoCuadricula, LadoCuadricula)).transform;

        int total = filas * filas;
        indiceDistinto = Random.Range(0, total);
        celdas = new Button[total];

        float paso = LadoCuadricula / filas;
        float tamanoCelda = paso - 8f;
        int tamanoGlifo = Mathf.RoundToInt(tamanoCelda * 0.55f);

        for (int i = 0; i < total; i++)
        {
            int fila = i / filas;
            int columna = i % filas;
            float x = -LadoCuadricula / 2f + paso / 2f + columna * paso;
            float y = LadoCuadricula / 2f - paso / 2f - fila * paso;

            celdas[i] = tarjeta.CrearBoton(cuadricula, "Celda_" + i, new Vector2(x, y),
                                           new Vector2(tamanoCelda, tamanoCelda),
                                           tarjeta.ColorBoton, tarjeta.ColorBotonSeleccion, tarjeta.ColorBordeBoton,
                                           i.ToString(), ElegirCelda, true);

            string glifo = GlifoIgual;
            if (i == indiceDistinto) glifo = GlifoDistinto;
            HackUI.CrearTexto(celdas[i].transform, "Glifo", glifo, tamanoGlifo, TextAnchor.MiddleCenter,
                              Vector2.zero, new Vector2(tamanoCelda, tamanoCelda), Color.white);
        }

        tiempoError = 0f;
        tarjeta.MostrarEstadoNormal("BUSCA EL GLIFO DIFERENTE  (" + filas + " x " + filas + ")");

        // La selección se queda en la misma celda donde estaba (cómodo con el mando)
        int indiceSeleccion = Mathf.Clamp(indiceAnterior, 0, total - 1);
        ultimaSeleccion = celdas[indiceSeleccion].gameObject;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(ultimaSeleccion);
    }

    private void ElegirCelda(string valor)
    {
        if (ctx.InputBloqueado || ctx.EnTransicion) return;
        if (resuelto || tiempoError > 0f) return;

        int indice = int.Parse(valor);
        indiceAnterior = indice;
        TeclaCodigoUI celda = celdas[indice].GetComponent<TeclaCodigoUI>();

        if (indice == indiceDistinto)
        {
            resuelto = true;
            if (celda != null) celda.CambiarColoresBorde(HackUI.ColVerde, HackUI.ColVerde);
            tarjeta.MostrarEstado("GLIFO ENCONTRADO: MATRIZ ESCANEADA", HackUI.ColVerde);
            ctx.CompletarModulo();
        }
        else
        {
            if (celda != null) celda.CambiarColoresBorde(HackUI.ColRojo, HackUI.ColRojo);
            tarjeta.MostrarEstado("GLIFO INCORRECTO: REESCANEANDO...", HackUI.ColRojo);
            tiempoError = DuracionError;
        }
    }

    // ── Selección (para el mando) ───────────────────────────────────

    private void MantenerSeleccion()
    {
        if (EventSystem.current == null || celdas == null) return;
        GameObject actual = EventSystem.current.currentSelectedGameObject;

        for (int i = 0; i < celdas.Length; i++)
        {
            if (celdas[i] != null && celdas[i].gameObject == actual)
            {
                ultimaSeleccion = actual;
                indiceAnterior = i;
                return;
            }
        }

        if (actual == null && ultimaSeleccion != null)
            EventSystem.current.SetSelectedGameObject(ultimaSeleccion);
    }
}
