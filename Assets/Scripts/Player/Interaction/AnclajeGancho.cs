using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Nodo superior de enganche para el Gancho en el Nivel 4.
/// Se registra automáticamente en una lista estática y mantiene una rotación
/// y pulso emisivo suave para identificar fácilmente cada punto de columpio.
/// </summary>
public class AnclajeGancho : MonoBehaviour
{
    public static readonly List<AnclajeGancho> Todos = new List<AnclajeGancho>();

    [Header("Apariencia")]
    [SerializeField] private float velocidadGiro = 65f;
    [SerializeField] private float velocidadPulso = 3.2f;

    private Transform anilloVisual;
    private Material materialEmisivo;
    private Color colorBaseEmision = new Color(0.15f, 0.9f, 1f);
    private Vector3 escalaBaseAnillo = Vector3.one;
    private bool visualCreado;

    public Vector3 PuntoEnganche
    {
        get { return transform.position; }
    }

    private void OnEnable()
    {
        if (!Todos.Contains(this))
            Todos.Add(this);
    }

    private void OnDisable()
    {
        Todos.Remove(this);
    }

    private void Start()
    {
        AsegurarVisual();
    }

    private void OnDestroy()
    {
        Todos.Remove(this);
        if (materialEmisivo != null)
            Destroy(materialEmisivo);
    }

    public void ConfigurarVisual(Transform anillo, Material matEmisivo, Color colorEmision)
    {
        anilloVisual = anillo;
        materialEmisivo = matEmisivo;
        colorBaseEmision = colorEmision;
        if (anilloVisual != null)
            escalaBaseAnillo = anilloVisual.localScale;
        visualCreado = true;
    }

    private void AsegurarVisual()
    {
        if (visualCreado) return;
        visualCreado = true;

        if (GetComponentInChildren<MeshRenderer>() != null)
        {
            anilloVisual = transform.childCount > 0 ? transform.GetChild(0) : transform;
            escalaBaseAnillo = anilloVisual.localScale;
            return;
        }

        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        materialEmisivo = new Material(sh);
        materialEmisivo.color = new Color(0.05f, 0.35f, 0.45f);
        if (materialEmisivo.HasProperty("_BaseColor"))
            materialEmisivo.SetColor("_BaseColor", new Color(0.05f, 0.35f, 0.45f));
        materialEmisivo.EnableKeyword("_EMISSION");
        materialEmisivo.SetColor("_EmissionColor", colorBaseEmision * 3f);

        GameObject nucleo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nucleo.name = "Anclaje_Nucleo";
        nucleo.transform.SetParent(transform, false);
        nucleo.transform.localScale = Vector3.one * 0.65f;
        Destroy(nucleo.GetComponent<Collider>());
        nucleo.GetComponent<MeshRenderer>().sharedMaterial = materialEmisivo;

        GameObject aro = GameObject.CreatePrimitive(PrimitiveType.Cube);
        aro.name = "Anclaje_Aro";
        aro.transform.SetParent(transform, false);
        aro.transform.localScale = new Vector3(1.15f, 0.2f, 1.15f);
        Destroy(aro.GetComponent<Collider>());
        aro.GetComponent<MeshRenderer>().sharedMaterial = materialEmisivo;

        anilloVisual = aro.transform;
        escalaBaseAnillo = aro.transform.localScale;
    }

    private void Update()
    {
        if (anilloVisual != null)
        {
            anilloVisual.Rotate(0f, velocidadGiro * Time.deltaTime, 0f, Space.Self);
            float pulso = 1f + Mathf.Sin(Time.time * velocidadPulso) * 0.08f;
            anilloVisual.localScale = escalaBaseAnillo * pulso;
        }

        if (materialEmisivo != null)
        {
            float intensidad = 2.6f + Mathf.Sin(Time.time * velocidadPulso) * 0.8f;
            materialEmisivo.SetColor("_EmissionColor", colorBaseEmision * intensidad);
        }
    }
}
