using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menú principal de SEEN. Conecta con el Canvas construido por UIMenuBuilder
/// (menú: SEEN / Construir Menú Principal).
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelMenu;
    public GameObject panelAjustes;
    public GameObject panelNiveles;

    [Header("Botones de nivel")]
    public Button botonNivel1;
    public Button botonNivel2;
    public Button botonNivel3;
    public Button botonNivel4;
    public Button botonNivel5;

    [Header("Ajustes")]
    public SettingsManager ajustes;

    private void Start()
    {
        MostrarMenuPrincipal();

        // En el menú el ratón debe estar libre y visible
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Botón JUGAR: muestra el selector de niveles.</summary>
    public void Jugar()
    {
        Time.timeScale = 1f;
        if (panelMenu != null) panelMenu.SetActive(false);
        if (panelAjustes != null) panelAjustes.SetActive(false);
        if (panelNiveles != null) panelNiveles.SetActive(true);
    }

    /// <summary>Carga un nivel por su número (1-5).</summary>
    public void IrANivel(int numero)
    {
        string escena = numero == 1 ? "Juego" :
                        numero == 2 ? "Nivel2" :
                        numero == 3 ? "Nivel3" :
                        numero == 4 ? "Nivel4" :
                        numero == 5 ? "Nivel5" : "Juego";

        Time.timeScale = 1f;
        Boot.PermitirInicioEscena = true;

        if (Application.CanStreamedLevelBeLoaded(escena))
            SceneManager.LoadScene(escena);
        else
            Debug.LogWarning("[MainMenu] La escena " + escena +
                             " no está en Build Settings. Construila con SEEN → Construir Nivel " + numero);
    }

    // Métodos individuales para los botones (los listeners persistentes
    // del builder necesitan métodos sin argumentos)
    public void CargarNivel1() { IrANivel(1); }
    public void CargarNivel2() { IrANivel(2); }
    public void CargarNivel3() { IrANivel(3); }
    public void CargarNivel4() { IrANivel(4); }
    public void CargarNivel5() { IrANivel(5); }

    public void MostrarAjustes()
    {
        if (panelMenu != null) panelMenu.SetActive(false);
        if (panelAjustes != null) panelAjustes.SetActive(true);
        if (panelNiveles != null) panelNiveles.SetActive(false);
    }

    public void MostrarMenuPrincipal()
    {
        if (panelMenu != null) panelMenu.SetActive(true);
        if (panelAjustes != null) panelAjustes.SetActive(false);
        if (panelNiveles != null) panelNiveles.SetActive(false);
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