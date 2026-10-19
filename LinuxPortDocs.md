# Content Manager – Linux port notes

This file documents what has been done to port the app to Linux (Avalonia 11.3), what is left, and what can’t be ported (yet).

## Packages installed in the dev container

Every package installed with `apt` while working on the port:

| Package | Why |
| --- | --- |
| `python3-lxml` (+ dependencies such as `python3-bs4`) | XAML conversion scripts in `PortTools/` (WPF XAML → Avalonia XAML) |
| `libx11-dev libxi-dev libxrandr-dev libxcursor-dev libxext-dev libxfixes-dev libsm-dev libice-dev` | Avalonia’s X11 backend `dlopen`s `libX11.so` & co (the unversioned names come only with `-dev` packages). Normal desktop distributions already have them |
| `xdotool` | Only for testing: moving/clicking the mouse in XWayland (screenshots are made with `grim`) |

NuGet packages are restored by the build itself (no manual installation needed). At runtime on a normal Linux desktop only
the .NET 10 runtime and X11/Wayland (XWayland) libraries are needed.

## Building and running

```
dotnet build AcManager/AcManager.csproj -m:1
cd Output/x64/Debug && dotnet AssettoCorsa.dll
```

Environment variables for debugging: `CM_AVALONIA_LOG=1` (Avalonia trace log to console), `CM_DUMP_TREE=<seconds>`
(dumps visual trees of open windows to `/tmp/cm_tree.txt`).

The app still targets Windows as well: platform-specific code is guarded with `OperatingSystem.IsWindows()` / `#if`, and Windows
build paths of the libraries are unchanged. The UI is Avalonia on both platforms (WPF is gone from the code base).

## Approach

The project (originally WPF, .NET 10) was converted to Avalonia while keeping the XAML layout, themes, control names and code-behind as
close to the original as possible.

- **`FirstFloor.ModernUI/Compat/*.cs`** and `Build/AvaloniaCompat.props` (global `using` aliases) provide a thin WPF-compatibility layer
  (`DependencyProperty` → `StyledProperty` wrapper `DP.Register`, `FrameworkElement`, mouse/keyboard event args, `ResourceDictionary`,
  `Visibility`, `Dispatcher`, …), so most code-behind compiles unchanged.
- **`PortTools/`** – the conversion pipeline (python, needs `python3-lxml`):
  `xaml_convert.py` (+`markup.py`) converts WPF XAML into Avalonia `.axaml` (triggers → styles with pseudo-classes or *state properties*
  `pa:T.S0…`, templates, `DataTemplate`s, event handler signatures, xmlns hoisting, removal of members Avalonia doesn’t have – `api.json` is a
  dump of Avalonia’s public API made by `gen_api_json.sh`), `fix_codebehind.py`/`cs_rules.py`/`cs_app_rules.py` adjust handler signatures in
  `.xaml.cs`, `gen_names.py` generates `*.Names.cs` for named non-control objects (links, groups, DataGrid columns).
- **`PortTools/reconvert_app.sh`** regenerates *all* AcManager `.axaml` from the pristine WPF XAML (git revision `bc05004b`) and then applies
  `patches/App.py`. **Hand edits to generated AcManager `.axaml` files are lost on reconversion** – put them into the converter or the patch
  file. ModernUI/AcManager.Controls `.axaml` were converted once and edited by hand afterwards (do not rerun `reconvert_modernui.sh`).
- Generated command bindings (`*.CommandBindings.g.cs`) emulate `<CommandBindings>`, registered via `ModuleInitializer`.

## Deviations from the Windows application (not portable / replaced)

Anything marked “stub” keeps compiling and the feature is hidden or disabled.

- **Embedded browser** (CefSharp, IE `WebBrowser`): not available. `FirstFloor.ModernUI` web pages show a stub; links open in the system browser.
- **VLC video** (`LibVLCSharp` WPF host): Windows only.
- **Custom Showroom / 3D rendering** (SlimDX/SharpDX D3D11 on a WPF `D3DImage`): Windows only. `IncludeShowrooms` property + `ShowroomStubs.cs`
  keep the rest of the app building; `D3DImage` is a no-op. `LiteShowroomSettings.axaml` is kept for settings pages.
