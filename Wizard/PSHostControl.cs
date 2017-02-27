using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Copyright Keith Garner (KeithGa@KeithGa.com) all rights reserved.
// Microsoft Reciprocal License (Ms-RL) 
// http://www.microsoft.com/en-us/openness/licenses.aspx
    
namespace PowerShell_Wizard_Host
{

    using System.Threading;
    using System.Management.Automation;
    using System.Windows.Forms;
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Management.Automation.Host;
    using System.Management.Automation.Runspaces;
    using System.Text.RegularExpressions;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Collections;
    using System.Data;
    using System.Drawing;

    public class PSHostControl : System.Windows.Forms.TableLayoutPanel
    {

        #region Control Initialization

        /// <summary>
        /// Private properties
        /// </summary>
        private PowerShell powershell = null;
        private MyHost PSHost = null;
        private CommandParameterCollection CommandParams = null;
        private ErrorProvider ErrorProvider1 = new ErrorProvider();

        public PSHostControl()
            : base()
        {
            this.Dock = DockStyle.Top;
            this.AutoSize = true;
            this.ColumnCount = 1;
            this.RowCount = 1;

            if (!this.DesignMode)
            {

                // Create the Powershell controller...
                this.PSHost = new MyHost(this);
                powershell = PowerShell.Create();

                // Replace cmdlets with our own implementations...
                InitialSessionState iss = InitialSessionState.CreateDefault();
                iss.Commands.Remove("Out-GridView", typeof(System.Management.Automation.Runspaces.SessionStateCmdletEntry));
                iss.Commands.Add(new SessionStateCmdletEntry("Out-GridView", typeof(HostGridView), null));
                iss.Commands.Add(new SessionStateCmdletEntry("Select-MDTObject", typeof(SelectMDTObject), null));
                iss.Commands.Add(new SessionStateCmdletEntry("Edit-KeyValuePair", typeof(HostGridEdit), null));

                powershell.Runspace = RunspaceFactory.CreateRunspace(this.PSHost,iss);
                powershell.Runspace.ApartmentState = System.Threading.ApartmentState.STA;
                powershell.Runspace.Open();

                if (System.Management.Automation.Runspaces.Runspace.DefaultRunspace == null)
                    System.Management.Automation.Runspaces.Runspace.DefaultRunspace = powershell.Runspace;

                powershell.InvocationStateChanged += new EventHandler<PSInvocationStateChangedEventArgs>(Powershell_InvocationStateChanged);

                powershell.Streams.Error.DataAdded += new EventHandler<DataAddedEventArgs>(delegate(object sender, DataAddedEventArgs e)
                {
                    this.PSHost.UI.WriteErrorLine(((PSDataCollection<ErrorRecord>)sender)[e.Index].ToString());
                });

                PSHostCallBack.ParentControl = this;

            }
            else
            {
                this.AddControl(new GrowLabel("PowerShell script..."));

            }
        }

        public void Stop()
        {
            DebugWriteLine("Hard Stop!");
            this.onNavigationCancel(); // FLush anything waiting...
            powershell.Runspace.CloseAsync();
            powershell.BeginStop(null, null);
        }

        public void Start(string[] Args)
        {

            Trace.WriteLineIf(string.IsNullOrEmpty(this.Script), "Script is empty, nothing to do...");
#if DEBUG
            DebugWriteLine("################################################################");
            DebugWriteLine(this.Script);
            DebugWriteLine("################################################################");
#endif
            DebugWriteLine("Start the script " + string.Join(" ", Args));
            powershell.AddScript(this.Script);
            ParsePowerShellParmsFromArgs(Args);

            foreach (var Command in powershell.Commands.Commands)
            {
                CommandParams = Command.Parameters; // For use by the Form...
                break;
            }

            powershell.AddCommand("out-string");
            powershell.AddParameter("-stream");

            PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();
            output.DataAdded += new EventHandler<DataAddedEventArgs>(delegate(object sender, DataAddedEventArgs e)
            {
                this.PSHost.UI.WriteLine(output[e.Index].ToString());
            });

            IAsyncResult asyncResult = powershell.BeginInvoke<PSObject, PSObject>(null, output);
        }



        #endregion

        #region UI Processing...

        public void ClearScreen()
        {
            this.Controls.Clear();
        }

        /// <summary>
        /// Overloaded to support Read-Host with and without -SecureString
        /// </summary>
        /// <param name="PasswordChar"></param>
        /// <param name="MaxChars"></param>
        public void ReadLine(char PasswordChar)
        {
            TextBox ReadBox = new TextBox() { PasswordChar = PasswordChar };
            ReadBox.Validating += new CancelEventHandler(delegate(object sender, CancelEventArgs e)
            {
                DebugWriteLine("Validate the TextBox");
                if (PasswordChar == '\0')
                {
                    xResult = new PSObject(((TextBox)sender).Text);
                }
                else
                {
                    var secure = new System.Security.SecureString();
                    foreach (char c in ((TextBox)sender).Text)
                    {
                        secure.AppendChar(c);
                    }

                    xResult = new PSObject(secure);
                }

            });

            ReadBox.KeyPress += new KeyPressEventHandler(delegate(object sender, KeyPressEventArgs e)
            {
                if (e.KeyChar == 13)
                {
                    DebugWriteLine("User pressed ENTER!");
                    this.OnNavigationNext(ReadBox);
                }
            });

            this.AddControl(ReadBox);
            this.OnControlNavigation(ReadBox);
            ReadBox.Focus();

        }

        public void ReadKey(ReadKeyOptions options)
        {
            TextBox ReadBox = new TextBox() { BorderStyle = System.Windows.Forms.BorderStyle.None };
            ReadBox.KeyPress += new KeyPressEventHandler(delegate(object sender, KeyPressEventArgs e)
            {
                DebugWriteLine("User pressed the ANY KEY!");
                e.Handled = true;
                xResult = new PSObject(new KeyInfo(0, e.KeyChar, 0, false));
                this.OnNavigationNext(ReadBox);
            });

            this.AddControl(ReadBox);
            this.OnControlNavigation(ReadBox);
            ReadBox.Focus();
        }

        public void WriteProgress(long SourceId, ProgressRecord record)
        {
            foreach (var Item in this.Controls)
            {
                if (Item.GetType() == typeof(ProgressPanel))
                    if (((ProgressPanel)Item).IsEqual(record))
                    {
                        if (record.RecordType == ProgressRecordType.Completed)
                        {
                            DebugWriteLine("WriteProgressFinished(Source:{0},Activity:{1},ParentActivity:{2})", SourceId, record.ActivityId, record.ParentActivityId);
                            this.OnNavigationNext((Control)Item);
                        }
                        else
                        {
                            Trace.Write(".");
                            ((ProgressPanel)Item).Progress = record;
                        }
                        return;
                    }
            }
            DebugWriteLine("WriteProgressNew(Source:{0},Activity:{1},ParentActivity:{2})", SourceId, record.ActivityId, record.ParentActivityId);
            if (record.RecordType != ProgressRecordType.Completed)
                this.AddControl(new ProgressPanel() { Progress = record });
        }

