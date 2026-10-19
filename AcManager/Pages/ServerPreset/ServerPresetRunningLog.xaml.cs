using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using AcManager.Controls;
using AcManager.Tools.Objects;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI;
using AvaloniaEdit.Document;
using JetBrains.Annotations;

namespace AcManager.Pages.ServerPreset {
    public partial class ServerPresetRunningLog : global::Avalonia.Controls.UserControl {
        public ServerPresetRunningLog() {
            InitializeComponent();
            if (this.TryFindResource(@"TextEditorSyntaxLog") is string data) {
                this.Text.SyntaxHighlighting = HighlighterHolder.Get(data);
            }
            Loaded += (sender, args) => EnsureSet();
            DataContextChanged += (sender, args) => EnsureSet();
            this.OnActualUnload(EnsureSet);
        }

        [CanBeNull]
        private SelectedPage.ViewModel Model => DataContext as SelectedPage.ViewModel;

        private ServerPresetObject _previousServer;
        private BetterObservableCollection<string> _previousServerLog;

        private void EnsureSet() {
            var server = IsLoaded ? Model?.SelectedObject : null;
            var serverLog = server?.RunningLog;

            if (server != _previousServer) {
                if (_previousServer != null) {
                    _previousServer.PropertyChanged -= OnServerChanged;
                }
                if (server != null) {
                    server.PropertyChanged += OnServerChanged;
                }
                _previousServer = server;
            }

            if (serverLog != _previousServerLog) {
                if (_previousServerLog != null) {
                    _previousServerLog.CollectionChanged -= OnLogChanged;
                }

                if (serverLog != null) {
                    serverLog.CollectionChanged += OnLogChanged;
                    this.Text.Document = new TextDocument(serverLog.JoinToString("\n")) { UndoStack = { SizeLimit = 0 } };
                    this.Text.ScrollToEnd();
                } else {
                    this.Text.Document = new TextDocument { UndoStack = { SizeLimit = 0 } };
                }

                _previousServerLog = serverLog;
            }
        }

        private void OnServerChanged(object sender, PropertyChangedEventArgs e) {
            EnsureSet();
        }

        private void OnLogChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (_previousServerLog == null) return;

            var textView = this.Text.TextArea.TextView;
            var needsToScroll = textView.VerticalOffset + textView.ActualHeight + 1 >= textView.DocumentHeight;
            if (e.Action == NotifyCollectionChangedAction.Add) {
                this.Text.Document.BeginUpdate();
                foreach (var item in e.NewItems.OfType<string>()) {
                    this.Text.Document.Insert(this.Text.Document.TextLength, "\n");
                    this.Text.Document.Insert(this.Text.Document.TextLength, item);
                }
                this.Text.Document.EndUpdate();
            } else {
                this.Text.Document.Text = _previousServerLog.JoinToString("\n");
            }
            if (needsToScroll) {
                this.Text.ScrollToEnd();
            }
        }
    }
}