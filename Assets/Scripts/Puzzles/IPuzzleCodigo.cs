/// <summary>
/// Contrato del puzzle del código de la sala: una pantalla muestra "CÓDIGO BLOQUEADO"
/// hasta que algo en el juego lo habilita; entonces muestra el código que abre la puerta.
/// Quien necesite habilitarlo (un terminal, un botón, un trigger...) depende de esta
/// interfaz y no de la clase concreta (igual que los puzzles dependen de IContextoHack).
/// </summary>
public interface IPuzzleCodigo
{
    /// <summary>El código generado al azar (por ejemplo "4821").</summary>
    string Codigo { get; }

    /// <summary>True cuando la pantalla ya muestra el código (no está bloqueado).</summary>
    bool CodigoHabilitado { get; }

    /// <summary>
    /// Intenta habilitar el código. La implementación decide con un condicional
    /// si realmente se puede habilitar en este momento.
    /// </summary>
    void HabilitarCodigo();

    /// <summary>Vuelve a bloquear el código (la pantalla muestra "CÓDIGO BLOQUEADO").</summary>
    void DeshabilitarCodigo();
}
