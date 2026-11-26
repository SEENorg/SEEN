using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Vigilante de la sala: cuando ya no queda ningún enemigo, dispara las consecuencias:
///   - habilita la pantalla del código (si se asignó),
///   - muestra los objetos escondidos (plataformas, puertas, etc.),
///   - ejecuta cualquier evento extra que se conecte en el Inspector.
///
/// SRP: solo vigila enemigos y avisa. No muestra el código ni mueve plataformas.
/// DIP: habla con la pantalla por la interfaz IPuzzleCodigo, no por PuzzleCodigo.
/// OCP: para una sala nueva no se toca el código, solo se configura el Inspector.
///
/// Enemigos: se pueden arrastrar uno por uno a la lista Y/O poner un contenedor
/// (objeto vacío) que los tenga adentro. Todo es opcional, se pueden combinar.
/// </summary>
public class ControlSalaEnemigos : MonoBehaviour
{
    [Header("Enemigos de esta sala")]
    [Tooltip("Enemigos sueltos (opcional)")]
    [SerializeField] private GameObject[] enemigos;

    [Tooltip("Objeto vacío que tiene enemigos adentro (opcional). Cuenta también las copias de Repost")]
    [SerializeField] private GameObject contenedorEnemigos;

    [Header("Al derrotarlos a todos...")]
    [Tooltip("Pantalla con el script PuzzleCodigo que se habilita (opcional)")]
    [SerializeField] private GameObject pantallaCodigo;

    [Tooltip("Objetos que empiezan ESCONDIDOS y aparecen (ej: contenedor de plataformas). Opcional")]
    [SerializeField] private GameObject[] objetosAMostrar;

    [Tooltip("Cualquier otra cosa que quieras ejecutar (abrir una puerta, sonido...)")]
    [SerializeField] private UnityEvent alDerrotarATodos;

    [Header("Opciones")]
    [Tooltip("Cada cuántos segundos revisa si quedan enemigos")]
    [SerializeField] private float segundosEntreRevisiones = 0.5f;

    private IPuzzleCodigo pantalla;
    private bool yaTerminado;

    private void Awake()
    {
        if (pantallaCodigo != null)
            pantalla = pantallaCodigo.GetComponent<IPuzzleCodigo>();

        // Lo que debe aparecer empieza escondido
        if (objetosAMostrar != null)
        {
            for (int i = 0; i < objetosAMostrar.Length; i++)
                if (objetosAMostrar[i] != null) objetosAMostrar[i].SetActive(false);
        }

        if (!HayEnemigosQueVigilar())
            Debug.LogWarning("ControlSalaEnemigos: no hay enemigos asignados en " + name);
    }

    private void Start()
    {
        InvokeRepeating(nameof(Revisar), segundosEntreRevisiones, segundosEntreRevisiones);
    }

    private void Revisar()
    {
        if (yaTerminado) return;
        if (!HayEnemigosQueVigilar()) return;
        if (QuedanVivos()) return;

        yaTerminado = true;
        CancelInvoke(nameof(Revisar));
        EjecutarConsecuencias();
    }

    private void EjecutarConsecuencias()
    {
        if (pantalla != null) pantalla.HabilitarCodigo();

        if (objetosAMostrar != null)
        {
            for (int i = 0; i < objetosAMostrar.Length; i++)
                if (objetosAMostrar[i] != null) objetosAMostrar[i].SetActive(true);
        }

        if (alDerrotarATodos != null) alDerrotarATodos.Invoke();
    }

    private bool HayEnemigosQueVigilar()
    {
        bool hayLista = enemigos != null && enemigos.Length > 0;
        bool hayContenedor = contenedorEnemigos != null;
        return hayLista || hayContenedor;
    }

    // Un enemigo cuenta como muerto si su objeto fue destruido o está desactivado.
    private bool QuedanVivos()
    {
        if (enemigos != null)
        {
            for (int i = 0; i < enemigos.Length; i++)
            {
                GameObject e = enemigos[i];
                if (e != null && e.activeInHierarchy)
                    return true;
            }
        }

        if (contenedorEnemigos != null)
        {
            // true = incluye también a los desactivados (los muertos se ignoran abajo)
            EnemyBase[] delContenedor = contenedorEnemigos.GetComponentsInChildren<EnemyBase>(true);
            for (int i = 0; i < delContenedor.Length; i++)
            {
                EnemyBase e = delContenedor[i];
                if (e != null && e.gameObject.activeInHierarchy && !e.Muerto)
                    return true;
            }
        }
        return false;
    }
}
