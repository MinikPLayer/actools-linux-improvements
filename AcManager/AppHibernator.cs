using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AcTools.Windows.Compat;
using System.Windows.Input;
using AcManager.Pages.Settings;
using AcManager.Tools.GameProperties;
using AcManager.Tools.Helpers;
using AcManager.Tools.SemiGui;
using AcManager.Tools.SharedMemory;
using AcTools.Utils.Helpers;
using AcTools.Windows.Input;
using FirstFloor.ModernUI;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Windows;
using FirstFloor.ModernUI.Windows.Controls;

namespace AcManager {
    public partial class AppHibernator : IDisposable {
        private IKeyboardListener _keyboard;

        private void InitKeyboardWatcher() {
            if (_keyboard != null) return;
            _keyboard = KeyboardListenerFactory.Get();
            _keyboard.WatchFor(Keys.Oemtilde, Keys.F1);
            _keyboard.PreviewKeyDown += (sender, args) => {
                if (SettingsHolder.Drive.ShowCspSettingsWithShortcut
                        && (args.Key == Keys.Oemtilde || args.Key == Keys.F1)
                        && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {
                    if (SettingsShadersPatch.CloseOpenedSettings != null) {
                        SettingsShadersPatch.CloseOpenedSettings();
                    } else {
                        ActionExtension.InvokeInMainThreadAsync(() => SettingsShadersPatch.GetShowSettingsCommand().Execute(null));
                    }
                }
            };
        }

        public void SetListener() {
            SettingsHolder.Drive.PropertyChanged += Drive_PropertyChanged;
            UpdateGameListeners();

            if (SettingsHolder.Drive.ShowCspSettingsWithShortcut) {
                InitKeyboardWatcher();
            } else {
                SettingsHolder.Drive.PropertyChanged += (sender, args) => {
                    if (args.PropertyName == nameof(SettingsHolder.Drive.ShowCspSettingsWithShortcut)) {
                        InitKeyboardWatcher();
                    }
                };
            }

#if DEBUG
            Hibernated = true;
#endif
        }

        private bool _added;

        private void UpdateGameListeners() {
            if (SettingsHolder.Drive.WatchForSharedMemory) {
                if (!_added) {
                    AcSharedMemory.Instance.Start += OnStart;
                    AcSharedMemory.Instance.Finish += OnFinish;
                    AcSharedMemory.Instance.MonitorFramesPerSecondBegin += OnStart;
                    AcSharedMemory.Instance.MonitorFramesPerSecondEnd += OnFinish;
                    GameWrapper.Started += OnGameWrapperStarted;
                    GameWrapper.Ended += OnGameWrapperEnded;
                    _added = true;
                }
            } else if (_added) {
                AcSharedMemory.Instance.Start -= OnStart;
                AcSharedMemory.Instance.Finish -= OnFinish;
                AcSharedMemory.Instance.MonitorFramesPerSecondBegin -= OnStart;
                AcSharedMemory.Instance.MonitorFramesPerSecondEnd -= OnFinish;
                GameWrapper.Started -= OnGameWrapperStarted;
                GameWrapper.Ended -= OnGameWrapperEnded;
                _added = false;
            }
        }

        private bool _raceStartedByCm;

        private void OnGameWrapperStarted(object sender, GameStartedArgs e) {
            _raceStartedByCm = true;
        }

        private async void OnGameWrapperEnded(object sender, GameEndedArgs e) {
            await Task.Delay(5000);
            _raceStartedByCm = false;
        }

        private void Drive_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(SettingsHolder.Drive.WatchForSharedMemory)) {
                UpdateGameListeners();
            }
        }

        private IList<Window> _hiddenWindows;
        private Avalonia.Controls.TrayIcon _trayIcon;

        private static Avalonia.Controls.NativeMenuItem CreateMenuItem(string header, Action action) {
            var item = new Avalonia.Controls.NativeMenuItem { Header = header };
            item.Click += (sender, args) => action();
            return item;
        }

