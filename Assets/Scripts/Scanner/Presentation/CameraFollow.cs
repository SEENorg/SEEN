using UnityEngine;

namespace Game.Scanner
{
    /// <summary>
    /// Hace que la pantalla flote delante de la cámara, a un lado de la mirilla,
    /// siguiéndola con un pequeño retraso (efecto HUD holográfico).
    /// Responsabilidad única: posición y rotación. No sabe qué se muestra.
    ///
    /// Va en el objeto del Canvas, que NO debe ser hijo de la cámara
    /// (si lo fuera, la seguiría de forma rígida y no habría retraso).
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Tooltip("Cámara a seguir. Arrastra aquí la Main Camera del jugador.")]
        [SerializeField] private Transform camara;
        [Tooltip("Configuración compartida del escáner (distancia, desplazamiento, suavizado).")]
        [SerializeField] private ScannerConfig config;

        private void OnEnable()
        {
            // Al aparecer se coloca de golpe, sin viajar desde su posición anterior.
            Ajustar(true);
        }

        // LateUpdate: se ejecuta después de que el jugador movió la cámara en Update.
        private void LateUpdate()
        {
            Ajustar(false);
        }

        private void Ajustar(bool inmediato)
        {
            if (camara == null || config == null) return;

            Vector3 posicionObjetivo = camara.position
                                     + camara.forward * config.distancia
                                     + camara.right * config.desplazamiento.x
                                     + camara.up * config.desplazamiento.y;

            // La pantalla queda paralela a la pantalla del monitor (misma rotación que
            // la cámara), así se ve de frente y rectangular aunque esté a un lado.
            // Si se gira para "mirar" a la cámara, con un campo de visión amplio se ve de lado.
            // La inclinación extra de la configuración se suma encima (0, 0 = totalmente de frente).
            Quaternion rotacionObjetivo = camara.rotation
                                        * Quaternion.Euler(config.inclinacionExtra.x, config.inclinacionExtra.y, 0f);

            if (inmediato)
            {
                transform.SetPositionAndRotation(posicionObjetivo, rotacionObjetivo);
                return;
            }

            // Suavizado exponencial: no depende de los FPS. Tiempo sin escalar para la pausa.
            float dt = Time.unscaledDeltaTime;
            float tPosicion = 1f - Mathf.Exp(-config.suavizadoPosicion * dt);
            float tRotacion = 1f - Mathf.Exp(-config.suavizadoRotacion * dt);

            Vector3 posicion = Vector3.Lerp(transform.position, posicionObjetivo, tPosicion);
            Quaternion rotacion = Quaternion.Slerp(transform.rotation, rotacionObjetivo, tRotacion);
            transform.SetPositionAndRotation(posicion, rotacion);
        }
    }
}
