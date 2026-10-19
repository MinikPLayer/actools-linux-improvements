using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace System.Windows.Input {
    public delegate void ExecutedRoutedEventHandler(object sender, ExecutedRoutedEventArgs e);

    public delegate void CanExecuteRoutedEventHandler(object sender, CanExecuteRoutedEventArgs e);

    public class ExecutedRoutedEventArgs : EventArgs {
        public ExecutedRoutedEventArgs(ICommand command, object parameter) {
            Command = command;
            Parameter = parameter;
        }

        public ICommand Command { get; }
        public object Parameter { get; }
        public bool Handled { get; set; }
        public object OriginalSource { get; set; }
        public object Source { get; set; }
    }

    public class CanExecuteRoutedEventArgs : EventArgs {
        public CanExecuteRoutedEventArgs(ICommand command, object parameter) {
            Command = command;
            Parameter = parameter;
        }

        public ICommand Command { get; }
        public object Parameter { get; }
        public bool CanExecute { get; set; }
        public bool ContinueRouting { get; set; }
        public bool Handled { get; set; }
        public object OriginalSource { get; set; }
        public object Source { get; set; }
    }

    public class CommandBinding {
        public CommandBinding() { }

        public CommandBinding(ICommand command) {
            Command = command;
        }

        public CommandBinding(ICommand command, ExecutedRoutedEventHandler executed) {
            Command = command;
            Executed += executed;
        }

        public CommandBinding(ICommand command, ExecutedRoutedEventHandler executed, CanExecuteRoutedEventHandler canExecute) {
            Command = command;
            Executed += executed;
            CanExecute += canExecute;
        }

        public ICommand Command { get; set; }

        public event ExecutedRoutedEventHandler PreviewExecuted;
        public event ExecutedRoutedEventHandler Executed;
        public event CanExecuteRoutedEventHandler PreviewCanExecute;
        public event CanExecuteRoutedEventHandler CanExecute;

        internal bool HasCanExecute => CanExecute != null || PreviewCanExecute != null;

        internal void RaiseCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            PreviewCanExecute?.Invoke(sender, e);
            if (!e.Handled) CanExecute?.Invoke(sender, e);
        }

        internal void RaiseExecuted(object sender, ExecutedRoutedEventArgs e) {
            PreviewExecuted?.Invoke(sender, e);
            if (!e.Handled) Executed?.Invoke(sender, e);
        }
    }

    public class CommandBindingCollection : List<CommandBinding> {
        public void AddRange(IEnumerable<CommandBinding> bindings) {
            foreach (var b in bindings) Add(b);
        }
    }

    public class InputGestureCollection : List<KeyGesture> { }

    public static class CommandManager {
        private static readonly ConditionalWeakTable<object, CommandBindingCollection> Bindings = new ConditionalWeakTable<object, CommandBindingCollection>();
        private static readonly List<(Type, CommandBinding)> ClassBindings = new List<(Type, CommandBinding)>();

        public static event EventHandler RequerySuggested;

        private static bool _scheduled;

        public static void InvalidateRequerySuggested() {
            if (_scheduled) return;
            _scheduled = true;
            Dispatcher.UIThread.Post(() => {
                _scheduled = false;
                RequerySuggested?.Invoke(null, EventArgs.Empty);
            }, DispatcherPriority.Input);
        }

        public static void RegisterClassCommandBinding(Type type, CommandBinding binding) {
            ClassBindings.Add((type, binding));
        }

        internal static CommandBindingCollection GetBindings(object element) {
            return Bindings.GetValue(element, _ => new CommandBindingCollection());
        }

        internal static IEnumerable<CommandBinding> GetAllBindings(object element) {
            if (Bindings.TryGetValue(element, out var local)) {
                foreach (var b in local.ToList()) yield return b;
            }

            var type = element.GetType();
            foreach (var (t, b) in ClassBindings.ToList()) {
                if (t.IsAssignableFrom(type)) yield return b;
            }
        }
    }

    public static class CommandBindingsExtension {
        extension(Avalonia.Controls.Control c) {
            public CommandBindingCollection CommandBindings => CommandManager.GetBindings(c);
        }

        extension(TopLevel c) {
            public CommandBindingCollection CommandBindings => CommandManager.GetBindings(c);
        }
    }

    /// <summary>
    /// Command routed through the visual tree: finds a CommandBinding starting from the target and going up.
    /// </summary>
    public class RoutedCommand : ICommand {
        private static WeakReference<Avalonia.Controls.Control> _lastSource;
        private static bool _hooked;

        public RoutedCommand() {
            HookSource();
        }

        public RoutedCommand(string name, Type ownerType) : this() {
            Name = name;
            OwnerType = ownerType;
        }

        public RoutedCommand(string name, Type ownerType, InputGestureCollection inputGestures) : this(name, ownerType) {
            InputGestures = inputGestures;
        }

        public string Name { get; }
        public Type OwnerType { get; }
        public InputGestureCollection InputGestures { get; } = new InputGestureCollection();

        private static void HookSource() {
            if (_hooked) return;
            _hooked = true;
            Keyboard.Hook();
            Dispatcher.UIThread.Post(() => {
                InputElement.PointerPressedEvent.AddClassHandler<Avalonia.Controls.Control>((s, e) => _lastSource = new WeakReference<Avalonia.Controls.Control>(s), RoutingStrategies.Tunnel, true);
            });
        }

        public event EventHandler CanExecuteChanged {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        private static IEnumerable<object> Route(object target) {
            var current = target as Visual;
            while (current != null) {
                yield return current;
                current = current.GetVisualParent() ?? (current as Control)?.Parent as Visual;
            }

            if (target is Visual v && v.GetVisualRoot() is TopLevel top && !Equals(top, target)) {
                yield return top;
            }
        }

        private static object GetDefaultTarget() {
            if (_lastSource != null && _lastSource.TryGetTarget(out var source) && source.GetVisualRoot() != null) return source;
            var focused = Keyboard.FocusedElement;
            if (focused != null) return focused;
            return (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        }

        public bool CanExecute(object parameter) => CanExecute(parameter, GetDefaultTarget());

        public bool CanExecute(object parameter, object target) {
            foreach (var element in Route(target)) {
                foreach (var binding in CommandManager.GetAllBindings(element)) {
                    if (!ReferenceEquals(binding.Command, this)) continue;
                    if (!binding.HasCanExecute) return true;

                    var args = new CanExecuteRoutedEventArgs(this, parameter) { OriginalSource = target, Source = element };
                    binding.RaiseCanExecute(element, args);
                    if (args.CanExecute || args.Handled) return args.CanExecute;
                }
            }

            return false;
        }

        public void Execute(object parameter) => Execute(parameter, GetDefaultTarget());

        public void Execute(object parameter, object target) {
            foreach (var element in Route(target)) {
                foreach (var binding in CommandManager.GetAllBindings(element)) {
                    if (!ReferenceEquals(binding.Command, this)) continue;
                    var args = new ExecutedRoutedEventArgs(this, parameter) { OriginalSource = target, Source = element };
                    binding.RaiseExecuted(element, args);
                    if (args.Handled) return;
                }
            }
        }
    }

    public class RoutedUICommand : RoutedCommand {
        public RoutedUICommand() { }

        public RoutedUICommand(string text, string name, Type ownerType) : base(name, ownerType) {
            Text = text;
        }

        public RoutedUICommand(string text, string name, Type ownerType, InputGestureCollection inputGestures) : base(name, ownerType, inputGestures) {
            Text = text;
        }

        public string Text { get; set; }
    }

    public static class ApplicationCommands {
        public static RoutedUICommand Copy { get; } = new RoutedUICommand("Copy", nameof(Copy), typeof(ApplicationCommands));
        public static RoutedUICommand Cut { get; } = new RoutedUICommand("Cut", nameof(Cut), typeof(ApplicationCommands));
        public static RoutedUICommand Paste { get; } = new RoutedUICommand("Paste", nameof(Paste), typeof(ApplicationCommands));
        public static RoutedUICommand Delete { get; } = new RoutedUICommand("Delete", nameof(Delete), typeof(ApplicationCommands));
        public static RoutedUICommand Close { get; } = new RoutedUICommand("Close", nameof(Close), typeof(ApplicationCommands));
        public static RoutedUICommand Open { get; } = new RoutedUICommand("Open", nameof(Open), typeof(ApplicationCommands));
        public static RoutedUICommand Save { get; } = new RoutedUICommand("Save", nameof(Save), typeof(ApplicationCommands));
        public static RoutedUICommand SaveAs { get; } = new RoutedUICommand("Save As", nameof(SaveAs), typeof(ApplicationCommands));
        public static RoutedUICommand New { get; } = new RoutedUICommand("New", nameof(New), typeof(ApplicationCommands));
        public static RoutedUICommand Undo { get; } = new RoutedUICommand("Undo", nameof(Undo), typeof(ApplicationCommands));
        public static RoutedUICommand Redo { get; } = new RoutedUICommand("Redo", nameof(Redo), typeof(ApplicationCommands));
        public static RoutedUICommand SelectAll { get; } = new RoutedUICommand("Select All", nameof(SelectAll), typeof(ApplicationCommands));
        public static RoutedUICommand Find { get; } = new RoutedUICommand("Find", nameof(Find), typeof(ApplicationCommands));
        public static RoutedUICommand Help { get; } = new RoutedUICommand("Help", nameof(Help), typeof(ApplicationCommands));
        public static RoutedUICommand Print { get; } = new RoutedUICommand("Print", nameof(Print), typeof(ApplicationCommands));
        public static RoutedUICommand Properties { get; } = new RoutedUICommand("Properties", nameof(Properties), typeof(ApplicationCommands));
        public static RoutedUICommand Stop { get; } = new RoutedUICommand("Stop", nameof(Stop), typeof(ApplicationCommands));
    }

    public static class NavigationCommands {
        public static RoutedUICommand BrowseBack { get; } = new RoutedUICommand("Back", nameof(BrowseBack), typeof(NavigationCommands));
        public static RoutedUICommand BrowseForward { get; } = new RoutedUICommand("Forward", nameof(BrowseForward), typeof(NavigationCommands));
        public static RoutedUICommand BrowseHome { get; } = new RoutedUICommand("Home", nameof(BrowseHome), typeof(NavigationCommands));
        public static RoutedUICommand BrowseStop { get; } = new RoutedUICommand("Stop", nameof(BrowseStop), typeof(NavigationCommands));
        public static RoutedUICommand Refresh { get; } = new RoutedUICommand("Refresh", nameof(Refresh), typeof(NavigationCommands));
        public static RoutedUICommand GoToPage { get; } = new RoutedUICommand("Go to page", nameof(GoToPage), typeof(NavigationCommands));
        public static RoutedUICommand NextPage { get; } = new RoutedUICommand("Next page", nameof(NextPage), typeof(NavigationCommands));
        public static RoutedUICommand PreviousPage { get; } = new RoutedUICommand("Previous page", nameof(PreviousPage), typeof(NavigationCommands));
        public static RoutedUICommand Zoom { get; } = new RoutedUICommand("Zoom", nameof(Zoom), typeof(NavigationCommands));
    }

    public static class ComponentCommands {
        public static RoutedUICommand MoveDown { get; } = new RoutedUICommand("Move down", nameof(MoveDown), typeof(ComponentCommands));
        public static RoutedUICommand MoveUp { get; } = new RoutedUICommand("Move up", nameof(MoveUp), typeof(ComponentCommands));
        public static RoutedUICommand ScrollPageDown { get; } = new RoutedUICommand("Scroll page down", nameof(ScrollPageDown), typeof(ComponentCommands));
        public static RoutedUICommand ScrollPageUp { get; } = new RoutedUICommand("Scroll page up", nameof(ScrollPageUp), typeof(ComponentCommands));
    }
}

namespace System.Windows {
    using System.Windows.Input;

    public static class SystemCommands {
        public static RoutedCommand CloseWindowCommand { get; } = new RoutedCommand(nameof(CloseWindowCommand), typeof(SystemCommands));
        public static RoutedCommand MaximizeWindowCommand { get; } = new RoutedCommand(nameof(MaximizeWindowCommand), typeof(SystemCommands));
        public static RoutedCommand MinimizeWindowCommand { get; } = new RoutedCommand(nameof(MinimizeWindowCommand), typeof(SystemCommands));
        public static RoutedCommand RestoreWindowCommand { get; } = new RoutedCommand(nameof(RestoreWindowCommand), typeof(SystemCommands));
        public static RoutedCommand ShowSystemMenuCommand { get; } = new RoutedCommand(nameof(ShowSystemMenuCommand), typeof(SystemCommands));

        public static void CloseWindow(Avalonia.Controls.Window window) => window.Close();
        public static void MaximizeWindow(Avalonia.Controls.Window window) => window.WindowState = Avalonia.Controls.WindowState.Maximized;
        public static void MinimizeWindow(Avalonia.Controls.Window window) => window.WindowState = Avalonia.Controls.WindowState.Minimized;
        public static void RestoreWindow(Avalonia.Controls.Window window) => window.WindowState = Avalonia.Controls.WindowState.Normal;
    }
}
