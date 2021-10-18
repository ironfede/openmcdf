using System.Collections.Generic;
using System.IO;

namespace OpenMcdf
{
    class SectorList
    {
        public SectorList(List<int> idIndexList, Stream sourceStream, int sectorSize, SectorType type)
        {
            IdIndexList = idIndexList;
            SourceStream = sourceStream;
            SectorSize = sectorSize;
            Type = type;
        }

        public SectorList(Stream sourceStream, int sectorSize, SectorType type)
        {
            SourceStream = sourceStream;
            SectorSize = sectorSize;
            Type = type;

            IdIndexList = new List<int>();
        }

        public List<int> IdIndexList { get; }

        public int Count => IdIndexList.Count;

        public Stream SourceStream { get; }

        public int SectorSize { get; }

        public SectorType Type { get; }

        public int Read(int sectorIndex, byte[] buffer, int position, int offset, int count)
        {
            var idIndex = IdIndexList[sectorIndex];
            var sectorPosition = SectorSize + idIndex * SectorSize;

            var streamPosition = sectorPosition + position;
            SourceStream.Seek(streamPosition, SeekOrigin.Begin);
            return SourceStream.Read(buffer, offset, count);
        }
    }
}