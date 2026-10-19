#!/usr/bin/env python3
"""
WPF XAML -> Avalonia XAML converter (best effort; things which can't be converted are marked with <!-- PORT: ... --> comments).

Usage:
    xaml_convert.py --scan <dir> [--scan <dir>] file.xaml [...]     # scan dirs to learn about keyed styles
    xaml_convert.py --all <dir> [...]                                 # convert every .xaml in dirs (each dir is also scanned)

Output goes next to the source file: X.xaml -> X.axaml. Dictionaries which contain plain (non-themed) styles
are split into X.axaml (resources, control themes) and X.styles.axaml (styles).
"""
import sys, os, re, copy, math, json
from lxml import etree
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from markup import Markup, parse as parse_markup, is_markup

NS_WPF = 'http://schemas.microsoft.com/winfx/2006/xaml/presentation'
NS_X = 'http://schemas.microsoft.com/winfx/2006/xaml'
NS_AV = 'https://github.com/avaloniaui'
NS_MC = 'http://schemas.openxmlformats.org/markup-compatibility/2006'
NS_D = 'http://schemas.microsoft.com/expression/blend/2008'
NS_MUI = 'http://firstfloorsoftware.com/ModernUI'
# prefixes for helper namespaces added to every converted file (they are removed again if unused)
HELPER_NS = {
    'i': 'using:Avalonia.Xaml.Interactivity',
    'ia': 'using:Avalonia.Xaml.Interactions.Core',
    'mui': 'http://firstfloorsoftware.com/ModernUI',
    'pa': 'clr-namespace:FirstFloor.ModernUI.Windows.Attached;assembly=FirstFloor.ModernUI',
    'pc': 'clr-namespace:FirstFloor.ModernUI.Windows.Converters;assembly=FirstFloor.ModernUI',
    'sw': 'clr-namespace:System.Windows;assembly=FirstFloor.ModernUI',
    'swi': 'clr-namespace:System.Windows.Input;assembly=FirstFloor.ModernUI',
}
NS_MUI = 'http://firstfloorsoftware.com/ModernUI'
NS_BLEND = 'http://schemas.microsoft.com/expression/2010/interactivity'
NS_BLEND_AV = 'using:Avalonia.Xaml.Interactivity'
NS_BLEND_AV_CORE = 'using:Avalonia.Xaml.Interactions.Core'
# WPF controls which Avalonia doesn't have and which are replaced by ModernUI ones
CUSTOM_ELEMENTS = {'ToolBar': NS_MUI, 'ToolBarTray': NS_MUI}
# custom shader effects of ModernUI, there are no pixel shaders in Avalonia
CUSTOM_EFFECTS = {'OverlayColorEffect', 'GrayscaleEffect', 'InvertKeepColorEffect', 'InvertColorEffect', 'AsTransparencyMaskEffect', 'ColorEffect'}
# styles for types which don't exist in Avalonia
UNSUPPORTED_TARGETS = {'GroupItem', 'Hyperlink_', 'ToolBarOverflowPanel', 'ToolBarPanel', 'DocumentViewer', 'FlowDocumentScrollViewer'}
# resource dictionaries of ModernUI which aren't converted (Avalonia’s own templates are used instead)
NOT_PORTED_ASSETS = {'MenuItem', 'DataGrid', 'DataGrid.Base', 'DataGrid.Cells', 'DataGrid.Headers', 'DataGrid.Rows', 'DatePicker',
                     'Calendar', 'ListView', 'SelectableBbCodeBlock', 'ProperPasswordBox', 'PasswordBox'}
SYSTEM_PARAMETER_VALUES = {
    'VerticalScrollBarWidthKey': '17', 'HorizontalScrollBarHeightKey': '17',
    'VerticalScrollBarButtonHeightKey': '17', 'HorizontalScrollBarButtonWidthKey': '17',
}
COMPAT_STATICS = {'SystemCommands': 'sw', 'SystemParameters': 'sw', 'NavigationCommands': 'swi', 'ApplicationCommands': 'swi', 'ComponentCommands': 'swi'}
X = '{%s}' % NS_X

ELEMENT_RENAME = {
    'Page': 'UserControl',
    'PasswordBox': 'TextBox',
    'ListView': 'ListBox',
    'ListViewItem': 'ListBoxItem',
    'AdornerDecorator': 'Panel',
    'ButtonBase': 'Button',
}

UNSUPPORTED = {
    'Storyboard', 'BeginStoryboard', 'StopStoryboard', 'DoubleAnimation', 'DoubleAnimationUsingKeyFrames', 'ColorAnimation', 'ThicknessAnimation',
    'ObjectAnimationUsingKeyFrames', 'SplineDoubleKeyFrame', 'EasingDoubleKeyFrame', 'LinearDoubleKeyFrame', 'DiscreteDoubleKeyFrame',
    'DiscreteObjectKeyFrame', 'ExponentialEase', 'CubicEase', 'QuadraticEase', 'PowerEase', 'WindowChrome', 'Typography',
    'CollectionViewSource', 'CollectionContainer', 'TickBar', 'AdornedElementPlaceholder', 'MouseBinding', 'KeyBinding', 'VisualStateGroup', 'VisualState',
    'VisualTransition', 'EventTrigger', 'MouseGesture', 'KeyGesture', 'PopupAnimation', 'ShaderEffect', 'AdornerDecorator_',
}

TYPE_RENAME = {
    'ButtonBase': 'Button',
    'ListView': 'ListBox',
    'ListViewItem': 'ListBoxItem',
    'Page': 'UserControl',
    'FrameworkElement': 'Control',
    'UIElement': 'Control',
    'Control': 'TemplatedControl',
    'Selector': 'SelectingItemsControl',
    'TextBoxBase': 'TextBox',
}

PSEUDO = {
    ('IsMouseOver', 'True'): ':pointerover',
    ('IsMouseOver', 'False'): ':not(:pointerover)',
    ('IsMouseDirectlyOver', 'True'): ':pointerover',
    ('IsPressed', 'True'): ':pressed',
    ('IsPressed', 'False'): ':not(:pressed)',
    ('IsEnabled', 'False'): ':disabled',
    ('IsEnabled', 'True'): ':not(:disabled)',
    ('IsChecked', 'True'): ':checked',
    ('IsChecked', 'False'): ':unchecked',
    ('IsSelected', 'True'): ':selected',
    ('IsSelected', 'False'): ':not(:selected)',
    ('IsFocused', 'True'): ':focus',
    ('IsKeyboardFocused', 'True'): ':focus',
    ('IsKeyboardFocused', 'False'): ':not(:focus)',
    ('IsKeyboardFocusWithin', 'True'): ':focus-within',
    ('IsKeyboardFocusWithin', 'False'): ':not(:focus-within)',
    ('IsDropDownOpen', 'True'): ':dropdownopen',
    ('IsDropDownOpen', 'False'): ':not(:dropdownopen)',
    ('IsExpanded', 'True'): ':expanded',
    ('IsExpanded', 'False'): ':not(:expanded)',
    ('IsReadOnly', 'True'): ':readonly',
    ('IsDefault', 'True'): ':default',
    ('ToggleButton.IsChecked', 'True'): ':checked',
    ('ToggleButton.IsChecked', 'False'): ':unchecked',
    ('IsEditable', 'True'): ':editable',
    ('IsDragging', 'True'): ':pressed',
    ('IsDragging', 'False'): ':not(:pressed)',
}

PROP_RENAME = {
    'CacheMode': None, 'RenderOptions.BitmapScalingMode_': None, 'BorderEdgeMode': None, 'EdgeMode': None,
    'Panel.ZIndex': 'ZIndex',
    'SnapsToDevicePixels': None,
    'UseLayoutRounding': None,
    'FocusVisualStyle': None,
    'Visibility': 'IsVisible',
    'ToolTip': 'ToolTip.Tip',
    'ToolTipService.ShowDuration': 'ToolTip.ShowDelay',
    'ToolTipService.InitialShowDelay': 'ToolTip.ShowDelay',
    'ToolTipService.Placement': 'ToolTip.Placement',
    'ToolTipService.IsEnabled': None,
    'ToolTipService.ShowOnDisabled': None,
    'ToolTipService.BetweenShowDelay': None,
    'ToolTipService.PlacementTarget': None,
    'TextOptions.TextFormattingMode': None,
    'TextOptions.TextRenderingMode': None,
    'TextOptions.TextHintingMode': None,
    'RenderOptions.EdgeMode': None,
    'RenderOptions.ClearTypeHint': None,
    'RenderOptions.CachingHint': None,
    'RenderOptions.BitmapScalingMode': 'RenderOptions.BitmapInterpolationMode',
    'RenderOptions.CacheInvalidationThresholdMinimum': None,
    'RenderOptions.CacheInvalidationThresholdMaximum': None,
    'AllowDrop': 'DragDrop.AllowDrop',
    'Validation.ErrorTemplate': None,
    'KeyboardNavigation.TabNavigation': None,
    'KeyboardNavigation.DirectionalNavigation': None,
    'KeyboardNavigation.ControlTabNavigation': None,
    'KeyboardNavigation.IsTabStop': 'IsTabStop',
    'VirtualizingPanel.IsVirtualizing': None,
    'VirtualizingStackPanel.IsVirtualizing': None,
    'VirtualizingPanel.VirtualizationMode': None,
    'VirtualizingStackPanel.VirtualizationMode': None,
    'VirtualizingPanel.ScrollUnit': None,
    'VirtualizingPanel.CacheLength': None,
    'ScrollViewer.CanContentScroll': None,
    'ScrollViewer.PanningMode': None,
    'ScrollViewer.IsDeferredScrollingEnabled': None,
    'Stylus.IsPressAndHoldEnabled': None,
    'Stylus.IsFlicksEnabled': None,
    'Stylus.IsTapFeedbackEnabled': None,
    'Stylus.IsTouchFeedbackEnabled': None,
    'WindowChrome.IsHitTestVisibleInChrome': None,
    'WindowChrome.ResizeGripDirection': None,
    'WindowStyle': None,
    'AllowsTransparency': None,
    'IsSynchronizedWithCurrentItem': None,
    'BitmapEffect': None,
    'CacheMode': None,
    'Language': None,
    'SnapsToDevicePixels': None,
    'ContentStringFormat': None,
    'ContentTemplateSelector': None,
    'ItemTemplateSelector': None,
    'NavigationUIVisibility': None,
    'TextOptions.TextFormattingMode': None,
    'TextBlock.TextOptions': None,
    'VirtualizingStackPanel.CacheLengthUnit': None,
    'VirtualizingPanel.CacheLengthUnit': None,
    'VirtualizingStackPanel.CacheLength': None,
    'VirtualizingStackPanel.ScrollUnit': None,
    'ScrollViewer.CanContentScroll': None,
    'CanContentScroll': None,
    'IsTextSearchEnabled': None,
    'IsTextSearchCaseSensitive': None,
    'TextSearch.TextPath': None,
    'ItemContainerStyleSelector': None,
    'OverridesDefaultStyle': None,
    'PanningMode': None,
    'CommandTarget': None,
    'a:Resources.Renames': None,
    'WindowChrome.WindowChrome': None,
    'WindowChrome.CaptionHeight': None,
    'ContentSource': None,
    'Typography.Variants': None,
    'PopupAnimation': None,
    'HasDropShadow': None,
    'ScrollViewer.PanningDeceleration': None,
    'ScrollViewer.PanningRatio': None,
    'ScrollViewer.IsManipulationEnabled': None,
    'IsManipulationEnabled': None,
    'Stylus.IsPressAndHoldEnabled': None,
    'FlowDirection': 'FlowDirection',
}

