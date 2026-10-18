using SharpDX;
using SharpDX.DirectInput;

namespace AcManager.Tools.Helpers.DirectInput {
    /// <summary>
    /// SlimDX had methods instead of properties for joystick state.
    /// </summary>
    public static class SharpDxDirectInputCompat {
        public static bool[] GetButtons(this JoystickState state) {
            return state.Buttons;
        }

        public static int[] GetPointOfViewControllers(this JoystickState state) {
            return state.PointOfViewControllers;
        }

        public static bool IsUnplugged(this SharpDXException e) {
            return e.ResultCode.Code == ResultCode.Unplugged.Code || e.Message.Contains("DIERR_UNPLUGGED");
        }

        public static int[] GetSliders(this JoystickState state) {
            return state.Sliders;
        }
    }
}
