using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

// Copyright Keith Garner (KeithGa@KeithGa.com) all rights reserved.
// Microsoft Reciprocal License (Ms-RL) 
// http://www.microsoft.com/en-us/openness/licenses.aspx

namespace PowerShell_Wizard_Host
{

    using System.Threading;
    using System.Windows.Forms;
    using System.Management.Automation;
    using System.Management.Automation.Host;
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Diagnostics;

    #region PSHost

    /// <summary>
    /// This is a sample implementation of the PSHost abstract class for 
    /// console applications. Not all members are implemented. Those that 
    /// are not implemented throw a NotImplementedException exception or 
    /// return nothing.
    /// </summary>
    internal class MyHost : PSHost
    {

        PSHostControl Parent = null;
        private MyHostUserInterface myHostUserInterface = null;
        private Guid myId = Guid.NewGuid();

        public MyHost(PSHostControl _Parent)
        {
            Parent = _Parent;
            myHostUserInterface = new MyHostUserInterface(Parent);
        }

        ///////////////////////////////////////////////////////////////////

        public override System.Globalization.CultureInfo CurrentCulture
        {
            get { return System.Threading.Thread.CurrentThread.CurrentCulture; }
        }

        public override System.Globalization.CultureInfo CurrentUICulture
        {
            get { return System.Threading.Thread.CurrentThread.CurrentUICulture; }
        }

        public override Guid InstanceId
        {
            get { return this.myId; }
        }

        public override string Name
        {
            get { return "PowerShellWizardHost"; }
        }

        public override PSHostUserInterface UI
        {
            get { return this.myHostUserInterface; }
        }

        public override Version Version
        {
            get { return new Version(1, 0, 1, 0); }
        }

        public override void EnterNestedPrompt()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public override void ExitNestedPrompt()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public override void NotifyBeginApplication()
        {
            return;
        }

        public override void NotifyEndApplication()
        {
            return;
        }

        /// <summary>
        /// Indicate to the host application that exit has
        /// been requested. Pass the exit code that the host
        /// application should use when exiting the process.
        /// </summary>
        /// <param name="exitCode">The exit code that the host application should use.</param>
        public override void SetShouldExit(int exitCode)
        {
            Trace.WriteLine(string.Format("PowerShell script has finsished with ExitCode: {0}", exitCode), "PSHost");
            this.Parent.ExitCode = exitCode;
        }
    }

    #endregion

    #region HostInterface

    /// <summary>
    /// An implementation of the PSHostUserInterface abstract class for Wizard Host
    /// Most functions are callbacks to other methods.
    /// </summary>
    internal class MyHostUserInterface : PSHostUserInterface
    {

        PSHostControl Parent = null;
        private MyRawUserInterface myRawUi = null;

        public MyHostUserInterface(PSHostControl _Parent)
        {
            Parent = _Parent;
            myRawUi = new MyRawUserInterface(Parent);
        }

        public void DebugWriteLine(string message)
        {
            Trace.WriteLine(message, "PSHostUserInterface");
            Debug.WriteLine(message, "PSHostUserInterface");
        }

        public void DebugWriteLine(string format, params object[] args)
        {
            this.DebugWriteLine(string.Format(format, args));
        }

        public void WriteExCommon(System.Drawing.Color foregroundColor, System.Drawing.Color backgroundColor, string category, string value)
        {
            if (foregroundColor == System.Drawing.Color.Black && backgroundColor == System.Drawing.Color.Black)
            {
                // Special override for "CLS" scenario.
                this.WriteExCommon(Parent.ForeColor, Parent.BackColor, category, value);
            }
            else if (value.Length > 0)
            {
                SynchronizedInvoke.InvokeIfRequired(this.Parent, () => Parent.Write(foregroundColor, backgroundColor, value));
            }
        }

        public void WriteEx(System.Drawing.Color foregroundColor, System.Drawing.Color backgroundColor, string category, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                Trace.Write(value);
                Debug.Write(value);
                this.WriteExCommon(foregroundColor, backgroundColor, category, value);
            }
        }

