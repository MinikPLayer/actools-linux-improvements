using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Threading;

// ReSharper disable once CheckNamespace
namespace System.Windows {
    public enum Visibility {
        Visible,
        Hidden,
        Collapsed
    }

    public enum MessageBoxResult {
        None = 0,
        OK = 1,
        Cancel = 2,
        Yes = 6,
        No = 7
    }

    public enum MessageBoxButton {
        OK = 0,
        OKCancel = 1,
        YesNoCancel = 3,
        YesNo = 4
    }

    public enum MessageBoxImage {
        None = 0,
        Error = 16,
        Question = 32,
        Warning = 48,
        Information = 64
    }

    public enum ResizeMode {
        NoResize,
        CanMinimize,
        CanResize,
        CanResizeWithGrip
    }



    public static class FontWeights {
        public static FontWeight Thin => FontWeight.Thin;
        public static FontWeight ExtraLight => FontWeight.ExtraLight;
        public static FontWeight Light => FontWeight.Light;
        public static FontWeight Normal => FontWeight.Normal;
        public static FontWeight Regular => FontWeight.Normal;
        public static FontWeight Medium => FontWeight.Medium;
        public static FontWeight SemiBold => FontWeight.SemiBold;
        public static FontWeight Bold => FontWeight.Bold;
        public static FontWeight ExtraBold => FontWeight.ExtraBold;
        public static FontWeight Black => FontWeight.Black;
    }

    public static class FontStyles {
        public static FontStyle Normal => FontStyle.Normal;
        public static FontStyle Italic => FontStyle.Italic;
        public static FontStyle Oblique => FontStyle.Oblique;
    }

    public static class VisibilityExtension {
        public static bool ToBool(this Visibility v) => v == Visibility.Visible;
        public static Visibility ToVisibility(this bool v) => v ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// WPF-style members for Avalonia controls (this way, most of code written for WPF compiles without changes).
    /// </summary>
    public static class WpfControlExtension {
        extension(Visual v) {
            public Visibility Visibility {
                get => v.IsVisible ? Visibility.Visible : Visibility.Collapsed;
                set => v.IsVisible = value == Visibility.Visible;
            }
        }

        extension(Avalonia.Controls.Control c) {
            public double ActualWidth => c.Bounds.Width;
            public double ActualHeight => c.Bounds.Height;

            public bool CaptureMouse() => false;
            public void ReleaseMouseCapture() { }
        }

        extension(Window w) {
            public double Left {
                get => w.Position.X / w.RenderScaling;
                set => w.Position = new PixelPoint((int)(value * w.RenderScaling), w.Position.Y);
            }

            public double Top {
                get => w.Position.Y / w.RenderScaling;
                set => w.Position = new PixelPoint(w.Position.X, (int)(value * w.RenderScaling));
            }

            public void DragMove() { }

            public ResizeMode ResizeMode {
                get => !w.CanResize ? ResizeMode.NoResize : ResizeMode.CanResize;
                set => w.CanResize = value == ResizeMode.CanResize || value == ResizeMode.CanResizeWithGrip;
            }
        }

        extension(Application a) {
            public Dispatcher Dispatcher => Dispatcher.UIThread;
        }

        extension(Dispatcher) {
            public static Dispatcher CurrentDispatcher => Dispatcher.UIThread;
        }

        public static void BeginInvoke(this Dispatcher d, Action action) {
            d.Post(action, DispatcherPriority.Normal);
        }

        public static void BeginInvoke(this Dispatcher d, Action action, DispatcherPriority priority) {
            d.Post(action, priority);
        }

        public static void BeginInvoke(this Dispatcher d, DispatcherPriority priority, Action action) {
            d.Post(action, priority);
        }

        public static void Invoke(this Dispatcher d, DispatcherPriority priority, Action action) {
            d.Invoke(action, priority);
        }

        public static T Invoke<T>(this Dispatcher d, DispatcherPriority priority, Func<T> action) {
            return d.Invoke(action, priority);
        }

