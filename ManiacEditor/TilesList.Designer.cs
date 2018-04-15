namespace ManiacEditor
{
    partial class TilesList
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.vScrollBar1 = new System.Windows.Forms.VScrollBar();
            this.panel1 = new System.Windows.Forms.Panel();
            this.graphicPanel = new ManiacEditor.DevicePanel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.tabberTileAttributes = new System.Windows.Forms.TabControl();
            this.tabNormalMagic = new System.Windows.Forms.TabPage();
            this.tacNormal = new ManiacEditor.TileAttributesControl();
            this.tabAlternateMagic = new System.Windows.Forms.TabPage();
            this.tacAlternate = new ManiacEditor.TileAttributesControl();
            this.panel1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tabberTileAttributes.SuspendLayout();
            this.tabNormalMagic.SuspendLayout();
            this.tabAlternateMagic.SuspendLayout();
            this.SuspendLayout();
            // 
            // vScrollBar1
            // 
            this.vScrollBar1.Dock = System.Windows.Forms.DockStyle.Right;
            this.vScrollBar1.Location = new System.Drawing.Point(123, 0);
            this.vScrollBar1.Name = "vScrollBar1";
            this.vScrollBar1.Size = new System.Drawing.Size(17, 233);
            this.vScrollBar1.TabIndex = 0;
            this.vScrollBar1.ValueChanged += new System.EventHandler(this.vScrollBar1_ValueChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.graphicPanel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(123, 233);
            this.panel1.TabIndex = 2;
            this.panel1.Resize += new System.EventHandler(this.panel1_Resize);
            // 
            // graphicPanel
            // 
            this.graphicPanel.AllowDrop = true;
            this.graphicPanel.DeviceBackColor = System.Drawing.Color.White;
            this.graphicPanel.Location = new System.Drawing.Point(0, 0);
            this.graphicPanel.Name = "graphicPanel";
            this.graphicPanel.Size = new System.Drawing.Size(126, 146);
            this.graphicPanel.TabIndex = 2;
            this.graphicPanel.OnRender += new ManiacEditor.RenderEventHandler(this.graphicPanel_OnRender);
            this.graphicPanel.DragEnter += new System.Windows.Forms.DragEventHandler(this.graphicPanel_DragEnter);
            this.graphicPanel.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.graphicPanel_MouseDoubleClick);
            this.graphicPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.graphicPanel_MouseDown);
            this.graphicPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.graphicPanel_MouseMove);
            this.graphicPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.graphicPanel_MouseUp);
            this.graphicPanel.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.graphicPanel_MouseWheel);
            this.graphicPanel.Resize += new System.EventHandler(this.graphicPanel_Resize);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.panel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tabberTileAttributes, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(146, 349);
            this.tableLayoutPanel1.TabIndex = 3;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.panel1);
            this.panel2.Controls.Add(this.vScrollBar1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(3, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(140, 233);
            this.panel2.TabIndex = 0;
            // 
            // tabberTileAttributes
            // 
            this.tabberTileAttributes.Controls.Add(this.tabNormalMagic);
            this.tabberTileAttributes.Controls.Add(this.tabAlternateMagic);
            this.tabberTileAttributes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabberTileAttributes.Location = new System.Drawing.Point(3, 242);
            this.tabberTileAttributes.Name = "tabberTileAttributes";
            this.tabberTileAttributes.SelectedIndex = 0;
            this.tabberTileAttributes.Size = new System.Drawing.Size(140, 104);
            this.tabberTileAttributes.TabIndex = 1;
            // 
            // tabNormalMagic
            // 
            this.tabNormalMagic.Controls.Add(this.tacNormal);
            this.tabNormalMagic.Location = new System.Drawing.Point(4, 22);
            this.tabNormalMagic.Name = "tabNormalMagic";
            this.tabNormalMagic.Padding = new System.Windows.Forms.Padding(3);
            this.tabNormalMagic.Size = new System.Drawing.Size(132, 78);
            this.tabNormalMagic.TabIndex = 0;
            this.tabNormalMagic.Text = "Normal";
            this.tabNormalMagic.UseVisualStyleBackColor = true;
            // 
            // tacNormal
            // 
            this.tacNormal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tacNormal.Location = new System.Drawing.Point(3, 3);
            this.tacNormal.Name = "tacNormal";
            this.tacNormal.Size = new System.Drawing.Size(126, 72);
            this.tacNormal.TabIndex = 0;
            this.tacNormal.Value = null;
            // 
            // tabAlternateMagic
            // 
            this.tabAlternateMagic.Controls.Add(this.tacAlternate);
            this.tabAlternateMagic.Location = new System.Drawing.Point(4, 22);
            this.tabAlternateMagic.Name = "tabAlternateMagic";
            this.tabAlternateMagic.Padding = new System.Windows.Forms.Padding(3);
            this.tabAlternateMagic.Size = new System.Drawing.Size(132, 80);
            this.tabAlternateMagic.TabIndex = 1;
            this.tabAlternateMagic.Text = "Alternate";
            this.tabAlternateMagic.UseVisualStyleBackColor = true;
            // 
            // tacAlternate
            // 
            this.tacAlternate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tacAlternate.Location = new System.Drawing.Point(3, 3);
            this.tacAlternate.Name = "tacAlternate";
            this.tacAlternate.Size = new System.Drawing.Size(126, 74);
            this.tacAlternate.TabIndex = 0;
            this.tacAlternate.Value = null;
            // 
            // TilesList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "TilesList";
            this.Size = new System.Drawing.Size(146, 349);
            this.panel1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.tabberTileAttributes.ResumeLayout(false);
            this.tabNormalMagic.ResumeLayout(false);
            this.tabAlternateMagic.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.VScrollBar vScrollBar1;
        private System.Windows.Forms.Panel panel1;
        private DevicePanel graphicPanel;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TabControl tabberTileAttributes;
        private System.Windows.Forms.TabPage tabNormalMagic;
        private System.Windows.Forms.TabPage tabAlternateMagic;
        private TileAttributesControl tacNormal;
        private TileAttributesControl tacAlternate;
    }
}
