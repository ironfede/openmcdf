using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties
{
    public enum VTPropertyType : ushort
    {
        VT_EMPTY = 0x0000,
        VT_NULL = 0x0001,
        VT_I2 = 0x0002,
        VT_I4 = 0x0003,
        VT_R4 = 0x0004,
        VT_R8 = 0x0005,
        VT_CY = 0x0006,
        VT_DATE = 0x0007,
        VT_BSTR = 0x0008,
        VT_ERROR = 0x000A,
        VT_BOOL = 0x000B,
        VT_VARIANT_VECTOR_HEADER = 0x000C, // NOT NORMATIVE, helper enum value
        VT_DECIMAL = 0x000E,
        VT_I1 = 0x0010,
        VT_UI1 = 0x0011,
        VT_UI2 = 0x0012,
        VT_UI4 = 0x0013,
        VT_I8 = 0x0014,         // MUST be an 8-byte signed integer. 
        VT_UI8 = 0x0015,        // MUST be an 8-byte unsigned integer. 
        VT_INT = 0x0016,        // MUST be a 4-byte signed integer. 
        VT_UINT = 0x0017,       // MUST be a 4-byte unsigned integer. 
        VT_LPSTR = 0x001E,      // MUST be a CodePageString. 
        VT_LPWSTR = 0x001F,     // MUST be a UnicodeString. 
        VT_FILETIME = 0x0040,   // MUST be a FILETIME (Packet Version). 
        VT_BLOB = 0x0041,       // MUST be a BLOB. 
        VT_STREAM = 0x0042,     // MUST be an IndirectPropertyName. The storage representing the (non-simple) property set MUST have a stream element with this name. 
        VT_STORAGE = 0x0043,    // MUST be an IndirectPropertyName. The storage representing the (non-simple) property set MUST have a storage element with this name. 
        VT_STREAMED_OBJECT = 0x0044, // MUST be an IndirectPropertyName. The storage representing the (non-simple) property set MUST have a stream element with this name. 
        VT_STORED_OBJECT = 0x0045, // MUST be an IndirectPropertyName. The storage representing the (non-simple) property set MUST have a storage element with this name. 
        VT_BLOB_OBJECT = 0x0046, //MUST be a BLOB. 
        VT_CF = 0x0047,         //MUST be a ClipboardData. 
        VT_CLSID = 0x0048,       //MUST be a GUID (Packet Version)
        VT_VERSIONED_STREAM = 0x0049,       //MUST be a Verisoned Stream, NOT allowed in simple property
        VT_VECTOR_LPSTR = VT_VECTOR_HEADER | VT_LPSTR,
        VT_VECTOR_HEADER = 0x1000,  //--- NOT NORMATIVE
        VT_ARRAY_HEADER = 0x2000,  //--- NOT NORMATIVE
    }
}
