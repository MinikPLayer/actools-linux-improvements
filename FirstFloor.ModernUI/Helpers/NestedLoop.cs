using System.Threading.Tasks;
using Avalonia.Threading;

namespace FirstFloor.ModernUI.Helpers {
    /// <summary>
    /// WPF-like blocking calls (modal dialogs and so on) need a nested message loop, which in Avalonia is a dispatcher frame.
    /// </summary>
    public static class NestedLoop {
        public static void RunUntilCompleted(Task task) {
            if (task.IsCompleted) return;

            var frame = new DispatcherFrame();
            task.ContinueWith(_ => Dispatcher.UIThread.Post(() => frame.Continue = false), TaskScheduler.Default);
            Dispatcher.UIThread.PushFrame(frame);
        }
    }
}
