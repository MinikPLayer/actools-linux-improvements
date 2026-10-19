using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using FirstFloor.ModernUI.Presentation;
using FirstFloor.ModernUI.Windows.Navigation;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows.Controls {
    [ContentProperty(nameof(AdditionalContent))]
    public class ModernWindow : DpiAwareWindow {
        public static RoutedUICommand NavigateTitleLink { get; } = new RoutedUICommand(UiStrings.NavigateLink, nameof(NavigateTitleLink), typeof(LinkCommands));

        public ModernWindow() {

            SetCurrentValue(MenuLinkGroupsProperty, new LinkGroupCollection());
            SetCurrentValue(TitleButtonsProperty, new ObservableCollection<UIElement>());
            SetCurrentValue(TitleLinksProperty, new LinkCollection());

            this.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, OnCloseWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, OnMaximizeWindow, OnCanResizeWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, OnMinimizeWindow, OnCanMinimizeWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, OnRestoreWindow, OnCanResizeWindow));
            this.CommandBindings.Add(new CommandBinding(LinkCommands.NavigateLink, OnNavigateLink, OnCanNavigateLink));
            this.CommandBindings.Add(new CommandBinding(NavigateTitleLink, OnNavigateTitleLink, OnCanNavigateTitleLink));
        }

        public event EventHandler<NavigatingCancelEventArgs> FrameNavigating;
        public event EventHandler<NavigationEventArgs> FrameNavigated;

        [CanBeNull]
        private ModernMenu _menu;

        [CanBeNull]
        private ModernFrame _frame;

        [CanBeNull]
        public Uri CurrentSource => _frame?.Source;

        public LinkGroup CurrentLinkGroup => _menu?.SelectedLinkGroup;

        protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e) {
            this.RememberTemplateScope(e);
            base.OnApplyTemplate(e);

            if (_frame != null) {
                _frame.Navigating -= OnFrameNavigating;
                _frame.Navigated -= OnFrameNavigated;
            }

            _frame = this.GetTemplateChild(@"ContentFrame") as ModernFrame;

            if (_frame != null) {
                _frame.Navigating += OnFrameNavigating;
                _frame.Navigated += OnFrameNavigated;
            }

            _menu = this.GetTemplateChild(@"PART_Menu") as ModernMenu;
        }

        private void OnFrameNavigated(object sender, NavigationEventArgs navigationEventArgs) {
            FrameNavigated?.Invoke(this, navigationEventArgs);

            var activeKey = _menu?.SelectedLinkGroup?.GroupKey;
            foreach (var link in TitleLinks.OfType<TitleLink>()) {
                link.IsActive = link.GroupKey == activeKey;
            }
        }

        private void OnFrameNavigating(object sender, NavigatingCancelEventArgs navigationEventArgs) {
            FrameNavigating?.Invoke(this, navigationEventArgs);
        }

        private void OnCanNavigateTitleLink(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }

        private void OnNavigateTitleLink(object sender, ExecutedRoutedEventArgs e) {
            if (LinkNavigator == null) return;

            var titleLink = e.Parameter as TitleLink;
            if (titleLink?.GroupKey != null && !titleLink.GroupKey.Contains(@"/")) {
                _menu?.SwitchToGroupByKey(titleLink.GroupKey);
                return;
            }

            var eParameter = (e.Parameter as Link)?.Source ?? e.Parameter;
            if (NavigationHelper.TryParseUriWithParameters(eParameter, false, out var uri, out var parameter, out var _, out _)) {
                LinkNavigator.Navigate(uri, e.Source as FrameworkElement, parameter);
            }
        }

        public string CurrentGroupKey {
            get => _menu?.SelectedLinkGroup?.GroupKey;
            set => _menu?.SwitchToGroupByKey(value);
        }

        public void NavigateTo(Uri uri) {
            if (_menu == null) return;
            if (uri == _menu.SelectedSource) {
                _menu.SelectedSource = new Uri("about:blank");
            }
            _menu.SelectedSource = uri;
        }

        private void OnCanNavigateLink(object sender, CanExecuteRoutedEventArgs e) {
            // true by default
            e.CanExecute = true;
            if (LinkNavigator?.Commands == null) return;

            // in case of command uri, check if ICommand.CanExecute is true
            // TODO: CanNavigate is invoked a lot, which means a lot of parsing. need improvements?
            if (!NavigationHelper.TryParseUriWithParameters(e.Parameter, false, out var uri, out var parameter, out var _, out _)) {
                return;
            }

            if (LinkNavigator.Commands.TryGetValue(uri, out var command)) {
                e.CanExecute = command.CanExecute(parameter);
            }
        }

        private void OnNavigateLink(object sender, ExecutedRoutedEventArgs e) {
            if (LinkNavigator == null) return;
            if (NavigationHelper.TryParseUriWithParameters(e.Parameter, false, out var uri, out var parameter, out var _, out _)) {
                LinkNavigator.Navigate(uri, e.Source as FrameworkElement, parameter);
            }
        }

        private void OnCanResizeWindow(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = CanResize;
        }

        private void OnCanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = CanResize;
        }

        private void OnCloseWindow(object target, ExecutedRoutedEventArgs e) {
            SystemCommands.CloseWindow(this);
        }

        private void OnMaximizeWindow(object target, ExecutedRoutedEventArgs e) {
            SystemCommands.MaximizeWindow(this);
        }

        private void OnMinimizeWindow(object target, ExecutedRoutedEventArgs e) {
            SystemCommands.MinimizeWindow(this);
        }

        private void OnRestoreWindow(object target, ExecutedRoutedEventArgs e) {
            SystemCommands.RestoreWindow(this);
        }

        public static readonly StyledProperty<Thickness> FrameMarginProperty = DP.Register<Thickness>(nameof(FrameMargin),
                typeof(ModernWindow));

        public Thickness FrameMargin {
            get => GetValue(FrameMarginProperty) as Thickness? ?? default;
            set => SetValue(FrameMarginProperty, value);
        }

        public static readonly StyledProperty<object> BackgroundContentProperty = DP.Register<object>(nameof(BackgroundContent),
                typeof(ModernWindow));

        public object BackgroundContent {
            get => GetValue(BackgroundContentProperty); // TODO: Optimize?
            set => SetValue(BackgroundContentProperty, value);
        }

        public static readonly StyledProperty<object> TitleLinksPrefixProperty = DP.Register<object>(nameof(TitleLinksPrefix),
                typeof(ModernWindow), new PropertyMetadata(null, (o, e) => {
                    ((ModernWindow)o)._titleLinksPrefix = e.NewValue;
                }));

        private object _titleLinksPrefix;

        public object TitleLinksPrefix {
            get => _titleLinksPrefix;
            set => SetValue(TitleLinksPrefixProperty, value);
        }

        public static readonly StyledProperty<LinkGroupCollection> MenuLinkGroupsProperty = DP.Register<LinkGroupCollection>(nameof(MenuLinkGroups),
                typeof(ModernWindow));

        public LinkGroupCollection MenuLinkGroups {
            get => (LinkGroupCollection)GetValue(MenuLinkGroupsProperty);
            set => SetValue(MenuLinkGroupsProperty, value);
        }

        public static readonly StyledProperty<ObservableCollection<UIElement>> TitleButtonsProperty = DP.Register<ObservableCollection<UIElement>>(nameof(TitleButtons), typeof(ModernWindow));

        public ObservableCollection<UIElement> TitleButtons {
            get => (ObservableCollection<UIElement>)GetValue(TitleButtonsProperty);
            set => SetValue(TitleButtonsProperty, value);
        }

        public static readonly StyledProperty<LinkCollection> TitleLinksProperty = DP.Register<LinkCollection>(nameof(TitleLinks),
                typeof(ModernWindow));

        public LinkCollection TitleLinks {
            get => (LinkCollection)GetValue(TitleLinksProperty);
            set => SetValue(TitleLinksProperty, value);
        }

        public static readonly StyledProperty<bool> IsTitleVisibleProperty = DP.Register<bool>(nameof(IsTitleVisible),
                typeof(ModernWindow),
                new PropertyMetadata(false));

        public bool IsTitleVisible {
            get => GetValue(IsTitleVisibleProperty) as bool? == true;
            set => SetValue(IsTitleVisibleProperty, value);
        }

        public static readonly StyledProperty<double> MaxRootWidthProperty = DP.Register<double>(nameof(MaxRootWidth),
                typeof(ModernWindow), new PropertyMetadata(double.PositiveInfinity, (o, e) => {
                    ((ModernWindow)o)._maxRootWidth = (double)e.NewValue;
                }));

        private double _maxRootWidth = double.PositiveInfinity;

        public double MaxRootWidth {
            get => _maxRootWidth;
            set => SetValue(MaxRootWidthProperty, value);
        }

        public static readonly StyledProperty<bool> IsRootMarginEvenProperty = DP.Register<bool>(nameof(IsRootMarginEven),
                typeof(ModernWindow), new PropertyMetadata(false, (o, e) => {
                    ((ModernWindow)o)._isRootMarginEven = (bool)e.NewValue;
                }));

        private bool _isRootMarginEven;

        public bool IsRootMarginEven {
            get => _isRootMarginEven;
            set => SetValue(IsRootMarginEvenProperty, value);
        }

        public static readonly StyledProperty<Uri> DefaultContentSourceProperty = DP.Register<Uri>(nameof(DefaultContentSource),
                typeof(ModernWindow));

        public Uri DefaultContentSource {
            get => (Uri)GetValue(DefaultContentSourceProperty);
            set => SetValue(DefaultContentSourceProperty, value);
        }

        public static readonly StyledProperty<IContentLoader> ContentLoaderProperty = DP.Register<IContentLoader>(nameof(ContentLoader),
                typeof(ModernWindow), new PropertyMetadata(new DefaultContentLoader()));

        public IContentLoader ContentLoader {
            get => (IContentLoader)GetValue(ContentLoaderProperty);
            set => SetValue(ContentLoaderProperty, value);
        }

        public static DependencyProperty LinkNavigatorProperty = DP.Register(nameof(LinkNavigator), typeof(ILinkNavigator), typeof(ModernWindow),
                new PropertyMetadata(new DefaultLinkNavigator()));

        public ILinkNavigator LinkNavigator {
            get => (ILinkNavigator)GetValue(LinkNavigatorProperty);
            set => SetValue(LinkNavigatorProperty, value);
        }

        public static readonly StyledProperty<string> SaveKeyProperty = DP.Register<string>(nameof(SaveKey),
                typeof(ModernWindow));

        public string SaveKey {
            get => (string)GetValue(SaveKeyProperty);
            set => SetValue(SaveKeyProperty, value);
        }

        public static readonly StyledProperty<object> AdditionalContentProperty = DP.Register<object>(nameof(AdditionalContent),
                typeof(ModernWindow));

        public object AdditionalContent {
            get => GetValue(AdditionalContentProperty);
            set => SetValue(AdditionalContentProperty, value);
        }

        public static readonly StyledProperty<Visibility> BackButtonVisibilityProperty = DP.Register<Visibility>(nameof(BackButtonVisibility),
                typeof(ModernWindow), new PropertyMetadata(Visibility.Visible));

        public Visibility BackButtonVisibility {
            get => GetValue(BackButtonVisibilityProperty) as Visibility? ?? default;
            set => SetValue(BackButtonVisibilityProperty, value);
        }

        public static readonly StyledProperty<Visibility> TitleButtonsVisibilityProperty = DP.Register<Visibility>(nameof(TitleButtonsVisibility),
                typeof(ModernWindow), new PropertyMetadata(Visibility.Visible));

        public Visibility TitleButtonsVisibility {
            get => (Visibility)GetValue(TitleButtonsVisibilityProperty);
            set => SetValue(TitleButtonsVisibilityProperty, value);
        }

        public static readonly StyledProperty<Visibility> MenuTopRowVisibilityProperty = DP.Register<Visibility>(nameof(MenuTopRowVisibility),
                typeof(ModernWindow), new PropertyMetadata(Visibility.Visible));

        public Visibility MenuTopRowVisibility {
            get => (Visibility)GetValue(MenuTopRowVisibilityProperty);
            set => SetValue(MenuTopRowVisibilityProperty, value);
        }

        public static readonly StyledProperty<bool> ShowErrorsButtonProperty = DP.Register<bool>(nameof(ShowErrorsButton),
                typeof(ModernWindow), new PropertyMetadata(true));

        public bool ShowErrorsButton {
            get => (bool)GetValue(ShowErrorsButtonProperty);
            set => SetValue(ShowErrorsButtonProperty, value);
        }

        public static readonly StyledProperty<object> CloseButtonProperty = DP.Register<object>(nameof(CloseButton),
                typeof(ModernWindow));

        public object CloseButton {
            get => GetValue(CloseButtonProperty);
            set => SetValue(CloseButtonProperty, value);
        }
    }
}