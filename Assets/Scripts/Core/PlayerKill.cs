using UnityEngine;

/// Bus de eventos de muerte del jugador. Cualquier hazard (barras rotatorias,
/// entidades hostiles, etc.) llama a PlayerKill.KillPlayer() y el
/// RespawnOnFall del jugador lo escucha para reaparecer en el último checkpoint.
/// Es static porque el evento de muerte es único para todo el juego: no tiene
/// sentido tener varias instancias.
public static class PlayerKill
{
    public static event System.Action OnKill;

    public static void KillPlayer()
    {
        if (OnKill != null)
            OnKill();
    }
}