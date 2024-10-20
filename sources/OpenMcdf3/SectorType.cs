using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMcdf3
{
    internal enum SectorType : ushort
    {
        Normal,
        Mini,
        FAT,
        DIFAT,
        RangeLockSector,
        Directory
    }
}
