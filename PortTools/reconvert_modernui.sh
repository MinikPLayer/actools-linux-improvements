#!/bin/bash
# re-run WPF->Avalonia XAML conversion for ModernUI from pristine git sources (commit before the port started)
# WARNING: do not run it anymore - ModernUI axaml files were edited by hand after the conversion (ProgressBar, ToolBar, Hyperlink, ...)
exit 1
cd /project/FirstFloor.ModernUI
BASE=5570fd95
for d in Assets Themes Dialogs; do
  for f in $(git ls-tree --name-only $BASE $d/ | grep '\.xaml$'); do git show $BASE:FirstFloor.ModernUI/$f > $f; done
done
python3 ../PortTools/xaml_convert.py --asm FirstFloor.ModernUI . --all Assets --all Themes --all Dialogs > /tmp/conv_mui.log 2>&1
tail -1 /tmp/conv_mui.log
python3 ../PortTools/apply_patches.py . ../PortTools/patches/ModernUI.py
for d in Assets Themes Dialogs; do rm -f $d/*.xaml; done