EVENT_RENAME = {
    'MouseLeftButtonDown': 'PointerPressed', 'MouseRightButtonDown': 'PointerPressed', 'MouseDown': 'PointerPressed',
    'PreviewMouseLeftButtonDown': 'PointerPressed', 'PreviewMouseRightButtonDown': 'PointerPressed', 'PreviewMouseDown': 'PointerPressed',
    'MouseLeftButtonUp': 'PointerReleased', 'MouseRightButtonUp': 'PointerReleased', 'MouseUp': 'PointerReleased',
    'PreviewMouseLeftButtonUp': 'PointerReleased', 'PreviewMouseRightButtonUp': 'PointerReleased', 'PreviewMouseUp': 'PointerReleased',
    'MouseMove': 'PointerMoved', 'PreviewMouseMove': 'PointerMoved', 'MouseEnter': 'PointerEntered', 'MouseLeave': 'PointerExited',
    'MouseWheel': 'PointerWheelChanged', 'PreviewMouseWheel': 'PointerWheelChanged',
    'PreviewKeyDown': 'KeyDown', 'PreviewKeyUp': 'KeyUp', 'PreviewTextInput': 'TextInput',
    'LostMouseCapture': 'PointerCaptureLost', 'MouseDoubleClick': 'DoubleTapped',
    'Drop': 'DragDrop.Drop', 'DragOver': 'DragDrop.DragOver', 'DragEnter': 'DragDrop.DragEnter', 'DragLeave': 'DragDrop.DragLeave',
    'PreviewDrop': 'DragDrop.Drop', 'PreviewDragOver': 'DragDrop.DragOver',
}

# argument types of Avalonia events which have to match exactly the signature of handlers referenced from XAML
EVENT_ARGS = {
    'PointerPressed': 'PointerPressedEventArgs', 'PointerReleased': 'PointerReleasedEventArgs', 'PointerMoved': 'PointerEventArgs',
    'PointerEntered': 'PointerEventArgs', 'PointerExited': 'PointerEventArgs', 'PointerWheelChanged': 'PointerWheelEventArgs',
    'PointerCaptureLost': 'PointerCaptureLostEventArgs', 'KeyDown': 'KeyEventArgs', 'KeyUp': 'KeyEventArgs',
    'TextInput': 'TextInputEventArgs', 'DoubleTapped': 'TappedEventArgs', 'Tapped': 'TappedEventArgs',
    'DragDrop.Drop': 'DragEventArgs', 'DragDrop.DragOver': 'DragEventArgs', 'DragDrop.DragEnter': 'DragEventArgs', 'DragDrop.DragLeave': 'DragEventArgs',
    'SelectionChanged': 'SelectionChangedEventArgs', 'TextChanged': 'TextChangedEventArgs', 'SizeChanged': 'SizeChangedEventArgs',
    'GotFocus': 'GotFocusEventArgs', 'ContextRequested': 'ContextRequestedEventArgs',
}

CURSORS = {'SizeWE': 'SizeWestEast', 'SizeNS': 'SizeNorthSouth', 'IBeam': 'Ibeam', 'ScrollAll': 'SizeAll',
           'SizeNESW': 'TopRightCorner', 'SizeNWSE': 'TopLeftCorner'}


def full_name_in(owner, prop, names):
    return (owner, prop) == names


def local(tag):
    return etree.QName(tag).localname


def ns_of(tag):
    return etree.QName(tag).namespace


def retag(old_tag, new_local):
    """New tag in the namespace of the old one (WPF’s becomes Avalonia’s, custom controls stay where they are)."""
    ns = ns_of(old_tag)
    return '{%s}%s' % (NS_AV if ns in (NS_WPF, None) else ns, new_local)


def typed_parent(style):
    """Nested `^` selector whose parent is a theme or a style with a type selector: the type is known already."""
    sel = style.get('Selector') or ''
    if not sel.startswith('^'):
        return False
    p = style.getparent()
    if p is None or not isinstance(p.tag, str):
        return False
    if local(p.tag) == 'ControlTheme':
        return True
    if local(p.tag) == 'Style':
        ps = p.get('Selector') or ''
        return bool(re.match(r'^[A-Za-z]', ps)) or typed_parent(p)
    return False


def is_handler_name(v):
    return bool(v) and re.match(r'^[A-Za-z_]\w*$', v) is not None


def fix_handler_types(cs_path, handlers):
    """Makes handlers referenced by XAML events match the exact Avalonia delegate types (`MouseButtonEventArgs` etc. are aliases of the base ones)."""
    if not os.path.exists(cs_path) or not handlers:
        return
    src = open(cs_path, encoding='utf-8-sig').read()
    orig = src
    for name, (ev, args) in handlers.items():
        if not args:
            continue
        pat = re.compile(r'(\b%s\s*\(\s*(?:object|\w+)\??\s+\w+\s*,\s*)([\w.<>]+)(\s+\w+\s*\))' % re.escape(name))
        src = pat.sub(lambda m: m.group(1) + args + m.group(3), src)
    if src != orig:
        open(cs_path, 'w', encoding='utf-8').write(src)


def is_el(n):
    return isinstance(n.tag, str)


def qn(name, ns=NS_AV):
    return '{%s}%s' % (ns, name)


def type_name(t):
    t = t.strip()
    if t.startswith('{x:Type') and t.endswith('}'):
        t = t[7:-1].strip()
    if ':' in t:
        return t
    return TYPE_RENAME.get(t, t)


# ------------------------------------------------------------------ registry of keyed styles
class Registry:
    def __init__(self):
        self.styles = {}      # key -> dict(file, target, based, has_template, el)

    def scan(self, paths):
        for p in paths:
            for root, dirs, files in os.walk(p):
                if '/obj' in root or '/bin' in root or '/Publish' in root:
                    continue
                for f in files:
                    if f.endswith('.xaml'):
                        self.scan_file(os.path.join(root, f))

    def scan_file(self, path):
        try:
            tree = etree.parse(path)
        except Exception:
            return
        for st in tree.getroot().iter('{%s}Style' % NS_WPF):
            key = st.get(X + 'Key')
            if not key:
                continue
            has_t = any(is_el(s) and local(s.tag) == 'Setter' and s.get('Property') == 'Template' for s in st)
            self.styles[key] = {'file': path, 'target': st.get('TargetType'), 'based': st.get('BasedOn'), 'has_template': has_t, 'el': st}

    def key_of(self, ref):
        """'{StaticResource Foo}' -> 'Foo'"""
        if ref and is_markup(ref):
            mk = parse_markup(ref)
            if mk.name in ('StaticResource', 'DynamicResource') and mk.args:
                return mk.args[0]
            if mk.name == 'x:Type':
                return None
        return None

    def chain(self, key):
        out = []
        seen = set()
        while key and key in self.styles and key not in seen:
            seen.add(key)
            out.append(key)
            key = self.key_of(self.styles[key]['based'])
        return out

    def is_theme(self, key):
        return any(self.styles[k]['has_template'] for k in self.chain(key))


class Ctx:
    def __init__(self, path, registry):
        self.path = path
        self.registry = registry
        self.todo = []
        self.state_counter = 0
        self.style_parts = []   # plain styles which go to X.styles.axaml
        self.inline_counter = 0
        self.handlers = {}

    def warn(self, el, msg):
        self.todo.append(msg)
        if el is not None and el.getparent() is not None:
            parent = el.getparent()
            parent.insert(parent.index(el), etree.Comment(' PORT: %s ' % msg.replace('--', '- -')))


# ------------------------------------------------------------------ values
def convert_pack_uri(v):
    m = re.match(r'^(?:pack://application:,,,)?/?([^;/]+);component/(.+)$', v)
    if m:
        return 'avares://%s/%s' % (m.group(1), m.group(2))
    m = re.match(r'^pack://application:,,,/(.+)$', v)
    if m:
        return 'avares://__APP__/' + m.group(1)
    return v


def convert_markup_value(val, ctx, prop=None, el=None):
    if not is_markup(val):
        return convert_pack_uri(val) if 'pack://' in val else val
    try:
        mk = parse_markup(val)
    except Exception:
        return val
    return str(convert_markup(mk, ctx, prop, el))


