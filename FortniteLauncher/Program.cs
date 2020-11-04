using FortniteLauncher.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace FortniteLauncher
{
    class Program
    {
        private const string FORTNITE_EXECUTABLE = "FortniteClient-Win64-Shipping.exe";

        private static Process _fnProcess;
        private static Process FortniteEACProcess;
        private static bool InjectDLL;

        static void Main(string[] args)
        {
            string joinedArgs = string.Join(" ", args);

            // Check if the Fortnite client exists in the current work path.
            if (!File.Exists(FORTNITE_EXECUTABLE))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\"{FORTNITE_EXECUTABLE}\" is missing!");
                Console.ReadKey();
                Environment.Exit(1);
            }

            // Check if -NOSSLPINNING exists in args (regardless of case) to disable SSL pinning
            if (joinedArgs.ToUpper().Contains("-NOSSLPINNING"))
            {
                joinedArgs = Regex.Replace(joinedArgs, "-NOSSLPINNING", string.Empty, RegexOptions.IgnoreCase);
                InjectDLL = true;
                string PlataniumPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Platanium.dll");
                if (!File.Exists(PlataniumPath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\"Platanium.dll\" is missing!");
                    Console.ReadKey();
                    Environment.Exit(1);
                }
            }

            // Setup a process exit event handler
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);

            // Fortnite EAC
            FortniteEACProcess = new Process
            {
                StartInfo =
                {
                    FileName = "FortniteClient-Win64-Shipping_EAC.exe",
                    Arguments = $"{joinedArgs} -noeac -fromfl=be -fltoken=f7b9gah4h5380d10f721dd6a"

                }
            };
            FortniteEACProcess.Start();
            FortniteEACProcess.Suspend();

            // Initialize Fortnite process with start info
            _fnProcess = new Process
            {
                StartInfo =
                {
                    FileName = FORTNITE_EXECUTABLE,
                    Arguments = $"{joinedArgs} -noeac -fromfl=be -fltoken=f7b9gah4h5380d10f721dd6a"
                }
            };

            _fnProcess.Start(); // Start Fortnite client process

            // Check if -NOSSLPINNING exists in args (regardless of case) to disable SSL pinning
            if (InjectDLL)
            {
                string PlataniumPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Platanium.dll");
                IntPtr hProcess = Win32.OpenProcess(1082, false, _fnProcess.Id);
                IntPtr procAddress = Win32.GetProcAddress(Win32.GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                uint num = (uint)((PlataniumPath.Length + 1) * Marshal.SizeOf(typeof(char)));
                IntPtr intPtr = Win32.VirtualAllocEx(hProcess, IntPtr.Zero, num, 12288U, 4U);
                UIntPtr uintPtr;
                Win32.WriteProcessMemory(hProcess, intPtr, Encoding.Default.GetBytes(PlataniumPath), num, out uintPtr);
                Win32.CreateRemoteThread(hProcess, IntPtr.Zero, 0U, procAddress, intPtr, 0U, IntPtr.Zero);
                _fnProcess.WaitForExit(); // We'll wait for the Fortnite process to exit, otherwise our launcher will just close instantly
            }
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            if (!FortniteEACProcess.HasExited)
            {
                FortniteEACProcess.Resume();
                FortniteEACProcess.Kill();
            }
            if (!_fnProcess.HasExited)
            {
                _fnProcess.Kill();
            }
        }
    }
}
