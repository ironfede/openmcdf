using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties
{
    internal enum Behavior
    {
        CaseSensitive, CaseInsensitive
    }

    internal class PropertyContext
    {

        public Int32 CodePage { get; set; }
        public Behavior Behavior { get; set; }
        public UInt32 Locale { get; set; }
        //public Dictionary<int, string> PropertyDictionary { get; set; }

        public PropertyContext()
        {
            //PropertyDictionary = new Dictionary<int, string>();
        }
    }

    internal enum PropertyDimensions
    {
        IsScalar, IsVector, IsArray
    }
}
