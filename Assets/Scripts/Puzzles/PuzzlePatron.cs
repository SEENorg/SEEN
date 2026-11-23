using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Minijuego: memorizar una secuencia de luces y repetirla (tipo "Simon").
/// Cada módulo agrega más pasos. Si el jugador se equivoca, se genera una secuencia nueva.
///
/// Accesibilidad (daltonismo): cada panel tiene un número y el nombre de su color, y al
/// encenderse además crece y su borde se pone blanco. Se puede seguir sin distinguir colores.
/// Controles: ratón (clic), mando (cruceta + A) o teclado (flechas + Enter).
/// </summary>
public class PuzzlePatron : IPuzzle
{
    private const float AnchoTarjeta = 640f;
    private const float AltoTarjeta = 520f;
    private const float AnchoPanel = 200f;
    private const float AltoPanel = 118f;
    private const float EscalaEncendido = 1.08f;
    private const float AnchoProgreso = 360f;

    private readonly Color[] encendidos =
    {
        new Color(1f, 0.25f, 0.25f), new Color(0.25f, 0.8f, 1f),
        new Color(1f, 0.85f, 0.2f), new Color(0.4f, 1f, 0.35f)
    };

    private readonly Color[] apagados =
    {
        new Color(0.3f, 0.1f, 0.1f), new Color(0.1f, 0.25f, 0.3f),
        new Color(0.3f, 0.26f, 0.08f), new Color(0.12f, 0.3f, 0.13f)
    };

    private readonly string[] nombresColor = { "ROJO", "AZUL", "AMARILLO", "VERDE" };
    private readonly Color colorSegmentoVacio = new Color(0.11f, 0.20f, 0.26f);
    private readonly Color colorObservar = new Color(1f, 0.85f, 0.3f);

    private readonly IContextoHack ctx;
    private readonly List<int> secuencia = new List<int>();
    private CajasRedondeadasUI cajas;
    private TarjetaHackUI tarjeta;
    private Button[] botones;
    private Image[] fondos;
    private Image[] segmentos;
    private GameObject ultimaSeleccion;
    private int indiceJugador;
    private bool mostrandoSecuencia;
    private bool detenido;

    public PuzzlePatron(IContextoHack ctx)
    {
        this.ctx = ctx;
    }

    public void Construir()
    {
        detenido = false;
        cajas = new CajasRedondeadasUI();
        ctx.MostrarMarco(false);
        tarjeta = new TarjetaHackUI(ctx, cajas, AnchoTarjeta, AltoTarjeta, "MEMORIA DE SEÑAL: REPITE LA SECUENCIA DE LUCES");

        botones = new Button[4];
        fondos = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            float x = AnchoPanel / 2f + 6f;
            if (i % 2 == 0) x = -x;
            float y = 40f;
            if (i >= 2) y = 40f - AltoPanel - 14f;

            botones[i] = tarjeta.CrearBoton(tarjeta.Raiz, "Panel_" + i, new Vector2(x, y),
                                            new Vector2(AnchoPanel, AltoPanel), apagados[i], apagados[i],
                                            tarjeta.ColorBordeBoton, i.ToString(), PulsarPanel, false);
            fondos[i] = botones[i].GetComponent<Image>();

            // Número grande + nombre del color: no depende de distinguir colores
            Text numero = HackUI.CrearTexto(botones[i].transform, "Numero", (i + 1).ToString(), 40,
                                            TextAnchor.MiddleCenter, new Vector2(0f, 12f),
                                            new Vector2(AnchoPanel, 50f), Color.white);
            numero.fontStyle = FontStyle.Bold;
            HackUI.CrearTexto(botones[i].transform, "Color", nombresColor[i], 14, TextAnchor.MiddleCenter,
                              new Vector2(0f, -30f), new Vector2(AnchoPanel, 22f), new Color(1f, 1f, 1f, 0.75f));
        }

        int largo = Mathf.Clamp(3 + ctx.DificultadBase + ctx.IndiceModulo, 3, 8);
        CrearProgreso(largo);

        ultimaSeleccion = botones[0].gameObject;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(ultimaSeleccion);

