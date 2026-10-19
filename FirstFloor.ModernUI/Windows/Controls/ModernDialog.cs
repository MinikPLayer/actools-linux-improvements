using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FirstFloor.ModernUI.Commands;
using FirstFloor.ModernUI.Dialogs;
using FirstFloor.ModernUI.Helpers;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// Represents a Modern UI styled dialog window.
    /// </summary>
    public class ModernDialog : DpiAwareWindow {
        protected override Type StyleKeyOverride => typeof(ModernDialog);

        public static readonly StyledProperty<bool> ShowTopBlobProperty = DP.Register<bool>(nameof(ShowTopBlob),
                typeof(ModernDialog), new PropertyMetadata(true));

        public bool ShowTopBlob {
            get => GetValue(ShowTopBlobProperty) as bool? == true;
            set => SetValue(ShowTopBlobProperty, value);
        }

        /// <summary>
        /// Identifies the BackgroundContent dependency property.
        /// </summary>
        public static readonly StyledProperty<object> BackgroundContentProperty = DP.Register<object>("BackgroundContent",
                typeof(ModernDialog));

        /// <summary>
        /// Identifies the Buttons dependency property.
        /// </summary>
        public static readonly StyledProperty<IEnumerable<Control>> ButtonsProperty = DP.Register<IEnumerable<Control>>("Buttons", typeof(ModernDialog));

        private Button _okButton;
        private Button _goButton;
        private Button _cancelButton;
        private Button _yesButton;
        private Button _noButton;
        private Button _closeButton;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModernDialog"/> class.
        /// </summary>
        public ModernDialog() {
            WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner;

            CloseCommand = new DelegateCommand<MessageBoxResult?>(CloseWithResult);
            Buttons = new Control[] { CloseButton };
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (!e.Handled && (e.Key == Key.Escape || e.Key == Key.W && Keyboard.Modifiers == ModifierKeys.Control)) {
                CloseWithResult(MessageBoxResult.Cancel);
            }
        }

        public new Task<MessageBoxResult> ShowAndWaitAsync() {
            var task = new TaskCompletionSource<MessageBoxResult>();
            Closed += (s, a) => task.SetResult(MessageBoxResult);
            Show();
            Focus();
            return task.Task;
        }

        private bool? ToDialogResult(MessageBoxResult result) {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (result) {
                case MessageBoxResult.OK:
                case MessageBoxResult.Yes:
                    return true;
                case MessageBoxResult.Cancel:
                case MessageBoxResult.No:
                    return false;
                default:
                    return null;
            }
        }

        protected void CloseWithResult(MessageBoxResult? result) {
            if (result.HasValue) {
                MessageBoxResult = result.Value;
                try {
                    // sets the Window.DialogResult as well
                    if (DialogResult != ToDialogResult(result.Value)) {
                        DialogResult = ToDialogResult(result.Value);
                        return;
                    }
                } catch (InvalidOperationException) { }
            }
            Close();
        }

        // TODO: Sort those methods, it’s too much

        public static T CreateExtraDialogButton<T>(string content, ICommand command, bool isDefault = false, string toolTip = null) where T : Button, new() {
            return new T {
                Content = content /*.ToLower()*/,
                IsDefault = isDefault,
                MinHeight = 21,
                MinWidth = 65,
                Margin = new Thickness(4, 0, 0, 0),
                Command = command,
                ToolTip = toolTip
            };
        }

        public static Button CreateExtraDialogButton(string content, ICommand command, bool isDefault = false, string toolTip = null) {
            return CreateExtraDialogButton<Button>(content, command, isDefault, toolTip);
        }

        public static Button CreateExtraDialogButton(string content, Action action) {
            return CreateExtraDialogButton(content, new DelegateCommand(action));
        }

        public Button CreateExtraStyledDialogButton([Localizable(false)] string styleKey, string content, ICommand command) {
            return new Button {
                Content = content /*.ToLower()*/,
                MinHeight = 21,
                MinWidth = 65,
                Margin = new Thickness(4, 0, 0, 0),
                Style = this.FindResource(styleKey) as Style,
                Command = command
            };
        }

        private static object GetUacContent(string content) {
            try {
                return new DockPanel {
                    Children = {
                        new Image { Width = 14, Height = 14, Source = UacIcon.Get(), Stretch = Stretch.Uniform, Margin = new Thickness(-2d, 0d, 6d, 0d) },
                        new TextBlock { Text = content }
                    }
                };
            } catch (Exception e) {
                Helpers.Logging.Warning(e);
                return content;
            }
        }

        public Button CreateExtraStyledDialogButton([Localizable(false)] string styleKey, string content, Action action, Func<bool> canExecute = null) {
            return CreateExtraStyledDialogButton(styleKey, content, new DelegateCommand(action, canExecute));
        }

        public Button CreateCloseDialogButton(string content, bool isDefault, bool isCancel, MessageBoxResult result, ICommand command = null,
                bool isUacButton = false) {
            return new Button {
                Content = isUacButton ? GetUacContent(content) : content,
                Command = command == null ? CloseCommand : new CombinedCommand(CloseCommand, command),
                CommandParameter = result,
                IsDefault = isDefault,
                IsCancel = isCancel,
                MinHeight = 21,
                MinWidth = 65,
                Margin = new Thickness(4, 0, 0, 0)
            };
        }

        public Button CreateStyledCloseDialogButton(string styleKey, string content, bool isDefault, bool isCancel, MessageBoxResult result,
                Action action = null) {
            return new Button {
                Content = content,
                Command = action == null ? CloseCommand : new CombinedCommand(CloseCommand, new DelegateCommand(action)),
                CommandParameter = result,
                IsDefault = isDefault,
                IsCancel = isCancel,
                MinHeight = 21,
                MinWidth = 65,
                Margin = new Thickness(4, 0, 0, 0),
                Style = this.FindResource(styleKey) as Style
            };
        }

        /// <summary>
        /// Gets the close window command.
        /// </summary>
        public ICommand CloseCommand { get; }

        /// <summary>
        /// Gets the Ok button.
        /// </summary>
        public Button OkButton => _okButton ??
                (_okButton = CreateCloseDialogButton(UiStrings.Ok, true, false, MessageBoxResult.OK));

        /// <summary>
        /// Gets the Go button (result is MessageBoxResult.OK).
        /// </summary>
        public Button GoButton => _goButton ??
                (_goButton = CreateStyledCloseDialogButton(@"Go.Button", UiStrings.Go, true, false, MessageBoxResult.OK));

        /// <summary>
        /// Gets the Cancel button.
        /// </summary>
        public Button CancelButton => _cancelButton ??
                (_cancelButton = CreateCloseDialogButton(UiStrings.Cancel, false, true, MessageBoxResult.Cancel));

        /// <summary>
        /// Gets the Yes button.
        /// </summary>
        public Button YesButton => _yesButton ??
                (_yesButton = CreateCloseDialogButton(UiStrings.Yes, true, false, MessageBoxResult.Yes));

        /// <summary>
        /// Gets the No button.
        /// </summary>
        public Button NoButton => _noButton ??
                (_noButton = CreateCloseDialogButton(UiStrings.No, false, true, MessageBoxResult.No));

        /// <summary>
        /// Gets the Close button.
        /// </summary>
        public Button CloseButton => _closeButton ??
                (_closeButton = CreateCloseDialogButton(UiStrings.Close, true, false, MessageBoxResult.None));

        /// <summary>
        /// Gets or sets the background content of this window instance.
        /// </summary>
        public object BackgroundContent {
            get => GetValue(BackgroundContentProperty);
            set => SetValue(BackgroundContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the dialog buttons.
        /// </summary>
        public IEnumerable<Control> Buttons {
            get => (IEnumerable<Control>)GetValue(ButtonsProperty);
            set => SetValue(ButtonsProperty, value);
        }

        /// <summary>
        /// Gets the message box result.
        /// </summary>
        /// <value>
        /// The message box result.
        /// </value>
        public MessageBoxResult MessageBoxResult { get; protected set; } = MessageBoxResult.None;

        public bool IsResultCancel => MessageBoxResult == MessageBoxResult.Cancel;
        public bool IsResultOk => MessageBoxResult == MessageBoxResult.OK;
        public bool IsResultYes => MessageBoxResult == MessageBoxResult.Yes;
        public bool IsResultNo => MessageBoxResult == MessageBoxResult.No;

        public static MessageBoxResult ShowMessage(string text, string title, MessageBoxButton button,
                MessageDialog.ShowMessageCallbacks doNotAskAgainLoadSave, Window owner = null) {
            return MessageDialog.Show(text, title, button, doNotAskAgainLoadSave, owner);
        }

        public static MessageBoxResult ShowMessage(string text, string title, MessageBoxButton button, Window owner = null) {
            return MessageDialog.Show(text, title, button, owner);
        }

        public static MessageBoxResult ShowMessage(string text, string title, MessageBoxButton button, [NotNull] string doNotAskAgainKey, Window owner = null) {
            return MessageDialog.Show(text, title, button, doNotAskAgainKey, owner);
        }

        public static MessageBoxResult ShowMessage(string text) {
            return ShowMessage(text, "", MessageBoxButton.OK);
        }

        public static readonly StyledProperty<string> IconSourceProperty = DP.Register<string>(nameof(IconSource),
                typeof(ModernDialog));

        public string IconSource {
            get => (string)GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }

        public static readonly StyledProperty<HorizontalAlignment> ButtonsRowContentAlignmentProperty = DP.Register<HorizontalAlignment>(nameof(ButtonsRowContentAlignment), typeof(ModernDialog));

        public HorizontalAlignment ButtonsRowContentAlignment {
            get => GetValue(ButtonsRowContentAlignmentProperty) as HorizontalAlignment? ?? default;
            set => SetValue(ButtonsRowContentAlignmentProperty, value);
        }

        public static readonly StyledProperty<object> ExtraContentProperty = DP.Register<object>(nameof(ExtraContent),
                typeof(ModernDialog));

        public object ExtraContent {
            get => GetValue(ExtraContentProperty);
            set => SetValue(ExtraContentProperty, value);
        }

        public static readonly StyledProperty<object> ButtonsRowContentProperty = DP.Register<object>(nameof(ButtonsRowContent),
                typeof(ModernDialog));

        public object ButtonsRowContent {
            get => GetValue(ButtonsRowContentProperty);
            set => SetValue(ButtonsRowContentProperty, value);
        }

        public static readonly StyledProperty<Thickness> ButtonsMarginProperty = DP.Register<Thickness>(nameof(ButtonsMargin),
                typeof(ModernDialog));

        public Thickness ButtonsMargin {
            get => GetValue(ButtonsMarginProperty) as Thickness? ?? default;
            set => SetValue(ButtonsMarginProperty, value);
        }

        public static readonly StyledProperty<ItemsPanelTemplate> MainButtonsPanelTemplateProperty = DP.Register<ItemsPanelTemplate>(nameof(MainButtonsPanelTemplate), typeof(ModernDialog));

        public ItemsPanelTemplate MainButtonsPanelTemplate {
            get => (ItemsPanelTemplate)GetValue(MainButtonsPanelTemplateProperty);
            set => SetValue(MainButtonsPanelTemplateProperty, value);
        }

        public static readonly StyledProperty<Thickness> ContentMarginProperty = DP.Register<Thickness>(nameof(ContentMargin),
                typeof(ModernDialog));

        public Thickness ContentMargin {
            get => (Thickness)GetValue(ContentMarginProperty);
            set => SetValue(ContentMarginProperty, value);
        }

        public static readonly StyledProperty<bool> ShowTitleProperty = DP.Register<bool>(nameof(ShowTitle),
                typeof(ModernDialog), new PropertyMetadata(true));

        public bool ShowTitle {
            get => GetValue(ShowTitleProperty) as bool? ?? default;
            set => SetValue(ShowTitleProperty, value);
        }

        public static MessageBoxResult? GetButtonBehavior(DependencyObject obj) {
            return (MessageBoxResult?)obj.GetValue(ButtonBehaviorProperty);
        }

        public static void SetButtonBehavior(DependencyObject obj, MessageBoxResult? value) {
            obj.SetValue(ButtonBehaviorProperty, value);
        }

        public static readonly AttachedProperty<MessageBoxResult?> ButtonBehaviorProperty = DP.RegisterAttached<MessageBoxResult?>("ButtonBehavior",
                typeof(FatalErrorMessage), new UIPropertyMetadata(null, OnButtonBehaviorChanged));

        private static void OnButtonBehaviorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is Button element)) return;
            var newValue = (MessageBoxResult?)e.NewValue;
            if (newValue.HasValue) {
                element.Click += OnButtonBehavorClick;
            } else {
                element.Click -= OnButtonBehavorClick;
            }
        }

        private static void OnButtonBehavorClick(object sender, RoutedEventArgs e) {
            var element = (FrameworkElement)sender;
            var value = GetButtonBehavior(element);
            (Window.GetWindow(element) as ModernDialog)?.CloseCommand.Execute(value);
        }

        private FrameworkElement _bottomRow, _bottomRowExtra;
        private double _offsetX, _offsetY;

        private void UpdateBottomRowStyle() {
            if (_bottomRow == null) return;

            var offsetX = _bottomRow.ActualWidth;
            var offsetY = _bottomRow.ActualHeight;
            if (Math.Abs(offsetX - _offsetX) < 0.5 && Math.Abs(offsetY - _offsetY) < 0.5) return;

            if (_bottomRowExtra != null && _bottomRowExtra.HorizontalAlignment == HorizontalAlignment.Right) {
                _offsetX += _bottomRowExtra.ActualWidth;
            }

            _offsetX = offsetX;
            _offsetY = offsetY;

            // the style isn’t used by the theme anymore
        }

        protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e) {
            this.RememberTemplateScope(e);
            if (_bottomRow != null) {
                _bottomRow.SizeChanged -= OnBottomRowSizeChanged;
            }

            if (_bottomRowExtra != null) {
                _bottomRowExtra.SizeChanged -= OnBottomRowSizeChanged;
            }

            base.OnApplyTemplate(e);

            _bottomRow = this.GetTemplateChild(@"PART_BottomRow_Buttons") as FrameworkElement;
            _bottomRowExtra = this.GetTemplateChild(@"PART_BottomRow_Content") as FrameworkElement;

            if (_bottomRow != null) {
                _bottomRow.SizeChanged += OnBottomRowSizeChanged;
                UpdateBottomRowStyle();
            }

            if (_bottomRowExtra != null) {
                _bottomRowExtra.SizeChanged += OnBottomRowSizeChanged;
            }
        }

        private void OnBottomRowSizeChanged(object sender, SizeChangedEventArgs e) {
            UpdateBottomRowStyle();
        }

    }
}