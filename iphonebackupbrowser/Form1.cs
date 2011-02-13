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
using System.IO.Compression;
using System.Threading;

using mbdbdump;

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

        #endregion


        private List<iPhoneBackup> backups = new List<iPhoneBackup>();
        private iPhoneManifestData manifest;
        private mbdb.MBFileRecord[] files92;

        string appsDirectory;
        private Dictionary<string, iPhoneIPA> appsCatalog;

        private ListViewColumnSorter lvwColumnSorter;


        public Form1()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            listView1.Columns.Add("Display Name", 200);
            listView1.Columns.Add("Name", 200);
            listView1.Columns.Add("Files", 50, HorizontalAlignment.Right);
            listView1.Columns.Add("Size", 90, HorizontalAlignment.Right);
            listView1.Columns.Add("App Size", 90, HorizontalAlignment.Right);

            listView2.Columns.Add("Name", 400);
            listView2.Columns.Add("Size");
            listView2.Columns.Add("Date", 130);
            listView2.Columns.Add("Domain", 300);
            listView2.Columns.Add("Key", 250);

            // Créer une instance d'une méthode de trie de la colonne ListView et l'attribuer
            // au contrôle ListView.            
            lvwColumnSorter = new ListViewColumnSorter();
            listView2.ListViewItemSorter = lvwColumnSorter;

            LoadManifests();
            
            // asynchronous load .ipa
            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();

            backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
            
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;

            backgroundWorker1.RunWorkerAsync();
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
                comboBox1_SelectedIndexChanged(null, null);
        }

        // This event handler updates the progress bar.
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.Text = "iPhone Backup Browser - Loading IPA " + e.ProgressPercentage.ToString("N0") + "%"; // +(string)e.UserState;
        }


        private void LoadIPAs(BackgroundWorker worker, DoWorkEventArgs e)
        {
            Dictionary<string, iPhoneIPA> apps = new Dictionary<string, iPhoneIPA>();
            string appsDirectory;

            try
            {
                appsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                appsDirectory = Path.Combine(appsDirectory, "iTunes", "Mobile Applications");

                DirectoryInfo d = new DirectoryInfo(appsDirectory);

                FileInfo[] fi = d.GetFiles("*.ipa");

                int lastprogress = -1;

                for (int i = 0; i < fi.Length; ++i)
                {
                    if (worker.CancellationPending) e.Cancel = true;

                    //System.Threading.Thread.Sleep(20);

                    int progress = (i * 100 / fi.Length);
                    if (lastprogress != progress)
                    {
                        lastprogress = progress;
                        worker.ReportProgress(progress, fi[i].Name);
                    }

                    using (ZipStorer zip = ZipStorer.Open(fi[i].FullName, FileAccess.Read))
                    {
                        iPhoneIPA ipa = new iPhoneIPA();

                        ipa.fileName = fi[i].Name;

                        foreach (ZipStorer.ZipFileEntry f in zip.ReadCentralDir())
                        {
                            if (worker.CancellationPending) e.Cancel = true;

                            // computes the files total size
                            ipa.totalSize += f.FileSize;

                            // analyzes the app metadata
                            if (f.FilenameInZip == "iTunesMetadata.plist")
                            {
                                MemoryStream mem = new MemoryStream();
                                zip.ExtractFile(f, mem);

                                ipa.softwareVersionBundleId = f.Comment;

                                if (mem.Length <= 8) continue;

                                byte[] xml = mem.ToArray();

                                // iTunesMetadata.plist is actually a binary plist
                                if (xml[0] == 'b' && xml[1] == 'p')
                                    DLL.bplist2xml(mem.ToArray(), (int)mem.Length, out xml, false);

                                if (xml != null)
                                {
                                    using (StreamReader sr = new StreamReader(new MemoryStream(xml)))
                                    {
                                        xdict dd = xdict.open(sr);

                                        if (dd != null)
                                        {
                                            dd.findKey("softwareVersionBundleId", out ipa.softwareVersionBundleId);
                                            dd.findKey("itemName", out ipa.itemName);                                                
                                        }                                       
                                    }
                                }
                            }
                        }

                        // if we have found the app id
                        if (ipa.softwareVersionBundleId != null)
                        {
                            //Debug.WriteLine("{0} {1}", fi[i].Name, softwareVersionBundleId);

                            // if the BundleId has been already found in some .ipa, we assume this one is more recent
                            if (apps.ContainsKey(ipa.softwareVersionBundleId))
                                apps[ipa.softwareVersionBundleId] = ipa;
                            else
                                apps.Add(ipa.softwareVersionBundleId, ipa);
                        }
                    }

                }
            }
            catch (DirectoryNotFoundException /*ex*/)
            {
                apps = null;
                appsDirectory = null;
                //MessageBox.Show(ex.ToString());
            }

            this.appsCatalog = apps;
            this.appsDirectory = appsDirectory;
        }


        private void LoadManifests()
        {
            backups.Clear();
            comboBox1.Items.Clear();
            listView1.Items.Clear();
            listView2.Items.Clear();
         
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
                        f.ModificationTime = (string)q.item;
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

        private void parseAll92(xdict di, HashSet<string> files)
        {
            dict sd;

            if (!di.findKey("Applications", out sd))
                return;

            Dictionary<string, appFiles> filesByDomain = new Dictionary<string, appFiles>();
            
            for (int i = 0; i < files92.Length; ++i)
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

                iPhoneIPA ipa = null;
                if (appsCatalog != null)
                {
                    appsCatalog.TryGetValue(app.Identifier, out ipa);
                }

                ListViewItem lvi = new ListViewItem();
                lvi.Tag = app;
                lvi.Text = app.DisplayName;
                lvi.SubItems.Add(ipa != null ? ipa.itemName : app.Name);
                lvi.SubItems.Add(app.Files != null ? app.Files.Count.ToString() : "N/A");                
                //lvi.SubItems.Add(app.Identifier != null ? app.Identifier : "N/A");
                lvi.SubItems.Add(app.FilesLength.ToString("N0"));

                if (ipa != null)
                    lvi.SubItems.Add(ipa.totalSize.ToString("N0"));

                listView1.Items.Add(lvi);
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


                ListViewItem lvi = new ListViewItem();
                lvi.Tag = system;
                lvi.Text = system.DisplayName;
                lvi.SubItems.Add(system.Name);
                lvi.SubItems.Add(system.Files != null ? system.Files.Count.ToString() : "N/A");
                //lvi.SubItems.Add(system.Identifier != null ? system.Identifier : "N/A");
                lvi.SubItems.Add(system.FilesLength.ToString("N0"));
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
            files92 = null;


            try
            {
                iPhoneBackup backup = (iPhoneBackup)comboBox1.SelectedItem;

                // backup iTunes 9.2+
                if (File.Exists(Path.Combine(backup.path, "Manifest.mbdb")))
                {
                    files92 = mbdbdump.mbdb.ReadMBDB(backup.path, false, true);

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

                                parseAll92(dd, files);
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

                    if (manifest.Files == null)
                    {
                        ff = new iPhoneFile();

                        mbdb.MBFileRecord x = files92[Int32.Parse(f)];

                        ff.Key = x.key;
                        ff.Domain = x.Domain;
                        ff.Path = x.Path;
                        ff.ModificationTime = x.aTime.ToString();
                        ff.FileLength = x.FileLength;
                    }
                    else
                    {
                        //Debug.WriteLine("{0} {1}", f, "");
                        ff = manifest.Files[f];

                        iPhoneBackup backup = (iPhoneBackup)comboBox1.SelectedItem;

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
                    lvi.SubItems.Add(ff.FileLength.ToString());
                    lvi.SubItems.Add(ff.ModificationTime);
                    lvi.SubItems.Add(ff.Domain);
                    lvi.SubItems.Add(ff.Key);

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
            iPhoneBackup backup = (iPhoneBackup)comboBox1.SelectedItem;
            iPhoneFile file = (iPhoneFile)listView2.FocusedItem.Tag;

            string ext = "";
            if (files92 == null)
                ext = ".mddata";

            string argument = @"/select, """ + Path.Combine(backup.path, file.Key + ext) + @"""";

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

            string ext = "";
            if (files92 == null) ext = ".mddata";

            iPhoneBackup backup = (iPhoneBackup)comboBox1.SelectedItem;

            int k = 0;
            filenames = new string[listView2.SelectedItems.Count];

            foreach (ListViewItem i in listView2.SelectedItems)
            {
                filenames[k++] = Path.Combine(backup.path, ((iPhoneFile)i.Tag).Key + ext);
            }

            listView2.DoDragDrop(new DataObject(DataFormats.FileDrop, filenames), DragDropEffects.Copy);
        }


        private void button1_Click(object sender, EventArgs e)
        {
            iPhoneBackup backup = (iPhoneBackup)comboBox1.SelectedItem;

            if (backup == null)
                return;

            System.Diagnostics.Process prc = new System.Diagnostics.Process();
            prc.StartInfo.FileName = backup.path;
            prc.Start();
        }


        private void button2_Click(object sender, EventArgs e)
        {
            LoadManifests();
        }


        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            iPhoneApp app = (iPhoneApp)listView1.FocusedItem.Tag;

            if (app == null) return;

            iPhoneIPA ipa;
            if (!appsCatalog.TryGetValue(app.Identifier, out ipa))
                return;

            string argument = @"/select, """ + Path.Combine(appsDirectory, ipa.fileName) + @"""";
            System.Diagnostics.Process.Start("explorer.exe", argument);
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
