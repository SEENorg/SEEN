using UnityEngine;

/// <summary>
/// Punto de control: al tocarlo, el jugador reaparecerá aquí si muere.
/// Muestra un faro emisivo como referencia visual.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Faro emisivo (decoración). Se pone verde al activarse.")]
    public Renderer beaconRenderer;

    private bool activated;

    private void Start()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activated) return;
        if (!other.CompareTag("Player")) return;

        activated = true;

        var respawn = other.GetComponentInParent<RespawnOnFall>();
        if (respawn != null)
            respawn.SetSpawnPoint(transform.position);

        if (beaconRenderer != null)
            beaconRenderer.material.SetColor("_EmissionColor", Color.green);

        Debug.Log("[Checkpoint] Punto de control activado: " + name);
    }
}
