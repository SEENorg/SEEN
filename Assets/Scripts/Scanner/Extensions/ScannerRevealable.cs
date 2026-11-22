using UnityEngine;

namespace Game.Scanner
{
    /// <summary>
    /// Suscriptor de ejemplo: hace que un objeto solo se vea con la Pantalla del
    /// Sistema activa (o al revés). Es la mecánica central del diseño de SEEN:
    /// pistas, caminos o enemigos invisibles (las "Vistas") que solo revela la pantalla.
    ///
    /// Uso: agrégalo al objeto y arrastra el asset ScannerEvents.
    /// Solo apaga los Renderers (y, si lo pides, los Colliders) del objeto y sus hijos.
    /// </summary>
    public class ScannerRevealable : MonoBehaviour
    {
        [Tooltip("Canal de eventos del escáner.")]
        [SerializeField] private ScannerEvents eventos;
        [Tooltip("Activo: el objeto solo se ve con la pantalla abierta. Inactivo: el objeto desaparece al abrir la pantalla.")]
        [SerializeField] private bool visibleConPantalla = true;
        [Tooltip("Si está activo, también se apagan los colliders mientras el objeto está oculto (un camino que no existe hasta que lo revelas).")]
        [SerializeField] private bool incluirColliders = false;

        private Renderer[] renderers;
        private Collider[] colliders;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider>(true);
        }

        private void OnEnable()
        {
            if (eventos == null)
            {
                Debug.LogWarning("[ScannerRevealable] Falta el asset ScannerEvents en " + name + ".", this);
                return;
            }
            eventos.OnScanToggled += Aplicar;
            // Por si el objeto aparece con la pantalla ya abierta.
            Aplicar(eventos.PantallaActiva);
        }

        private void OnDisable()
        {
            if (eventos == null) return;
            eventos.OnScanToggled -= Aplicar;
        }

        private void Aplicar(bool pantallaActiva)
        {
            bool mostrar = pantallaActiva == visibleConPantalla;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].enabled = mostrar;
            }

            if (!incluirColliders) return;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) colliders[i].enabled = mostrar;
            }
        }
    }
}
