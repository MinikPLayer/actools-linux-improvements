using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Input;
using FirstFloor.ModernUI.Commands;
using FirstFloor.ModernUI.Windows.Attached;
using FirstFloor.ModernUI.Windows.Media;

namespace FirstFloor.ModernUI.Windows.Controls {
    public class TagsList : Control {
        private CommandBase _closeCommand;

        public ICommand CloseCommand => _closeCommand ?? (_closeCommand = new DelegateCommand<string>(o => {
            ItemsSource.Remove(o);
        }));

        private CommandBase _changeCommand;

        public ICommand ChangeCommand => _changeCommand ?? (_changeCommand = new DelegateCommand<TextBox>(o => {
            if (!(o?.DataContext is string originalValue)) return;

            var newValue = o.Text.Trim();
            if (Equals(originalValue, newValue)) return;

            if (string.IsNullOrEmpty(newValue)) {
                ItemsSource.Remove(originalValue);
            } else {
                var index = ItemsSource.IndexOf(originalValue);
                if (index == -1) return;

                ItemsSource[index] = o.Text;
            }
        }));

        public static readonly StyledProperty<ObservableCollection<string>> ItemsSourceProperty = DP.Register<ObservableCollection<string>>("ItemsSource",
            typeof(TagsList));

        public static readonly StyledProperty<CollectionView> SuggestionsSourceProperty = DP.Register<CollectionView>("SuggestionsSource",
            typeof(TagsList));

        public static readonly StyledProperty<ContextMenu> ItemContextMenuProperty = DP.Register<ContextMenu>(nameof(ItemContextMenu),
                typeof(TagsList));

        public ContextMenu ItemContextMenu {
            get => (ContextMenu)GetValue(ItemContextMenuProperty);
            set => SetValue(ItemContextMenuProperty, value);
        }

        public TagsList() {
        }

        public ObservableCollection<string> ItemsSource {
            get => (ObservableCollection<string>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public CollectionView SuggestionsSource {
            get => (CollectionView)GetValue(SuggestionsSourceProperty);
            set => SetValue(SuggestionsSourceProperty, value);
        }

        private ComboBox _previousTextBox;

        protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e) {
            this.RememberTemplateScope(e);
            if (_previousTextBox != null) {
                _previousTextBox.RemovePreviewKeyDown(TextBox_KeyDown);
                _previousTextBox.LostFocus -= TextBox_LostFocus;
                _previousTextBox = null;
            }

            base.OnApplyTemplate(e);

            if (!(e.NameScope.Find("PART_NewTagTextBox") is ComboBox textBox)) return;
            textBox.AddPreviewKeyDown(TextBox_KeyDown);
            textBox.LostFocus += TextBox_LostFocus;
            _previousTextBox = textBox;
        }

        private void AddNewTag(string tag) {
            var tagLower = tag.ToLower();
            if (ItemsSource.FirstOrDefault(x => x.ToLower() == tagLower) != null) return;
            ItemsSource.Add(tag);
        }

        private static bool IsCaretAtFront(DependencyObject d) {
            var t = d.FindVisualChild<TextBox>();
            return t?.SelectionStart == 0 && t.SelectionLength == 0;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key != Key.Back && e.Key != Key.Enter && e.Key != Key.Tab &&
                    e.Key != Key.Escape) return;

            if (!(sender is ComboBox textBox)) return;
            if (e.Key != Key.Back) {
                if (string.IsNullOrWhiteSpace(textBox.Text)) return;

                AddNewTag(textBox.Text.Trim());
                textBox.Text = "";

                if (e.Key == Key.Tab) {
                    textBox.MoveFocus(Keyboard.Modifiers == ModifierKeys.Shift ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next);
                } else if (e.Key == Key.Escape) {
                    textBox.RemoveFocus();
                }
            } else if (string.IsNullOrWhiteSpace(textBox.Text) || IsCaretAtFront(textBox)) {
                textBox.MoveFocus(FocusNavigationDirection.Previous);
            } else {
                return;
            }

            e.Handled = true;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e) {
            var textBox = sender as ComboBox;
            if (string.IsNullOrWhiteSpace(textBox?.Text)) return;

            AddNewTag(textBox.Text.Trim());
            textBox.Text = "";
        }

        public void RemoveTag(string value) {
            ItemsSource.Remove(value);
        }

        public void ReplaceTag(string oldValue, string newValue) {
            if (ItemsSource.Contains(oldValue)) {
                ItemsSource[ItemsSource.IndexOf(oldValue)] = newValue;
            } else {
                ItemsSource.Add(newValue);
            }
        }

        public static readonly StyledProperty<bool> IsReadOnlyProperty = DP.Register<bool>(nameof(IsReadOnly),
                typeof(TagsList));

        public bool IsReadOnly {
            get => GetValue(IsReadOnlyProperty) as bool? == true;
            set => SetValue(IsReadOnlyProperty, value);
        }
    }

    public class TagsListDataTemplateSelector : DataTemplateSelector {
        public DataTemplate TagDataTemplate { get; set; }

        public DataTemplate NewTagDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            return item is string ? TagDataTemplate : NewTagDataTemplate;
        }
    }
}
