using System.Collections.Generic;
using UnityEngine;

public class EnemyRepost : EnemyBase
{
    [Header("Duplicación")]
    [Tooltip("Cuántas copias crea cada Repost original (1 = solo una)")]
    [SerializeField] private int maxCopias = 1;
    [SerializeField] private GameObject prefabRepost;
    [Tooltip("Vida de las copias (con 1 mueren de un solo disparo)")]
    [SerializeField] private int vidaCopias = 1;

    [Header("Persecución lenta")]
    [Tooltip("Velocidad con la que sigue al jugador (lenta a propósito)")]
    [SerializeField] private float velocidadPersecucion = 1.6f;
    [Tooltip("Si el jugador está más cerca que esto, lo persigue")]
    [SerializeField] private float distanciaDeteccion = 12f;

    [Header("Separación entre Reposts")]
    [Tooltip("Distancia mínima que intentan mantener entre ellos")]
    [SerializeField] private float distanciaSeparacion = 2.5f;
    [Tooltip("Qué tanto se alejan unos de otros (más alto = se separan más)")]
    [SerializeField] private float fuerzaSeparacion = 1.5f;

    [Header("Límite de zona (por si una pared no tiene collider)")]
    [Tooltip("Hasta dónde se puede alejar de su sala (metros). 0 = sin límite")]
    [SerializeField] private float radioMaximo = 12f;

    [Header("Movimiento Pasivo (cuando el jugador está lejos)")]
    [SerializeField] private float velocidadDeriva = 1f;
    [SerializeField] private float radioDeriva = 3f;

    // Lista de todos los Reposts activos (para que se esquiven entre sí)
    private static readonly List<EnemyRepost> todos = new List<EnemyRepost>();

    private int duplicadosGenerados;
    private bool esCopia;   // las copias NO se vuelven a duplicar
    private Vector3 centroDeriva;
    private Vector3 destinoDeriva;
    private float tiempoSiguienteDestino;
    private float alturaFijaY;
    private Vector3 centroLimite;
    private bool centroAsignado;   // las copias heredan el centro del original

    private void OnEnable()  { todos.Add(this); }
    private void OnDisable() { todos.Remove(this); }

    protected override void Start()
    {
        base.Start();

        centroDeriva = transform.position;
        if (!centroAsignado) centroLimite = transform.position;
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
        MoverConSeparacion(destino, velocidadPersecucion);
    }

    // Movimiento pasivo de antes: deambula alrededor de su punto de origen.
    private void Derivar()
    {
        bool avanzo = MoverConSeparacion(destinoDeriva, velocidadDeriva);

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

        // Solo el original se multiplica; las copias reciben daño y mueren normal
        if (tipoDaño != DamageType.Cargado && !esCopia)
            Duplicar();

        base.RecibirDaño(cantidad, tipoDaño);
    }

    private void Duplicar()
    {
        if (duplicadosGenerados >= maxCopias) return;

        // Si no tiene prefab asignado (pasa en otros niveles), se clona a sí mismo
        GameObject molde = prefabRepost != null ? prefabRepost : gameObject;

        duplicadosGenerados++;
        // Misma altura que el padre
        Vector3 punto = BuscarPuntoLibre();
        punto.y = transform.position.y;
        GameObject nuevo = Instantiate(molde, punto, Quaternion.identity);

        // Marcar la copia: no se duplica y tiene poca vida
        EnemyRepost copia = nuevo.GetComponent<EnemyRepost>();
        if (copia != null)
        {
            copia.esCopia = true;
            copia.centroLimite = centroLimite;     // misma sala que el original
            copia.centroAsignado = true;
            copia.vidaMaxima = Mathf.Max(1, vidaCopias);   // se aplica en su Start()
        }
    }

    // Busca un sitio cercano al que se pueda llegar SIN atravesar paredes.
    // Usa la misma revisión de obstáculos con la que se mueven los enemigos.
    private Vector3 BuscarPuntoLibre()
    {
        for (int intento = 0; intento < 10; intento++)
        {
            Vector2 giro = Random.insideUnitCircle.normalized;
            Vector3 direccion = new Vector3(giro.x, 0f, giro.y);
            float distancia = Random.Range(1f, 2f);

            if (!HayObstaculo(direccion, distancia))
                return transform.position + direccion * distancia;
        }

        // No hay espacio libre alrededor: nace justo donde está el original
        return transform.position;
    }

    // Empuje que lo aleja de los otros Reposts que tiene muy cerca
    private Vector3 CalcularSeparacion()
    {
        Vector3 empuje = Vector3.zero;

        for (int i = 0; i < todos.Count; i++)
        {
            EnemyRepost otro = todos[i];
            if (otro == this || otro == null || otro.muerto) continue;

            Vector3 desde = transform.position - otro.transform.position;
            desde.y = 0f;
            float dist = desde.magnitude;
            if (dist >= distanciaSeparacion) continue;

            // Si están exactamente encima, se elige una dirección al azar
            Vector3 dir;
            if (dist < 0.01f)
            {
                Vector2 azar = Random.insideUnitCircle.normalized;
                dir = new Vector3(azar.x, 0f, azar.y);
            }
            else
            {
                dir = desde / dist;
            }

            // Más cerca = empuja más fuerte
            empuje += dir * (1f - dist / distanciaSeparacion);
        }

        return empuje;
    }

    // Igual que MoverHacia, pero sumando el empuje que lo aleja de los otros Reposts
    private bool MoverConSeparacion(Vector3 destino, float velocidad)
    {
        Vector3 haciaDestino = destino - transform.position;
        haciaDestino.y = 0f;

        Vector3 direccion = haciaDestino.normalized + CalcularSeparacion() * fuerzaSeparacion;
        if (direccion.sqrMagnitude < 0.0001f) return false;

        float alcance = Mathf.Max(haciaDestino.magnitude, 0.5f);
        Vector3 nuevoDestino = transform.position + direccion.normalized * alcance;
        nuevoDestino.y = destino.y;

        if (SeSaldriaDeLaZona(direccion.normalized))
            return false;

        return MoverHacia(nuevoDestino, velocidad);
    }

    // True si el siguiente paso lo alejaría más allá del radio máximo de su sala
    private bool SeSaldriaDeLaZona(Vector3 direccion)
    {
        if (radioMaximo <= 0f) return false;

        Vector3 actual = transform.position - centroLimite;
        actual.y = 0f;
        Vector3 siguiente = transform.position + direccion * 0.3f - centroLimite;
        siguiente.y = 0f;

        return siguiente.magnitude > radioMaximo && siguiente.magnitude > actual.magnitude;
    }
}
