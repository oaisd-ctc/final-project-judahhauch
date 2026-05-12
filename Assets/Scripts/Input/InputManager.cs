using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static PlayerInput PlayerInput;

    public static Vector2 Movement;
    public static bool JumpWasPressed;
    public static bool JumpisHeld;
    public static bool JumpWasReleased;
    public static bool RunIsHeld;
    public static bool DashWasPressed;
    public static bool AttackWasPressed;


    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction runAction;
    private InputAction dashAction;
    private InputAction attackAction;

    private void Awake()
    {
        PlayerInput = GetComponent<PlayerInput>();

        moveAction = PlayerInput.actions["Move"];
        jumpAction = PlayerInput.actions["Jump"];
        runAction = PlayerInput.actions["Run"];

        dashAction = PlayerInput.actions["Dash"];
        attackAction = PlayerInput.actions["Attack"];

    }


    private void Update()
    {
        Movement = moveAction.ReadValue<Vector2>();

        JumpWasPressed = jumpAction.WasPressedThisFrame();
        JumpisHeld = jumpAction.IsPressed();
        JumpWasReleased = jumpAction.WasReleasedThisFrame();

        RunIsHeld = runAction.IsPressed();
        DashWasPressed = dashAction.WasPressedThisFrame();

        AttackWasPressed = attackAction.WasPressedThisFrame();

    }
}