        /// <summary>
        /// Write output to the console.
        /// NOTE: Will only handle one color set (Foreground and Background) per line. Lines with Multiple colors not allowed here.
        /// </summary>
        public void Write(System.Drawing.Color foregroundColor, System.Drawing.Color backgroundColor, string value)
        {
            GrowLabel FoundControl = null;
            if (this.Controls.Count > 0)
            {
                if (Parent.Controls[Parent.Controls.Count - 1].GetType() == typeof(GrowLabel))
                {
                    FoundControl = (GrowLabel)Parent.Controls[Parent.Controls.Count - 1];
                    if (foregroundColor != FoundControl.ForeColor || backgroundColor != FoundControl.BackColor)
                    {
                        FoundControl = null;
                    }
                }
            }
            if (FoundControl == null)
            {
                GrowLabel NewLabel = new GrowLabel(value);
                this.AddControl(NewLabel);
                NewLabel.ForeColor = foregroundColor;
                NewLabel.BackColor = backgroundColor;
            }
            else
            {
                FoundControl.Text += value;
            }

        }

        public void PromptForChoice(string caption, string message, System.Collections.ObjectModel.Collection<ChoiceDescription> choices, int defaultChoice)
        {
            FlowPanel ChoicePanel = new FlowPanel();
            ChoicePanel.AddControl(new GrowLabel(caption) { Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold) });
            ChoicePanel.AddControl(new GrowLabel(message));

            Trace.Indent();
            int Index = 0;
            foreach (var Item in choices)
            {
                DebugWriteLine("NewChoiceDescription: {0} {1}", Item.Label, Item.HelpMessage);
#if COMMANDLINK
                // Command Links are cool! But not for everyone.
                CommandLink Button = new CommandLink(){ Note = Item.HelpMessage};
                Button.Click += new EventHandler(delegate(object sender, EventArgs e)
                {
                    xResult = new PSObject(((CommandLink)sender).Tag);
                    this.OnNavigationNext(ChoicePanel);
                });
#else
                RadioButton Button = new RadioButton() { Checked = Index == defaultChoice };
                Button.Validating += new CancelEventHandler(delegate(object sender, CancelEventArgs e)
                {
                    if (((RadioButton)sender).Checked) xResult = new PSObject(((RadioButton)sender).Tag);
                });
#endif
                Button.Text = Item.Label;
                Button.Tag = Index++;
                Button.TabIndex = 1;
                Button.Font = new System.Drawing.Font(Button.Font.FontFamily, Button.Font.Size + 2);

                ChoicePanel.AddControl(Button);

                if (!string.IsNullOrEmpty(Item.HelpMessage) && Button.GetType() == typeof(RadioButton))
                {
                    ChoicePanel.AddControl(new GrowLabel(Item.HelpMessage));
                }
            }
            Trace.Unindent();

            //this.ClearScreen();
            this.DoChangeWindowTitle(caption);
            this.AddControl(ChoicePanel);
            this.OnControlNavigation(ChoicePanel);
        }

        public void Prompt(string caption, string message, System.Collections.ObjectModel.Collection<FieldDescription> descriptions)
        {
            FlowPanel PromptPanel = new FlowPanel();
            PromptPanel.AddControl(new GrowLabel(caption) { Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold) });
            PromptPanel.AddControl(new GrowLabel(message));

