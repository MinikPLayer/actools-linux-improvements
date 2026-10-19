#!/usr/bin/env python3
"""
Mechanical WPF -> Avalonia(compat) rewrites for C# sources. Idempotent, safe to re-run.
Usage: cs_rules.py <dir or file> [...]
"""
import re, sys, os

RULES = [
    # dependency properties go through the compat layer (see FirstFloor.ModernUI/Compat)
    (r'\bDependencyProperty\.RegisterAttachedReadOnly\(', 'DP.RegisterAttachedReadOnly('),
    (r'\bDependencyProperty\.RegisterAttached\(', 'DP.RegisterAttached('),
    (r'\bDependencyProperty\.RegisterReadOnly\(', 'DP.RegisterReadOnly('),
    (r'\bDependencyProperty\.Register\(', 'DP.Register('),
    (r'\bDependencyProperty\.UnsetValue\b', 'Avalonia.AvaloniaProperty.UnsetValue'),
    # SetValue(FooPropertyKey, ...) inside of instance methods
    (r'(?<![\w.])SetValue\((\w+PropertyKey)\b(?!\.)', r'SetValue(\1.DependencyProperty'),
    (r'(?<![\w.])SetCurrentValue\((\w+PropertyKey)\b(?!\.)', r'SetCurrentValue(\1.DependencyProperty'),
    # FooProperty.AddOwner(typeof(X)) / OverrideMetadata
    (r'\b(\w+(?:\.\w+)*Property)\.AddOwner\(typeof\((\w+)\)\)', r'DP.AddOwner(\1, typeof(\2))'),
    (r'\b(\w+(?:\.\w+)*Property)\.AddOwner\(typeof\((\w+)\),', r'DP.AddOwner(\1, typeof(\2),'),
    (r'\b(\w+(?:\.\w+)*Property)\.OverrideMetadata\(', None),  # handled below
    # event registration
    # default style keys are resolved by the type in Avalonia
    (r'[ \t]*DP\.OverrideMetadata\(DefaultStyleKeyProperty, typeof\(\w+\), new FrameworkPropertyMetadata\(typeof\(\w+\)\)\);[ \t]*\r?\n', ''),
    (r'[ \t]*(?:this\.)?DefaultStyleKey = typeof\(\w+\);[ \t]*\r?\n', ''),
    (r'\bInternalChildren\b', 'Children'),
    (r'\bPlacementMode\.MousePoint\b', 'PlacementMode.Pointer'),
    (r'(?<![\w.])(\w+(?:\.\w+)*)\.Style = \(Style\)(?:this\.)?(?:\w+\.)?(?:Try)?FindResource\((@?"[^"]+")\);', r'\1.SetStyle(\2);'),
    (r'(?<![\w.])Style = \(Style\)(?:this\.)?(?:\w+\.)?(?:Try)?FindResource\((@?"[^"]+")\);', r'this.SetStyle(\1);'),
    (r'(?<![\w.])Dispatcher\.(Invoke|BeginInvoke|InvokeAsync|CheckAccess|VerifyAccess)\(', r'Dispatcher.UIThread.\1('),
    # template hooks
    (r'(public|protected) override void OnApplyTemplate\(\)\s*\{', r'protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e) {\n            this.RememberTemplateScope(e);'),
    (r'\bbase\.OnApplyTemplate\(\)', 'base.OnApplyTemplate(e)'),
    (r'(?<![\w.])GetTemplateChild\(', 'this.GetTemplateChild('),
    (r'OnPropertyChanged\(DependencyPropertyChangedEventArgs ', 'OnPropertyChanged(Avalonia.AvaloniaPropertyChangedEventArgs '),
]

_EVENT_NAMES = [l.strip() for l in open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'wpf_event_names.txt')) if l.strip()]
_EVENT_RX = re.compile(r'(?:(?P<dot>\.)|(?<![\w.]))(?P<name>%s)\s*(?P<sign>[+-])=\s*' % '|'.join(sorted(_EVENT_NAMES, key=len, reverse=True)))


def _find_stmt_end(text, i):
    depth = 0
    q = None
    n = len(text)
    while i < n:
        c = text[i]
        if q:
            if c == '\\' and q == '"':
                i += 1
            elif c == q:
                q = None
        elif c in '"\'':
            q = c
        elif c in '({[':
            depth += 1
        elif c in ')}]':
            depth -= 1
        elif c == ';' and depth <= 0:
            return i
        i += 1
    return -1


