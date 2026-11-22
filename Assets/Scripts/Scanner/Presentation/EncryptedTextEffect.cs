using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.Scanner
{
    /// <summary>
    /// Efecto de "código encriptado": el texto aparece como caracteres aleatorios
    /// que se van descifrando de izquierda a derecha.
    ///
    /// Es un ScriptableObject para poder cambiar de efecto arrastrando otro asset
    /// en el Inspector de ScannerUIView, sin tocar código (principio abierto/cerrado).
    /// Crear: clic derecho en Project > Create > SEEN > Scanner > Efecto texto cifrado.
    /// </summary>
    [CreateAssetMenu(fileName = "EncryptedTextEffect", menuName = "SEEN/Scanner/Efecto texto cifrado")]
    public class EncryptedTextEffect : ScriptableObject, ITextScrambleEffect
    {
        [Tooltip("Caracteres propios de este efecto. Si está vacío, usa los de ScannerConfig. Ej: \"01\" para un efecto binario.")]
        [SerializeField] private string caracteresPropios = "";

        public IEnumerator Revelar(TMP_Text destino, string textoFinal, ScannerConfig config, float retraso)
        {
            if (destino == null) yield break;
            if (textoFinal == null) textoFinal = "";

            // 1. Retraso inicial (tiempo sin escalar).
            float espera = retraso;
            while (espera > 0f)
            {
                espera -= Time.unscaledDeltaTime;
                yield return null;
                if (destino == null) yield break;
            }

            string alfabeto = ObtenerAlfabeto(config);
            float velocidad = 0f;
            float intervalo = 0.04f;
            if (config != null)
            {
                velocidad = config.velocidadDescifrado;
                intervalo = config.intervaloCambio;
            }

            // Sin velocidad o sin texto: se muestra directamente.
            if (velocidad <= 0f || textoFinal.Length == 0)
            {
                destino.text = textoFinal;
                yield break;
            }

            // 2. Descifrado progresivo.
            int largo = textoFinal.Length;
            char[] buffer = new char[largo];
            float revelados = 0f;
            float tiempoCambio = intervalo;   // para pintar en el primer frame

            while (revelados < largo)
            {
                float dt = Time.unscaledDeltaTime;
                revelados += dt * velocidad;
                tiempoCambio += dt;

                if (tiempoCambio >= intervalo)
                {
                    tiempoCambio = 0f;
                    int fijos = Mathf.Min((int)revelados, largo);
                    for (int i = 0; i < largo; i++)
                    {
                        char original = textoFinal[i];
                        if (i < fijos || original == ' ' || original == '\n')
                            buffer[i] = original;
                        else
                            buffer[i] = alfabeto[Random.Range(0, alfabeto.Length)];
                    }
                    destino.text = new string(buffer);
                }

                yield return null;
                if (destino == null) yield break;
            }

            // 3. Contrato: siempre termina con el texto exacto.
            destino.text = textoFinal;
        }

        private string ObtenerAlfabeto(ScannerConfig config)
        {
            if (!string.IsNullOrEmpty(caracteresPropios)) return caracteresPropios;
            if (config != null && !string.IsNullOrEmpty(config.caracteresCifrado)) return config.caracteresCifrado;
            return "01";
        }
    }
}
