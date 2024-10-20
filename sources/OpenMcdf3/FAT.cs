namespace OpenMcdf3
{
    internal class FAT
    {
        private const int N_HEADER_FAT_ENTRY = 109; //Number of FAT sectors id in the header

        private List<Sector> fatSectors = new List<Sector>();
        private List<Sector> difatSectors = new List<Sector>();

        private Header header;
        private Stream sourceStream;

        internal FAT(Header header, Stream sourceStream)
        {
            this.header = header;
            this.sourceStream = sourceStream;
        }

        internal int SectorSize => 2 << (header.SectorShift - 1);

        internal int MinSizeStandardStream => (int)header.MinSizeStandardStream;

        /// <summary>
        /// Get the FAT sector chain
        /// </summary>
        /// <returns>List of FAT sectors</returns>
        private void LoadFatSectorChain()
        {
            // Read DIFAT entries from the header Fat entry array (max 109 entries)
            LoadDifatSectorChain();

            int idx = 0;
            int nextSecID;

            // Read FAT entries from the header Fat entry array (max 109 entries)
            while (idx < header.FATSectorsNumber && idx < N_HEADER_FAT_ENTRY)
            {
                nextSecID = header.DIFAT[idx];
                Sector s = MaterializeSector(nextSecID, SectorType.FAT);

                fatSectors.Add(s);

                idx++;
            }

            //Is there any DIFAT sector containing other FAT entries ?
            if (difatSectors.Count > 0)
            {
                HashSet<int> processedSectors = new HashSet<int>();

                using StreamView difatStream
                    = new StreamView
                        (
                        difatSectors,
                        SectorSize,
                        difatSectors.Count * SectorSize,
                        null,
                        sourceStream);

                StreamRW difatStreamRW = new(difatStream);

                int i = 0;

                while (fatSectors.Count < header.FATSectorsNumber)
                {
                    nextSecID = difatStreamRW.ReadInt32();

                    EnsureUniqueSectorIndex(nextSecID, processedSectors);

                    Sector s = MaterializeSector(nextSecID, SectorType.FAT);

                    fatSectors.Add(s);

                    if (difatStream.Position == (SectorSize - 4 + i * SectorSize))
                    {
                        // Skip DIFAT chain fields considering the possibility
                        // that the last FAT entry has been already read

                        if (difatStreamRW.ReadInt32() == Sector.ENDOFCHAIN)
                            break;

                        i++;
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Get the DIFAT Sector chain
        /// </summary>
        /// <returns>A list of DIFAT sectors</returns>
        private void LoadDifatSectorChain()
        {

            HashSet<int> processedSectors = new HashSet<int>();

            if (header.DIFATSectorsNumber != 0)
            {
                int validationCount = (int)header.DIFATSectorsNumber;

                if (header.FirstDIFATSectorID != Sector.ENDOFCHAIN)
                {
                    int nextSecID = header.FirstDIFATSectorID;

                    do
                    {
                        Sector s = MaterializeSector(nextSecID, SectorType.DIFAT); //sectors[header.FirstDIFATSectorID];
                        difatSectors.Add(s);

                        nextSecID = BitConverter.ToInt32(s.Data, SectorSize - 4);

                        // Strictly speaking, the following condition is not correct from
                        // a specification point of view:
                        // only ENDOFCHAIN should break DIFAT chain but
                        // a lot of existing compound files use FREESECT as DIFAT chain termination
                        if (nextSecID is Sector.FREESECT or Sector.ENDOFCHAIN) break;

                    } while (true && validationCount-- >= 0);
                }
            }

            //    //Sector s = MaterializeSector(header.FirstDIFATSectorID, SectorType.DIFAT); //sectors[header.FirstDIFATSectorID];
            //    difatSectors.Add(s);

            //    while (true && validationCount >= 0)
            //    {
            //        nextSecID = BitConverter.ToInt32(s.Data, SectorSize - 4);
            //        //EnsureUniqueSectorIndex(nextSecID, processedSectors);

            //        // Strictly speaking, the following condition is not correct from
            //        // a specification point of view:
            //        // only ENDOFCHAIN should break DIFAT chain but
            //        // a lot of existing compound files use FREESECT as DIFAT chain termination
            //        if (nextSecID is Sector.FREESECT or Sector.ENDOFCHAIN) break;

            //        validationCount--;

            //        if (validationCount < 0)
            //        {
            //            //if (closeStream)
            //            //    Close();

            //            //if (ValidationExceptionEnabled)
            //            //    throw new CFCorruptedFileException("DIFAT sectors count mismatched. Corrupted compound file");
            //        }

            //        s = MaterializeSector(nextSecID, SectorType.DIFAT);


            //        result.Add(s);
            //    }
            //}

            //return result;
        }

        private Sector MaterializeSector(int id, SectorType sectorType)
        {
            Sector sector = new Sector(SectorSize, sectorType);

            sourceStream.Seek(1 + SectorSize * id, SeekOrigin.Begin);
            sourceStream.ReadExactly(sector.Data);

            return sector;
        }

        public void WriteToChain(int sid, byte[] data, int offset, int count)
        {
            List<Sector> streamChain = GetSectorChain(sid, SectorType.Normal);
            StreamView sv = new StreamView(streamChain, SectorSize);
            sv.Seek(offset, SeekOrigin.Begin);
            sv.Write(data, offset, count);

        }

        /// <summary>
        /// Get a standard sector chain
        /// </summary>
        /// <param name="secID">First SecID of the required chain</param>
        /// <returns>A list of sectors</returns>
        private List<Sector> GetSectors(int secID, int startSector, int endSector)
        {
            List<Sector> result
                   = new List<Sector>();

            int nextSecID = secID;


            HashSet<int> processedSectors = new HashSet<int>();

            using StreamView fatStream = new StreamView(fatSectors, SectorSize, fatSectors.Count * SectorSize, null, true);
            StreamRW fatStreamRW = new(fatStream);

            while (true)
            {
                if (nextSecID == Sector.ENDOFCHAIN) break;

                if (nextSecID < 0)
                    throw new CFCorruptedFileException(string.Format("Next Sector ID reference is below zero. NextID : {0}", nextSecID));

                if (nextSecID >= sectors.Count)
                    throw new CFCorruptedFileException(string.Format("Next Sector ID reference an out of range sector. NextID : {0} while sector count {1}", nextSecID, sectors.Count));

                Sector s = sectors[nextSecID];
                
                if (s == null)
                {
                    s = new Sector(SectorSize, sourceStream)
                    {
                        Id = nextSecID,
                        Type = SectorType.Normal
                    };
                    sectors[nextSecID] = s;
                }

                result.Add(s);

                fatStreamRW.Seek(nextSecID * 4, SeekOrigin.Begin);
                int next = fatStreamRW.ReadInt32();

                EnsureUniqueSectorIndex(next, processedSectors);
                nextSecID = next;
            }

            return result;
        }

        /// <summary>
        /// Get a standard sector chain
        /// </summary>
        /// <param name="secID">First SecID of the required chain</param>
        /// <returns>A list of sectors</returns>
        private List<Sector> GetNormalSectorChain(int secID)
        {
            List<Sector> result
                   = new List<Sector>();

            int nextSecID = secID;

            List<Sector> fatSectors = GetFatSectorChain();
            HashSet<int> processedSectors = new HashSet<int>();

            using StreamView fatStream
                = new StreamView(fatSectors, SectorSize, fatSectors.Count * SectorSize, null, sourceStream);
            StreamRW fatStreamRW = new(fatStream);

            while (true)
            {
                if (nextSecID == Sector.ENDOFCHAIN) break;

                if (nextSecID < 0)
                    throw new CFCorruptedFileException(string.Format("Next Sector ID reference is below zero. NextID : {0}", nextSecID));

                if (nextSecID >= sectors.Count)
                    throw new CFCorruptedFileException(string.Format("Next Sector ID reference an out of range sector. NextID : {0} while sector count {1}", nextSecID, sectors.Count));

                Sector s = sectors[nextSecID];
                if (s == null)
                {
                    s = new Sector(SectorSize, sourceStream)
                    {
                        Id = nextSecID,
                        Type = SectorType.Normal
                    };
                    sectors[nextSecID] = s;
                }

                result.Add(s);

                fatStreamRW.Seek(nextSecID * 4, SeekOrigin.Begin);
                int next = fatStreamRW.ReadInt32();

                EnsureUniqueSectorIndex(next, processedSectors);
                nextSecID = next;
            }

            return result;
        }

        /// <summary>
        /// Get a sector chain from a compound file given the first sector ID
        /// and the required sector type.
        /// </summary>
        /// <param name="secID">First chain sector's id </param>
        /// <param name="chainType">Type of Sectors in the required chain (mini sectors, normal sectors or FAT)</param>
        /// <returns>A list of Sectors as the result of their concatenation</returns>
        internal List<Sector> GetSectorChain(int secID, SectorType chainType)
        {
            return chainType switch
            {
                //SectorType.DIFAT => GetDifatSectorChain(),
                //SectorType.FAT => GetFatSectorChain(),
                SectorType.Normal => GetNormalSectorChain(secID),
                //SectorType.Mini => GetMiniSectorChain(secID),
                _ => throw new CFException("Unsupported chain type"),
            };
        }
        private void EnsureUniqueSectorIndex(int nextSecID, HashSet<int> processedSectors)
        {
            //if (!ValidationExceptionEnabled)
            //{
            //    return;
            //}

            //if (!processedSectors.Add(nextSecID))
            //{
            //    throw new CFCorruptedFileException("The file is corrupted.");
            //}
        }
    }
}
