using UnityEngine;

/// <summary>
/// Efecto de viento/velocidad para el DASH: partículas que nacen en un círculo
/// de un cuarto de la pantalla y se expansionan hacia afuera, atravesando el
/// borde hasta desaparecer. No giran alrededor de la pantalla.
/// Se coloca en el jugador (dibuja en OnGUI).
/// </summary>
public class SpeedLines : MonoBehaviour
{
    [Header("Apariencia")]
    [Tooltip("Cantidad de partículas")]
    [SerializeField] private int cantidad = 70;
    [Tooltip("Radio inicial (fracción del alto de pantalla)")]
    [SerializeField] private float radioInicial = 0.3f;
    [Tooltip("Largo del trazo")]
    [SerializeField] private float largo = 200f;
    [SerializeField] private float grosor = 1.5f;
    [SerializeField] private Color color = new Color(0.78f, 0.93f, 1f, 0.7f);

    [Header("Comportamiento")]
    [SerializeField] private float velocidadFade = 5f;
    [SerializeField] private float velocidadExpansion = 0.85f;

    private PlayerController pc;
    private float intensidad;
    private float fase;
    private Texture2D textura;

    private void Awake()
    {
        textura = new Texture2D(1, 1);
        textura.SetPixel(0, 0, Color.white);
        textura.Apply();
    }

    private void Start()
    {
        pc = GetComponent<PlayerController>();
    }

    private void Update()
    {
        bool dashing = pc != null && pc.IsDashing;

        float objetivo;
        if (dashing)
            objetivo = 1f;
        else
            objetivo = 0f;

        float multiplicador;
        if (dashing)
            multiplicador = 3f;
        else
            multiplicador = 1f;

        intensidad = Mathf.MoveTowards(intensidad, objetivo,
                        Time.deltaTime * velocidadFade * multiplicador);
        fase += Time.deltaTime * velocidadExpansion;
    }

    private void OnGUI()
    {
        if (intensidad <= 0.02f) return;

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        float r0 = radioInicial * Screen.height;                       // nace a 1/4 de pantalla
        float rMax = new Vector2(Screen.width, Screen.height).magnitude * 0.55f;  // más allá de las esquinas

        Color c = color;
        GUI.color = color;

        for (int i = 0; i < cantidad; i++)
        {
            // Ángulo FIJO por partícula (no giratorio)
            float ang = (i * 137.507f) % 360f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            // Progreso individible de la partícula
            float prog = ((i / (float)cantidad) + fase * (0.6f + (i % 4) * 0.15f)) % 1f;
            float r = r0 + prog * (rMax - r0);
            if (r > rMax) continue;

            // Largo crece con la intensidad
            float largoActual = largo * (0.35f + 0.65f * intensidad);

            Vector2 p1 = new Vector2(cx + dir.x * r, cy + dir.y * r);
            Vector2 p2 = new Vector2(cx + dir.x * (r + largoActual), cy + dir.y * (r + largoActual));

            // Desvanecer: aparece suave y se pierde al salir
            float alpha = intensidad;
            if (prog < 0.12f)
            {
                alpha *= prog / 0.12f;
            }
            else if (prog > 0.75f)
            {
                alpha *= 1f - ((prog - 0.75f) / 0.25f);
            }

            c.a = color.a * alpha;
            GUI.color = c;

            Linea(p1, p2, grosor);
        }
    }

    private void Linea(Vector2 desde, Vector2 hasta, float ancho)
    {
        Vector2 delta = hasta - desde;
        float largoTrazo = delta.magnitude;
        if (largoTrazo < 0.5f) return;

        float ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        GUIUtility.RotateAroundPivot(ang, desde);
        GUI.DrawTexture(new Rect(desde.x, desde.y - ancho / 2f, largoTrazo, ancho), textura);
        GUI.matrix = Matrix4x4.identity;
    }
}