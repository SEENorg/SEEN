using UnityEngine;

/// <summary>
/// Barra de piso que se enciende en rojo de forma aleatoria.
/// Apagada (color normal) → aviso corto (naranja) → encendida (roja, mata) → apagada...
/// Cada barra tiene sus propios tiempos aleatorios, así no se sincronizan entre ellas.
///
/// Uso: ponlo en cada barra (necesita Renderer y Collider).
/// </summary>
[RequireComponent(typeof(Collider))]
public class BarraPeligro : MonoBehaviour
{
    private enum Estado { Apagada, Aviso, Encendida }

    [Header("Tiempos (segundos, se sortea un valor entre mínimo y máximo)")]
    [SerializeField] private float apagadaMin = 2.5f;
    [SerializeField] private float apagadaMax = 5.0f;
    [SerializeField] private float encendidaMin = 2.5f;
    [SerializeField] private float encendidaMax = 4.0f;

    [Tooltip("Parpadeo de advertencia antes de ponerse roja (0 = sin aviso)")]
    [SerializeField] private float tiempoAviso = 0.8f;

    [Header("Colores")]
    [SerializeField] private Color colorApagada = new Color(0.1f, 0.35f, 0.45f);
    [SerializeField] private Color colorAviso = new Color(1f, 0.55f, 0f);
    [SerializeField] private Color colorEncendida = new Color(1f, 0.05f, 0.05f);

    [Header("Daño")]
    [Tooltip("Segundos mínimos entre dos golpes de esta barra")]
    [SerializeField] private float cooldownDano = 1f;

    private Renderer rend;
    private Material material;
    private Estado estado;
    private float temporizador;
    private float tiempoUltimoGolpe = -10f;

    private void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        if (rend != null)
            material = rend.material;   // copia propia, para no pintar todas las barras a la vez
        else
            Debug.LogWarning("[BarraPeligro] No encontré un Renderer en " + name, this);
    }

    private void Start()
    {
        // Empieza apagada con una espera aleatoria, para que cada barra arranque distinta
        CambiarEstado(Estado.Apagada);
        temporizador = Random.Range(0f, apagadaMax);
    }

    private void Update()
    {
        temporizador -= Time.deltaTime;
        if (temporizador > 0f) return;

        switch (estado)
        {
            case Estado.Apagada:
                CambiarEstado(tiempoAviso > 0f ? Estado.Aviso : Estado.Encendida);
                break;
            case Estado.Aviso:
                CambiarEstado(Estado.Encendida);
                break;
            case Estado.Encendida:
                CambiarEstado(Estado.Apagada);
                break;
        }
    }

    private void CambiarEstado(Estado nuevo)
    {
        estado = nuevo;

        switch (nuevo)
        {
            case Estado.Apagada:
                temporizador = Random.Range(apagadaMin, apagadaMax);
                Pintar(colorApagada);
                break;
            case Estado.Aviso:
                temporizador = tiempoAviso;
                Pintar(colorAviso);
                break;
            case Estado.Encendida:
                temporizador = Random.Range(encendidaMin, encendidaMax);
                Pintar(colorEncendida);
                break;
        }
    }

    private void Pintar(Color color)
    {
        if (material == null) return;

        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * 2f);
        material.color = color;                       // Standard
        if (material.HasProperty("_BaseColor"))       // URP
            material.SetColor("_BaseColor", color);
    }

    // Stay (no Enter): así también mata si el jugador ya está encima cuando se pone roja.
    private void OnTriggerStay(Collider other)
    {
        Golpear(other);
    }

    // Por si el collider de la barra NO es trigger (el jugador camina sobre ella).
    private void OnCollisionStay(Collision collision)
    {
        Golpear(collision.collider);
    }

    private void Golpear(Collider other)
    {
        if (estado != Estado.Encendida) return;
        if (!other.CompareTag("Player")) return;
        if (Time.time - tiempoUltimoGolpe < cooldownDano) return;

        tiempoUltimoGolpe = Time.time;
        Debug.Log("[BarraPeligro] " + name + " golpeó al jugador (estaba en rojo).", this);
        PlayerKill.KillPlayer();
    }
}
