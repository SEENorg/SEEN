using UnityEngine;

/// <summary>
/// Plataforma móvil: se desplaza de forma suave (senoidal) entre su posición
/// inicial y posición inicial + moveAmount.
///
/// FIX "el jugador viaja con la plataforma":
///  · Se mueve con rb.MovePosition (Rigidbody cinemático) dentro de FixedUpdate
///    para que la fricción arrastre al jugador.
///  · El recorrido es suavizado con una curva senoidal → al invertir el sentido
///    no hay tirones que lancen al jugador.
///  · Expone <see cref="Velocity"/> para que el PlayerController transporte al
///    jugador exactamente con la plataforma (sin resbalarse) y limite su
///    velocidad vertical (sin salir disparado hacia arriba).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MovingPlatform : MonoBehaviour
{
    [Tooltip("Desplazamiento total del recorrido")]
    public Vector3 moveAmount = new Vector3(6f, 0f, 0f);
    [Tooltip("Velocidad del recorrido (radianes/seg aprox; valores chicos = lento)")]
    public float speed = 1.2f;

    private Vector3 startPos;
    private Vector3 lastPos;
    private Rigidbody rb;

    /// <summary>Velocidad actual de la plataforma (unidades/segundo, mundial).</summary>
    public Vector3 Velocity { get; private set; }

    private void Awake()
    {
        startPos = transform.position;
        lastPos = startPos;
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void FixedUpdate()
    {
        // Seno suavizado: 0 → 1 → 0 (sin paradas bruscas)
        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        Vector3 destino = startPos + moveAmount * t;

        rb.MovePosition(destino);

        // Velocidad real para el transporte del jugador
        Velocity = (destino - lastPos) / Time.fixedDeltaTime;
        lastPos = destino;
    }
}
