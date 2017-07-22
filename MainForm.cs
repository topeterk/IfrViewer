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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using static IFR.IFRHelper;
using static IfrViewer.HpkParser;

namespace IfrViewer
{
    public partial class MainForm : Form
    {
        private const string EmptyDetails = "No data available";
        private readonly BackgroundWorker DragDropWorker;

        public MainForm()
        {
            InitializeComponent();
            IFRHelper.log = log; // Use local window as logging window
            DragDropWorker = new BackgroundWorker();
            DragDropWorker.DoWork += DragDropWorker_DoWork;
        }

        #region GUI

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Update Version
            Text += " - v" + Application.ProductVersion + " Alpha (UEFI 2.6)";

            // Set size of window to 80 percent by default
            MainForm_SizeChanged(sender, e);
            Width = (int)(Screen.GetWorkingArea(DesktopLocation).Width * 0.80);
            Height = (int)(Screen.GetWorkingArea(DesktopLocation).Height * 0.80);
            CenterToScreen();
            Show();

            // Load project from command line argument (when available)
            List<string> Files = new List<string>();
            Boolean DoTranslate = false;
            Boolean DoHTML = false;
            foreach (string arg in Environment.GetCommandLineArgs().SubArray(1, Environment.GetCommandLineArgs().Length-1))
            {
                if (arg.StartsWith("-")) // is option?
                {
                    if (arg.Equals("-P")) // Start package
                    {
                        if (0 < Files.Count) // Parse previous package before loading next one..
                        {
                            LoadFiles(Files.ToArray()); // Load files of current package
                            Files.Clear();
                        }
                    }
                    else if (arg.StartsWith("-L=")) // Set display language
                    {
                        ts_parse_lang.Text = arg.Substring(3);
                    }
                    else if (arg.Equals("-T")) // Do Translation (Parsing logical tree)
                    {
                        DoTranslate = true;
                    }
                    else if (arg.Equals("-H")) // Do HTML output
                    {
                        DoHTML = true;
                    }
                    else CreateLogEntry(LogSeverity.WARNING, "Main", "Argument unkown \"" + arg + "\"");
                }
                else Files.Add(arg); // argument is a file
            }
            LoadFiles(Files.ToArray()); // Load last package

            if (0 == tv_tree.Nodes.Count)
                tv_details.Nodes.Add(EmptyDetails);

            TreeNode EmptyTree = tv_logical.Nodes.Add("No parsed packages available");
            EmptyTree.Tag = EmptyDetails;

            if (DoTranslate) // Parse logical tree
                parseLogicalViewToolStripMenuItem_Click(null, null);

            if (DoHTML) // Create HTML output
                createHTMLToolStripMenuItem_Click(null, null);
        }

