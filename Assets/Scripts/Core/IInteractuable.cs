using UnityEngine;

/// <summary>
/// Contrato para todo objeto con el que el jugador puede interactuar
/// mirándolo y presionando E (terminales, cofres, palancas...).
/// </summary>
public interface IInteractuable
{
    /// <summary>Texto que se muestra al mirar el objeto.</summary>
    string TextoPrompt { get; }

    /// <summary>Si es false, la mirilla ignora el objeto (ya resuelto, bloqueado...).</summary>
    bool PuedeInteractuar { get; }

    /// <summary>Se ejecuta cuando el jugador presiona E sobre el objeto.</summary>
    void Interactuar();
}