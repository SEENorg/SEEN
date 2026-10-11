using UnityEngine;

/// <summary>
/// Meta del nivel: al tocarla se muestra la pantalla de NIVEL COMPLETADO
/// (GameFeedback se encarga de pausar el juego y ofrecer reintentar/menú).
/// </summary>
public class LevelGoal : MonoBehaviour
{
    /// <summary>Evento estático: lo usan las ayudas en pantalla para ocultarse.</summary>
    public static event System.Action OnGoalReached;

    private bool completed;

    private void OnTriggerEnter(Collider other)
    {
        if (completed) return;
        if (!other.CompareTag("Player")) return;

        completed = true;
        OnGoalReached?.Invoke();
        GameFeedback.NivelCompletado();
    }
}
