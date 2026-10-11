using UnityEngine;

/// <summary>
/// Entidad hostil que patrulla entre dos puntos (en mundo SEEN serían
/// "comentarios hostiles" personificados). Mata al jugador al tocarlo.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PatrolHazard : MonoBehaviour
{
    [Tooltip("Punto A del recorrido (coordenadas de mundo)")]
    public Vector3 pointA = Vector3.left * 4f;
    [Tooltip("Punto B del recorrido (coordenadas de mundo)")]
    public Vector3 pointB = Vector3.right * 4f;
    [Tooltip("Velocidad de patrulla (unidades/segundo)")]
    public float speed = 2.5f;

    private Vector3 previousPos;

    private void Start()
    {
        transform.position = pointA;
        previousPos = pointA;
    }

    private void Update()
    {
        float t = Mathf.PingPong(Time.time * speed / Vector3.Distance(pointA, pointB), 1f);
        Vector3 next = Vector3.Lerp(pointA, pointB, t);

        // Orientar hacia el movimiento
        Vector3 dir = next - previousPos;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir);

        previousPos = next;
        transform.position = next;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            PlayerKill.KillPlayer();
    }
}
