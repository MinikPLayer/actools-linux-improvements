using System.Threading.Tasks;

namespace AcManager.Controls.UserControls.Web {
    /// <summary>
    /// A JavaScript function passed from a web page to C# (CefSharp has the same interface, but there is no embedded browser in the Avalonia
    /// version, so the proxies of JS APIs only keep the shape of the original API).
    /// </summary>
    public interface IJavascriptCallback {
        Task<object> ExecuteAsync(params object[] args);
    }
}