            Trace.Indent();
            int index = 0;
            foreach (var FieldDesc in descriptions)
            {
                string BaseType = FieldDesc.ParameterTypeFullName;

                DebugWriteLine("NewFieldDescription: Name:{0} Label:{1} Mandatory:{2} Type:{3} Default:[{4}] Attr: {5}",
                    FieldDesc.Name, FieldDesc.Label, FieldDesc.IsMandatory, FieldDesc.ParameterTypeFullName, FieldDesc.DefaultValue, FieldDesc.Attributes.ToString());

                switch (BaseType)
                {

                    case "System.IO.FileInfo":
                    case "System.IO.DirectoryInfo":

                        if (!string.IsNullOrEmpty(FieldDesc.Label))
                            PromptPanel.AddControl(new GrowLabel(FieldDesc.Label) { Margin = new Padding(0, 10, 0, 0) });

                        TextBox FileName = new TextBox() { Name = FieldDesc.Name };
                        PromptPanel.AddControl(FileName);
                        FileName.Validating += new CancelEventHandler(delegate(object sender, CancelEventArgs e)
                        {
                            if (FieldDesc.IsMandatory && string.IsNullOrEmpty(((TextBox)sender).Text))
                            {
                                ErrorProvider1.SetError((Control)sender, "Missing Text");
                                // e.Cancel = true;
                                return;
                            }
                            else
                                ErrorProvider1.Clear();

                            FileName.Tag = ((TextBox)sender).Text;

                        }
                        );

                        if (FieldDesc.DefaultValue != null)
                            if (!string.IsNullOrEmpty(FieldDesc.DefaultValue.BaseObject.ToString()))
                                FileName.Text = FieldDesc.DefaultValue.BaseObject.ToString();

                        Button MoreFileInfo = new Button() { FlatStyle = System.Windows.Forms.FlatStyle.Flat, Text = "Browse...", UseVisualStyleBackColor = true };
                        MoreFileInfo.Click += new EventHandler(delegate(object sender, EventArgs e)
                        {
                            if (BaseType == "System.IO.FileInfo")
                            {
                                OpenFileName ofn = new OpenFileName();
                                ofn.structSize = Marshal.SizeOf(ofn);
                                ofn.filter = "All Files\0*";

                                ofn.file = new String(new char[256]);
                                ofn.maxFile = ofn.file.Length;

                                ofn.fileTitle = new String(new char[64]);
                                ofn.maxFileTitle = ofn.fileTitle.Length;

                                ofn.initialDir = Environment.SystemDirectory;
                                ofn.title = "Open file";

                                if (LibWrap.GetOpenFileName(ofn))
                                {
                                    FileName.Text = ofn.file;
                                    FileName.Tag = ofn.file;
                                }
                            }
                            else
                            {
                                FolderBrowserDialog NewDialog = new FolderBrowserDialog();
                                if (NewDialog.ShowDialog() == DialogResult.OK)
                                {
                                    FileName.Text = NewDialog.SelectedPath;
                                    FileName.Tag = NewDialog.SelectedPath;
                                }
                            }
                        }
                        );

                        PromptPanel.Controls.Add(MoreFileInfo);

                        break;

                    case "System.Boolean":
                    case "System.Management.Automation.SwitchParameter":

                        CheckBox Switch = new CheckBox() { Name = FieldDesc.Name, Text = FieldDesc.Label, Tag = false, Margin = new Padding(0, 10, 0, 0) };
                        if (FieldDesc.DefaultValue.BaseObject.GetType() == typeof(System.Boolean))
                        {
                            Switch.Checked = (System.Boolean)FieldDesc.DefaultValue.BaseObject;
                        }
                        Switch.Validating += new CancelEventHandler(delegate(object sender, CancelEventArgs e)
                        {
                            ((CheckBox)sender).Tag = ((CheckBox)sender).Checked;
                        });
                        PromptPanel.AddControl(Switch);


                        break;

                    case "System.Management.Automation.PSCredential":

                        if (!string.IsNullOrEmpty(FieldDesc.Label))
                            PromptPanel.AddControl(new GrowLabel(FieldDesc.Label) { Margin = new Padding(0, 10, 0, 0) });

                        PromptPanel.AddControl(new GrowLabel("UserName"));
                        TextBox UserName = new TextBox() { Text = (FieldDesc.DefaultValue != null) ? ((string)FieldDesc.DefaultValue.BaseObject) : "" };
                        PromptPanel.AddControl(UserName);
                        UserName.Validating += new CancelEventHandler(delegate(object sender, CancelEventArgs e)
                        {
                            if (FieldDesc.IsMandatory && string.IsNullOrEmpty(((TextBox)sender).Text))
                            {
                                ErrorProvider1.SetError((Control)sender, "Missing Text");
                                e.Cancel = true;
                                return;
                            }
                            ((TextBox)sender).Tag = ((TextBox)sender).Text;
                            if (!e.Cancel)
                                ErrorProvider1.Clear();
                        }
                        );

                        PromptPanel.AddControl(new GrowLabel("Password"));
                        TextBox Password = new TextBox() { PasswordChar = '*', Name = FieldDesc.Name };
                        PromptPanel.AddControl(Password);
                        Password.Validating += new CancelEventHandler(delegate(object sender, CancelEventArgs e)
                        {
                            if (FieldDesc.IsMandatory && string.IsNullOrEmpty(((TextBox)sender).Text))
                            {
                                ErrorProvider1.SetError((Control)sender, "Missing Text");
                                e.Cancel = true;
                                return;
                            }
                            if (string.IsNullOrEmpty(UserName.Text))
                            {
                                ErrorProvider1.SetError((Control)sender, "Missing UserName");
                                e.Cancel = true;
                                return;
                            }
                            var secure = new System.Security.SecureString();
                            foreach (char c in ((TextBox)sender).Text)
                            {
                                secure.AppendChar(c);
                            }
                            ((TextBox)sender).Tag = new PSCredential(UserName.Text, secure);
                            if (!e.Cancel)
                                ErrorProvider1.Clear();
                        }
                        );

                        break;

                    case "System.Security.SecureString":

                        if (!string.IsNullOrEmpty(FieldDesc.Label))
                            PromptPanel.AddControl(new GrowLabel(FieldDesc.Label) { Margin = new Padding(0, 10, 0, 0) });

                        TextBox ReadSBox = new TextBox() { PasswordChar = '*', Name = FieldDesc.Name };
                        ReadSBox.Validating += new CancelEventHandler(delegate(object sender, CancelEventArgs e)
                        {
                            if (FieldDesc.IsMandatory && string.IsNullOrEmpty(((TextBox)sender).Text))
                            {
                                ErrorProvider1.SetError((Control)sender, "Missing Text");
                                e.Cancel = true;
                                return;
                            }
                            var secure = new System.Security.SecureString();
                            foreach (char c in ((TextBox)sender).Text)
                            {
                                secure.AppendChar(c);
                            }
                            ((TextBox)sender).Tag = secure;
                            if (!e.Cancel)
                                ErrorProvider1.Clear();
                        }
                            );
                        PromptPanel.AddControl(ReadSBox);

                        break;

                    case "System.String[]":

                        if (!string.IsNullOrEmpty(FieldDesc.Label))
                            PromptPanel.AddControl(new GrowLabel(FieldDesc.Label) { Margin = new Padding(0, 10, 0, 0) });

                        ComboBox ReadCombo = new ComboBox();
                        ReadCombo.Name = FieldDesc.Name;

                        if (FieldDesc.DefaultValue != null)
                            //if ( FieldDesc.DefaultValue.BaseObject.GetType().IsArray )
                            foreach (var Item in (FieldDesc.DefaultValue.BaseObject as object[]))
                                ReadCombo.Items.Add(Item.ToString());

                        if (ReadCombo.Items.Count > 0)
                            ReadCombo.Text = ReadCombo.Items[0].ToString();

                        ReadCombo.Validating += new CancelEventHandler(delegate (object sender, CancelEventArgs e)
                        {
                            if (FieldDesc.IsMandatory && string.IsNullOrEmpty(((ComboBox)sender).Text))
                            {
                                ErrorProvider1.SetError((Control)sender, "Missing Text");
                                e.Cancel = true;
                                return;
                            }
                            try
                            {
                                ((ComboBox)sender).Tag = ((ComboBox)sender).Text;
                            }
                            catch (SystemException ex)
                            {
                                ErrorProvider1.SetError((Control)sender, ex.Message);
                                e.Cancel = true;
                            }
                            if (!e.Cancel)
                                ErrorProvider1.Clear();
                        }
                            );

                        PromptPanel.AddControl(ReadCombo);
                        break;

                    case "System.Guid":
                    case "System.DateTime":
                    case "System.SByte":
                    case "System.Byte":
                    case "System.Char":
                    case "System.Decimal":
                    case "System.Double":
                    case "System.Int16":
                    case "System.Int32":
                    case "System.Int64":
                    case "System.Single":
                    case "System.UInt16":
                    case "System.UInt32":
                    case "System.UInt64":
                    case "System.UIntPtr":
                    case "System.String":

                        if (!string.IsNullOrEmpty(FieldDesc.Label))
                            PromptPanel.AddControl(new GrowLabel(FieldDesc.Label) { Margin = new Padding(0, 10, 0, 0) });

                        TextBox ReadBox = new TextBox();
                        ReadBox.Name = FieldDesc.Name;

                        if (FieldDesc.DefaultValue != null)
                            if (!string.IsNullOrEmpty(FieldDesc.DefaultValue.BaseObject.ToString()))
                                ReadBox.Text = FieldDesc.DefaultValue.BaseObject.ToString();

                        ReadBox.Validating += new CancelEventHandler(delegate(object sender, CancelEventArgs e)
                        {
                            if (FieldDesc.IsMandatory && string.IsNullOrEmpty(((TextBox)sender).Text))
                            {
                                ErrorProvider1.SetError((Control)sender, "Missing Text");
                                e.Cancel = true;
                                return;
                            }
                            try
                            {
                                ((TextBox)sender).Tag = TypeDescriptor.GetConverter(Type.GetType(BaseType)).ConvertFromInvariantString(((TextBox)sender).Text);
                            }
                            catch (SystemException ex)
                            {
                                ErrorProvider1.SetError((Control)sender, ex.Message);
                                e.Cancel = true;
                            }
                            if (!e.Cancel)
                                ErrorProvider1.Clear();
                        }
                            );
                        PromptPanel.AddControl(ReadBox);

                        break;

                    // Not Supported in free version of PowerShell Wizard Host
                    case "MDT_Application/Driver/Package/OperatingSystem/TaskSequence":  // Future
                    case "Microsoft.HyperV.PowerShell.VMSwitch": //Future
                    case "Microsoft.Management.Infrastructure.CimInstance#ROOT/StandardCimv2/MSFT_NetAdapter": //Future

                    case "System.Collections.Hashtable":
                    case "System.Xml.XmlDocument":
                    default:
                        break;
                }

                if (!string.IsNullOrEmpty(FieldDesc.HelpMessage))
                    PromptPanel.AddControl(new GrowLabel(FieldDesc.HelpMessage));

                index++;
            }
            Trace.Unindent();

