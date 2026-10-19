using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Windows.Controls;
using FirstFloor.ModernUI.Windows.Media;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows.Attached {
    public static class Draggable {
        public const string SourceFormat = "Data-Source";

        private static readonly DraggableEvents Events = new DraggableEvents();

        private class DraggableEvents {
            public event EventHandler<DraggableMovedEventArgs> DragStartedInner;
            public event EventHandler<DraggableMovedEventArgs> DragEndedInner;

            internal void RaiseDragStarted(string format, object obj) {
                DragStartedInner?.Invoke(this, new DraggableMovedEventArgs(format, obj));
            }

            internal void RaiseDragEnded(string format, object obj) {
                DragEndedInner?.Invoke(this, new DraggableMovedEventArgs(format, obj));
            }
        }

        /// <summary>
        /// Weak event.
        /// </summary>
        public static event EventHandler<DraggableMovedEventArgs> DragStarted {
            add => WeakEventManager<DraggableEvents, DraggableMovedEventArgs>.AddHandler(Events, nameof(Events.DragStartedInner), value);
            remove => WeakEventManager<DraggableEvents, DraggableMovedEventArgs>.RemoveHandler(Events, nameof(Events.DragStartedInner), value);
        }

        /// <summary>
        /// Weak event.
        /// </summary>
        public static event EventHandler<DraggableMovedEventArgs> DragEnded {
            add => WeakEventManager<DraggableEvents, DraggableMovedEventArgs>.AddHandler(Events, nameof(Events.DragEndedInner), value);
            remove => WeakEventManager<DraggableEvents, DraggableMovedEventArgs>.RemoveHandler(Events, nameof(Events.DragEndedInner), value);
        }

        public static bool GetForceDisabled(DependencyObject obj) {
            return obj.GetValue(ForceDisabledProperty) as bool? == true;
        }

        public static void SetForceDisabled(DependencyObject obj, bool value) {
            obj.SetValue(ForceDisabledProperty, value);
        }

        public static readonly AttachedProperty<bool> ForceDisabledProperty = DP.RegisterAttached<bool>("ForceDisabled",
                typeof(Draggable), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static bool GetEnabled(FrameworkElement obj) {
            return obj.GetValue(EnabledProperty) as bool? == true;
        }

        public static void SetEnabled(FrameworkElement obj, bool value) {
            obj.SetValue(EnabledProperty, value);
        }

        public static readonly AttachedProperty<bool> EnabledProperty = DP.RegisterAttached<bool>("Enabled", typeof(Draggable),
                new PropertyMetadata(false, OnEnabledChanged));

        public static object GetData(DependencyObject obj) {
            return obj.GetValue(DataProperty);
        }

        public static void SetData(DependencyObject obj, object value) {
            obj.SetValue(DataProperty, value);
        }

        public static readonly AttachedProperty<object> DataProperty = DP.RegisterAttached<object>("Data",
                typeof(Draggable), new PropertyMetadata(null, OnEnabledChanged));

        public static bool GetKeepSelection(DependencyObject obj) {
            return obj.GetValue(KeepSelectionProperty) as bool? == true;
        }

        public static void SetKeepSelection(DependencyObject obj, bool value) {
            obj.SetValue(KeepSelectionProperty, value);
        }

        public static readonly AttachedProperty<bool> KeepSelectionProperty = DP.RegisterAttached<bool>("KeepSelection",
                typeof(Draggable), new PropertyMetadata(false));

        public static bool GetIsDestinationHighlighted(DependencyObject obj) {
            return obj.GetValue(IsDestinationHighlightedProperty) as bool? == true;
        }

        public static void SetIsDestinationHighlighted(DependencyObject obj, bool value) {
            obj.SetValue(IsDestinationHighlightedProperty, value);
        }

        public static readonly AttachedProperty<bool> IsDestinationHighlightedProperty = DP.RegisterAttached<bool>("IsDestinationHighlighted",
                typeof(Draggable), new FrameworkPropertyMetadata(false));

        private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var element = (FrameworkElement)d;

            if (e.Property == DataProperty && (GetEnabled(element) || e.OldValue != null)) return;
            if (e.Property == EnabledProperty && (GetData(element) != null || e.OldValue as bool? != false)) return;

            EventHandler<PointerEventArgs> handler;
            switch (element) {
                case DataGrid _:
                    handler = OnDataGridMouseMove;
                    break;
                case ListBox _:
                    handler = OnListBoxMouseMove;
                    break;
                case ItemsControl _:
                    handler = OnItemsControlMouseMove;
                    break;
                default:
                    handler = OnElementMouseMove;
                    break;
            }

            if (e.Property == DataProperty && e.NewValue != null ||
                    e.Property == EnabledProperty && e.NewValue as bool? != false) {
                element.AddPreviewMouseDown(OnElementMouseDown);
                element.AddPreviewMouseMove(handler);
                DragDrop.SetAllowDrop(element, true);
            } else {
                element.RemovePreviewMouseDown(OnElementMouseDown);
                element.RemovePreviewMouseMove(handler);
                DragDrop.SetAllowDrop(element, false);
            }
        }

        private static bool _dragging;
        private static object _previous;
        private static Point _startingPoint;

        private static void OnElementMouseDown(object sender, MouseButtonEventArgs e) {
            if (!e.Handled) {
                if (sender is FrameworkElement element && !IgnoreSpecialControls(sender, e)) {
                    if (GetForceDisabled(element)) return;

                    _previous = element;
                    _startingPoint = VisualExtension.GetMousePosition();
                    return;
                }
            }

            _previous = null;
        }

        [CanBeNull]
        private static Avalonia.Controls.Control PrepareItem(FrameworkElement element) {
            {
                var item = element as Avalonia.Controls.ListBoxItem;
                var parent = item?.GetParent<ListBox>();
                if (parent != null) {
                    if (!GetKeepSelection(parent)) {
                        parent.SelectedItem = item;
                    }
                    return parent;
                }
            }

            {
                var row = element as Avalonia.Controls.DataGridRow;
                var parent = row?.GetParent<DataGrid>();
                if (parent != null) {
                    if (!GetKeepSelection(parent)) {
                        parent.SelectedItem = row.DataContext;
                    }
                    return parent;
                }
            }

            return element?.GetParent<ItemsControl>();
        }

        private static System.Collections.Generic.IEnumerable<Window> GetWindows() {
            return (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?
                    .Windows ?? (System.Collections.Generic.IEnumerable<Window>)new Window[0];
        }

        private static void MarkDestinations(string format) {
            foreach (var destination in GetWindows()
                    .SelectMany(VisualTreeHelperEx.FindVisualChildren<FrameworkElement>)
                    .Where(x => string.Equals(GetDestination(x)?.ToString(), format, StringComparison.Ordinal))) {
                SetIsDestinationHighlighted(destination, true);
            }
        }

        private static void UnmarkDestinations() {
            foreach (var destination in GetWindows()
                    .SelectMany(VisualTreeHelperEx.FindVisualChildren<FrameworkElement>)
                    .Where(GetIsDestinationHighlighted)) {
                SetIsDestinationHighlighted(destination, false);
            }
        }

        private static readonly List<Type> DraggableTypes = new List<Type>(10);

        public static void RegisterDraggable(Type type) {
            if (!DraggableTypes.Contains(type)) {
                DraggableTypes.Add(type);
            }
        }

        public static void RegisterDraggable<T>() {
            DraggableTypes.Add(typeof(T));
        }

        [CanBeNull]
        private static string GetFormat([NotNull] object draggable) {
            var format = (draggable as IDraggable)?.DraggableFormat;
            if (format != null) {
                return format;
            }

            var type = draggable.GetType();
            var registered = DraggableTypes.FirstOrDefault(x => x == type || type.IsSubclassOf(x));
            if (registered != null) {
                return registered.ToString();
            }

            return null;
        }

        private static bool MoveDraggable([CanBeNull] FrameworkElement element, [CanBeNull] object draggable, [NotNull] PointerEventArgs e) {
            if (element == null || draggable == null) return false;

            var format = GetFormat(draggable);
            if (format == null) return false;

            _dragging = true;

            var data = new DataObject();
            data.Set(format, draggable);
            MarkDestinations(format);
            Events.RaiseDragStarted(format, draggable);

            var source = PrepareItem(element);
            if (source != null) {
                data.Set(SourceFormat, source);
            }

            // PORT: no drag preview for now (WPF version showed a semi-transparent copy of the element)
            DoDragAsync(e, data, (draggable as IDraggableCloneable)?.CanBeCloned == true ?
                    DragDropEffects.Copy | DragDropEffects.Move : DragDropEffects.Move, format, draggable);
            return true;
        }

        private static async void DoDragAsync(PointerEventArgs e, DataObject data, DragDropEffects effects, string format, object draggable) {
            try {
                await DragDrop.DoDragDrop(e, data, effects);
            } catch (Exception ex) {
                Logging.Warning(ex);
            } finally {
                _dragging = false;
                UnmarkDestinations();
                Events.RaiseDragEnded(format, draggable);
            }
        }

        private static bool MoveBasic(FrameworkElement element, PointerEventArgs e) {
            return element != null && MoveDraggable(element, GetData(element) ?? element.DataContext, e);
        }

        private static bool MoveFromListBox(FrameworkElement element, PointerEventArgs e) {
            return MoveBasic((element as ListBox)?.GetFromPoint<Avalonia.Controls.ListBoxItem>(e.GetPosition(element)), e);
        }

        private static bool MoveFromItemsControl(FrameworkElement element, PointerEventArgs e) {
            if (!(element is ItemsControl items)) return false;
            var item = items.GetFromPoint<FrameworkElement>(e.GetPosition(element))?
                    .GetParents()
                    .OfType<FrameworkElement>()
                    .FirstOrDefault(x => items.ItemsSource?.OfType<object>().Contains(x.DataContext) == true ||
                            ReferenceEquals(x.TemplatedParent, element));
            return MoveBasic(item, e);
        }

        private static bool MoveFromDataGrid(FrameworkElement element, PointerEventArgs e) {
            var row = (element as DataGrid)?.GetFromPoint<Avalonia.Controls.DataGridRow>(e.GetPosition(element));
            return row != null && MoveDraggable(row, row.DataContext, e);
        }

        private static bool IsIgnored(DependencyObject o) {
            return (o as UIElement)?.IsEnabled == true &&
                    (o is TextBox || o is Thumb || o is ColorPicker || o is Slider);
        }

        private static bool IgnoreSpecialControls(object sender, MouseEventArgs e) {
            var reference = sender as UIElement;
            if (!(reference?.InputHitTest(e.GetPosition(reference)) is DependencyObject element) || IsIgnored(element) ||
                    reference.FindVisualChildren<Avalonia.Controls.Primitives.Popup>().Any(x => x.IsOpen)) return true;
            return element.GetParents().TakeWhile(parent => !ReferenceEquals(parent, sender)).Any(IsIgnored);
        }

        private static bool IsDragging(object sender, MouseEventArgs e) {
            if (_dragging || _previous != sender || !e.GetCurrentPoint(sender as Visual).Properties.IsLeftButtonPressed ||
                    VisualExtension.GetMousePosition().DistanceTo(_startingPoint) < 3d) return false;
            if (IgnoreSpecialControls(sender, e)) {
                _previous = null;
                return false;
            }

            return true;
        }

        private static void OnElementMouseMove(object sender, MouseEventArgs e) {
            e.Handled = e.Handled || IsDragging(sender, e) && MoveBasic((FrameworkElement)sender, e);
        }

        private static void OnListBoxMouseMove(object sender, MouseEventArgs e) {
            e.Handled = e.Handled || IsDragging(sender, e) && MoveFromListBox((FrameworkElement)sender, e);
        }

        private static void OnItemsControlMouseMove(object sender, MouseEventArgs e) {
            e.Handled = e.Handled || IsDragging(sender, e) && MoveFromItemsControl((FrameworkElement)sender, e);
        }

        private static void OnDataGridMouseMove(object sender, MouseEventArgs e) {
            e.Handled = e.Handled || IsDragging(sender, e) && MoveFromDataGrid((FrameworkElement)sender, e);
        }

        public static object GetDestination(DependencyObject obj) {
            return obj.GetValue(DestinationProperty);
        }

        public static void SetDestination(DependencyObject obj, object value) {
            obj.SetValue(DestinationProperty, value);
        }

        public static readonly AttachedProperty<object> DestinationProperty = DP.RegisterAttached<object>("Destination",
                typeof(Draggable), new UIPropertyMetadata(OnDestinationChanged));

        private static void OnDestinationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is ItemsControl element)) return;
            if (e.NewValue != null) {
                var type = e.NewValue as Type;
                if (type != null) {
                    RegisterDraggable(type);
                }

                element.AddDrop(OnDestinationDrop);
                DragDrop.SetAllowDrop(element, true);
            } else {
                element.RemoveDrop(OnDestinationDrop);
                DragDrop.SetAllowDrop(element, false);
            }
        }

        public static IDraggableDestinationConverter GetDestinationConverter(DependencyObject obj) {
            return (IDraggableDestinationConverter)obj.GetValue(DestinationConverterProperty);
        }

        public static void SetDestinationConverter(DependencyObject obj, IDraggableDestinationConverter value) {
            obj.SetValue(DestinationConverterProperty, value);
        }

        public static readonly AttachedProperty<IDraggableDestinationConverter> DestinationConverterProperty = DP.RegisterAttached<IDraggableDestinationConverter>("DestinationConverter", typeof(Draggable), new UIPropertyMetadata(OnDestinationConverterChanged));

        private static void OnDestinationConverterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is ItemsControl element) || !(e.NewValue is IDraggableDestinationConverter)) return;
            var newValue = (IDraggableDestinationConverter)e.NewValue;
            if (newValue != null) {
                element.AddDrop(OnDestinationConverterDrop);
                DragDrop.SetAllowDrop(element, true);
            } else {
                element.RemoveDrop(OnDestinationConverterDrop);
                DragDrop.SetAllowDrop(element, false);
            }
        }

        private static void OnDestinationDrop(object sender, DragEventArgs e) {
            var destination = (ItemsControl)sender;
            var format = GetDestination(destination);
            var item = e.Data.Get(format?.ToString());
            var source = e.Data.Get(SourceFormat) as Avalonia.Controls.Control;
            InnerDrop(source, destination, item, e);
        }

        private static void OnDestinationConverterDrop(object sender, DragEventArgs e) {
            var destination = (ItemsControl)sender;
            var converter = GetDestinationConverter(destination);
            var item = converter?.Convert(e.Data);
            var source = e.Data.Get(SourceFormat) as Avalonia.Controls.Control;
            InnerDrop(source, destination, item, e);
        }

        [CanBeNull]
        private static IEnumerable GetActualList([CanBeNull] Avalonia.Controls.Control itemsControl) {
            var list = (itemsControl as ItemsControl)?.ItemsSource ?? (itemsControl as DataGrid)?.ItemsSource;
            if (list is ListCollectionView view) {
                list = view.SourceCollection;
            }

            return list;
        }

        private static MethodInfo GetListMethod(IList list, Type type, bool add) {
            return (add
                    ? from x in list.GetType().GetMethods()
                        where x.Name == "Add"
                        let p = x.GetParameters()
                        where p.Length == 1 && (p[0].ParameterType == type || type.IsSubclassOf(p[0].ParameterType))
                        select x
                    : from x in list.GetType().GetMethods()
                        where x.Name == "Insert"
                        let p = x.GetParameters()
                        where p.Length == 2 && (p[1].ParameterType == type || type.IsSubclassOf(p[1].ParameterType))
                        select x).FirstOrDefault();
        }

        public static bool IsCopyAction(this DragEventArgs e) {
            return e.KeyModifiers.HasFlag(KeyModifiers.Control);
        }

        public static bool IsSpecificAction(this DragEventArgs e) {
            return e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        }

        [ContractAnnotation("item:null => null; item:notnull => notnull")]
        public static object GetDraggedItem([NotNull] this DragEventArgs e, object item) {
            return e.IsCopyAction() && (item as IDraggableCloneable)?.CanBeCloned == true ?
                    ((IDraggableCloneable)item).Clone() : item;
        }

        private static void InnerDrop([CanBeNull] Avalonia.Controls.Control source, [NotNull] ItemsControl destination, [CanBeNull] object item, DragEventArgs e) {
            if (item == null) {
                e.DragEffects = DragDropEffects.None;
                return;
            }

            var newIndex = destination.GetMouseItemIndex();
            var type = item.GetType();

            try {
                var destinationList = GetActualList(destination) as IList;
                var sourceList = GetActualList(source) as IList;

                if (destinationList == null) {
                    Logging.Warning("Can’t find target: " + destination.ItemsSource);
                    e.DragEffects = DragDropEffects.None;
                    return;
                }

                if (ReferenceEquals(sourceList, destinationList) && sourceList.IndexOf(item) == newIndex) {
                    e.DragEffects = DragDropEffects.None;
                    return;
                }

                var method = GetListMethod(destinationList, type, newIndex == -1);
                if (method == null) {
                    e.DragEffects = DragDropEffects.None;
                    return;
                }

                var draggedItem = e.GetDraggedItem(item);
                if (ReferenceEquals(draggedItem, item)) {
                    sourceList?.Remove(item);
                }

                if (newIndex >= destinationList.Count) {
                    newIndex = -1;
                    method = GetListMethod(destinationList, type, true);

                    if (method == null) {
                        e.DragEffects = DragDropEffects.None;
                        return;
                    }
                }

                method.Invoke(destinationList, newIndex == -1 ? new[] { draggedItem } : new[] { newIndex, draggedItem });
            } catch (Exception ex) {
                Logging.Debug(ex);
                e.DragEffects = DragDropEffects.None;
            }
        }
    }
}
