using UnityEngine;

/// <summary>
/// Contrato que define las entradas necesarias para controlar al jugador.
/// No depende de ningún dispositivo ni de una implementación concreta.
/// </summary>
public interface IPlayerInput
{
    Vector2 LeerMovimiento();
    Vector2 LeerMirada(out bool esMando);

    bool SaltoPresionado();
    bool DashPresionado();
    bool AtaquePresionado();
    bool InteraccionPresionada();
    bool LiberarCursorPresionado();
    bool CapturarCursorPresionado();
}