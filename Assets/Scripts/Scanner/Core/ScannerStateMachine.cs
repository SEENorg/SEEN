using System;

namespace Game.Scanner
{
    /// <summary>Estados de la Pantalla del Sistema.</summary>
    public enum ScannerState
    {
        Off,        // apagada
        Opening,    // fade de entrada
        Idle,       // abierta, buscando señal
        Scanning,   // abierta, mostrando un enemigo
        Closing     // fade de salida
    }

    /// <summary>
    /// Máquina de estados de la pantalla. No es un MonoBehaviour: es una clase
    /// normal que solo sabe en qué estado está, cuánto lleva en él y qué
    /// transiciones son válidas. Quien la usa reacciona al evento EstadoCambiado.
    ///
    /// Transiciones válidas:
    ///   Off      -> Opening
    ///   Opening  -> Idle | Closing
    ///   Idle     -> Scanning | Closing
    ///   Scanning -> Idle | Closing
    ///   Closing  -> Off | Opening
    /// Cualquier otra se rechaza (CambiarA devuelve false y no lanza el evento).
    /// </summary>
    public class ScannerStateMachine
    {
        private ScannerState estado = ScannerState.Off;
        private float tiempoEnEstado;

        /// <summary>(estado anterior, estado nuevo)</summary>
        public event Action<ScannerState, ScannerState> EstadoCambiado;

        public ScannerState Estado
        {
            get { return estado; }
        }

        public float TiempoEnEstado
        {
            get { return tiempoEnEstado; }
        }

        /// <summary>True si la pantalla puede recibir enemigos detectados.</summary>
        public bool PuedeDetectar
        {
            get { return estado == ScannerState.Idle || estado == ScannerState.Scanning; }
        }

        /// <summary>True si la pantalla está abierta o abriéndose.</summary>
        public bool EstaAbiertaOAbriendo
        {
            get
            {
                return estado == ScannerState.Opening
                    || estado == ScannerState.Idle
                    || estado == ScannerState.Scanning;
            }
        }

        public void Avanzar(float deltaTiempo)
        {
            tiempoEnEstado += deltaTiempo;
        }

        public bool CambiarA(ScannerState nuevo)
        {
            if (!EsTransicionValida(estado, nuevo)) return false;

            ScannerState anterior = estado;
            estado = nuevo;
            tiempoEnEstado = 0f;

            if (EstadoCambiado != null) EstadoCambiado(anterior, nuevo);
            return true;
        }

        private bool EsTransicionValida(ScannerState desde, ScannerState hacia)
        {
            switch (desde)
            {
                case ScannerState.Off:
                    return hacia == ScannerState.Opening;
                case ScannerState.Opening:
                    return hacia == ScannerState.Idle || hacia == ScannerState.Closing;
                case ScannerState.Idle:
                    return hacia == ScannerState.Scanning || hacia == ScannerState.Closing;
                case ScannerState.Scanning:
                    return hacia == ScannerState.Idle || hacia == ScannerState.Closing;
                case ScannerState.Closing:
                    return hacia == ScannerState.Off || hacia == ScannerState.Opening;
                default:
                    return false;
            }
        }
    }
}
