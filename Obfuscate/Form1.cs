using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using dnlib.DotNet;
using Obfuscate.Properties;

namespace Obfuscate
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void label1_DragDrop(object sender, DragEventArgs e)
        {
            string filepath = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();

            ModuleDefMD asmDef = null;
            using (asmDef = ModuleDefMD.Load(filepath))
            using (SaveFileDialog saveFileDialog1 = new SaveFileDialog())
            {
                saveFileDialog1.Filter = ".exe (*.exe)|*.exe";
                saveFileDialog1.InitialDirectory = Application.StartupPath;
                saveFileDialog1.OverwritePrompt = false;
                saveFileDialog1.FileName = "Output";
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    asmDef = Obfuscate.obfuscate(asmDef);
                    asmDef.Write(saveFileDialog1.FileName);
                    asmDef.Dispose();                }
            }
        }

        private void label1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else e.Effect = DragDropEffects.None;
        }
    }
}
