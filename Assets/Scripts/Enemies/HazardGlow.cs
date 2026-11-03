using UnityEngine;

/// <summary>
/// Hace que un objeto dañino emanen luz verde pulsante (los "objetos dañinos"
/// del mundo SEEN se ven como energía verde tóxica).
/// Usa una instancia propia del material para no afectar a los demás objetos.
/// </summary>
public class HazardGlow : MonoBehaviour
{
    [Tooltip("Intensidad mínima de emisión")]
    public float minIntensidad = 1.6f;
    [Tooltip("Intensidad máxima de emisión")]
    public float maxIntensidad = 4.5f;
    [Tooltip("Velocidad del pulso")]
    public float velocidad = 2.2f;
    [Tooltip("Luz verde que emana (opcional, se crea si no hay)")]
    public bool crearLuz = true;

    private Material mat;
    private Color emisionBase;
    private Light luz;

    private void Awake()
    {
        var rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            mat = rend.material;   // instancia propia
            mat.EnableKeyword("_EMISSION");
            emisionBase = mat.GetColor("_EmissionColor");
            if (emisionBase == Color.clear || emisionBase.maxColorComponent <= 0f)
                emisionBase = new Color(0.1f, 1f, 0.25f);
        }

        if (crearLuz)
        {
            luz = GetComponent<Light>();
            if (luz == null) luz = gameObject.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = new Color(0.25f, 1f, 0.35f);
            luz.range = 6f;
            luz.intensity = 2.5f;
            luz.shadows = LightShadows.None;
        }
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * velocidad) + 1f) * 0.5f;
        float intensidad = Mathf.Lerp(minIntensidad, maxIntensidad, t);

        if (mat != null)
            mat.SetColor("_EmissionColor", emisionBase * intensidad);

        if (luz != null)
            luz.intensity = intensidad * 0.7f;
    }
}
