using UnityEngine;

namespace Game.Scanner
{
    /// <summary>
    /// Orquesta la Pantalla del Sistema: escucha la entrada y al detector, mueve
    /// la máquina de estados, le dice a la vista qué mostrar y publica los eventos.
    ///
    /// Solo depende de abstracciones (IScannerInput, IEnemyDetector, IScannerView).
    /// Unity no puede serializar interfaces, así que cada dependencia se arrastra
    /// como MonoBehaviour y se convierte a su interfaz en Awake. OnValidate avisa
    /// en el Inspector si arrastras un componente que no implementa la interfaz.
    /// </summary>
    [DisallowMultipleComponent]
    public class ScannerController : MonoBehaviour
    {
        [Header("Dependencias (cualquier componente que implemente la interfaz)")]
        [Tooltip("Componente que implemente IScannerInput (ej: NewInputSystemScannerInput).")]
        [SerializeField] private MonoBehaviour componenteEntrada;
        [Tooltip("Componente que implemente IEnemyDetector (ej: RaycastEnemyDetector).")]
        [SerializeField] private MonoBehaviour componenteDetector;
        [Tooltip("Componente que implemente IScannerView (ej: ScannerUIView, en el Canvas).")]
        [SerializeField] private MonoBehaviour componenteVista;

        [Header("Datos compartidos")]
        [Tooltip("Canal de eventos donde se publica lo que pasa (Observer).")]
        [SerializeField] private ScannerEvents eventos;
        [Tooltip("Configuración compartida del escáner.")]
        [SerializeField] private ScannerConfig config;

        private IScannerInput entrada;
        private IEnemyDetector detector;
        private IScannerView vista;
        private ScannerStateMachine maquina;
        private IEnemyDataProvider objetivo;
        private bool listo;

        /// <summary>Estado actual, por si otro sistema quiere consultarlo.</summary>
        public ScannerState EstadoActual
        {
            get
            {
                if (maquina == null) return ScannerState.Off;
                return maquina.Estado;
            }
        }

        // ── Ciclo de vida ─────────────────────────────────────────────

        private void Awake()
        {
            maquina = new ScannerStateMachine();
            entrada = componenteEntrada as IScannerInput;
            detector = componenteDetector as IEnemyDetector;
            vista = componenteVista as IScannerView;

            listo = ValidarDependencias();
            if (!listo)
            {
                enabled = false;
                return;
            }
            eventos.Reiniciar();
        }

        private void OnEnable()
        {
            if (!listo) return;
            maquina.EstadoCambiado += AlCambiarEstado;
            detector.OnEnemyDetected += AlDetectarEnemigo;
            detector.OnEnemyLost += AlPerderEnemigo;
        }

        private void OnDisable()
        {
            if (!listo) return;
            ApagarInmediato();
            maquina.EstadoCambiado -= AlCambiarEstado;
            detector.OnEnemyDetected -= AlDetectarEnemigo;
            detector.OnEnemyLost -= AlPerderEnemigo;
        }

        private void Update()
        {
            // Tiempo sin escalar: el fade termina aunque el juego se pause.
            maquina.Avanzar(Time.unscaledDeltaTime);

            bool pausado = Time.timeScale <= 0f;

            if (pausado && config.cerrarAlPausar && maquina.EstaAbiertaOAbriendo)
            {
                maquina.CambiarA(ScannerState.Closing);
            }

            if (entrada.AlternarPresionado())
            {
                bool bloqueado = pausado && config.bloquearEnPausa;
                if (!bloqueado) Alternar();
            }

            if (maquina.Estado == ScannerState.Opening && maquina.TiempoEnEstado >= config.duracionFade)
            {
                maquina.CambiarA(ScannerState.Idle);
            }
            else if (maquina.Estado == ScannerState.Closing && maquina.TiempoEnEstado >= config.duracionFade)
            {
                maquina.CambiarA(ScannerState.Off);
            }
        }

        // ── API pública ───────────────────────────────────────────────

        /// <summary>Abre la pantalla si está cerrada y la cierra si está abierta.</summary>
        public void Alternar()
        {
            if (!listo) return;
            ScannerState actual = maquina.Estado;
            if (actual == ScannerState.Off || actual == ScannerState.Closing)
                maquina.CambiarA(ScannerState.Opening);
            else
                maquina.CambiarA(ScannerState.Closing);
        }

        // ── Reacciones ────────────────────────────────────────────────

