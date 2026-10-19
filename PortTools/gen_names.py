#!/usr/bin/env python3
"""
Avalonia's name generator creates fields only for named controls. Named non-controls (links, groups, resources...) are registered in the
name scope though. For every 'name does not exist' error (CS0103) which matches an x:Name of the page's axaml, this creates
X.Names.cs with a property which looks the object up in the name scope.
Usage: gen_names.py <errors file> <project dir>
"""
import os, re, sys, collections
from lxml import etree
sys.path.insert(0, os.path.dirname(__file__))
import fix_codebehind as fc

errs, proj = sys.argv[1], sys.argv[2]
root = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
fc.load_xmlns(root)
idx = fc.build_type_index(root)
X = '{http://schemas.microsoft.com/winfx/2006/xaml}'
want = collections.defaultdict(set)
for l in open(errs):
    m = re.match(r"(\S+\.xaml\.cs)\(\d+,\d+\): error CS0103: The name '(\w+)'", l)
    if m: want[m.group(1)].add(m.group(2))
    m = re.match(r"(\S+\.xaml\.cs)\(\d+,\d+\): error CS0120: .*?property '(\w+)\.", l)
    if m: want[m.group(1)].add(m.group(2))
for cs, names in want.items():
    ax = cs[:-len('.xaml.cs')] + '.axaml'
    if not os.path.exists(ax): continue
    tree = etree.parse(ax)
    cls = tree.getroot().get(X + 'Class')
    if not cls: continue
    props = []
    for el in tree.iter():
        n = el.get(X + 'Name')
        if n in names:
            q = etree.QName(el.tag)
            t = fc.resolve(q.namespace, q.localname, idx)
            if not t and q.namespace == fc.NS_AV:
                t = 'Avalonia.Controls.' + q.localname
            if t: props.append((n, t))
    if not props: continue
    ns, _, short = cls.rpartition('.')
    out = os.path.join(os.path.dirname(cs), os.path.basename(cs)[:-len('.xaml.cs')] + '.Names.cs')
    body = '\n'.join('        private global::%s _%s_;\n        internal global::%s %s => _%s_ ?? (_%s_ = Avalonia.Controls.NameScopeExtensions.FindNameScope(this)?.Find("%s") as global::%s);\n' % (t, n, t, n, n, n, n, t) for n, t in props)
    open(out, 'w', encoding='utf-8').write('// Named objects which are not controls (no generated fields for them), see PortTools/gen_names.py\nnamespace %s {\n    public partial class %s {\n%s    }\n}\n' % (ns, short, body))
    print('written', out, [p[0] for p in props])
