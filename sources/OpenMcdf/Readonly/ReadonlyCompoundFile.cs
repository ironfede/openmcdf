/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. 
 * 
 * The Original Code is OpenMCDF - Compound Document Format library.
 * 
 * The Initial Developer of the Original Code is Federico Blaseotto.*/

#define FLAT_WRITE // No optimization on the number of write operations

using System;
using System.Collections.Generic;
using System.IO;
using RedBlackTree;

namespace OpenMcdf
{
    /// <summary>
    ///     Standard Microsoft&#169; Compound File implementation.
    ///     It is also known as OLE/COM structured storage
    ///     and contains a hierarchy of storage and stream objects providing
    ///     efficent storage of multiple kinds of documents in a single file.
    ///     Version 3 and 4 of specifications are supported.
    /// </summary>
    public class ReadonlyCompoundFile : IDisposable
    {
        /// <summary>
        ///     Number of DIFAT entries in the header
        /// </summary>
        private const int HEADER_DIFAT_ENTRIES_COUNT = 109;

        /// <summary>
        ///     Sector ID Size (int)
        /// </summary>
        private const int SIZE_OF_SID = 4;

        /// <summary>
        ///     Initial capacity of the flushing queue used
        ///     to optimize commit writing operations
        /// </summary>
        private const int FLUSHING_QUEUE_SIZE = 6000;

        /// <summary>
        ///     Maximum size of the flushing buffer used
        ///     to optimize commit writing operations
        /// </summary>
        private const int FLUSHING_BUFFER_MAX_SIZE = 1024 * 1024 * 16;

        private readonly bool _disableCache;

        /// <summary>
        ///     Flag for unallocated sector zeroing out.
        /// </summary>
        private readonly bool _eraseFreeSectors;

        private readonly string _fileName;

        private readonly List<int> _levelSiDs = new();

        /// <summary>
        ///     Flag for sector recycling.
        /// </summary>
        private readonly bool _sectorRecycle;

        /// <summary>
        ///     Number of FAT entries in a DIFAT Sector
        /// </summary>
        private readonly int DIFAT_SECTOR_FAT_ENTRIES_COUNT = 127;

        /// <summary>
        ///     Sectors ID entries in a FAT Sector
        /// </summary>
        private readonly int FAT_SECTOR_ENTRIES_COUNT = 128;

        private IByteArrayPool _byteArrayPool;

        private List<IDirectoryEntry> _directoryEntries
            = new();

        /// <summary>
        ///     CompoundFile header
        /// </summary>
        private Header _header;

        private int _lockSectorId = -1;

        private Dictionary<int, Sector> Sectors
        {
            get => _sectors ??= new Dictionary<int, Sector>();
            set => _sectors = value;
        }
        private Dictionary<int, Sector> _sectors;

        private bool _transactionLockAdded;
        private bool _transactionLockAllocated;

        /// <summary>
        ///     Load an existing compound file.
        /// </summary>
        /// <param name="fileName">Compound file to read from</param>
        /// <example>
        ///     <code>
        /// //A xls file should have a Workbook stream
        /// String filename = "report.xls";
        /// 
        /// CompoundFile cf = new CompoundFile(filename);
        /// CFStream foundStream = cf.RootStorage.GetStream("Workbook");
        /// 
        /// byte[] temp = foundStream.GetData();
        /// 
        /// Assert.IsNotNull(temp);
        /// 
        /// cf.Close();
        /// </code>
        /// </example>
        /// <remarks>
        ///     File will be open in read-only mode: it has to be saved
        ///     with a different filename. A wrapping implementation has to be provided
        ///     in order to remove/substitute an existing file. Version will be
        ///     automatically recognized from the file. Sector recycle is turned off
        ///     to achieve the best reading/writing performance in most common scenarios.
        /// </remarks>
        public ReadonlyCompoundFile(string fileName)
        {
            _sectorRecycle = false;
            _eraseFreeSectors = false;

            _fileName = fileName;

            FileStream fs = null;

            try
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                Load(fs);
            }
            catch
            {
                if (fs != null)
                {
                    fs.Close();
                }

                throw;
            }

            DIFAT_SECTOR_FAT_ENTRIES_COUNT = GetSectorSize() / 4 - 1;
            FAT_SECTOR_ENTRIES_COUNT = GetSectorSize() / 4;
        }

        public ReadonlyCompoundFile(Stream stream, IByteArrayPool byteArrayPool = null)
        {
            _disableCache = true;
            _byteArrayPool = byteArrayPool;
            Configuration |= CFSConfiguration.LeaveOpen;

            Load(stream);

            DIFAT_SECTOR_FAT_ENTRIES_COUNT = GetSectorSize() / 4 - 1;
            FAT_SECTOR_ENTRIES_COUNT = GetSectorSize() / 4;
        }

        /// <summary>
        ///     Get the configuration parameters of the CompoundFile object.
        /// </summary>
        public CFSConfiguration Configuration { get; } = CFSConfiguration.Default;

        /// <summary>
        ///     Compound underlying stream. Null when new CF has been created.
        /// </summary>
        internal Stream SourceStream { private set; get; }


        public bool ValidationExceptionEnabled { get; } = true;

        private IByteArrayPool ByteArrayPool => _byteArrayPool ??= new ByteArrayPool();

        /// <summary>
        ///     Return true if this compound file has been
        ///     loaded from an existing file or stream
        /// </summary>
        public bool HasSourceStream => SourceStream != null;

        /// <summary>
        ///     The entry point object that represents the
        ///     root of the structures tree to get or set storage or
        ///     stream data.
        /// </summary>
        /// <example>
        ///     <code>
        /// 
        ///    //Create a compound file
        ///    string FILENAME = "MyFileName.cfs";
        ///    CompoundFile ncf = new CompoundFile();
        /// 
        ///    CFStorage l1 = ncf.RootStorage.AddStorage("Storage Level 1");
        /// 
        ///    l1.AddStream("l1ns1");
        ///    l1.AddStream("l1ns2");
        ///    l1.AddStream("l1ns3");
        ///    CFStorage l2 = l1.AddStorage("Storage Level 2");
        ///    l2.AddStream("l2ns1");
        ///    l2.AddStream("l2ns2");
        /// 
        ///    ncf.Save(FILENAME);
        ///    ncf.Close();
        /// </code>
        /// </example>
        public ReadonlyCompoundFileStorage RootStorage { get; private set; }

        public CFSVersion Version => (CFSVersion)_header.MajorVersion;

        private bool CloseStream => Configuration.HasFlag(CFSConfiguration.LeaveOpen);

        internal bool IsClosed { get; private set; }

