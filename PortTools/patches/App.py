# manual fixes of converted AcManager XAML (applied by reconvert_app.sh after the conversion)
PATCHES = [
    ('Pages/Dialogs/SelectTrackDialog.axaml', [
        ('''<!-- PORT: removed VisualStateManager.VisualStateGroups -->''',
         '''<mui:BetterImage.Transitions><Transitions><DoubleTransition Property="Opacity" Duration="0:0:0.3"/></Transitions></mui:BetterImage.Transitions>'''),
    ]),
]