        private void AlCambiarEstado(ScannerState anterior, ScannerState nuevo)
        {
            switch (nuevo)
            {
                case ScannerState.Opening:
                    if (VistaViva()) vista.Mostrar();
                    eventos.PublicarScanToggled(true);
                    break;

                case ScannerState.Idle:
                    // Al terminar de abrirse empieza a escanear.
                    if (anterior == ScannerState.Opening && DetectorVivo()) detector.Activar();
                    break;

                case ScannerState.Closing:
                    DejarObjetivo(false);
                    if (DetectorVivo()) detector.Desactivar();
                    if (VistaViva()) vista.Ocultar();
                    eventos.PublicarScanToggled(false);
                    break;
            }
            eventos.PublicarStateChanged(nuevo);
        }

        private void AlDetectarEnemigo(IEnemyDataProvider proveedor)
        {
            if (!maquina.PuedeDetectar) return;
            if (proveedor == null || proveedor == objetivo) return;

            // Si ya había otro objetivo, se suelta primero (los suscriptores reciben Lost y luego Detected).
            DejarObjetivo(false);

            objetivo = proveedor;
            objetivo.DatosCambiados += AlCambiarDatos;

            EnemyScanData datos = objetivo.ObtenerDatos();
            if (VistaViva()) vista.MostrarEnemigo(datos);
            eventos.PublicarEnemyDetected(objetivo, datos);

            if (maquina.Estado == ScannerState.Idle) maquina.CambiarA(ScannerState.Scanning);
        }

        private void AlPerderEnemigo()
        {
            if (objetivo == null) return;
            DejarObjetivo(true);
            if (maquina.Estado == ScannerState.Scanning) maquina.CambiarA(ScannerState.Idle);
        }

        private void AlCambiarDatos(IEnemyDataProvider proveedor)
        {
            if (proveedor != objetivo) return;
            if (VistaViva()) vista.ActualizarEnemigo(proveedor.ObtenerDatos());
        }

        // ── Ayudantes ─────────────────────────────────────────────────

        private void DejarObjetivo(bool avisarVista)
        {
            if (objetivo == null) return;
            objetivo.DatosCambiados -= AlCambiarDatos;
            objetivo = null;
            if (avisarVista && VistaViva()) vista.LimpiarEnemigo();
            eventos.PublicarEnemyLost();
        }

        /// <summary>Cierra sin fade (al desactivar el componente o cambiar de escena).</summary>
        private void ApagarInmediato()
        {
            if (maquina.EstaAbiertaOAbriendo) maquina.CambiarA(ScannerState.Closing);
            if (maquina.Estado == ScannerState.Closing) maquina.CambiarA(ScannerState.Off);
        }

        // Al cerrar una escena, Unity puede destruir la vista o el detector antes que
        // este componente. Comprobar el MonoBehaviour (no la interfaz) detecta eso.
        private bool VistaViva()
        {
            return componenteVista != null;
        }

        private bool DetectorVivo()
        {
            return componenteDetector != null;
        }

        private bool ValidarDependencias()
        {
            bool ok = true;
            if (entrada == null)
            {
                Debug.LogError("[ScannerController] Falta un componente que implemente IScannerInput.", this);
                ok = false;
            }
            if (detector == null)
            {
                Debug.LogError("[ScannerController] Falta un componente que implemente IEnemyDetector.", this);
                ok = false;
            }
            if (vista == null)
            {
                Debug.LogError("[ScannerController] Falta un componente que implemente IScannerView.", this);
                ok = false;
            }
            if (eventos == null)
            {
                Debug.LogError("[ScannerController] Falta el asset ScannerEvents.", this);
                ok = false;
            }
            if (config == null)
            {
                Debug.LogError("[ScannerController] Falta el asset ScannerConfig.", this);
                ok = false;
            }
            return ok;
        }

        private void OnValidate()
        {
            componenteEntrada = ComprobarTipo<IScannerInput>(componenteEntrada, "IScannerInput");
            componenteDetector = ComprobarTipo<IEnemyDetector>(componenteDetector, "IEnemyDetector");
            componenteVista = ComprobarTipo<IScannerView>(componenteVista, "IScannerView");
        }

        /// <summary>Si el componente arrastrado no implementa la interfaz, lo quita y avisa.</summary>
        private MonoBehaviour ComprobarTipo<T>(MonoBehaviour componente, string nombreInterfaz) where T : class
        {
            if (componente == null) return null;
            if ((componente as T) != null) return componente;

            // Si arrastraron el GameObject, Unity tomó el primer MonoBehaviour:
            // se busca en ese mismo objeto uno que sí implemente la interfaz.
            MonoBehaviour[] candidatos = componente.GetComponents<MonoBehaviour>();
            for (int i = 0; i < candidatos.Length; i++)
            {
                if ((candidatos[i] as T) != null) return candidatos[i];
            }

            Debug.LogWarning("[ScannerController] " + componente.GetType().Name + " no implementa " + nombreInterfaz + ".", this);
            return null;
        }
    }
}
