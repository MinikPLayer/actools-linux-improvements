#!/usr/bin/env python3
"""Mechanical C# fixes for the app (AcManager): namespaces which don't exist anymore. Idempotent. Usage: cs_app_rules.py <dir>"""
import os, re, sys

RULES = [
    (r'^using System\.Windows\.Forms\.Integration;\r?\n', ''),
    (r'^using FirstFloor\.ModernUI\.Win32;\r?\n', ''),
    (r'^using System\.Windows\.Forms;', 'using AcTools.Windows.Compat;'),
    (r'^using OxyPlot\.Wpf;', 'using OxyPlot.Avalonia;'),
    (r'^using Keys = System\.Windows\.Forms\.Keys;', 'using Keys = AcTools.Windows.Compat.Keys;'),
    (r'^[ \t]*\[PermissionSet\(SecurityAction\.Demand, Name = "FullTrust"\), ComVisible\(true\)\]\r?\n', '        [ComVisible(true)]\n'),
]

def process(path):
    raw = open(path, 'rb').read()
    bom = raw.startswith(b'\xef\xbb\xbf')
    s = raw.decode('utf-8-sig')
    o = s
    for pat, rep in RULES:
        s = re.sub(pat, rep, s, flags=re.M)
    # a file must not have the same using twice
    lines = s.split('\n')
    seen = set()
    out = []
    for l in lines:
        t = l.strip()
        if t.startswith('using AcTools.Windows.Compat;'):
            if t in seen:
                continue
            seen.add(t)
        out.append(l)
    s = '\n'.join(out)
    if s != o:
        open(path, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))
        return 1
    return 0

n = 0
for root in sys.argv[1:]:
    for r, ds, fs in os.walk(root):
        if '/obj' in r or '/bin' in r:
            continue
        for f in fs:
            if f.endswith('.cs'):
                n += process(os.path.join(r, f))
print('changed', n)