        private void AddTrayIcon() {
            if (_trayIcon != null) return;

            var menu = new Avalonia.Controls.NativeMenu();
            if (SettingsShadersPatch.IsCustomShadersPatchInstalled()) {
                menu.Items.Add(CreateMenuItem("Custom Shaders Patch settings", () => SettingsShadersPatch.GetShowSettingsCommand().Execute(null)));
            }

            if (RhmService.Instance.Active) {
                menu.Items.Add(CreateMenuItem("RHM settings", () => RhmService.Instance.ShowSettingsCommand.ExecuteAsync().Ignore()));
            }

            menu.Items.Add(new Avalonia.Controls.NativeMenuItemSeparator());
            menu.Items.Add(CreateMenuItem(UiStrings.Restore, WakeUp));
            menu.Items.Add(CreateMenuItem(UiStrings.Close, () => OnCloseMenuItemClick(null, EventArgs.Empty)));

            _trayIcon = new Avalonia.Controls.TrayIcon {
                Icon = new Avalonia.Controls.WindowIcon(Avalonia.Platform.AssetLoader.Open(AppIconService.GetTrayIconUri())),
                ToolTipText = AppStrings.Hibernate_TrayText,
                Menu = menu,
                IsVisible = true
            };

            _trayIcon.Clicked += OnTrayIconDoubleClick;
        }

        private void RemoveTrayIcon() {
            if (_trayIcon != null) {
                _trayIcon.IsVisible = false;
                Task.Delay(TimeSpan.FromSeconds(1d)).ContinueWith(r => ActionExtension.InvokeInMainThreadAsync(() => DisposeHelper.Dispose(ref _trayIcon)));
            }
        }

        private void OnRestoreMenuItemClick(object sender, EventArgs e) {
            WakeUp();
        }

        private void OnCloseMenuItemClick(object sender, EventArgs e) {
            var app = Application.Current;
            if (app == null) {
                Environment.Exit(0);
            } else {
                app.Shutdown();
            }
        }

        private void OnTrayIconDoubleClick(object sender, EventArgs e) {
            Hibernated = false;
        }

        private void WakeUp() {
            _raceStartedByCm = true;
            Hibernated = false;
        }

        private bool _hibernated;

        public bool Hibernated {
            get => _hibernated;
            set {
                if (Equals(value, _hibernated)) return;
                _hibernated = value;

                ActionExtension.InvokeInMainThreadAsync(() => {
                    try {
                        if (value) {
                            /* add an icon to the tray for manual restoration just in case */
                            AddTrayIcon();

                            /* hide windows */
                            _hiddenWindows = Application.Current?.Windows.OfType<Window>().Where(x => x.Visibility == Visibility.Visible
                                    && !x.Topmost).ToList();
                            if (_hiddenWindows != null) {
                                foreach (var window in _hiddenWindows) {
                                    window.Visibility = Visibility.Collapsed;
                                }
                            }
                        } else {
                            /* show hidden windows */
                            if (_hiddenWindows != null) {
                                foreach (var window in _hiddenWindows) {
                                    window.Visibility = Visibility.Visible;
                                }
                                _hiddenWindows = null;
                            }

                            if (_raceStartedByCm) {
                                (Application.Current?.MainWindow as DpiAwareWindow)?.BringToFront();
                            }

                            /* remove tray icon */
                            RemoveTrayIcon();
                        }
                    } catch (Exception e) {
                        Logging.Error(e);
                    }
                });
            }
        }

        private void OnStart(object sender, EventArgs e) {
            if (!SettingsHolder.Drive.HideWhileRacing) return;
            Hibernated = true;
        }

        private void OnFinish(object sender, EventArgs e) {
            Hibernated = false;
        }

        public void Dispose() {
            RemoveTrayIcon();
        }
    }
}