        internal IDirectoryEntry RootEntry => _directoryEntries[0];

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        ///     Returns the size of standard sectors switching on CFS version (3 or 4)
        /// </summary>
        /// <returns>Standard sector size</returns>
        internal int GetSectorSize()
        {
            return 2 << (_header.SectorShift - 1);
        }

        private void OnSizeLimitReached()
        {
            var rangeLockSector = new Sector(GetSectorSize(), SourceStream);
            Sectors.Add(Sectors.Count, rangeLockSector);

            rangeLockSector.Type = SectorType.RangeLockSector;

            _transactionLockAdded = true;
            _lockSectorId = rangeLockSector.Id;
        }

        /// <summary>
        ///     Load directory entries from compound file. Header and FAT MUST be already loaded.
        /// </summary>
        private void LoadDirectoriesWithLowMemory()
        {
            var directoryChain = GetNormalSectorChainLowMemory(_header.FirstDirectorySectorID);

            if (!(directoryChain.Count > 0))
            {
                throw new CFCorruptedFileException("Directory sector chain MUST contain at least 1 sector");
            }

            if (_header.FirstDirectorySectorID == Sector.ENDOFCHAIN)
            {
                _header.FirstDirectorySectorID = directoryChain.IdIndexList[0];
            }

            var dirReader = new ReadonlyStreamViewForSectorList(directoryChain, directoryChain.Count * GetSectorSize(),
                SourceStream, ByteArrayPool);

            while (dirReader.Position < dirReader.Length)
            {
                var directoryEntry
                    = (DirectoryEntry)DirectoryEntry.New(string.Empty, StgType.StgInvalid, _directoryEntries);
                //We are not inserting dirs. Do not use 'InsertNewDirectoryEntry'
                directoryEntry.Read(dirReader, Version);
            }
        }

        /// <summary>
        ///     Get a standard sector chain
        /// </summary>
        /// <param name="secID">First SecID of the required chain</param>
        /// <returns>A list of sectors</returns>
        private SectorList GetNormalSectorChainLowMemory(int secID)
        {
            var sectorIdIndexList = new List<int>();
            var nextSecId = secID;
            var fatSectors = GetFatSectorChainLowMemory();

            var fatStream =
                new ReadonlyStreamViewForSectorList(fatSectors, fatSectors.Count * GetSectorSize(), SourceStream,
                    ByteArrayPool);

            while (nextSecId != Sector.ENDOFCHAIN)
            {
                if (nextSecId < 0)
                {
                    throw new CFCorruptedFileException($"Next Sector ID reference is below zero. NextID : {nextSecId}");
                }

                sectorIdIndexList.Add(nextSecId);

                const int sizeOfInt32 = 4;
                fatStream.Seek(nextSecId * sizeOfInt32, SeekOrigin.Begin);
                var next = fatStream.ReadInt32();

                nextSecId = next;
            }

            var sectorList = new SectorList(sectorIdIndexList, SourceStream, GetSectorSize(), SectorType.Normal);
            return sectorList;
        }

        /// <summary>
        ///     Get the FAT sector chain
        /// </summary>
        /// <returns>List of FAT sectors</returns>
        private SectorList GetFatSectorChainLowMemory()
        {
            const int N_HEADER_FAT_ENTRY = 109; //Number of FAT sectors id in the header

            int nextSecId;

            var difatSectors = GetDifatSectorChain();

            var idIndexList = new List<int>(Math.Min(_header.FATSectorsNumber, N_HEADER_FAT_ENTRY));

            var idx = 0;

            // Read FAT entries from the header Fat entry array (max 109 entries)
            while (idx < _header.FATSectorsNumber && idx < N_HEADER_FAT_ENTRY)
            {
                nextSecId = _header.DIFAT[idx];
                idIndexList.Add(nextSecId);

                idx++;
            }

            //Is there any DIFAT sector containing other FAT entries ?
            if (difatSectors.Count > 0)
            {
                var difatStream
                    = new StreamView
                    (
                        difatSectors,
                        GetSectorSize(),
                        _header.FATSectorsNumber > N_HEADER_FAT_ENTRY
                            ? (_header.FATSectorsNumber - N_HEADER_FAT_ENTRY) * 4
                            : 0,
                        null,
                        SourceStream
                    );

                var i = 0;

                while (idIndexList.Count < _header.FATSectorsNumber)
                {
                    nextSecId = difatStream.ReadInt32();

                    idIndexList.Add(nextSecId);

                    if (difatStream.Position == GetSectorSize() - 4 + i * GetSectorSize())
                    {
                        // Skip DIFAT chain fields considering the possibility that the last FAT entry has been already read
                        var sign = difatStream.ReadInt32();
                        if (sign == Sector.ENDOFCHAIN)
                        {
                            break;
                        }

                        i++;
                    }
                }
            }

            return new SectorList(idIndexList, SourceStream, GetSectorSize(), SectorType.FAT);
        }

        /// <summary>
        ///     Load compound file from an existing stream.
        /// </summary>
        /// <param name="stream">Stream to load compound file from</param>
        private void Load(Stream stream)
        {
            try
            {
                _header = new Header();
                _directoryEntries = new List<IDirectoryEntry>();

                SourceStream = stream;

                _header.Read(stream);

                if (stream.Length > 0x7FFFFF0)
                {
                    _transactionLockAllocated = true;
                }

                LoadDirectoriesWithLowMemory();

                RootStorage
                    = new ReadonlyCompoundFileStorage(this, _directoryEntries[0]);
            }
            catch (Exception)
            {
                if (stream != null && CloseStream)
                {
                    stream.Close();
                }

                throw;
            }
        }


        private void PersistMiniStreamToStream(List<Sector> miniSectorChain)
        {
            var miniStream
                = GetSectorChain(RootEntry.StartSetc, SectorType.Normal);

            var miniStreamView
                = new StreamView(
                    miniStream,
                    GetSectorSize(),
                    RootStorage.Size,
                    null,
                    SourceStream);

            for (var i = 0; i < miniSectorChain.Count; i++)
            {
                var s = miniSectorChain[i];

                if (s.Id == -1)
                {
                    throw new CFException("Invalid minisector index");
                }

                // Ministream sectors already allocated
                miniStreamView.Seek(Sector.MINISECTOR_SIZE * s.Id, SeekOrigin.Begin);
                miniStreamView.Write(s.GetData(), 0, Sector.MINISECTOR_SIZE);
            }
        }

