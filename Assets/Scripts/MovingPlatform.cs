using UnityEngine;

/// <summary>
/// Plataforma móvil: se desplaza de forma ping-pong entre su posición inicial
/// y posición inicial + moveAmount. El jugador se sube y la fricción lo arrastra.
/// </summary>
public class MovingPlatform : MonoBehaviour
{
    [Tooltip("Desplazamiento total del recorrido")]
    public Vector3 moveAmount = new Vector3(6f, 0f, 0f);
    [Tooltip("Velocidad del recorrido (unidades/segundo)")]
    public float speed = 1.2f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        transform.position = startPos + moveAmount * Mathf.PingPong(Time.time * speed, 1f);
    }
}
