using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FirstFloor.ModernUI.Dialogs;

namespace Microsoft.Win32 {
    /// <summary>Base of WPF-style blocking file dialogs, implemented on top of <see cref="FileRelatedDialogs"/>.</summary>
    public abstract class FileDialog {
        public string Filter { get; set; }
        public string Title { get; set; }
        public string InitialDirectory { get; set; }
        public string FileName { get; set; }
        public string DefaultExt { get; set; }
        public bool RestoreDirectory { get; set; }
        public bool CheckFileExists { get; set; }
        public bool CheckPathExists { get; set; }
        public bool ValidateNames { get; set; }
        public bool AddExtension { get; set; }
        public bool OverwritePrompt { get; set; }
        public bool Multiselect { get; set; }
        public string[] FileNames { get; protected set; } = new string[0];
        public IList<FileDialogCustomPlace> CustomPlaces { get; } = new List<FileDialogCustomPlace>();

        protected List<DialogFilterPiece> ParseFilters() {
            var result = new List<DialogFilterPiece>();
            var pieces = (Filter ?? "").Split('|');
            for (var i = 0; i + 1 < pieces.Length; i += 2) {
                var patterns = pieces[i + 1].Split(';').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
                result.Add(new DialogFilterPiece(pieces[i].Split('(')[0].Trim(), patterns));
            }
            return result;
        }

        public abstract bool? ShowDialog();
        public bool? ShowDialog(object owner) => ShowDialog();
    }

    public class OpenFileDialog : FileDialog {
        public OpenFileDialog() { CheckFileExists = true; }

        public override bool? ShowDialog() {
            var p = new OpenDialogParams { Title = Title, InitialDirectory = InitialDirectory, DefaultFileName = FileName, IncludeAllFiles = false, Filters = ParseFilters() };
            var files = Multiselect ? FileRelatedDialogs.OpenMultiple(p) : FileRelatedDialogs.Open(p) is string s ? new[] { s } : null;
            if (files == null || files.Length == 0) return false;
            FileNames = files;
            FileName = files[0];
            return true;
        }
    }

    public class SaveFileDialog : FileDialog {
        public SaveFileDialog() { OverwritePrompt = true; }

        public override bool? ShowDialog() {
            var p = new SaveDialogParams { Title = Title, InitialDirectory = InitialDirectory, DefaultFileName = FileName, DetaultExtension = DefaultExt, IncludeAllFiles = false, Filters = ParseFilters() };
            var file = FileRelatedDialogs.Save(p);
            if (file == null) return false;
            FileNames = new[] { file };
            FileName = file;
            return true;
        }
    }
}

namespace AcTools.Windows.Compat {

    /// <summary>WinForms’ FolderBrowserDialog replacement.</summary>
    public class FolderBrowserDialog {
        public string Description { get; set; }
        public string SelectedPath { get; set; }
        public bool ShowNewFolderButton { get; set; }
        public bool UseDescriptionForTitle { get; set; }

        public DialogResult ShowDialog() {
            var result = FileRelatedDialogs.PickFolder(Description, SelectedPath);
            if (result == null) return DialogResult.Cancel;
            SelectedPath = result;
            return DialogResult.OK;
        }
    }
}
