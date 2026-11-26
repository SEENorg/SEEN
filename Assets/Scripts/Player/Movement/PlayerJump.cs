using UnityEngine;

/// <summary>
/// Salto y doble salto, con buffer de salto (el salto se "recuerda" unos instantes) y
/// una guarda anti-doble-conteo al despegar. Avisa con un evento cuando se hace el doble salto.
/// Clase serializable: sus valores aparecen en el Inspector dentro de PlayerController.
/// </summary>
[System.Serializable]
public class PlayerJump
{
    [Header("Salto")]
    [SerializeField, Min(1)] private int maxJumps = 2;
    [SerializeField] private float jumpForce = 13.5f;
    [SerializeField, Range(0.5f, 1f)] private float doubleJumpMultiplier = 0.9f;

    [Header("Game Feel")]
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float jumpGroundGuard = 0.12f;

    private int jumpsLeft;
    private float jumpBufferTimer;
    private float groundGuardTimer;

    /// <summary>Se dispara al hacer un doble salto (con la posición del jugador).</summary>
    public event System.Action<Vector3> OnDobleSalto;

    public int JumpsLeft
    {
        get { return jumpsLeft; }
    }

    public int MaxJumps
    {
        get { return maxJumps; }
    }

    /// <summary>Registra si se presionó salto en este frame (buffer de salto). Se llama en Update.</summary>
    public void RegistrarEntrada(bool saltoPresionado)
    {
        if (saltoPresionado)
            jumpBufferTimer = jumpBufferTime;
        else if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;
    }

    /// <summary>Recarga los saltos al tocar el suelo y gasta el de suelo si se cae de un borde. FixedUpdate.</summary>
    public void ActualizarSaltosRestantes(bool enSuelo, bool estabaEnSuelo)
    {
        if (groundGuardTimer > 0f)
            groundGuardTimer -= Time.fixedDeltaTime;

        if (enSuelo)
        {
            if (groundGuardTimer <= 0f)
                jumpsLeft = maxJumps;
        }
        else if (estabaEnSuelo && groundGuardTimer <= 0f)
        {
            // Salió del suelo sin saltar (se cayó de un borde): consume el salto "de suelo"
            if (jumpsLeft >= maxJumps)
                jumpsLeft = maxJumps - 1;
        }
    }

    /// <summary>Limpia el estado al reaparecer: restaura salto y doble salto sin residuos del buffer.</summary>
    public void Reiniciar()
    {
        if (maxJumps < 2) maxJumps = 2;
        jumpsLeft = maxJumps;
        jumpBufferTimer = 0f;
        groundGuardTimer = 0f;
    }

    /// <summary>Inicializa los saltos disponibles al empezar el nivel.</summary>
    public void Inicializar()
    {
        if (maxJumps < 2) maxJumps = 2;
        jumpsLeft = maxJumps;
    }

    /// <summary>Garantiza al menos un salto en el aire tras soltar el gancho.</summary>
    public void RestaurarSaltoAereo()
    {
        if (jumpsLeft < 1) jumpsLeft = 1;
    }

    /// <summary>Salta si hay un salto pendiente en el buffer y quedan saltos. Se llama en FixedUpdate.</summary>
    public void IntentarSaltar(Rigidbody rb, Vector3 posicionJugador)
    {
        if (jumpBufferTimer <= 0f || jumpsLeft <= 0) return;

        bool isDoubleJump = jumpsLeft < maxJumps;
        float force = jumpForce;
        if (isDoubleJump) force = jumpForce * doubleJumpMultiplier;

        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;
        rb.AddForce(Vector3.up * force, ForceMode.Impulse);

        jumpsLeft--;
        jumpBufferTimer = 0f;
        groundGuardTimer = jumpGroundGuard;

        if (isDoubleJump && OnDobleSalto != null)
            OnDobleSalto(posicionJugador);
    }
}