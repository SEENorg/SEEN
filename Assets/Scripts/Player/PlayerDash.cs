using UnityEngine;

/// <summary>
/// Dash: un IMPULSO que suma velocidad (no reemplaza el movimiento). Después hay que
/// dejarse frenar por la fricción (agachado = más distancia). Tiene tiempo de recarga.
/// Clase serializable: sus valores aparecen en el Inspector dentro de PlayerController.
/// </summary>
[System.Serializable]
public class PlayerDash
{
    [Header("Dash (impulso)")]
    [SerializeField] private float dashForce = 22f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private bool dashCancelsGravity = true;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;

    public bool IsDashing
    {
        get { return isDashing; }
    }

    /// <summary>Segundos que faltan para poder volver a hacer dash.</summary>
    public float DashCooldownLeft
    {
        get { return dashCooldownTimer; }
    }

    public float DashCooldown
    {
        get { return dashCooldown; }
    }

    /// <summary>Descuenta el tiempo de recarga. Se llama en Update.</summary>
    public void ActualizarCooldown()
    {
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;
    }

    /// <summary>Se puede hacer dash si no se está haciendo uno y la recarga terminó.</summary>
    public bool PuedeIniciar()
    {
        return !isDashing && dashCooldownTimer <= 0f;
    }

    /// <summary>Da el impulso en la dirección del movimiento (o hacia adelante si no hay input).</summary>
    public void Iniciar(Rigidbody rb, float yaw, Vector2 entrada)
    {
        Vector3 fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

        Vector3 dashDir = fwd;
        if (entrada.sqrMagnitude > 0.01f)
            dashDir = (fwd * entrada.y + right * entrada.x).normalized;

        Vector3 v = rb.linearVelocity;
        if (dashCancelsGravity)
            v.y = 0f;

        // IMPULSO: suma velocidad, no reemplaza el movimiento
        rb.linearVelocity = v + dashDir * dashForce;

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
    }

    /// <summary>Termina el dash cuando se acaba su duración. Se llama en FixedUpdate.</summary>
    public void ActualizarFisica()
    {
        if (!isDashing) return;

        dashTimer -= Time.fixedDeltaTime;
        if (dashTimer > 0f) return;

        // Fin del dash: NO se clampea la velocidad — la fricción se encarga
        // de disipar el momentum (agachado = más distancia recorrida).
        isDashing = false;
    }

    /// <summary>Cancela el dash en curso y la recarga al reaparecer.</summary>
    public void Reiniciar()
    {
        isDashing = false;
        dashTimer = 0f;
        dashCooldownTimer = 0f;
    }

    /// <summary>Durante el dash se anula la velocidad vertical (no cae).</summary>
    public void AplicarGravedad(Rigidbody rb)
    {
        if (isDashing && dashCancelsGravity)
        {
            Vector3 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;
        }
    }
}