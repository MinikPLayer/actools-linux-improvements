using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AcTools
{
    public static class WineKernel32
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
    }

    public static class UnixUtils
    {
        private static bool GetIsUnixWine()
        {
            var moduleHandle = WineKernel32.GetModuleHandle("ntdll.dll");
            if (moduleHandle == null || moduleHandle == IntPtr.Zero)
            {
                AcToolsLogging.Write("Cannot resolve ntdll.dll!");
                return false;
            }

            var wineGetVersionProc = WineKernel32.GetProcAddress(moduleHandle, "wine_get_version");
            if (wineGetVersionProc == null || wineGetVersionProc == IntPtr.Zero)
            {
                AcToolsLogging.Write("Wine Unix not detected!");
                return false;
            }

            AcToolsLogging.Write("Wine Unix detected!");
            return true;
        }

        public static bool? isUnixWine = null;

        public static bool IsUnixWine => isUnixWine ??= GetIsUnixWine();
    }
}