        /// <summary>
        ///     Allocate space, setup sectors id and refresh header
        ///     for the new or updated mini sector chain.
        /// </summary>
        /// <param name="sectorChain">The new MINI sector chain</param>
        private void AllocateMiniSectorChain(List<Sector> sectorChain)
        {
            var miniFAT
                = GetSectorChain(_header.FirstMiniFATSectorID, SectorType.Normal);

            var miniStream
                = GetSectorChain(RootEntry.StartSetc, SectorType.Normal);

            var miniFATView
                = new StreamView(
                    miniFAT,
                    GetSectorSize(),
                    _header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE,
                    null,
                    SourceStream,
                    true
                );

            var miniStreamView
                = new StreamView(
                    miniStream,
                    GetSectorSize(),
                    RootStorage.Size,
                    null,
                    SourceStream);


            // Set updated/new sectors within the ministream
            // We are writing data in a NORMAL Sector chain.
            for (var i = 0; i < sectorChain.Count; i++)
            {
                var s = sectorChain[i];

                if (s.Id == -1)
                {
                    // Allocate, position ministream at the end of already allocated
                    // ministream's sectors

                    miniStreamView.Seek(RootStorage.Size + Sector.MINISECTOR_SIZE, SeekOrigin.Begin);
                    //miniStreamView.Write(s.GetData(), 0, Sector.MINISECTOR_SIZE);
                    s.Id = (int)(miniStreamView.Position - Sector.MINISECTOR_SIZE) / Sector.MINISECTOR_SIZE;

                    RootStorage.DirEntry.Size = miniStreamView.Length;
                }
            }

            // Update miniFAT
            for (var i = 0; i < sectorChain.Count - 1; i++)
            {
                var currentId = sectorChain[i].Id;
                var nextId = sectorChain[i + 1].Id;

                miniFATView.Seek(currentId * 4, SeekOrigin.Begin);
                miniFATView.Write(BitConverter.GetBytes(nextId), 0, 4);
            }

            // Write End of Chain in MiniFAT
            miniFATView.Seek(sectorChain[sectorChain.Count - 1].Id * SIZE_OF_SID, SeekOrigin.Begin);
            miniFATView.Write(BitConverter.GetBytes(Sector.ENDOFCHAIN), 0, 4);

            // Update sector chains
            AllocateSectorChain(miniStreamView.BaseSectorChain);
            AllocateSectorChain(miniFATView.BaseSectorChain);

            //Update HEADER and root storage when ministream changes
            if (miniFAT.Count > 0)
            {
                RootStorage.DirEntry.StartSetc = miniStream[0].Id;
                _header.MiniFATSectorsNumber = (uint)miniFAT.Count;
                _header.FirstMiniFATSectorID = miniFAT[0].Id;
            }
        }

        private void FreeMiniChain(List<Sector> sectorChain, bool zeroSector)
        {
            FreeMiniChain(sectorChain, 0, zeroSector);
        }

        private void FreeMiniChain(List<Sector> sectorChain, int nth_sector_to_remove, bool zeroSector)
        {
            var zeroedMiniSector = new byte[Sector.MINISECTOR_SIZE];

            var miniFAT
                = GetSectorChain(_header.FirstMiniFATSectorID, SectorType.Normal);

            var miniStream
                = GetSectorChain(RootEntry.StartSetc, SectorType.Normal);

            var miniFATView
                = new StreamView(miniFAT, GetSectorSize(), _header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE, null,
                    SourceStream);

            var miniStreamView
                = new StreamView(miniStream, GetSectorSize(), RootStorage.Size, null, SourceStream);

            // Set updated/new sectors within the ministream ----------
            if (zeroSector)
            {
                for (var i = nth_sector_to_remove; i < sectorChain.Count; i++)
                {
                    var s = sectorChain[i];

                    if (s.Id != -1)
                    {
                        // Overwrite
                        miniStreamView.Seek(Sector.MINISECTOR_SIZE * s.Id, SeekOrigin.Begin);
                        miniStreamView.Write(zeroedMiniSector, 0, Sector.MINISECTOR_SIZE);
                    }
                }
            }

            // Update miniFAT                ---------------------------------------
            for (var i = nth_sector_to_remove; i < sectorChain.Count; i++)
            {
                var currentId = sectorChain[i].Id;

                miniFATView.Seek(currentId * 4, SeekOrigin.Begin);
                miniFATView.Write(BitConverter.GetBytes(Sector.FREESECT), 0, 4);
            }

            // Write End of Chain in MiniFAT ---------------------------------------
            //miniFATView.Seek(sectorChain[(sectorChain.Count - 1) - nth_sector_to_remove].Id * SIZE_OF_SID, SeekOrigin.Begin);
            //miniFATView.Write(BitConverter.GetBytes(Sector.ENDOFCHAIN), 0, 4);

            // Write End of Chain in MiniFAT ---------------------------------------
            if (nth_sector_to_remove > 0 && sectorChain.Count > 0)
            {
                miniFATView.Seek(sectorChain[nth_sector_to_remove - 1].Id * 4, SeekOrigin.Begin);
                miniFATView.Write(BitConverter.GetBytes(Sector.ENDOFCHAIN), 0, 4);
            }

            // Update sector chains           ---------------------------------------
            AllocateSectorChain(miniStreamView.BaseSectorChain);
            AllocateSectorChain(miniFATView.BaseSectorChain);

            //Update HEADER and root storage when ministream changes
            if (miniFAT.Count > 0)
            {
                RootStorage.DirEntry.StartSetc = miniStream[0].Id;
                _header.MiniFATSectorsNumber = (uint)miniFAT.Count;
                _header.FirstMiniFATSectorID = miniFAT[0].Id;
            }
        }

        /// <summary>
        ///     Allocate space, setup sectors id in the FAT and refresh header
        ///     for the new or updated sector chain (Normal or Mini sectors)
        /// </summary>
        /// <param name="sectorChain">The new or updated normal or mini sector chain</param>
        private void SetSectorChain(List<Sector> sectorChain)
        {
            if (sectorChain == null || sectorChain.Count == 0)
            {
                return;
            }

            var type = sectorChain[0].Type;

            if (type == SectorType.Normal)
            {
                AllocateSectorChain(sectorChain);
            }
            else if (type == SectorType.Mini)
            {
                AllocateMiniSectorChain(sectorChain);
            }
        }

        /// <summary>
        ///     Allocate space, setup sectors id and refresh header
        ///     for the new or updated sector chain.
        /// </summary>
        /// <param name="sectorChain">The new or updated generic sector chain</param>
        private void AllocateSectorChain(List<Sector> sectorChain)
        {
            foreach (var s in sectorChain)
            {
                if (s.Id == -1)
                {
                    Sectors.Add(Sectors.Count, s);
                    s.Id = Sectors.Count - 1;
                }
            }

            AllocateFATSectorChain(sectorChain);
        }

