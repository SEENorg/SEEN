using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static HackUI;

/// <summary>
/// Minijuego: pide el código que muestra la pantalla de la sala (PuzzleCodigo).
/// Usa la misma interfaz de hackeo de pantalla completa que los demás módulos.
/// </summary>
public class PuzzleIngresarCodigo : IPuzzle
{
    private readonly IContextoHack ctx;
    private readonly Color colorCampo = new Color(0.10f, 0.13f, 0.17f);

    public PuzzleIngresarCodigo(IContextoHack ctx) { this.ctx = ctx; }

    public void Actualizar() { }
    public void Detener() { }

    public void Construir()
    {
        Transform contenido = ctx.Contenido;

        CrearTexto(contenido, "LblCodigo", "INGRESA EL CÓDIGO DE ACCESO", 30, TextAnchor.MiddleCenter,
                   new Vector2(0f, 150f), new Vector2(760f, 44f), ColAcento);
        CrearTexto(contenido, "LblCodigoInfo", "El código aparece en una pantalla de la sala", 20, TextAnchor.MiddleCenter,
                   new Vector2(0f, 105f), new Vector2(760f, 32f), new Color(0.6f, 0.7f, 0.8f));

        // Campo de texto
        GameObject campoGO = NuevoUI(contenido, "InputCodigo", new Vector2(-80f, -20f), new Vector2(380f, 62f));
        Image campoImagen = campoGO.AddComponent<Image>();
        campoImagen.color = colorCampo;
        InputField campo = campoGO.AddComponent<InputField>();
        campo.textComponent = CrearTexto(campoGO.transform, "Text", "", 34, TextAnchor.MiddleLeft,
                                         new Vector2(12f, 0f), new Vector2(360f, 56f), Color.white);
        campo.targetGraphic = campoImagen;
        campo.characterLimit = 8;

        // Botón
        Button boton = CrearBoton(contenido, "BtnCodigo", "VERIFICAR", new Vector2(200f, -20f),
                                  new Vector2(200f, 62f));
        boton.onClick.AddListener(() => Verificar(campo));

        // Deja el cursor listo para escribir
        if (EventSystem.current != null)
            campo.ActivateInputField();
    }

    /// <summary>Compara lo escrito con el código de la pantalla de la sala.</summary>
    private void Verificar(InputField campo)
    {
        string intento = campo.text;
        if (intento == null) intento = "";
        intento = intento.Trim();

        IPuzzleCodigo pantalla = BuscarPantallaCodigo();
        if (pantalla == null)
        {
            ctx.MostrarFeedback("NO HAY PANTALLA DE CÓDIGO EN LA SALA", false);
            return;
        }

        if (intento == pantalla.Codigo)
        {
            ctx.MostrarFeedback("CÓDIGO CORRECTO", true);
            ctx.CompletarModulo();
        }
        else
        {
            ctx.MostrarFeedback("CÓDIGO INCORRECTO", false);
            campo.text = "";
        }
    }

    /// <summary>Busca en la escena el script que implementa IPuzzleCodigo.</summary>
    private IPuzzleCodigo BuscarPantallaCodigo()
    {
        MonoBehaviour[] todos = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (MonoBehaviour mb in todos)
        {
            IPuzzleCodigo pantalla = mb as IPuzzleCodigo;
            if (pantalla != null) return pantalla;
        }
        return null;
    }
}