def fix_relative_source(rs):
    if rs.args and rs.args[0] in ('FindAncestor', 'Self', 'TemplatedParent', 'PreviousData'):
        mode = rs.args[0]
        rs.args = [mode] if mode in ('Self', 'TemplatedParent') else []
    if rs.get('Mode') in ('Self', 'TemplatedParent'):
        rs.args = [rs.get('Mode')]
        rs.remove('Mode')
    elif rs.get('Mode') == 'FindAncestor':
        rs.remove('Mode')
    at = rs.get('AncestorType')
    if isinstance(at, str):
        rs.set('AncestorType', '{x:Type %s}' % type_name(at))
    elif isinstance(at, Markup) and at.name == 'x:Type' and at.args:
        at.args[0] = type_name(at.args[0])


def convert_markup(mk, ctx, prop=None, el=None):
    name = mk.name
    mk.args = [convert_markup(a, ctx, None, el) if isinstance(a, Markup) else a for a in mk.args]
    for kv in mk.kwargs:
        if isinstance(kv[1], Markup):
            kv[1] = convert_markup(kv[1], ctx, kv[0], el)
        elif isinstance(kv[1], str) and 'pack://' in kv[1]:
            kv[1] = convert_pack_uri(kv[1])
    if name == 'Binding':
        for dead in ('IsAsync', 'NotifyOnSourceUpdated', 'NotifyOnTargetUpdated', 'NotifyOnValidationError', 'ValidatesOnDataErrors',
                     'ValidatesOnExceptions', 'ValidatesOnNotifyDataErrors', 'BindsDirectlyToSource', 'BindingGroupName', 'XPath', 'Delay'):
            mk.remove(dead)
        if mk.get('UpdateSourceTrigger') in ('Explicit', 'LostFocus', 'Default'):
            mk.remove('UpdateSourceTrigger')
        rs = mk.get('RelativeSource')
        if isinstance(rs, Markup):
            fix_relative_source(rs)
        fb = mk.get('FallbackValue')
        if isinstance(fb, Markup) and fb.name == 'x:Static' and fb.args and str(fb.args[0]).startswith('Visibility.'):
            mk.remove('FallbackValue')
        # Path=(Foo.Bar) is OK, Path=. also ok
    elif name == 'RelativeSource':
        fix_relative_source(mk)
    elif name == 'x:Type':
        if mk.args and isinstance(mk.args[0], str):
            mk.args[0] = type_name(mk.args[0])
    elif name in ('DynamicResource', 'StaticResource') and mk.args and isinstance(mk.args[0], Markup) and mk.args[0].name == 'x:Static' \
            and mk.args[0].args and isinstance(mk.args[0].args[0], str) and re.match(r'(sw:)?SystemParameters\.\w+Key$', mk.args[0].args[0]):
        # system metrics have no counterpart, constants are used (sizes of Windows scroll bars)
        key = mk.args[0].args[0].split('.')[-1]
        if key in SYSTEM_PARAMETER_VALUES:
            return SYSTEM_PARAMETER_VALUES[key]
    elif name == 'x:Static':
        if mk.args and isinstance(mk.args[0], str):
            owner = mk.args[0].split('.')[0]
            if owner in COMPAT_STATICS:
                mk.args[0] = '%s:%s' % (COMPAT_STATICS[owner], mk.args[0])
    return mk


def split_prop(prop):
    if '.' in prop:
        return prop.rsplit('.', 1)
    return None, prop


