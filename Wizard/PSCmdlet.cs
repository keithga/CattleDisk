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
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.Windows.Forms;
    using System.Data;
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using Microsoft.PowerShell.Commands;

    public enum OutputModeOption
    {
        None,
        Single,
        Multiple
    }

    [Cmdlet(VerbsCommon.Select, "MDTObject")]
    public class SelectMDTObject : PSCmdlet
    {
        [Parameter()]
        public OutputModeOption OutputMode
        { get; set; }

        [Parameter()]
        public string RootNode
        { get; set; }

        [Parameter()]
        public string FileType
        { get; set; }

        protected override void EndProcessing()
        {
            base.EndProcessing();

            ////////////////////////////////////////////////////////////////

            // Display List
            SynchronizedInvoke.InvokeIfRequired(PSHostCallBack.ParentControl, () => PSHostCallBack.ParentControl.DisplayMDTView(
                RootNode, base.InvokeCommand, this.OutputMode != OutputModeOption.None, this.OutputMode == OutputModeOption.Multiple, FileType));
            
            if (this.OutputMode == OutputModeOption.None)
                return; // Done do not wait for output...

            List<PSObject> selectedItems = null;
            if (PSHostCallBack.ParentControl.WaitForResultMDT(out selectedItems))
                if (selectedItems != null && selectedItems.Count > 0 && this.OutputMode != OutputModeOption.None)
                {
                    Trace.TraceInformation("Out-GridView() Complete Count: {0}", selectedItems.Count);
                    foreach (PSObject Item in selectedItems)
                    {
                        base.WriteObject(Item);
                    }
                }

            // Cleanup
            base.StopProcessing();
            SynchronizedInvoke.InvokeIfRequired(PSHostCallBack.ParentControl, () => PSHostCallBack.ParentControl.EndMDTView());

        }

    }


    [Cmdlet(VerbsData.Edit, "KeyValuePair", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113364")]
    public class HostGridEdit : PSCmdlet
    {
        List<PSObject> FullList = null;

        private ArrayList SendObj = new ArrayList();

        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject
        { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            FullList = new List<PSObject>();
        }

        [Parameter]
        public int[] HeaderWidths
        { get; set; }


        protected override void ProcessRecord()
        {
            if (this.InputObject == null)
                return;

            if (this.InputObject.BaseObject.GetType().IsArray)
            {
                foreach (object element in (object[])this.InputObject.BaseObject)
                    FullList.Add(new PSObject(element));
            }
            else if (this.InputObject.BaseObject is IDictionary)
            {
                foreach (object element in (IDictionary)this.InputObject.BaseObject)
                    FullList.Add(new PSObject(element));
            }
            else
            {
                FullList.Add(this.InputObject);
            }
            base.ProcessRecord();
        }

        protected override void EndProcessing()
        {
            object SendObj = null;

            if (FullList.Count == 0)
                return; //nothing to do.

            SendObj = new ArrayList();
            ((ArrayList)SendObj).AddRange(FullList);  // XxX Verify - Might be FUllList.BaseObject

            ////////////////////////////////////////////////////////////////

            // Display List
            SynchronizedInvoke.InvokeIfRequired(PSHostCallBack.ParentControl, () => PSHostCallBack.ParentControl.FinishedGridEdit(SendObj, FullList.Count, this.HeaderWidths));

            List<PSObject> result = null;
            if (PSHostCallBack.ParentControl.WaitForResultEdit(out result))
            {
                Trace.TraceInformation("Out-GridEdit() Complete Count: {0}", result.Count);
                foreach ( var Item in result )
                {
                    base.WriteObject ( Item );
                }
            }

            // Cleanup
            base.StopProcessing();
            SynchronizedInvoke.InvokeIfRequired(PSHostCallBack.ParentControl, () => PSHostCallBack.ParentControl.EndGridEdit());

            base.EndProcessing();
        }
    }

    [Cmdlet(VerbsCommon.Show,"HostGridView", DefaultParameterSetName = "PassThru", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113364")]
    public class HostGridView : PSCmdlet
    {
        List<PSObject> FullList = null;

        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject
        { get; set; }

        [Parameter, ValidateNotNullOrEmpty]
        public string Title
        { get; set; }

        [Parameter(ParameterSetName = "Wait")]
        public SwitchParameter Wait
        { get; set; }

        [Parameter(ParameterSetName = "OutputMode")]
        public OutputModeOption OutputMode 
        { get; set; }

        [Parameter()]
        public SwitchParameter PassThru
        {
            get
            {
                if (this.OutputMode != OutputModeOption.Multiple)
                    return new SwitchParameter(false);
                return new SwitchParameter(true);
            }
            set
            {
                this.OutputMode = (value.IsPresent ? OutputModeOption.Multiple : OutputModeOption.None);
            }
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            FullList = new List<PSObject>();
        }

        protected override void ProcessRecord()
        {
            if (this.InputObject == null)
                return;

            if (this.InputObject.BaseObject.GetType().IsArray)
            {
                foreach (object element in (object[])this.InputObject.BaseObject)
                    FullList.Add(new PSObject(element));
            }
            else if (this.InputObject.BaseObject is IDictionary)
            {
                foreach (object element in (IDictionary)this.InputObject.BaseObject)
                    FullList.Add(new PSObject(element));
            }
            else
            {
                FullList.Add(this.InputObject);
            }
        }

        protected override void EndProcessing()
        {
            object SendObj = null;
            bool isScalar = false;
            List<System.Management.Automation.TableControlColumn> TableColl = null;

            base.EndProcessing();
            if ( FullList.Count == 0)
                return; //nothing to do.

            ////////////////////////////////////////////////////////////////
            // Generate objects for call

            if ( ScalarTypes.Contains( FullList[0].TypeNames[0] ) )
            {
                isScalar = true;
                SendObj = new DataTable();
                ((DataTable)SendObj).Columns.Add(FullList[0].TypeNames[0],FullList[0].BaseObject.GetType());
                foreach ( PSObject  Item in FullList)
                    ((DataTable)SendObj).Rows.Add( Item );
            }
            else if (HasObjectSpecialForammting(FullList[0], out SendObj, out TableColl))
            {
                foreach (PSObject Item in FullList)
                {
                    SessionState.PSVariable.Set("MyOutGridViewDataItem", Item);
                    object[] values = new object[((DataTable)SendObj).Columns.Count];
                    for ( int i = 0 ; i < TableColl.Count; i++ )
                    {
                        if (TableColl[i].DisplayEntry.ValueType == DisplayEntryValueType.Property)
                        {
                            if (Item.Properties[TableColl[i].DisplayEntry.Value] != null )
                                values[i] = Item.Properties[TableColl[i].DisplayEntry.Value].Value;
                        }
                        else
                        {
                            Collection<PSObject> results = base.InvokeCommand.InvokeScript("$MyOutGridViewDataItem | foreach-object -process { " + TableColl[i].DisplayEntry.Value + " }");
                            if (results.Count > 0)
                                values[i] = results.First();
                        }
                    }

                    ((DataTable)SendObj).Rows.Add(values);
                }
            }
            else
            {
                SendObj = new ArrayList();
                ((ArrayList)SendObj).AddRange(FullList);  // XxX Verify - Might be FUllList.BaseObject
            }

            ////////////////////////////////////////////////////////////////

            // Display List
            SynchronizedInvoke.InvokeIfRequired(PSHostCallBack.ParentControl, () => PSHostCallBack.ParentControl.FinishedGridView(SendObj, FullList.Count,
                this.Wait || this.OutputMode != OutputModeOption.None, this.OutputMode == OutputModeOption.Multiple, !isScalar));


            if (!this.Wait && this.OutputMode == OutputModeOption.None)
                return; // Done do not wait for output...

            List<int> selectedItems = null;
            if (PSHostCallBack.ParentControl.WaitForResult(out selectedItems))
                if (selectedItems != null && selectedItems.Count > 0 && this.OutputMode != OutputModeOption.None)
                {
                    Trace.TraceInformation("Out-GridView() Complete Count: {0}", selectedItems.Count);
                    foreach (int Row in selectedItems)
                        if ( Row >=0 && Row < FullList.Count)
                            base.WriteObject(FullList[Row]);
                }

            // Cleanup
            base.StopProcessing();
            SynchronizedInvoke.InvokeIfRequired(PSHostCallBack.ParentControl, () => PSHostCallBack.ParentControl.EndGridView());
            FullList = null;
        }


        private bool HasObjectSpecialForammting (PSObject Sample, out object NewTable, out List<System.Management.Automation.TableControlColumn> TableColl )
        {
            NewTable = null;
            TableColl = null;
            foreach (PSObject result in base.InvokeCommand.InvokeScript("Get-FormatData -TypeName '" + Sample.TypeNames[0] + "'"))
            {
                if (result != null && result.BaseObject.GetType() == typeof(System.Management.Automation.ExtendedTypeDefinition))
                {
                    foreach (var child in ((System.Management.Automation.ExtendedTypeDefinition)result.BaseObject).FormatViewDefinition)
                    {
                        if (child.Control.GetType() == typeof(System.Management.Automation.TableControl))
                        {
                            NewTable = new DataTable();
                            TableColl = ((System.Management.Automation.TableControl)child.Control).Rows[0].Columns;
                            for (int i = 0; i < ((System.Management.Automation.TableControl)child.Control).Headers.Count && i < ((System.Management.Automation.TableControl)child.Control).Rows[0].Columns.Count; i++)
                            {
                                if (!string.IsNullOrEmpty(((System.Management.Automation.TableControl)child.Control).Headers[i].Label))
                                    ((DataTable)NewTable).Columns.Add(((System.Management.Automation.TableControl)child.Control).Headers[i].Label);
                                else if (!string.IsNullOrEmpty(((System.Management.Automation.TableControl)child.Control).Rows[0].Columns[i].DisplayEntry.Value))
                                    ((DataTable)NewTable).Columns.Add(((System.Management.Automation.TableControl)child.Control).Rows[0].Columns[i].DisplayEntry.Value);
                            }
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private List<string> ScalarTypes = new List<string>() { "System.String", "System.SByte", "System.Byte", "System.Int16", "System.UInt16",
            "System.Int32", "System.UInt32", "System.Int64", "System.UInt64", "System.Char", "System.Single", "System.Double", "System.Boolean",
            "System.Decimal", "System.IntPtr", "System.Security.SecureString", "System.Numerics.BigInteger" };

    }
}