            // this.ClearScreen();
            // this.DoChangeWindowTitle("Prompt");
            this.AddControl(PromptPanel);
            this.OnControlNavigation(PromptPanel, true);

        }

        #endregion

        #region MDTObject

        TreeView MDTView = null;

        public void EndMDTView()
        {
            this.Controls.Remove(MDTView);
            MDTView  = null;
        }

        public void DisplayMDTView(string RootNode, CommandInvocationIntrinsics InvokeCommand, bool ShouldWait, bool MultiSelect, string IconName)
        {
            ImageList NewList = new ImageList();

            MDTView = new TreeView() { CheckBoxes = MultiSelect };
            MDTView.BeforeSelect += new TreeViewCancelEventHandler(delegate(object sender, TreeViewCancelEventArgs e)
                {
                    // Do not allow the user to select a folder in non MultiSelect mode.
                    e.Cancel = !MultiSelect;
                    if (e.Node.Tag != null )
                        if ( ! (bool)((PSObject)e.Node.Tag).Properties["PSISContainer"].Value )
                            e.Cancel = false;
                });
            
            // Get some stock images for folders and files...
            NewList.Images.Add(IconType.GetImage("c:\\windows", (uint)IconType.FILE_ATTRIBUTE_DIRECTORY, 0));
            NewList.Images.Add(IconType.GetImage((IconName!=null)?IconName:"file.Dat", 0, (uint)IconType.SHGFI_USEFILEATTRIBUTES));
            MDTView.ImageList = NewList;

            MDTView.Nodes.Clear();
            TreeNode Root = new TreeNode(RootNode, 0, 0);
            MDTView.Nodes.Add(Root);
            IconType.HideCheckBox(Root);
            int Count = BuildTree(Root, RootNode, InvokeCommand);
            MDTView.ExpandAll();
            MDTView.Height = MDTView.ItemHeight * (Count + 2) + 5;

            this.AddControl(MDTView);
            if (ShouldWait)
                this.OnControlNavigation(MDTView, false);
        }

        public int BuildTree(TreeNode Node, string Path, CommandInvocationIntrinsics InvokeCommand)
        {
            int Count = 0;
            foreach ( PSObject result in InvokeCommand.InvokeScript("get-childitem -path '" + Path + "'"))
            {
                if ( result.Properties["PSISContainer"] != null )
                {
                    TreeNode NewNode = new TreeNode((string)result.Properties["Name"].Value, 1, 1);
                    NewNode.Tag = result;
                    Node.Nodes.Add(NewNode);
                    if ((bool)result.Properties["PSISContainer"].Value)
                    {
                        NewNode.ImageIndex = 0;
                        NewNode.SelectedImageIndex = 0;
                        IconType.HideCheckBox(NewNode);
                        Count += BuildTree(NewNode, Path + "\\" + (string)result.Properties["Name"].Value, InvokeCommand);
                    }
                    Count++;
                }
            }
            return Count;
        }

        public void GetAllSelectedTreeNodes ( List<PSObject> ReturnCollection, TreeNode Node)
        {
            if (Node.TreeView.CheckBoxes && Node.Checked)
                ReturnCollection.Add((PSObject)Node.Tag);
            else if (!Node.TreeView.CheckBoxes && Node.IsSelected)
            {
                ReturnCollection.Add((PSObject)Node.Tag);
                return;
            }

            foreach (TreeNode ChildNode in Node.Nodes)
                GetAllSelectedTreeNodes(ReturnCollection, ChildNode);
        }

        #endregion


        #region GridEdit

        private DataGridView GridEdit = null;


        public void EndGridEdit()
        {
            this.Controls.Remove(GridEdit);
            GridEdit = null;
        }

        public bool isActiveGridEdit()
        {
            return GridEdit != null;
        }

        public void FinishedGridEdit(object objects, int Count, int[] HeaderWidths )
        {
            bool hasColHeaders = true;
            GridEdit = new DataGridView()
            {
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                MultiSelect = false,
                ColumnHeadersVisible = hasColHeaders,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = hasColHeaders ? DataGridViewAutoSizeColumnsMode.None : DataGridViewAutoSizeColumnsMode.Fill
            };

            GridEdit.DataSource = objects;

            //Special handling for Column Display
            this.AddControl(GridEdit);

            int FoundToolTip = -1;
            if ( HeaderWidths != null )
            {
                int Remaining = GridEdit.Width - 3;
                for (int i = 0; i < Math.Min(HeaderWidths.Length, GridEdit.Columns.Count); i++)
                {
                    if ( HeaderWidths[i] == 0 )
                    {
                        GridEdit.Columns[i].Visible = false;
                    }
                    else
                    {
                        GridEdit.Columns[i].Width = Math.Min(Math.Abs(HeaderWidths[i]),Remaining);
                        Remaining -= Math.Abs(HeaderWidths[i]);
                        GridEdit.Columns[i].ReadOnly = (HeaderWidths[i] < 0);
                    }
                    if ( GridEdit.Columns[i].Name.ToLower() == "tooltip" )
                        FoundToolTip = i;
                }
            }

            if (FoundToolTip != -1)
            {
                foreach ( DataGridViewRow Row in GridEdit.Rows)
                {
                    foreach ( DataGridViewCell Cell in Row.Cells )
                    {
                        Cell.ToolTipText = Row.Cells[FoundToolTip].Value.ToString();
                    }
                }

            }

            // Calculate the Height of the control
            int h = hasColHeaders ? GridEdit.ColumnHeadersHeight : 0;
            foreach (DataGridViewRow Row in GridEdit.Rows)
                h += Row.Height;
            int w = 0;
            foreach (DataGridViewColumn Col in GridEdit.Columns)
                w += Col.Width;
            if (w > this.Width)
                h += System.Windows.Forms.SystemInformation.HorizontalScrollBarHeight;
            GridEdit.Height = h + 2;
            this.OnControlNavigation(GridEdit, false);
        }

        #endregion


        #region GridView

        private DataGridView GridView = null;

        public void EndGridView()
        {
            this.Controls.Remove(GridView);
            GridView = null;
        }

        public bool isActiveGridView()
        {
            return GridView != null;
        }

        public void FinishedGridView(object objects, int Count, bool ShouldWait, bool MultiSelect, bool ShowHeader)
        {
            GridView = new DataGridView()
            {
                ReadOnly = true,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                CellBorderStyle = DataGridViewCellBorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = MultiSelect,
                ColumnHeadersVisible = ShowHeader,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = ShowHeader?DataGridViewAutoSizeColumnsMode.None:DataGridViewAutoSizeColumnsMode.Fill
            };

            GridView.DataSource = objects;
            //Special handling for Column Display
            for ( int i = 0; i < GridView.Columns.Count ; i++ )
            {
                GridView.AutoResizeColumn(i, DataGridViewAutoSizeColumnMode.ColumnHeader);
                if (i >= 10)
                    GridView.Columns[i].Visible = false;  // Maximum of 10 Columns for Visibility
            }
            this.AddControl(GridView);

            // Calculate the Height of the control
            int h = ShowHeader ? GridView.ColumnHeadersHeight : 0;
            foreach (DataGridViewRow Row in GridView.Rows)
                h += Row.Height;
            int w = 0;
            foreach (DataGridViewColumn Col in GridView.Columns)
                w += Col.Width;
            if (w > this.Width)
                h += System.Windows.Forms.SystemInformation.HorizontalScrollBarHeight;
            GridView.Height = h + 2;

            if (ShouldWait)
                this.OnControlNavigation(GridView, false);
        }

        #endregion

        #region Properties

        [DefaultValue(0)]
        public int ExitCode
        { get; set; }

        public string Script
        { get; set; }

        public PSInvocationState GetCurrentState
        {
            get { return powershell.InvocationStateInfo.State; }
        }

        public CommandParameterCollection ParsedParameters
        { get { return CommandParams; } }

        public bool isPowerShellRunning
        {
            get
            {
                switch (GetCurrentState)
                {
                    case PSInvocationState.Disconnected:
                    case PSInvocationState.Stopping:
                    case PSInvocationState.Running:
                        return true;
                    case PSInvocationState.Failed:
                    case PSInvocationState.NotStarted:
                    case PSInvocationState.Completed:
                    case PSInvocationState.Stopped:
                    default:
                        return false;
                }
            }
        }

        #endregion

        #region Control Navigation

        /// <summary>
        /// Cross thread processing.
        /// </summary>

        private EventWaitHandle mWaitForNext = new EventWaitHandle(false, EventResetMode.AutoReset);
        public event EventHandler<EventArgs> ControlNavigationReady = null;
        private Control RequestedControlToCloseOnNext = null;
        private PSObject xResult = null;
        private Dictionary<string, PSObject> xResults = null;
        private bool isPromptFormActive = false;
        private bool isCanceled = false;

        /// <summary>
        /// The script has placed user controls on the form, and request a "next" button to continue.
        /// </summary>
        public void OnControlNavigation(Control ControlToClose, bool isPrompt)
        {
            OnControlNavigation(ControlToClose);
            isPromptFormActive = isPrompt;
        }

        public void OnControlNavigation(Control ControlToClose)
        {
            OnControlNavigation();
            RequestedControlToCloseOnNext = ControlToClose;
        }

        public void OnControlNavigation()
        {
            DebugWriteLine("OnControlNavigation - Prepare for user input...");
            isPromptFormActive = false;
            RequestedControlToCloseOnNext = null;
            OnResultPrepare();
            xResult = null;
            if (this.ControlNavigationReady != null)
                this.ControlNavigationReady(this, new EventArgs());
        }

        public void OnResultPrepare()
        {
            mWaitForNext.Reset();
            isCanceled = false;
        }

        /// <summary>
        /// Called from PSHost thread requesting data to be ready.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool WaitForResult(out PSObject result)
        {
            DebugWriteLine("WaitForResult - Wait for UserInput... result");
            mWaitForNext.WaitOne();
            DebugWriteLine("WaitForResult - Ready to continue... result");
            Trace.WriteLineIf(isPromptFormActive, "WaitForResult: isPromptFormActive", "PSHostControl");
            result = xResult;
            return (xResult != null && !isCanceled);
        }

        /// <summary>
        /// Called from PSHost thread requesting data to be ready.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool WaitForResult(out Dictionary<string, PSObject> result)
        {
            DebugWriteLine("WaitForResult - Wait for UserInput... results");
            mWaitForNext.WaitOne();
            DebugWriteLine("WaitForResult - Ready to continue... results");
            Trace.WriteLineIf(!isPromptFormActive, "WaitForResult: isPromptFormActive", "PSHostControl");
            result = xResults;
            return (xResults != null && !isCanceled);
        }

        /// <summary>
        /// Called from PSHost thread requesting data to be ready.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool WaitForResultEdit(out List<PSObject> result)
        {
            DebugWriteLine("WaitForResultEdit - Wait for UserInput... results");
            mWaitForNext.WaitOne();
            DebugWriteLine("WaitForResultEdit - Ready to continue... results");
            Trace.WriteLineIf(!isPromptFormActive, "WaitForResultEdit: isPromptFormActive", "PSHostControl");
            if (RequestedControlToCloseOnNext is System.Windows.Forms.DataGridView && !isCanceled)
            {
                List<PSObject> newResult = new List<PSObject>();
                foreach (DataGridViewRow Row in ((DataGridView)RequestedControlToCloseOnNext).Rows)
                    if (Row.Index != -1)
                        newResult.Add(new PSObject(Row.DataBoundItem));
                result = newResult;
                return true;
            }
            result = null;
            return false;
        }

        /// <summary>
        /// Called from out-gridview thread requesting data to be ready.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool WaitForResult(out List<int> result)
        {
            DebugWriteLine("WaitForResult - Wait for UserInput... results");
            mWaitForNext.WaitOne();
            DebugWriteLine("WaitForResult - Ready to continue... results");
            Trace.WriteLineIf(!isPromptFormActive, "WaitForResult: isPromptFormActive", "PSHostControl");
            if (RequestedControlToCloseOnNext is System.Windows.Forms.DataGridView && !isCanceled)
            {
                List<int> newResult = new List<int>();
                foreach (DataGridViewRow Row in ((DataGridView)RequestedControlToCloseOnNext).SelectedRows)
                    if (Row.Index != -1)
                        newResult.Add(Row.Index);
                result = newResult;
                return true;
            }
            result = null;
            return false;
        }


        /// <summary>
        /// Called from out-gridview thread requesting data to be ready.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool WaitForResultMDT(out List<PSObject> result)
        {
            DebugWriteLine("WaitForResultMDT - Wait for UserInput... results");
            mWaitForNext.WaitOne();
            DebugWriteLine("WaitForResultMDT - Ready to continue... results");
            Trace.WriteLineIf(!isPromptFormActive, "WaitForResultMDT: isPromptFormActive", "PSHostControl");
            if (RequestedControlToCloseOnNext is System.Windows.Forms.TreeView && !isCanceled)
            {
                List<PSObject> newResult = new List<PSObject>();
                SynchronizedInvoke.InvokeIfRequired(this, () => this.GetAllSelectedTreeNodes(newResult,((TreeView)this.RequestedControlToCloseOnNext).TopNode));
                result = newResult;
                return true;
            }
            result = null;
            return false;
        }



        /// <summary>
        /// User has finished with input and is ready to continue (next)
        /// </summary>
        /// 
        public void OnNavigationNextPrompt()
        {
            if (isPromptFormActive)
            {
                DebugWriteLine("OnNavigationNextPrompt - Gather up all child elements...");
                Trace.Indent();
                xResults = new Dictionary<string, PSObject>();
                foreach (Control control in this.Controls)
                {
                    if (control.GetType() == typeof(FlowPanel))
                    {
                        foreach (Control subcontrol in control.Controls)
                            if (!string.IsNullOrEmpty(subcontrol.Name))
                            {
                                DebugWriteLine("OnNavigationNextPrompt() Found: Name = [{0}]", subcontrol.Name);
                                xResults.Add(subcontrol.Name, new PSObject(subcontrol.Tag));
                            }
                        break;
                    }
                }
                Trace.Unindent();
            }

            mWaitForNext.Set();
            if (RequestedControlToCloseOnNext != null)
                this.Controls.Remove(RequestedControlToCloseOnNext);
        }

        public void OnNavigationNext(Control ControlToClose)
        {
            Trace.WriteLineIf(isPromptFormActive, "OnNavigationNext: isPromptFormActive", "PSHostControl");
            ((Form)this.TopLevelControl).ValidateChildren();
            mWaitForNext.Set();
            this.Controls.Remove(ControlToClose);
        }

        public void onNavigationCancel()
        {
            DebugWriteLine("onNavigationCancel - Signal to continue...");
            isCanceled = true;
            mWaitForNext.Set();
        }

        #endregion

        #region Callbacks

        public event EventHandler<PSInvocationStateChangedEventArgs> InvocationStateChanged = null;
        public event EventHandler<LinkClickedEventArgs> WindowTitleChanged = null;

        public void DoChangeWindowTitle(string NewTitle)
        {
            if (this.WindowTitleChanged != null)
                this.WindowTitleChanged(this, new LinkClickedEventArgs(NewTitle));
        }

        private void Powershell_InvocationStateChanged(object sender, PSInvocationStateChangedEventArgs e)
        {
            string ErrorMessage = null;
            if (e.InvocationStateInfo.State == PSInvocationState.Failed)
            {
                System.Management.Automation.RuntimeException r = (System.Management.Automation.RuntimeException)e.InvocationStateInfo.Reason;
                if (r.ErrorRecord.InvocationInfo != null)
                {
                    ErrorMessage = string.Format("{0} : {1}\r\n{2}\r\n+ CategoryInfo: {3}\r\n+ FullyQualifiedErrorId: {4}",
                        (r.ErrorRecord.InvocationInfo.MyCommand != null) ? r.ErrorRecord.InvocationInfo.MyCommand.Name : "<NULL>",
                        r.ErrorRecord.Exception.Message,
                        r.ErrorRecord.InvocationInfo.PositionMessage,
                        r.ErrorRecord.CategoryInfo,
                        r.ErrorRecord.FullyQualifiedErrorId);
                }
                else
                {
                    ErrorMessage = string.Format("{0} : {1}\r\n{2}\r\n+ CategoryInfo: {3}\r\n+ FullyQualifiedErrorId: {4}",
                        "<null>",
                        r.ErrorRecord.Exception.Message,
                        "<null>",
                        r.ErrorRecord.CategoryInfo,
                        r.ErrorRecord.FullyQualifiedErrorId);
                }
                if (ErrorMessage != null)
                {
                    DebugWriteLine(ErrorMessage);
                    this.PSHost.UI.WriteErrorLine(ErrorMessage);
                }

            }
            if (this.InvocationStateChanged != null)
            {
                SynchronizedInvoke.InvokeIfRequired(this, () => this.InvocationStateChanged(this, e));
            }
        }

        #endregion

        #region Support

        public void DebugWriteLine(string message)
        {
            Trace.WriteLine(message, "PSHostControl");
        }

        public void DebugWriteLine(string format, params object[] args)
        {
            this.DebugWriteLine(string.Format(format, args));
        }

        /// <summary>
        /// Custom Code for processing the commandline from the host program.
        /// Injects all arguments into the current Powershell Command as Parameters
        /// </summary>
        /// <param name="Args"></param>
        private void ParsePowerShellParmsFromArgs(string[] Args)
        {
            string Parameter = null;
            Regex MyRegEx = new Regex("^(?:/|-)([^/-: ]+)(?::?)([^:]*)$");
            for (int i = 1; i < Args.Length; i++)
            {
                Match Match = MyRegEx.Match(Args[i]);
                if (Match.Success && Match.Groups.Count == 3)
                {
                    // Found a PowerShell style command Line Argument

                    if (Parameter != null)
                    {
                        DebugWriteLine("CommandLine.AddParameter {0} = true", Parameter);
                        powershell.AddParameter(Parameter);
                    }

                    if (Match.Groups[2].Value.Trim() == "")
                    {
                        Parameter = Match.Groups[1].Value;
                    }
                    else if (Match.Groups[2].Value.ToUpper() == "$TRUE") // Special Case 
                    {
                        DebugWriteLine("CommandLine.AddParameter {0} = true", Match.Groups[1].Value);
                        powershell.AddParameter(Match.Groups[1].Value, true);
                        Parameter = null;
                    }
                    else if (Match.Groups[2].Value.ToUpper() == "$FALSE") // Special Case
                    {
                        DebugWriteLine("CommandLine.AddParameter {0} = true", Match.Groups[1].Value);
                        powershell.AddParameter(Match.Groups[1].Value, false);
                        Parameter = null;
                    }
                    else
                    {
                        DebugWriteLine("CommandLine.AddParameter {0} = {1}", Match.Groups[1].Value, Match.Groups[2].Value);
                        powershell.AddParameter(Match.Groups[1].Value, Match.Groups[2].Value);
                        Parameter = null;
                    }
                }
                else
                {
                    string Val = Args[i].Trim().TrimEnd(new char[] { '\'', '\"' }).TrimStart(new char[] { '\'', '\"' });
                    if (Parameter != null)
                    {
                        DebugWriteLine("CommandLine.AddParameter {0} = {1}", Parameter, Val);
                        powershell.AddParameter(Parameter, Val);
                        Parameter = null;
                    }
                    else
                    {
                        DebugWriteLine("CommandLine.AddArgument {0} ", Parameter, Val);
                        powershell.AddArgument(Val);
                    }
                }

            }

            if (Parameter != null)
            {
                powershell.AddParameter(Parameter); //Flush...
            }



        }


        public void AddControl(System.Windows.Forms.Control NewControl)
        {
            NewControl.TabIndex = 1;
            NewControl.Dock = DockStyle.Top;
            NewControl.Margin = new Padding(1);
            this.Controls.Add(NewControl);
            NewControl.Focus();
        }

        #endregion

    }

    #region Support Classes

    public class IconType
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0; // 'Large icon
        public const uint SHGFI_SMALLICON = 0x1; // 'Small icon
        public const uint SHGFI_OPENICON = 0x000000002;
        public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        public const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DestroyIcon(IntPtr hIcon);

        public static Icon GetImage(string fName, uint dwFileAttr, uint Options)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            var res = SHGetFileInfo(fName, dwFileAttr , ref shinfo, (uint)Marshal.SizeOf(shinfo), Options| SHGFI_OPENICON | SHGFI_ICON | SHGFI_SMALLICON);
            if (res == IntPtr.Zero)
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());

            // Now clone the icon, so that it can be successfully stored in an ImageList
            var icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();

            DestroyIcon(shinfo.hIcon);
            return icon;
        }

        // constants used to hide a checkbox
        public const int TVIF_STATE = 0x8;
        public const int TVIS_STATEIMAGEMASK = 0xF000;
        public const int TV_FIRST = 0x1100;
        public const int TVM_SETITEM = TV_FIRST + 63;

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        // struct used to set node properties
        public struct TVITEM
        {
            public int mask;
            public IntPtr hItem;
            public int state;
            public int stateMask;
            [MarshalAs(UnmanagedType.LPTStr)]
            public String lpszText;
            public int cchTextMax;
            public int iImage;
            public int iSelectedImage;
            public int cChildren;
            public IntPtr lParam;
        }

        public static void HideCheckBox(TreeNode node)
        {
            TVITEM tvi = new TVITEM();
            tvi.hItem = node.Handle;
            tvi.mask = TVIF_STATE;
            tvi.stateMask = TVIS_STATEIMAGEMASK;
            tvi.state = 0;
            IntPtr lparam = Marshal.AllocHGlobal(Marshal.SizeOf(tvi));
            Marshal.StructureToPtr(tvi, lparam, false);
            SendMessage(node.TreeView.Handle, TVM_SETITEM, IntPtr.Zero, lparam);
        }

    }

    /// <summary>
    /// Prototype for a Callback from Powershell. Allows us to make direct calls for anything we want.
    /// </summary>
    public static class PSHostCallBack
    {

        public static PSHostControl ParentControl
        { get; set; }

        public static string GetHostExe
        {
            get
            {
                return System.Reflection.Assembly.GetExecutingAssembly().Location;
            }
        }

        public static void DisplayRTF(string Data)
        {
            Trace.Assert(ParentControl != null, "Parent Control not ready yet");
            Trace.WriteLine(string.Format("DisplayRTF(Size:{0})", Data.Length), "PSHostCallback");

            RichTextBox RichTextBox1 = new RichTextBox()
            {
                Dock = DockStyle.Top,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = ParentControl.BackColor
            };
            RichTextBox1.ContentsResized += new ContentsResizedEventHandler(delegate(object sender, ContentsResizedEventArgs e)
            {
                ((RichTextBox)sender).Height = e.NewRectangle.Height;
            });

            SynchronizedInvoke.InvokeIfRequired(ParentControl, () => ParentControl.AddControl(RichTextBox1));
            SynchronizedInvoke.InvokeIfRequired(ParentControl, () => RichTextBox1.Rtf = Data);

        }

        public static void WaitForNext()
        {
            Trace.Assert(ParentControl != null, "Parent Control not ready yet");
            Trace.WriteLine("WaitForNExt()", "PSHostCallback");
            SynchronizedInvoke.InvokeIfRequired(ParentControl, () => ParentControl.OnControlNavigation());
            PSObject result = null;
            ParentControl.WaitForResult(out result);
        }


        public static void DisplayImage(string File)
        {
            Trace.Assert(ParentControl != null, "Parent Control not ready yet");
            Trace.WriteLine(string.Format("DisplayImage({0})", File), "PSHostCallback");
            SynchronizedInvoke.InvokeIfRequired(ParentControl, () => ParentControl.AddControl(new PictureBox() { Dock = DockStyle.Top, ImageLocation = File }));
        }

        public static string GetFileFromResource(string File)
        {
            Trace.Assert(ParentControl != null, "Parent Control not ready yet");
            Trace.WriteLine(string.Format("GetFileFromResource(PowerShell_Wizard_Host.{0})", File), "PSHostCallback");
            return (new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("PowerShell_Wizard_Host." + File))).ReadToEnd();
        }

        public static void DisplayHyperLink(string Text, string Link)
        {
            DisplayHyperLink(Text, Link, "");
        }

        public static void DisplayHyperLink(string Text, string Link, string Arguments)
        {
            Trace.Assert(ParentControl != null, "Parent Control not ready yet");
            Trace.WriteLine(string.Format("DisplayHyperLink({0},{1})", Text, Link), "PSHostCallback");
            LinkLabel MyLink = new LinkLabel() { Text = Text };
            MyLink.LinkClicked += new LinkLabelLinkClickedEventHandler(delegate(object sender, LinkLabelLinkClickedEventArgs e)
            {
                try { System.Diagnostics.Process.Start(Link, Arguments); }
                catch { }
            });
            SynchronizedInvoke.InvokeIfRequired(ParentControl, () => ParentControl.AddControl(MyLink));
        }

    }

    /// <summary>
    /// Label control that allows for better text wrapping.
    /// Stolen from: https://social.msdn.microsoft.com/Forums/windows/en-US/97c18a1d-729e-4a68-8223-0fcc9ab9012b/automatically-wrap-text-in-label?forum=winforms
    /// </summary>
    public class GrowLabel : Label
    {
        private bool mGrowing;
        public GrowLabel()
            : base()
        {
            this.AutoSize = false;
        }

        public GrowLabel(string value)
            : base()
        {
            this.AutoSize = false;
            this.Dock = DockStyle.Top;
            // Trim off the *last* line of any string
            if (value.EndsWith("\r\n"))
                this.Text = value.Substring(0, value.Length - 2);
            else if (value.EndsWith("\r"))
                this.Text = value.Substring(0, value.Length - 1);
            else if (value.EndsWith("\n"))
                this.Text = value.Substring(0, value.Length - 1);
            else
                this.Text = value;
#if DEBUG
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
#endif
        }

        private void resizeLabel()
        {
            if (mGrowing) return;
            try
            {
                mGrowing = true;
                System.Drawing.Size sz = new System.Drawing.Size(this.Width, Int32.MaxValue);
                sz = TextRenderer.MeasureText(this.Text, this.Font, sz, TextFormatFlags.WordBreak);
                this.ClientSize = new System.Drawing.Size(this.ClientSize.Width, sz.Height + this.Padding.Vertical);
            }
            finally
            {
                mGrowing = false;
            }
        }
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            resizeLabel();
        }
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            resizeLabel();
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            resizeLabel();
        }
    }

    /// <summary>
    /// Class for a CommandLink style button
    /// Stolen from http://blogs.msdn.com/b/knom/archive/2007/03/12/command_5f00_link.aspx
    /// </summary>
    class CommandLink : Button
    {
        private const int BS_COMMANDLINK = 0x0000000E;
        private const uint BCM_SETNOTE = 0x00001609;
        private const uint BCM_SETSHIELD = 0x0000160C;

        private string note_ = "";
        private bool mGrowing;

        public CommandLink()
        {
            this.FlatStyle = FlatStyle.System;
            this.AutoSize = true;
            this.Height = 41;
        }

        //Imports the user32.dll
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(HandleRef hWnd, UInt32 Msg, IntPtr wParam, string lParam);

        // Read / Write Property for the User Name. This Property
        // will be visible in the containing application.
        public string Note
        {
            get { return this.note_; }
            set
            {
                this.note_ = value;
                SendMessage(new HandleRef(this, this.Handle), BCM_SETNOTE, IntPtr.Zero, this.note_);
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cParams = base.CreateParams;
                cParams.Style |= BS_COMMANDLINK;
                return cParams;
            }
        }

        private void resizeLabel()
        {
            if (mGrowing) return;
            try
            {
                mGrowing = true;
                this.Height = 41;
                if (!string.IsNullOrEmpty(note_))
                {
                    System.Drawing.Size sz = new System.Drawing.Size(this.Width - 10, Int32.MaxValue);
                    sz = TextRenderer.MeasureText(this.note_.Trim(), this.Font, sz, TextFormatFlags.WordBreak);
                    this.Height += 3 + sz.Height;
                }
            }
            finally
            {
                mGrowing = false;
            }
        }
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            resizeLabel();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            resizeLabel();
        }

    }

    public class FlowPanel : System.Windows.Forms.TableLayoutPanel
    {
        public FlowPanel()
            : base()
        {
            this.TabIndex = 1;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Dock = DockStyle.Top;
            this.Margin = new Padding(1);
            this.Padding = new Padding(0, 0, 15, 0);
#if DEBUG
            this.BorderStyle = BorderStyle.FixedSingle;
#endif
        }

        public void AddControl(Control newItem)
        {
            newItem.Dock = DockStyle.Top;
            this.Controls.Add(newItem);
        }

    }

    public class ProgressPanel : System.Windows.Forms.TableLayoutPanel
    {
        private ProgressRecord CurrentProgress = null;
        private GrowLabel MainLabel = new GrowLabel() { Dock = DockStyle.Top };
        private GrowLabel StatusLabel = new GrowLabel() { Dock = DockStyle.Top };
        private GrowLabel SubLabel = new GrowLabel() { Dock = DockStyle.Top };
        private System.Windows.Forms.ProgressBar MainProgress = new System.Windows.Forms.ProgressBar() { Dock = DockStyle.Top };


        public ProgressPanel()
            : base()
        {
            this.Controls.Add(MainLabel);
            this.Controls.Add(StatusLabel);
            this.Controls.Add(MainProgress);
            this.Controls.Add(SubLabel);
            this.AutoSize = true;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        }

        public ProgressRecord Progress
        {
            get { return CurrentProgress; }
            set
            {
                MainLabel.Text = value.Activity;
                StatusLabel.Visible = !string.IsNullOrEmpty(value.StatusDescription);
                StatusLabel.Text = value.StatusDescription;
                MainProgress.Visible = (value.PercentComplete >= 0 && value.PercentComplete <= 100);
                if (value.PercentComplete >= 0 && value.PercentComplete <= 100)
                {
                    MainProgress.Value = value.PercentComplete;
                }
                SubLabel.Visible = !string.IsNullOrEmpty(value.CurrentOperation);
                SubLabel.Text = value.CurrentOperation;
                CurrentProgress = value;
            }
        }

        public bool IsEqual(ProgressRecord b)
        {
            if (b == null)
                return false;
            return (CurrentProgress.ActivityId == b.ActivityId) && (CurrentProgress.ParentActivityId == b.ParentActivityId);
        }

    }

    
    #endregion

    #region GetOpenFileName

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class OpenFileName
    {
        public int structSize = 0;
        public IntPtr dlgOwner = IntPtr.Zero;
        public IntPtr instance = IntPtr.Zero;

        public String filter = null;
        public String customFilter = null;
        public int maxCustFilter = 0;
        public int filterIndex = 0;

        public String file = null;
        public int maxFile = 0;

        public String fileTitle = null;
        public int maxFileTitle = 0;

        public String initialDir = null;

        public String title = null;

        public int flags = 0;
        public short fileOffset = 0;
        public short fileExtension = 0;

        public String defExt = null;

        public IntPtr custData = IntPtr.Zero;
        public IntPtr hook = IntPtr.Zero;

        public String templateName = null;

        public IntPtr reservedPtr = IntPtr.Zero;
        public int reservedInt = 0;
        public int flagsEx = 0;
    }

    public class LibWrap
    {
        //BOOL GetOpenFileName(LPOPENFILENAME lpofn);

        [DllImport("Comdlg32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);
    }

    #endregion


}

