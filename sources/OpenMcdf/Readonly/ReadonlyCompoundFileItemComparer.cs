using System.Collections.Generic;

namespace OpenMcdf
{
    internal class ReadonlyCompoundFileItemComparer : IComparer<ReadonlyCompoundFileItem>
    {
        public int Compare(ReadonlyCompoundFileItem x, ReadonlyCompoundFileItem y)
        {
            // X CompareTo Y : X > Y --> 1 ; X < Y  --> -1
            return (x.DirEntry.CompareTo(y.DirEntry));

            //Compare X < Y --> -1
        }
    }
}