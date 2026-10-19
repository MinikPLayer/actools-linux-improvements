using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FirstFloor.ModernUI.Commands;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Presentation;
using FirstFloor.ModernUI.Windows.Navigation;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows.Controls {
    public class ModernTab : Control {
        public static readonly StyledProperty<HorizontalAlignment> LinksHorizontalAlignmentProperty = DP.Register<HorizontalAlignment>("LinksHorizontalAlignment", typeof(ModernTab), new PropertyMetadata());

        public HorizontalAlignment LinksHorizontalAlignment {
            get => GetValue(LinksHorizontalAlignmentProperty) as HorizontalAlignment? ?? default;
            set => SetValue(LinksHorizontalAlignmentProperty, value);
        }

        public static readonly StyledProperty<Thickness> LinksMarginProperty = DP.Register<Thickness>("LinksMargin",
                typeof(ModernTab), new PropertyMetadata(new Thickness(0.0, 0.0, 0.0, 0.0)));

        public static readonly StyledProperty<Thickness> FrameMarginProperty = DP.Register<Thickness>("FrameMargin",
                typeof(ModernTab), new PropertyMetadata(new Thickness(0.0, 0.0, 0.0, 0.0)));

        public Thickness LinksMargin {
            get => GetValue(LinksMarginProperty) as Thickness? ?? default;
            set => SetValue(LinksMarginProperty, value);
        }

        public Thickness FrameMargin {
            get => GetValue(FrameMarginProperty) as Thickness? ?? default;
            set => SetValue(FrameMarginProperty, value);
        }

        public static readonly StyledProperty<SavePolicy> SavePolicyProperty = DP.Register<SavePolicy>(nameof(SavePolicy),
                typeof(ModernTab));

        public SavePolicy SavePolicy {
            get => GetValue(SavePolicyProperty) as SavePolicy? ?? default;
            set => SetValue(SavePolicyProperty, value);
        }

        public static readonly StyledProperty<IContentLoader> ContentLoaderProperty = DP.Register<IContentLoader>("ContentLoader",
                typeof(ModernTab), new PropertyMetadata(new DefaultContentLoader()));

        public static readonly StyledProperty<TabLayout> LayoutProperty = DP.Register<TabLayout>("Layout",
                typeof(ModernTab), new PropertyMetadata(TabLayout.Tab));

        public static readonly StyledProperty<LinkCollection> LinksProperty = DP.Register<LinkCollection>("Links",
                typeof(ModernTab), new PropertyMetadata(OnLinksChanged));

        public static readonly StyledProperty<Uri> SelectedSourceProperty = DP.Register<Uri>("SelectedSource",
                typeof(ModernTab), new PropertyMetadata(OnSelectedSourceChanged));

        public event EventHandler<SourceEventArgs> SelectedSourceChanged;
        public event EventHandler<NavigationEventArgs> FrameNavigated;

        private ListBox _linkList;

        public string PinnedSaveKey { get; set; }

        private void SavePinned() {
            var saveKey = PinnedSaveKey ?? (SaveKey != null ? SaveKey + "/pinned" : null);
            if (saveKey != null) {
                ValuesStorage.Storage.SetStringList(saveKey,
                        PinnedLinks.Select(x => Storage.EncodeList(x.DisplayName, x.Source?.OriginalString ?? "")));
            }
        }

        public void LoadPinned() {
            var saveKey = PinnedSaveKey ?? (SaveKey != null ? SaveKey + "/pinned" : null);
            if (saveKey != null) {
                PinnedLinks.Clear();
                foreach (var item in ValuesStorage.Storage.GetStringList(saveKey).Select(Storage.DecodeList)) {
                    var values = item.ToList();
                    if (values.Count == 2) {
                        PinnedLinks.Add(new Link {
                            DisplayName = values[0],
                            Source = new Uri(values[1], UriKind.Relative),
                            IsPinned = true
                        });
                    }
                }
                foreach (var link in PinnedLinks) {
                    link.PropertyChanged += OnPinnedLinkChanged;
                }
            }
        }

        /// <summary>
        /// Item of <see cref="LinksWithTitle"/> which shows the title of the current page (TabWithTitle layout).
        /// </summary>
        public sealed class TitleItem : NotifyPropertyChanged {
            private string _title;

            public string Title {
                get => _title;
                set => Apply(value, ref _title);
            }
        }

        public static readonly StyledProperty<System.Collections.IEnumerable> LinksWithTitleProperty = DP.Register<System.Collections.IEnumerable>(
                nameof(LinksWithTitle), typeof(ModernTab));

        /// <summary>
        /// Links, pinned links and the title of the current page, WPF used CompositeCollection for that.
        /// </summary>
        public System.Collections.IEnumerable LinksWithTitle => (System.Collections.IEnumerable)GetValue(LinksWithTitleProperty);

        private readonly Avalonia.Collections.AvaloniaList<object> _linksWithTitle = new Avalonia.Collections.AvaloniaList<object>();
        private readonly TitleItem _titleItem = new TitleItem();
        private LinkCollection _observedLinks, _observedPinnedLinks;

        private void ObserveLinks(ref LinkCollection observed, LinkCollection collection) {
            if (observed != null) {
                observed.CollectionChanged -= OnLinksCollectionChanged;
            }

            observed = collection;
            if (observed != null) {
                observed.CollectionChanged += OnLinksCollectionChanged;
            }

            RebuildLinksWithTitle();
        }

        private void OnLinksCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            RebuildLinksWithTitle();
        }

        private void RebuildLinksWithTitle() {
            var selected = _linkList?.SelectedItem;
            var items = (Links ?? Enumerable.Empty<Link>()).Cast<object>().Concat(PinnedLinks ?? Enumerable.Empty<Link>()).ToList();
            var titled = Title != null;
            if (titled) {
                _titleItem.Title = Title;
                items.Add(_titleItem);
            }

            if (_linksWithTitle.SequenceEqual(items)) return;
            _linksWithTitle.Clear();
            _linksWithTitle.AddRange(items);
            if (selected != null && _linksWithTitle.Contains(selected) && _linkList != null) {
                _linkList.SelectedItem = selected;
            }
        }

        protected override void OnPropertyChanged(Avalonia.AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);
            if (change.Property == LinksProperty) {
                ObserveLinks(ref _observedLinks, change.NewValue as LinkCollection);
            } else if (change.Property == PinnedLinksProperty) {
                ObserveLinks(ref _observedPinnedLinks, change.NewValue as LinkCollection);
            } else if (change.Property == TitleProperty) {
                RebuildLinksWithTitle();
            }
        }

        public ModernTab() {
            SetCurrentValue(LinksWithTitleProperty, _linksWithTitle);
            SetCurrentValue(LinksProperty, new LinkCollection());
            SetCurrentValue(PinnedLinksProperty, new LinkCollection());
            SetCurrentValue(PinCurrentCommandProperty, new DelegateCommand(async () => {
                if (Title == null || SelectedSource == null || _linkList == null) return;
                var link = new Link {
                    DisplayName = Title,
                    Source = SelectedSource,
                    IsPinned = true
                };
                Title = null;
                await Task.Yield();
                PinnedLinks.Add(link);
                _linkList.SelectedItem = link;
                link.PropertyChanged += OnPinnedLinkChanged;
                SavePinned();
            }));
        }

        private void OnPinnedLinkChanged(object sender, PropertyChangedEventArgs args) {
            var link = (Link)sender;
            if (!link.IsPinned) {
                var reselect = SelectedSource == link.Source;
                PinnedLinks.Remove(link);
                if (reselect) {
                    Title = link.DisplayName;
                    _linkList.SelectedItem = null;
                }
                SavePinned();
            }
        }

        public ModernFrame Frame { get; private set; }

        private static void OnLinksChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((ModernTab)o).UpdateSelection(false);
        }

        private static void OnSelectedSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((ModernTab)o).OnSelectedSourceChanged((Uri)e.NewValue);
        }

        private void OnSelectedSourceChanged(Uri newValue) {
            UpdateSelection(true);
            SelectedSourceChanged?.Invoke(this, new SourceEventArgs(newValue));
        }

        private static object FindFittingLink(LinkCollection collection, Uri source) {
            foreach (var link in collection) {
                if (link.IsShown && link.Source == source) return link;
                if (link is LinkWithList list) {
                    foreach (var child in list.Children) {
                        if (child.IsShown && child.Source == source) {
                            list.SelectedLink = child;
                            return list;
                        }
                    }
                }
            }
            return null;
        }

        private static object FindFittingLink(LinkCollection collection, string source) {
            foreach (var link in collection) {
                if (link.IsShown && link.Source?.OriginalString == source) return link;
                if (link is LinkWithList list) {
                    foreach (var child in list.Children) {
                        if (child.IsShown && child.Source?.OriginalString == source) {
                            list.SelectedLink = child;
                            return list;
                        }
                    }
                }
            }
            return null;
        }

        private object FindFittingLink(string source) {
            return FindFittingLink(Links, source) ?? FindFittingLink(PinnedLinks, source);
        }

        private object FindFittingLink(Uri source) {
            return FindFittingLink(Links, source) ?? FindFittingLink(PinnedLinks, source);
        }

        private void UpdateSelection(bool skipLoading) {
            if (_linkList == null || Links == null || SavePolicy == SavePolicy.SkipLoadingFlexible) {
                return;
            }

            if (SavePolicy == SavePolicy.SkipLoading && Frame.Source != null) {
                _linkList.SelectedItem = FindFittingLink(SelectedSource) ?? (skipLoading ? null : Links.FirstOrDefault());
                return;
            }

            if (!skipLoading && SavePolicy == SavePolicy.Flexible && SaveKey != null) {
                Frame.Source = ValuesStorage.Get<Uri>(SaveKey) ?? Links.FirstOrDefault()?.Source;
            } else {
                var saved = skipLoading || SaveKey == null ? null : ValuesStorage.Get<string>(SaveKey);
                _linkList.SelectedItem = (saved == null ? null : FindFittingLink(saved))
                        ?? FindFittingLink(SelectedSource) ?? (skipLoading ? null : Links.FirstOrDefault());
            }
        }

        protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e) {
            this.RememberTemplateScope(e);
            base.OnApplyTemplate(e);

            if (_linkList != null) {
                _linkList.SelectionChanged -= OnLinkListSelectionChanged;
            }

            if (Frame != null) {
                Frame.Navigated -= Frame_Navigated;
            }

            _linkList = this.GetTemplateChild(@"PART_LinkList") as ListBox;
            Frame = this.GetTemplateChild(@"PART_Frame") as ModernFrame;

            if (_linkList != null) {
                _linkList.SelectionChanged += OnLinkListSelectionChanged;
            }

            if (Frame != null) {
                Frame.Navigated += Frame_Navigated;
            }

            UpdateSelection(false);
        }

        public double? GetLinkListWidth() {
            return _linkList?.ActualWidth;
        }

        private void OnLinkListSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (_linkList.SelectedItem is Link link && !(link is LinkWithList) && link.Source != SelectedSource) {
                SetCurrentValue(SelectedSourceProperty, link.Source);
            }
        }

        private void Frame_Navigated(object sender, NavigationEventArgs navigationEventArgs) {
            FrameNavigated?.Invoke(this, navigationEventArgs);
            if (Layout == TabLayout.TabWithTitle) {
                if (Frame.Content is ITabCanBePinned pinnable && pinnable.Title != null) {
                    var pinned = PinnedLinks.FirstOrDefault(x => x.Source == SelectedSource);
                    if (pinned == null) {
                        Title = pinnable.Title;
                    } else {
                        Title = null;
                        if (pinned.DisplayName != pinnable.Title) {
                            pinned.DisplayName = pinnable.Title;
                            SavePinned();
                        }
                    }
                } else {
                    Title = null;
                }
            }

            if (SaveKey != null && (_linkList.SelectedItem != null || SavePolicy == SavePolicy.Flexible)) {
                ValuesStorage.Set(SaveKey, Frame.Source.OriginalString);
            }
        }

        public IContentLoader ContentLoader {
            get => (IContentLoader)GetValue(ContentLoaderProperty);
            set => SetValue(ContentLoaderProperty, value);
        }

        public TabLayout Layout {
            get => GetValue(LayoutProperty) as TabLayout? ?? default;
            set {
                Title = null;
                SetValue(LayoutProperty, value);
            }
        }

        public LinkCollection Links {
            get => (LinkCollection)GetValue(LinksProperty);
            set => SetValue(LinksProperty, value);
        }

        public static readonly StyledProperty<LinkCollection> PinnedLinksProperty = DP.Register<LinkCollection>(nameof(PinnedLinks),
                typeof(ModernTab));

        public LinkCollection PinnedLinks {
            get => (LinkCollection)GetValue(PinnedLinksProperty);
            set => SetValue(PinnedLinksProperty, value);
        }

        public Uri SelectedSource {
            get => (Uri)GetValue(SelectedSourceProperty);
            set => SetValue(SelectedSourceProperty, value);
        }

        public static readonly StyledProperty<string> TitleProperty = DP.Register<string>(nameof(Title),
                typeof(ModernTab));

        public string Title {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly StyledProperty<ICommand> PinCurrentCommandProperty = DP.Register<ICommand>(nameof(PinCurrentCommand),
                typeof(ModernTab));

        public ICommand PinCurrentCommand {
            get => (ICommand)GetValue(PinCurrentCommandProperty);
            set => SetValue(PinCurrentCommandProperty, value);
        }

        // saving and loading uri
        public static readonly StyledProperty<string> SaveKeyProperty = DP.Register<string>(nameof(SaveKey),
                typeof(ModernTab), new PropertyMetadata(OnSaveKeyChanged));

        [CanBeNull]
        public string SaveKey {
            get => (string)GetValue(SaveKeyProperty);
            set => SetValue(SaveKeyProperty, value);
        }

        private static void OnSaveKeyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((ModernTab)o).LoadPinned();
            ((ModernTab)o).UpdateSelection(false);
        }

        public static readonly StyledProperty<ControlTemplate> LinksListBoxTemplateProperty = DP.Register<ControlTemplate>(nameof(LinksListBoxTemplate),
                typeof(ModernTab));

        public ControlTemplate LinksListBoxTemplate {
            get => (ControlTemplate)GetValue(LinksListBoxTemplateProperty);
            set => SetValue(LinksListBoxTemplateProperty, value);
        }
    }
}