using System;
using System.Windows.Markup;
using Avalonia.Data;

namespace FirstFloor.ModernUI.Windows.Bindings {
    /// <summary>
    /// XAML-side version of <see cref="BooleanBinding"/>: Avalonia’s compiler doesn’t accept Binding subclasses as markup extensions.
    /// </summary>
    public class BooleanBindingExtension : MarkupExtension {
        public BooleanBindingExtension() { }

        public BooleanBindingExtension(string path) {
            Path = path;
        }

        [ConstructorArgument("path")]
        public string Path { get; set; }

        public object TrueValue { get; set; }
        public object FalseValue { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider) {
            var result = new BooleanBinding(Path) { TrueValue = TrueValue, FalseValue = FalseValue };
            return result;
        }
    }
}
