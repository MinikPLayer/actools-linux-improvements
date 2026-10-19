"""
Tiny parser/serializer for XAML markup extensions: {Binding Path, Mode=TwoWay, Converter={StaticResource X}}
"""
import re


class Markup:
    def __init__(self, name, args=None, kwargs=None):
        self.name = name
        self.args = args or []          # positional values (str or Markup)
        self.kwargs = kwargs or []      # list of [key, value] to keep order

    def get(self, key, default=None):
        for k, v in self.kwargs:
            if k == key:
                return v
        return default

    def has(self, key):
        return any(k == key for k, _ in self.kwargs)

    def set(self, key, value):
        for kv in self.kwargs:
            if kv[0] == key:
                kv[1] = value
                return
        self.kwargs.append([key, value])

    def remove(self, key):
        self.kwargs = [kv for kv in self.kwargs if kv[0] != key]

    def __str__(self):
        parts = []
        for a in self.args:
            parts.append(str(a))
        for k, v in self.kwargs:
            parts.append('%s=%s' % (k, v))
        head = self.name
        if parts:
            return '{' + head + ' ' + ', '.join(parts) + '}'
        return '{' + head + '}'


def is_markup(s):
    return s is not None and s.startswith('{') and s.endswith('}') and not s.startswith('{}')


def _split_top(s):
    """split by commas on top level (respecting braces and quotes)"""
    out, depth, cur, q = [], 0, [], None
    i = 0
    while i < len(s):
        c = s[i]
        if q:
            cur.append(c)
            if c == q:
                q = None
        elif c in '\'"':
            q = c
            cur.append(c)
        elif c == '{':
            depth += 1
            cur.append(c)
        elif c == '}':
            depth -= 1
            cur.append(c)
        elif c == ',' and depth == 0:
            out.append(''.join(cur).strip())
            cur = []
        else:
            cur.append(c)
        i += 1
    last = ''.join(cur).strip()
    if last:
        out.append(last)
    return out


def _find_eq(s):
    depth, q = 0, None
    for i, c in enumerate(s):
        if q:
            if c == q:
                q = None
        elif c in '\'"':
            q = c
        elif c == '{':
            depth += 1
        elif c == '}':
            depth -= 1
        elif c == '=' and depth == 0:
            return i
    return -1


def parse(s):
    """Parse '{Name ...}' into Markup (nested values are parsed too if they are markup)."""
    s = s.strip()
    inner = s[1:-1].strip()
    m = re.match(r'([^\s,{}=]+)\s*(.*)$', inner, re.S)
    name, rest = m.group(1), m.group(2)
    mk = Markup(name)
    for part in _split_top(rest):
        eq = _find_eq(part)
        if eq > 0 and not part.startswith('{'):
            k, v = part[:eq].strip(), part[eq + 1:].strip()
            mk.kwargs.append([k, parse(v) if is_markup(v) else v])
        else:
            mk.args.append(parse(part) if is_markup(part) else part)
    return mk
