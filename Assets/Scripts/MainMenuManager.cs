using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Paneles de UI")]
    [SerializeField] private GameObject menuPrincipal;
    [SerializeField] private GameObject menuAjustes;

    private void Start()
    {
        // Asegurarse de que al inicio se muestre el menú principal y no los ajustes
        MostrarMenuPrincipal();
    }

    /// <summary>
    /// Se llama desde el botón "Jugar"
    /// </summary>
    public void Jugar()
    {
        // Carga la siguiente escena en la lista (Build Settings). 
        // Normalmente el Menú es el índice 0, y el Nivel 1 es el índice 1.
        Debug.Log("Cargando el nivel del juego...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    /// <summary>
    /// Se llama desde el botón "Ajustes" o "Gráficos"
    /// </summary>
    public void MostrarAjustes()
    {
        if (menuPrincipal != null) menuPrincipal.SetActive(false);
        if (menuAjustes != null) menuAjustes.SetActive(true);
    }

    /// <summary>
    /// Se llama desde el botón "Volver" en la pantalla de ajustes
    /// </summary>
    public void MostrarMenuPrincipal()
    {
        if (menuAjustes != null) menuAjustes.SetActive(false);
        if (menuPrincipal != null) menuPrincipal.SetActive(true);
    }

    /// <summary>
    /// Se llama desde el botón "Salir"
    /// </summary>
    public void SalirDelJuego()
    {
        Debug.Log("Saliendo de SEEN...");
        Application.Quit();
    }

    // --- FUNCIONES EXTRA PARA AJUSTES (Opcional) ---

    public void CambiarCalidadGrafica(int indiceCalidad)
    {
        // El dropdown debe pasar valores como 0 (Bajo), 1 (Medio), 2 (Alto)
        QualitySettings.SetQualityLevel(indiceCalidad);
        Debug.Log("Calidad gráfica cambiada a: " + QualitySettings.names[indiceCalidad]);
    }
}
