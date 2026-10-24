using UnityEngine;
using UnityEngine.InputSystem;

namespace Verso
{
    /// <summary>
    /// Leitura centralizada de input (teclado/mouse + gamepad) usando o Input System.
    /// Mapeamento conforme GDD: WASD/analógico, Espaço/A, F ou clique direito/Y, R/Select, Esc/Start.
    /// </summary>
    public static class GameInput
    {
        public static float MouseSensitivity = 0.12f;
        public static float GamepadLookSpeed = 180f;

        public static Vector2 Move
        {
            get
            {
                Vector2 v = Vector2.zero;
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
                }
                var gp = Gamepad.current;
                if (gp != null)
                {
                    Vector2 stick = gp.leftStick.ReadValue();
                    if (stick.sqrMagnitude > 0.02f) v += stick;
                }
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        /// <summary>Delta de câmera em graus para este frame.</summary>
        public static Vector2 LookDelta
        {
            get
            {
                Vector2 v = Vector2.zero;
                var mouse = Mouse.current;
                if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
                    v += mouse.delta.ReadValue() * MouseSensitivity;
                var gp = Gamepad.current;
                if (gp != null)
                {
                    Vector2 stick = gp.rightStick.ReadValue();
                    if (stick.sqrMagnitude > 0.02f) v += stick * GamepadLookSpeed * Time.unscaledDeltaTime;
                }
                return v;
            }
        }

        public static bool JumpPressed =>
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        public static bool JumpHeld =>
            (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed);

        public static bool FlipPressed =>
            (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) ||
            (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked && Mouse.current.rightButton.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);

        public static bool RestartPressed =>
            (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame);

        public static bool PausePressed =>
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

        public static bool ClickPressed =>
            Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        public static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
