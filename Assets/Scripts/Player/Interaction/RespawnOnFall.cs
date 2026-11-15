using UnityEngine;

/// <summary>
/// Reaparición del jugador: al caer al vacío (killY) o al tocar un hazard,
/// vuelve al último checkpoint activado con la velocidad limpia.
/// </summary>
public class RespawnOnFall : MonoBehaviour
{
    [Tooltip("Altura por debajo de la cual el jugador reaparece")]
    [SerializeField] private float killY = -10f;

    private Vector3 spawnPoint;
    private Rigidbody rb;
    private PlayerController jugador;

    private void Start()
    {
        spawnPoint = transform.position;
        rb = GetComponent<Rigidbody>();
        jugador = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        PlayerKill.OnKill += DoRespawn;
    }

    private void OnDisable()
    {
        PlayerKill.OnKill -= DoRespawn;
    }

    private void Update()
    {
        if (transform.position.y < killY)
            DoRespawn();
    }

    /// <summary>Actualiza el punto de reaparición (llamado por los checkpoints).</summary>
    public void SetSpawnPoint(Vector3 point)
    {
        spawnPoint = point + Vector3.up * 0.5f;

        // El antivirus vuelve a su posición original al tomar un checkpoint
        Antivirus av = Object.FindFirstObjectByType<Antivirus>();
        if (av != null) av.Reiniciar();
    }

    /// <summary>Reaparece al jugador. Público para que los hazards también lo llamen.</summary>
    public void DoRespawn()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        transform.position = spawnPoint;

        // Saltos, buffers, dash y agachado vuelven a su estado inicial
        if (jugador != null) jugador.ResetEstado();

        Debug.Log("[Respawn] Reapareciendo en el último checkpoint.");
    }
}