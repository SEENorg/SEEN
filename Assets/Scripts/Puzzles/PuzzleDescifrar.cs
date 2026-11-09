using UnityEngine;
using UnityEngine.UI;
using static HackUI;

/// <summary>
/// Minijuego: descifrar un código César. Se muestra una palabra cifrada y su desplazamiento;
/// el jugador escribe la palabra original. (La rueda César, que se abre con TAB, es una ayuda
/// independiente: este puzzle no depende de ella.)
/// </summary>
public class PuzzleDescifrar : IPuzzle
{
    private readonly IContextoHack ctx;

    private readonly string[] palabras = { "PUERTA", "DATOS", "SISTEMA", "RED", "CLAVE", "HACKER",
                                           "SEEN", "NUCLEO", "BOTS", "SENAL", "VIRUS", "CONEXION" };

    private readonly Color colorCampo = new Color(0.10f, 0.13f, 0.17f);

    public PuzzleDescifrar(IContextoHack ctx) { this.ctx = ctx; }

    public void Actualizar() { }
    public void Detener() { }

    public void Construir()
    {
        Transform contenido = ctx.Contenido;

        // Dificultad: tutorial (0) fácil → Nivel 3 (2) difícil
        int dificultad = Mathf.Clamp(ctx.DificultadBase + ctx.IndiceModulo, 0, 5);
        int desplazamiento = 3 + dificultad;                       // 3..8

        string palabra = palabras[Random.Range(0, palabras.Length)];
        string codigoCifrado = Desplazar(palabra, desplazamiento);

        CrearTexto(contenido, "Lbl1", "CÓDIGO INTERCEPTADO", 24, TextAnchor.MiddleCenter,
                   new Vector2(0f, 180f), new Vector2(700f, 36f), new Color(0.7f, 0.8f, 0.9f));

        Text codigo = CrearTexto(contenido, "Codigo", codigoCifrado, 54, TextAnchor.MiddleCenter,
                                 new Vector2(0f, 115f), new Vector2(700f, 72f), ColAcento);
        codigo.fontStyle = FontStyle.Bold;

        CrearTexto(contenido, "Lbl2", "Desplazamiento César: -" + desplazamiento, 20, TextAnchor.MiddleCenter,
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
            string valor = textoIngresado.Trim().ToUpperInvariant();

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

    /// <summary>Cifra el texto desplazando cada letra n posiciones hacia adelante.</summary>
    private string Desplazar(string texto, int n)
    {
        char[] letras = texto.ToCharArray();
        for (int i = 0; i < letras.Length; i++)
        {
            if (letras[i] >= 'A' && letras[i] <= 'Z')
                letras[i] = (char)('A' + ((letras[i] - 'A' + n) % 26));
        }
        return new string(letras);
    }
}