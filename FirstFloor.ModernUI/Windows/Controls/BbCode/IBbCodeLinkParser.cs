using System;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows.Controls.BbCode {
    public interface IBbCodeLinkParser {
        bool TryParseUriWithParameters(string value, bool detectParametersInWebUrls, out Uri uri, out string parameter, out string targetName, out string toolTip);

        [NotNull]
        string Urlify([NotNull] string value);
    }
}
