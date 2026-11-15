using UnityEngine;

/// Contrato para todo objeto con el que el jugador puede interactuar
/// mirándolo y presionando E (terminales, cofres...).
public interface IInteractuable
{
    ///Texto que se muestra al mirar el objeto.
    string TextoPrompt { get; }

    /// Si es false, la mirilla ignora el objeto (ya resuelto, bloqueado...).
    bool PuedeInteractuar { get; }

    /// Se ejecuta cuando el jugador presiona E sobre el objeto.
    void Interactuar();
}