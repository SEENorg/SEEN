using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Lee el teclado y el ratón del jugador. No guarda estado: cada método consulta el
/// dispositivo en el momento, así que el resultado es correcto sin importar en qué
/// orden se llamen los demás componentes del jugador.
/// </summary>
public class PlayerInputReader
{
    /// <summary>Dirección de movimiento: WASD o flechas (x = lateral, y = adelante/atrás).</summary>
    public Vector2 LeerMovimiento()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 move = Vector2.zero;
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
        }
        return Vector2.ClampMagnitude(move, 1f);
#else
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
    }

    /// <summary>Movimiento del ratón en este frame.</summary>
    public Vector2 LeerMirada()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.delta.ReadValue();
        return Vector2.zero;
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }

    /// <summary>Espacio: salto.</summary>
    public bool SaltoPresionado()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        return kb != null && kb.spaceKey.wasPressedThisFrame;
#else
        return Input.GetButtonDown("Jump");
#endif
    }

    /// <summary>Shift o clic derecho: dash.</summary>
    public bool DashPresionado()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool shift = kb != null && (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame);
        bool rmb = mouse != null && mouse.rightButton.wasPressedThisFrame;
        return shift || rmb;
#else
        return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetMouseButtonDown(1);
#endif
    }
    /// <summary>Escape: liberar el cursor.</summary>
    public bool LiberarCursorPresionado()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    /// <summary>Clic izquierdo: volver a capturar el cursor.</summary>
    public bool CapturarCursorPresionado()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }
}