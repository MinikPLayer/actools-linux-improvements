#!/usr/bin/env python3
"""
Extension members (compat layer) can't be called without a receiver. For every CS0103 error about a name which is provided by the
compat layer as an extension member, prefix the call with 'this.'.
Usage: qualify_this.py <errors file> name1,name2,...
"""
import re, sys, collections
errs = open(sys.argv[1]).read().splitlines()
names = set(sys.argv[2].split(','))
todo = collections.defaultdict(list)
for e in errs:
    m = re.match(r"(.+?)\((\d+),(\d+)\): error CS0103: The name '(\w+)' does not exist", e)
    if m and m.group(4) in names and not m.group(1).startswith('/project/') is False:
        todo[m.group(1)].append((int(m.group(2)), int(m.group(3)), m.group(4)))
for f, items in todo.items():
    lines = open(f, encoding='utf-8-sig').read().split('\n')
    for ln, col, name in sorted(items, reverse=True):
        l = lines[ln - 1]
        if l[col - 1:col - 1 + len(name)] == name:
            lines[ln - 1] = l[:col - 1] + 'this.' + l[col - 1:]
    open(f, 'w', encoding='utf-8').write('\n'.join(lines))
    print('patched', f, len(items))
