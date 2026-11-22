using System;
using UnityEngine;

namespace Game.Scanner
{
    /// <summary>
    /// Detecta enemigos lanzando un rayo (o un SphereCast, que funciona como un
    /// cono estrecho) desde el centro de la cámara.
    /// Responsabilidad única: detectar. No dibuja nada ni sabe que existe una pantalla.
    ///
    /// Cómo decide qué ve:
    /// 1. Recoge todo lo que cruza el rayo y lo ordena por distancia.
    /// 2. Ignora los colliders del propio jugador.
    /// 3. El primer objeto con IEnemyDataProvider es el objetivo.
    /// 4. Un trigger sin datos (meta, checkpoint) no tapa la vista.
    /// 5. Un collider sólido sin datos es una pared: lo de atrás no se ve.
    /// </summary>
    public class RaycastEnemyDetector : MonoBehaviour, IEnemyDetector
    {
        private const int MaxImpactos = 16;

        [Tooltip("Desde dónde sale el rayo. Arrastra aquí la Main Camera del jugador.")]
        [SerializeField] private Transform origen;
        [Tooltip("El objeto del jugador (el que tiene la etiqueta Player). Sus colliders se ignoran para que el rayo no choque con el propio jugador.")]
        [SerializeField] private Transform raizJugador;
        [Tooltip("Configuración compartida del escáner.")]
        [SerializeField] private ScannerConfig config;

        public event Action<IEnemyDataProvider> OnEnemyDetected;
        public event Action OnEnemyLost;

        private readonly RaycastHit[] impactos = new RaycastHit[MaxImpactos];
        private bool activo;
        private float temporizador;
        private float ultimaVezVisto;
        private IEnemyDataProvider actual;

        public bool Activo
        {
            get { return activo; }
        }

        public void Activar()
        {
            activo = true;
            // Así el primer escaneo ocurre en el siguiente frame, sin esperar el intervalo.
            temporizador = float.MaxValue;
        }

        public void Desactivar()
        {
            activo = false;
            actual = null;
        }

        private void Update()
        {
            if (!activo) return;
            if (origen == null || config == null) return;

            // Tiempo escalado: con el juego en pausa no se escanea.
            temporizador += Time.deltaTime;
            if (temporizador < config.intervaloDeteccion) return;
            temporizador = 0f;

            IEnemyDataProvider encontrado = Buscar();

            if (encontrado != null)
            {
                ultimaVezVisto = Time.time;
                if (encontrado != actual)
                {
                    actual = encontrado;
                    if (OnEnemyDetected != null) OnEnemyDetected(actual);
                }
                return;
            }

            // Sin objetivo a la vista: se espera el tiempo de gracia antes de soltarlo.
            if (actual != null && Time.time - ultimaVezVisto >= config.tiempoGracia)
            {
                actual = null;
                if (OnEnemyLost != null) OnEnemyLost();
            }
        }

        private IEnemyDataProvider Buscar()
        {
            Vector3 desde = origen.position;
            Vector3 direccion = origen.forward;
            int cantidad;

            if (config.radioDeteccion > 0f)
            {
                cantidad = Physics.SphereCastNonAlloc(desde, config.radioDeteccion, direccion, impactos,
                                                      config.alcance, config.capasDeteccion,
                                                      QueryTriggerInteraction.Collide);
            }
            else
            {
                cantidad = Physics.RaycastNonAlloc(desde, direccion, impactos,
                                                   config.alcance, config.capasDeteccion,
                                                   QueryTriggerInteraction.Collide);
            }

            OrdenarPorDistancia(cantidad);

            for (int i = 0; i < cantidad; i++)
            {
                Collider col = impactos[i].collider;
                if (col == null) continue;
                if (EsDelJugador(col.transform)) continue;

                IEnemyDataProvider proveedor = col.GetComponentInParent<IEnemyDataProvider>();
                if (proveedor != null) return proveedor;

                // Un trigger sin datos (meta, checkpoint...) no tapa la vista.
                if (col.isTrigger) continue;

                // Algo sólido sin datos: es una pared.
                return null;
            }
            return null;
        }

        /// <summary>Ordenamiento por inserción: los resultados de NonAlloc no vienen ordenados.</summary>
        private void OrdenarPorDistancia(int cantidad)
        {
            for (int i = 1; i < cantidad; i++)
            {
                RaycastHit clave = impactos[i];
                int j = i - 1;
                while (j >= 0 && impactos[j].distance > clave.distance)
                {
                    impactos[j + 1] = impactos[j];
                    j--;
                }
                impactos[j + 1] = clave;
            }
        }

        private bool EsDelJugador(Transform t)
        {
            if (raizJugador != null) return t.IsChildOf(raizJugador);
            if (t.CompareTag("Player")) return true;
            return t.root.CompareTag("Player");
        }

        private void OnDrawGizmosSelected()
        {
            if (origen == null || config == null) return;
            Gizmos.color = Color.cyan;
            Vector3 fin = origen.position + origen.forward * config.alcance;
            Gizmos.DrawLine(origen.position, fin);
            if (config.radioDeteccion > 0f) Gizmos.DrawWireSphere(fin, config.radioDeteccion);
        }
    }
}
