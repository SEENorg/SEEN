using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ajustes del juego: calidad gráfica, resolución, pantalla completa,
/// volumen maestro y sensibilidad del ratón. Los valores se guardan en PlayerPrefs.
/// La sensibilidad la aplica el PlayerController leyendo la misma clave.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    [Header("UI — Controles")]
    public Dropdown calidadDropdown;
    public Dropdown resolucionDropdown;
    public Toggle pantallaCompletaToggle;
    public Slider volumenSlider;
    public Slider sensibilidadSlider;

    private Resolution[] resoluciones;

    private void Start()
    {
        if (volumenSlider != null)
        {
            volumenSlider.value = PlayerPrefs.GetFloat("volumen", 1f);
            volumenSlider.onValueChanged.AddListener(CambiarVolumen);
            AudioListener.volume = volumenSlider.value;
        }

        if (sensibilidadSlider != null)
        {
            sensibilidadSlider.value = PlayerPrefs.GetFloat("sensibilidad", 0.15f);
            sensibilidadSlider.onValueChanged.AddListener(CambiarSensibilidad);
        }

        ConfigurarCalidad();
        ConfigurarResoluciones();

        if (pantallaCompletaToggle != null)
        {
            pantallaCompletaToggle.isOn = Screen.fullScreen;
            pantallaCompletaToggle.onValueChanged.AddListener(CambiarPantallaCompleta);
        }
    }

    // ── Calidad ───────────────────────────────────────────────────

    private void ConfigurarCalidad()
    {
        if (calidadDropdown == null) return;

        calidadDropdown.ClearOptions();
        var opciones = new List<string>(QualitySettings.names);
        calidadDropdown.AddOptions(opciones);

        int guardada = PlayerPrefs.GetInt("calidad", QualitySettings.GetQualityLevel());
        guardada = Mathf.Clamp(guardada, 0, opciones.Count - 1);
        calidadDropdown.value = guardada;
        calidadDropdown.RefreshShownValue();
        calidadDropdown.onValueChanged.AddListener(CambiarCalidad);
    }

    public void CambiarCalidad(int indice)
    {
        QualitySettings.SetQualityLevel(indice);
        PlayerPrefs.SetInt("calidad", indice);
    }

    // ── Resolución ────────────────────────────────────────────────

    private void ConfigurarResoluciones()
    {
        if (resolucionDropdown == null) return;

        resoluciones = Screen.resolutions;
        if (resoluciones.Length == 0) return;

        resolucionDropdown.ClearOptions();

        var opciones = new List<string>();
        int actual = 0;
        for (int i = 0; i < resoluciones.Length; i++)
        {
            opciones.Add($"{resoluciones[i].width} x {resoluciones[i].height}");
            if (resoluciones[i].width == Screen.currentResolution.width &&
                resoluciones[i].height == Screen.currentResolution.height)
                actual = i;
        }

        resolucionDropdown.AddOptions(opciones);
        resolucionDropdown.value = PlayerPrefs.GetInt("resolucion", actual);
        resolucionDropdown.RefreshShownValue();
        resolucionDropdown.onValueChanged.AddListener(CambiarResolucion);
    }

    public void CambiarResolucion(int indice)
    {
        if (resoluciones == null || indice < 0 || indice >= resoluciones.Length) return;

        Resolution r = resoluciones[indice];
        Screen.SetResolution(r.width, r.height, Screen.fullScreen);
        PlayerPrefs.SetInt("resolucion", indice);
    }

    public void CambiarPantallaCompleta(bool activa)
    {
        Screen.fullScreen = activa;
        PlayerPrefs.SetInt("pantallaCompleta", activa ? 1 : 0);
    }

    // ── Volumen y sensibilidad ────────────────────────────────────

    public void CambiarVolumen(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat("volumen", valor);
    }

    public void CambiarSensibilidad(float valor)
    {
        PlayerPrefs.SetFloat("sensibilidad", valor);
    }
}
