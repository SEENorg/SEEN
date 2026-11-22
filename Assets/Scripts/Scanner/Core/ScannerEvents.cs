using System;
using UnityEngine;

namespace Game.Scanner
{
    /// <summary>
    /// Canal de eventos de la Pantalla del Sistema (patrón Observer, publicador/suscriptor).
    ///
    /// Es un ScriptableObject: un asset que el publicador (ScannerController) y los
    /// suscriptores (audio, objetos que se revelan, VFX...) reciben por el Inspector.
    /// Nadie busca a nadie y no hay Singleton: los dos lados solo conocen este asset.
    ///
    /// Regla para los suscriptores: suscribirse en OnEnable (+=) y
    /// desuscribirse en OnDisable (-=). Este asset vive más que las escenas, así
    /// que un suscriptor que no se desuscribe deja una referencia colgando.
    /// </summary>
    [CreateAssetMenu(fileName = "ScannerEvents", menuName = "SEEN/Scanner/Canal de eventos")]
    public class ScannerEvents : ScriptableObject
    {
        /// <summary>true = la pantalla se abre, false = se cierra.</summary>
        public event Action<bool> OnScanToggled;

        /// <summary>Se detectó un enemigo (proveedor y sus datos en ese momento).</summary>
        public event Action<IEnemyDataProvider, EnemyScanData> OnEnemyDetected;

        /// <summary>Se perdió la señal del enemigo actual.</summary>
        public event Action OnEnemyLost;

        /// <summary>Cambió el estado de la pantalla.</summary>
        public event Action<ScannerState> OnStateChanged;

        private bool pantallaActiva;

        /// <summary>
        /// Último valor publicado en OnScanToggled. Sirve a los objetos que aparecen
        /// con la pantalla ya abierta (por ejemplo, un clon de Repost).
        /// </summary>
        public bool PantallaActiva
        {
            get { return pantallaActiva; }
        }

        private void OnEnable()
        {
            pantallaActiva = false;
        }

        /// <summary>
        /// Vuelve al estado inicial. Lo llama ScannerController al arrancar, porque
        /// un ScriptableObject conserva sus valores entre una partida y otra en el editor.
        /// </summary>
        public void Reiniciar()
        {
            pantallaActiva = false;
        }

        public void PublicarScanToggled(bool activa)
        {
            pantallaActiva = activa;
            if (OnScanToggled != null) OnScanToggled(activa);
        }

        public void PublicarEnemyDetected(IEnemyDataProvider proveedor, EnemyScanData datos)
        {
            if (OnEnemyDetected != null) OnEnemyDetected(proveedor, datos);
        }

        public void PublicarEnemyLost()
        {
            if (OnEnemyLost != null) OnEnemyLost();
        }

        public void PublicarStateChanged(ScannerState estado)
        {
            if (OnStateChanged != null) OnStateChanged(estado);
        }
    }
}
