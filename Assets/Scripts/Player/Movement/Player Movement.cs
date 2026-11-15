using UnityEngine;

/// <summary>
/// Movimiento horizontal con MOMENTUM:
///   • Agachado (slide) conserva el momentum casi sin fricción (la forma rápida de moverse).
///   • De pie también conserva momentum, pero con más fricción.
/// Si el jugador está agachado lo decide PlayerCrouch; aquí solo se aplica cómo se mueve en cada caso.
/// Clase serializable: sus valores aparecen en el Inspector dentro de PlayerController.
/// </summary>
[System.Serializable]
public class PlayerMovement
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Momentum / fricción")]
    [Tooltip("Aceleración al caminar (unidades/s²)")]
    [SerializeField] private float aceleracion = 55f;
    [Tooltip("Fricción en el suelo estando de pie (GRIP: frena al instante)")]
    [SerializeField] private float friccionSuelo = 20f;
    [Tooltip("Fricción en el aire")]
    [SerializeField] private float friccionAire = 1.2f;

    public float MoveSpeed
    {
        get { return moveSpeed; }
    }

    /// <summary>Aplica la aceleración o la fricción a la velocidad horizontal. Se llama en FixedUpdate.</summary>
    public void Mover(Rigidbody rb, Vector2 entrada, float yaw, bool enSuelo)
    {
        Vector3 wish = Quaternion.Euler(0f, yaw, 0f) * new Vector3(entrada.x, 0f, entrada.y);
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        Vector3 v = rb.linearVelocity;
        Vector3 hVel = new Vector3(v.x, 0f, v.z);

        if (wish.sqrMagnitude > 0.01f)
        {
            // DE PIE = GRIP normal: la velocidad se acerca al objetivo
            hVel = Vector3.MoveTowards(hVel, wish * moveSpeed, aceleracion * Time.fixedDeltaTime);
        }
        else
        {
            // Sin input: frenar por fricción
            float friccion = friccionAire;
            if (enSuelo) friccion = friccionSuelo;
            hVel = Vector3.MoveTowards(hVel, Vector3.zero, friccion * Time.fixedDeltaTime);
        }

        rb.linearVelocity = new Vector3(hVel.x, v.y, hVel.z);
    }
}