        /// <summary>
        ///     Check for transaction lock sector addition and mark it in the FAT.
        /// </summary>
        private void CheckForLockSector()
        {
            //If transaction lock has been added and not yet allocated in the FAT...
            if (_transactionLockAdded && !_transactionLockAllocated)
            {
                var fatStream = new StreamView(GetFatSectorChain(), GetSectorSize(), SourceStream);

                fatStream.Seek(_lockSectorId * 4, SeekOrigin.Begin);
                fatStream.Write(BitConverter.GetBytes(Sector.ENDOFCHAIN), 0, 4);

                _transactionLockAllocated = true;
            }
        }

        /// <summary>
        ///     Allocate space, setup sectors id and refresh header
        ///     for the new or updated FAT sector chain.
        /// </summary>
        /// <param name="sectorChain">The new or updated generic sector chain</param>
        private void AllocateFATSectorChain(List<Sector> sectorChain)
        {
            var fatSectors = GetSectorChain(-1, SectorType.FAT);

            var fatStream =
                new StreamView(
                    fatSectors,
                    GetSectorSize(),
                    _header.FATSectorsNumber * GetSectorSize(),
                    null,
                    SourceStream,
                    true
                );

            // Write FAT chain values --

            for (var i = 0; i < sectorChain.Count - 1; i++)
            {
                var sN = sectorChain[i + 1];
                var sC = sectorChain[i];

                fatStream.Seek(sC.Id * 4, SeekOrigin.Begin);
                fatStream.Write(BitConverter.GetBytes(sN.Id), 0, 4);
            }

            fatStream.Seek(sectorChain[sectorChain.Count - 1].Id * 4, SeekOrigin.Begin);
            fatStream.Write(BitConverter.GetBytes(Sector.ENDOFCHAIN), 0, 4);

            // Merge chain to CFS
            AllocateDIFATSectorChain(fatStream.BaseSectorChain);
        }

        /// <summary>
        ///     Setup the DIFAT sector chain
        /// </summary>
        /// <param name="sectorChainOfFAT">A FAT sector chain</param>
        private void AllocateDIFATSectorChain(List<Sector> sectorChainOfFAT)
        {
            // Get initial sector's count
            _header.FATSectorsNumber = sectorChainOfFAT.Count;

            // Allocate Sectors
            foreach (var s in sectorChainOfFAT)
            {
                if (s.Id == -1)
                {
                    Sectors.Add(Sectors.Count, s);
                    s.Id = Sectors.Count - 1;
                    s.Type = SectorType.FAT;
                }
            }

            // Sector count...
            var nCurrentSectors = Sectors.Count;

            // Temp DIFAT count
            var nDIFATSectors = (int)_header.DIFATSectorsNumber;

            if (sectorChainOfFAT.Count > HEADER_DIFAT_ENTRIES_COUNT)
            {
                nDIFATSectors = Ceiling((double)(sectorChainOfFAT.Count - HEADER_DIFAT_ENTRIES_COUNT) /
                                        DIFAT_SECTOR_FAT_ENTRIES_COUNT);
                nDIFATSectors = LowSaturation(nDIFATSectors - (int)_header.DIFATSectorsNumber); //required DIFAT
            }

            // ...sum with new required DIFAT sectors count
            nCurrentSectors += nDIFATSectors;

            // ReCheck FAT bias
            while (_header.FATSectorsNumber * FAT_SECTOR_ENTRIES_COUNT < nCurrentSectors)
            {
                var extraFATSector = new Sector(GetSectorSize(), SourceStream);
                Sectors.Add(Sectors.Count, extraFATSector);

                extraFATSector.Id = Sectors.Count - 1;
                extraFATSector.Type = SectorType.FAT;

                sectorChainOfFAT.Add(extraFATSector);

                _header.FATSectorsNumber++;
                nCurrentSectors++;

                //... so, adding a FAT sector may induce DIFAT sectors to increase by one
                // and consequently this may induce ANOTHER FAT sector (TO-THINK: May this condition occure ?)
                if (nDIFATSectors * DIFAT_SECTOR_FAT_ENTRIES_COUNT <
                    (_header.FATSectorsNumber > HEADER_DIFAT_ENTRIES_COUNT
                        ? _header.FATSectorsNumber - HEADER_DIFAT_ENTRIES_COUNT
                        : 0))
                {
                    nDIFATSectors++;
                    nCurrentSectors++;
                }
            }


            var difatSectors =
                GetSectorChain(-1, SectorType.DIFAT);

            var difatStream
                = new StreamView(difatSectors, GetSectorSize(), SourceStream);

            // Write DIFAT Sectors (if required)
            // Save room for the following chaining
            for (var i = 0; i < sectorChainOfFAT.Count; i++)
            {
                if (i < HEADER_DIFAT_ENTRIES_COUNT)
                {
                    _header.DIFAT[i] = sectorChainOfFAT[i].Id;
                }
                else
                {
                    // room for DIFAT chaining at the end of any DIFAT sector (4 bytes)
                    if (i != HEADER_DIFAT_ENTRIES_COUNT &&
                        (i - HEADER_DIFAT_ENTRIES_COUNT) % DIFAT_SECTOR_FAT_ENTRIES_COUNT == 0)
                    {
                        var temp = new byte[sizeof(int)];
                        difatStream.Write(temp, 0, sizeof(int));
                    }

                    difatStream.Write(BitConverter.GetBytes(sectorChainOfFAT[i].Id), 0, sizeof(int));
                }
            }

            // Allocate room for DIFAT sectors
            for (var i = 0; i < difatStream.BaseSectorChain.Count; i++)
            {
                if (difatStream.BaseSectorChain[i].Id == -1)
                {
                    Sectors.Add(Sectors.Count, difatStream.BaseSectorChain[i]);
                    difatStream.BaseSectorChain[i].Id = Sectors.Count - 1;
                    difatStream.BaseSectorChain[i].Type = SectorType.DIFAT;
                }
            }

            _header.DIFATSectorsNumber = (uint)nDIFATSectors;


            // Chain first sector
            if (difatStream.BaseSectorChain != null && difatStream.BaseSectorChain.Count > 0)
            {
                _header.FirstDIFATSectorID = difatStream.BaseSectorChain[0].Id;

                // Update header information
                _header.DIFATSectorsNumber = (uint)difatStream.BaseSectorChain.Count;

                // Write chaining information at the end of DIFAT Sectors
                for (var i = 0; i < difatStream.BaseSectorChain.Count - 1; i++)
                {
                    Buffer.BlockCopy(
                        BitConverter.GetBytes(difatStream.BaseSectorChain[i + 1].Id),
                        0,
                        difatStream.BaseSectorChain[i].GetData(),
                        GetSectorSize() - sizeof(int),
                        4);
                }

                Buffer.BlockCopy(
                    BitConverter.GetBytes(Sector.ENDOFCHAIN),
                    0,
                    difatStream.BaseSectorChain[difatStream.BaseSectorChain.Count - 1].GetData(),
                    GetSectorSize() - sizeof(int),
                    sizeof(int)
                );
            }
            else
            {
                _header.FirstDIFATSectorID = Sector.ENDOFCHAIN;
            }

            // Mark DIFAT Sectors in FAT
            var fatSv =
                new StreamView(sectorChainOfFAT, GetSectorSize(), _header.FATSectorsNumber * GetSectorSize(), null,
                    SourceStream);

            for (var i = 0; i < _header.DIFATSectorsNumber; i++)
            {
                fatSv.Seek(difatStream.BaseSectorChain[i].Id * 4, SeekOrigin.Begin);
                fatSv.Write(BitConverter.GetBytes(Sector.DIFSECT), 0, 4);
            }

            for (var i = 0; i < _header.FATSectorsNumber; i++)
            {
                fatSv.Seek(fatSv.BaseSectorChain[i].Id * 4, SeekOrigin.Begin);
                fatSv.Write(BitConverter.GetBytes(Sector.FATSECT), 0, 4);
            }

            //fatSv.Seek(fatSv.BaseSectorChain[fatSv.BaseSectorChain.Count - 1].Id * 4, SeekOrigin.Begin);
            //fatSv.Write(BitConverter.GetBytes(Sector.ENDOFCHAIN), 0, 4);

            _header.FATSectorsNumber = fatSv.BaseSectorChain.Count;
        }

