using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static HackUI;

/// <summary>
/// Minijuego: memorizar una secuencia de luces de colores y repetirla (tipo "Simon").
/// Cada módulo agrega más pasos. Si el jugador se equivoca, se genera una secuencia nueva.
/// </summary>
public class PuzzlePatron : IPuzzle
{
    private readonly IContextoHack ctx;

    private readonly Color[] encendidos = {
        new Color(1f, 0.25f, 0.25f), new Color(0.25f, 0.8f, 1f),
        new Color(1f, 0.85f, 0.2f), new Color(0.4f, 1f, 0.35f)
    };

    private readonly Color[] apagados = {
        new Color(0.3f, 0.1f, 0.1f), new Color(0.1f, 0.25f, 0.3f),
        new Color(0.3f, 0.26f, 0.08f), new Color(0.12f, 0.3f, 0.13f)
    };

    private readonly List<int> secuencia = new List<int>();
    private Button[] botones;
    private int indiceJugador;
    private bool mostrandoSecuencia;
    private bool detenido;

    public PuzzlePatron(IContextoHack ctx) { this.ctx = ctx; }

    public void Actualizar() { }

    /// <summary>Se llama al cerrar el hackeo o cambiar de módulo: frena las corrutinas pendientes.</summary>
    public void Detener()
    {
        detenido = true;
    }

    public void Construir()
    {
        detenido = false;
        Transform contenido = ctx.Contenido;

        int largo = Mathf.Clamp(3 + ctx.DificultadBase + ctx.IndiceModulo, 3, 8);
        CrearTexto(contenido, "LblPatron", "REPITE LA SECUENCIA DE LUCES", 26, TextAnchor.MiddleCenter,
                   new Vector2(0f, 195f), new Vector2(760f, 40f), ColAcento);

        Transform grid = NuevoUI(contenido, "Grid", new Vector2(0f, -10f), new Vector2(360f, 260f)).transform;
        botones = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            float x = 90f;
            if (i % 2 == 0) x = -90f;
            float y = -65f;
            if (i < 2) y = 65f;

            Button b = CrearBoton(grid, "Boton_" + i, "", new Vector2(x, y), new Vector2(170f, 120f), apagados[i]);
            b.image.color = apagados[i];
            int idx = i;
            b.onClick.AddListener(() => ClickPatron(idx));
            botones[i] = b;
        }

        GenerarSecuencia(largo);
        ctx.IniciarCorrutina(MostrarSecuencia());
    }

    /// <summary>Crea una secuencia nueva al azar y bloquea los clics hasta mostrarla.</summary>
    private void GenerarSecuencia(int largo)
    {
        secuencia.Clear();
        for (int i = 0; i < largo; i++)
            secuencia.Add(Random.Range(0, 4));
        indiceJugador = 0;
        mostrandoSecuencia = true;
    }

    private IEnumerator MostrarSecuencia()
    {
        ctx.MostrarFeedback("OBSERVA LA SECUENCIA...", false);
        yield return new WaitForSecondsRealtime(0.7f);

        for (int i = 0; i < secuencia.Count; i++)
        {
            if (detenido) yield break;

            int idx = secuencia[i];
            if (botones[idx] != null) botones[idx].image.color = encendidos[idx];
            yield return new WaitForSecondsRealtime(0.45f);
            if (botones[idx] != null) botones[idx].image.color = apagados[idx];
            yield return new WaitForSecondsRealtime(0.22f);
        }

        if (detenido) yield break;
        mostrandoSecuencia = false;
        ctx.MostrarFeedback("REPITE LA SECUENCIA", false);
    }

    private void ClickPatron(int idx)
    {
        if (mostrandoSecuencia || ctx.EnTransicion || ctx.InputBloqueado) return;

        // Flash del botón
        ctx.IniciarCorrutina(FlashBoton(idx));

        if (secuencia[indiceJugador] == idx)
        {
            indiceJugador++;
            if (indiceJugador >= secuencia.Count)
            {
                ctx.MostrarFeedback("MÓDULO COMPLETADO", true);
                ctx.CompletarModulo();
            }
        }
        else
        {
            ctx.MostrarFeedback("SECUENCIA INCORRECTA", false);
            indiceJugador = 0;
            mostrandoSecuencia = true;   // bloquea los clics mientras se prepara la secuencia nueva
            ctx.IniciarCorrutina(ReiniciarSecuenciaNueva());
        }
    }

    private IEnumerator FlashBoton(int idx)
    {
        if (botones[idx] != null) botones[idx].image.color = encendidos[idx];
        yield return new WaitForSecondsRealtime(0.18f);
        if (botones[idx] != null) botones[idx].image.color = apagados[idx];
    }

    /// <summary>Al fallar, genera una secuencia NUEVA (no repite la misma).</summary>
    private IEnumerator ReiniciarSecuenciaNueva()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        if (detenido) yield break;

        GenerarSecuencia(secuencia.Count);
        yield return ctx.IniciarCorrutina(MostrarSecuencia());
    }
}