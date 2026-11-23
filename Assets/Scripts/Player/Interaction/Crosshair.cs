using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Mirilla (crosshair) en primera persona:
/// un pequeño círculo al centro de la pantalla que crece y se pone verde
/// cuando el jugador está mirando un objeto con el que puede interactuar
/// (la tableta naranja de hackeo). Al presionar E sobre ella, inicia el hackeo.
/// </summary>
public class Crosshair : MonoBehaviour
{
    [Header("Tamaños")]
    [Tooltip("Radio del círculo normal (píxeles)")]
    [SerializeField] private float radioNormal = 5f;
    [Tooltip("Radio cuando apunta a un objeto interactuable")]
    [SerializeField] private float radioActivo = 13f;
    [Tooltip("Velocidad de crecimiento/achicamiento")]
    [SerializeField] private float suavizado = 14f;

    [Header("Detección")]
    [Tooltip("Distancia máxima de interacción")]
    [SerializeField] private float alcance = 7f;

    [Header("Colores")]
    [SerializeField] private Color colorNormal = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color colorActivo = new Color(0.2f, 1f, 0.4f, 0.95f);
    [SerializeField] private Color colorListo = new Color(0.25f, 0.85f, 1f, 0.9f);

    private IInteractuable objetivo;
    private float radioActual;
    private Texture2D texturaCirculo;
    private IPlayerInput entrada;

    private void Awake()
{
    texturaCirculo = CrearTexturaAnillo(128, 10);
    radioActual = radioNormal;

    PlayerController jugador = Object.FindFirstObjectByType<PlayerController>();

    if (jugador != null)
    {
        entrada = jugador.Entrada;
    }
    else
    {
        Debug.LogError("No se encontró PlayerController para obtener la entrada.");
    }
}

    private void Update()
    {
        objetivo = null;

        if (Camera.main != null && !HackManager.Abierto)
        {
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, alcance, ~0, QueryTriggerInteraction.Ignore))
            {
                var interactuable = hit.collider.GetComponentInParent<IInteractuable>();
                if (interactuable != null && interactuable.PuedeInteractuar)
                    objetivo = interactuable;
            }

            if (objetivo != null)
            {
                HackManager.MostrarPrompt(TextoConMando(objetivo.TextoPrompt));
                if (InteraccionPresionada())
                {
                    objetivo.Interactuar();
                }
                }
        }

        // Animación de tamaño
        float objetivoRadio = radioNormal;
        if (objetivo != null) objetivoRadio = radioActivo;
        radioActual = Mathf.Lerp(radioActual, objetivoRadio, Time.deltaTime * suavizado);
    }

    /// <summary>
    /// E en el teclado o Y en el mando. Y también se lee directo del mando por si la
    /// acción "Interact" del PlayerInput no tiene el mando activo en ese momento.
    /// </summary>
    private bool InteraccionPresionada()
    {
        if (entrada != null && entrada.InteraccionPresionada()) return true;
#if ENABLE_INPUT_SYSTEM
        Gamepad mando = Gamepad.current;
        if (mando != null && mando.buttonNorth.wasPressedThisFrame) return true;
#endif
        return false;
    }

    /// <summary>Si hay un mando conectado, el aviso dice "Presione E o Y ..." en vez de solo "Presione E ...".</summary>
    private string TextoConMando(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto;
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null) return texto.Replace("Presione E", "Presione E o Y");
#endif
        return texto;
    }

    private void OnGUI()
    {
        if (HackManager.Abierto || EstadoMenus.HayMenuAbierto) return;

        Color c = colorNormal;
        if (objetivo != null) c = colorActivo;
        else if (GameFeedback.NivelCompletadoVisible) c = colorListo;

        Color previa = GUI.color;
        GUI.color = c;

        float d = radioActual * 2f;
        Rect r = new Rect((Screen.width - d) / 2f, (Screen.height - d) / 2f, d, d);
        GUI.DrawTexture(r, texturaCirculo);

        GUI.color = previa;
    }

    /// <summary>Genera una textura de anillo (círculo hueco) para la mirilla.</summary>
    private Texture2D CrearTexturaAnillo(int size, int grosor)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float centro = (size - 1) / 2f;
        float radioExterno = size / 2f - 1f;
        float radioInterno = radioExterno - grosor;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - centro;
                float dy = y - centro;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                bool anillo = dist <= radioExterno && dist >= radioInterno;
                if (anillo) tex.SetPixel(x, y, Color.white);
                else tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
            }
        }

        tex.Apply();
        return tex;
    }
}