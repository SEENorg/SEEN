using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// La tarjeta común de los puzzles de hackeo: fondo redondeado, título con el nombre del
/// acceso ("ACCESO: PUERTA 3"), subtítulo, caja de estado y pie con "SISTEMA ACTIVO" y el
/// contador de módulos. Cada puzzle solo agrega su contenido en el centro.
/// También crea botones que se iluminan al seleccionarlos (mando, teclado o ratón).
/// </summary>
public class TarjetaHackUI
{
    public readonly Color ColorTarjeta = new Color(0.035f, 0.055f, 0.075f, 0.98f);
    public readonly Color ColorBordeTarjeta = new Color(0.16f, 0.22f, 0.27f);
    public readonly Color ColorCaja = new Color(0.06f, 0.13f, 0.18f);
    public readonly Color ColorTextoSuave = new Color(0.55f, 0.65f, 0.72f);
    public readonly Color ColorBoton = new Color(0.055f, 0.15f, 0.21f);
    public readonly Color ColorBotonSeleccion = new Color(0.08f, 0.22f, 0.30f);
    public readonly Color ColorBordeBoton = new Color(0.11f, 0.24f, 0.31f);

    private readonly CajasRedondeadasUI cajas;
    private readonly Transform raiz;
    private readonly Text textoEstado;
    private readonly Image bordeEstado;

    /// <summary>Transform de la tarjeta: aquí se agregan los elementos del puzzle (centro = 0, 0).</summary>
    public Transform Raiz
    {
        get { return raiz; }
    }

    public TarjetaHackUI(IContextoHack ctx, CajasRedondeadasUI cajas, float ancho, float alto, string subtitulo)
    {
        this.cajas = cajas;
        float arriba = alto / 2f;

        Image borde = cajas.CrearCaja(ctx.Contenido, "Tarjeta", new Vector2(0f, 20f), new Vector2(ancho, alto),
                                      ColorTarjeta, ColorBordeTarjeta, 1f);
        raiz = borde.transform.parent;

        string titulo = ctx.TextoAcceso;
        if (string.IsNullOrEmpty(titulo)) titulo = "SEEN // ACCESO";
        Text textoTitulo = HackUI.CrearTexto(raiz, "Titulo", titulo, 28, TextAnchor.MiddleCenter,
                                             new Vector2(0f, arriba - 35f), new Vector2(ancho - 40f, 36f),
                                             HackUI.ColAcento);
        textoTitulo.fontStyle = FontStyle.Bold;

        HackUI.CrearTexto(raiz, "Subtitulo", subtitulo, 14, TextAnchor.MiddleCenter,
                          new Vector2(0f, arriba - 64f), new Vector2(ancho - 40f, 22f), ColorTextoSuave);

        bordeEstado = cajas.CrearCaja(raiz, "CajaEstado", new Vector2(0f, arriba - 110f),
                                      new Vector2(ancho - 60f, 46f), ColorCaja, BordeCeleste(), 1.2f);
        textoEstado = HackUI.CrearTexto(bordeEstado.transform.parent, "TextoEstado", "", 17,
                                        TextAnchor.MiddleCenter, Vector2.zero, new Vector2(ancho - 80f, 40f),
                                        HackUI.ColAcento);
        textoEstado.fontStyle = FontStyle.Bold;

        string modulo = ctx.TextoModulo;
        if (string.IsNullOrEmpty(modulo)) modulo = "MÓDULO DE ACCESO";
        float abajo = -alto / 2f + 25f;
        float mitad = (ancho - 60f) / 4f;
        HackUI.CrearTexto(raiz, "Estado", "• SISTEMA ACTIVO", 14, TextAnchor.MiddleLeft,
                          new Vector2(-mitad, abajo), new Vector2(mitad * 2f, 22f), HackUI.ColAcento);
        HackUI.CrearTexto(raiz, "Modulo", modulo, 14, TextAnchor.MiddleRight,
                          new Vector2(mitad, abajo), new Vector2(mitad * 2f, 22f), ColorTextoSuave);
    }

    /// <summary>Escribe un mensaje en la caja de estado con su color (borde incluido).</summary>
    public void MostrarEstado(string mensaje, Color color)
    {
        if (textoEstado != null)
        {
            textoEstado.text = mensaje;
            textoEstado.color = color;
        }
        if (bordeEstado != null) bordeEstado.color = color;
    }

    /// <summary>Mensaje normal: texto celeste y borde celeste suave.</summary>
    public void MostrarEstadoNormal(string mensaje)
    {
        MostrarEstado(mensaje, HackUI.ColAcento);
        if (bordeEstado != null) bordeEstado.color = BordeCeleste();
    }

    /// <summary>
    /// Crea un botón redondeado que se ilumina al seleccionarlo.
    /// usarTinte = true: el Button aclara el fondo al seleccionarlo (normal → seleccion).
    /// usarTinte = false: el fondo queda en "normal" y el puzzle lo cambia a mano (luces).
    /// </summary>
    public Button CrearBoton(Transform padre, string nombre, Vector2 posicion, Vector2 tamano,
                             Color normal, Color seleccion, Color borde, string valor,
                             Action<string> alPresionar, bool usarTinte)
    {
        Image imagenBorde = cajas.CrearCaja(padre, nombre, posicion, tamano, Color.white, borde, 1.4f);
        GameObject go = imagenBorde.transform.parent.gameObject;
        Image fondo = go.GetComponent<Image>();

        Button boton = go.AddComponent<Button>();
        boton.targetGraphic = fondo;
        if (usarTinte)
        {
            boton.transition = Selectable.Transition.ColorTint;
            ColorBlock colores = boton.colors;
            colores.normalColor = normal;
            colores.highlightedColor = seleccion;
            colores.selectedColor = seleccion;
            colores.pressedColor = Color.Lerp(seleccion, Color.white, 0.25f);
            colores.disabledColor = Color.Lerp(normal, Color.black, 0.4f);
            colores.colorMultiplier = 1f;
            colores.fadeDuration = 0.06f;
            boton.colors = colores;
        }
        else
        {
            boton.transition = Selectable.Transition.None;
            fondo.color = normal;
        }

        Navigation navegacion = boton.navigation;
        navegacion.mode = Navigation.Mode.Automatic;
        boton.navigation = navegacion;

        TeclaCodigoUI tecla = go.AddComponent<TeclaCodigoUI>();
        tecla.Configurar(valor, imagenBorde, borde, HackUI.ColAcento, alPresionar);
        boton.onClick.AddListener(tecla.Presionar);
        return boton;
    }

    private Color BordeCeleste()
    {
        Color color = HackUI.ColAcento;
        color.a = 0.85f;
        return color;
    }
}
