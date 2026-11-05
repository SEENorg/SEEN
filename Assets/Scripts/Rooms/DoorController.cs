using UnityEngine;

/// <summary>
/// Puerta que se abre cuando se completa el hackeo de su tableta.
/// Al abrirse se desplaza en la dirección configurada (hacia abajo por defecto,
/// o hacia los lados para puertas corredizas) y TODOS sus colliders (el de la
/// raíz y los de los hijos) se desactivan. Funciona con puertas simples (un cubo)
/// y con prefabs de assets (modelo en un hijo, collider en otro hijo).
/// </summary>
[RequireComponent(typeof(Collider))]
public class DoorController : MonoBehaviour
{
    [Header("Movimiento al abrir")]
    [Tooltip("Dirección en ejes LOCALES de la puerta. Hundirse: (0,-1,0). Hacia los lados: (1,0,0) o (-1,0,0). " +
             "Si no se mueve hacia donde quieres, prueba el signo contrario o el eje Z: (0,0,1).")]
    public Vector3 direccionApertura = Vector3.down;

    [Tooltip("Metros que se mueve. Si es 0, se calcula solo con la medida de la puerta en esa dirección.")]
    public float distancia = 0f;

    [Tooltip("Metros extra que se suman a la distancia calculada automáticamente")]
    public float margenExtra = 0.5f;

    [Tooltip("Velocidad de apertura (unidades/segundo)")]
    public float velocidad = 3.5f;

    [Tooltip("Metros de respaldo si no se puede medir la puerta")]
    public float apertura = 4.5f;

    private bool abierta;
    private Collider[] colliders;
    private Vector3 destino;

    private void Start()
    {
        colliders = GetComponentsInChildren<Collider>();

        // Dirección de apertura en el mundo (se calcula una vez, con la puerta cerrada)
        Vector3 dirLocal = direccionApertura;
        if (dirLocal.sqrMagnitude < 0.0001f)
            dirLocal = Vector3.down;
        Vector3 dirMundo = transform.TransformDirection(dirLocal.normalized);

        float recorrido = distancia;
        if (recorrido <= 0f)
            recorrido = MedirEnDireccion(dirMundo) + margenExtra;

        destino = transform.position + dirMundo * recorrido;
        Debug.Log("[DoorController] " + name + " direccion=" + dirMundo + " recorrido=" + recorrido);
    }

    private void Update()
    {
        if (!abierta) return;
        transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);
    }

    /// <summary>Abre la puerta.</summary>
    public void Abrir()
    {
        Debug.Log("[DoorController] Abrir() llamado en " + name);
        if (abierta) return;
        abierta = true;

        // Desactivar todos los colliders: el de la raíz y los de los hijos
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        // Si el prefab trae un sonido, se reproduce al abrir
        AudioSource sonido = GetComponent<AudioSource>();
        if (sonido != null) sonido.Play();
    }

    /// <summary>Medida de la puerta (con todos sus renderers) a lo largo de una dirección del mundo.</summary>
    private float MedirEnDireccion(Vector3 dirMundo)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return apertura;

        Bounds limites = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            limites.Encapsulate(renderers[i].bounds);

        Vector3 medida = limites.size;
        return Mathf.Abs(dirMundo.x) * medida.x
             + Mathf.Abs(dirMundo.y) * medida.y
             + Mathf.Abs(dirMundo.z) * medida.z;
    }
}