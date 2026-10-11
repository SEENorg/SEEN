using UnityEngine;

/// <summary>
/// Meta del nivel de pruebas: al tocarla el jugador completa el recorrido.
/// Más adelante cargará la siguiente escena / registrará el progreso.
/// </summary>
public class LevelGoal : MonoBehaviour
{
    /// <summary>Evento estático: se dispara al alcanzar la meta.</summary>
    public static event System.Action OnGoalReached;

    private bool completed;

    private void OnTriggerEnter(Collider other)
    {
        if (completed) return;
        if (!other.CompareTag("Player")) return;

        completed = true;
        OnGoalReached?.Invoke();
        Debug.Log("<color=green>[LevelGoal] ¡META ALCANZADA! Todas las mecánicas dominadas.</color>");
    }
}
