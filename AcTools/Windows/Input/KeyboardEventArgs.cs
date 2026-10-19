using System;
using AcTools.Windows.Compat;

namespace AcTools.Windows.Input {
    public class KeyboardEventArgs : EventArgs {
        public readonly Keys Key;
        public bool Handled;

        public KeyboardEventArgs(int keyCode) {
            Key = (Keys)keyCode;
        }
    }
}