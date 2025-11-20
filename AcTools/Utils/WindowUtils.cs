using System.Diagnostics;

namespace AcTools.Utils;
using HWND = IntPtr;

public static class WindowUtils
{
    public static void BringProcessWindowToFront(Process process)
    {
        throw new NotImplementedException();
    }

    public static HWND GetForegroundWindow()
    {
        throw new NotImplementedException();
    }

    public static void SetForegroundWindow(HWND hwnd)
    {
        throw new NotImplementedException();
    }

    public static HWND FindWindow(string className, string windowName)
    {
        throw new NotImplementedException();
    }

    public static HWND FindWindowEx(HWND parent, HWND childAfter, string className, string windowName)
    {
        throw new NotImplementedException();
    }
}