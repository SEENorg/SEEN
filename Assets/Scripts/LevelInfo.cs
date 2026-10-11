using UnityEngine;

/// <summary>
/// Información del nivel: nombre para mostrar y escena siguiente a cargar
/// al completarlo (vacía si es el último nivel).
/// </summary>
public class LevelInfo : MonoBehaviour
{
    [Tooltip("Nombre visible del nivel")]
    public string nombreNivel = "Nivel 1";

    [Tooltip("Escena a cargar al completar (vacía = no hay siguiente)")]
    public string escenaSiguiente = "";

    private void Start()
    {
        GameFeedback.NombreNivelActual = nombreNivel;
        GameFeedback.EscenaSiguiente = escenaSiguiente;
    }
}
