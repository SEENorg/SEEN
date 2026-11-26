
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : IPlayerInput
{
    private readonly PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
private InputAction attackAction;
private InputAction interactAction;
private InputAction reloadAction;

    public PlayerInputReader(PlayerInput input)
    {
        playerInput = input;

        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        jumpAction = playerInput.actions["Jump"];
        sprintAction = playerInput.actions["Sprint"];
        attackAction = playerInput.actions["Attack"];
        interactAction = playerInput.actions["Interact"];
        reloadAction = playerInput.actions["Reload"];
    }

    public Vector2 LeerMovimiento()
    {
        return moveAction.ReadValue<Vector2>();
    }

   public Vector2 LeerMirada(out bool esMando)
{
    esMando = lookAction.activeControl != null &&
              lookAction.activeControl.device is Gamepad;

    return lookAction.ReadValue<Vector2>();
}

    public bool SaltoPresionado()
    {
        return jumpAction.WasPressedThisFrame();
    }

    /// <summary>
    /// Dash: la acción "Sprint" del Input System, y además SIEMPRE Shift o clic derecho
    /// directo del teclado/ratón. Así el dash funciona aunque el mapa de controles
    /// (Input Actions) cambie o no tenga Shift asignado.
    /// </summary>
    public bool DashPresionado()
    {
        if (sprintAction != null && sprintAction.WasPressedThisFrame()) return true;

        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool shift = kb != null && (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame);
        bool clicDerecho = mouse != null && mouse.rightButton.wasPressedThisFrame;
        return shift || clicDerecho;
    }

    public bool LiberarCursorPresionado()
    {
        return Keyboard.current != null &&
               Keyboard.current.escapeKey.wasPressedThisFrame;
    }

    public bool CapturarCursorPresionado()
    {
        return Mouse.current != null &&
               Mouse.current.leftButton.wasPressedThisFrame;
    }
    public bool AtaquePresionado()
    {
        return attackAction.WasPressedThisFrame();
    }

    public bool AtaqueMantenido()
    {
        return attackAction != null && attackAction.IsPressed();
    }

public bool InteraccionPresionada()
{
    return interactAction.WasPressedThisFrame();
}
public bool RecargaPresionada()
{
    return reloadAction.WasPressedThisFrame();
}
}
