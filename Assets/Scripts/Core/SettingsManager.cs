using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ajustes del juego. Todo se guarda en PlayerPrefs.
///  · Calidad gráfica: Alta / Media / Baja (afecta sombras, cascadas, VSync, AA
///    y el nivel de calidad general de Unity).
///  · Resolución: lista de mayor a menor.
///  · Pantalla completa, volumen maestro y sensibilidad del ratón (la lee el
///    PlayerController desde la clave "sensibilidad").
/// </summary>
public class SettingsManager : MonoBehaviour
{
    [Header("UI — Controles")]
    public Dropdown calidadDropdown;      // 0 = Alta, 1 = Media, 2 = Baja
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

    // ── Calidad gráfica: Alta / Media / Baja ─────────────────────

    private void ConfigurarCalidad()
    {
        if (calidadDropdown == null) return;

        calidadDropdown.ClearOptions();
        calidadDropdown.AddOptions(new List<string> { "Alta", "Media", "Baja" });

        calidadDropdown.value = AjustesJuego.PerfilGuardado; // 0 = Alta
        calidadDropdown.RefreshShownValue();
        calidadDropdown.onValueChanged.AddListener(CambiarPerfilGrafico);

        CambiarPerfilGrafico(calidadDropdown.value);
    }

    /// <summary>0 = Alta, 1 = Media, 2 = Baja.</summary>
    public void CambiarPerfilGrafico(int perfil)
    {
        AjustesJuego.AplicarPerfilGrafico(perfil);
    }

    // ── Resoluciones (de mayor a menor) ───────────────────────────

    private void ConfigurarResoluciones()
    {
        if (resolucionDropdown == null) return;

        resoluciones = Screen.resolutions;
        if (resoluciones == null || resoluciones.Length == 0) return;

        resolucionDropdown.ClearOptions();
        resolucionDropdown.AddOptions(new List<string>(AjustesJuego.ListaResoluciones()));
        resolucionDropdown.value = AjustesJuego.IndiceResolucionGuardado;
        resolucionDropdown.RefreshShownValue();
        resolucionDropdown.onValueChanged.AddListener(CambiarResolucion);
    }

    public void CambiarResolucion(int indice)
    {
        AjustesJuego.AplicarResolucion(indice);
    }

    public void CambiarPantallaCompleta(bool activa)
    {
        AjustesJuego.AplicarPantallaCompleta(activa);
    }

    // ── Volumen y sensibilidad ────────────────────────────────────

    public void CambiarVolumen(float valor)
    {
        AjustesJuego.AplicarVolumen(valor);
    }

    public void CambiarSensibilidad(float valor)
    {
        AjustesJuego.AplicarSensibilidad(valor);
    }
}
