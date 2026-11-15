using UnityEngine;

/// <summary>
/// Base común de todos los enemigos. Gestiona vida, daño, muerte, referencia
/// al jugador, el estado de buff y el cambio de color de la luz y el material.
/// </summary>
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Identidad")]
    [SerializeField] protected EnemyType tipo;

    [Header("Vida")]
    [SerializeField] protected int vidaMaxima = 3;
    [SerializeField] protected int vidaActual = 3;

    [Header("Daño al jugador")]
    [Tooltip("Si es true, al tocar al jugador lo mata (usa PlayerKill).")]
    [SerializeField] protected bool mataAlTocar = true;

    [Header("Luz y Material")]
    [Tooltip("Esfera de luz del enemigo")]
    [SerializeField] protected Light luzPropia;
    [Tooltip("El Renderer de la esfera (para cambiar el color del material)")]
    [SerializeField] protected Renderer esferaRenderer;
    
    [SerializeField] protected Color colorLuzNormal = Color.green;
    [SerializeField] protected Color colorLuzBuffeado = new Color(1f, 0.4f, 0f); // Naranja

    protected Transform jugador;
    protected bool muerto;
    protected bool buffeado;

    private Material materialInstancia; // Para no afectar a otros enemigos

    public EnemyType Tipo { get { return tipo; } }
    public int VidaActual { get { return vidaActual; } }
    public int VidaMaxima { get { return vidaMaxima; } }
    public bool Muerto { get { return muerto; } }
    public bool Buffeado { get { return buffeado; } }

    protected virtual void Start()
    {
        vidaActual = vidaMaxima;
        BuscarJugador();

        // Clonamos el material para que el cambio de color sea único para este enemigo
        if (esferaRenderer != null)
        {
            materialInstancia = esferaRenderer.material;
        }

        ActualizarColorLuz();
    }

    protected virtual void Update()
    {
        if (jugador == null && !muerto)
            BuscarJugador();
    }

    protected void BuscarJugador()
    {
        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null)
            jugador = pc.transform;
    }

    public virtual void RecibirDaño(int cantidad, DamageType tipoDaño)
    {
        if (muerto) return;
        vidaActual -= cantidad;

        if (vidaActual <= 0)
            Morir();
    }

    protected virtual void Morir()
    {
        if (muerto) return;
        muerto = true;
        OnMuerte();
        Destroy(gameObject);
    }

    protected virtual void OnMuerte() { }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (muerto) return;
        if (!mataAlTocar) return;
        
        if (other.CompareTag("Player"))
        {
            // Matar al jugador (RespawnOnFall escucha PlayerKill.OnKill y lo reaparece)
            PlayerKill.KillPlayer();
        }
    }

    public void AplicarBuff(bool aplicar)
    {
        if (buffeado == aplicar) return;
        buffeado = aplicar;
        ActualizarColorLuz();
    }

    protected void ActualizarColorLuz()
    {
        Color colorObjetivo = buffeado ? colorLuzBuffeado : colorLuzNormal;

        // 1. Cambiar la luz
        if (luzPropia != null)
        {
            luzPropia.color = colorObjetivo;
        }

        // 2. Cambiar la emisión del material de la esfera
        if (materialInstancia != null)
        {
            materialInstancia.EnableKeyword("_EMISSION");
            
            // Multiplicamos el color por intensidad para que brille
            materialInstancia.SetColor("_EmissionColor", colorObjetivo * 2.5f);
            
            // Si es URP, actualizamos también el color base
            if (materialInstancia.HasProperty("_BaseColor"))
            {
                materialInstancia.SetColor("_BaseColor", colorObjetivo * 0.5f);
            }
        }
    }
}