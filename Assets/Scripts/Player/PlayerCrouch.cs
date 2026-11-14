/// <summary>
/// Estado de "agachado": el jugador está agachado solo si mantiene la tecla Y está en el suelo.
/// Otros componentes lo consultan (el movimiento lo usa para deslizarse, la cámara para bajar).
/// </summary>
public class PlayerCrouch
{
    private bool agachado;

    public bool Agachado
    {
        get { return agachado; }
    }

    /// <summary>Actualiza el estado. Se llama en Update.</summary>
    public void Actualizar(bool quiereAgacharse, bool enSuelo)
    {
        agachado = quiereAgacharse && enSuelo;
    }

    /// <summary>Vuelve a estar de pie (al reaparecer).</summary>
    public void Reiniciar()
    {
        agachado = false;
    }
}