using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace WebTVDATEditor
{
    public partial class frmMain : Form
    {
        /* WebTV/MSN TV Strings Editor - An editing tool that allows for modification of string files used by WebTV and MSN TV-based software.
         * Most code written by wtv-411, with some quality of life improvements (responsive UI controls) by nitrate92.
         * This program is currently not under a proper license, but you are free to mess with the source code as you please. Also, don't be a prude about it.
         * 
         * THINGS TO EVENTUALLY ADD:
         * - Detect newline format used for strings (some use CF, some use LF) - this is implemented, but it assumes that every string file has a newline in it and defaults to LF if none
         *   are found. Not too sure how well that'd go when modifying strings for MSN TV builds.
         * - Add program icon
         * 
         * THINGS THAT WOULD BE NICE TO HAVE:
         * - Ability to find strings - postponed for now since I can't get the feature to work properly
         */

        // variables
        private string currentFile = null;
        private object _unsaveLock = new object();
        private int _workingStringId = -1;
        private bool _unsavedChanges = false;
        // string stuff
        private WTVStringNewlineType _newlineType = WTVStringNewlineType.None;
        private List<WebTVStringTableEntry> _wtvStrings = new List<WebTVStringTableEntry>();

        //Stuff for find feature - currently unused
        public FIND_DIRECTION findDirection = FIND_DIRECTION.None;
        private frmFind _findForm = null;

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            /*this.findDirection = FIND_DIRECTION.UP;*/ //set default find direction to up
            // temporary until newline toggle functionality is implemented
            this.lineFeedMenuItem.Enabled = false;
            this.carriageReturnMenuItem.Enabled = false;
            RefreshUI(true);

        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "WebTV String Files (*.dir)|*.dir";
            //open.RestoreDirectory = true;
            if (open.ShowDialog() == DialogResult.OK)
            {
                if (Path.GetDirectoryName(open.FileName) + "\\" + Path.GetFileNameWithoutExtension(open.FileName) == this.currentFile) return;

                switch (this.GetUnsavedChangesChoice())
                {
                    case DialogResult.Yes:
                        DoSaveChanges();
                        break;
                    case DialogResult.No:
                        // Do nothing here and proceed with termination
                        break;
                    case DialogResult.Cancel:
                    default:
                        return;
                }

                DoOpenFile(open.FileName);
            }
        }

        void DoOpenFile(string path)
        {
            // Close Find form if it's already open before opening new file
            if (this._findForm != null)
            {
                this._findForm.Close();
            }

            this.currentFile = Path.GetDirectoryName(path) + "\\" + Path.GetFileNameWithoutExtension(path);
            PrepareStringFileOpen();
            // Detect newline type used in strings and normalize them for display in the TextBox
            DetermineNewlineType();
            RefreshUI(true);
        }

        void PrepareStringFileOpen()
        {
            if (this.currentFile == null) return;
            Stream datFile, dirFile;
            byte[] dirTmp = new byte[4];

            this.tmrTrackUnsavedData.Stop();
            this._wtvStrings.Clear();
            this._unsavedChanges = false;
            this._workingStringId = -1;

            // open .dir file and corresponding .dat file
            // if .dat file doesn't exist, terminate
            // parse data in .dir file as an array of 32-bit numbers
            // use each array element to read in .dat file until we reach NUL byte
            // store parsed data in memory and display in GUI
            
            try
            {
                dirFile = File.OpenRead(this.currentFile + ".dir");
            }
            catch (Exception)
            {
                this.currentFile = null;
                RefreshUI(true);
                MessageBox.Show(".DIR file for WebTV/MSN TV string file could not be opened.", "WebTV String Editor", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            try
            {
                datFile = File.OpenRead(this.currentFile + ".dat");
            } catch (Exception)
            {
                this.currentFile = null;
                RefreshUI(true);
                MessageBox.Show(".DAT file for WebTV/MSN TV string file does not exist or could not be opened.", "WebTV String Editor", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            while (true)
            {
                int bytesRead = dirFile.Read(dirTmp, 0, 4);
                if (bytesRead < 4) break;

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(dirTmp);
                int dirPos = (int)BitConverter.ToUInt32(dirTmp, 0);
                byte[] datContent = ReadTillNulByte(datFile, dirPos);
                this._wtvStrings.Add(new WebTVStringTableEntry(Encoding.GetEncoding(932).GetString(datContent), dirPos)); //use Shift-JIS as a cheap hack for now to support both US and Japanese strings
            }

            datFile.Close();
            dirFile.Close();
        }

        void DetermineNewlineType()
        {
            var nlIndex = -1;
            
            foreach (WebTVStringTableEntry s in this._wtvStrings)
            {
                nlIndex = s.Data.IndexOf("\r");
                if (nlIndex != -1)
                {
                    if (s.Data.Length > (nlIndex + 1) && s.Data[nlIndex + 1] == '\n')
                    {
                        this._newlineType = WTVStringNewlineType.CRLF;
                    }
                    else
                    {
                        this._newlineType = WTVStringNewlineType.CR;
                    }

                    break;
                }
                else
                {
                    nlIndex = s.Data.IndexOf("\n");
                    if (nlIndex != -1)
                    {
                        this._newlineType = WTVStringNewlineType.LF;
                        break;
                    }
                }
            }

            // Do no newlines exist in strings? assume LF encoding to be safe (used by string files in most WebTV builds)
            if (nlIndex == -1)
            {
                this._newlineType = WTVStringNewlineType.LF;
            }
        }

        string NormalizeNewlineInString(string s)
        {
            if (this._newlineType == WTVStringNewlineType.CRLF)
            {
                return s;
            } else
            {
                var nlChars = GetCharsForNewlineType();
                return s.Replace(nlChars, "\r\n");
            }
        }

        string UnnormalizeNewlineInString(string s)
        {
            if (this._newlineType == WTVStringNewlineType.CRLF)
            {
                return s;
            }
            else
            {
                var nlChars = GetCharsForNewlineType();
                return s.Replace("\r\n", nlChars);
            }
        }

        string GetCharsForNewlineType()
        {
            switch (this._newlineType)
            {
                case WTVStringNewlineType.CRLF:
                    return "\r\n";
                case WTVStringNewlineType.CR:
                    return "\r";
                case WTVStringNewlineType.LF:
                default:
                    return "\n";
            }
        }

        void RefreshUI(bool refreshTreeView)
        {
            // Clear editing text box
            if (this._workingStringId == -1)
            {
                txtContent.Clear();
                txtContent.Hide();
                this.wordWrapToolStripMenuItem.Enabled = false;
                //this.findToolStripMenuItem.Enabled = false;
            } else
            {
                txtContent.Show();
                this.wordWrapToolStripMenuItem.Enabled = true;
                //this.findToolStripMenuItem.Enabled = true;
            }

            // Change title based on if file is open, unsaved, or anything else
            string title = "";
            if (this.currentFile != null && this.currentFile != "")
            {
                title += Path.GetFileNameWithoutExtension(this.currentFile);
                if (this._unsavedChanges)
                    title += "*";
                title += " - ";
            }
            title += "WebTV String Editor";
            this.Text = title;

            if (this.currentFile != null && this.currentFile != "" && this._unsavedChanges)
            {
                saveMenuItem.Enabled = true;
            }
            else
            {
                saveMenuItem.Enabled = false;
            }

            UpdateWordWrapControls();

            //clear status
            if (this.currentFile != null && this.currentFile != "" && this._newlineType != WTVStringNewlineType.None)
            {
                statusLbl.Text = "Newline Encoding: " + this._newlineType.ToString();
                switch(this._newlineType)
                {
                    case WTVStringNewlineType.CR:
                        carriageReturnMenuItem.Checked = true;
                        lineFeedMenuItem.Checked = false;
                        break;
                    case WTVStringNewlineType.LF:
                        lineFeedMenuItem.Checked = true;
                        carriageReturnMenuItem.Checked = false;
                        break;
                    default:
                        break;
                }
            } else
            {
                statusLbl.Text = "";
            }
            
            //TODO: Add a thing where treeview has disabled look until file is loaded
            if (refreshTreeView)
                LoadWebTVDATStringsIntoTreeView();
        }

        void UpdateWordWrapControls()
        {
            wordWrapToolStripMenuItem.Checked = txtContent.WordWrap;
        }

        void LoadWebTVDATStringsIntoTreeView()
        {
            treeView1.BeginUpdate();
            treeView1.Nodes.Clear();
            for (int i = 0; i < this._wtvStrings.Count; i++)
            {
                TreeNode stringRoot = treeView1.Nodes.Add("String #" + (i + 1).ToString());
                TreeNode dataNode = treeView1.Nodes[i].Nodes.Add("Data");
                dataNode.Tag = i;
                // Expand node so user has easier access to string data
                stringRoot.Expand();
            }
            treeView1.EndUpdate();
        }

        byte[] ReadTillNulByte(Stream stream, int pos)
        {
            int b;

            MemoryStream tmp = new MemoryStream();
            stream.Seek(pos, SeekOrigin.Begin);
            while (true)
            {
                b = stream.ReadByte();
                if (b == 0x00 || b == -1) break;
                tmp.WriteByte((byte)b);
            }
            stream.Seek(0, SeekOrigin.Begin);

            return tmp.ToArray();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void saveMenuItem_Click(object sender, EventArgs e)
        {
            DoSaveChanges();
        }

        void DoSaveChanges()
        {
            if (!this._unsavedChanges) return;

            // Write data to .dat first, then write locations in .dir
            int i = 0;
            MemoryStream wtvStringData = new MemoryStream();
            MemoryStream wtvStringLocs = new MemoryStream();

            // Write new string locations and new string data
            foreach (WebTVStringTableEntry entry in this._wtvStrings)
            {
                entry.FileLocation = i;

                byte[] tmp = Encoding.GetEncoding(932).GetBytes(entry.Data);
                wtvStringData.Write(tmp, 0, tmp.Length);
                wtvStringData.WriteByte((byte)0x00);

                byte[] locTmp = BitConverter.GetBytes((uint)entry.FileLocation);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(locTmp);
                wtvStringLocs.Write(locTmp, 0, 4);

                i += tmp.Length + 1; //add 1 extra character for NUL
            }

            using (var f = File.Create(this.currentFile + ".dat"))
            {
                f.Write(wtvStringData.ToArray(), 0, wtvStringData.ToArray().Length);
                f.Close();
            }

            using (var f = File.Create(this.currentFile + ".dir"))
            {
                f.Write(wtvStringLocs.ToArray(), 0, wtvStringLocs.ToArray().Length);
                f.Close();
            }

            // Reset working string ID and refresh UI
            this._workingStringId = -1;
            this._unsavedChanges = false;
            RefreshUI(true);
        }

        // UI event handlers

        // TreeView

        private void treeView1_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            var parentNode = e.Node;
            if (parentNode.FirstNode == null) return;

            if ((int)parentNode.FirstNode.Tag == this._workingStringId)
            {
                this._workingStringId = -1;
                RefreshUI(false); // Don't refresh TreeView when it's about to update itself on collapse
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag == null)
            {
                tmrTrackUnsavedData.Stop();
                this._workingStringId = -1;
                RefreshUI(false);
                return;
            }
            
            int stringId = (int)e.Node.Tag;
            WebTVStringTableEntry entry = this._wtvStrings[stringId];
            this._workingStringId = stringId;
            txtContent.Text = NormalizeNewlineInString(entry.Data);
            tmrTrackUnsavedData.Start();

            RefreshUI(false);
        }

        // Data track timer

        private void tmrTrackUnsavedData_Tick(object sender, EventArgs e)
        {
            lock(_unsaveLock)
            {
                if (this._workingStringId == -1) return;

                var s = this.UnnormalizeNewlineInString(this.txtContent.Text);
                if (s != this._wtvStrings[this._workingStringId].Data)
                {
                    this._wtvStrings[this._workingStringId].Data = s;
                    this._unsavedChanges = true;
                    RefreshUI(false);
                    //Debug.Print("Caught unsaved changes.");
                }
            }
        }

        // Form stuff

        private void frmMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void frmMain_DragDrop(object sender, DragEventArgs e)
        {
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (paths.Length != 1) return;

            if (paths[0].EndsWith(".dir"))
            {
                DoOpenFile(paths[0]);
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            switch (this.GetUnsavedChangesChoice())
            {
                case DialogResult.Yes:
                    DoSaveChanges();
                    break;
                case DialogResult.No:
                    // Do nothing here and proceed with termination
                    break;
                case DialogResult.Cancel:
                default:
                    e.Cancel = true;
                    break;
            }
        }

        private DialogResult GetUnsavedChangesChoice()
        {
            if (this._unsavedChanges)
                return MessageBox.Show("You have unsaved changes. Would you like to save them?", "WebTV String Editor", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
            return DialogResult.No; // default
        }

        private void wordWrapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (txtContent.WordWrap == false)
            {
                txtContent.WordWrap = true;
            }
            else
            {
                txtContent.WordWrap = false;
            }
        }

        private void fontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fontDialog1.ShowColor = true;

            fontDialog1.Font = txtContent.Font;
            fontDialog1.Color = txtContent.ForeColor;

            if (fontDialog1.ShowDialog() != DialogResult.Cancel)
            {
                txtContent.Font = fontDialog1.Font;
                txtContent.ForeColor = fontDialog1.Color;
            }
        }


        // functions for unused/scrapped features

        // Find
        
        /*
        public void OnFindCallback(string toFind)
        {
            var findIndex = -1;
            if (this.findDirection == FIND_DIRECTION.UP)
            {
                var findSampleUp = txtContent.Text.Substring(0, txtContent.SelectionStart);
                findIndex = findSampleUp.LastIndexOf(toFind.ToLower());
            }
            else
            {
                findIndex = txtContent.Text.IndexOf(toFind.ToLower(), txtContent.SelectionStart + txtContent.SelectionLength);
            }

            if (findIndex != -1)
            {
                txtContent.Focus();
                txtContent.Select(findIndex, toFind.Length);
            }
            else
            {
                MessageBox.Show("text not found", "WebTV String Editor", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        public void OnFindFormClose()
        {
            if (this._findForm == null) return;
            this._findForm = null;
            Debug.Print("Find form destroyed!");
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this._findForm != null)
            {
                this._findForm.Focus();
                return;
            }

            this._findForm = new frmFind();
            this._findForm.Owner = this;
            this._findForm.Show();
        }*/
    }
}
