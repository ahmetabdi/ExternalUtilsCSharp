using ExternalUtilsCSharp;
using ExternalUtilsCSharp.MathObjects;
using ExternalUtilsCSharp.MemObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SWTORSample
{
    class Program
    {
        private static bool m_bWork;
        private static KeyUtils keyUtils;
        public static MemUtils MemUtils;

        static void Main(string[] args)
        {
            m_bWork = true;
            keyUtils = new KeyUtils();
            MemUtils = new ExternalUtilsCSharp.MemUtils();

            MemUtils.UseUnsafeReadWrite = true;
            Thread thread = new Thread(new ThreadStart(Loop));
            thread.IsBackground = true;
            thread.Start();

            Console.WriteLine("Press ESC to exit");

            while (!m_bWork) Thread.Sleep(250);

            Console.WriteLine("Waiting for thread to exit...");
            thread.Join();

            Console.WriteLine("Bye.");
        }

        private static void Loop()
        {
            ProcUtils proc;
            ProcessModule swtorModule = null;
            ProcessModule memoryManModule = null;

            while (!ProcUtils.ProcessIsRunning("swtor") && m_bWork) { Thread.Sleep(500); }
            if (!m_bWork)
                return;

            Process swtor = Process.GetProcessesByName("swtor").FirstOrDefault(p => string.IsNullOrWhiteSpace(p.MainWindowTitle));
            proc = new ProcUtils(swtor, WinAPI.ProcessAccessFlags.VirtualMemoryRead | WinAPI.ProcessAccessFlags.VirtualMemoryWrite | WinAPI.ProcessAccessFlags.VirtualMemoryOperation);
            MemUtils.Handle = proc.Handle;

            while (swtorModule == null) { swtorModule = proc.GetModuleByName("swtor.exe"); }
            while (memoryManModule == null) { memoryManModule = proc.GetModuleByName("MemoryMan.dll"); }

            int swrba = swtorModule.BaseAddress.ToInt32();
            int mma = memoryManModule.BaseAddress.ToInt32();
            int pb = MemUtils.Read<int>((IntPtr)(swrba) + 0x01412EA4);

            float prev_x = 0.0f;
            float prev_y = 0.0f;
            float prev_z = 0.0f;

            byte[] nop = { 0x90, 0x90, 0x90, 0x90, 0x90 };
            byte[] up_down_bytes = { 0xF3, 0x0F, 0x11, 0x46, 0x0C };


            while (proc.IsRunning && m_bWork)
            {
                Thread.Sleep((int)(1000f / 60f));

                //Don't do anything if game is not in foreground
                //if (WinAPI.GetForegroundWindow() != proc.Process.MainWindowHandle)
                //    continue;

                int xyz2 = MemUtils.Read<int>((IntPtr)(pb) + 0x18);
                IntPtr xyz = (IntPtr)(xyz2) + 0x14;

                IntPtr x_address = (IntPtr)(xyz);
                IntPtr y_address = (IntPtr)(xyz + 0x04);
                IntPtr z_address = (IntPtr)(xyz + 0x08);

                float x = MemUtils.Read<float>(x_address);
                float y = MemUtils.Read<float>(y_address);
                float z = MemUtils.Read<float>(z_address);

                #region Handling input
                keyUtils.Update();
                Console.WriteLine("X: " + x + " Y: " + y + " Z: " + z);
                //if (keyUtils.KeyIsDown(WinAPI.VirtualKeyShort.ESCAPE))
                //    m_bWork = false;
                if (keyUtils.KeyIsDown(WinAPI.VirtualKeyShort.NUMPAD1) && keyUtils.KeyIsDown(WinAPI.VirtualKeyShort.KEY_W)) // FORWARD
                    MemUtils.Write<float>((IntPtr)(z_address), prev_z -= 0.15f);
                if (keyUtils.KeyIsDown(WinAPI.VirtualKeyShort.NUMPAD1) && keyUtils.KeyIsDown(WinAPI.VirtualKeyShort.KEY_A)) // LEFT
                    MemUtils.Write<float>((IntPtr)(x_address), prev_x -= 0.15f);
                if (keyUtils.KeyIsDown(WinAPI.VirtualKeyShort.NUMPAD1) && keyUtils.KeyIsDown(WinAPI.VirtualKeyShort.KEY_S)) // BACK
                    MemUtils.Write<float>((IntPtr)(z_address), prev_z += 0.15f);
                if (keyUtils.KeyIsDown(WinAPI.VirtualKeyShort.NUMPAD1) && keyUtils.KeyIsDown(WinAPI.VirtualKeyShort.KEY_D)) // RIGHT
                    MemUtils.Write<float>((IntPtr)(x_address), prev_x += 0.15f);
                if (keyUtils.KeyIsDown(WinAPI.VirtualKeyShort.NUMPAD1) && keyUtils.KeyIsDown(WinAPI.VirtualKeyShort.SPACE)) // UP
                    MemUtils.Write<float>((IntPtr)(y_address), prev_y + 0.10f);
                if (keyUtils.KeyIsDown(WinAPI.VirtualKeyShort.NUMPAD1) && keyUtils.KeyIsDown(WinAPI.VirtualKeyShort.CONTROL)) // DOWN
                    MemUtils.Write<float>((IntPtr)(y_address), prev_y - 0.10f);

                #endregion
                prev_x = x;
                prev_y = y;
                prev_z = z;

            }


        }
    }
}
