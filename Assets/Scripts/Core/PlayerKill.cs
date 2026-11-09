using UnityEngine;

/// <summary>
/// Bus de eventos de muerte del jugador. Cualquier hazard (barras rotatorias,
/// entidades hostiles, etc.) llama a PlayerKill.KillPlayer() y el
/// RespawnOnFall del jugador lo escucha para reaparecer en el último checkpoint.
/// </summary>
public static class PlayerKill
{
    public static event System.Action OnKill;

    public static void KillPlayer()
    {
        OnKill?.Invoke();
    }
}
