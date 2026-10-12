using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bandera estática que indica si la escena actual se abrió legítimamente
/// (desde el menú, o desde una pantalla de reintentar/siguiente nivel).
/// Si no, el BootRedirect manda al jugador al menú principal.
/// </summary>
public static class Boot
{
    public static bool PermitirInicioEscena { get; set; }
}

/// <summary>
/// Se coloca en las escenas de nivel. Si el jugador le da Play directamente
/// en una escena de nivel (sin pasar por el menú), lo redirige al menú
/// principal. Desde el menú sí se puede entrar normalmente.
/// </summary>
public class BootRedirect : MonoBehaviour
{
    private void Awake()
    {
        if (!Application.isPlaying) return;

        if (!Boot.PermitirInicioEscena)
        {
            Debug.Log("[Boot] Play iniciado fuera del menú → cargando menú principal.");
            SceneManager.LoadScene("MainMenu");
            return;
        }

        // Consumir el permiso: la próxima carga sin permiso volvería al menú
        Boot.PermitirInicioEscena = false;
    }
}
