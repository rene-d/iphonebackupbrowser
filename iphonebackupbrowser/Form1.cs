// convertir DTD -> XSD : http://www.hitsw.com/xml_utilites/

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Forms;
using mbdbdump;
using System.Data.SQLite;


namespace iphonebackupbrowser
{
    public partial class Form1 : Form
    {
        #region ListViewColumnSorter

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
            private IComparer ObjectCompare;

            /// <summary>
            /// Constructeur de classe.  Initializes various elements
            /// </summary>
            public ListViewColumnSorter()
            {
                // Initialise la colonne sur '0'
                ColumnToSort = 0;

                // Initialise l'ordre de tri sur 'aucun'
                OrderOfSort = SortOrder.Ascending;

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

                if (((ListViewItem)x).SubItems.Count <= ColumnToSort)
                    return 0;
                
                // Envoie les objets à comparer aux objets ListViewItem                
                ListViewItem.ListViewSubItem siX = ((ListViewItem)x).SubItems[ColumnToSort];
                ListViewItem.ListViewSubItem siY = ((ListViewItem)y).SubItems[ColumnToSort];

                // Compare les deux éléments
                if (siX.Tag != null || siY.Tag != null)
                {
                    long a, b;

                    if (siX.Tag != null) a = (long)(siX.Tag); else a = 0;
                    if (siY.Tag != null) b = (long)(siY.Tag); else b = 0;

                    if (a < b)
                        compareResult = -1;
                    else if (a > b)
                        compareResult = 1;
                    else
                        compareResult = 0;
                }
                else
                {
                    compareResult = ObjectCompare.Compare(siX.Text, siY.Text);
                }

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

        #endregion


        private List<iPhoneBackup> backups = new List<iPhoneBackup>();

        // manifest data of old backups
        private iPhoneManifestData manifest;

        // file list for new backups
        private List<mbdb.MBFileRecord> files92;

        // apps from iTunes
        //private string appsDirectory;
        //private Dictionary<string, iPhoneIPA> appsCatalog;
        private iPhoneApps appsCatalog;

        private ListViewColumnSorter lvwColumnSorter1, lvwColumnSorter2;

        // the current displayed backup
        private iPhoneBackup currentBackup = null;


        public Form1()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            this.toolStrip2.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            
            
            listView1.Columns.Add("Display Name", 200);
            listView1.Columns.Add("Name", 200);
            listView1.Columns.Add("Files", 50, HorizontalAlignment.Right);
            listView1.Columns.Add("Size", 90, HorizontalAlignment.Right);
            listView1.Columns.Add("App Size", 90, HorizontalAlignment.Right);

            listView2.Columns.Add("Name", 400);
            listView2.Columns.Add("Size", 90, HorizontalAlignment.Right);
            listView2.Columns.Add("Date", 130);
            listView2.Columns.Add("Domain", 300);
            listView2.Columns.Add("Key", 250);

            buttonCSVExport.Enabled = false;
            
            // Créer une instance d'une méthode de trie de la colonne ListView et l'attribuer
            // au contrôle ListView.            
            lvwColumnSorter1 = new ListViewColumnSorter();
            listView1.ListViewItemSorter = lvwColumnSorter1;
            lvwColumnSorter2 = new ListViewColumnSorter();
            listView2.ListViewItemSorter = lvwColumnSorter2;

            LoadManifests();
            
            // asynchronous load .ipa
            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();

            backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
            
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;

            backgroundWorker1.RunWorkerAsync();

            toolStripButton2.Enabled = false;

            searchComboBox.Visible = false;
            toolStripButton2.Visible = false;
        }


        // This BackgroundWorker is used to demonstrate the 
        // preferred way of performing asynchronous operations.
        private BackgroundWorker backgroundWorker1;


         // This event handler is where the time-consuming work is done.
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            LoadIPAs(worker, e);
        }

        // This event handler deals with the results of the background operation.
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Text = "iPhone Backup Browser";
            backgroundWorker1 = null;

