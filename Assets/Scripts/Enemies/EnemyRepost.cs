using UnityEngine;

public class EnemyRepost : EnemyBase
{
    [Header("Duplicación")]
    [SerializeField] private int maxDuplicados = 3;
    [SerializeField] private GameObject prefabRepost;

    [Header("Movimiento Pasivo")]
    [SerializeField] private float velocidadDeriva = 1f;
    [SerializeField] private float radioDeriva = 3f;

    private int duplicadosGenerados;
    private Vector3 centroDeriva;
    private Vector3 destinoDeriva;
    private float tiempoSiguienteDestino;

    protected override void Start()
    {
        base.Start();
        vidaMaxima = 1;
        vidaActual = vidaMaxima;
        
        centroDeriva = transform.position;
        ElegirNuevoDestino();
    }

    protected override void Update()
    {
        base.Update();
        if (muerto) return;

        // Movimiento suave hacia el destino
        transform.position = Vector3.MoveTowards(transform.position, destinoDeriva, velocidadDeriva * Time.deltaTime);

        // Si llega al destino o pasa el tiempo, elegir uno nuevo
        if (Vector3.Distance(transform.position, destinoDeriva) < 0.1f || Time.time > tiempoSiguienteDestino)
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

        if (tipoDaño != DamageType.Cargado)
        {
            Duplicar();
            return;
        }

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