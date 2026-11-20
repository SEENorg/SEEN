using UnityEngine;

/// <summary>
/// Detección de suelo bajo el jugador y transporte con plataformas móviles.
/// Clase serializable: sus valores aparecen en el Inspector dentro de PlayerController.
/// </summary>
[System.Serializable]
public class PlayerGround
{
    [Header("Detección de suelo")]
    [Tooltip("Capas de suelo. Si lo dejás en Nothing, detecta contra todo.")]
    [SerializeField] private LayerMask groundMask;
    [Tooltip("Margen extra bajo los pies para considerar que está en suelo")]
    [SerializeField] private float groundSkin = 0.12f;

    private CapsuleCollider capsule;
    private int mascara;
    private bool enSuelo;
    private bool estabaEnSuelo;
    private MovingPlatform plataforma;

    public bool EstaEnSuelo
    {
        get { return enSuelo; }
    }

    /// <summary>Si estaba en el suelo en el paso de física anterior.</summary>
    public bool EstabaEnSuelo
    {
        get { return estabaEnSuelo; }
    }

    public void Iniciar(CapsuleCollider capsula)
    {
        capsule = capsula;

        // La máscara se calcula una sola vez
        mascara = ~0;
        if (groundMask.value != 0) mascara = groundMask.value;
    }

    /// <summary>Lanza detección hacia abajo desde los pies usando el bounds en mundo. Se llama en FixedUpdate.</summary>
    public void Detectar(Transform jugador)
    {
        if (capsule == null)
        {
            enSuelo = false;
            plataforma = null;
            return;
        }

        Bounds b = capsule.bounds;
        float radioEsfera = Mathf.Min(b.extents.x, b.extents.z) * 0.85f;
        float distEsfera = Mathf.Max(0.05f, (b.extents.y - radioEsfera) + groundSkin + 0.08f);

        enSuelo = false;
        plataforma = null;

        RaycastHit[] hits = Physics.SphereCastAll(b.center, radioEsfera, Vector3.down, distEsfera, mascara, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i].collider;
            if (c == null || c == capsule || c.transform.IsChildOf(jugador)) continue;
            if (hits[i].normal.y <= 0.35f) continue;

            enSuelo = true;
            plataforma = c.GetComponentInParent<MovingPlatform>();
            return;
        }

        float distRayo = b.extents.y + groundSkin + 0.08f;
        RaycastHit[] rayHits = Physics.RaycastAll(b.center, Vector3.down, distRayo, mascara, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < rayHits.Length; i++)
        {
            Collider c = rayHits[i].collider;
            if (c == null || c == capsule || c.transform.IsChildOf(jugador)) continue;
            if (rayHits[i].normal.y <= 0.35f) continue;

            enSuelo = true;
            plataforma = c.GetComponentInParent<MovingPlatform>();
            return;
        }
    }

    /// <summary>Guarda el estado de este paso de física para el siguiente. Se llama al final de FixedUpdate.</summary>
    public void GuardarEstado()
    {
        estabaEnSuelo = enSuelo;
    }

    /// <summary>
    /// Mueve al jugador POR POSICIÓN con la plataforma sobre la que está (no se le suma
    /// velocidad, porque eso lo disparaba al desplazarse).
    /// </summary>
    public void TransportarConPlataforma(Rigidbody rb)
    {
        if (plataforma == null) return;

        Vector3 delta = plataforma.Velocity * Time.fixedDeltaTime;
        if (delta.sqrMagnitude > 0.0001f)
            rb.MovePosition(rb.position + delta);

        // Anti-lanzamiento: si la plataforma sube, el jugador no la supera
        float subida = plataforma.Velocity.y;
        if (subida > 0f && rb.linearVelocity.y > subida)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, subida, rb.linearVelocity.z);
    }
}