class Converter:
    def __init__(self, ctx):
        self.ctx = ctx

    # --------------------------------------------------------------- driver
    def convert_root(self, root):
        tag = local(root.tag)
        if tag == 'ResourceDictionary':
            self.convert_dictionary(root)
        else:
            self.convert_element(root)

    def convert_tree(self, el):
        for child in list(el):
            if is_el(child):
                self.convert_element(child)

    def convert_element(self, el):
        tag = local(el.tag)
        ns = ns_of(el.tag)
        if ns == NS_X and tag == 'Null':
            parent = el.getparent()
            comment = etree.Comment(' PORT: removed x:Null resource %s ' % el.get(X + 'Key'))
            comment.tail = el.tail
            parent.replace(el, comment)
            return
        if '.' in tag:
            self.convert_property_element(el, tag, ns)
            return
        if ns == NS_MUI and tag in CUSTOM_EFFECTS or ns == NS_WPF and tag in ('PriorityBinding_',):
            self.ctx.todo.append('removed %s' % tag)
            parent = el.getparent()
            comment = etree.Comment(' PORT: removed %s (pixel shader effect) ' % tag)
            comment.tail = el.tail
            parent.replace(el, comment)
            return
        if ns == NS_WPF and tag == 'PriorityBinding':
            # first binding which has a value
            el.tag = qn('MultiBinding')
            el.set('Converter', '{x:Static pc:FirstNotNullConverter.Instance}')
            for c in list(el):
                if is_el(c):
                    self.convert_element(c)
            return
        if ns == NS_BLEND:
            self.convert_blend(el, tag)
            return
        if ns == NS_WPF and tag in CUSTOM_ELEMENTS:
            el.tag = '{%s}%s' % (CUSTOM_ELEMENTS[tag], tag)
            self.convert_attributes(el)
            for c in list(el):
                if is_el(c):
                    self.convert_element(c)
            return
        if ns == NS_WPF and tag in UNSUPPORTED:
            self.ctx.todo.append('removed %s' % tag)
            parent = el.getparent()
            comment = etree.Comment(' PORT: removed %s ' % tag)
            comment.tail = el.tail
            parent.replace(el, comment)
            return
        if ns == NS_WPF and (tag.endswith('Transform') or (tag.startswith('DataGrid') and tag.endswith('Column')) or tag in ('SolidColorBrush', 'LinearGradientBrush', 'GradientStop')):
            # not named in Avalonia (no Name property); transforms are reachable by the property path only
            el.attrib.pop(X + 'Name', None)
            el.attrib.pop('Name', None)
        if ns == NS_WPF and tag in ('Style', 'ControlTemplate') and (el.get('TargetType') or '').split(':')[-1] in UNSUPPORTED_TARGETS:
            parent = el.getparent()
            comment = etree.Comment(' PORT: removed %s for %s (no such control in Avalonia) ' % (tag, el.get('TargetType')))
            comment.tail = el.tail
            parent.replace(el, comment)
            return
        if ns == NS_WPF:
            if tag == 'Style':
                StyleConverter(self).convert(el)
                return
            if tag == 'ControlTemplate':
                StyleConverter(self).convert_template_standalone(el)
                return
            if tag in ('DataTemplate', 'HierarchicalDataTemplate'):
                StyleConverter(self).convert_data_template(el)
                return
            if tag == 'ResourceDictionary':
                self.convert_dictionary(el)
                return
            if tag == 'Line':
                self.convert_line(el)
            elif tag == 'DropShadowEffect':
                self.convert_shadow(el)
            elif tag == 'Hyperlink':
                self.ctx.warn(el, 'Hyperlink needs manual conversion')
                el.tag = qn('TextBlock')
            else:
                el.tag = qn(ELEMENT_RENAME.get(tag, tag))
                if tag == 'PasswordBox':
                    # a text box which hides its text
                    el.set('PasswordChar', '\u25cf')
                    if el.get('Password') is not None:
                        el.set('Text', el.attrib.pop('Password'))
        if tag == 'RepeatButton' and el.get('Command'):
            # parts of a scroll bar are recognized by names in Avalonia
            m = re.match(r'\{x:Static (?:\w+:)?ScrollBar\.(Line|Page)(Up|Down|Left|Right)Command\}$', el.get('Command'))
            if m:
                del el.attrib['Command']
                el.set(X + 'Name', 'PART_%s%sButton' % (m.group(1), 'Up' if m.group(2) in ('Up', 'Left') else 'Down'))
            m = re.match(r'\{x:Static (?:\w+:)?Slider\.(Decrease|Increase)Large\}$', el.get('Command'))
            if m:
                del el.attrib['Command']
                el.set(X + 'Name', 'PART_%sButton' % m.group(1))
        self.convert_attributes(el)
        self.convert_tree(el)

    # --------------------------------------------------------------- attributes
    def convert_attributes(self, el):
        for name in list(el.attrib):
            val = el.attrib[name]
            if name.startswith('{'):
                q = etree.QName(name)
                if q.namespace == NS_D:
                    del el.attrib[name]
                elif q.namespace == NS_X:
                    if q.localname in ('Shared',):
                        del el.attrib[name]
                    elif is_markup(val):
                        el.attrib[name] = convert_markup_value(val, self.ctx, None, el)
                elif q.namespace == NS_MC:
                    pass
                else:
                    el.attrib[name] = convert_markup_value(val, self.ctx, name, el)
                continue
            new_name, new_val = self.convert_attribute(el, name, val)
            if new_name is None:
                del el.attrib[name]
            elif new_name != name or new_val != val:
                del el.attrib[name]
                if new_name in el.attrib and new_name == 'ToolTip.ShowDelay':
                    continue
                el.set(new_name, new_val)
        self.fix_container_style_attr(el)
        self.fix_style_attr(el)

    def fix_container_style_attr(self, el):
        st = el.get('ItemContainerStyle')
        if st is None:
            return False
        reg = self.ctx.registry
        key = reg.key_of(st) if reg else None
        if not key or key not in reg.styles:
            del el.attrib['ItemContainerStyle']
            el.set('ItemContainerTheme', st)
            return False
        if reg.is_theme(key):
            del el.attrib['ItemContainerStyle']
            el.set('ItemContainerTheme', st)
            return True
        # plain style: copy it into a Styles collection of the items control
        info = reg.styles[key]
        copy_el = copy.deepcopy(info['el'])
        copy_el.attrib.pop(X + 'Key', None)
        target = copy_el.get('TargetType') or 'ListBoxItem'
        holder = etree.Element(retag(el.tag, local(el.tag) + '.Styles'))
        container = type_name(target)
        copy_el.set('TargetType', container)
        # use the converter for this (clone of) style as unkeyed implicit one, then give it the right selector
        StyleConverter(self).convert(copy_el, class_name=None)
        copy_el.set('Selector', container.replace(':', '|'))
        copy_el.attrib.pop('__plain', None)
        holder.append(copy_el)
        del el.attrib['ItemContainerStyle']
        el.insert(0, holder)
        return True

    def fix_style_attr(self, el):
        st = el.get('Style')
        if st is None:
            return
        reg = self.ctx.registry
        key = reg.key_of(st) if reg else None
        if is_markup(st) and st.strip() == '{x:Null}':
            del el.attrib['Style']
            return
        if key and key in reg.styles:
            if reg.is_theme(key):
                del el.attrib['Style']
                el.set('Theme', st)
            else:
                del el.attrib['Style']
                classes = (el.get('Classes') or '').split()
                for k in reversed(reg.chain(key)):
                    cls = k.replace(' ', '_')
                    if cls not in classes:
                        classes.append(cls)
                el.set('Classes', ' '.join(classes))
        elif key:
            # unknown to the registry (defined in an unscanned place) - assume theme
            del el.attrib['Style']
            el.set('Theme', st)
            self.ctx.warn(None, 'unknown style %s used as Theme' % key)

    def convert_attribute(self, el, name, val):
        tag = local(el.tag)
        if name in ('IsItemsHost', 'CacheLength', 'CacheLengthUnit', 'ScrollUnit', 'VirtualizationMode', 'IsVirtualizing') and tag in (
                'VirtualizingStackPanel', 'StackPanel', 'WrapPanel', 'VirtualizingPanel', 'Grid', 'DockPanel', 'Canvas', 'UniformGrid'):
            return None, val
        if name == 'SelectionMode' and val == 'Extended':
            return name, 'Multiple'
        if name in ('HorizontalAlignment', 'VerticalAlignment', 'HorizontalContentAlignment', 'VerticalContentAlignment') and val and val[0].islower():
            # enums are case-insensitive in WPF
            return name, val[0].upper() + val[1:]
        m = re.match(r'^TextBlock\.(Foreground|FontSize|FontFamily|FontWeight|FontStyle)$', name)
        if m:
            return 'TextElement.' + m.group(1), convert_markup_value(val, self.ctx, name, el)
        if name in EVENT_RENAME or name in EVENT_ARGS:
            new = EVENT_RENAME.get(name, name)
            if is_handler_name(val):
                self.ctx.handlers[val] = (new, EVENT_ARGS.get(new))
            return new, val
        if name == 'Visibility':
            return 'IsVisible', self.convert_visibility(val, el)
        if name in PROP_RENAME:
            new = PROP_RENAME[name]
            if new is None:
                return None, val
            if name == 'RenderOptions.BitmapScalingMode':
                return new, {'Linear': 'MediumQuality', 'NearestNeighbor': 'None', 'HighQuality': 'HighQuality', 'Fant': 'HighQuality', 'LowQuality': 'LowQuality', 'Unspecified': 'Unspecified'}.get(val, 'HighQuality')
            return new, convert_markup_value(val, self.ctx, name, el)
        if name == 'ItemContainerStyle':
            return name, val
        if name == 'Cursor':
            return name, CURSORS.get(val, val)
        if name in ('Command', 'CommandParameter') and re.match(r'^(SystemCommands|NavigationCommands|ApplicationCommands|ComponentCommands)\.\w+$', val or ''):
            return name, '{x:Static %s:%s}' % (COMPAT_STATICS[val.split('.')[0]], val)
        if name == 'ResizeMode':
            return 'CanResize', 'False' if val == 'NoResize' else 'True'
        if name == 'TargetType':
            return name, type_name(val)
        if name == 'Source' and 'component/' in val:
            return name, convert_pack_uri(val)
        if name.endswith('.Visibility'):
            return name, convert_markup_value(val, self.ctx, name, el)
        if is_markup(val):
            return name, convert_markup_value(val, self.ctx, name, el)
        if 'pack://' in val:
            return name, convert_pack_uri(val)
        return name, val

    def convert_visibility(self, val, el):
        if is_markup(val):
            mk = parse_markup(val)
            mk = convert_markup(mk, self.ctx, 'Visibility', el)
            if mk.name in ('Binding', 'TemplateBinding') and not mk.has('Converter'):
                mk.set('Converter', '{x:Static pc:VisibilityConverters.ToBool}')
            return str(mk)
        if val == 'Visible':
            return 'True'
        if val in ('Collapsed', 'Hidden'):
            return 'False'
        return val

    # --------------------------------------------------------------- property elements
    def convert_property_element(self, el, tag, ns):
        owner, prop = tag.split('.', 1)
        if prop == 'LayoutTransform':
            parent = el.getparent()
            comment = etree.Comment(' PORT: removed %s ' % tag)
            comment.tail = el.tail
            parent.replace(el, comment)
            return
        if prop in ('CacheMode', 'BitmapEffect', 'OpacityMask_'):
            parent = el.getparent()
            comment = etree.Comment(' PORT: removed %s ' % tag)
            comment.tail = el.tail
            parent.replace(el, comment)
            return
        if prop == 'ToolTip':
            el.tag = qn('ToolTip.Tip')
            self.convert_tree(el)
            return
        if ns != NS_WPF:
            self.convert_tree(el)
            return
        if prop == 'ToolTip':
            el.tag = qn('ToolTip.Tip')
            self.convert_tree(el)
            return
        if prop == 'Style':
            self.convert_style_property(el, owner)
            return
        if prop in ('ItemContainerStyle',):
            self.convert_item_container_style(el, owner)
            return
        if full_name_in(owner, prop, ('VisualStateManager', 'VisualStateGroups')):
            parent = el.getparent()
            comment = etree.Comment(' PORT: removed %s ' % tag)
            comment.tail = el.tail
            parent.replace(el, comment)
            return
        if prop in ('InputBindings', 'CommandBindings'):
            parent = el.getparent()
            comment = etree.Comment(' PORT: removed %s ' % tag)
            comment.tail = el.tail
            parent.replace(el, comment)
            return
        if prop == 'Resources':
            el.tag = qn('%s.Resources' % TYPE_RENAME.get(owner, owner))
            self.convert_tree(el)
            return
        if prop in ('Triggers',) and owner in ('Style', 'ControlTemplate', 'DataTemplate'):
            return
        if prop == 'Visibility':
            el.tag = qn('%s.IsVisible' % owner)
            self.ctx.warn(el, 'Visibility property element')
            return
        if prop == 'LayoutTransform':
            parent = el.getparent()
            comment = etree.Comment(' PORT: removed %s ' % tag)
            comment.tail = el.tail
            parent.replace(el, comment)
            return
        if prop in ('Effect',):
            el.tag = qn('%s.Effect' % owner)
            self.convert_tree(el)
            return
        prop = {'DecreaseRepeatButton': 'DecreaseButton', 'IncreaseRepeatButton': 'IncreaseButton'}.get(prop, prop)
        el.tag = qn('%s.%s' % (TYPE_RENAME.get(owner, owner), prop))
        self.convert_tree(el)

    def convert_dictionary(self, el):
        el.tag = qn('ResourceDictionary')
        self.convert_attributes(el)
        for child in list(el):
            if not is_el(child):
                continue
            t = local(child.tag)
            if t == 'ResourceDictionary.MergedDictionaries':
                child.tag = qn('ResourceDictionary.MergedDictionaries')
                for d in list(child):
                    if is_el(d):
                        self.convert_merged(d)
            else:
                self.convert_element(child)

    def convert_blend(self, el, tag):
        """Blend interactivity -> Avalonia.Xaml.Behaviors"""
        if tag == 'Interaction.Triggers':
            el.tag = '{%s}Interaction.Behaviors' % NS_BLEND_AV
        elif tag == 'Interaction.Behaviors':
            el.tag = '{%s}Interaction.Behaviors' % NS_BLEND_AV
        elif tag == 'EventTrigger':
            el.tag = '{%s}EventTriggerBehavior' % NS_BLEND_AV_CORE
        elif tag in ('InvokeCommandAction', 'CallMethodAction', 'ChangePropertyAction'):
            el.tag = '{%s}%s' % (NS_BLEND_AV_CORE, tag)
        else:
            self.ctx.warn(el, 'blend element %s' % tag)
        for c in list(el):
            if is_el(c):
                self.convert_element(c)

    def convert_merged(self, d):
        t = local(d.tag)
        src = d.get('Source')
        if t in ('ResourceDictionary', 'SharedResourceDictionary') and src:
            base = os.path.basename(src)[:-5] if src.endswith('.xaml') else ''
            if base in NOT_PORTED_ASSETS:
                # the resource dictionary is replaced by the default Avalonia theme (see FirstFloor.ModernUI/Deferred.props)
                parent = d.getparent()
                comment = etree.Comment(' PORT: removed include of %s ' % base)
                comment.tail = d.tail
                parent.replace(d, comment)
                return
            d.tag = qn('ResourceInclude')
            d.set('Source', convert_pack_uri(src).replace('.xaml', '.axaml'))
            for a in list(d.attrib):
                if a != 'Source':
                    del d.attrib[a]
        else:
            self.convert_element(d)

    def convert_line(self, el):
        el.tag = qn('Line')
        x1, y1, x2, y2 = (el.attrib.pop(k, '0') for k in ('X1', 'Y1', 'X2', 'Y2'))
        el.set('StartPoint', '%s,%s' % (x1, y1))
        el.set('EndPoint', '%s,%s' % (x2, y2))

    def convert_shadow(self, el):
        el.tag = qn('DropShadowEffect')
        depth = float(el.attrib.pop('ShadowDepth', '5'))
        direction = float(el.attrib.pop('Direction', '315'))
        el.set('OffsetX', '%g' % round(depth * math.cos(math.radians(direction)), 3))
        el.set('OffsetY', '%g' % round(-depth * math.sin(math.radians(direction)), 3))
        el.attrib.pop('RenderingBias', None)

    def convert_style_property(self, el, owner):
        """<Button.Style><Style .../></Button.Style>"""
        styles = [c for c in el if is_el(c) and local(c.tag) == 'Style']
        if len(styles) != 1:
            self.ctx.warn(el, 'strange Style property element')
            return
        st = styles[0]
        has_t = any(is_el(s) and local(s.tag) == 'Setter' and s.get('Property') == 'Template' for s in st)
        if has_t:
            el.tag = qn('%s.Theme' % owner)
            StyleConverter(self).convert(st, force_theme=True)
            st.attrib.pop(X + 'Key', None)
        else:
            # inline style -> Styles collection with a unique class
            self.ctx.inline_counter += 1
            cls = 'inline%d' % self.ctx.inline_counter
            parent = el.getparent()
            tt = type_name(st.get('TargetType') or owner)
            StyleConverter(self).convert(st, class_name=cls)
            st.attrib['Selector'] = '%s.%s' % (tt, cls)
            el.tag = retag(el.tag, '%s.Styles' % owner)
            classes = (parent.get('Classes') or '').split()
            classes.append(cls)
            parent.set('Classes', ' '.join(classes))

    def convert_item_container_style(self, el, owner):
        styles = [c for c in el if is_el(c) and local(c.tag) == 'Style']
        if len(styles) != 1:
            self.ctx.warn(el, 'strange ItemContainerStyle')
            return
        st = styles[0]
        has_t = any(is_el(s) and local(s.tag) == 'Setter' and s.get('Property') == 'Template' for s in st)
        if has_t:
            el.tag = qn('%s.ItemContainerTheme' % owner)
            StyleConverter(self).convert(st, force_theme=True)
            st.attrib.pop(X + 'Key', None)
        else:
            tt = type_name(st.get('TargetType') or 'ListBoxItem')
            StyleConverter(self).convert(st, class_name=None)
            st.attrib['Selector'] = tt
            el.tag = retag(el.tag, '%s.Styles' % owner)


