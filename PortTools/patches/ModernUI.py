# Manual fixes for XAML converted from FirstFloor.ModernUI (applied after xaml_convert.py)
PATCHES = [
    ('Themes/ModernFrame.axaml', [
        ('re', r'<ControlTheme\.Resources>.*?</ControlTheme\.Resources>', ''),
    ]),
    ('Dialogs/WaitingDialog.axaml', [
        ('re', r'<c:ModernProgressRing IsActive="True" Width="80" Height="80" HorizontalAlignment="Center"/>', '<ProgressBar IsIndeterminate="True" Width="80" HorizontalAlignment="Center"/>'),
    ]),
    ('Assets/ListBox.axaml', [
        ('re', r'<Style Selector="\^:selected /template/ Border#PART_Border"><Setter Property="Style" Value="\{StaticResource HighlightedBackground\}"/>\s*</Style>', ''),
    ]),

    ('Assets/Button.styles.axaml', [
        ('<Style Selector="TemplatedControl.', '<Style Selector="Button.'),
    ]),
    ('Assets/ContextMenu.axaml', [
        ('re', r'\s*<Setter Property="VerticalContentAlignment" Value="Center"/>', ''),
    ]),

    ('Themes/ModernMenu.axaml', [
        ('re', r'\s*<ResourceInclude Source="avares://FirstFloor.ModernUI/Assets/(TextBox|BetterTextBox).axaml"/>', ''),
    ]),
    ('Themes/ModernTab.axaml', [
        ('re', r'\s*<ResourceInclude Source="avares://FirstFloor.ModernUI/Assets/BetterTextBox.axaml"/>', ''),
    ]),
    ('Assets/Button.axaml', [
        ('re', r'\s*<ResourceInclude Source="avares://FirstFloor.ModernUI/Assets/(Easing|ModernProgressRing.Special).axaml"/>', ''),
    ]),
    ('Assets/Converters.axaml', [
        ('re', r'\s*<c:(ChainConverter|IconPathToImageConverter) x:Key="[^"]+"/>', ''),
    ]),

    ('Themes/ModernFrame.axaml', [
        ('    <Setter Property="BorderBrush" Value="{x:Null}"/>\n', ''),
        (' c:ModernFrame.KeepContentAlive="False"', ''),
    ]),
]
