using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;


namespace bplistui
{

    class DLL
    {
        [DllImport("bplist.dll", EntryPoint = "test", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int bplist2xml(byte[] ptr, int len, out string xml, bool useOpenStepEpoch);

        [DllImport("bplist.dll", EntryPoint = "mdinfo", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern int mdinfo(string filename, out string Domain, out string Path);
    }


    class iPhoneBackup
    {
        public string DeviceName;
        public string DisplayName;
        public string LastBackupDate;

        public string path;                 // chemin du backup

        public override string ToString()
        {
            return DisplayName + " (" + LastBackupDate + ")";
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
    };


    class iPhoneFile
    {
        public string Key;                  
        public string Domain;
        public int FileLength;
        public string ModificationTime;
        public string Path;                 // information issue de .mdinfo
    };


    class iPhoneManifestData
    {
        //public List<iPhoneApp> Applications;
        public Dictionary<string, iPhoneFile> Files;
    };
}