class StyleConverter:
    def __init__(self, conv):
        self.conv = conv
        self.ctx = conv.ctx
        self.template_names = {}
        self.template_types = {}
        self.hoisted_resources = None

    def selector_type_of(self, el):
        tag = local(el.tag)
        ns = ns_of(el.tag)
        if ns == NS_WPF or ns == NS_AV:
            return ELEMENT_RENAME.get(tag, tag) if '.' not in tag else '*'
        prefix = None
        for p, u in el.nsmap.items():
            if u == ns and p:
                prefix = p
        return '%s|%s' % (prefix, tag) if prefix else tag

    def sel(self, name):
        t = self.template_types.get(name, '')
        return ('%s#%s' % (t, name)) if t and t != '*' else '#%s' % name

    # -------------------------------------------------- Style
    def effective(self, style, key):
        """Flatten BasedOn chain; returns (setters, triggers, resources, template_setter, theme_base_key)"""
        reg = self.ctx.registry
        elements = [style]
        base_key = None
        b = style.get('BasedOn')
        k = reg.key_of(b) if (reg and b) else None
        theme_base = None
        while k and k in reg.styles:
            st = reg.styles[k]
            has_t = st['has_template']
            if has_t and theme_base is None and False:
                pass
            elements.append(st['el'])
            k = reg.key_of(st['based'])
        return elements

    def convert(self, style, force_theme=False, class_name=None):
        reg = self.ctx.registry
        key = style.get(X + 'Key')
        if key and key.strip().startswith('{x:Type'):
            style.attrib.pop(X + 'Key')
            key = None
        target = style.get('TargetType')
        sel_type = type_name(target) if target else 'Control'
        based_attr = style.get('BasedOn')
        based_key = reg.key_of(based_attr) if (reg and based_attr) else None

        own_setters = [c for c in style if is_el(c) and local(c.tag) == 'Setter']
        template_setter = next((s for s in own_setters if s.get('Property') == 'Template'), None)
        chain_has_template = template_setter is not None or (based_key is not None and reg.is_theme(based_key))
        as_theme = force_theme or chain_has_template

        triggers_el = next((c for c in style if is_el(c) and local(c.tag) == 'Style.Triggers'), None)
        resources_el = next((c for c in style if is_el(c) and local(c.tag) == 'Style.Resources'), None)

        # flatten non-theme bases
        inherited_setters = []
        inherited_triggers = []
        theme_base_key = None
        k = based_key
        flat_elements = []
        while k and k in reg.styles:
            if as_theme and reg.styles[k]['has_template']:
                theme_base_key = k
                break
            flat_elements.append(reg.styles[k]['el'])
            k = reg.key_of(reg.styles[k]['based'])
        for be in reversed(flat_elements):
            for s in be:
                if is_el(s) and local(s.tag) == 'Setter':
                    inherited_setters.append(copy.deepcopy(s))
                elif is_el(s) and local(s.tag) == 'Style.Triggers':
                    inherited_triggers += [copy.deepcopy(t) for t in s]

        # dedupe: own setters override inherited ones with the same property
        own_props = {s.get('Property') for s in own_setters}
        inherited_setters = [s for s in inherited_setters if s.get('Property') not in own_props or s.get('Property') == 'Template']

        # remove old setters/trigger containers from the style and rebuild
        for c in list(style):
            if is_el(c) and (local(c.tag) in ('Setter', 'Style.Triggers')):
                style.remove(c)
        all_setters = inherited_setters + own_setters
        insert_at = 0
        for s in all_setters:
            style.insert(insert_at, s)
            insert_at += 1

        if as_theme:
            style.tag = qn('ControlTheme')
            if key is None:
                style.set(X + 'Key', '{x:Type %s}' % sel_type)
            style.set('TargetType', sel_type)
            if theme_base_key:
                style.set('BasedOn', '{StaticResource %s}' % theme_base_key)
            else:
                style.attrib.pop('BasedOn', None)
            self.convert_setters(style, [s for s in all_setters if s.get('Property') != 'Template'])
            tmpl_triggers = []
            ts = next((s for s in all_setters if s.get('Property') == 'Template'), None)
            template_styles = []
            if ts is not None:
                tmpl_triggers = self.convert_template_in_setter(ts)
            trig = [t for t in (list(triggers_el) if triggers_el is not None else [])] + inherited_triggers
            nested = self.make_base_styles(tmpl_triggers + trig, in_template=True)
            nested += self.triggers_to_styles(trig + tmpl_triggers, in_template=True, theme_root=True)
            for n in nested:
                style.append(n)
            if self.hoisted_resources is not None:
                hoisted = self.hoisted_resources
                hoisted.tag = qn('ControlTheme.Resources')
                for c in list(hoisted):
                    if is_el(c):
                        self.conv.convert_element(c)
                rd = etree.Element(qn('ResourceDictionary'))
                for c in list(hoisted):
                    rd.append(c)
                hoisted.append(rd)
                style.insert(0, hoisted)
                self.hoisted_resources = None
        else:
            style.tag = qn('Style')
            style.attrib.pop('BasedOn', None)
            style.attrib.pop('TargetType', None)
            style.attrib.pop(X + 'Key', None)
            sel_t = sel_type.replace(':', '|')
            if class_name is not None:
                sel = '%s.%s' % (sel_t, class_name)
            elif key:
                sel = '%s.%s' % (sel_t, key.replace(' ', '_'))
            else:
                sel = sel_t
            style.set('Selector', sel)
            self.convert_setters(style, all_setters)
            trig = (list(triggers_el) if triggers_el is not None else []) + inherited_triggers
            for n in self.triggers_to_styles(trig, in_template=False, theme_root=False):
                style.append(n)
            if class_name is None and not as_theme:
                # goes to X.styles.axaml
                style.set('__plain', '1')
        if resources_el is not None:
            resources_el.tag = qn('ControlTheme.Resources' if as_theme else 'Style.Resources')
            self.conv.convert_tree(resources_el)
            rd = etree.Element(qn('ResourceDictionary'))
            for c in list(resources_el):
                rd.append(c)
            resources_el.append(rd)

    def convert_setters(self, parent, setters):
        for s in setters:
            self.convert_setter(s)

    def convert_setter(self, s):
        if s.tag != qn('Setter'):
            s.tag = qn('Setter')
        prop = s.get('Property')
        newp = self.map_prop(prop)
        if newp is None:
            if s.getparent() is not None:
                s.getparent().remove(s)
            return
        v = s.get('Value')
        if prop == 'ResizeMode':
            newp = 'CanResize'
            s.set('Value', 'False' if v == 'NoResize' else 'True')
            v = None
        if newp == 'IsVisible':
            if v is not None:
                s.set('Value', self.conv.convert_visibility(v, s))
        elif v is not None:
            s.set('Value', convert_markup_value(v, self.ctx, prop, s))
        s.set('Property', newp)
        for c in list(s):
            if is_el(c) and local(c.tag) == 'Setter.Value':
                c.tag = qn('Setter.Value')
                for cc in list(c):
                    if is_el(cc):
                        self.conv.convert_element(cc)

    def map_prop(self, prop):
        if prop.endswith('Resources.Renames') or prop.endswith('Resources.Replaces'):
            return None
        if prop == 'ItemContainerStyle':
            return 'ItemContainerTheme'
        m = re.match(r'^TextBlock\.(Foreground|FontSize|FontFamily|FontWeight|FontStyle)$', prop)
        if m:
            return 'TextElement.' + m.group(1)
        if prop in PROP_RENAME:
            return PROP_RENAME[prop]
        if '.' in prop:
            t, p = prop.rsplit('.', 1)
            if ':' not in t:
                return '%s.%s' % (TYPE_RENAME.get(t, t), p)
        return prop

    # -------------------------------------------------- templates
    def convert_template_in_setter(self, setter):
        value = setter.find('{%s}Setter.Value' % NS_WPF)
        if value is None:
            value = setter.find('{%s}Setter.Value' % NS_AV)
        tmpl = value.find('{%s}ControlTemplate' % NS_WPF) if value is not None else None
        if tmpl is None:
            tmpl = value.find('{%s}ControlTemplate' % NS_AV) if value is not None else None
        if tmpl is None:
            self.ctx.warn(setter, 'Template setter without inline ControlTemplate')
            return []
        triggers = self.prepare_template(tmpl)
        value.tag = qn('Setter.Value')
        setter.tag = qn('Setter')
        setter.attrib.pop('Value', None)
        return triggers

    def prepare_template(self, tmpl):
        res = next((c for c in tmpl if is_el(c) and local(c.tag) == 'ControlTemplate.Resources'), None)
        if res is not None:
            tmpl.remove(res)
            self.hoisted_resources = res
        trig = next((c for c in tmpl if is_el(c) and local(c.tag) == 'ControlTemplate.Triggers'), None)
        triggers = list(trig) if trig is not None else []
        if trig is not None:
            tmpl.remove(trig)
        tmpl.tag = qn('ControlTemplate')
        tt = tmpl.get('TargetType')
        if tt:
            tmpl.set('TargetType', type_name(tt))
        self.template_names = {}
        for el in tmpl.iter():
            if is_el(el):
                n = el.get(X + 'Name') or el.get('Name')
                if n:
                    self.template_names[n] = el
                # WPF’s ContentPresenter takes the content of the templated parent itself, in Avalonia it has to be bound
                if ns_of(el.tag) == NS_WPF and local(el.tag) == 'ContentPresenter' and el.get('Content') is None and el.get('ContentSource') is None \
                        and not any(is_el(c) and local(c.tag) == 'ContentPresenter.Content' for c in el):
                    el.set('Content', '{TemplateBinding Content}')
                    if el.get('ContentTemplate') is None:
                        el.set('ContentTemplate', '{TemplateBinding ContentTemplate}')
        # remember element types before conversion changes the tags
        self.template_types = {n: self.selector_type_of(e) for n, e in self.template_names.items()}
        for child in list(tmpl):
            if is_el(child):
                self.conv.convert_element(child)
        return triggers

    def convert_template_standalone(self, el):
        triggers = self.prepare_template(el)
        if triggers:
            self.ctx.warn(el, 'ControlTemplate with triggers outside of a style (%d triggers)' % len(triggers))

    def convert_data_template(self, el):
        hier = local(el.tag) == 'HierarchicalDataTemplate'
        trig = next((c for c in el if is_el(c) and local(c.tag) in ('DataTemplate.Triggers', 'HierarchicalDataTemplate.Triggers')), None)
        el.tag = qn('TreeDataTemplate' if hier else 'DataTemplate')
        triggers = list(trig) if trig is not None else []
        if trig is not None:
            el.remove(trig)
        res = next((c for c in el if is_el(c) and local(c.tag) in ('DataTemplate.Resources', 'HierarchicalDataTemplate.Resources')), None)
        if res is not None:
            # Avalonia’s data templates have no resources, those go to the root element of the template
            el.remove(res)
            root0 = next((c for c in el if is_el(c)), None)
            if root0 is not None:
                res.tag = retag(root0.tag, local(root0.tag) + '.Resources')
                root0.insert(0, res)
        self.template_names = {}
        for e in el.iter():
            if is_el(e):
                n = e.get(X + 'Name') or e.get('Name')
                if n:
                    self.template_names[n] = e
        self.template_types = {n: self.selector_type_of(e) for n, e in self.template_names.items()}
        self.conv.convert_attributes(el)
        for child in list(el):
            if is_el(child):
                self.conv.convert_element(child)
        if not triggers:
            return
        root = next((c for c in el if is_el(c)), None)
        if root is None:
            self.ctx.warn(el, 'DataTemplate triggers without content')
            return
        rname = root.get(X + 'Name') or root.get('Name')
        if not rname:
            rname = '__Dt%d' % self.ctx.state_counter
            self.ctx.state_counter += 1
            root.set(X + 'Name', rname)
            self.template_names[rname] = root
            self.template_types[rname] = self.selector_type_of(root)
        outer = etree.Element(qn('Style'))
        outer.set('Selector', self.sel(rname))
        for n in self.make_base_styles(triggers, in_template=False, root_name=rname) + self.triggers_to_styles(triggers, in_template=False, theme_root=False, in_data_template=True):
            outer.append(n)
        holder = etree.Element(qn('Control.Styles'))  # fixed below to proper owner type
        holder.tag = retag(root.tag, local(root.tag) + '.Styles')
        holder.append(outer)
        root.insert(0, holder)

    # -------------------------------------------------- triggers
    def new_state(self):
        n = self.ctx.state_counter
        self.ctx.state_counter += 1
        return 'S%d' % n

    def make_base_styles(self, triggers, in_template, root_name=None):
        """Moves local values of template elements, which are targeted by trigger setters, to styles (so triggers can override them)."""
        out = []
        moved = {}
        for trig in triggers:
            if not is_el(trig):
                continue
            for s in trig.iter('{%s}Setter' % NS_WPF):
                tn = s.get('TargetName')
                if not tn or tn not in self.template_names:
                    continue
                prop = self.map_prop(s.get('Property'))
                if prop is None:
                    continue
                el = self.template_names[tn]
                val = el.get(prop)
                if val is not None and (tn, prop) not in moved:
                    moved[(tn, prop)] = val
                    del el.attrib[prop]
        by_target = {}
        for (tn, prop), val in moved.items():
            by_target.setdefault(tn, []).append((prop, val))
        for tn, items in by_target.items():
            st = etree.Element(qn('Style'))
            st.set('Selector', '^ /template/ %s' % self.sel(tn) if in_template else '^ %s' % self.sel(tn))
            for prop, val in items:
                se = etree.SubElement(st, qn('Setter'))
                se.set('Property', prop)
                se.set('Value', val)
            out.append(st)
        return out

    def triggers_to_styles(self, triggers, in_template, theme_root, in_data_template=False):
        out = []
        for trig in triggers:
            if not is_el(trig):
                continue
            t = local(trig.tag)
            try:
                if t in ('Trigger', 'MultiTrigger', 'DataTrigger', 'MultiDataTrigger'):
                    out += self.convert_trigger(trig, in_template)
                elif t == 'EventTrigger':
                    self.ctx.warn(None, 'EventTrigger')
                    out.append(etree.Comment(' PORT: EventTrigger not converted '))
                else:
                    self.ctx.warn(None, 'unknown trigger %s' % t)
            except Exception as e:
                self.ctx.warn(None, 'trigger conversion failed: %r' % e)
                out.append(etree.Comment(' PORT: trigger conversion failed: %s ' % str(e).replace('--', '- -')))
        return out

    def cond_fragment(self, kind, prop, value, binding, sp_defs):
        if kind == 'prop':
            v = value
            if v is not None and is_markup(v):
                mk = parse_markup(v)
                if mk.name == 'x:Static' and mk.args:
                    v = mk.args[0].split('.')[-1]
                elif mk.name == 'x:Null':
                    v = 'null'
            if prop == 'ResizeMode':
                prop, v = 'CanResize', ('False' if v == 'NoResize' else 'True')
            prop = self.map_prop(prop) or prop
            if (prop, v) in PSEUDO:
                return PSEUDO[(prop, v)]
            if '.' in prop:
                t, pn = prop.rsplit('.', 1)
                return '[(%s.%s)=%s]' % (t.replace(':', '|'), pn, v)
            return '[%s=%s]' % (prop, v)
        sp = self.new_state()
        sp_defs.append((sp, binding, value))
        return '[(pa|T.%s)=True]' % sp

    def read_binding(self, trig, attr='Binding', prefix='DataTrigger'):
        b = trig.get(attr)
        if b is None:
            be = trig.find('{%s}%s.%s' % (NS_WPF, prefix, attr))
            if be is not None and len(be):
                inner = be[0]
                return ('element', inner)
            return None
        return convert_markup_value(b, self.ctx)

    def convert_trigger(self, trig, in_template):
        t = local(trig.tag)
        setters = [c for c in trig if is_el(c) and local(c.tag) == 'Setter']
        enter = trig.find('{%s}%s.EnterActions' % (NS_WPF, t))
        exit_ = trig.find('{%s}%s.ExitActions' % (NS_WPF, t))
        conds = []
        if t == 'Trigger':
            conds.append(('prop', trig.get('Property'), trig.get('Value'), None))
        elif t == 'DataTrigger':
            conds.append(('data', None, trig.get('Value'), self.read_binding(trig)))
        elif t == 'MultiTrigger':
            for c in trig.find('{%s}MultiTrigger.Conditions' % NS_WPF):
                conds.append(('prop', c.get('Property'), c.get('Value'), None))
        elif t == 'MultiDataTrigger':
            for c in trig.find('{%s}MultiDataTrigger.Conditions' % NS_WPF):
                conds.append(('data', None, c.get('Value'), self.read_binding(c, prefix='Condition')))
        sp_defs = []
        frags = []
        for kind, prop, value, binding in conds:
            if kind == 'data' and (binding is None or isinstance(binding, tuple)):
                self.ctx.warn(None, 'DataTrigger with element binding')
                return [etree.Comment(' PORT: DataTrigger with element binding not converted ')]
            frags.append(self.cond_fragment(kind, prop, value, binding, sp_defs))
        frag = frags[-1] if frags else ''
        outer = frags[:-1]
        out = []
        for sp, binding, value in sp_defs:
            out.append(self.make_state_style(sp, binding, value))
        groups = {}
        order = []
        for s in setters:
            tn = s.get('TargetName')
            groups.setdefault(tn, []).append(s)
            if tn not in order:
                order.append(tn)
        for tn in order:
            st = etree.Element(qn('Style'))
            if tn:
                st.set('Selector', ('^%s /template/ %s' if in_template else '^%s %s') % (frag, self.sel(tn)))
            else:
                st.set('Selector', '^%s' % frag)
            for s in groups[tn]:
                s2 = copy.deepcopy(s)
                s2.attrib.pop('TargetName', None)
                st.append(s2)
                self.convert_setter(s2)
            if len(st):
                # several conditions: nest the styles, so conditions are combined with AND
                for of in reversed(outer):
                    wrapper = etree.Element(qn('Style'))
                    wrapper.set('Selector', '^%s' % of)
                    wrapper.append(st)
                    st = wrapper
                out.append(st)
        if enter is not None or exit_ is not None:
            self.ctx.warn(None, 'trigger actions (storyboards) not converted: %s' % frag)
            out.append(etree.Comment(' PORT: storyboard actions of trigger %s not converted ' % frag.replace('--', '- -')))
        return out

    def make_state_style(self, sp, binding, value):
        st = etree.Element(qn('Style'))
        st.set('Selector', '^')
        s = etree.SubElement(st, qn('Setter'))
        s.set('Property', 'pa:T.%s' % sp)
        if binding and is_markup(binding):
            mk = parse_markup(binding)
            expected = value if value is not None else ''
            if is_markup(expected):
                m2 = parse_markup(expected)
                if m2.name == 'x:Static' and m2.args:
                    expected = m2.args[0].split('.')[-1]
                elif m2.name == 'x:Null':
                    expected = '{x:Null}'
            if mk.name == 'Binding':
                if mk.has('Converter'):
                    self.ctx.warn(None, 'data trigger binding with converter, parameter replaced')
                    mk.set('Converter', '{x:Static pc:StateConverter.Instance}')
                else:
                    mk.set('Converter', '{x:Static pc:StateConverter.Instance}')
                mk.set('ConverterParameter', expected)
                s.set('Value', str(mk))
            else:
                s.set('Value', binding)
        return st


