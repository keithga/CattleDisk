namespace PowerShell_Wizard_Host
{
    partial class PSWizHost
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PSWizHost));
            this.LayoutGlobal = new System.Windows.Forms.TableLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.LabelComputer = new System.Windows.Forms.Label();
            this.LabelTitle = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.ButtonC = new System.Windows.Forms.Button();
            this.ButtonN = new System.Windows.Forms.Button();
            this.ButtonB = new System.Windows.Forms.Button();
            this.panel5 = new System.Windows.Forms.Panel();
            this.LayoutStatus = new System.Windows.Forms.FlowLayoutPanel();
            this.StatusPrevious = new System.Windows.Forms.Label();
            this.StatusCurrent = new System.Windows.Forms.Label();
            this.panel6 = new System.Windows.Forms.Panel();
            this.PowershellHostControl1 = new PowerShell_Wizard_Host.PSHostControl();
            this.LayoutGlobal.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel4.SuspendLayout();
            this.LayoutStatus.SuspendLayout();
            this.panel6.SuspendLayout();
            this.SuspendLayout();
            // 
            // LayoutGlobal
            // 
            this.LayoutGlobal.AutoSize = true;
            this.LayoutGlobal.ColumnCount = 3;
            this.LayoutGlobal.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.LayoutGlobal.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 1F));
            this.LayoutGlobal.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.LayoutGlobal.Controls.Add(this.panel1, 0, 0);
            this.LayoutGlobal.Controls.Add(this.panel2, 0, 2);
            this.LayoutGlobal.Controls.Add(this.panel3, 0, 3);
            this.LayoutGlobal.Controls.Add(this.panel5, 1, 1);
            this.LayoutGlobal.Controls.Add(this.LayoutStatus, 0, 1);
            this.LayoutGlobal.Controls.Add(this.panel6, 2, 1);
            this.LayoutGlobal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LayoutGlobal.Location = new System.Drawing.Point(0, 0);
            this.LayoutGlobal.Margin = new System.Windows.Forms.Padding(0);
            this.LayoutGlobal.Name = "LayoutGlobal";
            this.LayoutGlobal.RowCount = 4;
            this.LayoutGlobal.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 75F));
            this.LayoutGlobal.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.LayoutGlobal.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 1F));
            this.LayoutGlobal.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.LayoutGlobal.Size = new System.Drawing.Size(784, 537);
            this.LayoutGlobal.TabIndex = 1;
            // 
            // panel1
            // 
            this.LayoutGlobal.SetColumnSpan(this.panel1, 3);
            this.panel1.Controls.Add(this.LabelComputer);
            this.panel1.Controls.Add(this.LabelTitle);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(20);
            this.panel1.Size = new System.Drawing.Size(778, 69);
            this.panel1.TabIndex = 0;
            // 
            // LabelComputer
            // 
            this.LabelComputer.AutoSize = true;
            this.LabelComputer.Dock = System.Windows.Forms.DockStyle.Right;
            this.LabelComputer.Font = new System.Drawing.Font("Segoe UI Light", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel, ((byte)(0)));
            this.LabelComputer.Location = new System.Drawing.Point(659, 20);
            this.LabelComputer.Name = "LabelComputer";
            this.LabelComputer.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.LabelComputer.Size = new System.Drawing.Size(99, 24);
            this.LabelComputer.TabIndex = 1;
            this.LabelComputer.Text = "DESTINATION SERVER\r\nLOCALHOST";
            // 
            // LabelTitle
            // 
            this.LabelTitle.AutoSize = true;
            this.LabelTitle.Font = new System.Drawing.Font("Segoe UI Light", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel, ((byte)(0)));
            this.LabelTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(115)))), ((int)(((byte)(148)))));
            this.LabelTitle.Location = new System.Drawing.Point(20, 20);
            this.LabelTitle.Name = "LabelTitle";
            this.LabelTitle.Size = new System.Drawing.Size(113, 32);
            this.LabelTitle.TabIndex = 0;
            this.LabelTitle.Text = "Working...";
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.ControlLight;
            this.LayoutGlobal.SetColumnSpan(this.panel2, 3);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 496);
            this.panel2.Margin = new System.Windows.Forms.Padding(0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(784, 1);
            this.panel2.TabIndex = 1;
            // 
            // panel3
            // 
            this.panel3.AutoSize = true;
            this.panel3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.LayoutGlobal.SetColumnSpan(this.panel3, 3);
            this.panel3.Controls.Add(this.panel4);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(0, 497);
            this.panel3.Margin = new System.Windows.Forms.Padding(0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(784, 40);
            this.panel3.TabIndex = 2;
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.ButtonC);
            this.panel4.Controls.Add(this.ButtonN);
            this.panel4.Controls.Add(this.ButtonB);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel4.Location = new System.Drawing.Point(503, 0);
            this.panel4.Margin = new System.Windows.Forms.Padding(0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(281, 40);
            this.panel4.TabIndex = 0;
            // 
            // ButtonC
            // 
            this.ButtonC.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ButtonC.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ButtonC.Location = new System.Drawing.Point(185, 8);
            this.ButtonC.Name = "ButtonC";
            this.ButtonC.Size = new System.Drawing.Size(75, 23);
            this.ButtonC.TabIndex = 4;
            this.ButtonC.Text = "Cancel";
            this.ButtonC.UseVisualStyleBackColor = true;
            this.ButtonC.Click += new System.EventHandler(this.ButtonC_Click);
            // 
            // ButtonN
            // 
            this.ButtonN.Enabled = false;
            this.ButtonN.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ButtonN.Location = new System.Drawing.Point(90, 8);
            this.ButtonN.Name = "ButtonN";
            this.ButtonN.Size = new System.Drawing.Size(75, 23);
            this.ButtonN.TabIndex = 3;
            this.ButtonN.Text = "Next";
            this.ButtonN.UseVisualStyleBackColor = true;
            this.ButtonN.Click += new System.EventHandler(this.ButtonN_Click);
            // 
            // ButtonB
            // 
            this.ButtonB.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ButtonB.Location = new System.Drawing.Point(12, 8);
            this.ButtonB.Name = "ButtonB";
            this.ButtonB.Size = new System.Drawing.Size(72, 23);
            this.ButtonB.TabIndex = 0;
            this.ButtonB.Text = "Back";
            this.ButtonB.UseVisualStyleBackColor = true;
            this.ButtonB.Visible = false;
            this.ButtonB.Click += new System.EventHandler(this.ButtonN_Click);
            // 
            // panel5
            // 
            this.panel5.BackColor = System.Drawing.SystemColors.ControlLight;
            this.panel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel5.Location = new System.Drawing.Point(200, 80);
            this.panel5.Margin = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(1, 411);
            this.panel5.TabIndex = 3;
            // 
            // LayoutStatus
            // 
            this.LayoutStatus.Controls.Add(this.StatusPrevious);
            this.LayoutStatus.Controls.Add(this.StatusCurrent);
            this.LayoutStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LayoutStatus.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.LayoutStatus.Location = new System.Drawing.Point(15, 78);
            this.LayoutStatus.Margin = new System.Windows.Forms.Padding(15, 3, 3, 3);
            this.LayoutStatus.Name = "LayoutStatus";
            this.LayoutStatus.Size = new System.Drawing.Size(182, 415);
            this.LayoutStatus.TabIndex = 4;
            // 
            // StatusPrevious
            // 
            this.StatusPrevious.AutoSize = true;
            this.StatusPrevious.Font = new System.Drawing.Font("Segoe UI Light", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.StatusPrevious.Location = new System.Drawing.Point(3, 0);
            this.StatusPrevious.Name = "StatusPrevious";
            this.StatusPrevious.Padding = new System.Windows.Forms.Padding(2);
            this.StatusPrevious.Size = new System.Drawing.Size(114, 42);
            this.StatusPrevious.TabIndex = 0;
            this.StatusPrevious.Text = "Before you begin\r\nWelcome";
            this.StatusPrevious.Visible = false;
            // 
            // StatusCurrent
            // 
            this.StatusCurrent.AutoSize = true;
            this.StatusCurrent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(114)))), ((int)(((byte)(188)))));
            this.StatusCurrent.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.StatusCurrent.ForeColor = System.Drawing.SystemColors.Window;
            this.StatusCurrent.Location = new System.Drawing.Point(3, 42);
            this.StatusCurrent.MinimumSize = new System.Drawing.Size(200, 20);
            this.StatusCurrent.Name = "StatusCurrent";
            this.StatusCurrent.Padding = new System.Windows.Forms.Padding(3);
            this.StatusCurrent.Size = new System.Drawing.Size(200, 25);
            this.StatusCurrent.TabIndex = 1;
            this.StatusCurrent.Text = "Working...";
            // 
            // panel6
            // 
            this.panel6.AutoScroll = true;
            this.panel6.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panel6.Controls.Add(this.PowershellHostControl1);
            this.panel6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel6.Location = new System.Drawing.Point(204, 78);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(577, 415);
            this.panel6.TabIndex = 5;
            // 
            // PowershellHostControl1
            // 
            this.PowershellHostControl1.AutoSize = true;
            this.PowershellHostControl1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.PowershellHostControl1.ColumnCount = 1;
            this.PowershellHostControl1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.PowershellHostControl1.Dock = System.Windows.Forms.DockStyle.Top;
            this.PowershellHostControl1.Location = new System.Drawing.Point(0, 0);
            this.PowershellHostControl1.Name = "PowershellHostControl1";
            this.PowershellHostControl1.RowCount = 1;
            this.PowershellHostControl1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.PowershellHostControl1.Script = null;
            this.PowershellHostControl1.Size = new System.Drawing.Size(577, 0);
            this.PowershellHostControl1.TabIndex = 6;
            this.PowershellHostControl1.ControlNavigationReady += new System.EventHandler<System.EventArgs>(this.PowershellHostControl1_NextButtonRequest);
            this.PowershellHostControl1.InvocationStateChanged += new System.EventHandler<System.Management.Automation.PSInvocationStateChangedEventArgs>(this.PowershellHostControl1_InvocationStateChanged);
            this.PowershellHostControl1.WindowTitleChanged += new System.EventHandler<System.Windows.Forms.LinkClickedEventArgs>(this.PowershellHostControl1_WindowTitleChanged);
            // 
            // PSWizHost
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.CancelButton = this.ButtonC;
            this.CausesValidation = false;
            this.ClientSize = new System.Drawing.Size(784, 537);
            this.Controls.Add(this.LayoutGlobal);
            this.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(600, 400);
            this.Name = "PSWizHost";
            this.Text = "Setup Wizard";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PSWizHost_FormClosing);
            this.Load += new System.EventHandler(this.PSWizHost_Load);
            this.LayoutGlobal.ResumeLayout(false);
            this.LayoutGlobal.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.LayoutStatus.ResumeLayout(false);
            this.LayoutStatus.PerformLayout();
            this.panel6.ResumeLayout(false);
            this.panel6.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel LayoutGlobal;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label LabelComputer;
        private System.Windows.Forms.Label LabelTitle;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Button ButtonC;
        private System.Windows.Forms.Button ButtonN;
        private System.Windows.Forms.Button ButtonB;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.FlowLayoutPanel LayoutStatus;
        private System.Windows.Forms.Label StatusPrevious;
        private System.Windows.Forms.Label StatusCurrent;
        private System.Windows.Forms.Panel panel6;
        private PSHostControl PowershellHostControl1;
    }
}

