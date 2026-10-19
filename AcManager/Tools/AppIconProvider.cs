using System;
using FirstFloor.ModernUI.Windows;

namespace AcManager.Tools {
    public class AppIconProvider : IAppIconProvider {
        public Uri GetTrayIcon() {
            return WindowsVersionHelper.IsWindows10OrGreater ?
                    new Uri("pack://application:,,,/Assets/Icons/TrayIcon.ico", UriKind.Absolute) :
                    new Uri("pack://application:,,,/Assets/Icons/TrayIconWin8.ico", UriKind.Absolute);
        }

        public Uri GetAppIcon() {
            return new Uri("pack://application:,,,/Assets/Icons/Icon.ico", UriKind.Absolute);
        }
    }
}