        public static void Freeze(this object o) { }

        public static bool IsFrozen(this object o) => true;
    }
}

namespace System.Windows.Data {
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ValueConversionAttribute : Attribute {
        public ValueConversionAttribute(Type sourceType, Type targetType) { }

        public Type ParameterType { get; set; }
    }

    /// <summary>
    /// WPF-style multi-value converter (with an array of values), which is also the one Avalonia expects.
    /// </summary>
    public interface IMultiValueConverter : Avalonia.Data.Converters.IMultiValueConverter {
        object Convert(object[] values, Type targetType, object parameter, CultureInfo culture);

        object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();

        object Avalonia.Data.Converters.IMultiValueConverter.Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture) {
            return Convert(values.ToArray(), targetType, parameter, culture);
        }
    }
}

namespace System.Windows.Markup {
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ContentPropertyAttribute : Attribute {
        public ContentPropertyAttribute() { }
        public ContentPropertyAttribute(string name) { }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
    public sealed class ConstructorArgumentAttribute : Attribute {
        public ConstructorArgumentAttribute(string argumentName) { }
    }
}

namespace System.Windows.Controls { }
namespace System.Windows.Media { }
namespace System.Windows.Documents { }
namespace System.Windows.Shapes { }
namespace System.Windows.Threading { }
namespace System.Windows.Data { }
namespace System.Windows.Navigation { }
namespace System.Windows.Controls.Primitives { }
namespace System.Windows.Media.Effects { }
namespace System.Windows.Media.Imaging { }
namespace System.Windows.Media.Animation { }
namespace System.Windows.Interop { }
namespace System.Windows.Interactivity { }

namespace System.Windows {
    /// <summary>
    /// Template-related things from WPF, like this.GetTemplateChild().
    /// </summary>
    public static class TemplateExtension {
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Avalonia.Controls.Primitives.TemplatedControl, Avalonia.Controls.INameScope> Scopes
                = new System.Runtime.CompilerServices.ConditionalWeakTable<Avalonia.Controls.Primitives.TemplatedControl, Avalonia.Controls.INameScope>();

        public static void RememberTemplateScope(this Avalonia.Controls.Primitives.TemplatedControl c, Avalonia.Controls.Primitives.TemplateAppliedEventArgs e) {
            Scopes.AddOrUpdate(c, e.NameScope);
        }

        public static object GetTemplateChild(this Avalonia.Controls.Primitives.TemplatedControl c, string name) {
            return Scopes.TryGetValue(c, out var scope) ? scope.Find(name) : null;
        }
    }
}

namespace System.Windows {
    using System.Collections.Generic;
    using System.Linq;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Data;
    using Avalonia.Reactive;

    public static class ObservableExtension {
        public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext) {
            return source.Subscribe(new AnonymousObserver<T>(onNext));
        }
    }

    /// <summary>
    /// Simplified WeakEventManager (listeners are kept alive by the source; fine for our purposes).
    /// </summary>
    public static class WeakEventManager<TSource, TEventArgs> where TEventArgs : EventArgs {
        public static void AddHandler(TSource source, string eventName, EventHandler<TEventArgs> handler) {
            var e = typeof(TSource).GetEvent(eventName) ?? source?.GetType().GetEvent(eventName);
            e?.AddEventHandler(source, ConvertHandler(e, handler));
        }

        public static void RemoveHandler(TSource source, string eventName, EventHandler<TEventArgs> handler) {
            var e = typeof(TSource).GetEvent(eventName) ?? source?.GetType().GetEvent(eventName);
            e?.RemoveEventHandler(source, ConvertHandler(e, handler));
        }

        private static Delegate ConvertHandler(System.Reflection.EventInfo e, EventHandler<TEventArgs> handler) {
            return e.EventHandlerType == typeof(EventHandler<TEventArgs>) ? handler : Delegate.CreateDelegate(e.EventHandlerType, handler.Target, handler.Method);
        }
    }

