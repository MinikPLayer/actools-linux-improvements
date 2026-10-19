using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcManager.Controls;
using AcManager.Tools.Objects;
using FirstFloor.ModernUI.Dialogs;
using AcManager.Tools.Helpers;
using Avalonia.Media;

namespace AcManager.CustomShowroom {
    // The 3D showrooms are built on top of AcTools.Render.Wrapper (WinForms windows with Direct3D 11 swap chains), which is Windows-only.
    // Until they are ported (see LinuxPortDocs.md), these stand-ins keep the rest of the app working and tell user about it.
    // Compiled only if IncludeShowrooms is not set (see AcManager.csproj and Deferred.props).
    internal static class ShowroomStubHelper {
        public static Task NotAvailable() {
            MessageDialog.Show("3D showrooms are not available in this version: they need Direct3D, which is only present on Windows.",
                    "Showroom is not available", MessageBoxButton.OK);
            return Task.CompletedTask;
        }
    }

    public class CustomShowroomWrapper : ICustomShowroomWrapper {
        public static Task StartAsync(string kn5, string skinId = null, string presetFilename = null) => ShowroomStubHelper.NotAvailable();

        public static Task StartAsync(CarObject car, CarSkinObject skin = null, string presetFilename = null) => ShowroomStubHelper.NotAvailable();

        public static Task StartAsync(CarObject car, string customKn5, IEnumerable<CustomShowroomLodDefinition> lodDefinitions = null,
                CarSkinObject skin = null, string presetFilename = null) => ShowroomStubHelper.NotAvailable();

        Task ICustomShowroomWrapper.StartAsync(CarObject car, CarSkinObject skin, string presetFilename) => ShowroomStubHelper.NotAvailable();

        string ICustomShowroomWrapper.PresetableKeyValue => DarkRendererSettingsValues.DefaultPresetableKeyValue;
    }

    public static class TrackMapRendererWrapper {
        public static Task Run(TrackObjectBase track, bool aiLane) => ShowroomStubHelper.NotAvailable();
    }

    public static class TrackOutlineRendererWrapper {
        public static Task Run(TrackObjectBase track) => ShowroomStubHelper.NotAvailable();
        public static Task UpdateAsync(TrackObjectBase track) => ShowroomStubHelper.NotAvailable();
    }

    public static class CmPreviewsFormWrapper {
        public static Task<IReadOnlyList<UpdatePreviewError>> Run(IReadOnlyList<ToUpdatePreview> toUpdate, string presetFilename) {
            return ShowroomStubHelper.NotAvailable().ContinueWith(x => (IReadOnlyList<UpdatePreviewError>)new List<UpdatePreviewError>());
        }
    }

    internal static class AttachedHelper {
        public static bool AnyRunning => false;
    }

    public static class LiteShowroomFormWrapperWithTools {
        public static bool OptionAttachedToolsVerboseMode = false;
    }

    public static class LiteShowroomTools {
        public static ILiveryGenerator LiveryGenerator { get; set; }
    }
}
