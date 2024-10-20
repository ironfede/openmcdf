using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace OpenMcdf3
{
    public class CompoundFile
    {
        private Header header;
        private FAT fat;
        private Dictionary<int, List<Sector>> cachedChains = new Dictionary<int, List<Sector>>();
        private List<DirectoryEntry> directoryEntries = new List<DirectoryEntry>();



        public void WriteToChain(int sid, byte[] data, int position, int count)
        {
            if (!cachedChains.TryGetValue(sid, out List<Sector> streamChain))
            {
                streamChain = fat.GetSectorChain(sid, SectorType.Normal);
            }


            StreamView sv = new StreamView(streamChain, fat.SectorSize);
            sv.Seek(position, SeekOrigin.Begin);
            sv.Write(data, 0, count);

        }
    }
}
