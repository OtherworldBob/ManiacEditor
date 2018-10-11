using ManiacEditor.Entity_Renders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ManiacEditor
{
    public partial class DeveloperTerminal : Form
    {
        // For Interger Changer; Change to the Value you want to tweak
        int valueINI = Properties.EditorState.Default.developerInt;
        string curVal = "Current Value: ";

        public DeveloperTerminal()
        {
            InitializeComponent();
            currentLabel.Text = curVal + valueINI;
        }

        private void subtract10_Click(object sender, EventArgs e)
        {
            Properties.EditorState.Default.developerInt = Properties.EditorState.Default.developerInt - 10;
            currentLabel.Text = curVal + Properties.EditorState.Default.developerInt;
        }

        private void subtract_Click(object sender, EventArgs e)
        {
            Properties.EditorState.Default.developerInt = Properties.EditorState.Default.developerInt - 1;
            currentLabel.Text = curVal + Properties.EditorState.Default.developerInt;
        }

        private void add10_Click(object sender, EventArgs e)
        {
            Properties.EditorState.Default.developerInt = Properties.EditorState.Default.developerInt + 10;
            currentLabel.Text = curVal + Properties.EditorState.Default.developerInt;
        }

        private void add_Click(object sender, EventArgs e)
        {
            Properties.EditorState.Default.developerInt = Properties.EditorState.Default.developerInt + 1;
            currentLabel.Text = curVal + Properties.EditorState.Default.developerInt;
        }

        private void enterManualEntry_Click(object sender, EventArgs e)
        {
            int manualTextInt;
            if (!Int32.TryParse(manualEntryText.Text, out manualTextInt))
            {
                errorLabel.Text = "Error: Not an Int";
                currentLabel.Text = curVal + Properties.EditorState.Default.developerInt;
            }
            else
            {
                Properties.EditorState.Default.developerInt = manualTextInt;
                currentLabel.Text = curVal + Properties.EditorState.Default.developerInt;
            }
            
        }
    }
}
