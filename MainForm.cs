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

using IFR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using static IFR.IFRHelper;
using static IfrViewer.HpkParser;

namespace IfrViewer
{
    public partial class MainForm : Form
    {
        private const string EmptyDetails = "No data available";

        public MainForm()
        {
            InitializeComponent();
            IFRHelper.log = log; // Use local window as logging window
        }

        #region GUI

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Update Version
            Text += " - v" + Application.ProductVersion + " (UEFI 2.6)";

            // Set size of window to 80 percent by default
            MainForm_SizeChanged(sender, e);
            Width = (int)(Screen.GetWorkingArea(DesktopLocation).Width * 0.80);
            Height = (int)(Screen.GetWorkingArea(DesktopLocation).Height * 0.80);
            CenterToScreen();
            Show();

            // Load project from command line argument (when available)
            string[] Files = new string[Environment.GetCommandLineArgs().Length - 1];
            for (int i = 0; i < Environment.GetCommandLineArgs().Length - 1; i++)
                Files[i] = Environment.GetCommandLineArgs()[i+1];
            LoadFiles(Files);

            if (tv_tree.Nodes.Count == 0)
                tv_details.Nodes.Add(EmptyDetails);
        }

        private void CreateLogEntryMain(LogSeverity severity, string msg)
        {
            CreateLogEntry(severity, "Main", msg);

            log.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            log.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
            log.FirstDisplayedScrollingRowIndex = log.Rows[log.RowCount - 1].Index; // Scroll to bottom

            Update();
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            // Check if window got minimized then stop changing sizes!
            // When form gets maximized (means ClientSize changes back to normal) then SizeChanged event doesn't get fired   (;ﾟ︵ﾟ;)
            if ((ClientSize.Width == 0) || (ClientSize.Height == 0))
                return;

            splitContainer1.Width = ClientSize.Width - 24;
            splitContainer1.Height = ClientSize.Height - 24;
            splitContainer1_SplitterMoved(sender, null);
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            tabControl1_SizeChanged(null, e);
            tabControl1.Width = splitContainer1.Panel1.Width - 9;
            tabControl1.Height = splitContainer1.Panel1.Height - 6;
            splitContainer2.Width = splitContainer1.Panel2.Width - 6;
            splitContainer2.Height = splitContainer1.Panel2.Height - 6;
            splitContainer2_SplitterMoved(sender, null);
        }

        private void splitContainer2_SplitterMoved(object sender, SplitterEventArgs e)
        {
            tv_details.Width = splitContainer2.Panel1.Width - 6;
            tv_details.Height = splitContainer2.Panel1.Height - 6;
            log.Width = splitContainer2.Panel2.Width - 6;
            log.Height = splitContainer2.Panel2.Height - 6;
        }

        private void tabControl1_SizeChanged(object sender, EventArgs e)
        {
            tv_tree.Width = tv_tree.Parent.Width;
            tv_tree.Height = tv_tree.Parent.Height;
            tv_logical.Width = tv_tree.Parent.Width;
            tv_logical.Height = tv_tree.Parent.Height;
        }

        private TreeNode AddTreeNode(TreeNode root, string text, object obj)
        {
            TreeNode leaf = root.Nodes.Add(text);
            leaf.Tag = obj;
            return leaf;
        }

