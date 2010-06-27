// MBDB/MBDX decoder
// René DEVICHI 2010

using System;
using System.Text;
using System.IO;


namespace mbdbdump
{


    class Program
    {
        static void Main(string[] args)
        {
            mbdb.ReadMBDB(@"C:\temp", true, true);
        }
    }

}
