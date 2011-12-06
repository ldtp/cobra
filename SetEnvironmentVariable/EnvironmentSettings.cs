// Code from http://ghouston.blogspot.com/2005/08/how-to-create-and-change-environment.html
// Required after installation to make the environment variable visible,
// without rebooting the system

using System;
using System.Runtime.InteropServices;

namespace SetEnvironmentVariable
{
    class EnvironmentSettings
    {
        private static void SetVariable()
        {
            int result;
            SendMessageTimeout((System.IntPtr)HWND_BROADCAST,
                WM_SETTINGCHANGE, 0, "Environment", SMTO_BLOCK | SMTO_ABORTIFHUNG |
                SMTO_NOTIMEOUTIFNOTHUNG, 5000, out result);
        }

        [DllImport("user32.dll",
             CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool
            SendMessageTimeout(
            IntPtr hWnd,
            int Msg,
            int wParam,
            string lParam,
            int fuFlags,
            int uTimeout,
            out int lpdwResult
            );

        public const int HWND_BROADCAST = 0xffff;
        public const int WM_SETTINGCHANGE = 0x001A;
        public const int SMTO_NORMAL = 0x0000;
        public const int SMTO_BLOCK = 0x0001;
        public const int SMTO_ABORTIFHUNG = 0x0002;
        public const int SMTO_NOTIMEOUTIFNOTHUNG = 0x0008;

        static void Main(string[] args)
        {
            EnvironmentSettings.SetVariable();
        }
    }
}
