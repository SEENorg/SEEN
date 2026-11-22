using UnityEngine;

/// <summary>
/// TEMPORAL: ponlo en un enemigo para ver en la consola por qué no se mueve.
/// Cuando lo arregles, bórralo.
/// </summary>
public class DiagnosticoEnemigo : MonoBehaviour
{
    private EnemyBase enemigo;
    private Vector3 ultimaPos;
    private float proximoLog;

    private void Start()
    {
        enemigo = GetComponent<EnemyBase>();
        ultimaPos = transform.position;
        if (enemigo == null)
            Debug.LogWarning(name + ": NO tiene un script que herede de EnemyBase.");
        else
            Debug.Log(name + ": tipo = " + enemigo.GetType().Name);
    }

    private void Update()
    {
        if (enemigo == null || Time.unscaledTime < proximoLog) return;
        proximoLog = Time.unscaledTime + 2f;

        bool seMovio = Vector3.Distance(ultimaPos, transform.position) > 0.01f;
        ultimaPos = transform.position;
        if (seMovio) { Debug.Log(name + ": SÍ se está moviendo."); return; }

        string motivo = "";
        if (!enemigo.enabled) motivo += "[el script EnemyBase está DESACTIVADO] ";
        if (Time.timeScale == 0f) motivo += "[Time.timeScale = 0] ";
        if (!enemigo.PuedeMoverse) motivo += "[la puerta activadora no está abierta] ";
        if (enemigo.Muerto) motivo += "[está marcado como muerto] ";
        if (GetComponentInChildren<Animator>() != null || GetComponent<Animator>() != null)
            motivo += "[tiene un Animator: puede pisar la posición] ";
        if (transform.parent != null && transform.parent.GetComponent<Rigidbody>() != null)
            motivo += "[el padre tiene Rigidbody] ";

        // Quién lo bloquea: lanza la misma esfera que usa EnemyBase
        RaycastHit[] golpes = Physics.SphereCastAll(transform.position, 0.6f, transform.forward,
                                                    1f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < golpes.Length; i++)
        {
            Collider c = golpes[i].collider;
            if (c.transform.IsChildOf(transform)) continue;
            if (c.CompareTag("Player")) continue;
            motivo += "[cerca hay un collider: " + c.name + "] ";
        }

        if (motivo == "") motivo = "ninguna causa evidente (puede ser Hashtag, o Notificación con el jugador lejos)";
        Debug.Log(name + ": NO se mueve. Motivo: " + motivo);
    }
}
