using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla el arranque del juego:
///  • Si se pulsa Play estando en una escena de nivel (no el menú),
///    redirige automáticamente al menú principal.
///  • Desde el menú se marca el permiso para entrar a un nivel.
///
/// Usa [RuntimeInitializeOnLoadMethod], así que funciona SIEMPRE
/// (no depende de ajustes del editor ni de componentes en las escenas).
/// </summary>
public static class Boot
{
    /// <summary>Permiso para iniciar la escena actual (lo pone el menú).</summary>
    public static bool PermitirInicioEscena { get; set; }

    private const string EscenaMenu = "MainMenu";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ComprobarEscenaInicial()
    {
        string escenaActual = SceneManager.GetActiveScene().name;

        // Consumir el permiso de una carga anterior
        if (PermitirInicioEscena)
        {
            PermitirInicioEscena = false;
            Debug.Log("[Boot] Permiso consumido: entrando a '" + escenaActual + "'");
            return;
        }

        // Si ya estamos en el menú, todo bien
        if (escenaActual == EscenaMenu)
            return;
            
        if (escenaActual == "IntroScene")
            return;

        // Se inició el juego desde una escena de nivel → ir al menú
        if (Application.CanStreamedLevelBeLoaded(EscenaMenu))
        {
            Debug.Log("[Boot] Inicio fuera del menú ('" + escenaActual + "') → cargando el menú principal.");
            PermitirInicioEscena = true;   // permiso para que el menú no rebote
            SceneManager.LoadScene(EscenaMenu);
        }
    }
}
