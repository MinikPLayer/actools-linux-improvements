#!/usr/bin/env python3
"""Adds [Avalonia.Metadata.Content] to the property named by a WPF [ContentProperty(..)] class attribute (the Avalonia XAML compiler ignores the latter)."""
import re, sys, glob
for f in sys.argv[1:]:
    s = open(f, encoding='utf-8-sig').read()
    m = re.search(r'\[ContentProperty\((?:nameof\()?@?"?(\w+)"?\)?\)', s)
    if not m: continue
    name = m.group(1)
    if name in ('Content',) and not re.search(r'class \w+\s*:\s*(?!.*ContentControl)', s): continue
    if f'[Avalonia.Metadata.Content] ' in s: continue
    pat = re.compile(r'^(\s*)((?:\[[^\]\n]+\]\s*)*)(public\s+(?:virtual\s+|override\s+)?[\w<>\[\],.? ]+?\s+' + name + r'\s*(?:\{|=>))', re.M)
    pm = pat.search(s)
    if not pm:
        print('no property', f, name); continue
    s = s[:pm.start(3)] + '[Avalonia.Metadata.Content] ' + s[pm.start(3):]
    open(f, 'w', encoding='utf-8').write(s)
    print('ok', f, name)
