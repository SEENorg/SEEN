using UnityEngine;
using UnityEngine.UI;
using static HackUI;

/// <summary>
/// Minijuego: descifrar un código César SIMPLE (desplazamiento +1). Se muestra una palabra cifrada;
/// para descifrarla hay que avanzar cada letra 1 lugar (B pasa a ser C).
/// Las palabras las da un IBancoPalabras (por defecto, buen uso de redes sociales).
/// Mientras este puzzle está en pantalla (Activo), la tabla de ayuda se abre con TAB.
/// </summary>
public class PuzzleDescifrar : IPuzzle
{
    private readonly IContextoHack ctx;

    // De dónde salen las palabras (se le puede dar otro banco sin tocar este puzzle)
    private readonly IBancoPalabras banco;

    /// <summary>True solo mientras este puzzle está en pantalla. La tabla del TAB lo consulta.</summary>
    public static bool Activo { get; private set; }

    // Cuántas letras hay que AVANZAR para descifrar (B → C con 1)
    private const int Desplazamiento = 1;

    private readonly Color colorCampo = new Color(0.10f, 0.13f, 0.17f);

    // La fábrica sigue usando este constructor: por defecto, el banco de redes sociales
    public PuzzleDescifrar(IContextoHack ctx) : this(ctx, new BancoPalabrasRedes()) { }

    // Inyección por constructor: el banco llega desde afuera
    public PuzzleDescifrar(IContextoHack ctx, IBancoPalabras banco)
    {
        this.ctx = ctx;
        this.banco = banco;
    }

    public void Actualizar() { }
    public void Detener() { Activo = false; }

    public void Construir()
    {
        Activo = true;
        Transform contenido = ctx.Contenido;

        // Nivel: tutorial (0) fácil → Nivel 3 (2 o más) difícil; define qué tan larga es la palabra
        int nivel = Mathf.Clamp(ctx.DificultadBase + ctx.IndiceModulo, 0, 5);
        string palabra = banco.ObtenerPalabra(nivel);

        // Se cifra RETROCEDIENDO 1 letra; el jugador descifra AVANZANDO 1 (si ve B, es C)
        string codigoCifrado = Desplazar(palabra, -Desplazamiento);

        CrearTexto(contenido, "Lbl1", "CÓDIGO INTERCEPTADO", 24, TextAnchor.MiddleCenter,
                   new Vector2(0f, 180f), new Vector2(700f, 36f), new Color(0.7f, 0.8f, 0.9f));

        Text codigo = CrearTexto(contenido, "Codigo", codigoCifrado, 54, TextAnchor.MiddleCenter,
                                 new Vector2(0f, 115f), new Vector2(700f, 72f), ColAcento);
        codigo.fontStyle = FontStyle.Bold;

        CrearTexto(contenido, "Lbl2", "Desplazamiento César: +" + Desplazamiento + "  (avanza cada letra 1 lugar)", 20, TextAnchor.MiddleCenter,
                   new Vector2(0f, 58f), new Vector2(700f, 32f), new Color(0.6f, 0.7f, 0.8f));
        CrearTexto(contenido, "Lbl3", "Escribe el código original y presiona VERIFICAR", 20, TextAnchor.MiddleCenter,
                   new Vector2(0f, 20f), new Vector2(700f, 32f), new Color(0.6f, 0.7f, 0.8f));

        // Campo de texto
        GameObject campoGO = NuevoUI(contenido, "Input", new Vector2(-80f, -50f), new Vector2(380f, 62f));
        Image campoImagen = campoGO.AddComponent<Image>();
        campoImagen.color = colorCampo;
        InputField campo = campoGO.AddComponent<InputField>();
        campo.textComponent = CrearTexto(campoGO.transform, "Text", "", 30, TextAnchor.MiddleLeft,
                                         new Vector2(12f, 0f), new Vector2(360f, 56f), Color.white);
        campo.targetGraphic = campoImagen;

        Button boton = CrearBoton(contenido, "BtnVerificar", "VERIFICAR", new Vector2(200f, -50f),
                                  new Vector2(200f, 62f));
        boton.onClick.AddListener(() =>
        {
            string textoIngresado = campo.text;
            if (textoIngresado == null) textoIngresado = "";
            // Se quedan solo las letras (así no importan espacios, saltos de línea ni mayúsculas al pegar)
            string valor = SoloLetras(textoIngresado);

            if (valor == palabra)
            {
                ctx.MostrarFeedback("MÓDULO COMPLETADO", true);
                ctx.CompletarModulo();
            }
            else
            {
                ctx.MostrarFeedback("CÓDIGO INCORRECTO", false);
            }
        });
    }

    /// <summary>Devuelve solo las letras A-Z en mayúsculas (quita espacios, saltos de línea y símbolos).</summary>
    private string SoloLetras(string texto)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in texto.ToUpperInvariant())
        {
            if (c >= 'A' && c <= 'Z') sb.Append(c);
        }
        return sb.ToString();
    }

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