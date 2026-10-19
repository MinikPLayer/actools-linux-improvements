# Manual fixes for XAML converted from FirstFloor.ModernUI (applied after xaml_convert.py)
PATCHES = [
    ('Themes/ModernWindow.axaml', [
        ('re', r'<Setter Property="RenderTransform">\s*<Setter.Value>\s*<TranslateTransform X="\{Binding IconPathThickness[^>]*/>\s*</Setter.Value>\s*</Setter>', '<Setter Property="RenderTransform" Value="translate(2px, -3px)"/>'),
        ('re', r'<Setter Property="BackgroundContent" Value="\{StaticResource WindowBackgroundContent\}"/>',
         '<Setter Property="BackgroundContent"><Template><Border Background="{DynamicResource WindowBackgroundContentBrush}"/></Template></Setter>'),
        ('re', r'<Setter Property="CloseButton" Value="\{StaticResource WindowCloseButtonDefault\}"/>',
         '<Setter Property="CloseButton"><Template><Button Command="{Binding Source={x:Static sw:SystemCommands.CloseWindowCommand}}" ToolTip.Tip="{x:Static mui:UiStrings.Close}" Theme="{StaticResource SystemCloseButton}"><c:Cell Width="10" Height="10" ClipToBounds="True" VerticalAlignment="Center" HorizontalAlignment="Center"><Line Stroke="{Binding (TextBlock.Foreground), RelativeSource={RelativeSource Self}}" StartPoint="-5.01,-5.01" EndPoint="14.99,14.99" StrokeThickness="1"/><Line Stroke="{Binding (TextBlock.Foreground), RelativeSource={RelativeSource Self}}" StartPoint="-5.01,14.99" EndPoint="14.99,-5.01" StrokeThickness="1"/></c:Cell></Button></Template></Setter>'),
        ('re', r'\s*<Border x:Key="WindowBackgroundContent"[^>]*/>', ''),
        ('re', r'\s*<Button x:Key="WindowCloseButtonDefault".*?</Button>', ''),
    ]),
    ('Themes/ModernDialog.axaml', [
        ('re', r'<Setter Property="BackgroundContent" Value="\{StaticResource DialogBackgroundContent\}"/>',
         '<Setter Property="BackgroundContent"><Template><Border Background="{DynamicResource DialogBackgroundContentBrush}"/></Template></Setter>'),
        ('re', r'\s*<Border x:Key="DialogBackgroundContent"[^>]*/>', ''),
    ]),

    ('Assets/Fonts.axaml', [
        ('<FontFamily x:Key="DefaultFont">Segoe UI</FontFamily>', '<FontFamily x:Key="DefaultFont">Segoe UI, Open Sans, Noto Sans, DejaVu Sans, sans-serif</FontFamily>'),
        ('<FontFamily x:Key="MonospaceFont">Consolas</FontFamily>', '<FontFamily x:Key="MonospaceFont">Consolas, DejaVu Sans Mono, monospace</FontFamily>'),
        ('/FirstFloor.ModernUI;component/Fonts/#Segoe Condensed', 'avares://FirstFloor.ModernUI/Fonts#Segoe Condensed'),
    ]),

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
