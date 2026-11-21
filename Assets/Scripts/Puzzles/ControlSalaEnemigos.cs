using UnityEngine;

/// <summary>
/// Vigilante de la sala: cuando ya no queda ningún enemigo de su lista,
/// le pide a la pantalla del código que se habilite.
///
/// SRP: solo vigila enemigos y avisa. No muestra el código ni pelea.
/// DIP: habla con la pantalla por la interfaz IPuzzleCodigo, no por PuzzleCodigo.
/// OCP: para agregar enemigos solo se arrastran a la lista en el Inspector.
/// </summary>
public class ControlSalaEnemigos : MonoBehaviour
{
    [Header("Enemigos de esta sala (arrástralos aquí)")]
    [SerializeField] private GameObject[] enemigos;

    [Header("Objeto que tiene el script PuzzleCodigo")]
    [SerializeField] private GameObject pantallaCodigo;

    [Tooltip("Cada cuántos segundos revisa si quedan enemigos")]
    [SerializeField] private float segundosEntreRevisiones = 0.5f;

    private IPuzzleCodigo pantalla;
    private bool yaHabilitado;

    private void Awake()
    {
        if (pantallaCodigo != null)
            pantalla = pantallaCodigo.GetComponent<IPuzzleCodigo>();

        if (pantalla == null)
            Debug.LogWarning("ControlSalaEnemigos: falta asignar la pantalla del código.");

        if (enemigos == null || enemigos.Length == 0)
            Debug.LogWarning("ControlSalaEnemigos: la lista de enemigos está vacía.");
    }

    private void Start()
    {
        InvokeRepeating(nameof(Revisar), segundosEntreRevisiones, segundosEntreRevisiones);
    }

    private void Revisar()
    {
        if (yaHabilitado) return;
        if (pantalla == null) return;
        if (enemigos == null || enemigos.Length == 0) return;
        if (QuedanVivos()) return;

        yaHabilitado = true;
        CancelInvoke(nameof(Revisar));
        pantalla.HabilitarCodigo();
    }

    // Un enemigo cuenta como muerto si su objeto fue destruido o está desactivado.
    private bool QuedanVivos()
    {
        for (int i = 0; i < enemigos.Length; i++)
        {
            GameObject e = enemigos[i];
            if (e != null && e.activeInHierarchy)
                return true;
        }
        return false;
    }
}
