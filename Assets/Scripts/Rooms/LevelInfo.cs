using UnityEngine;

/// <summary>
/// Información del nivel: nombre para mostrar, escena siguiente a cargar
/// al completarlo (vacía si es el último nivel) y dificultad de los puzzles
/// (0 = tutorial, 1 = medio, 2 = difícil).
/// </summary>
public class LevelInfo : MonoBehaviour
{
    [Tooltip("Nombre visible del nivel")]
    public string nombreNivel = "Nivel 1";

    [Tooltip("Escena a cargar al completar (vacía = no hay siguiente)")]
    public string escenaSiguiente = "";

    [Tooltip("Dificultad de los puzzles: 0 = fácil (tutorial), 1 = medio, 2 = difícil")]
    public int dificultad;

    private void Awake()
    {
        // Se escribe en Awake (antes que el Start de GameFeedback) para
        // evitar problemas de orden entre componentes.
        GameFeedback.NombreNivelActual = nombreNivel;
        GameFeedback.EscenaSiguiente = escenaSiguiente;
        HackManager.DificultadBase = dificultad;
    }
}

