# Content Manager – Linux port notes

This file documents what has been done to port the app to Linux, what is left, and what can’t be ported (yet).
It is updated together with the code, newest sections at the bottom of each chapter.

## Packages installed in the dev container

Every package installed with `apt` while working on the port:

| Package | Why |
| --- | --- |
| `python3-lxml` (+ its dependencies `python3-bs4`, …) | XAML conversion scripts in `PortTools/` (WPF XAML → Avalonia XAML) |

NuGet packages are restored by the build itself (no manual installation needed).
