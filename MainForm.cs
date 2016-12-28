//MIT License
//
//Copyright(c) 2016 Peter Kirmeier
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
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static IFR.IFRHelper;

namespace IfrViewer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            IFRHelper.conout = conout; // Use local window as debug window
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

            // Load project from command line argument (when available)
            if (Environment.GetCommandLineArgs().Length > 1)
            {
                PrintLineToLocalConsole(IfrErrorSeverity.INFO, "Main", "Loading file: \"" + Environment.GetCommandLineArgs()[1] + "\"");
                HPKfile hpk = null;
                try
                {
                    hpk = new HPKfile(Environment.GetCommandLineArgs()[1]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    PrintLineToLocalConsole(IfrErrorSeverity.ERROR, "Main", "Loading file failed!");
                    MessageBox.Show("Loading file failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (null != hpk) // Loaded successfully?
                {
                    TreeNode root = tv.Nodes.Add(hpk.Name);
                    LoadHpkElementIntoTreeView(hpk, root);
                    PrintLineToLocalConsole(IfrErrorSeverity.INFO, "Main", "Loading file completed!");
                }
            }
        }

        private void LoadHpkElementIntoTreeView(HPKElement elem, TreeNode root)
        {
            // add all header fields to the tree..
            byte[] HeaderRaw = elem.HeaderRaw;
            if ((elem.Header != null) || (HeaderRaw != null))
            {
                TreeNode branch = root.Nodes.Add("Header");
                // handle raw..
                if (HeaderRaw != null)
                {
                    TreeNode leaf = branch.Nodes.Add("__RAW");
                    foreach (string line in HeaderRaw.HexDump(8).Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                        leaf.Nodes.Add(line);
                }
                // handle managed..
                if (elem.Header != null)
                {
                    foreach (System.Collections.Generic.KeyValuePair<string, object> pair in elem.HeaderToStringList())
                    {
                        branch.Nodes.Add(pair.Key + " = " + pair.Value.ToString());
                    }
                }
            }
            // add all payload fields to the tree..
            byte[] PayloadRaw = elem.PayloadRaw;
            if ((elem.Payload != null) || (PayloadRaw != null))
            {
                TreeNode branch = root.Nodes.Add("Payload");
                // handle raw..
                if (PayloadRaw != null)
                {
                    TreeNode leaf = branch.Nodes.Add("__RAW");
                    foreach (string line in PayloadRaw.HexDump(8).Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                        leaf.Nodes.Add(line);
                }
                // handle managed..
                if (elem.Payload != null)
                {
                    foreach (System.Collections.Generic.KeyValuePair<string, object> pair in elem.PayloadToStringList())
                    {
                        branch.Nodes.Add(pair.Key + " = " + pair.Value.ToString());
                    }
                }
            }

            // add all child elements to the tree..
            if (elem.Childs.Count > 0)
            {
                TreeNode branch = root.Nodes.Add("Childs");
                foreach (HPKElement child in elem.Childs)
                {
                    TreeNode leaf = branch.Nodes.Add(child.Name);
                    LoadHpkElementIntoTreeView(child, leaf);
                    branch.Expand();
                }
                root.Expand();
            }
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            splitContainer1.Width = ClientSize.Width - 24;
            splitContainer1.Height = ClientSize.Height - 24;
            tv.Width = splitContainer1.Panel1.Width - 6;
            tv.Height = splitContainer1.Panel1.Height - 6;
            conout.Width = splitContainer1.Panel2.Width - 6;
            conout.Height = splitContainer1.Panel2.Height - 6;
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            MainForm_SizeChanged(sender, e);
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
