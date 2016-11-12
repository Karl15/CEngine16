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
        Parser parser = new Parser();
        CEngine cengine = new CEngine();
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog(); // To select the source code file
            // .txt or .c containing standard C code main(){} function.  
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

            fPath = openFileDialog1.FileName;
            dPath = Path.GetDirectoryName(fPath);
        /* Syntax tree will be used instead of Parser 
         * to List<uint> uCwds and List<Cvbl> cvbls
        */ 
            // Parser will open source, parse, and execute generated 
            parser.begin(fPath, listBox1, cengine);
       /*
        * List<String> listBox1 formatted Strings show cycle by cycle execution
        * after ipl() which calls the execution method after loading memories
        */
            cengine.ipl(listBox1, parser.uCwds, parser.cvbls);
            FileStream log = new FileStream("logOut.txt", FileMode.OpenOrCreate);
            StreamWriter lwrt = new StreamWriter(log);
            foreach (string s in listBox1.Items)
            {
                lwrt.WriteLine(s);
            }
            lwrt.Flush();
            log.Close();
            openFileDialog1.Dispose();
        }

    }
}
