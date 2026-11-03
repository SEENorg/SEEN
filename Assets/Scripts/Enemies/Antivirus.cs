using UnityEngine;

/// <summary>
/// ANTIVIRUS: amalgama de cubos rojos brillantes con 0s y 1s que persigue al
/// jugador por el nivel. Si lo alcanza → GAME OVER.
/// Flota sobre el nivel (ignora obstáculos) y reaparece en su punto de
/// origen cuando el jugador reaparece en un checkpoint.
/// </summary>
public class Antivirus : MonoBehaviour
{
    [Header("Persecución")]
    [Tooltip("Velocidad de persecución (más lenta que el jugador rápido)")]
    public float velocidad = 6.4f;
    [Tooltip("Distancia a la que atrapa al jugador")]
    public float radioAtrapar = 2.2f;
    [Tooltip("Retraso inicial antes de empezar a perseguir (segundos)")]
    public float retrasoInicial = 2.5f;
    [Tooltip("Persigue siempre, sin importar la distancia al jugador")]
    public bool persigueSiempre = true;

    [Header("Apariencia")]
    [Tooltip("Cantidad de cubos de la amalgama")]
    [SerializeField] private int cantidadCubos = 14;
    [SerializeField] private Color colorCubo = new Color(1f, 0.08f, 0.08f);
    [SerializeField] private float tamanoCubo = 0.55f;
    [SerializeField] private float radioAmalgama = 1.6f;

    [Header("Estado")]
    [SerializeField] private bool persiguiendo = true;

    private Transform jugador;
    private Vector3 posicionInicial;
    private float temporizador;
    private readonly System.Collections.Generic.List<Transform> cubos = new System.Collections.Generic.List<Transform>();
    private Light luz;

    public float Velocidad => velocidad;

    private void Start()
    {
        posicionInicial = transform.position;
        ConstruirAmalgama();

        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null) jugador = pc.transform;
    }

    private void ConstruirAmalgama()
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.SetColor("_BaseColor", colorCubo * 0.4f);
        mat.color = colorCubo * 0.4f;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", colorCubo * 2.5f);

        var rng = new System.Random(99);
        for (int i = 0; i < cantidadCubos; i++)
        {
            var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.name = "Antivirus_Cubo_" + i;
            cubo.transform.SetParent(transform, true);

            // Distribución esférica alrededor del centro
            float u = (float)rng.NextDouble();
            float v = (float)rng.NextDouble();
            float theta = u * Mathf.PI * 2f;
            float phi = Mathf.Acos(2f * v - 1f);
            float r = radioAmalgama * (0.4f + 0.6f * Mathf.Sqrt((float)rng.NextDouble()));
            Vector3 offset = new Vector3(
                r * Mathf.Sin(phi) * Mathf.Cos(theta),
                r * Mathf.Cos(phi),
                r * Mathf.Sin(phi) * Mathf.Sin(theta));

            cubo.transform.localPosition = offset;
            float escala = tamanoCubo * (0.6f + 0.8f * (float)rng.NextDouble());
            cubo.transform.localScale = Vector3.one * escala;
            cubo.transform.rotation = Quaternion.Euler(
                (float)rng.NextDouble() * 90f, (float)rng.NextDouble() * 90f, (float)rng.NextDouble() * 90f);
            cubo.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(cubo.GetComponent<Collider>());

            // Dígito 0 o 1 flotando sobre el cubo
            var textoGO = new GameObject("Digito");
            textoGO.transform.SetParent(cubo.transform, true);
            var tm = textoGO.AddComponent<TextMesh>();
            tm.text = rng.Next(0, 2) == 0 ? "0" : "1";
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.fontSize = 48;
            tm.characterSize = 0.06f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 0.85f, 0.85f);
            textoGO.transform.localPosition = Vector3.zero;
            textoGO.transform.localRotation = Quaternion.identity;
            textoGO.transform.localScale = Vector3.one / escala;

            cubos.Add(cubo.transform);
        }

        // Luz roja que emana
        luz = GetComponent<Light>();
        if (luz == null) luz = gameObject.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = colorCubo;
        luz.range = 14f;
        luz.intensity = 6f;

        // Pulso sutil de la luz
        InvokeRepeating(nameof(PulsoLuz), 0f, 0.12f);
    }

    private float pulsoT;
    private void PulsoLuz()
    {
        if (luz == null) return;
        pulsoT += 0.12f;
        luz.intensity = 5f + Mathf.Sin(pulsoT * 6f) * 1.5f;
    }

    private void Update()
    {
        if (jugador == null)
        {
            var pc = Object.FindFirstObjectByType<PlayerController>();
            if (pc != null) jugador = pc.transform;
            if (jugador == null) return;
        }

        // Rotación constante de la amalgama
        foreach (var c in cubos)
            if (c != null) c.Rotate(Vector3.one * 40f * Time.deltaTime, Space.Self);

        if (!persiguiendo) return;

        temporizador += Time.deltaTime;
        if (temporizador < retrasoInicial) return;

        Vector3 destino = jugador.position + Vector3.up * 0.5f;

        // Persecución directa — SIN límite de distancia, siempre te busca
        transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);

        // ¿Atrapó al jugador?
        float dist = Vector3.Distance(transform.position, destino);
        if (dist < radioAtrapar)
        {
            persiguiendo = false;
            GameFeedback.MostrarGameOver();
        }
    }

    /// <summary>Reinicia la persecución (al reaparecer en un checkpoint).</summary>
    public void Reiniciar()
    {
        transform.position = posicionInicial;
        temporizador = 0f;
        persiguiendo = true;
    }

    /// <summary>Detiene la persecución (nivel completado).</summary>
    public void Detener()
    {
        persiguiendo = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioAtrapar);
    }
}