- **Closed-source `AcManager.Internal.dll`** (32-bit .NET Framework, cannot be loaded by 64-bit .NET 10): replaced by the open stub project
  `AcManager.Internal.Stub` (same assembly name & API; `IsAllRight=false`, keys null, simple HTTP for CM API, no shared entries upload).
  `AcTools.AcdEncryption` and `AcTools.Kn5Tools` (closed-source) are optional – the app falls back to basic implementations (log warnings are expected).
- **Windows-only P/Invoke / registry / WMI / COM** (taskbar, Kernel32 `SetDllDirectory`, Discord named pipe, FaultTolerantHeap, URI scheme
  registration, Start-menu shortcut, shared memory with the game, `ShellLink`): guarded or failing gracefully (logged as warnings in the log).
  Named memory maps (AC shared memory) are not supported by .NET on Linux.
- **Steam / AC root discovery**: `AcRootFinder` additionally searches native Linux Steam (`~/.steam/steam`, `~/.local/share/Steam`, Flatpak) including
  `libraryfolders.vdf`. The Windows-path “Z:\home” (Proton/Wine) handling is kept. Starting the game on Linux is not part of this port.
- **Second-instance handling**: Windows messages (`RegisterWindowMessage`/`PostMessage`) are used only on Windows; on other systems
  a `FileSystemWatcher` on the communication file is used.
- **Tray icon**: API changed to Avalonia’s `TrayIcon`; needs a StatusNotifier host on Linux (not available without D-Bus).
- **Toast notifications**: `notify-send` on Linux, otherwise a small borderless window.
- **Texture effects** (shaders): reimplemented on pixels (`AcManager.Tools/Helpers/TextureEffects.cs`). `System.Drawing.Common` is not supported on Linux
  – `ImageMagick`/Avalonia bitmaps are used where it mattered.
- **Window management**: WPF `ContentRendered` and `GetMousePosition` are emulated; `ShowDialog` runs a nested dispatcher frame (`NestedLoop`),
  `FileRelatedDialogs` wrap Avalonia’s async storage provider into blocking calls. Mouse tracking is a static tracker (`VisualExtension.Mouse.cs`).
- **Draggable**: ported to `DragDrop.DoDragDrop` without the drag preview image.
- **Tile panels**: simplified (`TilePanels.cs`), not virtualizing yet.
- **DataGrid**: Avalonia DataGrid with the original column definitions (`ColumnNames` attached `Name`); sort arrows mapped to pseudo-classes;
  header/cell theming is still default.
- **Not carried over from XAML** (Avalonia has no equivalent): `EventSetter`, `GroupStyle`, `ToolTip` on `Run`, `Popup.Placement=Absolute`,
  `VisualStateManager`, storyboard actions of triggers (marked `<!-- PORT: … -->` in the converted XAML), `TickPlacement=Both` (→ Outside),
  `x:Reference` (→ `ElementName`), `Duration` (→ `x:TimeSpan`), trigger comparison with resources (→ state properties).
- **`Assets/Development/**`** XAML of AcManager is excluded from the build.
- **Basic control templates** come from Avalonia’s Simple theme (`App.Initialize` adds `SimpleTheme`), ModernUI themes override them.

## Status

Whole solution part needed for the app builds (AcTools*, AcManager.Tools, AcManager.Controls, AcManager) – 0 errors. The app starts on
Linux (X11/XWayland), shows the AC root selector and the main window (Drive page).

Still deferred (see `FirstFloor.ModernUI/Deferred.props`): FancyHint/FancyScroll and adorners (Watermark/NewMark/LimitedMark), SplitButton,
OutlinedTextBlock, BbCode parser extras/Emoji, virtualizing panels, DataGrid helpers/cell styles, ColorPickerPanel, ScreenColorPicker,
Calendar/DatePicker/ListView/MenuItem/PasswordBox themes. Other projects still WPF/Windows only: CustomShowroom, PreviewUpdater,
CustomPreviewUpdater, CustomTracksBakery, LicensePlatesGenerator, FirstFloor.ModernUI.Win8Extension, tests.

Known visual issues (work in progress): some links lists are rendered via `ToString()`, text clipping in a few sliders.
