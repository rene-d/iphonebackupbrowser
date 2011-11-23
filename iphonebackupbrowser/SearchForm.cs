using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;


namespace iphonebackupbrowser
{ 

    public partial class SearchForm : Form
    {
        public void AddFile(string filename)
        {
        }

        public SearchForm()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            listView1.View = View.Details;
            listView1.Columns.Add("File", 200);
            listView1.Columns.Add("Found at", 200);
            listView1.Columns.Add("Text");

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string pattern;

            pattern = RemoveDiacritics(textBox1.Text);
            
            textBox1.Text = pattern;
        }


        private string RemoveDiacritics(string stIn)
        {
            string stFormD = stIn.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();
            for (int ich = 0; ich < stFormD.Length; ich++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(stFormD, ich);

                //if (uc != UnicodeCategory.NonSpacingMark) sb.Append(stFormD[ich]);

                switch (uc)
                {
                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.DecimalDigitNumber:
                    case UnicodeCategory.UppercaseLetter:
                        sb.Append(stFormD[ich]);
                        break;
                }
            }

            return (sb.ToString().Normalize(NormalizationForm.FormC).ToUpper());
        }
    }


}