        /// <summary>
        ///     Get the DIFAT Sector chain
        /// </summary>
        /// <returns>A list of DIFAT sectors</returns>
        private List<Sector> GetDifatSectorChain()
        {
            if (_header.DIFATSectorsNumber == 0)
            {
                return new List<Sector>(0);
            }

            var validationCount = 0;

            var result
                = new List<Sector>();

            var nextSecID
                = Sector.ENDOFCHAIN;

            var processedSectors = new HashSet<int>();

            if (_header.DIFATSectorsNumber != 0)
            {
                validationCount = (int)_header.DIFATSectorsNumber;

                Sector s;
                if (_disableCache)
                {
                    s = null;
                }
                else
                {
                    s = Sectors[_header.FirstDIFATSectorID];
                }

                if (s == null) //Lazy loading
                {
                    s = new Sector(GetSectorSize(), SourceStream);
                    s.Type = SectorType.DIFAT;
                    s.Id = _header.FirstDIFATSectorID;

                    if (!_disableCache)
                    {
                        Sectors[_header.FirstDIFATSectorID] = s;
                    }
                }

                result.Add(s);

                while (true && validationCount >= 0)
                {
                    nextSecID = BitConverter.ToInt32(s.GetData(), GetSectorSize() - 4);
                    EnsureUniqueSectorIndex(nextSecID, processedSectors);

                    // Strictly speaking, the following condition is not correct from
                    // a specification point of view:
                    // only ENDOFCHAIN should break DIFAT chain but 
                    // a lot of existing compound files use FREESECT as DIFAT chain termination
                    if (nextSecID == Sector.FREESECT || nextSecID == Sector.ENDOFCHAIN)
                    {
                        break;
                    }

                    validationCount--;

                    if (validationCount < 0)
                    {
                        if (CloseStream)
                        {
                            Dispose(true);
                        }

                        if (ValidationExceptionEnabled)
                        {
                            throw new CFCorruptedFileException(
                                "DIFAT sectors count mismatched. Corrupted compound file");
                        }
                    }

                    if (_disableCache)
                    {
                        s = null;
                    }
                    else
                    {
                        s = Sectors[nextSecID];
                    }

                    if (s == null)
                    {
                        s = new Sector(GetSectorSize(), SourceStream);
                        s.Id = nextSecID;

                        if (!_disableCache)
                        {
                            Sectors[nextSecID] = s;
                        }
                    }

                    result.Add(s);
                }
            }

            return result;
        }

        private void EnsureUniqueSectorIndex(int nextSecID, HashSet<int> processedSectors)
        {
            if (processedSectors.Contains(nextSecID) && ValidationExceptionEnabled)
            {
                throw new CFCorruptedFileException("The file is corrupted.");
            }

            processedSectors.Add(nextSecID);
        }

