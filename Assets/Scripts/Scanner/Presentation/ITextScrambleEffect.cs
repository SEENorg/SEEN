using System.Collections;
using TMPro;

namespace Game.Scanner
{
    /// <summary>
    /// Estrategia para hacer aparecer un texto (patrón Strategy).
    /// La vista no sabe si el efecto es hexadecimal, binario, glitch o máquina de
    /// escribir: solo arranca la corrutina que devuelve Revelar.
    ///
    /// CONTRATO (para que cualquier efecto pueda reemplazar a otro):
    /// 1. Al terminar, destino.text es exactamente textoFinal.
    /// 2. Usa tiempo sin escalar (Time.unscaledDeltaTime), para funcionar en pausa.
    /// 3. Si destino se destruye a mitad, termina sin error.
    /// 4. Se puede detener en cualquier momento (StopCoroutine) sin dejar estado roto:
    ///    no guarda nada entre una llamada y otra.
    /// 5. Respeta el retraso antes de empezar.
    /// </summary>
    public interface ITextScrambleEffect
    {
        IEnumerator Revelar(TMP_Text destino, string textoFinal, ScannerConfig config, float retraso);
    }
}
