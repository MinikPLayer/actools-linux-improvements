#!/bin/bash
# Re-runs the WPF->Avalonia XAML conversion for the app (AcManager) from the last commit which still had the WPF sources.
# C# files are NOT touched here (they are edited by hand after the first conversion, see cs_rules.py for the mechanical part).
cd /project/AcManager
BASE=bc05004b
for f in $(git ls-tree -r --name-only $BASE . | grep '\.xaml$'); do mkdir -p $(dirname $f); git show $BASE:./$f > $f; done
../PortTools/extract_wpf_sources.sh
python3 ../PortTools/xaml_convert.py --asm AssettoCorsa . --scan /tmp/wpf_scan/FirstFloor.ModernUI --scan /tmp/wpf_scan/AcManager.Controls --all . > /tmp/conv_app.log 2>&1
tail -1 /tmp/conv_app.log
grep -rl -e '=}' --include='*.axaml' . | xargs -r sed -i "s/=}/=''}/g"
grep -rl 'icsharpcode.net/sharpdevelop/avalonedit' --include='*.axaml' . | xargs -r sed -i 's#http://icsharpcode.net/sharpdevelop/avalonedit#using:AvaloniaEdit#'
find . -name '*.xaml' -not -path './obj/*' -delete
[ -f ../PortTools/patches/App.py ] && python3 ../PortTools/apply_patches.py . ../PortTools/patches/App.py
