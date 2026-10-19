# manual fixes of converted AcManager.Controls XAML (applied by reconvert_controls.sh after the conversion)
PATCHES = [
    ('Assets/TextEditor.styles.axaml', [
        ('xmlns:ae="http://icsharpcode.net/sharpdevelop/avalonedit"',
         'xmlns:ae="using:AvaloniaEdit" xmlns:aee="using:AvaloniaEdit.Editing" xmlns:aer="using:AvaloniaEdit.Rendering"'),
        ('Selector="ae|TextArea"', 'Selector="aee|TextArea"'),
        ('Selector="ae|TextView"', 'Selector="aer|TextView"'),
    ]),
    ('Themes/AcListPage.axaml', [
        (' Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}"', ''),
    ]),
    ('Dialogs/CupInformationDialog.axaml', [
        (' SelectionChanged="OnHyperlinkMouseSelection"', ''),
    ]),
    ('Themes/GraphDataViewer.axaml', [
        ('<oxy:ThicknessConverter x:Key="ThicknessConverter"/>', ''),
        ('<Setter Property="LineDashArray" Value="4,2"/>', ''),
        ('StrokeDashArray="{TemplateBinding LineDashArray}"', 'StrokeDashArray="4,2"'),
        ('StrokeThickness="{TemplateBinding BorderThickness, Converter={StaticResource ThicknessConverter}}"', 'StrokeThickness="1"'),
        ('<Setter Property="HorizontalLineVisibility" Value="Visible"/>', '<Setter Property="HorizontalLineVisibility" Value="True"/>'),
        ('<Setter Property="VerticalLineVisibility" Value="Visible"/>', '<Setter Property="VerticalLineVisibility" Value="True"/>'),
    ]),
]
