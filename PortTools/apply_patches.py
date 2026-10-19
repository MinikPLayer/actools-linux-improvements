#!/usr/bin/env python3
"""
Applies manual fixes to converted XAML files, so conversion can be re-run any time.
Usage: apply_patches.py <project dir> <patch module>   (patch module defines PATCHES = [(relative file, [(old, new) | ('re', pattern, repl)])])
"""
import sys, os, re, importlib.util

proj, mod = sys.argv[1], sys.argv[2]
spec = importlib.util.spec_from_file_location('patches', mod)
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)
bad = 0
for rel, items in m.PATCHES:
    p = os.path.join(proj, rel)
    if not os.path.exists(p):
        print('missing file', rel)
        bad += 1
        continue
    s = open(p, encoding='utf-8-sig').read()
    for it in items:
        if it[0] == 're':
            n = re.sub(it[1], it[2], s, flags=re.S)
            if n == s:
                print('  no match in %s: %s' % (rel, it[1][:60]))
                bad += 1
            s = n
        else:
            if it[0] not in s:
                print('  not found in %s: %s' % (rel, it[0][:60].replace('\n', ' ')))
                bad += 1
            s = s.replace(it[0], it[1])
    open(p, 'w', encoding='utf-8-sig').write(s)
print('patched, problems:', bad)
