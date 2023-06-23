using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebTVDATEditor
{
    public partial class frmFind : Form
    {
        public frmFind()
        {
            InitializeComponent();
        }

        private void frmFind_Load(object sender, EventArgs e)
        {
            //this should never happen
            if (this.Owner == null)
            {
                this.Close();
                return;
            }

            // We only need this on initial load
            switch (((frmMain)this.Owner).findDirection)
            {
                case FIND_DIRECTION.DOWN:
                    radioUp.Checked = false;
                    radioDown.Checked = true;
                    break;
                case FIND_DIRECTION.UP:
                default:
                    radioDown.Checked = false;
                    radioUp.Checked = true;
                    break;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void radioUp_CheckedChanged(object sender, EventArgs e)
        {
            //((frmMain)this.Owner).findDirection = FIND_DIRECTION.UP;
        }

        private void radioDown_CheckedChanged(object sender, EventArgs e)
        {
            //((frmMain)this.Owner).findDirection = FIND_DIRECTION.DOWN;
        }

        private void btnFind_Click(object sender, EventArgs e)
        {
            //((frmMain)this.Owner).OnFindCallback(this.txtToFind.Text);
        }

        private void frmFind_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.Owner == null) return;
            //((frmMain)this.Owner).OnFindFormClose();
        }
    }
}
