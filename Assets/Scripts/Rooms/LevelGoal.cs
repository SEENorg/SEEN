using UnityEngine;

/// <summary>
/// Meta del nivel: al tocarla se muestra la pantalla de NIVEL COMPLETADO
/// (GameFeedback se encarga de pausar el juego y ofrecer reintentar/menú).
/// El nivel solo termina cuando todos los terminales de hackeo requeridos
/// están resueltos; si falta alguno, se avisa al jugador y el nivel continúa.
/// </summary>
public class LevelGoal : MonoBehaviour
{
    /// <summary>Evento estático: lo usan las ayudas en pantalla para ocultarse.</summary>
    public static event System.Action OnGoalReached;

    [Header("Condición para terminar el nivel")]
    [Tooltip("Terminales que deben estar resueltos. Si la lista está vacía, se exigen TODOS los terminales de la escena.")]
    [SerializeField] private HackTerminal[] terminalesRequeridos;

    [Tooltip("Segundos que pasan entre cumplir la condición y mostrar el NIVEL COMPLETADO (para ver abrirse el cofre)")]
    [SerializeField] private float retrasoFinal = 1.5f;

    private bool completed;
    private float tiempoUltimoAviso = -10f;

    private void Start()
    {
        // Sin lista manual: se exigen todos los terminales que haya en la escena
        if (terminalesRequeridos == null || terminalesRequeridos.Length == 0)
            terminalesRequeridos = FindObjectsByType<HackTerminal>(FindObjectsSortMode.None);
    }

    private void OnTriggerEnter(Collider other)
    {
        IntentarCompletar(other);
    }

    // Si el jugador se queda dentro de la meta mientras resuelve el último
    // terminal, el nivel termina apenas lo completa (no hace falta salir y volver a entrar)
    private void OnTriggerStay(Collider other)
    {
        IntentarCompletar(other);
    }

    private void IntentarCompletar(Collider other)
    {
        if (completed) return;
        if (!other.CompareTag("Player")) return;

        int pendientes = ContarPendientes();
        if (pendientes > 0)
        {
            AvisarPendientes(pendientes);
            return;
        }

        completed = true;
        if (retrasoFinal > 0f)
            Invoke(nameof(Finalizar), retrasoFinal);
        else
            Finalizar();
    }

    private int ContarPendientes()
    {
        int pendientes = 0;
        for (int i = 0; i < terminalesRequeridos.Length; i++)
        {
            HackTerminal terminal = terminalesRequeridos[i];
            if (terminal != null && !terminal.resuelto)
                pendientes++;
        }
        return pendientes;
    }

    private void AvisarPendientes(int pendientes)
    {
        // Un aviso cada 4 segundos, para no llenar la pantalla de mensajes
        if (Time.time - tiempoUltimoAviso < 4f) return;
        tiempoUltimoAviso = Time.time;
        GameFeedback.Mensaje("Antes de salir debes hackear todos los terminales (faltan " + pendientes + ")");
    }

    private void Finalizar()
    {
        if (OnGoalReached != null) OnGoalReached();
        GameFeedback.NivelCompletado();
    }
}