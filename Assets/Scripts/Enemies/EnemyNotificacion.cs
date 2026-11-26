using UnityEngine;

/// <summary>
/// NOTIFICACIÓN: acecha al jugador, telegrafía una carga brillante en rojo/naranja
/// y ejecuta una embestida rápida en línea recta.
/// Si el jugador la esquiva o le acierta un disparo sobrecargado, queda aturdida.
/// </summary>
public class EnemyNotificacion : EnemyBase
{
    private enum Estado { Acechando, Cargando, Embestida }

    [Header("Persecución")]
    [SerializeField] private float velocidad = 3.5f;
    [SerializeField] private float distanciaDeteccion = 15f;
    [SerializeField] private float velocidadRotacion = 5f;

    [Header("Ataque")]
    [SerializeField] private float radioAtaque = 2f;
    [SerializeField] private float cooldownAtaque = 1.5f;

    [Header("Embestida (mecánica activa)")]
    [SerializeField] private float distanciaEmbestida = 8.5f;
    [SerializeField] private float tiempoCargaEmbestida = 0.65f;
    [SerializeField] private float duracionEmbestida = 0.45f;
    [SerializeField] private float velocidadEmbestida = 11.5f;
    [SerializeField] private float cooldownEmbestida = 3.0f;
    [SerializeField] private float aturdimientoTrasEmbestida = 1.3f;

    private float tiempoUltimoAtaque;
    private float tiempoUltimaEmbestida = -10f;
    private float alturaFijaY;
    private Estado estado = Estado.Acechando;
    private float temporizadorEstado;
    private Vector3 direccionEmbestida = Vector3.forward;

    protected override void Start()
    {
        base.Start();
        alturaFijaY = transform.position.y;
    }

    public override void ReiniciarPosicion()
    {
        estado = Estado.Acechando;
        temporizadorEstado = 0f;
        tiempoUltimaEmbestida = -10f;
        base.ReiniciarPosicion();
        ActualizarColorLuz();
    }

    protected override void OnAturdido()
    {
        estado = Estado.Acechando;
        temporizadorEstado = 0f;
        estadoMecanico = "";
    }

    public override void RecibirDaño(int cantidad, DamageType tipoDaño)
    {
        int vidaAntes = vidaActual;
        base.RecibirDaño(cantidad, tipoDaño);

        if (muerto || vidaActual >= vidaAntes) return;

        // Disparo sobrecargado mientras carga o embiste corta el ataque y la aturde
        if (tipoDaño == DamageType.Cargado && (estado == Estado.Cargando || estado == Estado.Embestida))
        {
            tiempoUltimaEmbestida = Time.time;
            Aturdir(1.8f);
        }
    }

    protected override void Update()
    {
        base.Update();
        if (muerto || jugador == null || !PuedeMoverse) return;

        float dist = Vector3.Distance(transform.position, jugador.position);

        if (estado == Estado.Cargando)
        {
            ActualizarCarga();
            return;
        }

        if (estado == Estado.Embestida)
        {
            ActualizarEmbestida();
            return;
        }

        estadoMecanico = "";
        if (dist > distanciaDeteccion) return;

        // Iniciar carga si está a distancia de embestida
        if (dist <= distanciaEmbestida && Time.time - tiempoUltimaEmbestida >= cooldownEmbestida)
        {
            IniciarCarga();
            return;
        }

        // Persecución con ligero zig-zag para que no camine rígida
        Vector3 hacia = new Vector3(jugador.position.x - transform.position.x, 0f, jugador.position.z - transform.position.z);
        Vector3 dir = hacia.sqrMagnitude > 0.001f ? hacia.normalized : transform.forward;
        Vector3 lateral = Vector3.Cross(Vector3.up, dir) * Mathf.Sin(Time.time * 3.5f) * 0.45f;
        Vector3 destino = transform.position + (dir + lateral).normalized * 2f;
        destino.y = alturaFijaY;

        MoverHacia(destino, velocidad);
        RotarHacia(hacia);

        if (dist <= radioAtaque && Time.time - tiempoUltimoAtaque > cooldownAtaque)
        {
            tiempoUltimoAtaque = Time.time;
            IntentarMatarJugador();
        }
    }

    private void IniciarCarga()
    {
        estado = Estado.Cargando;
        temporizadorEstado = tiempoCargaEmbestida;
        estadoMecanico = "[¡CARGANDO!]";
        ActualizarDireccionEmbestida();
        AplicarTinteLuz(new Color(1f, 0.25f, 0.05f), 4f);
    }

    private void ActualizarCarga()
    {
        temporizadorEstado -= Time.deltaTime;
        estadoMecanico = "[¡CARGANDO!]";

        // Ajusta puntería al comienzo de la carga; los últimos 0.15s fija trayectoria para permitir esquiva
        if (temporizadorEstado > 0.15f)
            ActualizarDireccionEmbestida();

        RotarHacia(direccionEmbestida, 12f);

        if (temporizadorEstado <= 0f)
        {
            estado = Estado.Embestida;
            temporizadorEstado = duracionEmbestida;
            estadoMecanico = "[¡EMBESTIDA!]";
        }
    }

    private void ActualizarEmbestida()
    {
        temporizadorEstado -= Time.deltaTime;
        estadoMecanico = "[¡EMBESTIDA!]";

        Vector3 destino = transform.position + direccionEmbestida * velocidadEmbestida * Time.deltaTime;
        destino.y = alturaFijaY;

        bool avanzo = MoverHacia(destino, velocidadEmbestida);
        RotarHacia(direccionEmbestida, 14f);

        if (!avanzo || temporizadorEstado <= 0f)
        {
            estado = Estado.Acechando;
            tiempoUltimaEmbestida = Time.time;
            estadoMecanico = "";
            Aturdir(aturdimientoTrasEmbestida);
        }
    }

    private void ActualizarDireccionEmbestida()
    {
        Vector3 hacia = new Vector3(jugador.position.x - transform.position.x, 0f, jugador.position.z - transform.position.z);
        if (hacia.sqrMagnitude > 0.001f)
            direccionEmbestida = hacia.normalized;
    }

    private void RotarHacia(Vector3 direccion, float factor = 1f)
    {
        direccion.y = 0f;
        if (direccion.sqrMagnitude > 0.01f)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadRotacion * factor * Time.deltaTime);
        }
    }
}