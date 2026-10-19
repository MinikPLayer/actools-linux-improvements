using System.IO;
using AcManager.Tools.Data;

namespace AcManager.Pages.ShadersPatch {
    public partial class ShadersCredits : global::Avalonia.Controls.UserControl {
        public ShadersCredits() {
            InitializeComponent();
            TextBlock.Text = PatchVersionInfo.ChangelogPrepare(File.ReadAllText(PatchHelper.TryGetConfigFilename("data_credits.txt") ?? string.Empty));
        }
    }
}