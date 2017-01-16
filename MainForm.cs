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
using System.IO;
using System.Windows.Forms;
using static IFR.IFRHelper;

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

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Update Version
            Text += " - v" + Application.ProductVersion;

            // Set size of window to 80 percent by default
            MainForm_SizeChanged(sender, e);
            Width = (int)(Screen.GetWorkingArea(DesktopLocation).Width * 0.80);
            Height = (int)(Screen.GetWorkingArea(DesktopLocation).Height * 0.80);
            CenterToScreen();
            Show();

            // Load project from command line argument (when available)
            for (int i = 1; i < Environment.GetCommandLineArgs().Length; i++)
                LoadFiles(new string[1] { Environment.GetCommandLineArgs()[i] });

            if (tv_tree.Nodes.Count == 0)
                tv_details.Nodes.Add(EmptyDetails);
        }

        private void LoadFiles(string[] filepaths)
        {
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
                    LoadHpkElementIntoTree(hpk, root);
                    root.Expand();
                    tv_tree.EndUpdate();
                    CreateLogEntryMain(LogSeverity.SUCCESS, "Loading file \"" + filename + "\" completed!");
                }
            }
        }

        private void CreateLogEntryMain(LogSeverity severity, string msg)
        {
            CreateLogEntry(severity, "Main", msg);

            log.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            log.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
            log.FirstDisplayedScrollingRowIndex = log.Rows[log.RowCount - 1].Index; // Scroll to bottom

            Update();
        }

        private void LoadHpkElementIntoTree(HPKElement elem, TreeNode root)
        {
            // add all child elements to the tree..
            if (elem.Childs.Count > 0)
            {
                foreach (HPKElement child in elem.Childs)
                {
                    LoadHpkElementIntoTree(child, AddTreeNode(root, child.Name + " [" + child.UniqueID + "]", child));
                    root.Expand();
                }
            }
        }

        private TreeNode AddTreeNode(TreeNode root, string text, object obj)
        {
            TreeNode leaf = root.Nodes.Add(text);
            leaf.Tag = obj;
            return leaf;
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
            tv_tree.Width = splitContainer1.Panel1.Width - 6;
            tv_tree.Height = splitContainer1.Panel1.Height - 6;
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

        private void tv_tree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            Cursor previousCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            tv_details.BeginUpdate();

            tv_details.Nodes.Clear();
            if (e.Node.Tag == null)
            {
                // should not happen because every node should have an objected bound!
                tv_details.Nodes.Add(EmptyDetails);
                CreateLogEntryMain(LogSeverity.WARNING, "No data found for \"" + e.Node.ToString() + "\"!");
            }
            else
            {
                HPKElement elem = (HPKElement)e.Node.Tag;
                const uint BytesPerLine = 16;

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

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] DroppedPathList = (string[])e.Data.GetData(DataFormats.FileDrop);

            // get all files of the dropped object(s) and add them..
            foreach (string path in DroppedPathList)
            {
                if (Directory.Exists(path))
                    LoadFiles(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
                else if (File.Exists(path))
                    LoadFiles(new string[1] { path });
            }
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Link; // Allow dopping files
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
