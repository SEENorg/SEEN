using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game.Scanner
{
    /// <summary>
    /// Lee la tecla de la Pantalla del Sistema desde el teclado (y LB en el mando).
    /// Usa el New Input System si está activo y, si no, el Input clásico,
    /// igual que PlayerInputReader y HackManager.
    /// La tecla se elige en ScannerConfig (por defecto Q).
    /// </summary>
    public class NewInputSystemScannerInput : MonoBehaviour, IScannerInput
    {
        [Tooltip("Configuración compartida del escáner (de aquí sale la tecla).")]
        [SerializeField] private ScannerConfig config;

        [Tooltip("Si está activo, el botón LB del mando también abre y cierra la pantalla.")]
        [SerializeField] private bool usarMando = true;

        public bool AlternarPresionado()
        {
            if (config == null) return false;
#if ENABLE_INPUT_SYSTEM
            // Mando: LB (botón superior izquierdo)
            Gamepad mando = Gamepad.current;
            if (usarMando && mando != null && mando.leftShoulder.wasPressedThisFrame) return true;

            Keyboard teclado = Keyboard.current;
            if (teclado == null) return false;
            if (config.teclaActivar == Key.None) return false;
            return teclado[config.teclaActivar].wasPressedThisFrame;
#else
            return Input.GetKeyDown(config.teclaActivar);
#endif
        }
    }
}