    public class UIPropertyMetadata : FrameworkPropertyMetadata {
        public UIPropertyMetadata() { }
        public UIPropertyMetadata(object defaultValue) : base(defaultValue) { }
        public UIPropertyMetadata(PropertyChangedCallback callback) : base(callback) { }
        public UIPropertyMetadata(object defaultValue, PropertyChangedCallback callback) : base(defaultValue, callback) { }
        public UIPropertyMetadata(object defaultValue, PropertyChangedCallback callback, CoerceValueCallback coerce)
                : base(defaultValue, FrameworkPropertyMetadataOptions.None, callback, coerce) { }
    }

    public class PropertyPath {
        public PropertyPath(string path) {
            Path = path;
        }

        public string Path { get; }

        public static implicit operator string(PropertyPath p) => p?.Path;

        public override string ToString() => Path;
    }


    /// <summary>
    /// Members of WPF’s Application.
    /// </summary>
    public static class ApplicationExtension {
        extension(Application a) {
            public IReadOnlyList<Window> Windows => (a.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows ?? (IReadOnlyList<Window>)Array.Empty<Window>();

            public Window MainWindow {
                get => (a.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                set {
                    if (a.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime l) l.MainWindow = value;
                }
            }

            public void Shutdown() => (a.ApplicationLifetime as IControlledApplicationLifetime)?.Shutdown();

            public void Shutdown(int exitCode) => (a.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown(exitCode);

            public object FindResource(object key) {
                return a.TryGetResource(key, a.ActualThemeVariant, out var value) ? value : null;
            }

            public object TryFindResource(object key) {
                return a.TryGetResource(key, a.ActualThemeVariant, out var value) ? value : null;
            }
        }

        public static object TryFindResource(this Avalonia.Controls.Control c, object key) {
            return c.TryFindResource(key, out var value) ? value : null;
        }

        public static object FindResource(this Avalonia.Controls.Control c, object key) {
            return c.TryFindResource(key, out var value) ? value : throw new ResourceNotFoundException(key);
        }

        public static object FindName(this Avalonia.Controls.Control c, string name) {
            return c.FindNameScope()?.Find(name);
        }

        public static void SetBinding(this AvaloniaObject o, AvaloniaProperty property, IBinding binding) {
            o.Bind(property, binding);
        }
    }

    public class ResourceNotFoundException : Exception {
        public ResourceNotFoundException(object key) : base("Resource not found: " + key) { }
    }
}

namespace System.Windows {
    using System.Threading;
    using System.Threading.Tasks;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.Primitives;
    using Avalonia.Input;
    using Avalonia.Input.Platform;
    using Avalonia.Threading;
    using Avalonia.VisualTree;

    public static class SyncHelper {
        /// <summary>
        /// Waits for a task without blocking UI (runs a nested dispatcher loop on the UI thread).
        /// </summary>
        public static T Wait<T>(Task<T> task) {
            Wait((Task)task);
            return task.GetAwaiter().GetResult();
        }

        public static void Wait(Task task) {
            if (task.IsCompleted) {
                task.GetAwaiter().GetResult();
                return;
            }

            if (Dispatcher.UIThread.CheckAccess()) {
                var frame = new DispatcherFrame();
                task.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);
                Dispatcher.UIThread.PushFrame(frame);
            }

            task.GetAwaiter().GetResult();
        }
    }

    public static class Clipboard {
        private static IClipboard Get() {
            var window = Application.Current?.MainWindow ?? Application.Current?.Windows.FirstOrDefault();
            return window?.Clipboard;
        }

        public static bool ContainsText() => !string.IsNullOrEmpty(GetText());

        public static string GetText() {
            var clipboard = Get();
            return clipboard == null ? null : SyncHelper.Wait(clipboard.GetTextAsync());
        }

