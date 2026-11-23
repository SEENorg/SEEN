using UnityEngine;

public interface IPlayerInput
{
    Vector2 LeerMovimiento();
    Vector2 LeerMirada(out bool esMando);

    bool SaltoPresionado();
    bool DashPresionado();
    bool AtaquePresionado();
    bool RecargaPresionada();
    bool InteraccionPresionada();

    bool LiberarCursorPresionado();
    bool CapturarCursorPresionado();
}