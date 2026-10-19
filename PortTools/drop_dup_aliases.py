#!/usr/bin/env python3
"""
Removes file-level `using X = ...;` lines which duplicate global aliases of Build/AvaloniaCompat.props
(compiler reports them as CS1537). Usage: drop_dup_aliases.py <file with build errors>
"""
import re, sys
by = {}
for l in open(sys.argv[1]):
    m = re.match(r'(\S+\.cs)\((\d+),\d+\): error CS1537', l)
    if m: by.setdefault(m.group(1), set()).add(int(m.group(2)))
for f, ls in by.items():
    raw = open(f, 'rb').read(); bom = raw.startswith(b'\xef\xbb\xbf'); L = raw.decode('utf-8-sig').split('\n')
    for i in sorted(ls, reverse=True):
        if L[i-1].lstrip().startswith('using '): del L[i-1]
    open(f, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + '\n'.join(L).encode())
print('files:', len(by))
