using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sistema de vida del jugador. Se integra con el flujo de muerte existente
/// (PlayerKill.OnKill) sin reemplazarlo: reduce una vida al recibir daño letal
/// y reinicia al personaje cuando su vida llega a 0.
/// </summary>
public class VidaJugador : MonoBehaviour
{
    public static VidaJugador Instancia { get; private set; }

    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 3;
    [SerializeField] private int vidaActual = 3;
    [SerializeField] private float cooldownDaño = 0.6f;

    private float tiempoUltimoDaño = -10f;

    /// <summary>Se dispara cada vez que la vida cambia (vidaActual, vidaMaxima).</summary>
    public System.Action<int, int> OnVidaCambiada;

    public int VidaActual => vidaActual;
    public int VidaMaxima => vidaMaxima;
    public float VidaNormalizada => vidaMaxima > 0 ? (float)vidaActual / vidaMaxima : 0f;

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(this);
            return;
        }
        Instancia = this;
        vidaActual = vidaMaxima;
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    private void OnEnable()
    {
        PlayerKill.OnKill += AlRecibirDañoLetal;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void OnDisable()
    {
        PlayerKill.OnKill -= AlRecibirDañoLetal;
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    /// <summary>Restaura la vida al máximo al volver al menú principal.</summary>
    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (escena.name == "MainMenu")
        {
            RestaurarVida();
        }
    }

    /// <summary>Llamado cuando el jugador recibe un impacto de amenaza o peligro.</summary>
    private void AlRecibirDañoLetal()
    {
        if (Time.unscaledTime - tiempoUltimoDaño < cooldownDaño) return;
        tiempoUltimoDaño = Time.unscaledTime;

        vidaActual = Mathf.Max(0, vidaActual - 1);
        OnVidaCambiada?.Invoke(vidaActual, vidaMaxima);

        if (vidaActual <= 0)
        {
            RespawnOnFall respawn = GetComponent<RespawnOnFall>();
            if (respawn == null) respawn = FindFirstObjectByType<RespawnOnFall>();
            if (respawn != null) respawn.DoRespawn();
            RestaurarVida();
        }
    }

    /// <summary>Restaura la vida al máximo (al cambiar de nivel o reiniciar partida).</summary>
    public void RestaurarVida()
    {
        vidaActual = vidaMaxima;
        OnVidaCambiada?.Invoke(vidaActual, vidaMaxima);
    }
}
