using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Presentation;
using JetBrains.Annotations;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Microsoft.Win32;
using Avalonia.Threading;

namespace FirstFloor.ModernUI.Dialogs {
    public sealed class DialogFilterPiece : Displayable {
        public static readonly DialogFilterPiece AllFiles = new DialogFilterPiece("All files", "*.*");
        public static readonly DialogFilterPiece DdsFiles = new DialogFilterPiece("DDS files", "*.dds");
        public static readonly DialogFilterPiece XmlFiles = new DialogFilterPiece("XML Files", "*.xml");
        public static readonly DialogFilterPiece IniFiles = new DialogFilterPiece("INI Files", "*.ini");
        public static readonly DialogFilterPiece JpegFiles = new DialogFilterPiece("JPEG files", "*.jpg", "*.jpeg");
        public static readonly DialogFilterPiece PngFiles = new DialogFilterPiece("PNG files", "*.png");
        public static readonly DialogFilterPiece DdsAndTiffFiles = new DialogFilterPiece("DDS & TIFF files", "*.dds", "*.tif", "*.tiff");
        public static readonly DialogFilterPiece ImageFiles = new DialogFilterPiece("Image files", "*.dds", "*.tif", "*.tiff", "*.jpg", "*.jpeg", "*.png");
        public static readonly DialogFilterPiece Applications = new DialogFilterPiece("Applications", "*.exe");
        public static readonly DialogFilterPiece ZipFiles = new DialogFilterPiece("ZIP archives", "*.zip");
        public static readonly DialogFilterPiece TextFiles = new DialogFilterPiece("Text files", "*.txt");
        public static readonly DialogFilterPiece ConfigFiles = new DialogFilterPiece("Config files", "*.cfg");
        public static readonly DialogFilterPiece FbxFiles = new DialogFilterPiece("FBX models", "*.fbx");
        public static readonly DialogFilterPiece Kn5Files = new DialogFilterPiece("KN5 models", "*.kn5");
        public static readonly DialogFilterPiece LutTables = new DialogFilterPiece("LUT tables", "*.lut");
        public static readonly DialogFilterPiece CsvTables = new DialogFilterPiece("LUT tables", "*.csv");
        public static readonly DialogFilterPiece TarGZipFiles = new DialogFilterPiece("Tar GZip archives", "*.tar.gz");
        public static readonly DialogFilterPiece DynamicLibraries = new DialogFilterPiece("Dynamic libraries", "*.dll");
        public static readonly DialogFilterPiece Archives = new DialogFilterPiece("Archives",
                "*.zip", "*.rar", "*.7z", "*.gzip", "*.tar", "*.tar.gz", "*.bz2");

        public DialogFilterPiece([NotNull] string displayName, [Localizable(false), NotNull] params string[] filter) {
            Filter = string.Join(";", filter);
            ShortName = displayName;
            DisplayName = $"{ShortName} ({Filter})";
            BaseExtension = filter.Length > 0 && filter[0].StartsWith("*.") ? filter[0].Substring(1) : null;
        }

        [NotNull]
        public string Filter { get; }

        [NotNull]
        public string ShortName { get; }

        [CanBeNull]
        public string BaseExtension { get; }

        [NotNull]
        public string WinFilter => $"{DisplayName}|{Filter}";
    }

    public abstract class DialogParamsBase {
        [CanBeNull]
        public string Title { get; set; }

        [CanBeNull]
        public string InitialDirectory { get; set; }

        [Localizable(false), CanBeNull]
        public string DirectorySaveKey { get; set; }

        [CanBeNull]
        internal string ActualSaveKey => DirectorySaveKey == null ? null : "dialogs:" + DirectorySaveKey;
    }

    public abstract class FileDialogParamsBase : DialogParamsBase {
        [NotNull]
        public List<FileDialogCustomPlace> CustomPlaces { get; set; } = new List<FileDialogCustomPlace>();

        [NotNull]
        public List<DialogFilterPiece> Filters { get; set; } = new List<DialogFilterPiece>();

        [CanBeNull]
        public string DetaultExtension { get; set; }

        [CanBeNull]
        public string DefaultFileName { get; set; }

        public bool IncludeAllFiles { get; set; } = true;
        public bool DereferenceLinks { get; set; } = true;
        public bool ValidateNames { get; set; } = true;
        public bool RestoreDirectory { get; set; } = false;

        internal IEnumerable<DialogFilterPiece> GetFilters() {
            return !IncludeAllFiles ? Filters : Filters.Concat(new[] { DialogFilterPiece.AllFiles });
        }
    }

    public class OpenDialogParams : FileDialogParamsBase {
        public bool CheckFileExists { get; set; } = true;
        public bool UseCachedIfAny { get; set; } = false;
    }

    public class SaveDialogParams : FileDialogParamsBase {
        public bool AddExtension { get; set; } = true;
        public bool CheckPathExists { get; set; } = true;
        public bool OverwritePrompt { get; set; } = true;
        public bool CreatePrompt { get; set; } = false;
    }

