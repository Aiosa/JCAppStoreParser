using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCAppStore_Parser.Utils
{
    public static class FileBrowser
    {
        public static string Open(string location)
        {
            if (location != null && !File.Exists(location) && !Directory.Exists(location))
            {
                return null;
            }

            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = location;
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                return fbd.SelectedPath;
            }
            return null;           
        }
    }

    /// <summary>
    /// From https://docs.microsoft.com/cs-cz/dotnet/framework/winforms/controls/how-to-open-files-using-the-openfiledialog-component
    /// </summary>
    public class OpenFileDialogForm : Form
    {
      

        private Button selectButton;
        private OpenFileDialog openFileDialog1;
        private TextBox textBox1;

        public OpenFileDialogForm()
        {
            openFileDialog1 = new OpenFileDialog();
            selectButton = new Button
            {
                Size = new Size(100, 20),
                Location = new Point(15, 15),
                Text = "Select file"
            };
            selectButton.Click += new EventHandler(SelectButton_Click);
            textBox1 = new TextBox
            {
                Size = new Size(300, 300),
                Location = new Point(15, 40),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            ClientSize = new Size(330, 360);
            Controls.Add(selectButton);
            Controls.Add(textBox1);
        }
        private void SetText(string text)
        {
            textBox1.Text = text;
        }
        private void SelectButton_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var sr = new StreamReader(openFileDialog1.FileName);
                    SetText(sr.ReadToEnd());
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
            }
        }
    }
}