        public static void SetText(string text) {
            var clipboard = Get();
            if (clipboard != null) SyncHelper.Wait(clipboard.SetTextAsync(text));
        }

        public static void Clear() {
            var clipboard = Get();
            if (clipboard != null) SyncHelper.Wait(clipboard.ClearAsync());
        }

        public static IDataObject GetDataObject() {
            var clipboard = Get();
            return null; // TODO: not available in Avalonia
        }

        public static void SetDataObject(IDataObject data) {
            var clipboard = Get();
            if (clipboard != null) SyncHelper.Wait(clipboard.SetDataObjectAsync(data));
        }

        public static void SetDataObject(object data, bool copy) {
            SetDataObject(data as IDataObject);
        }
    }

    public static class VisualTreeHelper {
        public static Visual GetParent(AvaloniaObject o) => (o as Visual)?.GetVisualParent();

        public static int GetChildrenCount(AvaloniaObject o) => (o as Visual)?.GetVisualChildren().Count() ?? 0;

        public static Visual GetChild(AvaloniaObject o, int index) => ((Visual)o).GetVisualChildren().ElementAt(index);
    }

    public static class ControlExtension2 {
        extension(Window) {
            public static Window GetWindow(AvaloniaObject o) => (o as Visual)?.GetVisualRoot() as Window;
        }

        extension(ScrollViewer v) {
            public double VerticalOffset => v.Offset.Y;
            public double HorizontalOffset => v.Offset.X;
            public double ScrollableHeight => Math.Max(0d, v.Extent.Height - v.Viewport.Height);
            public double ScrollableWidth => Math.Max(0d, v.Extent.Width - v.Viewport.Width);
            public double ViewportHeight => v.Viewport.Height;
            public double ViewportWidth => v.Viewport.Width;
            public double ExtentHeight => v.Extent.Height;
            public double ExtentWidth => v.Extent.Width;
            public void ScrollToVerticalOffset(double offset) => v.Offset = new Vector(v.Offset.X, offset);
            public void ScrollToHorizontalOffset(double offset) => v.Offset = new Vector(offset, v.Offset.Y);
            public void ScrollToTop() => v.ScrollToHome();
            public void ScrollToBottom() => v.ScrollToEnd();
        }

        extension(Popup p) {
            public bool StaysOpen {
                get => !p.IsLightDismissEnabled;
                set => p.IsLightDismissEnabled = !value;
            }
        }

        extension(ItemCollection c) {
            public bool IsEmpty => c.Count == 0;
        }

        extension(Size) {
            public static Size Empty => new Size(double.NegativeInfinity, double.NegativeInfinity);
        }

        extension(Size s) {
            public bool IsEmpty => double.IsNegativeInfinity(s.Width);
        }

        extension(Avalonia.Controls.Control c) {
            public void SetResourceReference(AvaloniaProperty property, object key) {
                c.Bind(property, c.GetResourceObservable(key));
            }

            public bool Focus(FocusNavigationDirection direction) => c.MoveFocus(direction);

            public bool MoveFocus(FocusNavigationDirection direction) {
                var next = KeyboardNavigationHandler.GetNext(c, direction);
                if (next == null) return false;
                next.Focus();
                return true;
            }
        }

        extension(DispatcherOperation o) {
            public Task Task => o.GetTask();
        }

        extension<T>(DispatcherOperation<T> o) {
            public Task<T> Task => o.GetTask();
        }

        extension(Dispatcher d) {
            public bool IsOnThread => d.CheckAccess();
        }
    }
}

namespace System.Windows.Input {
    public static class MouseExtension {
        extension(Mouse) {
            public static IInputElement DirectlyOver => Mouse.LastOver;
        }
    }
}

namespace System.Windows {
    using Avalonia.Styling;

