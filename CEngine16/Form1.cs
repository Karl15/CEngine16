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

namespace CEngine16
{
    public partial class Form1 : Form
    {
        String fPath;
        String dPath;
        Parser sP = new Parser();
        CEngine vp = new CEngine();
        public Form1()
        {
            InitializeComponent();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

            fPath = openFileDialog1.FileName;
            dPath = Path.GetDirectoryName(fPath);
            sP.cParse(fPath, listBox1, vp);
            //fPath = openFileDialog1.FileName;
            //dPath = Path.GetDirectoryName(fPath);
            //   vp.ex1(0,0,0);
            //vp.lB1 = listBox1;
            //vp.lB2 = listBox2;
            //vp.lB3 = listBox3;
            //listBox1.Hide();
            //vp.lB2.Items.Clear();
            //listBox3.Hide();
            //sP.cParse(fPath, listBox1, vp);
            //log = new FileStream("logOut.txt", FileMode.OpenOrCreate);
            //lwrt = new StreamWriter(log);
            //foreach (string s in listBox1.Items)
            //{
            //    lwrt.WriteLine(s);
            //}
            //lwrt.Flush();
            //log.Close();
            //vp.ipl(listBox1, sP.cas, sP.ccs, sP.cvbls);
            //sP.memInit();
            //listBox2.Show();
            // button3.Show();
            openFileDialog1.Dispose();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }
    }
}