_TB_RX = re.compile(r'\{TemplateBinding\s+([^}]+)\}')


def fix_template_bindings(root):
    """Setters of nested styles (outside of ControlTemplate) can't use TemplateBinding: use the templated parent binding instead."""
    for style in root.iter(qn('Style')):
        # Style which is not a child of a ControlTemplate content
        for setter in style.iter(qn('Setter')):
            v = setter.get('Value')
            if v and '{TemplateBinding' in v:
                setter.set('Value', _TB_RX.sub(lambda m: '{Binding %s, RelativeSource={RelativeSource TemplatedParent}}' % m.group(1).strip(), v))


_TEXT_PRESENTER_ATTRS = {
    'Text': '{TemplateBinding Text, Mode=TwoWay}', 'CaretIndex': '{TemplateBinding CaretIndex}',
    'SelectionStart': '{TemplateBinding SelectionStart}', 'SelectionEnd': '{TemplateBinding SelectionEnd}',
    'TextAlignment': '{TemplateBinding TextAlignment}', 'TextWrapping': '{TemplateBinding TextWrapping}',
    'PasswordChar': '{TemplateBinding PasswordChar}', 'RevealPassword': '{TemplateBinding RevealPassword}',
    'SelectionBrush': '{TemplateBinding SelectionBrush}', 'SelectionForegroundBrush': '{TemplateBinding SelectionForegroundBrush}',
    'CaretBrush': '{TemplateBinding CaretBrush}',
}


