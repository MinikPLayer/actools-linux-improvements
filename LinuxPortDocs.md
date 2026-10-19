# Content Manager – Linux port notes

This file documents what has been done to port the app to Linux, what is left, and what can’t be ported (yet).
It is updated together with the code, newest sections at the bottom of each chapter.

## Packages installed in the dev container

Every package installed with `apt` while working on the port:

| Package | Why |
| --- | --- |
| `python3-lxml` (+ its dependencies `python3-bs4`, …) | XAML conversion scripts in `PortTools/` (WPF XAML → Avalonia XAML) |

NuGet packages are restored by the build itself (no manual installation needed).

## Status (work in progress)

Builds on Linux: AcTools*, StringBasedFilter, AcManager.Tools, AcManager.ContentRepair/AcSound/LargeFilesSharing/Workshop/DiscordRpc, LicensePlates.
FirstFloor.ModernUI: C# builds; the tier-0 theme renders in a demo window. The XAML of the remaining ModernUI assets is being
un-deferred (see `FirstFloor.ModernUI/Deferred.props`); the last commit has ~88 XAML errors in HierarchicalComboBox, ProgressBar,
TagsList, AsyncButton, ButtonWithComboBox, RoundSlider, ComboBox, TreeView, Expander (WIP, not a clean build).
AcManager.Controls: csproj converted to Avalonia, XAML converted; C# not yet building. AcManager (app): not started.

### Tooling
- `PortTools/xaml_convert.py`, `cs_rules.py`, `fix_codebehind.py`, `drop_dup_aliases.py`, `apply_patches.py` (+ `patches/`).
- `PortTools/gen_api_json.sh` dumps Avalonia's public members to `api.json` (git-ignored); the converter drops setters/attributes
  Avalonia doesn't have.

### Not ported / replaced (documented deviations)
- Windows-only: CefSharp web browser, VLC video, SlimDX/D3D render forms, taskbar, UAC icon, IE WebBrowser (stub), registry features.
- Replaced by Avalonia defaults for now: DataGrid, DatePicker, Calendar, ListView, MenuItem, ToolBar, PasswordBox (TextBox with PasswordChar).
- Texture effects (shaders) reimplemented on pixels (`AcManager.Tools/Helpers/TextureEffects.cs`).
- Toast: notify-send on Linux, otherwise a small borderless window.
- LicensePlates Lua `showMsg` writes to stderr.

### apt installs
- python3-lxml (and dependencies such as python3-bs4) for the XAML converter. No other installs.
