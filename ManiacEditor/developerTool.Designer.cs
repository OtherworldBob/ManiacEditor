namespace ManiacEditor
{
    partial class DeveloperTerminal
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
            this.components = new System.ComponentModel.Container();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.add = new System.Windows.Forms.Button();
            this.subtract = new System.Windows.Forms.Button();
            this.add10 = new System.Windows.Forms.Button();
            this.subtract10 = new System.Windows.Forms.Button();
            this.manualEntryText = new System.Windows.Forms.TextBox();
            this.currentLabel = new System.Windows.Forms.Label();
            this.manualLabel = new System.Windows.Forms.Label();
            this.enterManualEntry = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.errorLabel = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // add
            // 
            this.add.Location = new System.Drawing.Point(82, 37);
            this.add.Name = "add";
            this.add.Size = new System.Drawing.Size(21, 23);
            this.add.TabIndex = 1;
            this.add.Text = "+";
            this.add.UseVisualStyleBackColor = true;
            this.add.Click += new System.EventHandler(this.add_Click);
            // 
            // subtract
            // 
            this.subtract.Location = new System.Drawing.Point(55, 37);
            this.subtract.Name = "subtract";
            this.subtract.Size = new System.Drawing.Size(21, 23);
            this.subtract.TabIndex = 2;
            this.subtract.Text = "-";
            this.subtract.UseVisualStyleBackColor = true;
            this.subtract.Click += new System.EventHandler(this.subtract_Click);
            // 
            // add10
            // 
            this.add10.Location = new System.Drawing.Point(109, 37);
            this.add10.Name = "add10";
            this.add10.Size = new System.Drawing.Size(40, 23);
            this.add10.TabIndex = 3;
            this.add10.Text = "+10";
            this.add10.UseVisualStyleBackColor = true;
            this.add10.Click += new System.EventHandler(this.add10_Click);
            // 
            // subtract10
            // 
            this.subtract10.Location = new System.Drawing.Point(9, 37);
            this.subtract10.Name = "subtract10";
            this.subtract10.Size = new System.Drawing.Size(40, 23);
            this.subtract10.TabIndex = 4;
            this.subtract10.Text = "-10";
            this.subtract10.UseVisualStyleBackColor = true;
            this.subtract10.Click += new System.EventHandler(this.subtract10_Click);
            // 
            // manualEntryText
            // 
            this.manualEntryText.Location = new System.Drawing.Point(9, 88);
            this.manualEntryText.Name = "manualEntryText";
            this.manualEntryText.Size = new System.Drawing.Size(94, 20);
            this.manualEntryText.TabIndex = 5;
            // 
            // currentLabel
            // 
            this.currentLabel.AutoSize = true;
            this.currentLabel.Location = new System.Drawing.Point(6, 21);
            this.currentLabel.Name = "currentLabel";
            this.currentLabel.Size = new System.Drawing.Size(74, 13);
            this.currentLabel.TabIndex = 6;
            this.currentLabel.Text = "Current Value:";
            // 
            // manualLabel
            // 
            this.manualLabel.AutoSize = true;
            this.manualLabel.Location = new System.Drawing.Point(6, 72);
            this.manualLabel.Name = "manualLabel";
            this.manualLabel.Size = new System.Drawing.Size(72, 13);
            this.manualLabel.TabIndex = 7;
            this.manualLabel.Text = "Manual Entry:";
            // 
            // enterManualEntry
            // 
            this.enterManualEntry.Location = new System.Drawing.Point(109, 88);
            this.enterManualEntry.Name = "enterManualEntry";
            this.enterManualEntry.Size = new System.Drawing.Size(40, 23);
            this.enterManualEntry.TabIndex = 8;
            this.enterManualEntry.Text = "GO!";
            this.enterManualEntry.UseVisualStyleBackColor = true;
            this.enterManualEntry.Click += new System.EventHandler(this.enterManualEntry_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.errorLabel);
            this.groupBox1.Controls.Add(this.currentLabel);
            this.groupBox1.Controls.Add(this.enterManualEntry);
            this.groupBox1.Controls.Add(this.add);
            this.groupBox1.Controls.Add(this.manualLabel);
            this.groupBox1.Controls.Add(this.subtract);
            this.groupBox1.Controls.Add(this.add10);
            this.groupBox1.Controls.Add(this.manualEntryText);
            this.groupBox1.Controls.Add(this.subtract10);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 145);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Value Incrementer";
            // 
            // errorLabel
            // 
            this.errorLabel.AutoSize = true;
            this.errorLabel.Location = new System.Drawing.Point(0, 129);
            this.errorLabel.Name = "errorLabel";
            this.errorLabel.Size = new System.Drawing.Size(0, 13);
            this.errorLabel.TabIndex = 9;
            // 
            // DeveloperTerminal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(633, 340);
            this.Controls.Add(this.groupBox1);
            this.Name = "DeveloperTerminal";
            this.Text = "Developer Terminal";
            this.TopMost = true;
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.Button add;
        private System.Windows.Forms.Button subtract;
        private System.Windows.Forms.Button add10;
        private System.Windows.Forms.Button subtract10;
        private System.Windows.Forms.TextBox manualEntryText;
        private System.Windows.Forms.Label currentLabel;
        private System.Windows.Forms.Label manualLabel;
        private System.Windows.Forms.Button enterManualEntry;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label errorLabel;
    }
}