/// <summary>
/// Una condición que debe cumplirse para que PuzzleCodigo muestre el código.
/// PuzzleCodigo solo conoce esta interfaz, así que para cambiar la regla NO se edita
/// PuzzleCodigo: se crea un script nuevo que la implemente y se pone en el MISMO objeto
/// que PuzzleCodigo (la pantalla). Si no hay ninguno, el código se puede habilitar siempre.
///
/// Ejemplo (script nuevo, por ejemplo CondicionTerminalResuelto.cs):
///
///   public class CondicionTerminalResuelto : MonoBehaviour, ICondicionCodigo
///   {
///       public HackTerminal terminal;   // se arrastra en el Inspector
///       public bool SeCumple() { return terminal != null &amp;&amp; terminal.resuelto; }
///   }
/// </summary>
public interface ICondicionCodigo
{
    /// <summary>True si ya se puede habilitar el código.</summary>
    bool SeCumple();
}
