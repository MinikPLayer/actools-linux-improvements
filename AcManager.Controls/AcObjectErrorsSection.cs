using System.Windows;
using System.Windows.Controls;
using AcManager.Tools.AcObjectsNew;

namespace AcManager.Controls {
    public class AcObjectErrorsSection : Control {
        protected override System.Type StyleKeyOverride => typeof(AcObjectErrorsSection);

        static AcObjectErrorsSection() {
        }

        public static readonly StyledProperty<AcCommonObject> AcObjectProperty = DP.Register<AcCommonObject>(nameof(AcObject),
                typeof(AcObjectErrorsSection));

        public AcCommonObject AcObject {
            get { return (AcCommonObject) GetValue(AcObjectProperty); }
            set { SetValue(AcObjectProperty, value); }
        }
    }
}
