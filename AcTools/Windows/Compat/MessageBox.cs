using System;
using System.Runtime.InteropServices;

namespace AcTools.Windows.Compat {
    public enum DialogResult { None, OK, Cancel, Abort, Retry, Ignore, Yes, No }

    public enum MessageBoxButtons { OK, OKCancel, AbortRetryIgnore, YesNoCancel, YesNo, RetryCancel }

    public enum MessageBoxIcon { None = 0, Error = 16, Question = 32, Warning = 48, Information = 64 }

    /// <summary>
    /// Replacement for System.Windows.Forms.MessageBox, used only for fatal errors before UI is ready. Native message box on Windows,
    /// stderr output elsewhere.
    /// </summary>
    public static class NativeMessageBox {
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW")]
        private static extern int MessageBoxNative(IntPtr hWnd, string text, string caption, uint type);

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None) {
            if (!OperatingSystem.IsWindows()) {
                Console.Error.WriteLine($"[{caption}] {text}");
                return DialogResult.OK;
            }

            switch (MessageBoxNative(IntPtr.Zero, text, caption, (uint)buttons | (uint)icon)) {
                case 2: return DialogResult.Cancel;
                case 3: return DialogResult.Abort;
                case 4: return DialogResult.Retry;
                case 5: return DialogResult.Ignore;
                case 6: return DialogResult.Yes;
                case 7: return DialogResult.No;
                default: return DialogResult.OK;
            }
        }
    }
}
