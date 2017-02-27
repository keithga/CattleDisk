using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

// Copyright Keith Garner (KeithGa@KeithGa.com) all rights reserved.
// Microsoft Reciprocal License (Ms-RL) 
// http://www.microsoft.com/en-us/openness/licenses.aspx

namespace PowerShell_Wizard_Host
{
    using System.Management.Automation;
    using System.Reflection;
    using System.IO;
    using System.Diagnostics;

    public partial class PSWizHost : Form
    {

        public PSWizHost()
        {
            InitializeComponent();
        }

        private void PSWizHost_Load(object sender, EventArgs e)
        {

#if DEBUG
            foreach ( var Resources in Assembly.GetExecutingAssembly().GetManifestResourceNames())
                Trace.WriteLine(string.Format("Resource names {0}",Resources),"Form");
#endif

            Trace.WriteLine("Load Powershell Script from embedded resource and start execution.", "Form");
            Stream DataStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PowerShell_Wizard_Host.Cattle.ps1");
            PowershellHostControl1.Script = (new StreamReader(DataStream)).ReadToEnd();
            PowershellHostControl1.Start(Environment.GetCommandLineArgs());

        }

        private void ButtonN_Click(object sender, EventArgs e)
        {
            Trace.WriteLine("Button Click Next.", "Form");
            if (PowershellHostControl1.isPowerShellRunning)
            {
                Trace.WriteLine("Validate any child elements.", "Form");
                if (!this.ValidateChildren())
                {
                    Trace.WriteLine("Some input elements reported error.", "Form");
                    MessageBox.Show("Please fill out all missing elements.");
                    return;
                }
                Trace.WriteLine("All form elements ready for processing.", "Form");
                PowershellHostControl1.OnNavigationNextPrompt();
                ButtonN.Enabled = false;
            }
            else
            {
                this.Close();
            }
        }

        private void ButtonC_Click(object sender, EventArgs e)
        {
            Trace.WriteLine("Button Click Cancel.", "Form");
            this.Close();
        }

        private void PSWizHost_FormClosing(object sender, FormClosingEventArgs e)
        {
            Trace.WriteLine("Form Closing.", "Form");
            if (PowershellHostControl1.isPowerShellRunning)
            {
                Trace.WriteLine("Powershell still running, prompt the user for cancel confirmation.", "Form");
                e.Cancel = true;
                if (MessageBox.Show("Are you sure you want to quit?", this.Text, MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Trace.WriteLine("User selected to confirm close, force any in-progress powershell script to stop.", "Form");
                    PowershellHostControl1.Stop();
                }
            }
        }

        private void PowershellHostControl1_InvocationStateChanged(object sender, System.Management.Automation.PSInvocationStateChangedEventArgs e)
        {
            switch (e.InvocationStateInfo.State)
            {
                case PSInvocationState.Running:
                    break;

                case PSInvocationState.Failed:

                    Environment.ExitCode = 1;
                    Trace.WriteLine("InvocationStateChanged -> Failed", "Form");
                    ButtonN.Enabled = true;
                    ButtonN.Text = "Finished";
                    ButtonN.Focus();
                    break;

                case PSInvocationState.Completed:
                case PSInvocationState.Disconnected:
                case PSInvocationState.Stopping:
                case PSInvocationState.NotStarted:
                case PSInvocationState.Stopped:
                default:

                    Trace.WriteLine("InvocationStateChanged -> Finished Successfully!", "Form");

                    Environment.ExitCode = PowershellHostControl1.ExitCode;

                    // Special Case, if the user passed the -silent switch, then just close the form
                    foreach (var Arg in PowershellHostControl1.ParsedParameters)
                    {
                        if (Arg.Name.ToString().ToLower() == "verbose")
                            if ((bool)Arg.Value)
                            {
                                Trace.WriteLine("User passed in Verbose on the command line, do not auto-exit.", "Form");
                                ButtonN.Enabled = true;
                                ButtonN.Text = "Finished";
                                ButtonN.Focus();
                                return;

                            }
                    }

                    Trace.WriteLine("All done, close the form.", "Form");
                    this.Close();
                    break;
            }
        }

        private void PowershellHostControl1_NextButtonRequest(object sender, EventArgs e)
        {
            Trace.WriteLine("The powershell script is waiting for user input.", "Form");
            ButtonN.Enabled = true;
            ButtonN.Focus();
        }

        private void PowershellHostControl1_WindowTitleChanged(object sender, LinkClickedEventArgs e)
        {
            if( !StatusPrevious.Visible)
            {
                StatusPrevious.Text = StatusCurrent.Text;
                StatusPrevious.Visible = true;
            }
            else
            {
                StatusPrevious.Text += "\r\n" + StatusCurrent.Text;
            }
            StatusCurrent.Text = e.LinkText;
            LabelTitle.Text = e.LinkText;
        }

    }
}
