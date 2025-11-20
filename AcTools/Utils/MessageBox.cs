using System.Diagnostics;

namespace AcTools.Utils;

public static class MessageBox
{
    public enum Type
    {
        Info,
        Warning,
        Error
    }

    public static void Show(string message, string title = "Message", Type type = Type.Info)
    {
        var args = new List<string>();
        switch (type)
        {
            case Type.Info:
                args.Add("--info");
                break;

            case Type.Warning:
                args.Add("--warning");
                break;

            case Type.Error:
                args.Add("--error");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        args.Add("--text");
        args.Add(message);

        args.Add("--title");
        args.Add(title);

        Process.Start(new ProcessStartInfo("zenity", args))?.WaitForExit();
    }
}