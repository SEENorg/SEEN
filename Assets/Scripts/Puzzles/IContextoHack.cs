using System.Collections;
using UnityEngine;

/// <summary>
/// Lo que un minijuego necesita del sistema de hackeo. Los puzzles dependen
/// de esta interfaz y no de HackManager (inversión de dependencias).
/// </summary>
public interface IContextoHack
{
    Transform Contenido { get; }
    int DificultadBase { get; }
    int IndiceModulo { get; }
    bool InputBloqueado { get; }
    bool EnTransicion { get; }
    void MostrarFeedback(string texto, bool exito);
    void CompletarModulo();
    void Regenerar();
    Coroutine IniciarCorrutina(IEnumerator rutina);

    /// <summary>Texto del acceso que se está hackeando, por ejemplo "ACCESO: PUERTA 1".</summary>
    string TextoAcceso { get; }

    /// <summary>Progreso de la cadena de módulos, por ejemplo "MÓDULO 1/3".</summary>
    string TextoModulo { get; }

    /// <summary>
    /// Muestra u oculta el marco del hackeo (fondo del panel, borde azul, título, contador y mensaje de abajo).
    /// Un puzzle que dibuja su propio marco lo oculta en Construir y lo vuelve a mostrar en Detener.
    /// </summary>
    void MostrarMarco(bool visible);
}