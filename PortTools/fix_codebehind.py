#!/usr/bin/env python3
"""
In WPF the base class of a XAML-backed partial class comes from the XAML root, in Avalonia it has to be declared in code-behind.
Usage: fix_codebehind.py <project dir> [...]  (looks for X.xaml.cs next to X.axaml)
"""
import os, re, sys
from lxml import etree

NS_AV = 'https://github.com/avaloniaui'
AV_NAMESPACES = ['Avalonia.Controls', 'Avalonia.Controls.Primitives', 'Avalonia.Controls.Shapes']
xmlns_map = {}   # uri -> [clr namespaces]


def load_xmlns(root_dir):
    for r, d, f in os.walk(root_dir):
        if '/obj' in r or '/bin' in r or '/Publish' in r:
            continue
        for fn in f:
            if fn == 'AssemblyInfo.cs':
                s = open(os.path.join(r, fn), encoding='utf-8-sig').read()
                for m in re.finditer(r'XmlnsDefinition\("([^"]+)",\s*"([^"]+)"\)', s):
                    xmlns_map.setdefault(m.group(1), []).append(m.group(2))


def resolve(uri, name, type_index):
    if uri == NS_AV:
        return {'UserControl': 'Avalonia.Controls.UserControl', 'Window': 'Avalonia.Controls.Window', 'Page': 'Avalonia.Controls.UserControl'}.get(name)
    if uri.startswith('clr-namespace:'):
        return '%s.%s' % (uri[len('clr-namespace:'):].split(';')[0], name)
    if uri.startswith('using:'):
        return '%s.%s' % (uri[len('using:'):], name)
    for ns in xmlns_map.get(uri, []):
        full = '%s.%s' % (ns, name)
        if full in type_index:
            return full
    return None


def build_type_index(root_dir):
    idx = set()
    rx = re.compile(r'^\s*namespace\s+([\w.]+)|^\s*(?:public |internal |protected |abstract |sealed |static |partial )*(?:class|struct|enum|interface)\s+(\w+)', re.M)
    for r, d, f in os.walk(root_dir):
        if '/obj' in r or '/bin' in r or '/Publish' in r:
            continue
        for fn in f:
            if fn.endswith('.cs'):
                try:
                    s = open(os.path.join(r, fn), encoding='utf-8-sig').read()
                except Exception:
                    continue
                ns = None
                for m in rx.finditer(s):
                    if m.group(1):
                        ns = m.group(1)
                    elif ns:
                        idx.add('%s.%s' % (ns, m.group(2)))
    return idx


def main(dirs):
    root = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
    load_xmlns(root)
    type_index = build_type_index(root)
    fixed = 0
    for d in dirs:
        for r, ds, fs in os.walk(d):
            if '/obj' in r or '/bin' in r or '/Publish' in r:
                continue
            for fn in fs:
                if not fn.endswith('.xaml.cs'):
                    continue
                cs = os.path.join(r, fn)
                ax = os.path.join(r, fn[:-len('.xaml.cs')] + '.axaml')
                if not os.path.exists(ax):
                    continue
                tree = etree.parse(ax)
                rt = tree.getroot()
                q = etree.QName(rt.tag)
                cls = rt.get('{http://schemas.microsoft.com/winfx/2006/xaml}Class')
                if not cls:
                    continue
                base = resolve(q.namespace, q.localname, type_index)
                if not base:
                    print('cannot resolve base of', cs, q.namespace, q.localname)
                    continue
                short = cls.split('.')[-1]
                raw = open(cs, 'rb').read()
                bom = raw.startswith(b'\xef\xbb\xbf')
                s = raw.decode('utf-8-sig')
                m = re.search(r'(partial\s+class\s+%s)(\s*:\s*([^{]+?))?(\s*(?:where [^{]+)?)\{' % re.escape(short), s)
                if not m:
                    print('class not found in', cs)
                    continue
                bases = m.group(3)
                gbase = 'global::' + base
                if bases and (base in bases or base.split('.')[-1] in [b.strip().split('<')[0] for b in bases.split(',')]):
                    continue
                if bases:
                    new = '%s : %s, %s%s{' % (m.group(1), gbase, bases.strip(), m.group(4) if m.group(4).strip() else ' ')
                else:
                    new = '%s : %s {' % (m.group(1), gbase)
                s = s[:m.start()] + new + s[m.end():]
                open(cs, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))
                fixed += 1
    print('fixed', fixed)


if __name__ == '__main__':
    main(sys.argv[1:])
