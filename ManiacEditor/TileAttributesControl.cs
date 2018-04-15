using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RSDKv5;

namespace ManiacEditor
{
    public partial class TileAttributesControl : UserControl
    {
        private TileConfig _value = null;

        public TileAttributesControl()
        {
            InitializeComponent();
        }

        public TileConfig Value
        {
            get => _value;
            set
            {
                _value = value;
                if (value == null)
                {
                    Enabled = false;
                }
                else
                {
                    tileConfigBindingSource.DataSource = Value;
                    Enabled = true;
                }
            }
        }
    }
}
