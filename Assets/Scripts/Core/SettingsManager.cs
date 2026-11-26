using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Ajustes del juego. Todo se guarda en PlayerPrefs.
///  · Calidad gráfica: Alta / Media / Baja.
///  · Resolución: lista de mayor a menor.
///  · Pantalla completa, volumen maestro y sensibilidad del ratón.
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
        IniciarVolumen();
        IniciarSensibilidad();
        ConfigurarCalidad();
        ConfigurarResoluciones();
        IniciarPantallaCompleta();
    }

    private void IniciarVolumen()
    {
        if (volumenSlider == null) return;
        volumenSlider.value = PlayerPrefs.GetFloat(AjustesJuego.CLAVE_VOLUMEN, 1f);
        volumenSlider.onValueChanged.AddListener(CambiarVolumen);
        AudioListener.volume = volumenSlider.value;
    }

    private void IniciarSensibilidad()
    {
        if (sensibilidadSlider == null) return;
        sensibilidadSlider.value = PlayerPrefs.GetFloat(AjustesJuego.CLAVE_SENSIBILIDAD, 0.15f);
        sensibilidadSlider.onValueChanged.AddListener(CambiarSensibilidad);
    }

    private void IniciarPantallaCompleta()
    {
        if (pantallaCompletaToggle == null) return;
        pantallaCompletaToggle.isOn = Screen.fullScreen;
        pantallaCompletaToggle.onValueChanged.AddListener(CambiarPantallaCompleta);
    }

    // ── Calidad gráfica: Alta / Media / Baja ─────────────────────

    private void ConfigurarCalidad()
    {
        if (calidadDropdown == null) return;

        calidadDropdown.ClearOptions();
        List<string> opciones = new List<string>();
        opciones.Add("Alta");
        opciones.Add("Media");
        opciones.Add("Baja");
        calidadDropdown.AddOptions(opciones);

        calidadDropdown.value = AjustesJuego.PerfilGuardado;
        calidadDropdown.RefreshShownValue();
        calidadDropdown.onValueChanged.AddListener(CambiarPerfilGrafico);

        CambiarPerfilGrafico(calidadDropdown.value);
    }

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
        List<string> opciones = new List<string>(AjustesJuego.ListaResoluciones());
        resolucionDropdown.AddOptions(opciones);
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

    public void CambiarVolumen(float valor)
    {
        AjustesJuego.AplicarVolumen(valor);
    }

    public void CambiarSensibilidad(float valor)
    {
        AjustesJuego.AplicarSensibilidad(valor);
    }
}