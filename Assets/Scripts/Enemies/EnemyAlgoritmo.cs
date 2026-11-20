using UnityEngine;

public class EnemyAlgoritmo : EnemyBase
{
    [Header("Protección")]
    [SerializeField] private GameObject cubosProtectores;
    [SerializeField] private float tiempoProtegido = 4f;
    [SerializeField] private float tiempoExpuesto = 2.5f;

    [Header("Movimiento Flotante")]
    [SerializeField] private float velocidadVuelo = 2f;
    [SerializeField] private float radioVuelo = 4f;
    [SerializeField] private float alturaVuelo = 1.5f;
    [Tooltip("Velocidad de rotación sobre su propio eje")]
    [SerializeField] private float velocidadRotacion = 50f;

    private bool expuesto;
    private float temporizador;
    private Vector3 posicionInicial;
    private Vector3 destinoActual;

    public bool Expuesto { get { return expuesto; } }

    protected override void Start()
    {
        base.Start();
        posicionInicial = transform.position;
        temporizador = 0f;
        expuesto = false;
        ElegirNuevoDestino();
        ActualizarCubos();
    }

    protected override void Update()
    {
        base.Update();
        if (muerto || !PuedeMoverse) return;

        // 1. Rotación constante sobre su propio eje
        transform.Rotate(Vector3.up * velocidadRotacion * Time.deltaTime, Space.Self);

        // 2. Movimiento suave hacia el destino, sin atravesar paredes
        bool avanzo = MoverHacia(destinoActual, velocidadVuelo);

        if (!avanzo || Vector3.Distance(transform.position, destinoActual) < 0.1f)
        {
            ElegirNuevoDestino();
        }

        // 3. Ciclo de protección
        temporizador += Time.deltaTime;
        if (!expuesto && temporizador >= tiempoProtegido)
        {
            expuesto = true;
            temporizador = 0f;
            ActualizarCubos();
        }
        else if (expuesto && temporizador >= tiempoExpuesto)
        {
            expuesto = false;
            temporizador = 0f;
            ActualizarCubos();
        }
    }

    private void ElegirNuevoDestino()
    {
        // Prueba varios puntos y se queda con el primero al que se puede llegar sin chocar
        for (int intento = 0; intento < 8; intento++)
        {
            Vector2 circulo = Random.insideUnitCircle * radioVuelo;
            Vector3 candidato = posicionInicial + new Vector3(circulo.x, alturaVuelo, circulo.y);

            Vector3 haciaCandidato = candidato - transform.position;
            float distancia = haciaCandidato.magnitude;
            if (distancia < 0.5f) continue;

            if (!HayObstaculo(haciaCandidato / distancia, distancia))
            {
                destinoActual = candidato;
                return;
            }
        }

        // Todos los caminos estaban bloqueados: se queda quieto y lo intenta de nuevo luego
        destinoActual = transform.position;
    }

    private void ActualizarCubos()
    {
        if (cubosProtectores != null)
            cubosProtectores.SetActive(!expuesto);
    }

    public override void RecibirDaño(int cantidad, DamageType tipoDaño)
    {
        if (muerto) return;
        base.RecibirDaño(cantidad, tipoDaño);
    }
}