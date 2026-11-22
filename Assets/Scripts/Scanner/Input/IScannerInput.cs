namespace Game.Scanner
{
    /// <summary>
    /// Fuente de la orden "abrir o cerrar la pantalla".
    /// Puede ser el teclado, un mando, un botón en pantalla o un script de tutorial.
    /// CONTRATO: devuelve true solo en el frame en que se pidió alternar
    /// (no mientras la tecla siga presionada).
    /// </summary>
    public interface IScannerInput
    {
        bool AlternarPresionado();
    }
}
