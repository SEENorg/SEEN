using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menú principal de SEEN. Conecta con el Canvas construido por UIMenuBuilder
/// (menú: SEEN → Construir Menú Principal).
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelMenu;
    public GameObject panelAjustes;

    [Header("Ajustes")]
    public SettingsManager ajustes;

    private void Start()
    {
        MostrarMenuPrincipal();

        // En el menú el ratón debe estar libre y visible
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Botón JUGAR: carga la escena del juego.</summary>
    public void Jugar()
    {
        Time.timeScale = 1f; // por si se vuelve de una partida pausada
        SceneManager.LoadScene("Juego");
    }

    public void MostrarAjustes()
    {
        if (panelMenu != null) panelMenu.SetActive(false);
        if (panelAjustes != null) panelAjustes.SetActive(true);
    }

    public void MostrarMenuPrincipal()
    {
        if (panelMenu != null) panelMenu.SetActive(true);
        if (panelAjustes != null) panelAjustes.SetActive(false);
    }

    public void SalirDelJuego()
    {
        Debug.Log("[MainMenu] Saliendo de SEEN...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
