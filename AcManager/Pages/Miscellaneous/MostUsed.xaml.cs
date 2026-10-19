using System.Collections.Generic;
using System.Linq;
using AcManager.Tools.Helpers;
using AcManager.Tools.Profile;
using FirstFloor.ModernUI.Helpers;

namespace AcManager.Pages.Miscellaneous {
    public partial class MostUsed : global::AcManager.Pages.Miscellaneous.MostSomethingBase {
        protected override void Initialize(ViewModel viewModel) {
            DataContext = viewModel;
            InitializeComponent();
        }

        protected override double GetCarValue(string carId, IStorage storage) {
            return PlayerStatsManager.Instance.GetDistanceDrivenByCar(carId, storage) / 1e3;
        }

        protected override double GetTrackValue(string trackId, IStorage storage) {
            return PlayerStatsManager.Instance.GetDistanceDrivenAtTrack(trackId, storage) / 1e3;
        }

        protected override string GetDisplayValue(double value) {
            return string.Format(SettingsHolder.CommonSettings.DistanceFormat, value * SettingsHolder.CommonSettings.DistanceMultiplier);
        }

        protected override double CalculateTotalTrackValue(IEnumerable<double> value) {
            return value.Sum();
        }
    }
}