#!/bin/bash
# re-run WPF->Avalonia XAML conversion for AcManager.Controls from the last commit which still had the WPF sources
cd /project/AcManager.Controls
BASE=cdeb0eb4^
for f in $(git ls-tree -r --name-only $BASE . | grep '\.xaml$'); do mkdir -p $(dirname $f); git show $BASE:./$f > $f; done
python3 ../PortTools/xaml_convert.py --asm AcManager.Controls . --scan ../FirstFloor.ModernUI . --all . > /tmp/conv_controls.log 2>&1
tail -1 /tmp/conv_controls.log
grep -rl -e '=}' --include='*.axaml' . | xargs -r sed -i "s/=}/=''}/g"
find . -name '*.xaml' -not -path './obj/*' -delete
