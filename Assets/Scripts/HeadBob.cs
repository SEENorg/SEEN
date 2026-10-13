using UnityEngine;

/// <summary>
/// Bob de cámara al caminar: un balanceo suave y sutil de la cabeza que se
/// intensifica con la velocidad. La pantalla sigue viéndose perfectamente
/// (amplitudes chicas y suavizado).
/// Se coloca en el jugador (mueve la cámara hija).
/// </summary>
public class HeadBob : MonoBehaviour
{
    [Header("Amplitudes (unidades, chicas a propósito)")]
    [SerializeField] private float amplitudY = 0.040f;
    [SerializeField] private float amplitudX = 0.025f;

    [Header("Ritmo")]
    [Tooltip("Frecuencia base del paso (ciclos por segundo)")]
    [SerializeField] private float frecuenciaCaminar = 7.5f;
    [Tooltip("Frecuencia al correr (más rápida)")]
    [SerializeField] private float frecuenciaCorrer = 11f;
    [Tooltip("Velocidad a la que entra/sale el efecto")]
    [SerializeField] private float suavizado = 6f;
    [Tooltip("Velocidad mínima para empezar a bobear")]
    [SerializeField] private float umbral = 0.6f;

    private PlayerController pc;
    private Camera cam;
    private Vector3 alturaBase;
    private float ciclo;
    private float peso;

    private void Start()
    {
        pc = GetComponent<PlayerController>();
        cam = GetComponentInChildren<Camera>();
        if (cam != null) alturaBase = cam.transform.localPosition;
    }

    private void Update()
    {
        if (cam == null || pc == null) return;

        // ¿Cuánto bob aplicamos? 0 quieto → 1 a toda velocidad
        float vel = pc.VelocidadHorizontal;
        float objetivo = (pc.IsGrounded && vel > umbral) ? Mathf.Clamp01(vel / Mathf.Max(0.01f, pc.MoveSpeed)) : 0f;
        peso = Mathf.MoveTowards(peso, objetivo, Time.deltaTime * suavizado);

        // Frecuencia según velocidad (caminar vs correr)
        float t = Mathf.Clamp01(vel / Mathf.Max(0.01f, pc.MoveSpeed));
        float frecuencia = Mathf.Lerp(frecuenciaCaminar, frecuenciaCorrer, t);

        ciclo += Time.deltaTime * frecuencia;
        if (ciclo > 1000f) ciclo -= 1000f;

        float offsetY = Mathf.Sin(ciclo) * amplitudY * peso;
        float offsetX = Mathf.Cos(ciclo * 0.5f) * amplitudX * peso;

        cam.transform.localPosition = new Vector3(alturaBase.x + offsetX,
                                                  alturaBase.y + offsetY,
                                                  alturaBase.z);
    }
}
