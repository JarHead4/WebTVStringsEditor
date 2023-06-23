using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebTVDATEditor
{
    public class WebTVStringTableEntry
    {
        public string Data;
        public int FileLocation;

        public WebTVStringTableEntry(string data, int location)
        {
            this.Data = data;
            this.FileLocation = location;
        }
    }

    public enum WebTVStringEncoding
    {
        None,
        ASCII,
        ShiftJIS
    }
}
