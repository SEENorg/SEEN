using UnityEngine;

public class EnemyRepost : EnemyBase
{
    [Header("Duplicación")]
    [SerializeField] private int maxDuplicados = 3;
    [SerializeField] private GameObject prefabRepost;

    [Header("Persecución lenta")]
    [Tooltip("Velocidad con la que sigue al jugador (lenta a propósito)")]
    [SerializeField] private float velocidadPersecucion = 1.6f;
    [Tooltip("Si el jugador está más cerca que esto, lo persigue")]
    [SerializeField] private float distanciaDeteccion = 12f;

    [Header("Movimiento Pasivo (cuando el jugador está lejos)")]
    [SerializeField] private float velocidadDeriva = 1f;
    [SerializeField] private float radioDeriva = 3f;

    private int duplicadosGenerados;
    private Vector3 centroDeriva;
    private Vector3 destinoDeriva;
    private float tiempoSiguienteDestino;
    private float alturaFijaY;

    protected override void Start()
    {
        base.Start();

        centroDeriva = transform.position;
        alturaFijaY = transform.position.y;
        ElegirNuevoDestino();
    }

    protected override void Update()
    {
        base.Update();
        if (muerto || !PuedeMoverse) return;

        if (JugadorCerca())
            Perseguir();
        else
            Derivar();
    }

    private bool JugadorCerca()
    {
        if (jugador == null) return false;
        return Vector3.Distance(transform.position, jugador.position) <= distanciaDeteccion;
    }

    // Va hacia el jugador, sin subir ni bajar, y sin atravesar paredes.
    private void Perseguir()
    {
        Vector3 destino = new Vector3(jugador.position.x, alturaFijaY, jugador.position.z);
        MoverHacia(destino, velocidadPersecucion);
    }

    // Movimiento pasivo de antes: deambula alrededor de su punto de origen.
    private void Derivar()
    {
        bool avanzo = MoverHacia(destinoDeriva, velocidadDeriva);

        if (!avanzo || Vector3.Distance(transform.position, destinoDeriva) < 0.1f || Time.time > tiempoSiguienteDestino)
        {
            ElegirNuevoDestino();
        }
    }

    private void ElegirNuevoDestino()
    {
        Vector2 punto = Random.insideUnitCircle * radioDeriva;
        destinoDeriva = centroDeriva + new Vector3(punto.x, 0f, punto.y);
        tiempoSiguienteDestino = Time.time + Random.Range(2f, 5f);
    }

    public override void RecibirDaño(int cantidad, DamageType tipoDaño)
    {
        if (muerto) return;

        if (tipoDaño != DamageType.Cargado && prefabRepost != null)
            Duplicar();

        base.RecibirDaño(cantidad, tipoDaño);
    }

    private void Duplicar()
    {
        if (duplicadosGenerados >= maxDuplicados) return;
        if (prefabRepost == null) return;

        duplicadosGenerados++;
        Vector3 offset = Random.insideUnitSphere * 2f;
        offset.y = 0f;
        Instantiate(prefabRepost, transform.position + offset, Quaternion.identity);
    }
}