        /// <summary>
        ///     Get the FAT sector chain
        /// </summary>
        /// <returns>List of FAT sectors</returns>
        private List<Sector> GetFatSectorChain()
        {
            var N_HEADER_FAT_ENTRY = 109; //Number of FAT sectors id in the header

            var result
                = new List<Sector>();

            var nextSecID
                = Sector.ENDOFCHAIN;

            var difatSectors = GetDifatSectorChain();

            var idx = 0;

            // Read FAT entries from the header Fat entry array (max 109 entries)
            while (idx < _header.FATSectorsNumber && idx < N_HEADER_FAT_ENTRY)
            {
                nextSecID = _header.DIFAT[idx];
                var s = Sectors[nextSecID];

                if (s == null)
                {
                    s = new Sector(GetSectorSize(), SourceStream);
                    s.Id = nextSecID;
                    s.Type = SectorType.FAT;
                    Sectors[nextSecID] = s;
                }

                result.Add(s);

                idx++;
            }

            //Is there any DIFAT sector containing other FAT entries ?
            if (difatSectors.Count > 0)
            {
                var processedSectors = new HashSet<int>();
                var difatStream
                    = new StreamView
                    (
                        difatSectors,
                        GetSectorSize(),
                        _header.FATSectorsNumber > N_HEADER_FAT_ENTRY
                            ? (_header.FATSectorsNumber - N_HEADER_FAT_ENTRY) * 4
                            : 0,
                        null,
                        SourceStream
                    );

                var i = 0;

                while (result.Count < _header.FATSectorsNumber)
                {
                    nextSecID = difatStream.ReadInt32();

                    EnsureUniqueSectorIndex(nextSecID, processedSectors);

                    var s = Sectors[nextSecID];

                    if (s == null)
                    {
                        s = new Sector(GetSectorSize(), SourceStream);
                        s.Type = SectorType.FAT;
                        s.Id = nextSecID;
                        Sectors[nextSecID] = s; //UUU
                    }

                    result.Add(s);

                    //difatStream.Read(nextDIFATSectorBuffer, 0, 4);
                    //nextSecID = BitConverter.ToInt32(nextDIFATSectorBuffer, 0);

                    if (difatStream.Position == GetSectorSize() - 4 + i * GetSectorSize())
                    {
                        // Skip DIFAT chain fields considering the possibility that the last FAT entry has been already read
                        var sign = difatStream.ReadInt32();
                        if (sign == Sector.ENDOFCHAIN)
                        {
                            break;
                        }

                        i++;
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///     Get a standard sector chain
        /// </summary>
        /// <param name="secID">First SecID of the required chain</param>
        /// <returns>A list of sectors</returns>
        private List<Sector> GetNormalSectorChain(int secID)
        {
            var result
                = new List<Sector>();

            var nextSecID = secID;

            var fatSectors = GetFatSectorChain();
            var processedSectors = new HashSet<int>();

            var fatStream
                = new StreamView(fatSectors, GetSectorSize(), fatSectors.Count * GetSectorSize(), null, SourceStream);

            while (nextSecID != Sector.ENDOFCHAIN)
            {
                if (nextSecID < 0)
                {
                    throw new CFCorruptedFileException(
                        string.Format("Next Sector ID reference is below zero. NextID : {0}", nextSecID));
                }

                if (nextSecID >= Sectors.Count)
                {
                    throw new CFCorruptedFileException(string.Format(
                        "Next Sector ID reference an out of range sector. NextID : {0} while sector count {1}",
                        nextSecID, Sectors.Count));
                }

                var s = Sectors[nextSecID];
                if (s == null)
                {
                    s = new Sector(GetSectorSize(), SourceStream);
                    s.Id = nextSecID;
                    s.Type = SectorType.Normal;
                    Sectors[nextSecID] = s;
                }

                result.Add(s);

                fatStream.Seek(nextSecID * 4, SeekOrigin.Begin);
                var next = fatStream.ReadInt32();

                EnsureUniqueSectorIndex(next, processedSectors);
                nextSecID = next;
            }


            return result;
        }

        /// <summary>
        ///     Get a mini sector chain
        /// </summary>
        /// <param name="secID">First SecID of the required chain</param>
        /// <returns>A list of mini sectors (64 bytes)</returns>
        private List<Sector> GetMiniSectorChain(int secID)
        {
            var result
                = new List<Sector>();

            if (secID != Sector.ENDOFCHAIN)
            {
                var nextSecID = secID;

                var miniFAT = GetNormalSectorChain(_header.FirstMiniFATSectorID);
                var miniStream = GetNormalSectorChain(RootEntry.StartSetc);

                var miniFATView
                    = new StreamView(miniFAT, GetSectorSize(), _header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE,
                        null, SourceStream);

                var miniStreamView =
                    new StreamView(miniStream, GetSectorSize(), RootStorage.Size, null, SourceStream);

                var miniFATReader = new BinaryReader(miniFATView);

                nextSecID = secID;

                var processedSectors = new HashSet<int>();

                while (true)
                {
                    if (nextSecID == Sector.ENDOFCHAIN)
                    {
                        break;
                    }

                    var ms = new Sector(Sector.MINISECTOR_SIZE, SourceStream);

                    ms.Id = nextSecID;
                    ms.Type = SectorType.Mini;

                    miniStreamView.Seek(nextSecID * Sector.MINISECTOR_SIZE, SeekOrigin.Begin);
                    miniStreamView.Read(ms.GetData(), 0, Sector.MINISECTOR_SIZE);

                    result.Add(ms);

                    miniFATView.Seek(nextSecID * 4, SeekOrigin.Begin);
                    var next = miniFATReader.ReadInt32();

                    nextSecID = next;
                    EnsureUniqueSectorIndex(nextSecID, processedSectors);
                }
            }

            return result;
        }

        /// <summary>
        ///     Get a sector chain from a compound file given the first sector ID
        ///     and the required sector type.
        /// </summary>
        /// <param name="secID">First chain sector's id </param>
        /// <param name="chainType">Type of Sectors in the required chain (mini sectors, normal sectors or FAT)</param>
        /// <returns>A list of Sectors as the result of their concatenation</returns>
        internal List<Sector> GetSectorChain(int secID, SectorType chainType)
        {
            switch (chainType)
            {
                case SectorType.DIFAT:
                    return GetDifatSectorChain();

                case SectorType.FAT:
                    return GetFatSectorChain();

                case SectorType.Normal:
                    return GetNormalSectorChain(secID);

                case SectorType.Mini:
                    return GetMiniSectorChain(secID);

                default:
                    throw new CFException("Unsupproted chain type");
            }
        }


        /// <summary>
        ///     Reset a directory entry setting it to StgInvalid in the Directory.
        /// </summary>
        /// <param name="sid">Sid of the directory to invalidate</param>
        internal void ResetDirectoryEntry(int sid)
        {
            _directoryEntries[sid].SetEntryName(string.Empty);
            _directoryEntries[sid].Left = null;
            _directoryEntries[sid].Right = null;
            _directoryEntries[sid].Parent = null;
            _directoryEntries[sid].StgType = StgType.StgInvalid;
            _directoryEntries[sid].StartSetc = DirectoryEntry.ZERO;
            _directoryEntries[sid].StorageCLSID = Guid.Empty;
            _directoryEntries[sid].Size = 0;
            _directoryEntries[sid].StateBits = 0;
            _directoryEntries[sid].StgColor = StgColor.Red;
            _directoryEntries[sid].CreationDate = new byte[8];
            _directoryEntries[sid].ModifyDate = new byte[8];
        }

        internal RBTree CreateNewTree()
        {
            var bst = new RBTree();
            //bst.NodeInserted += OnNodeInsert;
            //bst.NodeOperation += OnNodeOperation;
            //bst.NodeDeleted += new Action<RBNode<CFItem>>(OnNodeDeleted);
            //  bst.ValueAssignedAction += new Action<RBNode<CFItem>, CFItem>(OnValueAssigned);
            return bst;
        }

        internal RBTree GetChildrenTree(int sid)
        {
            var bst = new RBTree();


            // Load children from their original tree.
            DoLoadChildren(bst, _directoryEntries[sid]);
            //bst = DoLoadChildrenTrusted(directoryEntries[sid]);

            //bst.Print();
            //bst.Print();
            //Trace.WriteLine("#### After rethreading");

            return bst;
        }

        private RBTree DoLoadChildrenTrusted(IDirectoryEntry de)
        {
            RBTree bst = null;

            if (de.Child != DirectoryEntry.NOSTREAM)
            {
                bst = new RBTree(_directoryEntries[de.Child]);
            }

            return bst;
        }

        private void DoLoadChildren(RBTree bst, IDirectoryEntry de)
        {
            if (de.Child != DirectoryEntry.NOSTREAM)
            {
                if (_directoryEntries[de.Child].StgType == StgType.StgInvalid)
                {
                    return;
                }

                LoadSiblings(bst, _directoryEntries[de.Child]);
                NullifyChildNodes(_directoryEntries[de.Child]);
                bst.Insert(_directoryEntries[de.Child]);
            }
        }

        private void NullifyChildNodes(IDirectoryEntry de)
        {
            de.Parent = null;
            de.Left = null;
            de.Right = null;
        }

        // Doubling methods allows iterative behavior while avoiding
        // to insert duplicate items
        private void LoadSiblings(RBTree bst, IDirectoryEntry de)
        {
            _levelSiDs.Clear();

            if (de.LeftSibling != DirectoryEntry.NOSTREAM)
            // If there're more left siblings load them...
            {
                DoLoadSiblings(bst, _directoryEntries[de.LeftSibling]);
            }
            //NullifyChildNodes(directoryEntries[de.LeftSibling]);

            if (de.RightSibling != DirectoryEntry.NOSTREAM)
            {
                _levelSiDs.Add(de.RightSibling);

                // If there're more right siblings load them...
                DoLoadSiblings(bst, _directoryEntries[de.RightSibling]);
                //NullifyChildNodes(directoryEntries[de.RightSibling]);
            }
        }

        private void DoLoadSiblings(RBTree bst, IDirectoryEntry de)
        {
            if (ValidateSibling(de.LeftSibling))
            {
                _levelSiDs.Add(de.LeftSibling);

                // If there're more left siblings load them...
                DoLoadSiblings(bst, _directoryEntries[de.LeftSibling]);
            }

            if (ValidateSibling(de.RightSibling))
            {
                _levelSiDs.Add(de.RightSibling);

                // If there're more right siblings load them...
                DoLoadSiblings(bst, _directoryEntries[de.RightSibling]);
            }

            NullifyChildNodes(de);
            bst.Insert(de);
        }

        private bool ValidateSibling(int sid)
        {
            if (sid != DirectoryEntry.NOSTREAM)
            {
                // if this siblings id does not overflow current list
                if (sid >= _directoryEntries.Count)
                {
                    if (ValidationExceptionEnabled)
                    //this.Close();
                    {
                        throw new CFCorruptedFileException("A Directory Entry references the non-existent sid number " +
                                                           sid);
                    }

                    return false;
                }

                //if this sibling is valid...
                if (_directoryEntries[sid].StgType == StgType.StgInvalid)
                {
                    if (ValidationExceptionEnabled)
                    //this.Close();
                    {
                        throw new CFCorruptedFileException(
                            "A Directory Entry has a valid reference to an Invalid Storage Type directory [" + sid +
                            "]");
                    }

                    return false;
                }

                if (!Enum.IsDefined(typeof(StgType), _directoryEntries[sid].StgType))
                {
                    if (ValidationExceptionEnabled)
                    //this.Close();
                    {
                        throw new CFCorruptedFileException("A Directory Entry has an invalid Storage Type");
                    }

                    return false;
                }

                if (_levelSiDs.Contains(sid))
                {
                    throw new CFCorruptedFileException("Cyclic reference of directory item");
                }

                return true; //No fault condition encountered for sid being validated
            }

            return false;
        }

        /// <summary>
        ///     Scan FAT o miniFAT for free sectors to reuse.
        /// </summary>
        /// <param name="sType">Type of sector to look for</param>
        /// <returns>A Queue of available sectors or minisectors already allocated</returns>
        internal Queue<Sector> FindFreeSectors(SectorType sType)
        {
            var freeList = new Queue<Sector>();

            if (sType == SectorType.Normal)
            {
                var FatChain = GetSectorChain(-1, SectorType.FAT);
                var fatStream = new StreamView(FatChain, GetSectorSize(), _header.FATSectorsNumber * GetSectorSize(),
                    null, SourceStream);

                var idx = 0;

                while (idx < Sectors.Count)
                {
                    var id = fatStream.ReadInt32();

                    if (id == Sector.FREESECT)
                    {
                        if (Sectors[idx] == null)
                        {
                            var s = new Sector(GetSectorSize(), SourceStream);
                            s.Id = idx;
                            Sectors[idx] = s;
                        }

                        freeList.Enqueue(Sectors[idx]);
                    }

                    idx++;
                }
            }
            else
            {
                var miniFAT
                    = GetSectorChain(_header.FirstMiniFATSectorID, SectorType.Normal);

                var miniFATView
                    = new StreamView(miniFAT, GetSectorSize(), _header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE,
                        null, SourceStream);

                var miniStream
                    = GetSectorChain(RootEntry.StartSetc, SectorType.Normal);

                var miniStreamView
                    = new StreamView(miniStream, GetSectorSize(), RootStorage.Size, null, SourceStream);

                var idx = 0;

                var nMinisectors = (int)(miniStreamView.Length / Sector.MINISECTOR_SIZE);

                while (idx < nMinisectors)
                {
                    //AssureLength(miniStreamView, (int)miniFATView.Length);

                    var nextId = miniFATView.ReadInt32();

                    if (nextId == Sector.FREESECT)
                    {
                        var ms = new Sector(Sector.MINISECTOR_SIZE, SourceStream);
                        var temp = new byte[Sector.MINISECTOR_SIZE];

                        ms.Id = idx;
                        ms.Type = SectorType.Mini;

                        miniStreamView.Seek(ms.Id * Sector.MINISECTOR_SIZE, SeekOrigin.Begin);
                        miniStreamView.Read(ms.GetData(), 0, Sector.MINISECTOR_SIZE);

                        freeList.Enqueue(ms);
                    }

                    idx++;
                }
            }

            return freeList;
        }

        internal int ReadData(ReadonlyCompoundFileStream cFStream, long position, byte[] buffer, int count)
        {
            if (count > buffer.Length)
            {
                throw new ArgumentException("count parameter exceeds buffer size");
            }

            var de = cFStream.DirEntry;

            count = (int)Math.Min(de.Size - position, count);

            StreamView sView = null;


            if (de.Size < _header.MinSizeStandardStream)
            {
                sView
                    = new StreamView(GetSectorChain(de.StartSetc, SectorType.Mini), Sector.MINISECTOR_SIZE, de.Size,
                        null, SourceStream);
            }
            else
            {
                sView = new StreamView(GetSectorChain(de.StartSetc, SectorType.Normal), GetSectorSize(), de.Size, null,
                    SourceStream);
            }


            sView.Seek(position, SeekOrigin.Begin);
            var result = sView.Read(buffer, 0, count);

            return result;
        }

        internal int ReadData(ReadonlyCompoundFileStream cFStream, long position, byte[] buffer, int offset, int count)
        {
            var de = cFStream.DirEntry;

            count = (int)Math.Min(buffer.Length - offset, (long)count);

            StreamView sView = null;


            if (de.Size < _header.MinSizeStandardStream)
            {
                sView
                    = new StreamView(GetSectorChain(de.StartSetc, SectorType.Mini), Sector.MINISECTOR_SIZE, de.Size,
                        null, SourceStream);
            }
            else
            {
                sView = new StreamView(GetSectorChain(de.StartSetc, SectorType.Normal), GetSectorSize(), de.Size, null,
                    SourceStream);
            }


            sView.Seek(position, SeekOrigin.Begin);
            var result = sView.Read(buffer, offset, count);

            return result;
        }

        public void CopyTo(ReadonlyCompoundFileStream sourceCompoundFileStream, Stream destinationStream,
            IByteArrayPool byteArrayPool = null)
        {
            byteArrayPool ??= ByteArrayPool;
            SectorList sectorChain = null;
            var de = sourceCompoundFileStream.DirEntry;
            if (de.Size < _header.MinSizeStandardStream)
                //sectorChain = GetSectorChain(de.StartSetc, SectorType.Mini);
            {
                throw new NotSupportedException();
            }

            sectorChain = GetNormalSectorChainLowMemory(de.StartSetc);
            if (sectorChain == null)
            {
                return;
            }

            var reader = new ReadonlyStreamViewForSectorList(sectorChain, sectorChain.Count * GetSectorSize(),
                SourceStream, byteArrayPool);

            var count = de.Size;
            reader.CopyTo(destinationStream, byteArrayPool, 0, count);
        }

        internal byte[] GetData(ReadonlyCompoundFileStream cFStream)
        {
            if (IsClosed)
            {
                throw new CFDisposedException("Compound File closed: cannot access data");
            }

            byte[] result = null;

            var de = cFStream.DirEntry;

            //IDirectoryEntry root = directoryEntries[0];

            if (de.Size < _header.MinSizeStandardStream)
            {
                var miniView
                    = new StreamView(GetSectorChain(de.StartSetc, SectorType.Mini), Sector.MINISECTOR_SIZE, de.Size,
                        null, SourceStream);

                var br = new BinaryReader(miniView);

                result = br.ReadBytes((int)de.Size);
                br.Close();
            }
            else
            {
                var sView
                    = new StreamView(GetSectorChain(de.StartSetc, SectorType.Normal), GetSectorSize(), de.Size, null,
                        SourceStream);

                result = new byte[(int)de.Size];

                sView.Read(result, 0, result.Length);
            }

            return result;
        }

        public byte[] GetDataBySID(int sid)
        {
            if (IsClosed)
            {
                throw new CFDisposedException("Compound File closed: cannot access data");
            }

            if (sid < 0)
            {
                return null;
            }

            byte[] result = null;
            try
            {
                var de = _directoryEntries[sid];
                if (de.Size < _header.MinSizeStandardStream)
                {
                    var miniView
                        = new StreamView(GetSectorChain(de.StartSetc, SectorType.Mini), Sector.MINISECTOR_SIZE, de.Size,
                            null, SourceStream);
                    var br = new BinaryReader(miniView);
                    result = br.ReadBytes((int)de.Size);
                    br.Close();
                }
                else
                {
                    var sView
                        = new StreamView(GetSectorChain(de.StartSetc, SectorType.Normal), GetSectorSize(), de.Size,
                            null, SourceStream);
                    result = new byte[(int)de.Size];
                    sView.Read(result, 0, result.Length);
                }
            }
            catch
            {
                throw new CFException("Cannot get data for SID");
            }

            return result;
        }

        private static int Ceiling(double d)
        {
            return (int)Math.Ceiling(d);
        }

        private static int LowSaturation(int i)
        {
            return i > 0 ? i : 0;
        }

        internal void InvalidateDirectoryEntry(int sid)
        {
            if (sid >= _directoryEntries.Count)
            {
                throw new CFException("Invalid SID of the directory entry to remove");
            }

            ResetDirectoryEntry(sid);
        }

        /// <summary>
        ///     When called from user code, release all resources, otherwise, in the case runtime called it,
        ///     only unmanagd resources are released.
        /// </summary>
        /// <param name="disposing">If true, method has been called from User code, if false it's been called from .net runtime</param>
        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (!IsClosed)
                {
                    if (disposing)
                    {
                        // Call from user code...

                        if (Sectors != null)
                        {
                            Sectors.Clear();
                            Sectors = null;
                        }

                        RootStorage = null; // Some problem releasing resources...
                        _header = null;
                        _directoryEntries.Clear();
                        _directoryEntries = null;
                        //this.lockObject = null;
#if !FLAT_WRITE
                            this.buffer = null;
#endif
                    }

                    if (SourceStream != null && CloseStream && !Configuration.HasFlag(CFSConfiguration.LeaveOpen))
                    {
                        SourceStream.Close();
                    }
                }
            }
            finally
            {
                IsClosed = true;
            }
        }

        internal IList<IDirectoryEntry> GetDirectories()
        {
            return _directoryEntries;
        }

        private IList<IDirectoryEntry> FindDirectoryEntries(string entryName)
        {
            var result = new List<IDirectoryEntry>();

            foreach (var d in _directoryEntries)
            {
                if (d.GetEntryName() == entryName && d.StgType != StgType.StgInvalid)
                {
                    result.Add(d);
                }
            }

            return result;
        }


        /// <summary>
        ///     Get a list of all entries with a given name contained in the document.
        /// </summary>
        /// <param name="entryName">Name of entries to retrive</param>
        /// <returns>A list of name-matching entries</returns>
        /// <remarks>
        ///     This function is aimed to speed up entity lookup in
        ///     flat-structure files (only one or little more known entries)
        ///     without the performance penalty related to entities hierarchy constraints.
        ///     There is no implied hierarchy in the returned list.
        /// </remarks>
        public IList<ReadonlyCompoundFileItem> GetAllNamedEntries(string entryName)
        {
            var r = FindDirectoryEntries(entryName);
            var result = new List<ReadonlyCompoundFileItem>();

            foreach (var id in r)
            {
                if (id.GetEntryName() == entryName && id.StgType != StgType.StgInvalid)
                {
                    ReadonlyCompoundFileItem i = id.StgType == StgType.StgStorage
                        ? new ReadonlyCompoundFileStorage(this, id)
                        : new ReadonlyCompoundFileStream(this, id);
                    result.Add(i);
                }
            }

            return result;
        }

        public int GetNumDirectories()
        {
            if (IsClosed)
            {
                throw new CFDisposedException("Compound File closed: cannot access data");
            }

            return _directoryEntries.Count;
        }

        public string GetNameDirEntry(int id)
        {
            if (IsClosed)
            {
                throw new CFDisposedException("Compound File closed: cannot access data");
            }

            if (id < 0)
            {
                throw new CFException("Invalid Storage ID");
            }

            return _directoryEntries[id].Name;
        }

        public StgType GetStorageType(int id)
        {
            if (IsClosed)
            {
                throw new CFDisposedException("Compound File closed: cannot access data");
            }

            if (id < 0)
            {
                throw new CFException("Invalid Storage ID");
            }

            return _directoryEntries[id].StgType;
        }
    }
}