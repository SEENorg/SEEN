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
}