def rewrite_events(text):
    pos = 0
    while True:
        m = _EVENT_RX.search(text, pos)
        if not m:
            return text
        end = _find_stmt_end(text, m.end())
        if end < 0:
            return text
        rhs = text[m.end():end].strip()
        # the left side must not be a declaration of an event
        before = text[max(0, m.start() - 20):m.start()]
        verb = 'Add' if m.group('sign') == '+' else 'Remove'
        call = '%s%s(%s)' % (verb, m.group('name'), rhs)
        new = ('.' + call) if m.group('dot') else ('this.' + call)
        text = text[:m.start()] + new + text[end:]
        pos = m.start() + len(new)


# ---------------------------------------------------------------- overrides of WPF input virtuals
# name -> (avalonia routed event, routing, button filter or None, kind)
PREVIEW_OVERRIDES = {}
for _pre, _routing in (('OnPreview', 'Tunnel'),):
    PREVIEW_OVERRIDES[_pre + 'KeyDown'] = ('InputElement.KeyDownEvent', _routing, None)
    PREVIEW_OVERRIDES[_pre + 'KeyUp'] = ('InputElement.KeyUpEvent', _routing, None)
    PREVIEW_OVERRIDES[_pre + 'TextInput'] = ('InputElement.TextInputEvent', _routing, None)
    PREVIEW_OVERRIDES[_pre + 'MouseWheel'] = ('InputElement.PointerWheelChangedEvent', _routing, None)
    PREVIEW_OVERRIDES[_pre + 'MouseMove'] = ('InputElement.PointerMovedEvent', _routing, None)
    for _b in ('', 'Left', 'Right', 'Middle'):
        PREVIEW_OVERRIDES[_pre + 'Mouse' + _b + ('Button' if _b else '') + 'Down'] = ('InputElement.PointerPressedEvent', _routing, _b or None)
        PREVIEW_OVERRIDES[_pre + 'Mouse' + _b + ('Button' if _b else '') + 'Up'] = ('InputElement.PointerReleasedEvent', _routing, _b or None)
    PREVIEW_OVERRIDES['OnMouseWheel_'] = None
del PREVIEW_OVERRIDES['OnMouseWheel_']

# plain renames to Avalonia’s virtuals: wpf name -> (avalonia name, avalonia arg type, wpf-arg-type-name, button filter)
SIMPLE_OVERRIDES = {
    'OnMouseEnter': ('OnPointerEntered', 'PointerEventArgs', None),
    'OnMouseLeave': ('OnPointerExited', 'PointerEventArgs', None),
    'OnMouseMove': ('OnPointerMoved', 'PointerEventArgs', None),
    'OnMouseWheel': ('OnPointerWheelChanged', 'PointerWheelEventArgs', None),
    'OnLostMouseCapture': ('OnPointerCaptureLost', 'PointerCaptureLostEventArgs', None),
    'OnMouseDown': ('OnPointerPressed', 'PointerPressedEventArgs', None),
    'OnMouseUp': ('OnPointerReleased', 'PointerReleasedEventArgs', None),
    'OnMouseLeftButtonDown': ('OnPointerPressed', 'PointerPressedEventArgs', 'Left'),
    'OnMouseRightButtonDown': ('OnPointerPressed', 'PointerPressedEventArgs', 'Right'),
    'OnMouseLeftButtonUp': ('OnPointerReleased', 'PointerReleasedEventArgs', 'Left'),
    'OnMouseRightButtonUp': ('OnPointerReleased', 'PointerReleasedEventArgs', 'Right'),
    'OnGotKeyboardFocus': ('OnGotFocus', 'GotFocusEventArgs', None),
    'OnLostKeyboardFocus': ('OnLostFocus', 'RoutedEventArgs', None),
}
_OVR_RX = re.compile(r'(?P<mods>(?:public|protected|internal)\s+(?:sealed\s+)?)override\s+void\s+(?P<name>On\w+)\s*\(\s*(?P<type>[\w.]+)\s+(?P<arg>\w+)\s*\)\s*\{')


def _enclosing_class(text, pos):
    best = None
    for m in re.finditer(r'\bclass\s+(\w+)[^{;]*\{', text[:pos]):
        # check the class body still encloses pos
        depth = 0
        i = m.end()
        end = None
        while i < pos:
            c = text[i]
            if c == '{':
                depth += 1
            elif c == '}':
                if depth == 0:
                    end = i
                    break
                depth -= 1
            i += 1
        if end is None:
            best = m
    return best


