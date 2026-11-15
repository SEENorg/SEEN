using UnityEngine;

/// Lógica compartida de ajustes (usada por el menú principal y por el menú
/// de pausa). Todo se guarda en PlayerPrefs.
/// Las claves de PlayerPrefs están centralizadas aquí para evitar duplicación
/// con SettingsManager y PlayerLook.
public static class AjustesJuego
{
    public const int PERFIL_ALTA = 0;
    public const int PERFIL_MEDIA = 1;
    public const int PERFIL_BAJA = 2;

    // Claves de PlayerPrefs (centralizadas)
    public const string CLAVE_PERFIL_GRAFICO = "perfilGrafico";
    public const string CLAVE_VOLUMEN = "volumen";
    public const string CLAVE_SENSIBILIDAD = "sensibilidad";
    public const string CLAVE_PANTALLA_COMPLETA = "pantallaCompleta";
    public const string CLAVE_RESOLUCION = "resolucion";

    /// <summary>Aplica el perfil gráfico (sombras, cascadas, VSync, AA, calidad).</summary>
    public static void AplicarPerfilGrafico(int perfil)
    {
        perfil = Mathf.Clamp(perfil, PERFIL_ALTA, PERFIL_BAJA);
        string[] niveles = QualitySettings.names;

        if (perfil == PERFIL_ALTA)
        {
            QualitySettings.SetQualityLevel(niveles.Length - 1, true);
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 120f;
            QualitySettings.shadowCascades = 4;
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 4;
        }
        else if (perfil == PERFIL_MEDIA)
        {
            int mid = Mathf.Max(0, (niveles.Length - 1) / 2);
            QualitySettings.SetQualityLevel(mid, true);
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowDistance = 60f;
            QualitySettings.shadowCascades = 2;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 2;
        }
        else
        {
            QualitySettings.SetQualityLevel(0, true);
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
            QualitySettings.shadowCascades = 1;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
        }

        PlayerPrefs.SetInt(CLAVE_PERFIL_GRAFICO, perfil);
    }

    public static int PerfilGuardado
    {
        get { return PlayerPrefs.GetInt(CLAVE_PERFIL_GRAFICO, PERFIL_ALTA); }
    }

    public static void AplicarVolumen(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(CLAVE_VOLUMEN, v);
    }

    public static float VolumenGuardado
    {
        get { return PlayerPrefs.GetFloat(CLAVE_VOLUMEN, 1f); }
    }

    public static void AplicarSensibilidad(float v)
    {
        PlayerPrefs.SetFloat(CLAVE_SENSIBILIDAD, v);
    }

    public static float SensibilidadGuardada
    {
        get { return PlayerPrefs.GetFloat(CLAVE_SENSIBILIDAD, 0.15f); }
    }

    public static void AplicarPantallaCompleta(bool activa)
    {
        Screen.fullScreen = activa;
        PlayerPrefs.SetInt(CLAVE_PANTALLA_COMPLETA, activa ? 1 : 0);
    }

    public static bool PantallaCompletaGuardada
    {
        get { return Screen.fullScreen; }
    }

    // ── Resoluciones (índices descendentes: 0 = la más alta) ──────

    /// <summary>Aplica una resolución por índice descendente (0 = la mayor).</summary>
    public static void AplicarResolucion(int indiceDescendente)
    {
        Resolution[] res = Screen.resolutions;
        if (res == null || res.Length == 0) return;

        int idxReal = res.Length - 1 - indiceDescendente;
        if (idxReal < 0 || idxReal >= res.Length) return;

        Screen.SetResolution(res[idxReal].width, res[idxReal].height, Screen.fullScreen);
        PlayerPrefs.SetInt(CLAVE_RESOLUCION, indiceDescendente);
    }

    /// <summary>Lista de resoluciones de mayor a menor.</summary>
    public static string[] ListaResoluciones()
    {
        Resolution[] res = Screen.resolutions;
        if (res == null || res.Length == 0)
        {
            string[] auto = new string[1];
            auto[0] = "Auto";
            return auto;
        }

        string[] lista = new string[res.Length];
        for (int i = 0; i < res.Length; i++)
        {
            Resolution r = res[res.Length - 1 - i];
            lista[i] = r.width + " x " + r.height;
        }
        return lista;
    }

    /// <summary>Índice descendente guardado (0 = la mayor).</summary>
    public static int IndiceResolucionGuardado
    {
        get
        {
            int guardado = PlayerPrefs.GetInt(CLAVE_RESOLUCION, -1);
            if (guardado >= 0 && guardado < Screen.resolutions.Length)
                return guardado;

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