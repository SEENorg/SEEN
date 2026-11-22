using System;
using UnityEngine;

namespace Game.Scanner
{
    /// <summary>
    /// Adaptador entre los enemigos de SEEN (EnemyBase) y la Pantalla del Sistema.
    /// Lee el tipo, la vida y el estado de buff del EnemyBase que está en el mismo
    /// objeto y los entrega como IEnemyDataProvider (patrón Adapter).
    ///
    /// Así la pantalla muestra a Hashtag, Repost, Notificación y Algoritmo sin
    /// modificar EnemyBase ni ningún script de enemigo.
    ///
    /// Se agrega solo con el menú SEEN > Pantalla del Sistema > Hacer escaneables
    /// los enemigos de la escena. Los textos vacíos usan los valores por defecto
    /// de cada tipo; si escribes algo, se usa lo tuyo.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyBaseScanAdapter : MonoBehaviour, IEnemyDataProvider
    {
        [Tooltip("Enemigo del que se leen los datos. Si está vacío, se toma el EnemyBase de este mismo objeto.")]
        [SerializeField] private EnemyBase enemigo;

        [Header("Textos propios (vacío = texto por defecto del tipo)")]
        [SerializeField] private string nombrePropio = "";
        [SerializeField] private string tipoPropio = "";
        [TextArea(2, 4)]
        [SerializeField] private string descripcionPropia = "";
        [Tooltip("Si está activo, usa la amenaza de abajo en vez de la del tipo.")]
        [SerializeField] private bool usarAmenazaPropia = false;
        [SerializeField] private NivelAmenaza amenazaPropia = NivelAmenaza.Media;

        public event Action<IEnemyDataProvider> DatosCambiados;

        // Último estado conocido, para avisar solo cuando algo cambia.
        private int vidaAnterior = -1;
        private int vidaMaximaAnterior = -1;
        private bool buffAnterior;

        public Transform Objetivo
        {
            get { return transform; }
        }

        private void Reset()
        {
            enemigo = GetComponent<EnemyBase>();
        }

        private void Awake()
        {
            if (enemigo == null) enemigo = GetComponent<EnemyBase>();
            if (enemigo == null)
            {
                Debug.LogWarning("[EnemyBaseScanAdapter] " + name + " no tiene EnemyBase. La pantalla mostrará datos vacíos.", this);
            }
        }

        private void Update()
        {
            // Solo se revisa mientras la pantalla está mostrando a este enemigo
            // (es decir, cuando alguien está suscrito). Comparar dos enteros es muy barato.
            if (DatosCambiados == null || enemigo == null) return;

            int vida = enemigo.VidaActual;
            int vidaMaxima = enemigo.VidaMaxima;
            bool buff = enemigo.Buffeado;

            bool cambio = vida != vidaAnterior || vidaMaxima != vidaMaximaAnterior || buff != buffAnterior;
            GuardarEstado();

            if (cambio) DatosCambiados(this);
        }

        public EnemyScanData ObtenerDatos()
        {
            GuardarEstado();

            if (enemigo == null)
                return new EnemyScanData("ENTIDAD", "Desconocido", NivelAmenaza.Media, 0f, 0f, "");

            EnemyType clase = enemigo.Tipo;

            string nombre = NombrePorDefecto(clase);
            if (!string.IsNullOrEmpty(nombrePropio)) nombre = nombrePropio;

            string tipo = TipoPorDefecto(clase);
            if (!string.IsNullOrEmpty(tipoPropio)) tipo = tipoPropio;

            string descripcion = DescripcionPorDefecto(clase);
            if (!string.IsNullOrEmpty(descripcionPropia)) descripcion = descripcionPropia;

            NivelAmenaza amenaza = AmenazaPorDefecto(clase);
            if (usarAmenazaPropia) amenaza = amenazaPropia;

            // Un enemigo potenciado (buff) sube un nivel de amenaza.
            if (enemigo.Buffeado)
            {
                amenaza = SubirAmenaza(amenaza);
                tipo = tipo + " [POTENCIADO]";
            }

            return new EnemyScanData(nombre, tipo, amenaza, enemigo.VidaActual, enemigo.VidaMaxima, descripcion);
        }

        private void GuardarEstado()
        {
            if (enemigo == null) return;
            vidaAnterior = enemigo.VidaActual;
            vidaMaximaAnterior = enemigo.VidaMaxima;
            buffAnterior = enemigo.Buffeado;
        }

        // ── Valores por defecto de cada tipo (se pueden cambiar con los campos propios) ──

        private string NombrePorDefecto(EnemyType clase)
        {
            switch (clase)
            {
                case EnemyType.Hashtag: return "HASHTAG";
                case EnemyType.Repost: return "REPOST";
                case EnemyType.Notificacion: return "NOTIFICACIÓN";
                case EnemyType.Algoritmo: return "ALGORITMO";
                default: return "AMENAZA";
            }
        }

        private string TipoPorDefecto(EnemyType clase)
        {
            switch (clase)
            {
                case EnemyType.Hashtag: return "Etiquetado";
                case EnemyType.Repost: return "Viralización";
                case EnemyType.Notificacion: return "Interrupción";
                case EnemyType.Algoritmo: return "Recomendación";
                default: return "Desconocido";
            }
        }

        private NivelAmenaza AmenazaPorDefecto(EnemyType clase)
        {
            switch (clase)
            {
                case EnemyType.Hashtag: return NivelAmenaza.Baja;
                case EnemyType.Repost: return NivelAmenaza.Media;
                case EnemyType.Notificacion: return NivelAmenaza.Media;
                case EnemyType.Algoritmo: return NivelAmenaza.Alta;
                default: return NivelAmenaza.Media;
            }
        }

        private string DescripcionPorDefecto(EnemyType clase)
        {
            switch (clase)
            {
                case EnemyType.Hashtag: return "Te etiqueta para que el sistema sepa dónde encontrarte.";
                case EnemyType.Repost: return "Cada copia borra un poco el origen.";
                case EnemyType.Notificacion: return "Nadie la pidió, pero llega igual. Vive de tu atención.";
                case EnemyType.Algoritmo: return "No te odia. Solo te mide.";
                default: return "";
            }
        }

        private NivelAmenaza SubirAmenaza(NivelAmenaza actual)
        {
            if (actual == NivelAmenaza.Baja) return NivelAmenaza.Media;
            if (actual == NivelAmenaza.Media) return NivelAmenaza.Alta;
            return NivelAmenaza.Critica;
        }
    }
}
