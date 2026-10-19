using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace AcManager.Controls {
    [ContentProperty(nameof(DataTemplate))]
    public class FlexibleDataTemplateSelector : DataTemplateSelector {
        [Avalonia.Metadata.Content] public DataTemplate DataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            return DataTemplate;
        }

        public override Avalonia.Controls.Control Build(object param) {
            // controls are shown as they are
            return param is UIElement ? new ContentPresenter { Content = param } : DataTemplate?.Build(param);
        }
    }
}