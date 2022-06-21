using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UnityDiffFixerCommandLine;

namespace UnityDiffFixer
{
    public partial class GUIRunner : Form
    {
        private string m_oldFilePath = "";
        private string m_newFilePath = "";
        private Program m_program;
        private int m_returnCode;

        public GUIRunner(UnityDiffFixerCommandLine.Program program)
        {
            m_program = program;
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                m_oldFilePath = openFileDialog1.FileName;
                textBox1.Text = m_oldFilePath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                m_newFilePath = openFileDialog1.FileName;
                textBox2.Text = m_newFilePath;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
            this.m_returnCode = m_program.RunFromGUI(m_oldFilePath, m_newFilePath);
        }

        internal int GetReturnCode()
        {
            return this.m_returnCode;
        }

        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
                var data = e.Data.GetData(DataFormats.FileDrop);
                if (data != null && data is string[])
                {
                    m_oldFilePath = ((string[])data)[0];
                    textBox1.Text = m_oldFilePath;
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void textBox1_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void textBox1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void textBox2_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void textBox2_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
                var data = e.Data.GetData(DataFormats.FileDrop);
                if (data != null && data is string[])
                {
                    m_newFilePath = ((string[])data)[0];
                    textBox2.Text = m_newFilePath;
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void textBox2_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            m_oldFilePath = textBox1.Text;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            m_newFilePath = textBox2.Text;
        }
    }
}
