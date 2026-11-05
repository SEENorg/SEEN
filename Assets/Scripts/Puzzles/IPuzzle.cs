using UnityEngine;

/// <summary>Estrategia: un minijuego del sistema de hackeo.</summary>
public interface IPuzzle
{
    /// <summary>Dibuja el minijuego y arranca su lógica.</summary>
    void Construir();
    /// <summary>Se llama cada frame mientras el hackeo está abierto.</summary>
    void Actualizar();
    /// <summary>Se llama al cerrar el hackeo o cambiar de módulo.</summary>
    void Detener();
}
