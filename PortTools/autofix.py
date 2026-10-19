#!/usr/bin/env python3
"""
Applies mechanical fixes for compiler errors: python3 autofix.py <errors.txt>
 - CS0103 for names which are extension properties/methods in the compat layer: prefix with `this.`
"""
import re, sys, collections

THIS_NAMES = {'CommandBindings', 'InputBindings', 'Visibility', 'ActualWidth', 'ActualHeight', 'GetTemplateChild', 'FindResource', 'TryFindResource',
              'FindName', 'Left', 'Top', 'ResizeMode', 'SetBinding', 'BeginInvoke'}
fixes = collections.defaultdict(list)
for l in open(sys.argv[1]):
    m = re.match(r"(.+?)\((\d+),(\d+)\): error CS0103: The name '(\w+)' does not exist", l)
    if m and m.group(4) in THIS_NAMES:
        fixes[m.group(1)].append((int(m.group(2)), int(m.group(3)), m.group(4)))
total = 0
for path, items in fixes.items():
    raw = open(path, 'rb').read()
    bom = raw.startswith(b'\xef\xbb\xbf')
    lines = raw.decode('utf-8-sig').split('\n')
    for line, col, name in sorted(set(items), reverse=True):
        t = lines[line - 1]
        if t[col - 1:col - 1 + len(name)] == name and not t[:col - 1].endswith('this.'):
            lines[line - 1] = t[:col - 1] + 'this.' + t[col - 1:]
            total += 1
    open(path, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + '\n'.join(lines).encode('utf-8'))
print('fixed', total)