    public static class StyleHelper {
        /// <summary>
        /// Emulates `element.Style = (Style)FindResource(key)`: control themes are applied as themes, plain styles as classes.
        /// </summary>
        public static void SetStyle(this Avalonia.Controls.Control c, string key) {
            if (key == null) {
                c.Theme = null;
                return;
            }

            if (c.TryFindResource(key, out var value) && value is ControlTheme theme) {
                c.Theme = theme;
            } else {
                c.Classes.Add(key.Replace(' ', '_'));
            }
        }
    }
}

namespace System.Windows {
    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Styling;

    public static class ControlExtension3 {
        extension(Avalonia.Controls.Control c) {
            /// <summary>
            /// WPF’s Style property: control themes are applied as themes.
            /// </summary>
            public StyleBase Style {
                get => c.Theme;
                set {
                    if (value is ControlTheme theme) c.Theme = theme;
                    else if (value == null) c.Theme = null;
                }
            }

            public object ToolTip {
                get => Avalonia.Controls.ToolTip.GetTip(c);
                set => Avalonia.Controls.ToolTip.SetTip(c, value);
            }
        }

        extension(MenuItem m) {
            public string InputGestureText {
                get => m.InputGesture?.ToString();
                set {
                    try {
                        m.InputGesture = string.IsNullOrEmpty(value) ? null : KeyGesture.Parse(value.Replace("Ctrl", "Control"));
                    } catch {
                        // just a label
                    }
                }
            }
        }
    }
}

namespace System.Windows.Controls {
    /// <summary>
    /// WPF-style template selector: SelectTemplate() chooses a template for an item.
    /// </summary>
    public abstract class DataTemplateSelector : Avalonia.Controls.Templates.IDataTemplate {
        public virtual Avalonia.Markup.Xaml.Templates.DataTemplate SelectTemplate(object item, Avalonia.AvaloniaObject container) => null;

        public bool Match(object data) => true;

        public Avalonia.Controls.Control Build(object param) {
            return SelectTemplate(param, null)?.Build(param);
        }
    }
}

namespace System.Windows.Interop {
    public class WindowInteropHelper {
        public WindowInteropHelper(Avalonia.Controls.Window window) {
            Handle = window?.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        }

        public IntPtr Handle { get; }
    }
}

namespace System.Windows {
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Avalonia.Markup.Xaml;

    /// <summary>
    /// Replacement for WPF’s URI-based loading (pack URIs like /Assembly;component/Path/Page.xaml).
    /// Pages are classes in Avalonia, so URIs are mapped to types by convention: folders are namespaces, file name is the class name.
    /// </summary>
    public static class ComponentLoader {
        private static readonly Dictionary<string, Type> Cache = new Dictionary<string, Type>();

        public static Uri ToAvaloniaUri(Uri uri) {
            var s = uri.OriginalString;
            var m = System.Text.RegularExpressions.Regex.Match(s, @"^(?:pack://application:,,,)?/?([^;/]+);component/(.+)$");
            if (m.Success) {
                var path = m.Groups[2].Value;
                if (path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)) path = path.Substring(0, path.Length - 5) + ".axaml";
                return new Uri($"avares://{m.Groups[1].Value}/{path}");
            }

            if (s.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)) {
                return new Uri(s.Substring(0, s.Length - 5) + ".axaml", uri.IsAbsoluteUri ? UriKind.Absolute : UriKind.RelativeOrAbsolute);
            }

            return uri;
        }

        public static Type FindType(Uri uri) {
            var s = uri.OriginalString;
            var m = System.Text.RegularExpressions.Regex.Match(s, @"^(?:pack://application:,,,)?/?(?:([^;/]+);component/)?(.+?)(?:\.a?xaml)?$");
            if (!m.Success) return null;

            var assemblyName = m.Groups[1].Success ? m.Groups[1].Value : null;
            var relative = m.Groups[2].Value.Replace('/', '.');
            var key = (assemblyName ?? "") + "|" + relative;
            lock (Cache) {
                if (Cache.TryGetValue(key, out var cached)) return cached;
            }

            Type found = null;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => assemblyName == null || x.GetName().Name == assemblyName);
            foreach (var assembly in assemblies) {
                found = assembly.GetType(assembly.GetName().Name + "." + relative);
                if (found == null) {
                    try {
                        found = assembly.GetTypes().FirstOrDefault(x => x.FullName != null && x.FullName.EndsWith("." + relative, StringComparison.Ordinal)
                                && typeof(Avalonia.Controls.Control).IsAssignableFrom(x));
                    } catch (ReflectionTypeLoadException) { }
                }

                if (found != null) break;
            }

