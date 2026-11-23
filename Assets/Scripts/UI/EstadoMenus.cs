/// <summary>
/// Un solo lugar para preguntar si hay un menú encima del juego (pausa, pantalla de
/// controles, nivel completado o game over). Los HUD dibujados con OnGUI lo usan para
/// esconderse, porque OnGUI se dibuja por encima de los menús hechos con Canvas.
/// </summary>
public static class EstadoMenus
{
    public static bool HayMenuAbierto
    {
        get
        {
            if (PauseMenu.Pausado) return true;
            if (PantallaControles.Visible) return true;
            if (GameFeedback.NivelCompletadoVisible) return true;
            if (GameFeedback.GameOverVisible) return true;
            return false;
        }
    }
}
