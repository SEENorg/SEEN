using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;

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

    [Header("Ajustes")]
    public SettingsManager ajustes;

    private void Start()
    {
        MostrarMenuPrincipal();

        // En el menú el ratón debe estar libre y visible
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        eventSystem = EventSystem.current;
        
    }
    private void OnEnable()
{
    var uiModule = FindFirstObjectByType<InputSystemUIInputModule>();

    if (uiModule != null && uiModule.cancel.action != null)
    {
        uiModule.cancel.action.performed += AlCancelar;
    }
}

private void OnDisable()
{
    var uiModule = FindFirstObjectByType<InputSystemUIInputModule>();

    if (uiModule != null && uiModule.cancel.action != null)
    {
        uiModule.cancel.action.performed -= AlCancelar;
    }
}

private void AlCancelar(UnityEngine.InputSystem.InputAction.CallbackContext context)
{
    if (panelAjustes != null && panelAjustes.activeSelf)
    {
        MostrarMenuPrincipal();
    }
    else if (panelNiveles != null && panelNiveles.activeSelf)
    {
        MostrarMenuPrincipal();
    }
}
private EventSystem eventSystem;

    /// <summary>Botón JUGAR: muestra el selector de niveles.</summary>
    public void Jugar()
    {
        Time.timeScale = 1f;
        if (panelMenu != null) panelMenu.SetActive(false);
        if (panelAjustes != null) panelAjustes.SetActive(false);
        if (panelNiveles != null) panelNiveles.SetActive(true);
        if (botonNivel1 != null)
{
    EventSystem.current.SetSelectedGameObject(botonNivel1.gameObject);
}
    }

    /// <summary>Carga un nivel por su número (1-4).</summary>
    public void IrANivel(int numero)
    {
        string escena = numero == 1 ? "Juego" :
                        numero == 2 ? "Nivel2" :
                        numero == 3 ? "Nivel3" :
                        numero == 4 ? "Nivel4" : "Juego";

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

    public void MostrarAjustes()
{
    if (panelMenu != null) panelMenu.SetActive(false);
    if (panelAjustes != null) panelAjustes.SetActive(true);
    if (panelNiveles != null) panelNiveles.SetActive(false);

    if (eventSystem != null && panelAjustes != null)
    {
        eventSystem.SetSelectedGameObject(null);

        Selectable primerElemento =
            panelAjustes.GetComponentInChildren<Selectable>();

        if (primerElemento != null)
        {
            eventSystem.SetSelectedGameObject(primerElemento.gameObject);
        }
    }
}

   public void MostrarMenuPrincipal()
{
    if (panelMenu != null) panelMenu.SetActive(true);
    if (panelAjustes != null) panelAjustes.SetActive(false);
    if (panelNiveles != null) panelNiveles.SetActive(false);

    if (eventSystem != null)
    {
        eventSystem.SetSelectedGameObject(null);

        if (panelMenu != null)
        {
            Button primerBoton = panelMenu.GetComponentInChildren<Button>();

            if (primerBoton != null)
            {
                eventSystem.SetSelectedGameObject(primerBoton.gameObject);
            }
        }
    }
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