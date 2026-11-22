using UnityEngine;

namespace Game.Scanner
{
    /// <summary>
    /// Suscriptor de ejemplo: reproduce sonidos cuando la pantalla se abre, se
    /// cierra, detecta un enemigo o pierde la señal.
    /// No conoce al controlador, ni a la vista ni al detector: solo escucha el
    /// canal ScannerEvents. Se puede borrar sin que nada más se rompa.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ScannerAudioFeedback : MonoBehaviour
    {
        [Tooltip("Canal de eventos del escáner.")]
        [SerializeField] private ScannerEvents eventos;

        [Header("Sonidos (puedes dejar vacíos los que no uses)")]
        [SerializeField] private AudioClip sonidoAbrir;
        [SerializeField] private AudioClip sonidoCerrar;
        [SerializeField] private AudioClip sonidoDetectar;
        [SerializeField] private AudioClip sonidoPerder;
        [Range(0f, 1f)]
        [SerializeField] private float volumen = 0.8f;

        private AudioSource fuente;

        private void Awake()
        {
            fuente = GetComponent<AudioSource>();
            fuente.playOnAwake = false;
        }

        private void OnEnable()
        {
            if (eventos == null) return;
            eventos.OnScanToggled += AlAlternar;
            eventos.OnEnemyDetected += AlDetectar;
            eventos.OnEnemyLost += AlPerder;
        }

        private void OnDisable()
        {
            if (eventos == null) return;
            eventos.OnScanToggled -= AlAlternar;
            eventos.OnEnemyDetected -= AlDetectar;
            eventos.OnEnemyLost -= AlPerder;
        }

        private void AlAlternar(bool activa)
        {
            if (activa) Reproducir(sonidoAbrir);
            else Reproducir(sonidoCerrar);
        }

        private void AlDetectar(IEnemyDataProvider proveedor, EnemyScanData datos)
        {
            Reproducir(sonidoDetectar);
        }

        private void AlPerder()
        {
            Reproducir(sonidoPerder);
        }

        private void Reproducir(AudioClip clip)
        {
            if (clip == null || fuente == null) return;
            fuente.PlayOneShot(clip, volumen);
        }
    }
}
