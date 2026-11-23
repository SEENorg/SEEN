using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Ascensor: cuando el jugador entra a la cabina aparece "Presione F para subir".
/// Al presionar F (teclado) o Y (mando), toda la estructura sube hasta yFinal y se queda quieta ahí.
/// Con un mando conectado el aviso dice "Presione F o Y para subir".
///
/// Se pone en el objeto RAÍZ del ascensor (el padre de piso, paredes, etc.).
/// Necesita un Collider con "Is Trigger" marcado que cubra el interior de la cabina.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Ascensor : MonoBehaviour
{
    private enum Estado { EsperandoAbajo, Subiendo, Arriba }

    [Header("Recorrido")]
    [Tooltip("Altura (Y) donde empieza el ascensor")]
    [SerializeField] private float yInicial = 0.03f;
    [Tooltip("Altura (Y) donde se detiene arriba")]
    [SerializeField] private float yFinal = 9.28f;
    [Tooltip("Velocidad de subida (unidades por segundo)")]
    [SerializeField] private float velocidad = 2f;

    [Header("Mensaje en pantalla")]
    [SerializeField] private string textoPrompt = "Presione F para subir";

    private Rigidbody rb;
    private Estado estado = Estado.EsperandoAbajo;
    private bool jugadorDentro;
    private Rigidbody jugadorRb;
    private bool pedirSubida;
    private float destinoY;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;     // lo movemos nosotros, no la física
        rb.useGravity = false;

        // Recorrido = yFinal - yInicial, sumado a donde está el ascensor al empezar
        destinoY = transform.position.y + (yFinal - yInicial);
    }

    private void Update()
    {
        if (estado == Estado.EsperandoAbajo && jugadorDentro && SubidaPresionada())
            pedirSubida = true;
    }

    private void FixedUpdate()
    {
        if (estado == Estado.EsperandoAbajo)
        {
            if (!pedirSubida) return;
            pedirSubida = false;
            estado = Estado.Subiendo;
        }

        if (estado != Estado.Subiendo) return;

        // Sube hacia el destino sin pasarse
        float yActual = rb.position.y;
        float yNueva = Mathf.MoveTowards(yActual, destinoY, velocidad * Time.fixedDeltaTime);
        float delta = yNueva - yActual;

        rb.MovePosition(new Vector3(rb.position.x, yNueva, rb.position.z));
        LlevarAlJugador(delta);

        if (Mathf.Approximately(yNueva, destinoY))
            estado = Estado.Arriba;   // llegó: se queda quieto
    }

    // Mueve al jugador junto con la cabina para que no se quede atrás ni vibre.
    private void LlevarAlJugador(float delta)
    {
        if (!jugadorDentro || jugadorRb == null || delta <= 0f) return;

        jugadorRb.MovePosition(jugadorRb.position + Vector3.up * delta);

        // Anti-lanzamiento: que no salga disparado hacia arriba
        float vSubida = delta / Time.fixedDeltaTime;
        if (jugadorRb.linearVelocity.y > vSubida)
        {
            Vector3 v = jugadorRb.linearVelocity;
            jugadorRb.linearVelocity = new Vector3(v.x, vSubida, v.z);
        }
    }

    // ── Detección del jugador dentro de la cabina ───────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        jugadorDentro = true;
        jugadorRb = other.attachedRigidbody;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        jugadorDentro = false;
        jugadorRb = null;
    }

    // ── Entrada y mensaje ───────────────────────────────────────

    /// <summary>F en el teclado o Y en el mando.</summary>
    private bool SubidaPresionada()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.fKey.wasPressedThisFrame) return true;
        Gamepad mando = Gamepad.current;
        if (mando != null && mando.buttonNorth.wasPressedThisFrame) return true;
        return false;
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }

    /// <summary>Si hay un mando conectado, el aviso también nombra el botón Y.</summary>
    private string TextoAviso()
    {
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null) return textoPrompt.Replace("Presione F", "Presione F o Y");
#endif
        return textoPrompt;
    }

    // Mismo estilo que el aviso "Presione E para hackear".
    // Solo se dibuja esperando abajo con el jugador dentro: al subir desaparece al instante.
    private void OnGUI()
    {
        if (estado != Estado.EsperandoAbajo || !jugadorDentro) return;
        if (HackManager.Abierto || PauseMenu.Pausado) return;

        var estilo = new GUIStyle(GUI.skin.box)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        estilo.normal.textColor = new Color(0.3f, 1f, 0.45f);

        float w = 420f, h = 46f;
        GUI.Box(new Rect((Screen.width - w) / 2f, Screen.height - h - 40f, w, h),
                TextoAviso(), estilo);
    }

    // Para probar sin caminar hasta la cabina: clic derecho en el componente → "Subir ahora"
    [ContextMenu("Subir ahora")]
    private void SubirAhora()
    {
        if (Application.isPlaying && estado == Estado.EsperandoAbajo)
            pedirSubida = true;
    }
}
