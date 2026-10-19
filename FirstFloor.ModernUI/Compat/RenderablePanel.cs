using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Metadata;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// Avalonia’s Panel seals its Render method, so panels which draw something on their own (as WPF ones with OnRender) derive from this
    /// one. Only what is needed is implemented: Children, Background, layout is up to the derived class.
    /// </summary>
    public class RenderablePanel : Control {
        public static readonly StyledProperty<IBrush> BackgroundProperty = Border.BackgroundProperty.AddOwner<RenderablePanel>();

        public IBrush Background {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        private readonly Avalonia.Controls.Controls _children = new Avalonia.Controls.Controls();

        public RenderablePanel() {
            _children.CollectionChanged += OnChildrenChanged;
        }

        static RenderablePanel() {
            AffectsRender<RenderablePanel>(BackgroundProperty);
            AffectsMeasure<RenderablePanel>(BackgroundProperty);
        }

        [Content]
        public Avalonia.Controls.Controls Children => _children;

        private void OnChildrenChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add: {
                    var added = e.NewItems.OfType<Control>().ToList();
                    LogicalChildren.InsertRange(e.NewStartingIndex, added);
                    VisualChildren.InsertRange(e.NewStartingIndex, added);
                    break;
                }
                case NotifyCollectionChangedAction.Remove: {
                    var removed = e.OldItems.OfType<Control>().ToList();
                    LogicalChildren.RemoveAll(removed);
                    VisualChildren.RemoveAll(removed);
                    break;
                }
                default:
                    // Replace, move, reset: simply start from scratch
                    LogicalChildren.Clear();
                    VisualChildren.Clear();
                    LogicalChildren.AddRange(_children);
                    VisualChildren.AddRange(_children);
                    break;
            }

            InvalidateMeasure();
        }

        public override void Render(DrawingContext context) {
            var background = Background;
            if (background != null) {
                context.FillRectangle(background, new Rect(Bounds.Size));
            }

            base.Render(context);
        }
    }
}
