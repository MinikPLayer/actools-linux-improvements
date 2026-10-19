using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AcManager.Tools.ContentInstallation;
using AcManager.Tools.Managers.Plugins;
using AcTools.Utils;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Windows;
using FirstFloor.ModernUI.Windows.Media;

namespace AcManager.UserControls {
    public partial class InstallAdditionalContentList : global::Avalonia.Controls.UserControl {
        public ListCollectionView DownloadListView { get; }

        public InstallAdditionalContentList() {
            DownloadListView = new BetterListCollectionView(ContentInstallationManager.Instance.DownloadList);
            DownloadListView.SortDescriptions.Add(new SortDescription(nameof(ContentInstallationEntry.AddedDateTime), ListSortDirection.Descending));

            DataContext = this;
            InitializeComponent();

            this.AddWidthCondition(x => (x - 104).Clamp(120, 240)).Add(x => Resources[@"ButtonsRowWidth"] = x);
            Plugins.Ready += OnPluginsReady;
        }

        private async void OnPluginsReady(object sender, EventArgs e) {
            var list = ContentInstallationManager.Instance.DownloadList.Where(
                    x => x.RetryCommand.IsAbleToExecute && (x.State == ContentInstallationEntryState.WaitingForConfirmation || x.IsFailed)).ToList();
            Logging.Debug(list.Count);

            await Task.Yield();
            foreach (var entry in list) {
                Logging.Debug(entry.DisplayName);
                entry.RetryCommand.ExecuteAsync().Ignore();
            }
        }

        private static PluginsRequirement _requirement;
        public PluginsRequirement Plugins => _requirement ?? (_requirement = new PluginsRequirement(KnownPlugins.SevenZip));

        private void OnPasswordBoxKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                e.Handled = true;
                (((FrameworkElement)sender).DataContext as ContentInstallationEntry)?.ApplyPasswordCommand.Execute();
            }
        }

        private void OnItemMouseUp(object sender, PointerReleasedEventArgs e) {
            if (((FrameworkElement)sender).DataContext is ContentInstallationEntry item && !ReferenceEquals(ItemsListBox.SelectedItem, item)) {
                var control = ItemsListBox.ContainerFromIndex(ItemsListBox.Items.IndexOf(item));
                if (control != null) {
                    var button = control.FindVisualChildren<Button>().FirstOrDefault(x => x.IsPointerOver);
                    button?.Command?.Execute(button.CommandParameter);
                }

                ItemsListBox.SelectedItem = item;
            }
        }

        private void OnItemMouseDown(object sender, PointerPressedEventArgs e) { }

        private void OnMouseWheel(object sender, PointerWheelEventArgs e) {
            var scrollViewer = ((FrameworkElement)sender).FindVisualChild<ScrollViewer>();
            if (scrollViewer == null) return;
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta.Y * 120d);
            e.Handled = true;
        }
    }
}