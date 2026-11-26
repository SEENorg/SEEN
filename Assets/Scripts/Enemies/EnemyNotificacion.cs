using UnityEngine;

/// <summary>
/// NOTIFICACIÓN: persigue al jugador cuando lo detecta y lo mata al alcanzarlo.
/// Versión estable: no tiembla encima del jugador, esquiva las paredes en vez de
/// quedarse pegado, no persigue a otro piso y no se aleja de su zona.
/// </summary>
public class EnemyNotificacion : EnemyBase
{
    [Header("Persecución")]
    [SerializeField] private float velocidad = 3.5f;
    [SerializeField] private float distanciaDeteccion = 15f;
    [SerializeField] private float velocidadRotacion = 5f;

    [Tooltip("Se detiene a esta distancia del jugador (evita que tiemble encima de él)")]
    [SerializeField] private float distanciaParada = 1.2f;

    [Tooltip("Si el jugador está más arriba o más abajo que esto, no lo persigue (otro piso, ascensor, plataforma)")]
    [SerializeField] private float diferenciaAlturaMaxima = 3f;

    [Header("Zona (para que no se vaya del cuarto)")]
    [Tooltip("Hasta dónde se puede alejar del punto donde empezó (metros). 0 = sin límite")]
    [SerializeField] private float radioMaximo = 25f;

    [Header("Ataque")]
    [SerializeField] private float radioAtaque = 2f; // Un poco más grande por si acaso
    [SerializeField] private float cooldownAtaque = 1.5f;

    private float tiempoUltimoAtaque;
    private float alturaFijaY;
    private float ladoEsquivar = 1f;   // hacia qué lado rodea una pared (+1 / -1)

    protected override void Start()
    {
        base.Start();
        alturaFijaY = transform.position.y;
    }

    protected override void Update()
    {
        base.Update();
        if (muerto || jugador == null || !PuedeMoverse) return;
        if (Time.deltaTime <= 0f) return;   // juego en pausa

        // Distancia real al jugador
        float dist = Vector3.Distance(transform.position, jugador.position);
        if (dist > distanciaDeteccion) return;

        // No persigue a otro piso (si no, trata de atravesar techos y suelos)
        if (Mathf.Abs(jugador.position.y - alturaFijaY) > diferenciaAlturaMaxima) return;

        // Sin pared en medio: si hay una, se queda quieto en vez de pegarse a ella
        // (no sabe rodear cuartos, así que solo persigue cuando puede ir en línea recta)
        if (!LineaDeVistaLibre()) return;

        Vector3 destino = new Vector3(jugador.position.x, alturaFijaY, jugador.position.z);
        Vector3 haciaJugador = destino - transform.position;
        haciaJugador.y = 0f;

        // 1. Movimiento: solo si todavía no llegó junto al jugador
        if (haciaJugador.magnitude > distanciaParada && !FueraDeLaZona(haciaJugador.normalized))
            MoverEsquivando(destino, velocidad);

        // 2. Rotación (solo si no está justo encima)
        if (haciaJugador.sqrMagnitude > 0.01f)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(haciaJugador);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);
        }

        // 3. Atacar
        if (dist <= radioAtaque && Time.time - tiempoUltimoAtaque > cooldownAtaque)
        {
            tiempoUltimoAtaque = Time.time;
            Atacar();
        }
    }

    // Va directo al destino; si una pared lo frena, prueba a rodearla (en vez de quedarse pegado o temblar)
    private bool MoverEsquivando(Vector3 destino, float vel)
    {
        if (MoverHacia(destino, vel)) return true;

        Vector3 dir = destino - transform.position;
        dir.y = 0f;
        dir.Normalize();

        float[] giros = { 50f, 90f };
        for (int i = 0; i < giros.Length; i++)
        {
            Vector3 desvio = Quaternion.AngleAxis(giros[i] * ladoEsquivar, Vector3.up) * dir;
            if (MoverHacia(transform.position + desvio, vel * 0.8f))
                return true;
        }

        // No pudo por ese lado: la próxima vez lo intenta por el otro
        ladoEsquivar = -ladoEsquivar;
        return false;
    }

    // True si el siguiente paso lo alejaría más allá del radio máximo de su zona
    private bool FueraDeLaZona(Vector3 direccion)
    {
        if (radioMaximo <= 0f) return false;

        Vector3 actual = transform.position - posicionOrigen;
        actual.y = 0f;
        Vector3 siguiente = transform.position + direccion * 0.3f - posicionOrigen;
        siguiente.y = 0f;

        return siguiente.magnitude > radioMaximo && siguiente.magnitude > actual.magnitude;
    }

    private void Atacar()
    {
        IntentarMatarJugador();
    }
}
