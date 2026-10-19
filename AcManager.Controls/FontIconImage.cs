using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using FirstFloor.ModernUI.Windows.Controls;

namespace AcManager.Controls {
    public class FontIconImage : BetterImage {
        protected override System.Type StyleKeyOverride => typeof(FontIconImage);

        public FontIconImage() {
            ShowBroken = false;
        }

        // PORT: FindCropArea override (cropping of transparent areas) is dropped, BetterImage has no cropping on Avalonia yet

        static FontIconImage() {
        }
    }
}