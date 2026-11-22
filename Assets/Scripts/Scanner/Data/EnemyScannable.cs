using System;
using UnityEngine;

namespace Game.Scanner
{
    /// <summary>
    /// Expone los datos de un enemigo a la Pantalla del Sistema.
    /// Responsabilidad única: guardar y entregar datos. No detecta, no dibuja
    /// y no conoce al escáner.
    ///
    /// Uso: agrégalo al enemigo (o a su objeto raíz si el collider está en un hijo).
    /// Los enemigos existentes (EnemyBase, Antivirus...) no necesitan cambios.
    /// Si quieres que la vida se actualice en la pantalla, llama a EstablecerVida
    /// desde el código del enemigo cuando reciba daño.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyScannable : MonoBehaviour, IEnemyDataProvider
    {
        [Header("Identidad")]
        [Tooltip("Nombre que aparece en la pantalla (ej: NOTIFICACIÓN).")]
        [SerializeField] private string nombre = "ENTIDAD";
        [Tooltip("Categoría del enemigo (ej: Interrupción, Viralización, Vigilancia).")]
        [SerializeField] private string tipo = "Desconocido";
        [Tooltip("Nivel de peligro. Cambia el color de la línea AMENAZA.")]
        [SerializeField] private NivelAmenaza amenaza = NivelAmenaza.Media;

        [Header("Integridad (vida)")]
        [Tooltip("0 = la pantalla no muestra vida para este enemigo.")]
        [SerializeField] private float vidaMaxima = 0f;
        [Tooltip("Vida inicial. Se ajusta sola para no pasar de la máxima.")]
        [SerializeField] private float vidaActual = 0f;

        [Header("Metadatos")]
        [Tooltip("Texto que la pantalla muestra al final. Buen lugar para el mensaje del juego (qué representa este enemigo).")]
        [TextArea(2, 4)]
        [SerializeField] private string descripcion = "";

        public event Action<IEnemyDataProvider> DatosCambiados;

        public Transform Objetivo
        {
            get { return transform; }
        }

        public EnemyScanData ObtenerDatos()
        {
            return new EnemyScanData(nombre, tipo, amenaza, vidaActual, vidaMaxima, descripcion);
        }

        /// <summary>Actualiza la vida y avisa a la pantalla si cambió.</summary>
        public void EstablecerVida(float actual, float maxima)
        {
            if (maxima < 0f) maxima = 0f;
            actual = Mathf.Clamp(actual, 0f, maxima);

            bool igualActual = Mathf.Approximately(actual, vidaActual);
            bool igualMaxima = Mathf.Approximately(maxima, vidaMaxima);
            if (igualActual && igualMaxima) return;

            vidaActual = actual;
            vidaMaxima = maxima;
            NotificarCambio();
        }

        /// <summary>Cambia el nivel de amenaza (por ejemplo, si el enemigo se enfurece).</summary>
        public void EstablecerAmenaza(NivelAmenaza nueva)
        {
            if (nueva == amenaza) return;
            amenaza = nueva;
            NotificarCambio();
        }

        /// <summary>Avisa a quien esté escuchando que los datos cambiaron.</summary>
        public void NotificarCambio()
        {
            if (DatosCambiados != null) DatosCambiados(this);
        }

        private void OnValidate()
        {
            if (vidaMaxima < 0f) vidaMaxima = 0f;
            vidaActual = Mathf.Clamp(vidaActual, 0f, vidaMaxima);
        }
    }
}
