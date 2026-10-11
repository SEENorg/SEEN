using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpForce = 7f;
    
    [Header("Ajustes del Dash")]
    [SerializeField] private float dashForce = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Detección de Suelo (Ground Check)")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundDistance = 0.4f;

    private Rigidbody rb;
    private Camera mainCamera;
    
    // Estado del salto
    private bool isGrounded;
    private int jumpCount;
    private int maxJumps = 2; // Doble salto

    // Estado del dash
    private bool isDashing;
    private float dashCooldownTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        
        // Evita que la cápsula se caiga hacia los lados por las físicas
        rb.freezeRotation = true; 
    }

    void Update()
    {
        if (isDashing) return; // Si está en dash, no permite saltar ni caminar normal

        CheckGrounded();
        HandleJump();
        HandleDash();
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        HandleMovement();
    }

    private void CheckGrounded()
    {
        if (groundCheck == null)
        {
            Debug.LogWarning("No has asignado el GroundCheck en el inspector.");
            return;
        }
        
        // Crea una pequeña esfera en los pies del jugador para ver si toca la capa de suelo
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer);

        // Si toca el suelo y no está subiendo, resetea los saltos
        if (isGrounded && rb.linearVelocity.y <= 0f)
        {
            jumpCount = 0;
        }
    }

    private void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal"); // A, D, Flechas
        float z = Input.GetAxisRaw("Vertical");   // W, S, Flechas

        if (mainCamera == null) return;

        // Calcula el movimiento relativo a hacia dónde mira la cámara
        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        
        // Ignoramos la rotación en el eje Y para que no se entierre en el suelo al mirar abajo
        forward.y = 0f;
        right.y = 0f;
        
        forward.Normalize();
        right.Normalize();

        Vector3 move = (forward * z + right * x).normalized * moveSpeed;
        
        // Aplicamos la velocidad manteniendo la gravedad actual (rb.velocity.y)
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);

        // Si el jugador se mueve, hacer que mire hacia esa dirección
        if (move != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 10f);
        }
    }

    private void HandleJump()
    {
        // Usa el botón de salto por defecto de Unity (Espacio)
        if (Input.GetButtonDown("Jump") && jumpCount < maxJumps)
        {
            // Resetea la velocidad vertical antes de saltar para que el doble salto sea consistente
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpCount++;
        }
    }

    private void HandleDash()
    {
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        // Usa Shift Izquierdo o click derecho para el Dash
        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetMouseButtonDown(1)) && dashCooldownTimer <= 0f)
        {
            StartCoroutine(PerformDash());
        }
    }

    private IEnumerator PerformDash()
    {
        isDashing = true;
        
        // Guardamos si la gravedad estaba activada y la desactivamos para un dash recto
        bool originalGravity = rb.useGravity;
        rb.useGravity = false;
        
        // Obtenemos la dirección hacia donde mira la cámara, ignorando el eje Y
        Vector3 dashDirection = mainCamera.transform.forward;
        dashDirection.y = 0f;
        dashDirection.Normalize();

        if (dashDirection == Vector3.zero)
            dashDirection = transform.forward;

        // Limpiamos la velocidad actual e impulsamos
        rb.linearVelocity = Vector3.zero; 
        rb.AddForce(dashDirection * dashForce, ForceMode.VelocityChange);

        // Esperamos a que termine el dash
        yield return new WaitForSeconds(dashDuration);

        // Terminamos el dash
        rb.useGravity = originalGravity;
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); // Frenamos el impulso horizontal
        isDashing = false;
        dashCooldownTimer = dashCooldown;
    }
}