        private void tv_tree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag == null)
            {
                // should not happen because every node should have an object bound!
                ShowAtDetails(null);
                CreateLogEntryMain(LogSeverity.WARNING, "No data found for \"" + e.Node.ToString() + "\"!");
            }
            else
            {
                ShowAtDetails((HPKElement)e.Node.Tag);
            }
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] DroppedPathList = (string[])e.Data.GetData(DataFormats.FileDrop);
            List<string> DroppedFiles = new List<string>();

            // get all files of the dropped object(s) and add them..
            foreach (string path in DroppedPathList)
            {
                if (Directory.Exists(path))
                    DroppedFiles.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
                else if (File.Exists(path))
                    DroppedFiles.Add(path);
            }
            LoadFiles(DroppedFiles.ToArray());
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Link; // Allow dopping files
        }

        #endregion

        List<HiiPackageBase> Packages = new List<HiiPackageBase>();

        private void LoadFiles(string[] filepaths)
        {
            // Load HPKs into memory and build tree view
            foreach (string filename in filepaths)
            {
                CreateLogEntryMain(LogSeverity.INFO, "Loading file \"" + filename + "\"...");

                HPKfile hpk = null;
                try
                {
                    hpk = new HPKfile(filename);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    CreateLogEntryMain(LogSeverity.ERROR, "Loading file failed!" + Environment.NewLine + ex.ToString());
                    MessageBox.Show("Loading file failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (null != hpk) // Loaded successfully?
                {
                    tv_tree.BeginUpdate();
                    TreeNode root = tv_tree.Nodes.Add(hpk.Name);
                    root.Tag = hpk;
                    ShowAtRawTree(hpk, root);
                    root.Expand();
                    tv_tree.EndUpdate();

                    // Collect all new packages of this file
                    foreach (HiiPackageBase hpkpkg in hpk.Childs)
                        Packages.Add(hpkpkg);

                    CreateLogEntryMain(LogSeverity.SUCCESS, "Loading file \"" + filename + "\" completed!");
                }
            }

            ParsedHpkContainer ParsedHpkContainer = new ParsedHpkContainer(Packages);

            // Wipe existing data because new loaded HPK may provide any missed data
            tv_logical.Nodes.Clear();

            // Since HPKs interact with each other, build logical tree after loading is completely done
            foreach (ParsedHpkContainer.ParsedHpkNode pkg in ParsedHpkContainer.HpkPackages)
            {
                CreateLogEntryMain(LogSeverity.INFO, "Parsing package \"" + pkg.Name + "\" ...");

                tv_logical.BeginUpdate();
                TreeNode root = tv_logical.Nodes.Add(pkg.Name);
                root.Tag = pkg.Origin;
                ShowAtLogicalTree(pkg, root);
                root.Expand();
                tv_logical.EndUpdate();

                CreateLogEntryMain(LogSeverity.SUCCESS, "Parsing package \"" + pkg.Name + "\" completed!");
            }
        }
 
        private void ShowAtRawTree(HPKElement elem, TreeNode root)
        {
            // add all child elements to the tree..
            if (elem.Childs.Count > 0)
            {
                foreach (HPKElement child in elem.Childs)
                {
                    ShowAtRawTree(child, AddTreeNode(root, child.Name + " [" + child.UniqueID + "]", child));
                }
                root.Expand();
            }
        }
 
        private void ShowAtLogicalTree(ParsedHpkContainer.ParsedHpkNode node, TreeNode root)
        {
            // add all child elements to the tree..
            if (node.Childs.Count > 0)
            {
                foreach (ParsedHpkContainer.ParsedHpkNode child in node.Childs)
                {
                    ShowAtLogicalTree(child, AddTreeNode(root, child.Name, child.Origin));
                }
                root.Expand();
            }
        }

        private void ShowAtDetails(HPKElement elem)
        {
            Cursor previousCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            tv_details.BeginUpdate();

            tv_details.Nodes.Clear();
            if (elem == null)
            {
                // should not happen because every node should have an objected bound!
                tv_details.Nodes.Add(EmptyDetails);
            }
            else
            {
                const uint BytesPerLine = 16;

                // add common elements..
                tv_details.Nodes.Add("UniqueID = " + elem.UniqueID);

                // add all header fields to the tree..
                byte[] HeaderRaw = elem.HeaderRaw;
                if ((elem.Header != null) || (HeaderRaw != null))
                {
                    TreeNode branch = tv_details.Nodes.Add("Header");
                    // handle raw..
                    if (HeaderRaw != null)
                    {
                        TreeNode leaf = branch.Nodes.Add("__RAW");
                        foreach (string line in HeaderRaw.HexDump(BytesPerLine).Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                            leaf.Nodes.Add(line);
                    }
                    // handle managed..
                    if (elem.Header != null)
                    {
                        foreach (System.Collections.Generic.KeyValuePair<string, object> pair in elem.GetPrintableHeader(BytesPerLine))
                        {
                            branch.Nodes.Add(pair.Key + " = " + pair.Value.ToString());
                        }
                    }
                    //branch.Expand();
                }
                // add all payload fields to the tree..
                byte[] PayloadRaw = elem.PayloadRaw;
                if ((elem.Payload != null) || (PayloadRaw != null))
                {
                    TreeNode branch = tv_details.Nodes.Add("Payload");
                    // handle raw..
                    if (PayloadRaw != null)
                    {
                        TreeNode leaf = branch.Nodes.Add("__RAW");
                        foreach (string line in PayloadRaw.HexDump(BytesPerLine).Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                            leaf.Nodes.Add(line);
                    }
                    // handle managed..
                    if (elem.Payload != null)
                    {
                        foreach (System.Collections.Generic.KeyValuePair<string, object> pair in elem.GetPrintablePayload(BytesPerLine))
                        {
                            branch.Nodes.Add(pair.Key + " = " + pair.Value.ToString());
                        }
                    }
                    //branch.Expand();
                }
                tv_details.ExpandAll();
            }

            tv_details.EndUpdate();
            Cursor.Current = previousCursor;
        }

        private void tv_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TreeView)
            {
                TreeView tv = (TreeView)sender;
                if (e.KeyData == (Keys.Control | Keys.C))
                {
                    if (tv.SelectedNode != null)
                    {
                        Clipboard.SetText(tv.SelectedNode.Text);
                    }
                    e.SuppressKeyPress = true;
                }
            }
        }
    }

    static class IfrViewer
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
