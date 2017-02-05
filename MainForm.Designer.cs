//MIT License
//
//Copyright(c) 2016-2017 Peter Kirmeier
//
//Permission Is hereby granted, free Of charge, to any person obtaining a copy
//of this software And associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, And/Or sell
//copies of the Software, And to permit persons to whom the Software Is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice And this permission notice shall be included In all
//copies Or substantial portions of the Software.
//
//THE SOFTWARE Is PROVIDED "AS IS", WITHOUT WARRANTY Of ANY KIND, EXPRESS Or
//IMPLIED, INCLUDING BUT Not LIMITED To THE WARRANTIES Of MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE And NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS Or COPYRIGHT HOLDERS BE LIABLE For ANY CLAIM, DAMAGES Or OTHER
//LIABILITY, WHETHER In AN ACTION Of CONTRACT, TORT Or OTHERWISE, ARISING FROM,
//OUT OF Or IN CONNECTION WITH THE SOFTWARE Or THE USE Or OTHER DEALINGS IN THE
//SOFTWARE.

namespace IfrViewer
{
    partial class MainForm
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            this.tv_tree = new System.Windows.Forms.TreeView();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabrawtree = new System.Windows.Forms.TabPage();
            this.tablogicaltree = new System.Windows.Forms.TabPage();
            this.tv_logical = new System.Windows.Forms.TreeView();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.tv_details = new System.Windows.Forms.TreeView();
            this.log = new System.Windows.Forms.DataGridView();
            this.Type = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Origin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Message = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabrawtree.SuspendLayout();
            this.tablogicaltree.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.log)).BeginInit();
            this.SuspendLayout();
            // 
            // tv_tree
            // 
            this.tv_tree.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tv_tree.FullRowSelect = true;
            this.tv_tree.Location = new System.Drawing.Point(0, 0);
            this.tv_tree.Name = "tv_tree";
            this.tv_tree.Size = new System.Drawing.Size(743, 250);
            this.tv_tree.TabIndex = 0;
            this.tv_tree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tv_tree_AfterSelect);
            this.tv_tree.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tv_KeyDown);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Location = new System.Drawing.Point(12, 12);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tabControl1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(760, 464);
            this.splitContainer1.SplitterDistance = 282;
            this.splitContainer1.SplitterIncrement = 14;
            this.splitContainer1.SplitterWidth = 8;
            this.splitContainer1.TabIndex = 2;
            this.splitContainer1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer1_SplitterMoved);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabrawtree);
            this.tabControl1.Controls.Add(this.tablogicaltree);
            this.tabControl1.Location = new System.Drawing.Point(6, 3);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(751, 276);
            this.tabControl1.TabIndex = 1;
            this.tabControl1.SizeChanged += new System.EventHandler(this.tabControl1_SizeChanged);
            // 
            // tabrawtree
            // 
            this.tabrawtree.Controls.Add(this.tv_tree);
            this.tabrawtree.Location = new System.Drawing.Point(4, 22);
            this.tabrawtree.Name = "tabrawtree";
            this.tabrawtree.Padding = new System.Windows.Forms.Padding(3);
            this.tabrawtree.Size = new System.Drawing.Size(743, 250);
            this.tabrawtree.TabIndex = 0;
            this.tabrawtree.Text = "Raw Tree";
            this.tabrawtree.UseVisualStyleBackColor = true;
            // 
            // tablogicaltree
            // 
            this.tablogicaltree.Controls.Add(this.tv_logical);
            this.tablogicaltree.Location = new System.Drawing.Point(4, 22);
            this.tablogicaltree.Name = "tablogicaltree";
            this.tablogicaltree.Padding = new System.Windows.Forms.Padding(3);
            this.tablogicaltree.Size = new System.Drawing.Size(743, 250);
            this.tablogicaltree.TabIndex = 1;
            this.tablogicaltree.Text = "Logical Tree";
            this.tablogicaltree.UseVisualStyleBackColor = true;
            // 
            // tv_logical
            // 
            this.tv_logical.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tv_logical.FullRowSelect = true;
            this.tv_logical.Location = new System.Drawing.Point(0, 0);
            this.tv_logical.Name = "tv_logical";
            this.tv_logical.Size = new System.Drawing.Size(743, 250);
            this.tv_logical.TabIndex = 1;
            this.tv_logical.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tv_tree_AfterSelect);
            this.tv_logical.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tv_KeyDown);
            // 
            // splitContainer2
            // 
            this.splitContainer2.Location = new System.Drawing.Point(3, 3);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.tv_details);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.log);
            this.splitContainer2.Size = new System.Drawing.Size(754, 168);
            this.splitContainer2.SplitterDistance = 323;
            this.splitContainer2.SplitterWidth = 8;
            this.splitContainer2.TabIndex = 1;
            this.splitContainer2.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer2_SplitterMoved);
            // 
            // tv_details
            // 
            this.tv_details.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tv_details.FullRowSelect = true;
            this.tv_details.Location = new System.Drawing.Point(3, 3);
            this.tv_details.Name = "tv_details";
            this.tv_details.ShowLines = false;
            this.tv_details.ShowRootLines = false;
            this.tv_details.Size = new System.Drawing.Size(317, 162);
            this.tv_details.TabIndex = 1;
            this.tv_details.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tv_KeyDown);
            // 
            // log
            // 
            this.log.AllowUserToAddRows = false;
            this.log.AllowUserToDeleteRows = false;
            this.log.AllowUserToResizeColumns = false;
            this.log.AllowUserToResizeRows = false;
            this.log.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            this.log.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.log.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.log.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Type,
            this.Origin,
            this.Message});
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.log.DefaultCellStyle = dataGridViewCellStyle5;
            this.log.EnableHeadersVisualStyles = false;
            this.log.Location = new System.Drawing.Point(3, 3);
            this.log.Name = "log";
            this.log.ReadOnly = true;
            this.log.RowHeadersVisible = false;
            this.log.ShowEditingIcon = false;
            this.log.Size = new System.Drawing.Size(417, 162);
            this.log.TabIndex = 0;
            // 
            // Type
            // 
            this.Type.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.Type.HeaderText = "Type";
            this.Type.Name = "Type";
            this.Type.ReadOnly = true;
            this.Type.Width = 56;
            // 
            // Origin
            // 
            this.Origin.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.Origin.HeaderText = "Origin";
            this.Origin.Name = "Origin";
            this.Origin.ReadOnly = true;
            this.Origin.Width = 59;
            // 
            // Message
            // 
            this.Message.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.Message.HeaderText = "Message";
            this.Message.Name = "Message";
            this.Message.ReadOnly = true;
            this.Message.Width = 200;
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 488);
            this.Controls.Add(this.splitContainer1);
            this.MinimumSize = new System.Drawing.Size(800, 400);
            this.Name = "MainForm";
            this.Text = "IfrViewer";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.SizeChanged += new System.EventHandler(this.MainForm_SizeChanged);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabrawtree.ResumeLayout(false);
            this.tablogicaltree.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.log)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView tv_tree;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView log;
        private System.Windows.Forms.DataGridViewTextBoxColumn Message;
        private System.Windows.Forms.DataGridViewTextBoxColumn Origin;
        private System.Windows.Forms.DataGridViewTextBoxColumn Type;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TreeView tv_details;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabrawtree;
        private System.Windows.Forms.TabPage tablogicaltree;
        private System.Windows.Forms.TreeView tv_logical;
    }
}

