using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using AcManager.Tools.Objects;

namespace AcManager.Controls {
    [ContentProperty(nameof(PreviewContent))]
    public class TrackBlock : Control {
        static TrackBlock() {
        }

        public static readonly StyledProperty<bool> ShowPreviewProperty = DP.Register<bool>(nameof(ShowPreview),
                typeof(TrackBlock), new PropertyMetadata(true));

        public bool ShowPreview {
            get => GetValue(ShowPreviewProperty) as bool? == true;
            set => SetValue(ShowPreviewProperty, value);
        }

        public static readonly StyledProperty<TrackObjectBase> TrackProperty = DP.Register<TrackObjectBase>(nameof(Track),
                typeof(TrackBlock));

        public TrackObjectBase Track {
            get => (TrackObjectBase)GetValue(TrackProperty);
            set => SetValue(TrackProperty, value);
        }

        public static readonly StyledProperty<object> PreviewContentProperty = DP.Register<object>(nameof(PreviewContent),
                typeof(TrackBlock));

        public object PreviewContent {
            get => GetValue(PreviewContentProperty);
            set => SetValue(PreviewContentProperty, value);
        }
    }
}