            lock (Cache) {
                Cache[key] = found;
            }

            return found;
        }

        public static object Load(Uri uri) {
            var type = FindType(uri);
            if (type == null) throw new InvalidOperationException("Can’t find a type for " + uri);
            return Activator.CreateInstance(type);
        }
    }

    public static class ApplicationLoadExtension {
        extension(Application) {
            public static object LoadComponent(Uri uri) => ComponentLoader.Load(uri);
        }
    }

    public static class ResourceDictionaryExtension {
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ResourceDictionary, Uri> Sources
                = new System.Runtime.CompilerServices.ConditionalWeakTable<ResourceDictionary, Uri>();

        extension(ResourceDictionary d) {
            public Uri Source {
                get => Sources.TryGetValue(d, out var uri) ? uri : null;
                set {
                    Sources.AddOrUpdate(d, value);
                    LoadInto(d, value);
                }
            }

            public bool Contains(object key) => d.ContainsKey(key);
        }

        private static void LoadInto(ResourceDictionary target, Uri uri) {
            var loaded = AvaloniaXamlLoader.Load(ComponentLoader.ToAvaloniaUri(uri)) as ResourceDictionary
                    ?? throw new InvalidOperationException("Not a resource dictionary: " + uri);
            foreach (var pair in loaded) {
                target[pair.Key] = pair.Value;
            }

            foreach (var merged in loaded.MergedDictionaries.ToList()) {
                loaded.MergedDictionaries.Remove(merged);
                target.MergedDictionaries.Add(merged);
            }
        }
    }
}

namespace System.Windows {
    using Avalonia.Controls;
    using Avalonia.Input;

    public static class MiscExtension {
        extension(TextBox t) {
            public int SelectionLength {
                get => Math.Abs(t.SelectionEnd - t.SelectionStart);
                set => t.SelectionEnd = t.SelectionStart + value;
            }

            public string Password {
                get => t.Text;
                set => t.Text = value;
            }
        }

        extension(IDataObject d) {
            public object GetData(string format) => d.Get(format);
            public bool GetDataPresent(string format) => d.Contains(format);
        }

        extension(DragEventArgs e) {
            public DragDropEffects Effects {
                get => e.DragEffects;
                set => e.DragEffects = value;
            }
        }
    }
}

namespace System.Windows {
    using Avalonia.Controls;

    public static class DataGridColumnExtension {
        extension(DataGridColumn c) {
            public Visibility Visibility {
                get => c.IsVisible ? Visibility.Visible : Visibility.Collapsed;
                set => c.IsVisible = value == Visibility.Visible;
            }
        }
    }
}

namespace System.Windows {
    /// <summary>
    /// A few WPF system parameters used in styles (values aren’t meaningful with Avalonia, window chrome is drawn by the theme).
    /// </summary>
    public static class SystemParameters {
        public static Thickness WindowNonClientFrameThickness => new Thickness(0);
        public static Thickness WindowResizeBorderThickness => new Thickness(0);
        public static double VerticalScrollBarWidth => 17d;
        public static double HorizontalScrollBarHeight => 17d;
        public static double VerticalScrollBarButtonHeight => 17d;
        public static double HorizontalScrollBarButtonWidth => 17d;
        public static double CaptionHeight => 30d;
        public static double PrimaryScreenWidth => 1920d;
        public static double PrimaryScreenHeight => 1080d;
    }
}
