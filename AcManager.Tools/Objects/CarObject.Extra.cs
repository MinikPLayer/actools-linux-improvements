using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AcManager.Tools.Helpers;
using AcManager.Tools.Miscellaneous;
using AcTools.AcdFile;
using AcTools.DataFile;
using AcTools.Utils;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI;
using FirstFloor.ModernUI.Commands;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Windows.Controls;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

#pragma warning disable 649

namespace AcManager.Tools.Objects {
    public sealed partial class CarObject : ICupSupportedObject {
        private LazierThis<double?> _steerLock;
        public double? SteerLock => _steerLock.Get(() => AcdData?.GetIniFile("car.ini")["CONTROLS"].GetDoubleNullable("STEER_LOCK") * 2d);

        public CupContentType CupContentType => CupContentType.Car;
        public bool IsCupUpdateAvailable => CupClient.Instance?.ContainsAnUpdate(CupContentType.Car, Id, Version) ?? false;
        public CupClient.CupInformation CupUpdateInformation => CupClient.Instance?.GetInformation(CupContentType.Car, Id);

        protected override void OnVersionChanged() {
            OnPropertyChanged(nameof(IsCupUpdateAvailable));
            OnPropertyChanged(nameof(CupUpdateInformation));
        }

        void ICupSupportedObject.OnCupUpdateAvailableChanged() {
            OnPropertyChanged(nameof(IsCupUpdateAvailable));
            OnPropertyChanged(nameof(CupUpdateInformation));
        }

        public void SetValues(string author, string informationUrl, string version) {
            Author = author;
            Url = informationUrl;
            if (version.CompareAsVersionTo(Version) > 0) Version = version;
            SaveAsync();
        }

        [CanBeNull]
        private JObject ReadCmTexturesJson() {
            if (!File.Exists(CmTexturesJsonFilename)) return null;

            try {
                return JObject.Parse(File.ReadAllText(CmTexturesJsonFilename));
            } catch (Exception e) {
                NonfatalError.Notify("Can’t load car’s textures description", e);
                return null;
            }
        }

        [CanBeNull]
        private string ReadCmTexturesScript(out ExtraDataProvider dataProvider) {
            if (!File.Exists(CmTexturesScriptFilename)) {
                dataProvider = null;
                return null;
            }

            try {
                dataProvider = new ExtraDataProvider(CmTexturesScriptFilename);
                return File.ReadAllText(CmTexturesScriptFilename);
            } catch (Exception e) {
                NonfatalError.Notify("Can’t load car’s textures description", e);
                dataProvider = null;
                return null;
            }
        }

        [NotNull]
        private byte[] PrepareTexture(string filename, string textureEffect) {
            var effect = TextureEffects.Parse(textureEffect);
            return effect == null ? File.ReadAllBytes(filename) : TextureEffects.ApplyToFile(filename, effect);
        }

        private void SaveExtraCmTexture([NotNull] string textureName, [NotNull] Func<byte[]> actualSave) {
            try {
                var alreadySaved = false;
                string alreadySavedFilename = null;

                foreach (var skinObject in EnabledOnlySkins.ToList()) {
                    if (!Directory.Exists(skinObject.Location)) return;
                    var location = Path.Combine(skinObject.Location, textureName);

                    if (alreadySaved) {
                        if (alreadySavedFilename == null) {
                            FileUtils.TryToDelete(location);
                        } else {
                            FileUtils.HardLinkOrCopy(alreadySavedFilename, location, true);
                        }
                    } else {
                        try {
                            alreadySaved = true;
                            var data = actualSave();
                            if (data == null) {
                                FileUtils.TryToDelete(location);
                            } else {
                                File.WriteAllBytes(location, data);
                                alreadySavedFilename = location;
                            }
                        } catch (Exception e) {
                            Logging.Error(e);
                        }
                    }
                }
            } catch (Exception e) {
                NonfatalError.NotifyBackground($"Can’t set texture “{textureName}”", e);
            }
        }

        private void SetTrackOutlineTexture([CanBeNull] string filename, [NotNull] string textureName, [CanBeNull] string textureEffect) {
            SaveExtraCmTexture(textureName, () => {
                if (filename == null || !File.Exists(filename)) return null;
                return ActionExtension.InvokeInMainThread(() => PrepareTexture(filename, textureEffect));
            });
        }

        private void PrepareTrackOutlineTexture([NotNull] JToken cmTextures, [CanBeNull] TrackObjectBase track) {
            var outline = cmTextures.GetStringValueOnly("trackOutline");
            if (outline != null) {
                SetTrackOutlineTexture(track?.OutlineImage, outline, cmTextures.GetStringValueOnly("trackOutlineEffect"));
            }
        }

        private void RunCmTexturesJson([NotNull] JObject jObject, [NotNull] RaceTexturesContext texturesContext) {
            PrepareTrackOutlineTexture(jObject, texturesContext.Track);
        }

        public class RaceTexturesContext {
            [CanBeNull]
            public TrackObjectBase Track;

            [CanBeNull]
            public WeatherObject Weather;

            public double? Temperature, Wind, WindDirection;
        }

        public void PrepareRaceTextures([NotNull] RaceTexturesContext texturesContext) {
            var d = ReadCmTexturesJson();
            if (d != null) {
                try {
                    RunCmTexturesJson(d, texturesContext);
                } catch (Exception e) {
                    NonfatalError.NotifyBackground("Can’t prepare car textures", e);
                }
            }

            var s = ReadCmTexturesScript(out var data);
            if (s != null) {
                try {
                    RunCmTexturesScript(s, data, texturesContext,
                            (textureName, textureData) => SaveExtraCmTexture(textureName, () => textureData));
                } catch (Exception e) {
                    NonfatalError.NotifyBackground("Can’t run car textures script", e);
                }
            }
        }

        private DelegateCommand _packDataCommand;

        public DelegateCommand PackDataCommand => _packDataCommand ?? (_packDataCommand = new DelegateCommand(() => {
            try {
                var destination = Path.Combine(Location, "data.acd");
                var exists = File.Exists(destination);

                if (Author == AuthorKunos
                        ? ModernDialog.ShowMessage(ContentUtils.GetString("AppStrings", "Car_PackKunosDataMessage"), ToolsStrings.Common_Warning,
                                MessageBoxButton.YesNo) != MessageBoxResult.Yes
                        : exists && ModernDialog.ShowMessage(ContentUtils.GetString("AppStrings", "Car_PackExistingDataMessage"), ToolsStrings.Common_Warning,
                                MessageBoxButton.YesNo) != MessageBoxResult.Yes) {
                    return;
                }

                if (exists) {
                    FileUtils.Recycle(destination);
                }

                Acd.FromDirectory(Path.Combine(Location, "data")).Save(destination);
                UpdateAcdData();
                WindowsHelper.ViewFile(destination);
            } catch (Exception e) {
                NonfatalError.Notify(ContentUtils.GetString("AppStrings", "Car_CannotPackData"), ToolsStrings.Common_MakeSureThereIsEnoughSpace, e);
            }
        }, () => Directory.Exists(Path.Combine(Location, "data"))));
    }
}