using UnityEngine;

public class EnemyNotificacion : EnemyBase
{
    [Header("Persecución")]
    [SerializeField] private float velocidad = 3.5f;
    [SerializeField] private float distanciaDeteccion = 15f;
    [SerializeField] private float velocidadRotacion = 5f;

    [Header("Ataque")]
    [SerializeField] private float radioAtaque = 2f; // Un poco más grande por si acaso
    [SerializeField] private float cooldownAtaque = 1.5f;

    private float tiempoUltimoAtaque;
    private float alturaFijaY;

    protected override void Start()
    {
        base.Start();
        alturaFijaY = transform.position.y;
    }

    protected override void Update()
    {
        base.Update();
        if (muerto || jugador == null) return;

        // Distancia real al jugador
        float dist = Vector3.Distance(transform.position, jugador.position);
        
        if (dist > distanciaDeteccion) return;

        // 1. Movimiento directo hacia el jugador (esto es a prueba de fallos)
        Vector3 destino = new Vector3(jugador.position.x, alturaFijaY, jugador.position.z);
        
        // MoveTowards mueve el objeto desde su posición actual hacia el destino
        transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);

        // 2. Rotación (solo si se está moviendo y no está justo encima)
        Vector3 direccion = destino - transform.position;
        direccion.y = 0f; // Ignoramos la altura para que no se incline

        if (direccion.sqrMagnitude > 0.01f)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);
        }

        // 3. Atacar
        if (dist <= radioAtaque && Time.time - tiempoUltimoAtaque > cooldownAtaque)
        {
            tiempoUltimoAtaque = Time.time;
            Atacar();
        }
    }

    private void Atacar()
    {
        PlayerKill.KillPlayer();
    }
}