        public void WriteLineEx(System.Drawing.Color foregroundColor, System.Drawing.Color backgroundColor, string category, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                Trace.WriteLine(value, "PSHostUserInterface." + category);
                Debug.WriteLine(value, "PSHostUserInterface." + category);
            }
            this.WriteExCommon(foregroundColor, backgroundColor, category, value + "\r\n");
        }

        /// <summary>
        /// Gets an instance of the PSRawUserInterface class for this host
        /// application.
        /// </summary>
        public override PSHostRawUserInterface RawUI
        {
            get { return this.myRawUi; }
        }

        /// <summary>
        /// Prompts the user for input.
        /// </summary>
        /// <param name="caption">The caption or title of the prompt.</param>
        /// <param name="message">The text of the prompt.</param>
        /// <param name="descriptions">A collection of FieldDescription objects that 
        /// describe each field of the prompt.</param>
        public override Dictionary<string, PSObject> Prompt(string caption, string message, System.Collections.ObjectModel.Collection<FieldDescription> descriptions)
        {
            DebugWriteLine("Prompt(caption:{0},message:{1},Description count:{2})", caption, message,descriptions.Count);
            SynchronizedInvoke.InvokeIfRequired(this.Parent, () => this.Parent.Prompt(caption, message, descriptions));
            Dictionary<string, PSObject> result = null;
            if (this.Parent.WaitForResult(out result))
            {
                DebugWriteLine("Prompt() Complete Count: {0}", result.Count);
                return result;
            }
            DebugWriteLine("Prompt() Empty");
            return null;
        }

        /// <summary>
        /// Provides a set of choices that enable the user to choose a single option from a set of options.
        /// </summary>
        /// <param name="caption">Text that proceeds (a title) the choices.</param>
        /// <param name="message">A message that describes the choice.</param>
        /// <param name="choices">A collection of ChoiceDescription objects that describes each choice.</param>
        /// <param name="defaultChoice">The index of the label in the Choices parameter collection. To indicate no default choice, set to -1.</param>
        public override int PromptForChoice(string caption, string message, System.Collections.ObjectModel.Collection<ChoiceDescription> choices, int defaultChoice)
        {
            DebugWriteLine("PromptForChoice(caption:{0},message:{1})", caption, message);
            SynchronizedInvoke.InvokeIfRequired(this.Parent, () => this.Parent.PromptForChoice(caption, message, choices, defaultChoice));
            PSObject result = null;
            if (this.Parent.WaitForResult(out result))
            {
                DebugWriteLine("PromptForChoice() Complete");
                return (int)result.BaseObject;
            }
            DebugWriteLine("PromptForChoice() Empty");
            return -1;
        }

        /// <summary>
        /// Prompts the user for credentials. 
        /// </summary>
        /// <param name="caption">The caption for the message window.</param>
        /// <param name="message">The text of the message.</param>
        /// <param name="userName">The user name whose credential is to be prompted for.</param>
        /// <param name="targetName">The name of the target for which the credential is collected.</param>
        /// <param name="allowedCredentialTypes">A PSCredentialTypes constant that 
        /// identifies the type of credentials that can be returned.</param>
        /// <param name="options">A PSCredentialUIOptions constant that identifies the UI 
        /// behavior when it gathers the credentials.</param>
        /// <returns>PSCredential</returns>
        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            DebugWriteLine("PromptForCredentialEx(caption:{0},message:{1})", caption, message);
            string Name = "Credential";
            FieldDescription DefaultCred = new FieldDescription(Name);
            if (!string.IsNullOrEmpty(userName))
            {
                DefaultCred.DefaultValue = userName;
            }
            DefaultCred.SetParameterType(typeof(PSCredential));

            var Prompts = new System.Collections.ObjectModel.Collection<FieldDescription>() { DefaultCred };
            var results = this.Prompt(caption, message, Prompts);

            if (results != null)
            {
                if (results.ContainsKey(Name))
                {
                    DebugWriteLine("PromptForCredential() Complete");
                    return (PSCredential)(results[Name].BaseObject);
                }
            }
            DebugWriteLine("PromptForCredential() Empty");
            return null;
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            DebugWriteLine("PromptForCredential(caption:{0},message:{1},userName:{2},TargetName:{3})", caption, message, userName, targetName);
            return this.PromptForCredential(caption, message, userName, targetName, PSCredentialTypes.Default, PSCredentialUIOptions.Default);
        }


        public override string ReadLine()
        {
            DebugWriteLine("ReadLine()");
            SynchronizedInvoke.InvokeIfRequired(this.Parent, () => this.Parent.ReadLine('\0'));
            PSObject result = null;
            if (this.Parent.WaitForResult(out result))
            {
                DebugWriteLine("ReadLine() Complete");
                return (string)result.BaseObject;
            }
            DebugWriteLine("ReadLine() Empty");
            return "";
        }

        public override System.Security.SecureString ReadLineAsSecureString()
        {
            DebugWriteLine("ReadLineAsSecureString() Start");
            SynchronizedInvoke.InvokeIfRequired(this.Parent, () => this.Parent.ReadLine('*'));
            PSObject result = null;
            if (this.Parent.WaitForResult(out result))
            {
                DebugWriteLine("ReadLineAsSecureString() Complete");
                return (System.Security.SecureString)result.BaseObject;
            }
            DebugWriteLine("ReadLineAsSecureString() Empty");
            return new System.Security.SecureString();
        }

        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            // WAY to verbose
            // DebugWriteLine("WriteProgress(Source:{0},Activity:{1},ParentActivity:{2})",sourceId,record.ActivityId,record.ParentActivityId);
            SynchronizedInvoke.InvokeIfRequired(this.Parent, () => Parent.WriteProgress(sourceId, record));
        }

        #region Write Methods
        public override void Write(string value)
        {
            this.WriteEx(Parent.ForeColor, Parent.BackColor, "StdOut", value);
        }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            this.WriteEx(System.Drawing.Color.FromName(foregroundColor.ToString()), System.Drawing.Color.FromName(backgroundColor.ToString()), "StdOut", value);
        }

        public override void WriteDebugLine(string message)
        {
            this.WriteLineEx(System.Drawing.Color.DarkGray, System.Drawing.Color.Transparent, "DEBUG", message);
        }

        public override void WriteErrorLine(string value)
        {
            this.WriteLineEx(System.Drawing.Color.Red, System.Drawing.Color.Transparent, "ERROR", value);
        }

        public override void WriteLine()
        {
            this.WriteLine("");
        }

        public override void WriteLine(string value)
        {
            this.WriteLineEx(Parent.ForeColor, Parent.BackColor, "StdOut", value);
        }

        public override void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            this.WriteLineEx(System.Drawing.Color.FromName(foregroundColor.ToString()), System.Drawing.Color.FromName(backgroundColor.ToString()), "StdOut", value);
        }

        public override void WriteVerboseLine(string message)
        {
            this.WriteLineEx(System.Drawing.Color.Lime, System.Drawing.Color.Transparent, "VERBOSE", message);
        }

        public override void WriteWarningLine(string message)
        {
            this.WriteLineEx(System.Drawing.Color.Orange, System.Drawing.Color.Transparent, "WARNING", message);
        }
        #endregion
    }


    #endregion

    #region RawUI

    internal class MyRawUserInterface : PSHostRawUserInterface
    {
        PSHostControl Parent = null;

        public MyRawUserInterface(PSHostControl _Parent)
        {
            Parent = _Parent;
        }

        /// <summary>
        /// Required only for "out-string"
        /// </summary>
        public override Size BufferSize
        {
            get { return new Size(120, 50); }
            set { throw new NotImplementedException("The method or operation is not implemented."); }
        }

        public override ConsoleColor BackgroundColor
        { get; set; }

        public override Coordinates CursorPosition
        { get; set; }

        public override int CursorSize
        { get; set; }

        public override ConsoleColor ForegroundColor
        { get; set; }

        public override bool KeyAvailable
        { get { return false; } }

        public override Size MaxPhysicalWindowSize
        { get { return MaxPhysicalWindowSize; } }

        public override Size MaxWindowSize
        { get { return MaxPhysicalWindowSize; } }

        public override Coordinates WindowPosition
        { get; set; }

        public override Size WindowSize
        { get; set; }

        /// <summary>
        /// Gets or sets the title of the window mapped to the Console.Title property.
        /// </summary>
        public override string WindowTitle
        {
            get { return Console.Title; }
            set {
                Trace.WriteLine(string.Format("WindowTitle({0})", value), "PSHostRawUserInterface");
                SynchronizedInvoke.InvokeIfRequired(this.Parent, () => Parent.DoChangeWindowTitle(value));
            }
        }

        public override void FlushInputBuffer()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public override BufferCell[,] GetBufferContents(Rectangle rectangle)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        /// <summary>
        /// This API Reads a pressed, released, or pressed and released keystroke 
        /// from the keyboard device, blocking processing until a keystroke is 
        /// typed that matches the specified keystroke options.
        /// </summary>
        /// <param name="options">Options, such as IncludeKeyDown,  used when reading the keyboard.</param>
        public override KeyInfo ReadKey(ReadKeyOptions options)
        {
            Trace.WriteLine(string.Format("ReadKey({0})", options.ToString()), "PSHostRawUserInterface");
            SynchronizedInvoke.InvokeIfRequired(this.Parent, () => this.Parent.ReadKey(options));
            PSObject result = null;
            if (this.Parent.WaitForResult(out result))
            {
                return (KeyInfo)result.BaseObject;
            }
            return new KeyInfo();
        }

        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        /// <summary>
        /// Necessary only for "Clear-Host"
        /// </summary>
        public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
        {
            if (fill.BufferCellType == BufferCellType.Complete && fill.Character == ' ' && rectangle == (new Rectangle(-1, -1, -1, -1)))
            {
                Trace.WriteLine("SetBufferContents-->ClearScreen", "PSHostRawUserInterface");
                SynchronizedInvoke.InvokeIfRequired(this.Parent, () => this.Parent.ClearScreen());
            }
            else
            {
                throw new NotImplementedException("The method or operation is not implemented.");
            }
        }
    }


    #endregion


    #region Support Classes

    internal static class SynchronizedInvoke
    {
        /// <summary>
        /// Support calls across threads
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="action"></param>
        public static void InvokeIfRequired(this ISynchronizeInvoke obj, MethodInvoker action)
        {
            if (obj.InvokeRequired)
            {
                var args = new object[0];
                obj.Invoke(action, args);
            }
            else
            {
                action();
            }
        }

    }

    #endregion

}
