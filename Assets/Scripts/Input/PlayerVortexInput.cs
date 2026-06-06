using UnityEngine;
using VortexGame.Core;

namespace VortexGame.Input
{
    public sealed class PlayerVortexInput : MonoBehaviour
    {
        private VortexEntity vortex;
        private VirtualJoystick joystick;

        public void Initialize(VortexEntity controlledVortex, VirtualJoystick movementJoystick)
        {
            vortex = controlledVortex;
            joystick = movementJoystick;
        }

        private void Update()
        {
            if (vortex == null)
            {
                return;
            }

            Vector2 moveVector = joystick != null ? joystick.Value : GetFallbackInput();
            vortex.SetInput(moveVector);
        }

        private static Vector2 GetFallbackInput()
        {
            return new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical"));
        }
    }
}