def fix_text_hosts(root):
    """WPF text boxes host text in a ScrollViewer called PART_ContentHost, Avalonia needs a TextPresenter called PART_TextPresenter."""
    for sv in list(root.iter(qn('ScrollViewer'))):
        if sv.get(X + 'Name') != 'PART_ContentHost':
            continue
        tmpl = sv.getparent()
        while tmpl is not None and tmpl.tag != qn('ControlTemplate'):
            tmpl = tmpl.getparent()
        target = (tmpl.get('TargetType') or '') if tmpl is not None else ''
        if 'TextBox' not in target and 'PasswordBox' not in target:
            continue
        sv.set(X + 'Name', 'PART_ScrollViewer')
        for k, v in (('HorizontalScrollBarVisibility', 'Disabled'), ('VerticalScrollBarVisibility', 'Disabled')):
            if sv.get(k) is None:
                sv.set(k, '{TemplateBinding (ScrollViewer.%s)}' % k)
        presenter = etree.SubElement(sv, qn('TextPresenter'))
        presenter.set(X + 'Name', 'PART_TextPresenter')
        for k, v in _TEXT_PRESENTER_ATTRS.items():
            presenter.set(k, v)


class ApiInfo:
    """Public members of Avalonia types (see gen_api_json.sh): members which don't exist there are dropped from converted XAML."""
    def __init__(self):
        path = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'api.json')
        self.types = json.load(open(path)) if os.path.exists(path) else {}
        self.by_name = {}
        for full, info in self.types.items():
            if info.get('asm', '').startswith('Avalonia') and (info.get('ns') or '').startswith('Avalonia'):
                self.by_name.setdefault(full.split('.')[-1], []).append(full)
        self.cache = {}

    def resolve(self, name):
        cands = self.by_name.get(name)
        if not cands:
            return None
        for pref in ('Avalonia.Controls.', 'Avalonia.Controls.Primitives.', 'Avalonia.Controls.Shapes.', 'Avalonia.Controls.Presenters.', 'Avalonia.Controls.Documents.'):
            for c in cands:
                if c.startswith(pref) and c.count('.') == pref.count('.'):
                    return c
        return cands[0]

    def members(self, name):
        if name in self.cache:
            return self.cache[name]
        full = self.resolve(name)
        if full is None:
            self.cache[name] = None
            return None
        props, events = set(), set()
        while full in self.types:
            info = self.types[full]
            props.update(info['p'])
            events.update(info['e'])
            full = info['b']
            if not full:
                break
        self.cache[name] = (props, events)
        return self.cache[name]

    def has(self, type_name, member):
        m = self.members(type_name)
        return True if m is None else member in m[0] or member in m[1]


_API = None


def api():
    global _API
    if _API is None:
        _API = ApiInfo()
    return _API


def _setter_target_type(setter):
    """Type name which the setter belongs to (TargetType of a theme, or the type in a selector), or None."""
    el = setter.getparent()
    while el is not None:
        if el.tag == qn('ControlTheme') and el.get('TargetType'):
            t = el.get('TargetType')
            return t if ':' not in t else None
        if el.tag == qn('Style') and el.get('Selector'):
            m = re.match(r'^([A-Za-z]\w*)(?=[.:\[#\s]|$)', el.get('Selector').strip())
            if m and not el.get('Selector').strip().startswith('^'):
                return m.group(1)
            if m is None and not el.get('Selector').strip().startswith('^'):
                return None
            # nested style, the type is defined by the parent
        el = el.getparent()
    return None


def drop_missing_members(root, ctx_todo=None):
    """Setters and attributes of properties which Avalonia's controls don't have."""
    a = api()
    if not a.types:
        return
    # attributes on elements of the default namespace
    for el in root.iter():
        if not is_el(el) or el.tag.split('}')[0] != '{' + NS_AV:
            continue
        name = local(el.tag)
        if name in ('Setter', 'Style', 'ControlTheme', 'ControlTemplate', 'DataTemplate', 'Styles', 'ResourceDictionary', 'Template', 'ResourceInclude', 'StyleInclude'):
            continue
        if a.members(name) is None:
            continue
        for attr in list(el.attrib):
            if attr.startswith('{') or '.' in attr or attr in ('Name',):
                continue
            if not a.has(name, attr):
                del el.attrib[attr]
                el.insert(0, etree.Comment(' PORT: removed %s (not in Avalonia) ' % attr))
    for setter in list(root.iter(qn('Setter'))):
        prop = setter.get('Property')
        if not prop or '.' in prop:
            continue
        t = _setter_target_type(setter)
        if t and not a.has(t, prop):
            parent = setter.getparent()
            comment = etree.Comment(' PORT: removed setter of %s (not in Avalonia) ' % prop)
            comment.tail = setter.tail
            parent.replace(setter, comment)


def merge_duplicate_styles(root):
    """Avalonia refuses duplicate setters (and merging equal selectors is cheaper): merge nested styles with identical selectors, last setter wins."""
    for parent in list(root.iter()):
        if not is_el(parent) or local(parent.tag) not in ('ControlTheme', 'Style', 'Styles'):
            continue
        by_sel = {}
        for child in list(parent):
            if is_el(child) and child.tag == qn('Style') and child.get('Selector') is not None:
                sel = child.get('Selector')
                if sel in by_sel:
                    first = by_sel[sel]
                    # nested styles of the duplicate are moved too
                    for c in list(child):
                        if is_el(c) and c.tag == qn('Setter'):
                            prop = c.get('Property')
                            for old in [x for x in first if is_el(x) and x.tag == qn('Setter') and x.get('Property') == prop]:
                                first.remove(old)
                            first.append(c)
                        else:
                            first.append(c)
                    parent.remove(child)
                else:
                    by_sel[sel] = child
        # setters within a single style: last one wins
        for st in [c for c in parent if is_el(c) and c.tag in (qn('Style'), qn('ControlTheme'))] + [parent]:
            seen = {}
            for c in list(st):
                if is_el(c) and c.tag == qn('Setter'):
                    prop = c.get('Property')
                    if prop in seen:
                        st.remove(seen[prop])
                    seen[prop] = c


def finalize_names(root):
    """All remaining WPF-namespaced elements -> Avalonia namespace"""
    for el in root.iter():
        if not is_el(el):
            continue
        ns = ns_of(el.tag)
        if ns == NS_WPF:
            el.tag = qn(local(el.tag))
        elif ns and ns.startswith('clr-namespace:System;assembly='):
            el.tag = '{using:System}' + local(el.tag)
        for a in list(el.attrib):
            if a.startswith('{') and etree.QName(a).namespace in (NS_D,):
                del el.attrib[a]


IMPLICIT_SOURCES = []   # avares URIs of dictionaries with keyless data templates (see implicit_templates_cs)


def has_template_setter(style):
    return any(is_el(c) and local(c.tag) == 'Setter' and c.get('Property') == 'Template' for c in style)


