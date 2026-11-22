namespace Game.Scanner
{
    /// <summary>
    /// Lo que el controlador necesita de la pantalla. No dice cómo se dibuja:
    /// puede ser un Canvas de uGUI, UI Toolkit o un HUD de prueba en OnGUI.
    ///
    /// CONTRATO:
    /// - Mostrar/Ocultar hacen su propio fade usando ScannerConfig.duracionFade
    ///   y tiempo sin escalar (el controlador cuenta ese mismo tiempo).
    /// - Ninguno de los métodos lanza excepciones si se llama dos veces seguidas.
    /// - ActualizarEnemigo puede llegar en cualquier momento después de MostrarEnemigo.
    /// </summary>
    public interface IScannerView
    {
        void Mostrar();
        void Ocultar();
        void MostrarEnemigo(EnemyScanData datos);
        void ActualizarEnemigo(EnemyScanData datos);
        void LimpiarEnemigo();
    }
}
