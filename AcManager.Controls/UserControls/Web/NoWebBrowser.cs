using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AcManager.Tools.Helpers;
using FirstFloor.ModernUI.Commands;
using FirstFloor.ModernUI.Windows.Controls;
using JetBrains.Annotations;

namespace AcManager.Controls.UserControls.Web {
    /// <summary>
    /// There is no embedded browser in the Avalonia version (CefSharp and WinForms WebBrowser are Windows-only and WPF-only). Instead, this
    /// placeholder shows the address and offers to open it in the default browser.
    /// </summary>
    internal class NoWebBrowser : IWebSomething {
        private string _url = @"about:blank";
        private FrameworkElement _element;
        private TextBlock _addressBlock;

        public FrameworkElement GetElement(DpiAwareWindow parentWindow, bool preferTransparentBackground) {
            if (_element != null) return _element;

            _addressBlock = new TextBlock {
                Text = _url,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var button = new Button {
                Content = "Open in default browser",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            button.Click += (sender, args) => {
                if (_url != @"about:blank") WindowsHelper.ViewInBrowser(_url);
            };

            _element = new StackPanel {
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(20),
                Children = {
                    new TextBlock {
                        Text = "Embedded browser is not available on this platform",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 12)
                    },
                    _addressBlock,
                    button
                }
            };
            return _element;
        }

        public event EventHandler<UrlEventArgs> PageLoadingStarted { add { } remove { } }
        public event EventHandler<UrlEventArgs> PageLoaded { add { } remove { } }
        public event EventHandler<PageLoadingEventArgs> LoadingStateChanged { add { } remove { } }
        public event EventHandler<NewWindowEventArgs> NewWindow { add { } remove { } }
        public event EventHandler<UrlEventArgs> AddressChanged;
        public event EventHandler<TitleChangedEventArgs> TitleChanged { add { } remove { } }
        public event EventHandler<FaviconChangedEventArgs> FaviconChanged { add { } remove { } }

        public bool SupportsFavicons => false;

        public string GetUrl() => _url;

        public T GetJsBridge<T>(Func<T> factory) where T : JsBridgeBase => null;

        public void SetUserAgent(string userAgent) { }
        public void SetStyleProvider(ICustomStyleProvider provider) { }
        public void SetDownloadListener(IWebDownloadListener listener) { }
        public void SetNewWindowsBehavior(NewWindowsBehavior mode) { }
        public void ShowDevTools() { }
        public void Execute(string js) { }

        public void Navigate(string url) {
            _url = url;
            if (_addressBlock != null) _addressBlock.Text = url;
            AddressChanged?.Invoke(this, new UrlEventArgs(url));
        }

        public void OnLoaded() { }
        public void OnUnloaded() { }
        public void OnError(string error, string url, int line, int column) { }

        public Task<string> GetImageUrlAsync(string filename) => Task.FromResult<string>(null);

        public ICommand BackCommand => UnavailableCommand.Instance;
        public ICommand ForwardCommand => UnavailableCommand.Instance;
        public ICommand RefreshCommand => UnavailableCommand.Instance;

        public bool CanHandleAcApiRequests => false;
        public event EventHandler<AcApiRequestEventArgs> AcApiRequest { add { } remove { } }

        public bool IsInjectSupported => false;
        public event EventHandler<WebInjectEventArgs> Inject { add { } remove { } }

        public bool AreHeadersSupported => false;
        public event EventHandler<WebHeadersEventArgs> Headers { add { } remove { } }

        public bool CanConvertFilenames => false;

        public string ConvertFilename(string filename) => filename;
    }
}
