using UnityEngine;
using UnityEngine.UI;
/// <summary>Minijuego: encontrar el glifo distinto en una cuadrícula.</summary>
public class PuzzleEscanear : IPuzzle
{
    private readonly IContextoHack ctx;
    private int indiceEscaneado;

    public PuzzleEscanear(IContextoHack ctx)
    {
        this.ctx = ctx;
    }

    public void Actualizar()
    {
    }

    public void Detener()
    {
    }

    public void Construir()
    {
        HackUI.CrearTexto(ctx.Contenido, "LblScan", "HAZ CLIC EN EL GLIFO DIFERENTE", 26, TextAnchor.MiddleCenter,
                   new Vector2(0f, 186f), new Vector2(760f, 36f), HackUI.ColAcento);

        int filas = 4 + Mathf.Clamp(ctx.DificultadBase + ctx.IndiceModulo, 0, 2);
        HackUI.CrearTexto(ctx.Contenido, "LblScanInfo", "Cuadrícula " + filas + "x" + filas, 18, TextAnchor.MiddleCenter,
                   new Vector2(0f, 150f), new Vector2(400f, 26f), new Color(0.6f, 0.7f, 0.8f));

        ConstruirCuadricula(filas);
    }

    private void ConstruirCuadricula(int filas)
    {
        Transform viejo = ctx.Contenido.Find("GridScan");
        if (viejo != null) Object.Destroy(viejo.gameObject);

        float lado = 360f;
        float tam = lado / filas;
        Transform grid = HackUI.NuevoUI(ctx.Contenido, "GridScan", new Vector2(0f, -45f), new Vector2(360f, 360f)).transform;

        indiceEscaneado = Random.Range(0, filas * filas);
        Color colorCelda = new Color(0.13f, 0.15f, 0.19f);

        for (int i = 0; i < filas * filas; i++)
        {
            int f = i / filas;
            int c = i % filas;
            float x = -lado / 2f + tam / 2f + c * tam;
            float y = lado / 2f - tam / 2f - f * tam;
            bool esDiferente = i == indiceEscaneado;

            string glifo;
            if (esDiferente)
                glifo = "◈";
            else
                glifo = "◆";

            Button b = HackUI.CrearBoton(grid, "Celda_" + i, glifo,
                               new Vector2(x, y), new Vector2(tam - 8f, tam - 8f), colorCelda);
            int idx = i;
            b.onClick.AddListener(delegate
            {
                if (idx == indiceEscaneado)
                {
                    ctx.MostrarFeedback("MÓDULO COMPLETADO", true);
                    ctx.CompletarModulo();
                }
                else
                {
                    ctx.MostrarFeedback("GLIFO INCORRECTO — REESCANEANDO", false);
                    ConstruirCuadricula(filas);
                }
            });
        }
    }
}