        /// <summary>
        /// Creates a log entry for the "Main" module
        /// </summary>
        /// <param name="severity">Severity of message</param>
        /// <param name="msg">Message string</param>
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
            tabControl1.Width = splitContainer1.Panel1.Width - 9;
            tabControl1.Height = splitContainer1.Panel1.Height - 6;
            tabControl1_SizeChanged(sender, null);
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
            Control ActiveParent = tabControl1.SelectedTab;
            tv_tree.Width = ActiveParent.Width;
            tv_tree.Height = ActiveParent.Height;
            tv_logical.Width = ActiveParent.Width;
            tv_logical.Height = ActiveParent.Height;
        }

        /// <summary>
        /// Adds a text-object pair to a tree
        /// </summary>
        /// <param name="root">Node which the new node is added to</param>
        /// <param name="text">Displayed text of new node</param>
        /// <param name="obj">Object added to node for reference</param>
        /// <returns>New created tree node</returns>
        private TreeNode AddTreeNode(TreeNode root, string text, object obj)
        {
            TreeNode leaf = root.Nodes.Add(text);
            leaf.Tag = obj;
            return leaf;
        }

        /// <summary>
        /// Updates the details window when a tree node gets selected by user
        /// </summary>
        private void tv_tree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            ShowAtDetails(e.Node.Tag, e.Node.Name);
        }

        /// <summary>
        /// Handles drag and drop operation in order to load further files
        /// </summary>
        /// <param name="e">Argument must contain DragEventArgs</param>
        void DragDropWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            DragEventArgs drag_args = (DragEventArgs)e.Argument;
            string[] DroppedPathList = (string[])drag_args.Data.GetData(DataFormats.FileDrop);
            List<string> DroppedFiles = new List<string>();

            // get all files of the dropped object(s) and add them..
            foreach (string path in DroppedPathList)
            {
                if (Directory.Exists(path))
                    DroppedFiles.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
                else if (File.Exists(path))
                    DroppedFiles.Add(path);
            }

            Invoke(Delegate.CreateDelegate(typeof(DragDropFilesFunc), this, "DragDropFiles"), DroppedFiles.ToArray(), drag_args);
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            DragDropWorker.RunWorkerAsync(e);
        }

        private void tv_tree_DragOver(object sender, DragEventArgs e)
        {
            if (null != GetTreeNodeAtPoint(sender, e.X, e.Y)) // Is draging onto a tree node?
                e.Effect = DragDropEffects.Link; // Allow dropping files into existing package
            else
                e.Effect = DragDropEffects.Copy; // Allow dropping files into separate package
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; // Allow dopping files
        }

        private void parseLogicalViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ParseAllFiles(ts_parse_lang.Text);
            tabControl1.SelectedIndex = 1;
        }

        private void createHTMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateHTMLFiles(ts_parse_lang.Text);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO!
        }

        /// <summary>
        /// Retrieves the TreeNode object of a TreeView at a given position
        /// </summary>
        /// <param name="sender">TreeView object to search</param>
        /// <param name="x">Position X</param>
        /// <param name="x">Position Y</param>
        private TreeNode GetTreeNodeAtPoint(object sender, int x, int y)
        {
            if (!(sender is TreeView))
                return null;

            TreeView tv_sender = (TreeView)sender;
            Point pt = tv_sender.PointToClient(new Point(x, y));
            return tv_sender.GetNodeAt(pt);
        }

        private delegate void DragDropFilesFunc(string[] filepaths, DragEventArgs e);
        /// <summary>
        /// Loads a bunch of files via drag and drop
        /// </summary>
        /// <param name="filepaths">List of files to load</param>
        /// <param name="e">Drag and drop event arguments</param>
        private void DragDropFiles(string[] filepaths, DragEventArgs e)
        {
            TreeNode RootNode = null;
            if (DragDropEffects.Link == e.Effect) // object needs to be linked with an existing node?
            {
                RootNode = GetTreeNodeAtPoint(tv_tree, e.X, e.Y);
                while (null != RootNode.Parent) RootNode = RootNode.Parent;
            }
            LoadFiles(filepaths, RootNode);
        }
        #endregion

        /// <summary>
        /// Loads a bunch of files which are referring the same "package"
        /// </summary>
        /// <param name="filepaths">List of files to load</param>
        /// <param name="ParentNode">Optional node the loaded packages are added to</param>
        private void LoadFiles(string[] filepaths, TreeNode ParentNode = null)
        {
            Cursor previousCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            List<HiiPackageBase> Packages = new List<HiiPackageBase>();

            TreeNode PkgNodeRaw = ParentNode;
            if (null == PkgNodeRaw) // use given parent or create it
            {
                PkgNodeRaw = tv_tree.Nodes.Add("Package");
                PkgNodeRaw.Tag = EmptyDetails;
            }

            CreateLogEntryMain(LogSeverity.INFO, "Loading files...");

            // Load HPKs into memory and build tree view
            foreach (string filename in filepaths)
            {
                CreateLogEntryMain(LogSeverity.INFO, "Loading file \"" + filename + "\" ...");

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
                    TreeNode root = PkgNodeRaw.Nodes.Add(hpk.Name);
                    root.Tag = hpk;
                    ShowAtRawTree(hpk, root);
                    root.Expand();
                    tv_tree.EndUpdate();

                    // Collect all new packages of this file
                    foreach (HiiPackageBase hpkpkg in hpk.Childs)
                        Packages.Add(hpkpkg);
                }
            }

            CreateLogEntryMain(LogSeverity.SUCCESS, "Loading files completed!");

            Cursor.Current = previousCursor;
        }

        /// <summary>
        /// Parses a bunch of files which are referring the same "package" using a given default language
        /// </summary>
        /// <param name="Language">Primary language</param>
        private void ParseAllFiles(string Language)
        {
            Cursor previousCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            CreateLogEntryMain(LogSeverity.INFO, "Parsing packages...");

            tv_logical.Parent.Text = "Logical Tree (\"" + Language + "\")";
            tv_logical.Nodes.Clear();

            foreach (TreeNode node_files in tv_tree.Nodes)
            {
                List<HiiPackageBase> Packages = new List<HiiPackageBase>();

                TreeNode PkgNodeLogical = tv_logical.Nodes.Add("Package");
                PkgNodeLogical.Tag = EmptyDetails;

                // Collect all HII packages of the files that are building a logical package
                foreach (TreeNode node_pkg in node_files.Nodes)
                    foreach (HiiPackageBase hpk in (node_pkg.Tag as HPKfile).Childs)
                        Packages.Add(hpk);

                ParsedHpkStringContainer HpkStrings = new ParsedHpkStringContainer(Packages, Language);
                ParsedHpkContainer ParsedHpkContainer = new ParsedHpkContainer(Packages, HpkStrings);

                // Since HPKs interact with each other, build logical tree after loading is completely done
                foreach (ParsedHpkNode pkg in ParsedHpkContainer.HpkPackages)
                {
                    CreateLogEntryMain(LogSeverity.INFO, "Parsing \"" + pkg.Name + "\" ...");

                    tv_logical.BeginUpdate();
                    TreeNode root = PkgNodeLogical.Nodes.Add(pkg.Name);
                    root.Tag = pkg.Origin;
                    ShowAtLogicalTree(pkg, root);
                    root.Expand();
                    tv_logical.EndUpdate();
                }

                PkgNodeLogical.Expand();
            }

            if (0 == tv_logical.Nodes.Count)
            {
                TreeNode EmptyTree = tv_logical.Nodes.Add("No parsed packages available");
                EmptyTree.Tag = EmptyDetails;
            }

            CreateLogEntryMain(LogSeverity.SUCCESS, "Parsing packages completed!");

            Cursor.Current = previousCursor;
        }
        
        /// <summary>
        /// Creates HTML files foreach logical parsed formset using a given default language
        /// </summary>
        /// <param name="Language">Primary language</param>
        private void CreateHTMLFiles(string Language)
        {
            Cursor previousCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            CreateLogEntryMain(LogSeverity.INFO, "Creating HTML output...");

            foreach (TreeNode node_files in tv_tree.Nodes)
            {
                List<HiiPackageBase> Packages = new List<HiiPackageBase>();

                // Collect all HII packages of the files that are building a logical package
                foreach (TreeNode node_pkg in node_files.Nodes)
                    foreach (HiiPackageBase hpk in (node_pkg.Tag as HPKfile).Childs)
                        Packages.Add(hpk);

                ParsedHpkStringContainer HpkStrings = new ParsedHpkStringContainer(Packages, Language);
                HtmlBuilder HtmlBuilder = new HtmlBuilder(Packages, HpkStrings);
            }

            // TODO!
            //if (0 == tv_logical.Nodes[0].Nodes.Count)
            //    CreateLogEntryMain(LogSeverity.ERROR, "Creating HTML output failed!\nNo parsed package available");
            //else
            CreateLogEntryMain(LogSeverity.SUCCESS, "Creating HTML output completed!");

            Cursor.Current = previousCursor;
        }

        /// <summary>
        /// Adds a subtree according to the given HPK tree
        /// </summary>
        /// <param name="elem">Root node of HPK tree</param>
        /// <param name="root">Root node of target tree</param>
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

        /// <summary>
        /// Adds a subtree according to the given parsed HPK tree
        /// </summary>
        /// <param name="node">Root node of parsed HPK tree</param>
        /// <param name="root">Root node of target tree</param>
        private void ShowAtLogicalTree(ParsedHpkNode node, TreeNode root)
        {
            // add all child elements to the tree..
            if (node.Childs.Count > 0)
            {
                foreach (ParsedHpkNode child in node.Childs)
                {
                    ShowAtLogicalTree(child, AddTreeNode(root, child.Name, child.Origin));
                }
                root.Expand();
            }
        }

        /// <summary>
        /// Shows object's data at details window
        /// </summary>
        /// <param name="obj">Object to be displayed</param>
        /// <param name="name">Name of the displayed object (in case of error)</param>
        private void ShowAtDetails(object obj, string name)
        {
            Cursor previousCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            tv_details.BeginUpdate();

            tv_details.Nodes.Clear();
            if (obj == null)
            {
                // should not happen because every node should have an object!
                tv_details.Nodes.Add(EmptyDetails);
                CreateLogEntryMain(LogSeverity.WARNING, "No data found for \"" + name + "\"!");
            }
            else if (obj is HPKElement)
            {
                HPKElement elem = (HPKElement)obj;
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
            else if (obj is String)
            {
                tv_details.Nodes.Add((string)obj);
            }
            else // Unknown data type
            {
                tv_details.Nodes.Add(EmptyDetails);
                CreateLogEntryMain(LogSeverity.UNIMPLEMENTED, "Unkown data found for \"" + name + "\"!");
            }

            tv_details.EndUpdate();
            Cursor.Current = previousCursor;
        }

        /// <summary>
        /// Handler for CTRL+C copy shortcut on tree view nodes
        /// Copies the tree node's text to clipboard
        /// </summary>
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