    public static class FileRelatedDialogs {
        [CanBeNull]
        private static Window GetOwner() {
            return (Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.Windows
                    .FirstOrDefault(x => x.IsActive) ?? (Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        }

        /// <summary>
        /// Avalonia’s storage provider is async only, while the rest of the app expects blocking dialogs.
        /// </summary>
        private static T RunBlocking<T>(Func<Task<T>> fn) {
            if (!Dispatcher.UIThread.CheckAccess()) {
                return Dispatcher.UIThread.InvokeAsync(fn).GetAwaiter().GetResult();
            }

            var task = fn();
            FirstFloor.ModernUI.Helpers.NestedLoop.RunUntilCompleted(task);

            return task.GetAwaiter().GetResult();
        }

        private static List<FilePickerFileType> ToFileTypes(IEnumerable<DialogFilterPiece> filters) {
            return filters.Select(x => new FilePickerFileType(x.DisplayName) {
                Patterns = x.Filter.Split(';')
            }).ToList();
        }

        private static async Task<IStorageFolder> GetStartFolder(IStorageProvider provider, string directory) {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return null;
            return await provider.TryGetFolderFromPathAsync(directory);
        }

        [CanBeNull]
        private static string[] OpenInner([NotNull] OpenDialogParams p, string currentFilename, bool multiselect) {
            var key = p.ActualSaveKey;
            var owner = GetOwner();
            var provider = owner?.StorageProvider;
            if (provider == null) return null;

            var initial = currentFilename != null ? Path.GetDirectoryName(currentFilename)
                    : key == null ? p.InitialDirectory : ValuesStorage.Get(key, p.InitialDirectory);
            var result = RunBlocking(async () => await provider.OpenFilePickerAsync(new FilePickerOpenOptions {
                Title = p.Title,
                AllowMultiple = multiselect,
                FileTypeFilter = ToFileTypes(p.GetFilters()),
                SuggestedStartLocation = await GetStartFolder(provider, initial)
            }));

            var files = result.Select(x => x.TryGetLocalPath()).Where(x => x != null).ToArray();
            if (files.Length == 0) return null;
            if (key != null) {
                ValuesStorage.Set(key, Path.GetDirectoryName(files[0]));
            }

            return files;
        }

        /// <summary>
        /// Replacement of WinForms’ FolderBrowserDialog.
        /// </summary>
        [CanBeNull]
        public static string PickFolder([CanBeNull] string title = null, [CanBeNull] string initialDirectory = null) {
            try {
                var provider = GetOwner()?.StorageProvider;
                if (provider == null) return null;

                var result = RunBlocking(async () => await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
                    Title = title,
                    AllowMultiple = false,
                    SuggestedStartLocation = await GetStartFolder(provider, initialDirectory)
                }));
                return result.FirstOrDefault()?.TryGetLocalPath();
            } catch (Exception e) {
                NonfatalError.NotifyBackground("Can’t use Folder Dialog properly", e);
                return null;
            }
        }

        [CanBeNull]
        public static string Open([NotNull] OpenDialogParams p, string currentFilename = null) {
            var cached = p.ActualSaveKey + @":cached";

            try {
                if (p.UseCachedIfAny && ValuesStorage.Contains(cached) && File.Exists(ValuesStorage.Get<string>(cached))) {
                    return ValuesStorage.Get<string>(cached);
                }
            } catch (Exception e) {
                Logging.Warning(e);
            }

            try {
                var result = OpenInner(p, currentFilename, false)?.FirstOrDefault();
                if (result != null && p.UseCachedIfAny) {
                    ValuesStorage.Set(cached, result);
                }
                return result;
            } catch (Exception e) {
                NonfatalError.NotifyBackground("Can’t use Open File Dialog properly", e);
                return null;
            }
        }

        [CanBeNull]
        public static string[] OpenMultiple([NotNull] OpenDialogParams p, string currentFilename = null) {
            var cached = p.ActualSaveKey + @":cached";

            try {
                if (p.UseCachedIfAny && ValuesStorage.Contains(cached) && ValuesStorage.GetStringList(cached).All(File.Exists)) {
                    return ValuesStorage.GetStringList(cached).ToArray();
                }
            } catch (Exception e) {
                Logging.Warning(e);
            }

            try {
                var result = OpenInner(p, currentFilename, true);
                if (result != null && p.UseCachedIfAny) {
                    ValuesStorage.Set(cached, result);
                }
                return result?.Distinct().ToArray();
            } catch (Exception e) {
                NonfatalError.NotifyBackground("Can’t use Open File Dialog properly", e);
                return null;
            }
        }

        [CanBeNull]
        public static string Save([NotNull] SaveDialogParams p, string currentFilename = null) {
            var key = p.ActualSaveKey;
            try {
                var provider = GetOwner()?.StorageProvider;
                if (provider == null) return null;

                var filters = p.GetFilters().ToList();
                var extension = (p.DetaultExtension ?? filters[0].BaseExtension)?.TrimStart('.');
                var initial = currentFilename != null ? Path.GetDirectoryName(currentFilename)
                        : key == null ? p.InitialDirectory : ValuesStorage.Get(key, p.InitialDirectory);
                var name = currentFilename != null ? Path.GetFileNameWithoutExtension(currentFilename) : p.DefaultFileName;

                var file = RunBlocking(async () => await provider.SaveFilePickerAsync(new FilePickerSaveOptions {
                    Title = p.Title,
                    SuggestedFileName = name,
                    DefaultExtension = extension,
                    ShowOverwritePrompt = p.OverwritePrompt,
                    FileTypeChoices = ToFileTypes(filters),
                    SuggestedStartLocation = await GetStartFolder(provider, initial)
                }));

                var result = file?.TryGetLocalPath();
                if (result == null) return null;

                if (p.AddExtension && extension != null && !Path.HasExtension(result)) {
                    result += "." + extension;
                }

                if (key != null) {
                    ValuesStorage.Set(key, Path.GetDirectoryName(result));
                }

                return result;
            } catch (Exception e) {
                NonfatalError.NotifyBackground("Can’t use Save File Dialog properly", e);
                return null;
            }
        }
    }
}
