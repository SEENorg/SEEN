using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game.Scanner
{
    /// <summary>
    /// Configuración centralizada de la Pantalla del Sistema.
    /// Es un ScriptableObject: un asset en el proyecto que comparten todos los
    /// componentes del escáner. Cambiar un valor aquí lo cambia en todos lados,
    /// y se puede editar en Play sin tocar código.
    /// Crear: clic derecho en Project > Create > SEEN > Scanner > Configuración.
    /// </summary>
    [CreateAssetMenu(fileName = "ScannerConfig", menuName = "SEEN/Scanner/Configuración")]
    public class ScannerConfig : ScriptableObject
    {
        [Header("Entrada")]
#if ENABLE_INPUT_SYSTEM
        [Tooltip("Tecla que activa y desactiva la pantalla. No uses Tab: ya abre la rueda de cifrado (CipherWheel).")]
        public Key teclaActivar = Key.Q;
#else
        [Tooltip("Tecla que activa y desactiva la pantalla. No uses Tab: ya abre la rueda de cifrado (CipherWheel).")]
        public KeyCode teclaActivar = KeyCode.Q;
#endif
        [Tooltip("Si está activo, la tecla no hace nada mientras el juego está pausado (Time.timeScale = 0), por ejemplo durante un hackeo o en el menú de pausa.")]
        public bool bloquearEnPausa = true;
        [Tooltip("Si está activo, la pantalla se cierra sola cuando el juego se pausa.")]
        public bool cerrarAlPausar = true;

        [Header("Seguimiento de cámara")]
        [Tooltip("Distancia (en metros) entre la cámara y la pantalla.")]
        public float distancia = 0.7f;
        [Tooltip("Desplazamiento lateral (X) y vertical (Y) en metros, relativo a la cámara. X positivo = a la derecha, para no tapar la mirilla.")]
        public Vector2 desplazamiento = new Vector2(0.56f, -0.05f);
        [Tooltip("Inclinación extra en grados (X = cabeceo, Y = giro). La pantalla ya mira a la cámara; esto solo añade el ángulo tipo HUD flotante.")]
        public Vector2 inclinacionExtra = new Vector2(3f, 6f);
        [Tooltip("Qué tan rápido alcanza la posición objetivo. Más alto = más pegada a la cámara; más bajo = más flotante.")]
        public float suavizadoPosicion = 18f;
        [Tooltip("Qué tan rápido alcanza la rotación objetivo.")]
        public float suavizadoRotacion = 14f;

        [Header("Detección")]
        [Tooltip("Alcance máximo del escaneo, en metros.")]
        public float alcance = 40f;
        [Tooltip("Radio del cono de detección (SphereCast). 0 = rayo fino (Raycast).")]
        public float radioDeteccion = 0.25f;
        [Tooltip("Capas que ve el escáner. Incluye las de los enemigos Y las de las paredes (si quitas las paredes, el escáner verá enemigos a través de ellas). Excluye la capa ScannerUI.")]
        public LayerMask capasDeteccion = ~0;
        [Tooltip("Cada cuántos segundos se lanza el rayo. No hace falta hacerlo cada frame.")]
        public float intervaloDeteccion = 0.1f;
        [Tooltip("Segundos que se conserva un objetivo después de dejar de verlo. Evita parpadeos cuando el enemigo se mueve.")]
        public float tiempoGracia = 0.35f;

        [Header("Apariencia")]
        [Tooltip("Transparencia del fondo (0 = invisible, 1 = opaco).")]
        [Range(0f, 1f)] public float alphaFondo = 0.45f;
        public Color colorFondo = new Color(0.02f, 0.04f, 0.07f, 1f);
        public Color colorNeon = new Color(0.15f, 0.95f, 1f, 1f);
        public Color colorTexto = new Color(0.85f, 1f, 1f, 1f);
        [Tooltip("Color del destello al perder la señal.")]
        public Color colorAlerta = new Color(1f, 0.25f, 0.3f, 1f);
        [Tooltip("Duración del fade de entrada y de salida, en segundos.")]
        public float duracionFade = 0.25f;

        [Header("Colores por nivel de amenaza")]
        public Color colorAmenazaBaja = new Color(0.4f, 1f, 0.5f, 1f);
        public Color colorAmenazaMedia = new Color(1f, 0.9f, 0.3f, 1f);
        public Color colorAmenazaAlta = new Color(1f, 0.55f, 0.2f, 1f);
        public Color colorAmenazaCritica = new Color(1f, 0.2f, 0.3f, 1f);

        [Header("Efectos")]
        [Tooltip("Cuánto baja la opacidad en cada parpadeo (0 = sin flicker).")]
        [Range(0f, 1f)] public float intensidadFlicker = 0.08f;
        [Tooltip("Velocidad del pulso del borde luminoso.")]
        public float velocidadPulso = 3f;
        [Tooltip("Opacidad del halo que rodea al borde.")]
        [Range(0f, 1f)] public float alphaBrillo = 0.3f;
        [Tooltip("Cantidad de líneas de escaneo visibles.")]
        public float cantidadLineas = 120f;
        [Tooltip("Velocidad con la que se desplazan las líneas de escaneo.")]
        public float velocidadScanlines = 0.6f;
        [Range(0f, 1f)] public float alphaScanlines = 0.12f;
        [Range(0f, 1f)] public float alphaRuido = 0.06f;

        [Header("Texto cifrado")]
        [Tooltip("Caracteres aleatorios que se muestran mientras el texto se descifra.")]
        public string caracteresCifrado = "!@#$%&*0123456789ABCDEF";
        [Tooltip("Caracteres que se revelan por segundo.")]
        public float velocidadDescifrado = 40f;
        [Tooltip("Cada cuántos segundos cambian los caracteres aleatorios.")]
        public float intervaloCambio = 0.04f;
        [Tooltip("Retraso entre un campo de texto y el siguiente, para que se descifren en cascada.")]
        public float retrasoEntreCampos = 0.08f;
    }
}
