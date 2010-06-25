// convertir DTD -> XSD : http://www.hitsw.com/xml_utilites/

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;


namespace iphonebackupbrowser
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// Cette classe est une implémentation de l'interface 'IComparer'.
        /// </summary>
        public class ListViewColumnSorter : IComparer
        {
            /// <summary>
            /// Spécifie la colonne à trier
            /// </summary>
            private int ColumnToSort;
            /// <summary>
            /// Spécifie l'ordre de tri (en d'autres termes 'Croissant').
            /// </summary>
            private SortOrder OrderOfSort;
            /// <summary>
            /// Objet de comparaison ne respectant pas les majuscules et minuscules
            /// </summary>
            private CaseInsensitiveComparer ObjectCompare;

            /// <summary>
            /// Constructeur de classe.  Initializes various elements
            /// </summary>
            public ListViewColumnSorter()
            {
                // Initialise la colonne sur '0'
                ColumnToSort = 0;

                // Initialise l'ordre de tri sur 'aucun'
                OrderOfSort = SortOrder.None;

                // Initialise l'objet CaseInsensitiveComparer
                ObjectCompare = new CaseInsensitiveComparer();
            }

            /// <summary>
            /// Cette méthode est héritée de l'interface IComparer.  Il compare les deux objets passés en effectuant une comparaison 
            ///qui ne tient pas compte des majuscules et des minuscules.
            /// </summary>
            /// <param name="x">Premier objet à comparer</param>
            /// <param name="x">Deuxième objet à comparer</param>
            /// <returns>Le résultat de la comparaison. "0" si équivalent, négatif si 'x' est inférieur à 'y' 
            ///et positif si 'x' est supérieur à 'y'</returns>
            public int Compare(object x, object y)
            {
                int compareResult;
                ListViewItem listviewX, listviewY;

                // Envoit les objets à comparer aux objets ListViewItem
                listviewX = (ListViewItem)x;
                listviewY = (ListViewItem)y;

                // Compare les deux éléments
                compareResult = ObjectCompare.Compare(listviewX.SubItems[ColumnToSort].Text, listviewY.SubItems[ColumnToSort].Text);

                // Calcule la valeur correcte d'après la comparaison d'objets
                if (OrderOfSort == SortOrder.Ascending)
                {
                    // Le tri croissant est sélectionné, renvoie des résultats normaux de comparaison
                    return compareResult;
                }
                else if (OrderOfSort == SortOrder.Descending)
                {
                    // Le tri décroissant est sélectionné, renvoie des résultats négatifs de comparaison
                    return (-compareResult);
                }
                else
                {
                    // Renvoie '0' pour indiquer qu'ils sont égaux
                    return 0;
                }
            }

            /// <summary>
            /// Obtient ou définit le numéro de la colonne à laquelle appliquer l'opération de tri (par défaut sur '0').
            /// </summary>
            public int SortColumn
            {
                set
                {
                    ColumnToSort = value;
                }
                get
                {
                    return ColumnToSort;
                }
            }

            /// <summary>
            /// Obtient ou définit l'ordre de tri à appliquer (par exemple, 'croissant' ou 'décroissant').
            /// </summary>
            public SortOrder Order
            {
                set
                {
                    OrderOfSort = value;
                }
                get
                {
                    return OrderOfSort;
                }
            }

        }

        private List<iPhoneBackup> backups = new List<iPhoneBackup>();
        private iPhoneManifestData manifest;

        private ListViewColumnSorter lvwColumnSorter;


        public Form1()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            listView1.Columns.Add("Display Name", 200);
            listView1.Columns.Add("Name", 200);
            listView1.Columns.Add("Files");
            listView1.Columns.Add("Identifier", 200);

            listView2.Columns.Add("Name", 400);
            listView2.Columns.Add("Size");
            listView2.Columns.Add("Date", 130);
            listView2.Columns.Add("Domain", 300);
            listView2.Columns.Add("Key", 250);

            // Créer une instance d'une méthode de trie de la colonne ListView et l'attribuer
            // au contrôle ListView.            
            lvwColumnSorter = new ListViewColumnSorter();
            listView2.ListViewItemSorter = lvwColumnSorter;


            string s = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            s = Path.Combine(s, "Apple Computer", "MobileSync", "Backup");

            DirectoryInfo d = new DirectoryInfo(s);

            foreach (DirectoryInfo sd in d.EnumerateDirectories())
            {
                try
                {                    
                    string filename = Path.Combine(sd.FullName, "Info.plist");
                    xdict dd = xdict.open(filename);

                    if (dd != null)
                    {
                        iPhoneBackup backup = new iPhoneBackup();

                        backup.path = sd.FullName;

                        foreach (xdictpair p in dd)
                        {
                            if (p.item.GetType() == typeof(string))
                            {
                                switch (p.key)
                                {
                                    case "Device Name": backup.DeviceName = (string)p.item; break;
                                    case "Display Name": backup.DisplayName = (string)p.item; break;
                                    case "Last Backup Date": backup.LastBackupDate = (string)p.item; break;
                                }
                            }
                        }

                        backups.Add(backup);
                    }               
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(ex.InnerException.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            foreach (iPhoneBackup b in backups)
            {
                comboBox1.Items.Add(b);
            }

            //comboBox1.SelectedIndex = 0;
        }



        private void parseApplications(xdict di, HashSet<string> files)
        {
            dict sd;

            if (!di.findKey("Applications", out sd))
                return;

            //manifest.Applications = new List<iPhoneApp>();

            foreach (xdictpair p in new xdict(sd))
            {
                iPhoneApp app = new iPhoneApp();

                app.Key = p.key;

                foreach (xdictpair q in new xdict(p.item))
                {
                    if (q.key == "AppInfo")
                    {
                        xdict zz = new xdict(q.item);

                        zz.findKey("CFBundleDisplayName", out app.DisplayName);
                        zz.findKey("CFBundleName", out app.Name);
                        zz.findKey("CFBundleIdentifier", out app.Identifier);
                        zz.findKey("Container", out app.Container);
                    }
                    else if (q.key == "Files" && q.item.GetType() == typeof(array))
                    {
                        array ar = (array)q.item;

                        app.Files = new List<String>();

                        for (int k = 0; k < ar.Items.Length; ++k)
                        {
                            string name = (string)ar.Items[k];
                            app.Files.Add(name);
                            files.Add(name);
                        }
                    }
                }

                // il y a des applis mal paramétrées...
                if (app.Name == null) app.Name = app.Key;
                if (app.DisplayName == null) app.DisplayName = app.Name;

                //manifest.Applications.Add(app);

                ListViewItem lvi = new ListViewItem();
                lvi.Tag = app;
                lvi.Text = app.DisplayName;
                lvi.SubItems.Add(app.Name);
                lvi.SubItems.Add(app.Files != null ? app.Files.Count.ToString() : "N/A");
                lvi.SubItems.Add(app.Identifier != null ? app.Identifier  : "N/A");
                listView1.Items.Add(lvi);
            }
        }


        private void parseFiles(xdict di, HashSet<string> files)
        {
            dict sd;

            if (!di.findKey("Files", out sd))
                return;

            manifest.Files = new Dictionary<string, iPhoneFile>();

            iPhoneApp system = new iPhoneApp();
            system.Name = "System";
            system.DisplayName = "---";
            system.Identifier = "---";
            system.Container = "---";
            system.Files = new List<String>();

            foreach (xdictpair p in new xdict(sd))
            {
                //Debug.WriteLine("{0} {1}", p.key, p.item);

                iPhoneFile f = new iPhoneFile();

                f.Key = p.key;
                f.Path = null;

                foreach (xdictpair q in new xdict(p.item))
                {
                    //Debug.WriteLine("{0} {1}", q.key, q.item);

                    if (q.key == "Domain")
                        f.Domain = (string)q.item;
                    else if (q.key == "ModificationTime")
                        f.ModificationTime = (string)q.item;
                    else if (q.key == "FileLength")
                        f.FileLength = Convert.ToInt32((string)q.item);
                }

                manifest.Files.Add(p.key, f);

                if (!files.Contains(p.key))
                {
                    system.Files.Add(p.key);
                }
            }


            if (system.Files.Count != 0)
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Tag = system;
                lvi.Text = system.DisplayName;
                lvi.SubItems.Add(system.Name);
                lvi.SubItems.Add(system.Files != null ? system.Files.Count.ToString() : "N/A");
                lvi.SubItems.Add(system.Identifier != null ? system.Identifier : "N/A");
                listView1.Items.Add(lvi);
            }
        }


        private void parseApplications92(xdict di, HashSet<string> files)
        {
            dict sd;

            if (!di.findKey("Applications", out sd))
                return;

            //manifest.Applications = new List<iPhoneApp>();

            foreach (xdictpair p in new xdict(sd))
            {
                iPhoneApp app = new iPhoneApp();

                app.Key = p.key;

                xdict zz = new xdict(p.item);
                zz.findKey("CFBundleDisplayName", out app.DisplayName);
                zz.findKey("CFBundleName", out app.Name);
                zz.findKey("CFBundleIdentifier", out app.Identifier);
                zz.findKey("Container", out app.Container);                    
                

                // il y a des applis mal paramétrées...
                if (app.Name == null) app.Name = app.Key;
                if (app.DisplayName == null) app.DisplayName = app.Name;
                
                ListViewItem lvi = new ListViewItem();
                lvi.Tag = app;
                lvi.Text = app.DisplayName;
                lvi.SubItems.Add(app.Name);
                lvi.SubItems.Add("N/A");
                lvi.SubItems.Add("N/A");
                listView1.Items.Add(lvi);
            }
        }


        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            /**
            try
            {
                iPhoneBackup backup = (iPhoneBackup)comboBox1.SelectedItem;

                foreach (FileInfo sd in new DirectoryInfo(backup.path).EnumerateFiles("*.mdinfo"))
                {
                    //string Domain, Path;

                    //DLL_mdinfo(sd.FullName, out Domain, out Path);
                    //Debug.WriteLine("{0} {1}", Domain, Path);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            **/

            listView1.Items.Clear();
            listView2.Items.Clear();
            manifest = null;


            try
            {
                iPhoneBackup backup = (iPhoneBackup)comboBox1.SelectedItem;

                // backup iTunes 9.2+
                {
                    byte[] xml;
                    DLL.bplist2xml(Path.Combine(backup.path, "Manifest.plist"), out xml, false);

                    if (xml != null)
                    {
                        using (StreamReader sr = new StreamReader(new MemoryStream(xml)))
                        {
                            xdict dd = xdict.open(sr);

                            if (dd != null)
                            {
                                manifest = new iPhoneManifestData();

                                HashSet<string> files = new HashSet<string>();

                                parseApplications92(dd, files);

                                //parseFiles(dd, files);
                            }
                        }

                        return;
                    }
                }


                // backup iTunes 8.2+ et <= 9.1.1
                xdict d = xdict.open(Path.Combine(backup.path, "Manifest.plist"));

                string data;

                if (d != null && d.findKey("Data", out data))
                {
                    byte[] bdata = Convert.FromBase64String(data);
                    byte[] xml;

                    DLL.bplist2xml(bdata, bdata.Length, out xml, false);

                    if (xml != null)
                    {
                        using (StreamReader sr = new StreamReader(new MemoryStream(xml)))
                        {
                            xdict dd = xdict.open(sr);

                            if (dd != null)
                            {
                                manifest = new iPhoneManifestData();

                                HashSet<string> files = new HashSet<string>();

                                parseApplications(dd, files);

                                parseFiles(dd, files);
                            }
                        }
                    }

                    return;
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.InnerException.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }


        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            iPhoneApp app = (iPhoneApp)listView1.FocusedItem.Tag;

            listView2.Items.Clear();

            if (app.Files == null)
                return;

            foreach (string f in app.Files)
            {
                //Debug.WriteLine("{0} {1}", f, "");
                iPhoneFile ff = manifest.Files[f];

                iPhoneBackup backup = (iPhoneBackup)comboBox1.SelectedItem;

                if (ff.Path == null)
                {
                    string domain;
                    DLL.mdinfo(Path.Combine(backup.path, f + ".mdinfo"), out domain, out ff.Path);
                    if (ff.Path == null)
                        ff.Path = "N/A";
                }

                ListViewItem lvi = new ListViewItem();
                lvi.Tag = ff;
                lvi.Text = ff.Path;
                lvi.SubItems.Add(ff.FileLength.ToString());
                lvi.SubItems.Add(ff.ModificationTime);
                lvi.SubItems.Add(ff.Domain);
                lvi.SubItems.Add(f);
                listView2.Items.Add(lvi);
            }

        }


        private void listView2_DoubleClick(object sender, EventArgs e)
        {
            iPhoneBackup backup = (iPhoneBackup)comboBox1.SelectedItem;
            iPhoneFile file = (iPhoneFile)listView2.FocusedItem.Tag;

            string argument = @"/select, """ + Path.Combine(backup.path, file.Key + ".mddata") + @"""";

            System.Diagnostics.Process.Start("explorer.exe", argument);
        }


        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            switch (listView1.Sorting)
            {
                case SortOrder.None: listView1.Sorting = SortOrder.Ascending; break;
                case SortOrder.Ascending: listView1.Sorting = SortOrder.Descending; break;
                case SortOrder.Descending: listView1.Sorting = SortOrder.None; break;
            }
        }


        private void listView2_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Déterminer si la colonne sélectionnée est déjà la colonne triée.
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                // Inverser le sens de tri en cours pour cette colonne.
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Définir le numéro de colonne à trier ; par défaut sur croissant.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }

            // Procéder au tri avec les nouvelles options.
            this.listView2.Sort();
        }

        private void listView2_ItemDrag(object sender, ItemDragEventArgs e)
        {
            string[] filenames;

            iPhoneBackup backup = (iPhoneBackup)comboBox1.SelectedItem;

            int k = 0;
            //Array.Resize(ref filenames, listView2.SelectedItems.Count);
            filenames = new string[listView2.SelectedItems.Count];

            foreach (ListViewItem i in listView2.SelectedItems)
            {
                filenames[k++] = Path.Combine(backup.path, ((iPhoneFile)i.Tag).Key + ".mddata");
            }

            listView2.DoDragDrop(new DataObject(DataFormats.FileDrop, filenames), DragDropEffects.Copy);
        }


        /*
        private void listView2_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Peter.ShellContextMenu ctxMnu = new Peter.ShellContextMenu();
                FileInfo[] arrFI = new FileInfo[1];
                arrFI[0] = new FileInfo(@"c:\temp\a.xml");
                ctxMnu.ShowContextMenu(arrFI, this.PointToScreen(new Point(e.X, e.Y)));
            }
        }
        */
    }

}
