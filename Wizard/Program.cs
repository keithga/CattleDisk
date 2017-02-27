using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace PowerShell_Wizard_Host
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string LogPath = System.IO.Path.GetTempPath() + System.IO.Path.GetRandomFileName() + ".Log";
            Trace.Listeners.Add(new TextWriterTraceListener(LogPath, "myListener"));
            Trace.AutoFlush = true;
            Trace.WriteLine("Logging started with file: " + LogPath);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PSWizHost());
            Trace.Flush();
        }
    }
}
