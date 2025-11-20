using AcTools.FormsReplacement;

namespace AcTools.WindowsEmu.Input;

public class KeyboardEventArgs : EventArgs {
    public readonly Keys Key;
    public bool Handled;

    public KeyboardEventArgs(int keyCode) {
        Key = (Keys)keyCode;
    }
}