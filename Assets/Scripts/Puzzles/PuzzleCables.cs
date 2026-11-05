using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Minijuego: conectar los cables del mismo color.</summary>
public class PuzzleCables : IPuzzle
{
    private readonly IContextoHack ctx;

    public PuzzleCables(IContextoHack ctx)
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
        Transform contenido = ctx.Contenido;
        int cantidad = Mathf.Clamp(3 + ctx.DificultadBase + ctx.IndiceModulo, 3, 6);
        HackUI.CrearTexto(contenido, "LblCables", "CONECTA LOS CABLES DEL MISMO COLOR", 26, TextAnchor.MiddleCenter,
                   new Vector2(0f, 195f), new Vector2(760f, 40f), HackUI.ColAcento);

        Color[] colores = new Color[6];
        colores[0] = new Color(1f, 0.2f, 0.2f);
        colores[1] = new Color(0.2f, 0.8f, 1f);
        colores[2] = new Color(1f, 0.85f, 0.15f);
        colores[3] = new Color(1f, 0.3f, 0.8f);
        colores[4] = new Color(0.4f, 1f, 0.3f);
        colores[5] = new Color(0.7f, 0.4f, 1f);

        Transform izq = HackUI.NuevoUI(contenido, "Izq", new Vector2(-220f, 0f), new Vector2(160f, 300f)).transform;
        Transform der = HackUI.NuevoUI(contenido, "Der", new Vector2(220f, 0f), new Vector2(160f, 300f)).transform;

        List<int> ordenDer = new List<int>();
        for (int i = 0; i < cantidad; i++)
        {
            ordenDer.Add(i);
        }

        for (int i = ordenDer.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = ordenDer[i];
            ordenDer[i] = ordenDer[j];
            ordenDer[j] = temp;
        }

        int resueltos = 0;
        int seleccionado = -1;

        float alto = 300f / cantidad;
        for (int i = 0; i < cantidad; i++)
        {
            float y = 150f - alto * (i + 0.5f);
            int indiceIzq = i;
            float altoBoton = Mathf.Min(46f, alto - 12f);
            Button bIzq = HackUI.CrearBoton(izq, "Cable_" + i, "C" + (i + 1), new Vector2(0f, y),
                                  new Vector2(120f, altoBoton),
                                  colores[i]);
            bIzq.image.color = colores[i];
            bIzq.onClick.AddListener(delegate { seleccionado = indiceIzq; });
        }

        for (int k = 0; k < cantidad; k++)
        {
            int colorIdx = ordenDer[k];
            int indiceDer = k;
            float y = 150f - alto * (k + 0.5f);
            float altoBoton = Mathf.Min(46f, alto - 12f);
            Button bDer = HackUI.CrearBoton(der, "CableR_" + colorIdx, "C" + (colorIdx + 1), new Vector2(0f, y),
                                  new Vector2(120f, altoBoton),
                                  colores[colorIdx]);
            bDer.image.color = colores[colorIdx];
            bDer.onClick.AddListener(delegate
            {
                if (ctx.InputBloqueado) return;
                if (seleccionado < 0)
                {
                    ctx.MostrarFeedback("SELECCIONA UN CABLE DE LA IZQUIERDA", false);
                    return;
                }
                if (colorIdx == seleccionado)
                {
                    RectTransform rtIzq = izq.GetChild(seleccionado) as RectTransform;
                    RectTransform rtDer = der.GetChild(indiceDer) as RectTransform;
                    DibujarCable(contenido, rtIzq, rtDer, colores[colorIdx]);
                    izq.GetChild(seleccionado).GetComponent<Button>().interactable = false;
                    bDer.interactable = false;
                    seleccionado = -1;
                    resueltos++;
                    if (resueltos >= cantidad)
                    {
                        ctx.MostrarFeedback("MÓDULO COMPLETADO", true);
                        ctx.CompletarModulo();
                    }
                }
                else
                {
                    ctx.MostrarFeedback("CABLE INCORRECTO", false);
                    ctx.Regenerar();
                }
            });
        }
    }

    private void DibujarCable(Transform parent, RectTransform a, RectTransform b, Color color)
    {
        GameObject cableGO = new GameObject("Linea", typeof(RectTransform));
        cableGO.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)cableGO.transform;

        Vector2 pa = LocalAlContenedor(a, (RectTransform)parent);
        Vector2 pb = LocalAlContenedor(b, (RectTransform)parent);
        Vector2 delta = pb - pa;
        float dist = delta.magnitude;

        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(dist, 8f);
        rt.anchoredPosition = (pa + pb) / 2f;
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        Image img = cableGO.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    private Vector2 LocalAlContenedor(RectTransform hijo, RectTransform contenedor)
    {
        Vector3 mundo = hijo.TransformPoint(hijo.rect.center);
        return (Vector2)contenedor.InverseTransformPoint(mundo);
    }
}