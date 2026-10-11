using UnityEngine;

/// <summary>
/// Muestra los controles en pantalla durante el nivel de pruebas.
/// Se oculta cuando el jugador llega a la meta (LevelGoal lo avisa).
/// </summary>
public class TestLevelHints : MonoBehaviour
{
    [Tooltip("Segundos que se muestran las ayudas (0 = hasta llegar a la meta)")]
    [SerializeField] private float hideAfterSeconds = 20f;

    private float elapsed;
    private bool goalReached;
    private GUIStyle style;

    private void Start()
    {
        LevelGoal.OnGoalReached += () => goalReached = true;
    }

    private void OnDestroy()
    {
        LevelGoal.OnGoalReached -= () => goalReached = true;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (hideAfterSeconds > 0f && elapsed > hideAfterSeconds)
            goalReached = true;
    }

    private void OnGUI()
    {
        if (goalReached) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                padding = new RectOffset(14, 14, 12, 12)
            };
            style.normal.textColor = Color.white;
        }

        string contenido =
            "<b>SEEN — Nivel de pruebas</b>\n" +
            "WASD / Flechas   →   Moverse\n" +
            "Mouse   →   Mirar      (Esc libera el ratón)\n" +
            "Espacio   →   Saltar (doble salto)\n" +
            "Shift / Clic der   →   Dash";

        // Caja lo suficientemente grande para 5 líneas a 14pt (no se recorta)
        GUI.Box(new Rect(12f, 12f, 470f, 132f), contenido, style);
    }
}
