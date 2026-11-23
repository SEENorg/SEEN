
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

    public PlayerInputReader(PlayerInput input)
    {
        playerInput = input;

        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        jumpAction = playerInput.actions["Jump"];
        sprintAction = playerInput.actions["Sprint"];
        attackAction = playerInput.actions["Attack"];
        interactAction = playerInput.actions["Interact"];
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

    public bool DashPresionado()
    {
        return sprintAction.WasPressedThisFrame();
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

public bool InteraccionPresionada()
{
    return interactAction.WasPressedThisFrame();
}
}
