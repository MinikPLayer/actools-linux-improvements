using System;
using System.Windows.Markup;
using Avalonia.Data;
using FirstFloor.ModernUI.Serialization;

namespace FirstFloor.ModernUI.Helpers {
    /// <summary>
    /// XAML-side version of <see cref="Stored"/>: Avalonia’s compiler doesn’t accept Binding subclasses as markup extensions.
    /// </summary>
    public class StoredExtension : MarkupExtension {
        public StoredExtension() { }

        public StoredExtension(string key) {
            Key = key;
        }

        [ConstructorArgument("key")]
        public string Key { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider) {
            var key = Key ?? "";
            var i = key.IndexOf('=');
            return new Binding(nameof(StoredValue.Value)) {
                Source = StoredValue.Create(i == -1 ? key : key.Substring(0, i), i == -1 ? null : key.Substring(i + 1)),
                Mode = BindingMode.TwoWay
            };
        }
    }
}
