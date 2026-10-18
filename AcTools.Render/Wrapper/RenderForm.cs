using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AcTools.Render.Wrapper {
    /// <summary>
    /// A form to render into. Replacement for SlimDX.Windows.RenderForm.
    /// </summary>
    public class RenderForm : Form {
        private bool _isUserResizing;
        private Size _cachedSize;
        private FormWindowState _previousWindowState;

        public RenderForm() : this("AcTools") { }

        public RenderForm(string text) {
            Text = text;
            ClientSize = new Size(800, 600);
            ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Opaque, true);
        }

        /// <summary>
        /// Raised when user finished resizing the window (or maximized/restored it).
        /// </summary>
        public event EventHandler UserResized;

        protected virtual void OnUserResized(EventArgs e) {
            UserResized?.Invoke(this, e);
        }

        protected override void OnClientSizeChanged(EventArgs e) {
            base.OnClientSizeChanged(e);
            if (!_isUserResizing && (Size != _cachedSize || WindowState != _previousWindowState)) {
                OnUserResized(EventArgs.Empty);
            }

            _cachedSize = Size;
            _previousWindowState = WindowState;
        }

        protected override void OnResizeBegin(EventArgs e) {
            base.OnResizeBegin(e);
            _isUserResizing = true;
            _cachedSize = Size;
        }

        protected override void OnResizeEnd(EventArgs e) {
            base.OnResizeEnd(e);
            _isUserResizing = false;
            if (Size != _cachedSize) {
                OnUserResized(EventArgs.Empty);
            }

            _cachedSize = Size;
        }

        protected override void OnPaintBackground(PaintEventArgs e) {
            // Rendering is done by Direct3D
        }
    }

    public delegate void MainLoop();

    /// <summary>
    /// Idle-based message pump. Replacement for SlimDX.Windows.MessagePump.
    /// </summary>
    public static class MessagePump {
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMessage {
            public IntPtr Handle;
            public uint Message;
            public IntPtr WParam;
            public IntPtr LParam;
            public uint Time;
            public int X;
            public int Y;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PeekMessage(out NativeMessage message, IntPtr hwnd, uint messageFilterMin, uint messageFilterMax, uint flags);

        public static bool IsApplicationIdle => !PeekMessage(out _, IntPtr.Zero, 0, 0, 0);

        private class IdleHandler {
            private readonly MainLoop _mainLoop;

            public IdleHandler(MainLoop mainLoop) {
                _mainLoop = mainLoop;
            }

            public void OnIdle(object sender, EventArgs e) {
                while (IsApplicationIdle) {
                    _mainLoop();
                }
            }
        }

        public static void Run(Form form, MainLoop mainLoop) {
            if (form == null) throw new ArgumentNullException(nameof(form));
            if (mainLoop == null) throw new ArgumentNullException(nameof(mainLoop));

            var handler = new IdleHandler(mainLoop);
            Application.Idle += handler.OnIdle;
            try {
                Application.Run(form);
            } finally {
                Application.Idle -= handler.OnIdle;
            }
        }
    }
}
