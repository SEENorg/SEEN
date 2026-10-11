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
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 12, 10, 10)
            };
            style.normal.textColor = Color.white;
        }

        GUI.Box(new Rect(12, 12, 330, 96),
            "<b>SEEN — Nivel de pruebas</b>\n" +
            "WASD / Flechas  →  Moverse\n" +
            "Mouse  →  Mirar   |   Esc libera el mouse\n" +
            "Espacio  →  Saltar (doble salto)\n" +
            "Shift / Clic der  →  Dash", style);
    }
}