        GenerarSecuencia(largo);
        ctx.IniciarCorrutina(MostrarSecuencia());
    }

    public void Actualizar()
    {
        MantenerSeleccion();
    }

    /// <summary>Se llama al cerrar el hackeo o cambiar de módulo: frena las corrutinas pendientes.</summary>
    public void Detener()
    {
        detenido = true;
        ctx.MostrarMarco(true);
        if (cajas != null) cajas.Liberar();
        cajas = null;
        tarjeta = null;
        botones = null;
        fondos = null;
        segmentos = null;
        ultimaSeleccion = null;
    }

    // ── Secuencia ───────────────────────────────────────────────────

    /// <summary>Crea una secuencia nueva al azar y bloquea los clics hasta mostrarla.</summary>
    private void GenerarSecuencia(int largo)
    {
        secuencia.Clear();
        for (int i = 0; i < largo; i++) secuencia.Add(Random.Range(0, 4));
        indiceJugador = 0;
        mostrandoSecuencia = true;
        ActualizarProgreso();
    }

    private IEnumerator MostrarSecuencia()
    {
        if (tarjeta == null) yield break;
        tarjeta.MostrarEstado("OBSERVA LA SECUENCIA...", colorObservar);
        yield return new WaitForSecondsRealtime(0.8f);

        for (int i = 0; i < secuencia.Count; i++)
        {
            if (detenido) yield break;
            int indice = secuencia[i];
            Encender(indice, true);
            yield return new WaitForSecondsRealtime(0.5f);
            if (detenido) yield break;
            Encender(indice, false);
            yield return new WaitForSecondsRealtime(0.22f);
        }

        if (detenido) yield break;
        mostrandoSecuencia = false;
        MostrarTurno();
    }

    private void PulsarPanel(string valor)
    {
        if (mostrandoSecuencia || ctx.EnTransicion || ctx.InputBloqueado || detenido) return;

        int indice = int.Parse(valor);
        ctx.IniciarCorrutina(Destellar(indice));

        if (secuencia[indiceJugador] == indice)
        {
            indiceJugador++;
            ActualizarProgreso();
            if (indiceJugador >= secuencia.Count)
            {
                tarjeta.MostrarEstado("SECUENCIA CORRECTA", HackUI.ColVerde);
                ctx.CompletarModulo();
            }
            else
            {
                MostrarTurno();
            }
        }
        else
        {
            tarjeta.MostrarEstado("SECUENCIA INCORRECTA: NUEVA SECUENCIA", HackUI.ColRojo);
            mostrandoSecuencia = true;   // bloquea los clics mientras se prepara la nueva
            ctx.IniciarCorrutina(ReiniciarSecuenciaNueva());
        }
    }

    /// <summary>Al fallar, genera una secuencia NUEVA (no repite la misma).</summary>
    private IEnumerator ReiniciarSecuenciaNueva()
    {
        yield return new WaitForSecondsRealtime(1f);
        if (detenido) yield break;
        GenerarSecuencia(secuencia.Count);
        yield return ctx.IniciarCorrutina(MostrarSecuencia());
    }

    private IEnumerator Destellar(int indice)
    {
        Encender(indice, true);
        yield return new WaitForSecondsRealtime(0.18f);
        if (detenido) yield break;
        Encender(indice, false);
    }

    private void MostrarTurno()
    {
        tarjeta.MostrarEstadoNormal("TU TURNO: PASO " + (indiceJugador + 1) + " DE " + secuencia.Count);
    }

    // ── Aspecto ─────────────────────────────────────────────────────

    /// <summary>Encendido: color brillante, más grande y borde blanco (no solo cambia el color).</summary>
    private void Encender(int indice, bool encendido)
    {
        if (fondos == null || fondos[indice] == null || tarjeta == null) return;
        TeclaCodigoUI tecla = botones[indice].GetComponent<TeclaCodigoUI>();

        if (encendido)
        {
            fondos[indice].color = encendidos[indice];
            // Primero el borde (eso fija la escala normal) y después la escala de encendido
            if (tecla != null) tecla.CambiarColoresBorde(Color.white, Color.white);
            fondos[indice].transform.localScale = Vector3.one * EscalaEncendido;
        }
        else
        {
            fondos[indice].color = apagados[indice];
            // Vuelve al borde normal; CambiarColoresBorde también devuelve la escala normal
            if (tecla != null) tecla.CambiarColoresBorde(tarjeta.ColorBordeBoton, HackUI.ColAcento);
            else fondos[indice].transform.localScale = Vector3.one;
        }
    }

    private void CrearProgreso(int largo)
    {
        segmentos = new Image[largo];
        float separacion = 6f;
        float ancho = (AnchoProgreso - separacion * (largo - 1)) / largo;
        float inicio = -AnchoProgreso / 2f + ancho / 2f;
        for (int i = 0; i < largo; i++)
        {
            float x = inicio + i * (ancho + separacion);
            segmentos[i] = cajas.CrearRelleno(tarjeta.Raiz, "Paso_" + i, new Vector2(x, -192f),
                                              new Vector2(ancho, 8f), colorSegmentoVacio, 4f);
        }
    }

    private void ActualizarProgreso()
    {
        if (segmentos == null) return;
        for (int i = 0; i < segmentos.Length; i++)
        {
            if (i < indiceJugador) segmentos[i].color = HackUI.ColAcento;
            else segmentos[i].color = colorSegmentoVacio;
        }
    }

    // ── Selección (para el mando) ───────────────────────────────────

    private void MantenerSeleccion()
    {
        if (EventSystem.current == null || botones == null) return;
        GameObject actual = EventSystem.current.currentSelectedGameObject;

        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i] != null && botones[i].gameObject == actual)
            {
                ultimaSeleccion = actual;
                return;
            }
        }

        if (actual == null && ultimaSeleccion != null)
            EventSystem.current.SetSelectedGameObject(ultimaSeleccion);
    }
}
