using UnityEngine;

/// <summary>
/// Hazard de barras rotatorias (estilo "notificaciones" del mundo SEEN).
/// Gira constantemente y mata al jugador si lo toca.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RotatingHazard : MonoBehaviour
{
    [Tooltip("Velocidad de rotación en grados/segundo por eje")]
    public Vector3 rotationSpeed = new Vector3(0f, 90f, 0f);

    private void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            PlayerKill.KillPlayer();
    }
}
