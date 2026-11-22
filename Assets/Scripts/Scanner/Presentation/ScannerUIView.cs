using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scanner
{
    /// <summary>
    /// Dibuja la Pantalla del Sistema en un Canvas World Space: fade, flicker,
    /// líneas de escaneo, ruido, borde luminoso y los textos del enemigo.
    /// Responsabilidad única: presentar. No detecta enemigos, no lee teclas y no
    /// decide cuándo abrirse; eso lo ordena ScannerController por IScannerView.
    ///
    /// Las texturas de las líneas y del ruido se generan por código: no hace
    /// falta importar imágenes.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScannerUIView : MonoBehaviour, IScannerView
    {
        private const float DuracionDestello = 0.35f;
        private const float DuracionGlitchDetectar = 0.25f;
        private const float DuracionGlitchPerder = 0.2f;
        private const float EsperaTrasPerder = 0.6f;
        private const int SegmentosBarraVida = 10;

        [Header("Configuración")]
        [Tooltip("Configuración compartida del escáner (colores, fade, efectos).")]
        [SerializeField] private ScannerConfig config;
        [Tooltip("Asset del efecto de texto. Acepta cualquier ScriptableObject que implemente ITextScrambleEffect (ej: EncryptedTextEffect).")]
        [SerializeField] private ScriptableObject efectoTexto;

        [Header("Contenedor")]
        [Tooltip("Canvas World Space de la pantalla. Se apaga cuando está oculta, para no gastar rendimiento.")]
        [SerializeField] private Canvas canvas;
        [Tooltip("CanvasGroup del Canvas: controla la opacidad de todo a la vez.")]
        [SerializeField] private CanvasGroup grupo;

        [Header("Marco y efectos")]
        [Tooltip("Imagen del fondo semitransparente.")]
        [SerializeField] private Image fondo;
        [Tooltip("Las cuatro tiras finas del borde.")]
        [SerializeField] private Graphic[] bordes;
        [Tooltip("Las cuatro tiras anchas del halo (detrás del borde).")]
        [SerializeField] private Graphic[] brillos;
        [Tooltip("RawImage estirada que muestra las líneas de escaneo.")]
        [SerializeField] private RawImage scanlines;
        [Tooltip("RawImage estirada que muestra el ruido.")]
        [SerializeField] private RawImage ruido;

        [Header("Textos")]
        [SerializeField] private TMP_Text textoEstado;
        [SerializeField] private TMP_Text textoNombre;
        [SerializeField] private TMP_Text textoTipo;
        [SerializeField] private TMP_Text textoAmenaza;
        [SerializeField] private TMP_Text textoVida;
        [SerializeField] private TMP_Text textoDescripcion;

        private ITextScrambleEffect efecto;
        private float alphaVisible;
        private float alphaObjetivo;
        private float flicker = 1f;
        private float proximoFlicker;
        private float tiempoGlitch;
        private Color colorDestello = Color.white;
        private float tiempoDestello;
        private float desplazamientoScan;
        private float proximoRuido;
        private Texture2D texturaScan;
        private Texture2D texturaRuido;

        // ── Ciclo de vida ─────────────────────────────────────────────

        private void Awake()
        {
            if (grupo == null) grupo = GetComponent<CanvasGroup>();
            if (canvas == null) canvas = GetComponent<Canvas>();

            efecto = efectoTexto as ITextScrambleEffect;
            if (efectoTexto != null && efecto == null)
            {
                Debug.LogWarning("[ScannerUIView] " + efectoTexto.name + " no implementa ITextScrambleEffect. Los textos aparecerán sin efecto.", this);
            }

            // La pantalla no se puede tocar con el ratón y no bloquea clics.
            grupo.interactable = false;
            grupo.blocksRaycasts = false;
            grupo.alpha = 0f;

            CrearTexturas();
            AplicarColoresBase();
            LimpiarTextos();
            if (canvas != null) canvas.enabled = false;
        }

        private void OnDestroy()
        {
            if (texturaScan != null) Destroy(texturaScan);
            if (texturaRuido != null) Destroy(texturaRuido);
        }

        private void Update()
        {
            if (config == null) return;
            float dt = Time.unscaledDeltaTime;

            float duracion = Mathf.Max(0.01f, config.duracionFade);
            alphaVisible = Mathf.MoveTowards(alphaVisible, alphaObjetivo, dt / duracion);

            // Oculta del todo: se apaga el Canvas y no se calcula ningún efecto.
            if (alphaVisible <= 0f && alphaObjetivo <= 0f)
            {
                grupo.alpha = 0f;
                if (canvas != null && canvas.enabled) canvas.enabled = false;
                return;
            }

            ActualizarFlicker(dt);
            ActualizarMarco(dt);
            ActualizarScanlines(dt);
            ActualizarRuido(dt);
            grupo.alpha = alphaVisible * flicker;
        }

        // ── IScannerView ──────────────────────────────────────────────

        public void Mostrar()
        {
            alphaObjetivo = 1f;
            if (canvas != null) canvas.enabled = true;
            StopAllCoroutines();
            LimpiarTextos();
            Destellar(Color.white);
            tiempoGlitch = DuracionGlitchDetectar;
            EscribirCifrado(textoEstado, "> BUSCANDO SEÑAL...", 0f);
        }

        public void Ocultar()
        {
            alphaObjetivo = 0f;
            StopAllCoroutines();
        }

        public void MostrarEnemigo(EnemyScanData datos)
        {
            StopAllCoroutines();
            Destellar(Color.white);
            tiempoGlitch = DuracionGlitchDetectar;

            if (textoAmenaza != null) textoAmenaza.color = ColorAmenaza(datos.amenaza);

            float paso = 0f;
            if (config != null) paso = config.retrasoEntreCampos;

            EscribirCifrado(textoEstado, "> SEÑAL ADQUIRIDA", 0f);
            EscribirCifrado(textoNombre, "ID: " + datos.nombre.ToUpper(), paso);
            EscribirCifrado(textoTipo, "TIPO: " + datos.tipo, paso * 2f);
            EscribirCifrado(textoAmenaza, "AMENAZA: " + TextoAmenaza(datos.amenaza), paso * 3f);
            EscribirCifrado(textoVida, FormatearVida(datos), paso * 4f);
            EscribirCifrado(textoDescripcion, datos.descripcion, paso * 5f);
        }

        public void ActualizarEnemigo(EnemyScanData datos)
        {
            // Los datos cambian en caliente (por ejemplo, la vida): se escriben directo,
            // sin repetir todo el descifrado, con un pequeño glitch como aviso.
            StopAllCoroutines();
            tiempoGlitch = 0.1f;

            PonerTexto(textoEstado, "> SEÑAL ADQUIRIDA");
            PonerTexto(textoNombre, "ID: " + datos.nombre.ToUpper());
            PonerTexto(textoTipo, "TIPO: " + datos.tipo);
            PonerTexto(textoAmenaza, "AMENAZA: " + TextoAmenaza(datos.amenaza));
            PonerTexto(textoVida, FormatearVida(datos));
            PonerTexto(textoDescripcion, datos.descripcion);
            if (textoAmenaza != null) textoAmenaza.color = ColorAmenaza(datos.amenaza);
        }

        public void LimpiarEnemigo()
        {
            StopAllCoroutines();
            LimpiarTextos();
            if (config != null) Destellar(config.colorAlerta);
            tiempoGlitch = DuracionGlitchPerder;
            PonerTexto(textoEstado, "> SEÑAL PERDIDA");
            if (isActiveAndEnabled) StartCoroutine(VolverABuscar());
        }

        // ── Textos ────────────────────────────────────────────────────

        private IEnumerator VolverABuscar()
        {
            yield return new WaitForSecondsRealtime(EsperaTrasPerder);
            EscribirCifrado(textoEstado, "> BUSCANDO SEÑAL...", 0f);
        }

        private void EscribirCifrado(TMP_Text destino, string valor, float retraso)
        {
            if (destino == null) return;
            if (efecto == null || !isActiveAndEnabled)
            {
                destino.text = valor;
                return;
            }
            StartCoroutine(efecto.Revelar(destino, valor, config, retraso));
        }

        private void PonerTexto(TMP_Text destino, string valor)
        {
            if (destino != null) destino.text = valor;
        }

        private void LimpiarTextos()
        {
            PonerTexto(textoEstado, "");
            PonerTexto(textoNombre, "");
            PonerTexto(textoTipo, "");
            PonerTexto(textoAmenaza, "");
            PonerTexto(textoVida, "");
            PonerTexto(textoDescripcion, "");
        }

        private string FormatearVida(EnemyScanData datos)
        {
            if (!datos.TieneVida) return "INTEGRIDAD: --";

            int llenos = Mathf.RoundToInt(datos.vidaActual / datos.vidaMaxima * SegmentosBarraVida);
            string barra = "[";
            for (int i = 0; i < SegmentosBarraVida; i++)
            {
                if (i < llenos) barra += "#";
                else barra += "-";
            }
            barra += "]";

            return "INTEGRIDAD: " + barra + " " + Mathf.CeilToInt(datos.vidaActual) + "/" + Mathf.CeilToInt(datos.vidaMaxima);
        }

        private string TextoAmenaza(NivelAmenaza amenaza)
        {
            switch (amenaza)
            {
                case NivelAmenaza.Baja: return "BAJA";
                case NivelAmenaza.Media: return "MEDIA";
                case NivelAmenaza.Alta: return "ALTA";
                case NivelAmenaza.Critica: return "CRÍTICA";
                default: return "DESCONOCIDA";
            }
        }

        private Color ColorAmenaza(NivelAmenaza amenaza)
        {
            if (config == null) return Color.white;
            switch (amenaza)
            {
                case NivelAmenaza.Baja: return config.colorAmenazaBaja;
                case NivelAmenaza.Media: return config.colorAmenazaMedia;
                case NivelAmenaza.Alta: return config.colorAmenazaAlta;
                case NivelAmenaza.Critica: return config.colorAmenazaCritica;
                default: return config.colorTexto;
            }
        }

        // ── Efectos visuales ──────────────────────────────────────────

        private void Destellar(Color color)
        {
            colorDestello = color;
            tiempoDestello = DuracionDestello;
        }

        private void ActualizarFlicker(float dt)
        {
            if (tiempoGlitch > 0f) tiempoGlitch -= dt;

            proximoFlicker -= dt;
            if (proximoFlicker > 0f) return;

            float intensidad = config.intensidadFlicker;
            if (tiempoGlitch > 0f)
            {
                // Ráfaga fuerte al detectar o perder la señal.
                intensidad = 0.6f;
                proximoFlicker = 0.03f;
            }
            else
            {
                proximoFlicker = Random.Range(0.05f, 0.18f);
            }
            flicker = 1f - Random.value * intensidad;
        }

        private void ActualizarMarco(float dt)
        {
            float pulso = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * config.velocidadPulso);

            Color color = config.colorNeon;
            if (tiempoDestello > 0f)
            {
                tiempoDestello -= dt;
                float t = Mathf.Clamp01(tiempoDestello / DuracionDestello);
                color = Color.Lerp(config.colorNeon, colorDestello, t);
            }

            Color colorBorde = color;
            colorBorde.a = pulso;
            Color colorBrillo = color;
            colorBrillo.a = pulso * config.alphaBrillo;

            Pintar(bordes, colorBorde);
            Pintar(brillos, colorBrillo);
        }

        private void ActualizarScanlines(float dt)
        {
            if (scanlines == null) return;
            desplazamientoScan += dt * config.velocidadScanlines;
            if (desplazamientoScan > 1f) desplazamientoScan -= 1f;

            Rect uv = scanlines.uvRect;
            uv.y = desplazamientoScan;
            scanlines.uvRect = uv;
        }

        private void ActualizarRuido(float dt)
        {
            if (ruido == null) return;
            proximoRuido -= dt;
            if (proximoRuido > 0f) return;
            proximoRuido = 0.05f;

            // Mover la textura a un punto al azar da la sensación de ruido animado
            // sin generar una textura nueva cada vez.
            Rect uv = ruido.uvRect;
            uv.x = Random.value;
            uv.y = Random.value;
            ruido.uvRect = uv;
        }

        private void Pintar(Graphic[] graficos, Color color)
        {
            if (graficos == null) return;
            for (int i = 0; i < graficos.Length; i++)
            {
                if (graficos[i] != null) graficos[i].color = color;
            }
        }

        private void AplicarColoresBase()
        {
            if (config == null) return;

            if (fondo != null)
            {
                Color colorFondo = config.colorFondo;
                colorFondo.a = config.alphaFondo;
                fondo.color = colorFondo;
            }

            if (textoEstado != null) textoEstado.color = config.colorNeon;
            if (textoNombre != null) textoNombre.color = config.colorTexto;
            if (textoTipo != null) textoTipo.color = config.colorTexto;
            if (textoAmenaza != null) textoAmenaza.color = config.colorTexto;
            if (textoVida != null) textoVida.color = config.colorTexto;
            if (textoDescripcion != null) textoDescripcion.color = config.colorTexto;
        }

        private void CrearTexturas()
        {
            if (config == null) return;

            // Líneas de escaneo: textura de 1x4 píxeles que se repite en vertical.
            if (scanlines != null)
            {
                texturaScan = new Texture2D(1, 4, TextureFormat.RGBA32, false);
                texturaScan.wrapMode = TextureWrapMode.Repeat;
                texturaScan.filterMode = FilterMode.Point;
                texturaScan.SetPixel(0, 0, new Color(1f, 1f, 1f, 1f));
                texturaScan.SetPixel(0, 1, new Color(1f, 1f, 1f, 0.4f));
                texturaScan.SetPixel(0, 2, new Color(1f, 1f, 1f, 0f));
                texturaScan.SetPixel(0, 3, new Color(1f, 1f, 1f, 0f));
                texturaScan.Apply();

                Color colorLineas = config.colorNeon;
                colorLineas.a = config.alphaScanlines;
                scanlines.texture = texturaScan;
                scanlines.color = colorLineas;
                scanlines.raycastTarget = false;
                scanlines.uvRect = new Rect(0f, 0f, 1f, Mathf.Max(1f, config.cantidadLineas));
            }

            // Ruido: textura aleatoria de 64x64 que se repite.
            if (ruido != null)
            {
                int lado = 64;
                texturaRuido = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
                texturaRuido.wrapMode = TextureWrapMode.Repeat;
                texturaRuido.filterMode = FilterMode.Point;
                for (int y = 0; y < lado; y++)
                {
                    for (int x = 0; x < lado; x++)
                    {
                        float v = Random.value;
                        texturaRuido.SetPixel(x, y, new Color(v, v, v, Random.value));
                    }
                }
                texturaRuido.Apply();

                ruido.texture = texturaRuido;
                ruido.color = new Color(1f, 1f, 1f, config.alphaRuido);
                ruido.raycastTarget = false;
                ruido.uvRect = new Rect(0f, 0f, 8f, 5f);
            }
        }
    }
}
