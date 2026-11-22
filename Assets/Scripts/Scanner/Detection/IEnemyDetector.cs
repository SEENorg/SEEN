using System;

namespace Game.Scanner
{
    /// <summary>
    /// Algo que encuentra enemigos escaneables (raycast, cono, zona, lo que sea).
    ///
    /// CONTRATO:
    /// - Mientras está desactivado no lanza eventos.
    /// - OnEnemyDetected se lanza una vez por objetivo nuevo (no cada frame).
    ///   Si cambia de objetivo, lanza OnEnemyDetected con el nuevo, sin OnEnemyLost antes.
    /// - OnEnemyLost se lanza cuando deja de ver al objetivo actual.
    /// - Desactivar() olvida el objetivo SIN lanzar OnEnemyLost: quien lo desactiva
    ///   ya sabe que la pantalla se está cerrando y limpia su propio estado.
    /// </summary>
    public interface IEnemyDetector
    {
        event Action<IEnemyDataProvider> OnEnemyDetected;
        event Action OnEnemyLost;

        bool Activo { get; }

        void Activar();
        void Desactivar();
    }
}
