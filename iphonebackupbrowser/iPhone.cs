using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;


namespace iphonebackupbrowser
{

    //
    // the C++ service DLL
    //
    class DLL
    {
        [DllImport("bplist.dll", EntryPoint = "bplist2xml_buffer", CallingConvention = CallingConvention.StdCall)]
        private static extern int bplist2xml_(byte[] ptr, int len, out IntPtr xml_ptr, bool useOpenStepEpoch);

        [DllImport("bplist.dll", EntryPoint = "bplist2xml_file", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern int bplist2xml_(string filename, out IntPtr xml_ptr, bool useOpenStepEpoch);

        [DllImport("bplist.dll", EntryPoint = "mdinfo", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern int mdinfo(string filename, out string Domain, out string Path);

        [DllImport("ibbsearch.dll", EntryPoint = "search", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern void search(string filename, string pattern, out string results);

        // En C#, string et byte[] ne représentent pas la même chose: il y a la notion d'encoding dans une string,
        // (c'est forcément de l'Unicode), même marshalée depuis const char* d'une DLL non managée, qui est forcément
        // de l'ANSI.
        // Et ce n'est pas plus mal ainsi !
        public static int bplist2xml(byte[] ptr, int len, out byte[] xml, bool useOpenStepEpoch)
        {
            int xml_size;
            IntPtr xml_ptr;

            xml_size = bplist2xml_(ptr, len, out xml_ptr, useOpenStepEpoch);

            if (xml_size != 0)
            {
                xml = new byte[xml_size];
                Marshal.Copy(xml_ptr, xml, 0, xml_size);
                Marshal.FreeCoTaskMem(xml_ptr);
            }
            else
            {
                xml = null;
            }

            return xml_size;
        }


        public static int bplist2xml(string filename, out byte[] xml, bool useOpenStepEpoch)
        {
            int xml_size;
            IntPtr xml_ptr;

            xml_size = bplist2xml_(filename, out xml_ptr, useOpenStepEpoch);

            if (xml_size != 0)
            {
                xml = new byte[xml_size];
                Marshal.Copy(xml_ptr, xml, 0, xml_size);
                Marshal.FreeCoTaskMem(xml_ptr);
            }
            else
            {
                xml = null;
            }

            return xml_size;
        }
    }


    //
    // backup information retrieved from Info.plist
    //
    class iPhoneBackup
    {
        public string DeviceName;
        public string DisplayName;
        public DateTime LastBackupDate;     // originally a string

        public string path;                 // backup path

        public override string ToString()
        {
            string str = DisplayName + " (" + LastBackupDate + ")";
            if (custom) str = str + " *";
            return str;
        }

        public bool custom = false;         // backup loaded from a custom directory
        public int index;                   // index in the combobox control

        // delegate to sort backups (newer first)
        public static int SortByDate(iPhoneBackup a, iPhoneBackup b)
        {
            return b.LastBackupDate.CompareTo(a.LastBackupDate);
        }
    };


    class iPhoneApp
    {
        public string Key;
        public string DisplayName;          // CFBundleDisplayName
        public string Name;                 // CFBundleName
        public string Identifier;           // CFBundleIdentifier
        public string Container;            // le chemin d'install sur l'iPhone
        public List <String> Files;
        public long FilesLength;            // taille totale des fichiers
    };


    class iPhoneFile
    {
        public string Key;                  
        public string Domain;
        public long FileLength;
        public DateTime ModificationTime;   // initialement: string
        public string Path;                 // information issue de .mdinfo
    };


    class iPhoneManifestData
    {
        //public List<iPhoneApp> Applications;
        public Dictionary<string, iPhoneFile> Files;
    };


    class iPhoneIPA
    {
        public string softwareVersionBundleId;      // identifier
        public string itemName;                     // name of the app
        public string fileName;                     // .ipa archive name
        public uint totalSize = 0;                  // uncompressed size
    };
}
