using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using static HackUI;

/// <summary>
/// Minijuego: sincronizar una señal. Un marcador va y viene sobre una pista y hay que
/// detenerlo dentro de la zona verde (con el botón o con Espacio). Cada acierto mueve la zona
/// y acelera el marcador; un fallo reinicia el contador.
/// </summary>
public class PuzzleSincronizar : IPuzzle
{
    private readonly IContextoHack ctx;

    private RectTransform marcador;
    private RectTransform zona;
    private Text textoContador;
    private float tiempo;
    private float velocidad;
    private float ancho;
    private int aciertos;
    private int objetivo;
    private bool activo;

    public PuzzleSincronizar(IContextoHack ctx) { this.ctx = ctx; }

    public void Construir()
    {
        Transform contenido = ctx.Contenido;

        CrearTexto(contenido, "LblSinc", "SINCRONIZÁ LA SEÑAL EN LA ZONA VERDE", 26, TextAnchor.MiddleCenter,
                   new Vector2(0f, 172f), new Vector2(760f, 36f), ColAcento);
        CrearTexto(contenido, "LblSincInfo", "Hacé clic en el botón (o presioná Espacio) cuando el marcador esté dentro", 18,
                   TextAnchor.MiddleCenter, new Vector2(0f, 138f), new Vector2(760f, 26f), new Color(0.6f, 0.7f, 0.8f));

        ancho = 620f;
        objetivo = 2 + ctx.DificultadBase;                 // 2, 3 o 4 señales
        aciertos = 0;
        tiempo = 0f;
        velocidad = 380f + ctx.DificultadBase * 90f;
        activo = true;

        // Pista
        GameObject pista = NuevoUI(contenido, "PistaSinc", new Vector2(0f, 55f), new Vector2(ancho, 34f));
        pista.AddComponent<Image>().color = new Color(0.09f, 0.10f, 0.13f);

        // Zona verde (se mueve en cada acierto)
        float anchoZona = Mathf.Max(90f, 190f - ctx.DificultadBase * 40f);
        zona = (RectTransform)NuevoUI(contenido, "ZonaSinc",
            new Vector2(0f, 55f), new Vector2(anchoZona, 34f)).transform;
        zona.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.75f, 0.35f, 0.55f);

        // Marcador
        marcador = (RectTransform)NuevoUI(contenido, "MarcadorSinc",
            new Vector2(0f, 55f), new Vector2(8f, 52f)).transform;
        marcador.gameObject.AddComponent<Image>().color = ColAcento;

        // Contador
        textoContador = CrearTexto(contenido, "ContadorSinc",
            "SEÑALES SINCRONIZADAS: 0/" + objetivo, 22, TextAnchor.MiddleCenter,
            new Vector2(0f, -30f), new Vector2(560f, 32f), new Color(0.85f, 0.9f, 0.95f));

        Button boton = CrearBoton(contenido, "BtnSincronizar", "SINCRONIZAR", new Vector2(0f, -90f),
                                  new Vector2(280f, 60f));
        boton.onClick.AddListener(IntentarSincronizar);
    }

    /// <summary>Se llama cada frame: mueve el marcador y lee la tecla Espacio.</summary>
    public void Actualizar()
    {
        if (!activo) return;

        if (marcador == null)
        {
            activo = false;
            return;
        }

        tiempo += Time.unscaledDeltaTime;
        float x = Mathf.PingPong(tiempo * velocidad, ancho) - ancho / 2f;
        marcador.anchoredPosition = new Vector2(x, marcador.anchoredPosition.y);

        if (LeerEspacio())
            IntentarSincronizar();
    }

    /// <summary>Se llama al cerrar el hackeo o cambiar de módulo.</summary>
    public void Detener()
    {
        activo = false;
    }

    private void IntentarSincronizar()
    {
        if (!activo || marcador == null || zona == null) return;

        float posMarcador = marcador.anchoredPosition.x;
        float anchoZona = zona.sizeDelta.x;
        float posZona = zona.anchoredPosition.x;

        if (Mathf.Abs(posMarcador - posZona) <= anchoZona / 2f)
        {
            aciertos++;
            velocidad += 90f;

            float nuevoX = Random.Range(-ancho / 2f + anchoZona / 2f, ancho / 2f - anchoZona / 2f);
            zona.anchoredPosition = new Vector2(nuevoX, zona.anchoredPosition.y);

            if (textoContador != null)
                textoContador.text = "SEÑALES SINCRONIZADAS: " + aciertos + "/" + objetivo;

            if (aciertos >= objetivo)
            {
                activo = false;
                ctx.MostrarFeedback("MÓDULO COMPLETADO", true);
                ctx.CompletarModulo();
            }
            else
            {
                ctx.MostrarFeedback("SEÑAL SINCRONIZADA " + aciertos + "/" + objetivo, true);
            }
        }
        else
        {
            aciertos = 0;
            if (textoContador != null)
                textoContador.text = "SEÑALES SINCRONIZADAS: 0/" + objetivo;
            ctx.MostrarFeedback("SEÑAL PERDIDA", false);
        }
    }

    private bool LeerEspacio()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        return kb != null && kb.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }
}