def relocate_implicit(root, uri):
    """
    WPF dictionaries may contain implicit styles and data templates (without keys). Avalonia’s ones can’t, so
    - styles from `X.Resources` are moved to `X.Styles` (or nested into the style/theme X)
    - data templates from `X.Resources` are moved to `X.DataTemplates`
    - keyless data templates in a dictionary itself get a key (they are registered at startup, see ImplicitDataTemplates.g.cs)
    """
    for res in [e for e in root.iter() if is_el(e) and local(e.tag).endswith('.Resources') and local(e.tag) != 'ResourceDictionary']:
        owner = res.getparent()
        if owner is None:
            continue
        container = res
        inner = [c for c in res if is_el(c) and local(c.tag) == 'ResourceDictionary']
        if len(inner) == 1:
            container = inner[0]
        for c in list(container):
            if not is_el(c) or c.get(X + 'Key') is not None:
                continue
            t = local(c.tag)
            ot = local(owner.tag)
            if t == 'Style' and c.get('Selector'):
                container.remove(c)
                if ot in ('Style', 'ControlTheme'):
                    sel = c.get('Selector')
                    if ot == 'ControlTheme' and any(is_el(x) and local(x.tag) == 'Setter' and x.get('Property') == 'Template' for x in owner):
                        c2 = copy.deepcopy(c)
                        c2.set('Selector', '^ /template/ ' + sel)
                        owner.append(c2)
                    c.set('Selector', '^ ' + sel)
                    owner.append(c)
                else:
                    holder_tag = retag(owner.tag, ot + '.Styles')
                    holder = next((h for h in owner if is_el(h) and h.tag == holder_tag), None)
                    if holder is None:
                        holder = etree.Element(holder_tag)
                        owner.insert(0, holder)
                    holder.append(c)
            elif t in ('DataTemplate', 'TreeDataTemplate') and c.get('DataType') and ot not in ('Style', 'ControlTheme'):
                container.remove(c)
                holder_tag = retag(owner.tag, ot + '.DataTemplates')
                holder = next((h for h in owner if is_el(h) and h.tag == holder_tag), None)
                if holder is None:
                    holder = etree.Element(holder_tag)
                    owner.insert(0, holder)
                holder.append(c)
        # empty leftovers
        if len(container) == 0 and container is not res:
            res.remove(container)
        if len(res) == 0:
            owner.remove(res)
    if local(root.tag) == 'ResourceDictionary':
        found = False
        for c in root:
            if is_el(c) and local(c.tag) in ('DataTemplate', 'TreeDataTemplate') and c.get(X + 'Key') is None and c.get('DataType'):
                dt = c.get('DataType')
                c.set(X + 'Key', 'ImplicitTemplate:' + re.sub(r'[{}]|x:Type\s*', '', dt).strip())
                found = True
        if found and uri:
            IMPLICIT_SOURCES.append(uri)


def implicit_templates_cs(namespace, assembly):
    items = ',\n'.join('            "%s"' % u for u in sorted(set(IMPLICIT_SOURCES)))
    return '''// <auto-generated> by PortTools/xaml_convert.py: dictionaries with implicit (keyless in WPF) data templates </auto-generated>
namespace %s {
    public static class ImplicitDataTemplates {
        public static readonly string[] Sources = {
%s
        };
    }
}
''' % (namespace, items)


def convert_file(path, registry, out_dir=None):
    parser = etree.XMLParser(remove_blank_text=False, remove_comments=False, resolve_entities=False)
    tree = etree.parse(path, parser)
    root = tree.getroot()
    ctx = Ctx(path, registry)
    conv = Converter(ctx)
    conv.convert_root(root)
    finalize_names(root)
    merge_duplicate_styles(root)
    fix_text_hosts(root)
    drop_missing_members(root)
    fix_template_bindings(root)
    cs = os.path.splitext(path)[0] + '.xaml.cs'
    fix_handler_types(cs, ctx.handlers)
    relocate_implicit(root, 'avares://%s/%s.axaml' % (ASM[0], os.path.relpath(os.path.splitext(path)[0], ASM[1]).replace(os.sep, '/')))

    for e in root.iter():
        if is_el(e) and e.tag.endswith('}TreeDataTemplate'):
            for a in ('ItemTemplate', 'ItemContainerStyle', 'ItemTemplateSelector'):
                e.attrib.pop(a, None)
        if is_el(e) and e.get('Selector'):
            e.set('Selector', e.get('Selector').replace('=PositiveInfinity]', '=Infinity]').replace('=NegativeInfinity]', '=-Infinity]'))
        if is_el(e) and e.tag.endswith('}Setter') and e.get('Property') == 'LineDashArray':
            e.set('Value', e.get('Value').replace(' ', ','))
    # namespaces
    new_nsmap = {}
    for p, u in root.nsmap.items():
        if u == NS_WPF:
            new_nsmap[p] = NS_AV
        elif u == NS_D:
            continue
        else:
            new_nsmap[p] = u
    for hp, hu in HELPER_NS.items():
        new_nsmap.setdefault(hp, hu)
    for p, u in list(new_nsmap.items()):
        if u.startswith('clr-namespace:System;assembly=mscorlib'):
            new_nsmap[p] = 'using:System'
        elif u.startswith('clr-namespace:System;assembly=System') or u == 'clr-namespace:System;assembly=System.Runtime':
            new_nsmap[p] = 'using:System'
    if 'mc' in new_nsmap and root.get('{%s}Ignorable' % NS_MC) == 'd':
        del root.attrib['{%s}Ignorable' % NS_MC]
        new_nsmap.pop('mc', None)
    base = os.path.splitext(path)[0]
    is_dict = local(root.tag) == 'ResourceDictionary'

    # split plain styles of dictionaries
    plain = []
    if is_dict:
        for c in list(root):
            if is_el(c) and local(c.tag) == 'Style' and c.get('__plain') == '1':
                del c.attrib['__plain']
                root.remove(c)
                plain.append(c)
    outputs = []
    new_root = etree.Element(root.tag, nsmap=new_nsmap)
    for a, v in root.attrib.items():
        new_root.set(a, v)
    new_root.text = root.text
    for c in list(root):
        new_root.append(c)
    outputs.append((base + '.axaml', new_root))
    if plain:
        sroot = etree.Element(qn('Styles'), nsmap=new_nsmap)
        sroot.text = '\n  '
        # resources of the dictionary are needed for StaticResource lookups: include the dictionary
        res = etree.SubElement(sroot, qn('Styles.Resources'))
        rd = etree.SubElement(res, qn('ResourceDictionary'))
        md = etree.SubElement(rd, qn('ResourceDictionary.MergedDictionaries'))
        inc = etree.SubElement(md, qn('ResourceInclude'))
        inc.set('Source', 'avares://%s/%s.axaml' % (ASM[0], os.path.relpath(base, ASM[1]).replace(os.sep, '/')))
        for p in plain:
            sroot.append(p)
        outputs.append((base + '.styles.axaml', sroot))
    for p, r in outputs:
        for el in r.iter():
            if not isinstance(el.tag, str):
                continue
            el.attrib.pop('__plain', None)
            # attached property selectors need a type, `:is(Control)` matches everything
            if local(el.tag) == 'Style' and el.get('Selector') and not typed_parent(el):
                el.set('Selector', re.sub(r'(^|[\s>^,])(\[\()', r'\1:is(Control)\2', el.get('Selector')))
        etree.cleanup_namespaces(r, top_nsmap=new_nsmap, keep_ns_prefixes=[k for k in new_nsmap if k])
        s = etree.tostring(r, encoding='unicode')
        # lxml likes to invent prefixes for elements which got created in other contexts
        for m in set(re.findall(r'xmlns:(ns\d+)="%s"' % re.escape(NS_AV), s)):
            s = re.sub(r' xmlns:%s="%s"' % (m, re.escape(NS_AV)), '', s)
            s = re.sub(r'(</?)%s:' % m, r'\1', s)
            s = re.sub(r'(\s)%s:' % m, r'\1', s)
        for m, uri in set(re.findall(r'xmlns:(ns\d+)="([^"]+)"', s)):
            known = [k for k, v in new_nsmap.items() if v == uri and k]
            if known:
                s = re.sub(r' xmlns:%s="%s"' % (m, re.escape(uri)), '', s)
                s = re.sub(r'(</?)%s:' % m, r'\g<1>%s:' % known[0], s)
                s = re.sub(r'(\s)%s:' % m, r'\g<1>%s:' % known[0], s)
        with open(p, 'w', encoding='utf-8-sig') as f:
            f.write(s)
    return ctx, [p for p, _ in outputs]


ASM = ['__ASM__', '.']


def main(argv):
    scans, files, all_dirs = [], [], []
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--scan':
            scans.append(argv[i + 1]); i += 2; continue
        if a == '--all':
            all_dirs.append(argv[i + 1]); i += 2; continue
        if a == '--asm':
            ASM[0] = argv[i + 1]; ASM[1] = os.path.abspath(argv[i + 2]); i += 3; continue
        files.append(a); i += 1
    reg = Registry()
    reg.scan(scans + all_dirs + [os.path.dirname(f) for f in files])
    for d in all_dirs:
        for root, dirs, fs in os.walk(d):
            if '/obj' in root or '/bin' in root or '/Publish' in root:
                continue
            for f in fs:
                if f.endswith('.xaml'):
                    files.append(os.path.join(root, f))
    total = 0
    for f in files:
        try:
            ctx, outs = convert_file(f, reg)
        except Exception as e:
            print('FAILED', f, repr(e))
            continue
        total += len(ctx.todo)
        print('%s -> %s (%d todo)' % (f, ', '.join(os.path.basename(o) for o in outs), len(ctx.todo)))
        for t in ctx.todo[:5]:
            print('    -', t)
    if IMPLICIT_SOURCES:
        out = os.path.join(ASM[1], 'ImplicitDataTemplates.g.cs')
        with open(out, 'w', encoding='utf-8') as f:
            f.write(implicit_templates_cs(ASM[0], ASM[0]))
        print('implicit data templates in', len(set(IMPLICIT_SOURCES)), 'dictionaries ->', out)
    print('total todo', total)


if __name__ == '__main__':
    main(sys.argv[1:])
