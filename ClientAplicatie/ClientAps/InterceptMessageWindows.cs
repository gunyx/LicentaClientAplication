using System;
using System.Runtime.InteropServices;


namespace ClientAps
{
    /*
     * Clasa pentru folosirea metodelor externe din user32.dll pentru hook-uri la sistem (keyboard+time counter)
     */

    class InterceptMessageWindows
    { 
        [Serializable]
        public struct MSG
        {
            public IntPtr hwnd;

            public IntPtr lParam;

            public int message;

            public int pt_x;

            public int pt_y;

            public int time;

            public IntPtr wParam;

        }


        [DllImport("user32.dll")]
        public static extern bool KillTimer(IntPtr hWnd, UIntPtr uIDEvent);

        [DllImport("user32.dll")]
        public static extern UIntPtr SetTimer(IntPtr hWn, UIntPtr nIFEvent, uint uElapse);

        [DllImport("user32.dll")]
        public static extern void TIMERPROC(IntPtr unnamedParam1, uint unnamedParam2, UIntPtr unnamedParam3, ulong unnamedParam4);

        [DllImport("user32.dll")]
        public static extern bool PostThreadMessageA(ulong idThread, uint Msg, UIntPtr wParam, UIntPtr lParam);

        [DllImport("user32.dll")]
        public static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll")]
        public static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage([In] ref MSG lpmsg);
    }
}