            if (comboBox1.SelectedIndex != -1)
                loadCurrentBackup();
        }

        // This event handler updates the progress bar.
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.Text = "iPhone Backup Browser - Loading IPA " + e.ProgressPercentage.ToString("N0") + "%"; // +(string)e.UserState;
        }


        private void LoadIPAs(BackgroundWorker worker, DoWorkEventArgs e)
        {
            appsCatalog = new iPhoneApps();
            appsCatalog.LoadIPAs(worker, e);
        }

        
        private void LoadManifests()
        {
            backups.Clear();
            comboBox1.Items.Clear();
            listView1.Items.Clear();
            listView2.Items.Clear();
            buttonCSVExport.Enabled = false;
            
            string s = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            s = Path.Combine(s, "Apple Computer", "MobileSync", "Backup");

            DirectoryInfo d = new DirectoryInfo(s);

            foreach (DirectoryInfo sd in d.EnumerateDirectories())
            {
                LoadManifest(sd.FullName);            
            }

            foreach (iPhoneBackup b in backups)
            {
                b.index = comboBox1.Items.Add(b);
            }

            comboBox1.Items.Add("<Open from a custom directory>");            
        }


        private iPhoneBackup LoadManifest(string path)
        {
            iPhoneBackup backup = null;

            string filename = Path.Combine(path, "Info.plist");

            try
            {
                xdict dd = xdict.open(filename);

                if (dd != null)
                {
                    backup = new iPhoneBackup();

                    backup.path = path;

                    foreach (xdictpair p in dd)
                    {
                        if (p.item.GetType() == typeof(string))
                        {
                            switch (p.key)
                            {
                                case "Device Name": backup.DeviceName = (string)p.item; break;
                                case "Display Name": backup.DisplayName = (string)p.item; break;
                                case "Last Backup Date":                                    
                                    DateTime.TryParse((string)p.item, out backup.LastBackupDate);
                                    break;
                            }
                        }
                    }

                    backups.Add(backup);
                    backups.Sort(iPhoneBackup.SortByDate);
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

            return backup;
        }


        #region parse iTunes -> 9.1 (deprecated)

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
                //lvi.SubItems.Add(app.Identifier != null ? app.Identifier : "N/A");
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
                        f.ModificationTime = DateTime.Parse((string)q.item);
                    else if (q.key == "FileLength")
                        f.FileLength = Convert.ToInt64((string)q.item);
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
                //lvi.SubItems.Add(system.Identifier != null ? system.Identifier : "N/A");                
                listView1.Items.Add(lvi);
            }
        }
        #endregion


        private class appFiles
        {
            public List<int> indexes = new List<int>();
            public long FilesLength = 0;

            public void add(int index, long length)
            {
                indexes.Add(index);
                FilesLength += length;
            }
        }


        private void addApp(iPhoneApp app)
        {
            iPhoneIPA ipa = null;

            
            if (appsCatalog != null)
            {
                appsCatalog.TryGetValue(app.Identifier, out ipa);
            }

            ListViewItem lvi = new ListViewItem();
            lvi.Tag = app;
            lvi.Text = app.DisplayName;
            lvi.SubItems.Add(ipa != null ? ipa.itemName : app.Name);
            lvi.SubItems.Add(app.Files != null ? app.Files.Count.ToString("N0") : "N/A");
            lvi.SubItems.Add(app.FilesLength.ToString("N0"));
            lvi.SubItems.Add(ipa != null ? ipa.totalSize.ToString("N0") : "");

            lvi.SubItems[2].Tag = (long)(app.Files != null ? app.Files.Count : 0);
            lvi.SubItems[3].Tag = (long)app.FilesLength;
            lvi.SubItems[4].Tag = (long)(ipa != null ? ipa.totalSize : 0);

            listView1.Items.Add(lvi);
        }


        private void parseAll92(xdict mbdb)
        {
            dict sd;

            if (!mbdb.findKey("Applications", out sd))
                return;

            Dictionary<string, appFiles> filesByDomain = new Dictionary<string, appFiles>();
            
            for (int i = 0; i < files92.Count; ++i)
            {
                if ((files92[i].Mode & 0xF000) == 0x8000)
                {
                    string d = files92[i].Domain;
                    if (!filesByDomain.ContainsKey(d))                    
                        filesByDomain.Add(d, new appFiles());
                    
                    filesByDomain[d].add(i, files92[i].FileLength);                    
                }
            }


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

                if (filesByDomain.ContainsKey("AppDomain-" + app.Key))
                {
                    app.Files = new List<String>();

                    foreach (int i in filesByDomain["AppDomain-" + app.Key].indexes)
                    {
                        app.Files.Add(i.ToString());
                    }
                    app.FilesLength = filesByDomain["AppDomain-" + app.Key].FilesLength;

                    filesByDomain.Remove("AppDomain-" + app.Key);
                }

                addApp(app);
            }


            {
                iPhoneApp system = new iPhoneApp();

                system.Name = "System";
                system.DisplayName = "---";
                system.Identifier = "---";
                system.Container = "---";
                system.Files = new List<String>();

                foreach (appFiles i in filesByDomain.Values)
                {
                    foreach (int j in i.indexes)
                    {
                        system.Files.Add(j.ToString());
                    }
                    system.FilesLength = i.FilesLength;
                }


                addApp(system);

                /*
                ListViewItem lvi = new ListViewItem();
                lvi.Tag = system;
                lvi.Text = system.DisplayName;
                lvi.SubItems.Add(system.Name);
                lvi.SubItems.Add(system.Files != null ? system.Files.Count.ToString("N0") : "N/A");
                lvi.SubItems.Add(system.FilesLength.ToString("N0"));
                lvi.SubItems.Add("---");

                lvi.SubItems[2].Tag = (long)(system.Files != null ? system.Files.Count : 0);
                lvi.SubItems[3].Tag = (long)system.FilesLength;
                lvi.SubItems[4].Tag = (long)0;

                listView1.Items.Add(lvi);
                */
            }
        }


        private void loadCurrentBackup()
        {
            if (currentBackup == null)
                return;

            button1.ToolTipText = currentBackup.path;

            listView1.Items.Clear();
            listView2.Items.Clear();
            buttonCSVExport.Enabled = false;
            manifest = null;
            files92 = null;

            try
            {
                iPhoneBackup backup = currentBackup;

                // backup iTunes 9.2+
                if (File.Exists(Path.Combine(backup.path, "Manifest.mbdb")))
                {
                    files92 = mbdbdump.mbdb.ReadMBDB(backup.path);

                    byte[] xml;
                    DLL.bplist2xml(Path.Combine(backup.path, "Manifest.plist"), out xml, false);

                    if (xml != null)
                    {
                        using (StreamReader sr = new StreamReader(new MemoryStream(xml)))
                        {
                            xdict dd = xdict.open(sr);

                            if (dd != null)
                            {
                                parseAll92(dd);
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
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }


        private void listView1_Click(object sender, EventArgs e)
        {
            iPhoneApp app = (iPhoneApp)listView1.FocusedItem.Tag;

            listView2.Items.Clear();
            buttonCSVExport.Enabled = true;

            if (app.Files == null)
                return;

            listView2.BeginUpdate();
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                ListViewItem[] lvic = new ListViewItem[app.Files.Count];
                int idx = 0;

                foreach (string f in app.Files)
                {
                    iPhoneFile ff;

                    if (manifest == null)
                    {
                        ff = new iPhoneFile();

                        mbdb.MBFileRecord x = files92[Int32.Parse(f)];

                        ff.Key = x.key;
                        ff.Domain = x.Domain;
                        ff.Path = x.Path;
                        ff.ModificationTime = x.aTime;
                        ff.FileLength = x.FileLength;
                    }
                    else
                    {
                        //Debug.WriteLine("{0} {1}", f, "");
                        ff = manifest.Files[f];

                        iPhoneBackup backup = currentBackup;

                        if (ff.Path == null)
                        {
                            string domain;
                            DLL.mdinfo(Path.Combine(backup.path, f + ".mdinfo"), out domain, out ff.Path);
                            if (ff.Path == null)
                                ff.Path = "N/A";
                        }
                    }


                    ListViewItem lvi = new ListViewItem();
                    lvi.Tag = ff;
                    lvi.Text = ff.Path;
                    lvi.SubItems.Add(ff.FileLength.ToString("N0"));
                    lvi.SubItems.Add(ff.ModificationTime.ToString());
                    lvi.SubItems.Add(ff.Domain);
                    lvi.SubItems.Add(ff.Key);

                    lvi.SubItems[1].Tag = (long)ff.FileLength;
                    lvi.SubItems[2].Tag = (long)ff.ModificationTime.ToBinary();

                    lvic[idx++] = lvi;
                }

                listView2.Items.AddRange(lvic); 
            }
            finally
            {
                listView2.EndUpdate();
                Cursor.Current = Cursors.Default;
            }
        }


        private void listView2_DoubleClick(object sender, EventArgs e)
        {
            iPhoneBackup backup = currentBackup;
            iPhoneFile file = (iPhoneFile)listView2.FocusedItem.Tag;

            string ext = "";
            if (files92 == null)
                ext = ".mddata";

            string argument = @"/select, """ + Path.Combine(backup.path, file.Key + ext) + @"""";

            System.Diagnostics.Process.Start("explorer.exe", argument);
        }


        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Déterminer si la colonne sélectionnée est déjà la colonne triée.
            if (e.Column == lvwColumnSorter1.SortColumn)
            {
                // Inverser le sens de tri en cours pour cette colonne.
                if (lvwColumnSorter1.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter1.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter1.Order = SortOrder.Ascending;
                }                
            }
            else
            {
                // Définir le numéro de colonne à trier ; par défaut sur croissant.
                lvwColumnSorter1.SortColumn = e.Column;
                lvwColumnSorter1.Order = SortOrder.Ascending;                
            }                        

            // Procéder au tri avec les nouvelles options.
            this.listView1.Sort();
        }


        private void listView2_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Déterminer si la colonne sélectionnée est déjà la colonne triée.
            if (e.Column == lvwColumnSorter2.SortColumn)
            {
                // Inverser le sens de tri en cours pour cette colonne.
                if (lvwColumnSorter2.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter2.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter2.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Définir le numéro de colonne à trier ; par défaut sur croissant.
                lvwColumnSorter2.SortColumn = e.Column;
                lvwColumnSorter2.Order = SortOrder.Ascending;
            }

            // Procéder au tri avec les nouvelles options.
            this.listView2.Sort();
        }


        private void listView2_ItemDrag(object sender, ItemDragEventArgs e)
        {
            string[] filenames;
            //string[] filenames_real;

            string ext = "";
            if (files92 == null) ext = ".mddata";

            iPhoneBackup backup = currentBackup;

            int k = 0;
            filenames = new string[listView2.SelectedItems.Count];
            //filenames_real = new string[listView2.SelectedItems.Count];

            foreach (ListViewItem i in listView2.SelectedItems)
            {
                filenames[k] = Path.Combine(backup.path, ((iPhoneFile)i.Tag).Key + ext);
                //filenames_real[k] = "";

                ++k;
            }

            listView2.DoDragDrop(new DataObject(DataFormats.FileDrop, filenames), DragDropEffects.Copy);
        }


        // ouvre le répertoire du backup dans l'explorer
        private void button1_Click(object sender, EventArgs e)
        {
            if (currentBackup == null)
                return;

            System.Diagnostics.Process prc = new System.Diagnostics.Process();
            prc.StartInfo.FileName = currentBackup.path;
            prc.Start();
        }



        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            iPhoneApp app = (iPhoneApp)listView1.FocusedItem.Tag;

            if (app == null) return;
            if (appsCatalog == null) return;

            if (appsCatalog == null)
                return;

            iPhoneIPA ipa;
            if (!appsCatalog.TryGetValue(app.Identifier, out ipa))
                return;

            string argument = @"/select, """ + Path.Combine(appsCatalog.appsDirectory, ipa.fileName) + @"""";
            System.Diagnostics.Process.Start("explorer.exe", argument);
        }



        // export the file list to a CSV file
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (listView1.FocusedItem == null) return;
            if (listView1.FocusedItem.Tag == null) return;
            iPhoneApp app = (iPhoneApp)listView1.FocusedItem.Tag;
            
            SaveFileDialog fd = new SaveFileDialog();

            fd.Filter = "CSV file (*.csv)|*.csv|All files (*.*)|*.*";
            fd.FilterIndex = 1;
            fd.RestoreDirectory = true;
            fd.Title = "Export the file list of '" + app.Name + "' application";

            if (fd.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                using (StreamWriter fs = new StreamWriter(fd.FileName))
                {
                    fs.WriteLine("Path;FileLength;ModificationTime;Domain;Key");                    

                    if (manifest.Files == null)
                    {
                        foreach (string f in app.Files)
                        {
                            mbdb.MBFileRecord x = files92[Int32.Parse(f)];

                            fs.Write(x.Path);
                            fs.Write(';');
                            fs.Write(x.FileLength);
                            fs.Write(';');
                            fs.Write(x.aTime.ToString());
                            fs.Write(';');
                            fs.Write(x.Domain);
                            fs.Write(';');
                            fs.Write(x.key);
                            fs.WriteLine();
                        }
                    }
                    else
                    {
                        iPhoneBackup backup = currentBackup;

                        foreach (string f in app.Files)
                        {
                            iPhoneFile ff = manifest.Files[f];

                            if (ff.Path == null)
                            {
                                string domain;
                                DLL.mdinfo(Path.Combine(backup.path, f + ".mdinfo"), out domain, out ff.Path);
                                if (ff.Path == null)
                                    ff.Path = "N/A";
                            }

                            fs.Write(ff.Path);
                            fs.Write(';');
                            fs.Write(ff.FileLength);
                            fs.Write(';');
                            fs.Write(ff.ModificationTime);
                            fs.Write(';');
                            fs.Write(ff.Domain);
                            fs.Write(';');
                            fs.Write(ff.Key);
                            fs.WriteLine();
                        }                        
                    }                    
                }
            }
            finally
            {
            }
        }


        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == -1)
                return;

            if (comboBox1.SelectedItem.GetType() == typeof(iPhoneBackup))
            {
                if (currentBackup == null || currentBackup.index != comboBox1.SelectedIndex)
                {
                    currentBackup = (iPhoneBackup)comboBox1.SelectedItem;
                    loadCurrentBackup();
                }
                return;
            }


            // choose a backup from a non standard directory

            OpenFileDialog fd = new OpenFileDialog();

            //fd.InitialDirectory = "C:\\" ;
            fd.Filter = "iPhone Backup|Info.plist|All files (*.*)|*.*";
            fd.FilterIndex = 1;
            fd.RestoreDirectory = true;
            
            if (fd.ShowDialog() == DialogResult.OK)
            {
                //backups.Clear();
                //comboBox1.Items.Clear();
                //listView1.Items.Clear();
                //listView2.Items.Clear();

                iPhoneBackup b = LoadManifest(Path.GetDirectoryName(fd.FileName));

                if (b != null)
                {
                    b.custom = true;

                    comboBox1.Items.Insert(comboBox1.Items.Count - 1, b);

                    b.index = comboBox1.Items.Count - 2;

                    comboBox1.SelectedIndex = b.index;
                }
            }

            if (comboBox1.SelectedIndex == comboBox1.Items.Count - 1)
            {
                if (currentBackup != null) 
                    comboBox1.SelectedIndex = currentBackup.index;                
                else
                    comboBox1.SelectedIndex = -1;                
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {            
            MessageBox.Show(searchComboBox.Text);

            SearchForm dlg = new SearchForm();

            dlg.ShowDialog(this);
        }

        private void toolStripButton1_Click_1(object sender, EventArgs e)
        {
            (new AboutBox1()).ShowDialog(this);
        }

        private void searchComboBox_TextUpdate(object sender, EventArgs e)
        {
            toolStripButton2.Enabled = searchComboBox.Text != "";
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            LoadManifests();
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            string filename;
            string dest_root;
            string dest;

            string ext = (files92 == null) ? ".mddata" : "";

            iPhoneBackup backup = currentBackup;
            iPhoneApp app = listView1.FocusedItem.Tag as iPhoneApp;

            if (backup == null || app == null)
                return;

            dest_root = dest = Path.Combine(@"C:\temp", backup.DeviceName, app.Name );

            foreach (ListViewItem i in listView2.SelectedItems)
            {
                iPhoneFile f = i.Tag as iPhoneFile;
                if (f !=null)
                {
                    filename = Path.Combine(backup.path, f.Key + ext);

                    dest = Path.Combine(dest_root, f.Path);

                    dest = dest.Normalize(NormalizationForm.FormC);

                    StringBuilder sb = new StringBuilder(dest.Length);
                    char[] ss = Path.GetInvalidPathChars();

                    Array.Sort(ss);

                    foreach (char c in dest)
                    {
                        //System.IO.Path.GetInvalidFileNameChars()
                        if (c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar)
                        {
                            sb.Append(Path.DirectorySeparatorChar);
                        }
                        else if (c == Path.VolumeSeparatorChar)
                        {
                            sb.Append(c);
                        }
                        else if (Array.BinarySearch(ss, c) >= 0)
                        {
                            sb.Append('_');
                        }
                        else
                        {
                            sb.Append(c);
                        }
                    }

                    dest = sb.ToString();
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    File.Copy(filename, dest, true);
                }
            }

        }

    }
}
