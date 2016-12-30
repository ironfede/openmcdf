using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties.Interfaces
{
    public interface IBinarySerializable
    {
        void Write(BinaryWriter bw);
        void Read(BinaryReader br);
    }
}
