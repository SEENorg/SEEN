using UnityEngine;

/// <summary>
/// Puerta que se abre cuando se completa el hackeo de su tableta.
/// Al abrirse se hunde en el piso y su collider se desactiva.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DoorController : MonoBehaviour
{
    [Tooltip("Metros que baja al abrirse")]
    public float apertura = 4.5f;
    [Tooltip("Velocidad de apertura (unidades/segundo)")]
    public float velocidad = 3.5f;

    private bool abierta;
    private Vector3 posCerrada;
    private Collider col;

    private void Start()
    {
        posCerrada = transform.position;
        col = GetComponent<Collider>();
    }

    private void Update()
    {
        if (!abierta) return;
        if (transform.position.y <= posCerrada.y - apertura) return;
        transform.Translate(Vector3.down * velocidad * Time.deltaTime, Space.World);
    }

    /// <summary>Abre la puerta.</summary>
    public void Abrir()
    {
        if (abierta) return;
        abierta = true;
        if (col != null) col.enabled = false;
    }
}