def rewrite_overrides(text):
    registrations = {}   # class start -> list of lines
    out = text
    pos = 0
    while True:
        m = _OVR_RX.search(out, pos)
        if not m:
            break
        name = m.group('name')
        if name in SIMPLE_OVERRIDES:
            new_name, av_type, flt = SIMPLE_OVERRIDES[name]
            arg = m.group('arg')
            head = '%soverride void %s(%s %s) {' % (m.group('mods'), new_name, av_type, arg)
            if flt:
                head += '\n            if (!%s.IsButton(MouseButton.%s)) { base.%s(%s); return; }' % (arg, flt, new_name, arg)
            out = out[:m.start()] + head + out[m.end():]
            # base calls inside the method body: base.OnMouseLeftButtonDown(e) -> base.OnPointerPressed(e)
            body_end = _find_block_end(out, m.start() + len(head) - 1)
            body = out[m.start():body_end]
            body = re.sub(r'base\.%s\(' % name, 'base.%s(' % new_name, body)
            out = out[:m.start()] + body + out[body_end:]
            pos = m.start() + len(body)
            continue
        if name in PREVIEW_OVERRIDES:
            ev, routing, flt = PREVIEW_OVERRIDES[name]
            cls = _enclosing_class(out, m.start())
            if cls is None:
                pos = m.end()
                continue
            arg = m.group('arg')
            head = '%s%s %s(%s %s) {' % (m.group('mods').replace('sealed ', ''), 'void', name, m.group('type'), arg)
            head = head.replace('void', 'void', 1)
            out = out[:m.start()] + head + out[m.end():]
            body_end = _find_block_end(out, m.start() + len(head) - 1)
            body = out[m.start():body_end]
            body = re.sub(r'base\.%s\(\w+\);?' % name, '', body)
            out = out[:m.start()] + body + out[body_end:]
            if flt:
                handler = '(x, e) => { if (e is PointerEventArgs p && p.IsButton(MouseButton.%s)) x.%s(p); }' % (flt, name)
            elif ev.startswith('InputElement.Key') or ev.endswith('TextInputEvent'):
                handler = '(x, e) => x.%s((%s)e)' % (name, m.group('type'))
            else:
                handler = '(x, e) => x.%s((%s)e)' % (name, m.group('type'))
            line = '%s.AddClassHandler<%s>(%s, Avalonia.Interactivity.RoutingStrategies.%s);' % (ev, cls.group(1), handler, routing)
            registrations.setdefault((cls.group(1), cls.end()), []).append(line)
            pos = m.start() + len(body)
            continue
        pos = m.end()
    # insert registrations
    for (cname, brace_end), lines in sorted(registrations.items(), key=lambda kv: -kv[0][1]):
        m = re.search(r'static\s+%s\s*\(\s*\)\s*\{' % re.escape(cname), out)
        ins = '\n            '.join(lines)
        if m and m.start() > 0:
            out = out[:m.end()] + '\n            ' + ins + out[m.end():]
        else:
            out = out[:brace_end] + '\n        static %s() {\n            %s\n        }\n' % (cname, ins) + out[brace_end:]
    return out


def _find_block_end(text, open_pos):
    depth = 0
    i = open_pos
    q = None
    while i < len(text):
        c = text[i]
        if q:
            if c == '\\' and q == '"':
                i += 1
            elif c == q:
                q = None
        elif c in '"\'':
            q = c
        elif c == '{':
            depth += 1
        elif c == '}':
            depth -= 1
            if depth == 0:
                return i + 1
        i += 1
    return len(text)


def convert(text):
    for pat, rep in RULES:
        if rep is None: continue
        text = re.sub(pat, rep, text)
    # OverrideMetadata: X.OverrideMetadata(typeof(T), md) -> DP.OverrideMetadata(X, typeof(T), md)
    text = re.sub(r'\b(\w+(?:\.\w+)*Property)\.OverrideMetadata\(', r'DP.OverrideMetadata(\1, ', text)
    text = rewrite_events(text)
    text = rewrite_overrides(text)
    return text

def process(path):
    with open(path, 'rb') as f: raw = f.read()
    bom = raw.startswith(b'\xef\xbb\xbf')
    s = raw.decode('utf-8-sig')
    crlf = '\r\n' in s
    n = convert(s)
    if n != s:
        with open(path, 'wb') as f: f.write((b'\xef\xbb\xbf' if bom else b'') + n.encode('utf-8'))
        return True
    return False

if __name__ == '__main__':
    changed = 0
    for a in sys.argv[1:]:
        if os.path.isdir(a):
            for r, d, fs in os.walk(a):
                if '/obj' in r or '/bin' in r or '/Publish' in r or '/Compat' in r: continue
                for fn in fs:
                    if fn.endswith('.cs'):
                        changed += process(os.path.join(r, fn))
        else:
            changed += process(a)
    print('changed', changed)
