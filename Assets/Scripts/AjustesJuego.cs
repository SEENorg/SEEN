using UnityEngine;

/// <summary>
/// Lógica compartida de ajustes (usada por el menú principal y por el menú
/// de pausa). Todo se guarda en PlayerPrefs.
/// </summary>
public static class AjustesJuego
{
    public const int PERFIL_ALTA = 0;   // 0 = Alta, 1 = Media, 2 = Baja
    public const int PERFIL_MEDIA = 1;
    public const int PERFIL_BAJA = 2;

    /// <summary>Aplica el perfil gráfico (sombras, cascadas, VSync, AA, calidad).</summary>
    public static void AplicarPerfilGrafico(int perfil)
    {
        perfil = Mathf.Clamp(perfil, PERFIL_ALTA, PERFIL_BAJA);
        string[] niveles = QualitySettings.names;

        switch (perfil)
        {
            case PERFIL_ALTA:
                QualitySettings.SetQualityLevel(niveles.Length - 1, true);
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowDistance = 120f;
                QualitySettings.shadowCascades = 4;
                QualitySettings.vSyncCount = 1;
                QualitySettings.antiAliasing = 4;
                break;

            case PERFIL_MEDIA:
                int mid = Mathf.Max(0, (niveles.Length - 1) / 2);
                QualitySettings.SetQualityLevel(mid, true);
                QualitySettings.shadows = ShadowQuality.HardOnly;
                QualitySettings.shadowDistance = 60f;
                QualitySettings.shadowCascades = 2;
                QualitySettings.vSyncCount = 0;
                QualitySettings.antiAliasing = 2;
                break;

            case PERFIL_BAJA:
                QualitySettings.SetQualityLevel(0, true);
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.shadowDistance = 0f;
                QualitySettings.shadowCascades = 1;
                QualitySettings.vSyncCount = 0;
                QualitySettings.antiAliasing = 0;
                break;
        }

        PlayerPrefs.SetInt("perfilGrafico", perfil);
    }

    public static int PerfilGuardado => PlayerPrefs.GetInt("perfilGrafico", PERFIL_ALTA);

    public static void AplicarVolumen(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat("volumen", v);
    }

    public static float VolumenGuardado => PlayerPrefs.GetFloat("volumen", 1f);

    public static void AplicarSensibilidad(float v)
    {
        PlayerPrefs.SetFloat("sensibilidad", v);
    }

    public static float SensibilidadGuardada => PlayerPrefs.GetFloat("sensibilidad", 0.15f);

    public static void AplicarPantallaCompleta(bool activa)
    {
        Screen.fullScreen = activa;
        PlayerPrefs.SetInt("pantallaCompleta", activa ? 1 : 0);
    }

    public static bool PantallaCompletaGuardada => Screen.fullScreen;

    // ── Resoluciones (índices descendentes: 0 = la más alta) ──────

    /// <summary>Aplica una resolución por índice descendente (0 = la mayor).</summary>
    public static void AplicarResolucion(int indiceDescendente)
    {
        Resolution[] res = Screen.resolutions;
        if (res == null || res.Length == 0) return;

        int idxReal = res.Length - 1 - indiceDescendente;
        if (idxReal < 0 || idxReal >= res.Length) return;

        Screen.SetResolution(res[idxReal].width, res[idxReal].height, Screen.fullScreen);
        PlayerPrefs.SetInt("resolucion", indiceDescendente);
    }

    /// <summary>Lista de resoluciones de mayor a menor.</summary>
    public static string[] ListaResoluciones()
    {
        Resolution[] res = Screen.resolutions;
        if (res == null || res.Length == 0) return new[] { "Auto" };

        var lista = new string[res.Length];
        for (int i = 0; i < res.Length; i++)
            lista[i] = $"{res[res.Length - 1 - i].width} x {res[res.Length - 1 - i].height}";
        return lista;
    }

    /// <summary>Índice descendente guardado (0 = la mayor).</summary>
    public static int IndiceResolucionGuardado
    {
        get
        {
            int guardado = PlayerPrefs.GetInt("resolucion", -1);
            if (guardado >= 0 && guardado < Screen.resolutions.Length) return guardado;

            // Si no hay guardado, ubicar la resolución actual
            int actual = 0;
            Resolution[] res = Screen.resolutions;
            for (int i = res.Length - 1; i >= 0; i--)
            {
                if (res[i].width == Screen.currentResolution.width &&
                    res[i].height == Screen.currentResolution.height)
                {
                    actual = res.Length - 1 - i;
                    break;
                }
            }
            return actual;
        }
    }
}
