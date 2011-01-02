using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using BinaryTrees;
using System.Collections;
using System.Security.AccessControl;

/*
     The contents of this file are subject to the Mozilla Public License
     Version 1.1 (the "License"); you may not use this file except in
     compliance with the License. You may obtain a copy of the License at
     http://www.mozilla.org/MPL/

     Software distributed under the License is distributed on an "AS IS"
     basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
     License for the specific language governing rights and limitations
     under the License.

     The Original Code is OpenMCDF - Compound Document Format library.

     The Initial Developer of the Original Code is Federico Blaseotto.
*/

namespace OleCompoundFileStorage
{
    internal class DirEntryComparer : IComparer<IDirectoryEntry>
    {
        public int Compare(IDirectoryEntry x, IDirectoryEntry y)
        {
            // X CompareTo Y : X > Y --> 1 ; X < Y  --> -1
            return (x.CompareTo(y));

            //Compare X < Y --> -1
        }
    }

    /// <summary>
    /// Binary File Format Version. Sector size  is 512 byte for version 3,
    /// 4096 for version 4
    /// </summary>
    public enum CFSVersion : int
    {
        /// <summary>
        /// Compound file version 3 - The default and most common version available. Sector size 512 bytes, 2GB max file size.
        /// </summary>
        Ver_3 = 3,
        /// <summary>
        /// Compound file version 4 - Sector size is 4096 bytes.
        /// </summary>
        Ver_4 = 4
    }

    /// <summary>
    /// Update mode of the compound file.
    /// Default is ReadOnly.
    /// </summary>
    public enum UpdateMode
    {
        /// <summary>
        /// ReadOnly update mode prevents overwriting
        /// of the opened file. 
        /// Data changes have to be persisted on a different
        /// file when required.
        /// </summary>
        ReadOnly,
        /// <summary>
        /// Transacted mode allows subsequent data changing operations
        /// to be persisted directly on the opened file 
        /// using the <see cref="M:OleCompoundFileStorage.CompoundFile.Commit">Commit</see>
        /// method when required. Warning: this option may cause existing data loss if misused.
        /// </summary>
        Transacted
    }

    /// <summary>
    /// Standard Microsoft&#169; Compound File implementation.
    /// It is also known as OLE/COM structured storage 
    /// and contains a hierarchy of storage and stream objects providing
    /// efficent storage of multiple kinds of documents in a single file.
    /// </summary>
    public class CompoundFile : IDisposable
    {
        /// <summary>
        /// Returns the size of standard sectors switching on CFS version (3 or 4)
        /// </summary>
        /// <returns>Standard sector size</returns>
        internal int GetSectorSize()
        {
            return 2 << (header.SectorShift - 1);
        }

        /// <summary>
        /// Number of DIFAT entries in the header
        /// </summary>
        private const int HEADER_DIFAT_ENTRIES_COUNT = 109;

        /// <summary>
        /// Number of FAT entries in a DIFAT Sector
        /// </summary>
        private readonly int DIFAT_SECTOR_FAT_ENTRIES_COUNT = 127;

        /// <summary>
        /// Sectors ID entries in a FAT Sector
        /// </summary>
        private readonly int FAT_SECTOR_ENTRIES_COUNT = 128;

        /// <summary>
        /// Sector ID Size (int)
        /// </summary>
        private const int SIZE_OF_SID = 4;

        /// <summary>
        /// Flag for sector recycling.
        /// </summary>
        private bool sectorRecycle = false;


        /// <summary>
        /// Flag for unallocated sector zeroing out.
        /// </summary>
        private bool eraseFreeSectors = false;

        private BinaryReader streamReader;
        private BinaryWriter streamWriter;

        private ArrayList sectors = new ArrayList();

        private Header header;

        //private int nDirSectors = 0;

        /// <summary>
        /// Create a new, blank, standard compound file.
        /// Version of created compound file is setted to 3 and sector recycle is turned off
        /// to achieve the best reading/writing performance in most common scenarios.
        /// </summary>
        /// <example>
        /// <code>
        /// 
        ///     byte[] b = new byte[10000];
        ///     for (int i = 0; i &lt; 10000; i++)
        ///     {
        ///         b[i % 120] = (byte)i;
        ///     }
        ///
        ///     CompoundFile cf = new CompoundFile();
        ///     CFStream myStream = cf.RootStorage.AddStream("MyStream");
        ///
        ///     Assert.IsNotNull(myStream);
        ///     myStream.SetData(b);
        ///     cf.Save("MyCompoundFile.cfs");
        ///     cf.Close();
        ///     
        /// </code>
        /// </example>
        public CompoundFile()
        {
            this.header = new Header();
            this.sectorRecycle = false;

            DIFAT_SECTOR_FAT_ENTRIES_COUNT = (GetSectorSize() / 4) - 1;
            FAT_SECTOR_ENTRIES_COUNT = (GetSectorSize() / 4);

            //Root -- 
            rootStorage = new CFStorage(this);

            rootStorage.SetEntryName("Root Entry");
            rootStorage.StgType = StgType.StgRoot;
            rootStorage.StgColor = StgColor.Black;

            this.AddDirectoryEntry(rootStorage);
        }

        /// <summary>
        /// Create a new, blank, compound file.
        /// </summary>
        /// <param name="cfsVersion">Use a specific Compound File Version to set 512 or 4096 bytes sectors</param>
        /// <param name="sectorRecycle">If true, recycle unused sectors</param>
        /// <param name="eraseFreeSectors">If true, unallocated sectors will be overwritten with zeros</param>
        /// <example>
        /// <code>
        /// 
        ///     byte[] b = new byte[10000];
        ///     for (int i = 0; i &lt; 10000; i++)
        ///     {
        ///         b[i % 120] = (byte)i;
        ///     }
        ///
        ///     CompoundFile cf = new CompoundFile(CFSVersion.Ver_4, true);
        ///     CFStream myStream = cf.RootStorage.AddStream("MyStream");
        ///
        ///     Assert.IsNotNull(myStream);
        ///     myStream.SetData(b);
        ///     cf.Save("MyCompoundFile.cfs");
        ///     cf.Close();
        ///     
        /// </code>
        /// </example>
        /// <remarks>
        /// Sector recycling reduces data writing performances but avoids space wasting.
        /// </remarks>
        public CompoundFile(CFSVersion cfsVersion, bool sectorRecycle, bool eraseFreeSectors)
        {
            this.header = new Header((ushort)cfsVersion);
            this.sectorRecycle = sectorRecycle;

            DIFAT_SECTOR_FAT_ENTRIES_COUNT = (GetSectorSize() / 4) - 1;
            FAT_SECTOR_ENTRIES_COUNT = (GetSectorSize() / 4);

            //Root -- 
            rootStorage = new CFStorage(this);

            rootStorage.SetEntryName("Root Entry");
            rootStorage.StgType = StgType.StgRoot;
            rootStorage.StgColor = StgColor.Black;

            this.AddDirectoryEntry(rootStorage);
        }



        /// <summary>
        /// Load an existing compound file.
        /// </summary>
        /// <param name="fileName">Compound file to read from</param>
        /// <example>
        /// <code>
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
        /// File will be open in read-only mode: it has to be saved
        /// with a different filename. You have to provide a wrapping implementation
        /// in order to remove/substitute an existing file. Version will be
        /// automatically recognized from the file. Sector recycle is turned off
        /// to achieve the best reading/writing performance in most common scenarios.
        /// </remarks>
        public CompoundFile(String fileName)
        {
            this.sectorRecycle = false;
            this.updateMode = UpdateMode.ReadOnly;
            this.eraseFreeSectors = false;

            LoadFile(fileName);

            DIFAT_SECTOR_FAT_ENTRIES_COUNT = (GetSectorSize() / 4) - 1;
            FAT_SECTOR_ENTRIES_COUNT = (GetSectorSize() / 4);
        }

        /// <summary>
        /// Load an existing compound file.
        /// </summary>
        /// <param name="fileName">Compound file to read from</param>
        /// <param name="sectorRecycle">If true, recycle unused sectors</param>
        /// <param name="updateMode">Select the update mode of the underlying data file</param>
        /// <example>
        /// <code>
        /// String srcFilename = "data_YOU_CAN_CHANGE.xls";
        /// 
        /// CompoundFile cf = new CompoundFile(srcFilename, UpdateMode.Transacted, true);
        ///
        /// Random r = new Random();
        ///
        /// byte[] buffer = GetBuffer(r.Next(3, 4095), 0x0A);
        ///
        /// cf.RootStorage.AddStream("MyStream").SetData(buffer);
        /// 
        /// //This will persist data to the underlying media.
        /// cf.Commit();
        /// cf.Close();
        ///
        /// </code>
        /// </example>
        public CompoundFile(String fileName, UpdateMode updateMode, bool sectorRecycle, bool eraseFreeSectors)
        {
            this.sectorRecycle = sectorRecycle;
            this.updateMode = updateMode;
            this.eraseFreeSectors = eraseFreeSectors;

            LoadFile(fileName);

            DIFAT_SECTOR_FAT_ENTRIES_COUNT = (GetSectorSize() / 4) - 1;
            FAT_SECTOR_ENTRIES_COUNT = (GetSectorSize() / 4);
        }



        /// <summary>
        /// Load an existing compound file from a stream.
        /// </summary>
        /// <param name="fileName">Streamed compound file</param>
        /// <example>
        /// <code>
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
        public CompoundFile(Stream stream)
        {
            LoadStream(stream);

            DIFAT_SECTOR_FAT_ENTRIES_COUNT = (GetSectorSize() / 4) - 1;
            FAT_SECTOR_ENTRIES_COUNT = (GetSectorSize() / 4);
        }



        /// <summary>
        /// Load an existing compound file from a stream.
        /// </summary>
        /// <param name="stream">Streamed compound file</param>
        /// <param name="sectorRecycle">If true, recycle unused sectors</param>
        /// <example>
        /// <code>
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
        public CompoundFile(Stream stream, bool sectorRecycle)
        {
            this.updateMode = UpdateMode.ReadOnly;
            this.sectorRecycle = sectorRecycle;

            LoadStream(stream);


            DIFAT_SECTOR_FAT_ENTRIES_COUNT = (GetSectorSize() / 4) - 1;
            FAT_SECTOR_ENTRIES_COUNT = (GetSectorSize() / 4);
        }


        private UpdateMode updateMode = UpdateMode.ReadOnly;
        private String fileName = String.Empty;

        private void LoadFile(String fileName)
        {
            this.fileName = fileName;

            this.header = new Header();
            this.directoryEntries = new List<IDirectoryEntry>();

            FileStream fs = null;

            if (this.updateMode == UpdateMode.ReadOnly)
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                streamWriter = null;
            }
            else
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                streamWriter = new BinaryWriter(fs);
            }


            streamReader = new BinaryReader(fs);

            header.Read(streamReader);

            int n_sector = Ceiling((double)((fs.Length - GetSectorSize()) / GetSectorSize()));

            this.sectors = new ArrayList(n_sector);

            for (int i = 0; i < n_sector; i++)
            {
                sectors.Add(null);
            }

            LoadDirectories();

            this.rootStorage
                = new CFStorage(this, directoryEntries[0]);
        }

        /// <summary>
        /// Commit data changes since the previously commit operation
        /// to the underlying compound file on the disk.
        /// </summary>
        /// <remarks>
        /// This method can be used
        /// only if <see cref="T:OleCompoundFileStorage.CompoundFile">CompoundFile</see> 
        /// has been opened in <see cref="T:OleCompoundFileStorage.UpdateMode">Transacted mode</see>.
        /// </remarks>
        public void UpdateFile()
        {
            UpdateFile(false);
        }

        /// <summary>
        /// Commit data changes since the previously commit operation
        /// to the underlying compound file on the disk.
        /// </summary>
        /// <param name="releaseMemory">If true, release loaded sectors to limit memory usage but reduces following read operations</param>
        /// <remarks>
        /// This method can be used
        /// only if <see cref="T:OleCompoundFileStorage.CompoundFile">CompoundFile</see> 
        /// has been opened in <see cref="T:OleCompoundFileStorage.UpdateMode">Transacted mode</see>.
        /// </remarks>
        public void UpdateFile(bool releaseMemory)
        {
            if (_disposed)
                throw new CFException("Compound File closed: cannot commit data");

            if (updateMode != UpdateMode.Transacted)
                throw new CFException("Cannot commit data in Read-Only update mode");

            if (!streamWriter.BaseStream.CanSeek)
                throw new CFException("Cannot commit data to a non seekable media");

            //FileStream fs = null;
            //BinaryWriter bw = null;

            try
            {
                //fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);
                //bw = new BinaryWriter(fs);

                streamWriter.BaseStream.Seek(0, SeekOrigin.Begin);
                streamWriter.Write((byte[])Array.CreateInstance(typeof(byte), GetSectorSize()));

                SaveDirectory();

                for (int i = 0; i < sectors.Count; i++)
                {
                    // Note:
                    // Here sectors should not be loaded dynamically because
                    // if they are null it means that no change has involved them;

                    Sector s = sectors[i] as Sector;

                    if (s != null && s.DirtyFlag)
                    {
                        streamWriter.BaseStream.Seek(GetSectorSize() + i * GetSectorSize(), SeekOrigin.Begin);
                        streamWriter.Write(s.Data);

                        if (releaseMemory)
                            sectors[i] = null;
                    }

                }

                // Seek to beginning position and save header (first 512 or 4096 bytes)
                streamWriter.BaseStream.Seek(0, SeekOrigin.Begin);
                header.Write(streamWriter);


            }
            catch (Exception ex)
            {
                throw new CFException("Error while committing data", ex);
            }
        }

        private void LoadStream(Stream stream)
        {
            this.header = new Header();
            this.directoryEntries = new List<IDirectoryEntry>();
            this.updateMode = UpdateMode.ReadOnly;

            Stream temp = null;

            if (stream.CanSeek)
            {
                temp = stream;
            }
            else
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                MemoryStream ms = new MemoryStream(buffer);
                temp = ms;
            }

            temp.Seek(0, SeekOrigin.Begin);

            if (streamReader != null)
            {
                streamReader.Close();
            }

            streamReader = new BinaryReader(temp);

            header.Read(streamReader);


            int n_sector = Ceiling((double)((temp.Length - GetSectorSize()) / GetSectorSize()));

            sectors = new ArrayList(n_sector);

            for (int i = 0; i < n_sector; i++)
            {
                sectors.Add(null);
            }

            LoadDirectories();

            this.rootStorage
                = new CFStorage(this, directoryEntries[0]);
        }

        /// <summary>
        /// Return true if this compound file has been 
        /// loaded from an existing file.
        /// </summary>
        public bool IsFileMapped
        {
            get { return streamReader != null; }
        }


        /// <summary>
        /// Allocate space, setup sectors id and refresh header
        /// for the new or updated mini sector chain.
        /// </summary>
        /// <param name="sectorChain">The new MINI sector chain</param>
        private void SetMiniSectorChain(List<Sector> sectorChain)
        {
            List<Sector> miniFAT
                = GetSectorChain(header.FirstMiniFATSectorID, SectorType.Normal);

            List<Sector> miniStream
                = GetSectorChain(RootEntry.StartSetc, SectorType.Normal);

            StreamView miniFATView
                = new StreamView(miniFAT, GetSectorSize(), header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE);

            StreamView miniStreamView
                = new StreamView(miniStream, GetSectorSize(), this.rootStorage.Size);

            // Set updated/new sectors within the ministream
            for (int i = 0; i < sectorChain.Count; i++)
            {
                Sector s = sectorChain[i];

                if (s.IsAllocated)
                {
                    // Overwrite
                    miniStreamView.Seek(Sector.MINISECTOR_SIZE * s.Id, SeekOrigin.Begin);
                    miniStreamView.Write(s.Data, 0, Sector.MINISECTOR_SIZE);
                }
                else
                {
                    // Allocate, position ministream at the end of already allocated
                    // ministream's sectors

                    miniStreamView.Seek(this.rootStorage.Size, SeekOrigin.Begin);
                    miniStreamView.Write(s.Data, 0, Sector.MINISECTOR_SIZE);
                    s.Id = (int)(miniStreamView.Position - Sector.MINISECTOR_SIZE) / Sector.MINISECTOR_SIZE;

                    this.rootStorage.Size = miniStreamView.Length;
                }
            }

            // Update miniFAT
            for (int i = 0; i < sectorChain.Count - 1; i++)
            {
                Int32 currentId = sectorChain[i].Id;
                Int32 nextId = sectorChain[i + 1].Id;

                AssureLength(miniFATView, Math.Max(currentId * SIZE_OF_SID, nextId * SIZE_OF_SID));

                miniFATView.Seek(currentId * 4, SeekOrigin.Begin);
                miniFATView.Write(BitConverter.GetBytes(nextId), 0, 4);
            }

            AssureLength(miniFATView, sectorChain[sectorChain.Count - 1].Id * SIZE_OF_SID);

            // Write End of Chain in MiniFAT
            miniFATView.Seek(sectorChain[sectorChain.Count - 1].Id * SIZE_OF_SID, SeekOrigin.Begin);
            miniFATView.Write(BitConverter.GetBytes(Sector.ENDOFCHAIN), 0, 4);

            // Update sector chains
            SetNormalSectorChain(miniStreamView.BaseSectorChain);
            SetNormalSectorChain(miniFATView.BaseSectorChain);

            //Update HEADER and root storage when ministream changes
            if (miniFAT.Count > 0)
            {
                ((IDirectoryEntry)this.rootStorage).StartSetc = miniStream[0].Id;
                header.MiniFATSectorsNumber = (uint)miniFAT.Count;
                header.FirstMiniFATSectorID = miniFAT[0].Id;
            }
        }

        private void FreeChain(List<Sector> sectorChain, bool zeroSector)
        {
            byte[] ZEROED_SECTOR = new byte[GetSectorSize()];

            List<Sector> FAT
                = GetSectorChain(-1, SectorType.FAT);

            StreamView FATView
                = new StreamView(FAT, GetSectorSize(), FAT.Count * GetSectorSize());

            // Zeroes out sector data (if requested)

            if (zeroSector)
            {
                for (int i = 0; i < sectorChain.Count; i++)
                {
                    Sector s = sectorChain[i];
                    s.ZeroData();
                }
            }

            // Update FAT marking unallocated sectors
            for (int i = 0; i < sectorChain.Count - 1; i++)
            {
                Int32 currentId = sectorChain[i].Id;
                Int32 nextId = sectorChain[i + 1].Id;

                AssureLength(FATView, Math.Max(currentId * SIZE_OF_SID, nextId * SIZE_OF_SID));

                FATView.Seek(currentId * 4, SeekOrigin.Begin);
                FATView.Write(BitConverter.GetBytes(Sector.FREESECT), 0, 4);
            }
        }

        private void FreeMiniChain(List<Sector> sectorChain, bool zeroSector)
        {

            byte[] ZEROED_MINI_SECTOR = new byte[Sector.MINISECTOR_SIZE];

            List<Sector> miniFAT
                = GetSectorChain(header.FirstMiniFATSectorID, SectorType.Normal);

            List<Sector> miniStream
                = GetSectorChain(RootEntry.StartSetc, SectorType.Normal);

            StreamView miniFATView
                = new StreamView(miniFAT, GetSectorSize(), header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE);

            StreamView miniStreamView
                = new StreamView(miniStream, GetSectorSize(), this.rootStorage.Size);

            // Set updated/new sectors within the ministream
            if (zeroSector)
            {
                for (int i = 0; i < sectorChain.Count; i++)
                {
                    Sector s = sectorChain[i];

                    if (s.IsAllocated)
                    {
                        // Overwrite
                        miniStreamView.Seek(Sector.MINISECTOR_SIZE * s.Id, SeekOrigin.Begin);
                        miniStreamView.Write(ZEROED_MINI_SECTOR, 0, Sector.MINISECTOR_SIZE);
                    }
                }
            }

            // Update miniFAT
            for (int i = 0; i < sectorChain.Count - 1; i++)
            {
                Int32 currentId = sectorChain[i].Id;
                Int32 nextId = sectorChain[i + 1].Id;

                AssureLength(miniFATView, Math.Max(currentId * SIZE_OF_SID, nextId * SIZE_OF_SID));

                miniFATView.Seek(currentId * 4, SeekOrigin.Begin);
                miniFATView.Write(BitConverter.GetBytes(Sector.FREESECT), 0, 4);
            }

            AssureLength(miniFATView, sectorChain[sectorChain.Count - 1].Id * SIZE_OF_SID);

            // Write End of Chain in MiniFAT
            miniFATView.Seek(sectorChain[sectorChain.Count - 1].Id * SIZE_OF_SID, SeekOrigin.Begin);
            miniFATView.Write(BitConverter.GetBytes(Sector.FREESECT), 0, 4);

            // Update sector chains
            SetNormalSectorChain(miniStreamView.BaseSectorChain);
            SetNormalSectorChain(miniFATView.BaseSectorChain);

            //Update HEADER and root storage when ministream changes
            if (miniFAT.Count > 0)
            {
                ((IDirectoryEntry)this.rootStorage).StartSetc = miniStream[0].Id;
                header.MiniFATSectorsNumber = (uint)miniFAT.Count;
                header.FirstMiniFATSectorID = miniFAT[0].Id;
            }
        }

        /// <summary>
        /// Allocate space, setup sectors id and refresh header
        /// for the new or updated sector chain.
        /// </summary>
        /// <param name="sectorChain">The new or updated generic sector chain</param>
        private void SetNormalSectorChain(List<Sector> sectorChain)
        {

            foreach (Sector s in sectorChain)
            {
                if (!s.IsAllocated)
                {
                    //int freeId =fatScan.GetFreeSectorID();

                    //if (freeId != Sector.ENDOFCHAIN)
                    //{
                    //}

                    if (header.MajorVersion == (ushort)CFSVersion.Ver_4)
                        CheckTransactionLockSector();

                    sectors.Add(s);
                    s.Id = sectors.Count - 1;
                }
            }

            SetFATSectorChain(sectorChain);
        }

        private void CheckTransactionLockSector()
        {
            int sSize = GetSectorSize();

            if (sSize * (sectors.Count + 2) > 0x7FFFFF00)
            {
                Sector rangeLockSector = new Sector(GetSectorSize());
                rangeLockSector.Id = sectors.Count - 1;
                rangeLockSector.Type = SectorType.RangeLockSector;
                sectors.Add(new Sector(GetSectorSize()));
            }
        }

        /// <summary>
        /// Allocate space, setup sectors id and refresh header
        /// for the new or updated FAT sector chain.
        /// </summary>
        /// <param name="sectorChain">The new or updated generic sector chain</param>
        private void SetFATSectorChain(List<Sector> sectorChain)
        {
            StreamView fatStream =
                new StreamView(
                    GetSectorChain(-1, SectorType.FAT),
                    GetSectorSize(),
                    header.FATSectorsNumber * GetSectorSize()
                    );

            AssureLength(fatStream, (sectorChain.Count * 4));

            // Write FAT chain values --

            for (int i = 0; i < sectorChain.Count - 1; i++)
            {
                if (header.MajorVersion == (ushort)CFSVersion.Ver_4)
                {
                    if (sectorChain[i].Type == SectorType.RangeLockSector)
                    {
                        fatStream.Seek(sectorChain[i].Id * 4, SeekOrigin.Begin);
                        fatStream.Write(BitConverter.GetBytes(Sector.ENDOFCHAIN), 0, 4);
                        continue;
                    }
                }

                Sector sN = sectorChain[i + 1];
                Sector sC = sectorChain[i];

                fatStream.Seek(sC.Id * 4, SeekOrigin.Begin);
                fatStream.Write(BitConverter.GetBytes(sN.Id), 0, 4);
            }

            fatStream.Seek(sectorChain[sectorChain.Count - 1].Id * 4, SeekOrigin.Begin);
            fatStream.Write(BitConverter.GetBytes(Sector.ENDOFCHAIN), 0, 4);

            //SetDIFATSectorChain(fatStream.BaseSectorChain);

            // Merge chain to CFS
            SetDIFATSectorChain(fatStream.BaseSectorChain);
        }

        /// <summary>
        /// Setup the DIFAT sector chain
        /// </summary>
        /// <param name="FATsectorChain">A FAT sector chain</param>
        private void SetDIFATSectorChain(List<Sector> FATsectorChain)
        {
            // Get initial sector's count
            header.FATSectorsNumber = FATsectorChain.Count;

            // Allocate Sectors
            foreach (Sector s in FATsectorChain)
            {
                if (!s.IsAllocated)
                {
                    sectors.Add(s);
                    s.Id = sectors.Count - 1;
                    s.Type = SectorType.FAT;
                }
            }

            // Sector count...
            int nCurrentSectors = sectors.Count;

            // Temp DIFAT count
            int nDIFATSectors = (int)header.DIFATSectorsNumber;

            if (FATsectorChain.Count > HEADER_DIFAT_ENTRIES_COUNT)
            {
                nDIFATSectors = Ceiling((double)(FATsectorChain.Count - HEADER_DIFAT_ENTRIES_COUNT) / DIFAT_SECTOR_FAT_ENTRIES_COUNT);
                nDIFATSectors = LowSaturation(nDIFATSectors - (int)header.DIFATSectorsNumber); //required DIFAT
            }

            // ...sum with new required DIFAT sectors count
            nCurrentSectors += nDIFATSectors;

            // ReCheck FAT bias
            while (header.FATSectorsNumber * FAT_SECTOR_ENTRIES_COUNT < nCurrentSectors)
            {
                Sector extraFATSector = new Sector(GetSectorSize());
                sectors.Add(extraFATSector);

                extraFATSector.Id = sectors.Count - 1;
                extraFATSector.Type = SectorType.FAT;

                FATsectorChain.Add(extraFATSector);

                header.FATSectorsNumber++;
                nCurrentSectors++;

                //... so, adding a FAT sector may induce DIFAT sectors to increase by one
                // and consequently this may induce ANOTHER FAT sector (TO-THINK: Could this condition occure ?)
                if (nDIFATSectors * DIFAT_SECTOR_FAT_ENTRIES_COUNT <
                    (header.FATSectorsNumber > HEADER_DIFAT_ENTRIES_COUNT ?
                    header.FATSectorsNumber - HEADER_DIFAT_ENTRIES_COUNT :
                    0))
                {
                    nDIFATSectors++;
                    nCurrentSectors++;
                }
            }


            List<Sector> difatSectors =
                        GetSectorChain(-1, SectorType.DIFAT);

            StreamView difatStream
                = new StreamView(difatSectors, GetSectorSize());

            AssureLength(difatStream, nDIFATSectors * GetSectorSize());

            // Write DIFAT Sectors (if required)
            // Save room for the following chaining
            for (int i = 0; i < FATsectorChain.Count; i++)
            {
                if (i < HEADER_DIFAT_ENTRIES_COUNT)
                {
                    header.DIFAT[i] = FATsectorChain[i].Id;
                }
                else
                {
                    // room for DIFAT chaining at the end of any DIFAT sector (4 bytes)
                    if (i != HEADER_DIFAT_ENTRIES_COUNT && (i - HEADER_DIFAT_ENTRIES_COUNT) % DIFAT_SECTOR_FAT_ENTRIES_COUNT == 0)
                    {
                        byte[] temp = new byte[sizeof(int)];
                        difatStream.Write(temp, 0, sizeof(int));
                    }

                    difatStream.Write(BitConverter.GetBytes(FATsectorChain[i].Id), 0, sizeof(int));

                }
            }

            // Allocate room for DIFAT sectors
            for (int i = 0; i < difatStream.BaseSectorChain.Count; i++)
            {
                if (!difatStream.BaseSectorChain[i].IsAllocated)
                {
                    sectors.Add(difatStream.BaseSectorChain[i]);
                    difatStream.BaseSectorChain[i].Id = sectors.Count - 1;
                    difatStream.BaseSectorChain[i].Type = SectorType.DIFAT;
                }
            }

            header.DIFATSectorsNumber = (uint)nDIFATSectors;


            // Chain first sector
            if (difatStream.BaseSectorChain != null && difatStream.BaseSectorChain.Count > 0)
            {
                header.FirstDIFATSectorID = difatStream.BaseSectorChain[0].Id;

                // Update header information
                header.DIFATSectorsNumber = (uint)difatStream.BaseSectorChain.Count;

                // Write chaining information at the end of DIFAT Sectors
                for (int i = 0; i < difatStream.BaseSectorChain.Count - 1; i++)
                {
                    Buffer.BlockCopy(
                        BitConverter.GetBytes(difatStream.BaseSectorChain[i + 1].Id),
                        0,
                        difatStream.BaseSectorChain[i].Data,
                        GetSectorSize() - sizeof(int),
                        4);
                }

                Buffer.BlockCopy(
                    BitConverter.GetBytes(Sector.ENDOFCHAIN),
                    0,
                    difatStream.BaseSectorChain[difatStream.BaseSectorChain.Count - 1].Data,
                    GetSectorSize() - sizeof(int),
                    sizeof(int)
                    );
            }
            else
                header.FirstDIFATSectorID = Sector.ENDOFCHAIN;

            // Mark DIFAT Sectors in FAT
            StreamView fatSv =
                new StreamView(FATsectorChain, GetSectorSize(), header.FATSectorsNumber * GetSectorSize());


            for (int i = 0; i < header.DIFATSectorsNumber; i++)
            {
                fatSv.Seek(difatStream.BaseSectorChain[i].Id * 4, SeekOrigin.Begin);
                fatSv.Write(BitConverter.GetBytes(Sector.DIFSECT), 0, 4);
            }

            for (int i = 0; i < header.FATSectorsNumber; i++)
            {
                fatSv.Seek(fatSv.BaseSectorChain[i].Id * 4, SeekOrigin.Begin);
                fatSv.Write(BitConverter.GetBytes(Sector.FATSECT), 0, 4);
            }

            //fatSv.Seek(fatSv.BaseSectorChain[fatSv.BaseSectorChain.Count - 1].Id * 4, SeekOrigin.Begin);
            //fatSv.Write(BitConverter.GetBytes(Sector.ENDOFCHAIN), 0, 4);

            header.FATSectorsNumber = fatSv.BaseSectorChain.Count;
        }

        /// <summary>
        /// Check for sector chain having enough sectors
        /// to get an amount of <typeparamref name="length"/>  bytes
        /// </summary>
        /// <param name="streamView">StreamView decorator for a sector chain</param>
        /// <param name="length">Amount of bytes to check for</param>
        private static void AssureLength(StreamView streamView, int length)
        {
            if (streamView != null && streamView.Length < length)
            {
                streamView.SetLength(length);
            }
        }

        /// <summary>
        /// Get the DIFAT Sector chain
        /// </summary>
        /// <returns>A list of DIFAT sectors</returns>
        private List<Sector> GetDifatSectorChain()
        {
            List<Sector> result
                = new List<Sector>();

            int nextSecID
               = Sector.ENDOFCHAIN;

            if (header.DIFATSectorsNumber != 0)
            {
                Sector s = sectors[header.FirstDIFATSectorID] as Sector;

                if (s == null) //Lazy loading
                {
                    s = Sector.LoadSector(header.FirstDIFATSectorID, streamReader, GetSectorSize());
                    s.Type = SectorType.DIFAT;
                    //s.Id = header.FirstDIFATSectorID;
                    //header.FirstDIFATSectorID = s.Id;  ?
                    sectors[header.FirstDIFATSectorID] = s;
                }

                result.Add(s);

                while (true)
                {
                    nextSecID = BitConverter.ToInt32(s.Data, GetSectorSize() - 4);

                    // Strictly speaking, the following condition is not correct from
                    // a specification point of view:
                    // only ENDOFCHAIN should break DIFAT chain but 
                    // a lot of existing compound files use FREESECT as DIFAT chain termination
                    if (nextSecID == Sector.FREESECT || nextSecID == Sector.ENDOFCHAIN) break;

                    if (sectors[nextSecID] == null)
                    {
                        sectors[nextSecID] = Sector.LoadSector(
                            nextSecID,
                            streamReader,
                           GetSectorSize()
                        );
                    }

                    s = sectors[nextSecID] as Sector;

                    result.Add(s);
                }
            }

            return result;
        }

        /// <summary>
        /// Get the FAT sector chain
        /// </summary>
        /// <returns>List of FAT sectors</returns>
        private List<Sector> GetFatSectorChain()
        {
            int N_HEADER_FAT_ENTRY = 109; //Number of FAT sectors id in the header

            List<Sector> result
               = new List<Sector>();

            int nextSecID
               = Sector.ENDOFCHAIN;

            List<Sector> difatSectors = GetDifatSectorChain();

            int idx = 0;

            // Read FAT entries from the header Fat entry array (max 109 entries)
            while (idx < header.FATSectorsNumber && idx < N_HEADER_FAT_ENTRY)
            {
                nextSecID = header.DIFAT[idx];

                if (sectors[nextSecID] == null && streamReader != null)
                {
                    sectors[nextSecID] = Sector.LoadSector(
                           nextSecID,
                           streamReader,
                           GetSectorSize());
                }

                result.Add(sectors[nextSecID] as Sector);

                idx++;
            }

            //Is there any DIFAT sector containing other FAT entries ?
            if (difatSectors.Count > 0)
            {
                StreamView difatStream
                    = new StreamView
                        (
                        difatSectors,
                        GetSectorSize(),
                        header.FATSectorsNumber > N_HEADER_FAT_ENTRY ?
                            (header.FATSectorsNumber - N_HEADER_FAT_ENTRY) * 4 :
                            0
                        );

                byte[] nextDIFATSectorBuffer = new byte[4];

                difatStream.Read(nextDIFATSectorBuffer, 0, 4);
                nextSecID = BitConverter.ToInt32(nextDIFATSectorBuffer, 0);

                int i = 0;
                int nFat = N_HEADER_FAT_ENTRY;

                while (nFat < header.FATSectorsNumber)
                {
                    if (difatStream.Position == ((GetSectorSize() - 4) + i * GetSectorSize()))
                    {
                        difatStream.Seek(4, SeekOrigin.Current);
                        i++;
                        continue;
                    }


                    if (sectors[nextSecID] == null && streamReader != null)
                    {
                        sectors[nextSecID] = Sector.LoadSector(
                               nextSecID,
                               streamReader,
                               GetSectorSize());
                    }

                    result.Add(sectors[nextSecID] as Sector);

                    difatStream.Read(nextDIFATSectorBuffer, 0, 4);
                    nextSecID = BitConverter.ToInt32(nextDIFATSectorBuffer, 0);
                    nFat++;
                }
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

            StreamView fatStream = new StreamView(fatSectors, GetSectorSize(), fatSectors.Count * GetSectorSize());

            while (true)
            {
                if (nextSecID == Sector.ENDOFCHAIN) break;

                if (sectors[nextSecID] == null && streamReader != null)
                {
                    sectors[nextSecID] = Sector.LoadSector(nextSecID, streamReader, GetSectorSize());
                }

                result.Add(sectors[nextSecID] as Sector);

                fatStream.Seek(nextSecID * 4, SeekOrigin.Begin);
                nextSecID = fatStream.ReadInt32();
            }


            return result;
        }

        /// <summary>
        /// Get a mini sector chain
        /// </summary>
        /// <param name="secID">First SecID of the required chain</param>
        /// <returns>A list of mini sectors (64 bytes)</returns>
        private List<Sector> GetMiniSectorChain(int secID)
        {
            List<Sector> result
                  = new List<Sector>();

            int nextSecID = secID;

            List<Sector> miniFAT = GetNormalSectorChain(header.FirstMiniFATSectorID);
            List<Sector> miniStream = GetNormalSectorChain(RootEntry.StartSetc);

            StreamView miniFATView = new StreamView(miniFAT, GetSectorSize(), header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE);
            StreamView miniStreamView = new StreamView(miniStream, GetSectorSize(), rootStorage.Size);

            BinaryReader miniFATReader = new BinaryReader(miniFATView);

            nextSecID = secID;

            while (true)
            {
                if (nextSecID == Sector.ENDOFCHAIN)
                    break;

                Sector ms = new Sector(Sector.MINISECTOR_SIZE);
                byte[] temp = new byte[Sector.MINISECTOR_SIZE];

                ms.Id = nextSecID;
                miniStreamView.Seek(nextSecID * Sector.MINISECTOR_SIZE, SeekOrigin.Begin);

                miniStreamView.Read(ms.Data, 0, Sector.MINISECTOR_SIZE);

                result.Add(ms);

                miniFATView.Seek(nextSecID * 4, SeekOrigin.Begin);
                nextSecID = miniFATReader.ReadInt32();
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

        private IDirectoryEntry rootStorage;

        /// <summary>
        /// The entry point object that represents the 
        /// root of the structures tree to get or set storage or
        /// stream data.
        /// <example>
        /// <code>
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
        /// </summary>
        public CFStorage RootStorage
        {
            get
            {
                return rootStorage as CFStorage;
            }
        }

        internal void AddDirectoryEntry(IDirectoryEntry de)
        {
            // Find first available invalid slot (if any)
            for (int i = 0; i < directoryEntries.Count; i++)
            {
                if (directoryEntries[i].StgType == StgType.StgInvalid)
                {
                    directoryEntries[i] = de;
                    de.SID = i;
                    return;
                }
            }

            // No invalid directory entry found
            directoryEntries.Add(de);
            de.SID = directoryEntries.Count - 1;
        }



        internal BinarySearchTree<IDirectoryEntry> GetChildrenTree(int sid)
        {
            BinarySearchTree<IDirectoryEntry> bst
                = new BinarySearchTree<IDirectoryEntry>(new DirEntryComparer());

            // Load children from their original tree.
            DoLoadChildren(bst, directoryEntries[sid]);

            // Rebuild of (Red)-Black tree of entry children.
            bst.VisitTreeInOrder(RefreshSIDs);

            return bst;
        }

        private void DoLoadChildren(BinarySearchTree<IDirectoryEntry> bst, IDirectoryEntry de)
        {
            if (de.Child != DirectoryEntry.NOSTREAM)
            {
                if (directoryEntries[de.Child].StgType == StgType.StgInvalid) return;

                if (directoryEntries[de.Child].StgType == StgType.StgStream)
                    bst.Add(new CFStream(this, directoryEntries[de.Child]));
                else
                {
                    CFStorage cfs
                        = new CFStorage(this, directoryEntries[de.Child]);

                    // Add the storage child
                    bst.Add(cfs);

                    BinarySearchTree<IDirectoryEntry> bstChild
                        = new BinarySearchTree<IDirectoryEntry>(new DirEntryComparer());

                    cfs.SetChildrenTree(bstChild);

                    // Recursive call to load __direct__ children
                    DoLoadChildren(bstChild, cfs);
                }

                DoLoadSiblings(bst, directoryEntries[de.Child]);
            }
        }

        private void DoLoadSiblings(BinarySearchTree<IDirectoryEntry> bst, IDirectoryEntry de)
        {
            if (de.LeftSibling != DirectoryEntry.NOSTREAM)
            {
                // If there're more left siblings load them...
                DoLoadSiblings(bst, directoryEntries[de.LeftSibling]);
            }

            if (directoryEntries[de.SID].StgType == StgType.StgStream)
                bst.Add(new CFStream(this, directoryEntries[de.SID]));
            else
            {
                CFStorage cfs = new CFStorage(this, directoryEntries[de.SID]);
                bst.Add(cfs);

                // If try to load children them
                if (((IDirectoryEntry)cfs).Child != DirectoryEntry.NOSTREAM)
                {
                    BinarySearchTree<IDirectoryEntry> bstSib
                        = new BinarySearchTree<IDirectoryEntry>(new DirEntryComparer());
                    cfs.SetChildrenTree(bstSib);
                    DoLoadChildren(bstSib, cfs);
                }
            }


            if (de.RightSibling != DirectoryEntry.NOSTREAM)
            {
                // If there're more right siblings load them...
                DoLoadSiblings(bst, directoryEntries[de.RightSibling]);
            }
        }


        /// <summary>
        /// Load directory entries from compound file. Header and FAT MUST be already loaded.
        /// </summary>
        private void LoadDirectories()
        {
            List<Sector> directoryChain
                = GetSectorChain(header.FirstDirectorySectorID, SectorType.Normal);

            if (header.FirstDirectorySectorID == Sector.ENDOFCHAIN)
                header.FirstDirectorySectorID = directoryChain[0].Id;

            BinaryReader dirReader
                = new BinaryReader(new StreamView(directoryChain, GetSectorSize(), directoryChain.Count * GetSectorSize()));


            while (dirReader.BaseStream.Position < directoryChain.Count * GetSectorSize())
            {
                DirectoryEntry de
                = new DirectoryEntry(StgType.StgInvalid);

                de.Read(dirReader);
                this.AddDirectoryEntry(de);
            }
        }

        internal void RefreshSIDs(BinaryTreeNode<IDirectoryEntry> Node)
        {
            if (Node.Value != null)
            {
                if (Node.Left != null && (Node.Left.Value.StgType != StgType.StgInvalid))
                {
                    Node.Value.LeftSibling = Node.Left.Value.SID;
                }
                else
                {
                    Node.Value.LeftSibling = DirectoryEntry.NOSTREAM;
                }

                if (Node.Right != null && (Node.Right.Value.StgType != StgType.StgInvalid))
                {
                    Node.Value.RightSibling = Node.Right.Value.SID;
                }
                else
                {
                    Node.Value.RightSibling = DirectoryEntry.NOSTREAM;
                }
            }
        }

        internal void RefreshIterative(BinaryTreeNode<IDirectoryEntry> node)
        {
            if (node == null) return;
            RefreshSIDs(node);
            RefreshIterative(node.Left);
            RefreshIterative(node.Right);
        }

        private void SaveDirectory()
        {
            const int DIRECTORY_SIZE = 128;

            List<Sector> directorySectors
                = GetSectorChain(header.FirstDirectorySectorID, SectorType.Normal);

            StreamView sv = new StreamView(directorySectors, GetSectorSize(), 0);
            BinaryWriter bw = new BinaryWriter(sv);

            foreach (IDirectoryEntry di in directoryEntries)
            {
                di.Write(bw);
            }

            int delta = directoryEntries.Count;

            while (delta % (GetSectorSize() / DIRECTORY_SIZE) != 0)
            {
                DirectoryEntry dummy = new DirectoryEntry(StgType.StgInvalid);
                dummy.Write(bw);
                delta++;
            }

            SetNormalSectorChain(directorySectors);

            header.FirstDirectorySectorID = directorySectors[0].Id;

            //Version 4 supports directory sectors count
            if (header.MajorVersion == 3)
            {
                header.DirectorySectorsNumber = 0;
            }
            else
            {
                header.DirectorySectorsNumber = directorySectors.Count;
            }

            bw.Flush();
            bw.Close();
        }


        /// <summary>
        /// Saves the in-memory image of Compound File to a file.
        /// </summary>
        /// <param name="fileName">File name to write the compound file to</param>
        public void Save(String fileName)
        {
            if (_disposed)
                throw new CFException("Compound File closed: cannot save data");

            FileStream fs = null;

            try
            {
                fs = new FileStream(fileName, FileMode.Create);
                Save(fs);
            }
            catch (Exception ex)
            {
                throw new CFException("Error saving file [" + fileName + "]", ex);
            }
            finally
            {
                if (fs != null)
                    fs.Flush();

                if (fs != null)
                    fs.Close();

            }
        }

        /// <summary>
        /// Saves the in-memory image of Compound File to a stream.
        /// <remarks>
        /// A non-seekable media (like a network stream) induces a 
        /// slightly performance penalty.
        /// </remarks>
        /// </summary>
        /// <param name="stream">The stream to save compound File to</param>
        public void Save(Stream stream)
        {
            if (_disposed)
                throw new CFException("Compound File closed: cannot save data");

            Stream tempStream = null;

            if (!stream.CanSeek)
                tempStream = new MemoryStream();
            else
                tempStream = stream;

            BinaryWriter bw = new BinaryWriter(tempStream);

            bw.Write((byte[])Array.CreateInstance(typeof(byte), GetSectorSize()));

            SaveDirectory();

            if (streamReader != null)
                streamReader.BaseStream.Seek(0, SeekOrigin.Begin);

            for (int i = 0; i < sectors.Count; i++)
            {
                Sector s = sectors[i] as Sector;

                if (streamReader != null && s == null)
                {
                    s = Sector.LoadSector(i, streamReader, GetSectorSize());
                    sectors[i] = s;
                }

                bw.Write(s.Data);
            }

            bw.BaseStream.Seek(0, SeekOrigin.Begin);
            header.Write(bw);

            bw.Flush();

            if (!stream.CanSeek)
            {
                ((MemoryStream)tempStream).WriteTo(stream);
            }
        }


        /// <summary>
        /// Scan FAT o miniFAT for free sectors to reuse.
        /// </summary>
        /// <param name="sType">Type of sector to look for</param>
        /// <returns>A stack of available sectors or minisectors already allocated</returns>
        internal Stack<Sector> FindFreeSectors(SectorType sType)
        {
            Stack<Sector> freeList = new Stack<Sector>();

            if (sType == SectorType.Normal)
            {

                List<Sector> FatChain = GetSectorChain(-1, SectorType.FAT);
                StreamView fatStream = new StreamView(FatChain, GetSectorSize());

                int ptr = 0;

                while (ptr < sectors.Count)
                {
                    int id = fatStream.ReadInt32();
                    ptr += 4;

                    if (id == Sector.FREESECT)
                    {
                        freeList.Push(sectors[ptr - 4] as Sector);
                    }
                }
            }
            else
            {
                List<Sector> miniFAT
                    = GetSectorChain(header.FirstMiniFATSectorID, SectorType.Normal);

                StreamView miniFATView
                    = new StreamView(miniFAT, GetSectorSize(), header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE);

                List<Sector> miniStream
                    = GetSectorChain(RootEntry.StartSetc, SectorType.Normal);

                StreamView miniStreamView
                    = new StreamView(miniStream, GetSectorSize(), rootStorage.Size);

                long ptr = 0;

                int nMinisectors = (int)(miniStreamView.Length / Sector.MINISECTOR_SIZE);

                while (ptr < nMinisectors)
                {
                    //AssureLength(miniStreamView, (int)miniFATView.Length);

                    int id = miniFATView.ReadInt32();
                    ptr += 4;

                    if (id == Sector.FREESECT)
                    {
                        Sector ms = new Sector(Sector.MINISECTOR_SIZE);
                        byte[] temp = new byte[Sector.MINISECTOR_SIZE];

                        ms.Id = (int)((ptr - 4) / 4);
                        ms.Type = SectorType.Mini;

                        miniStreamView.Seek(ms.Id * Sector.MINISECTOR_SIZE, SeekOrigin.Begin);
                        miniStreamView.Read(ms.Data, 0, Sector.MINISECTOR_SIZE);

                        freeList.Push(ms);
                    }

                }
            }

            return freeList;
        }

        internal void SetData(IDirectoryEntry directoryEntry, Byte[] buffer)
        {
            SetStreamData(directoryEntry, buffer);
        }

        /// <summary>
        /// INTERNAL DEVELOPMENT. DO NOT CALL.
        /// </summary>
        /// <param name="directoryEntry"></param>
        /// <param name="buffer"></param>
        internal void AppendData(IDirectoryEntry directoryEntry, Byte[] buffer)
        {
            //CheckFileLength();

            if (buffer == null)
                throw new CFException("Parameter [buffer] cannot be null");

            //Quick and dirty :-)
            if (buffer.Length == 0) return;

            SectorType _st = SectorType.Normal;
            int _sectorSize = GetSectorSize();

            if (buffer.Length + directoryEntry.Size < header.MinSizeStandardStream)
            {
                _st = SectorType.Mini;
                _sectorSize = Sector.MINISECTOR_SIZE;
            }

            // Check for transition ministream -> stream:
            // Only in this case we need to free old sectors,
            // otherwise they will be overwritten.

            int streamSize = (int)directoryEntry.Size;
            byte[] temp = null;

            if (directoryEntry.StartSetc != Sector.ENDOFCHAIN)
            {
                if ((directoryEntry.Size + buffer.Length) > header.MinSizeStandardStream && directoryEntry.Size < header.MinSizeStandardStream)
                {
                    temp = new byte[streamSize];

                    StreamView miniData
                        = new StreamView(GetMiniSectorChain(directoryEntry.StartSetc), Sector.MINISECTOR_SIZE);

                    miniData.Read(temp, 0, streamSize);
                    FreeMiniChain(GetMiniSectorChain(directoryEntry.StartSetc), this.eraseFreeSectors);

                    directoryEntry.StartSetc = Sector.ENDOFCHAIN;
                    directoryEntry.Size = 0;
                }
            }

            List<Sector> sectorChain
                = GetSectorChain(directoryEntry.StartSetc, _st);

            Stack<Sector> freeList = FindFreeSectors(_st); // Collect available free sectors

            StreamView sv = new StreamView(sectorChain, _sectorSize, buffer.Length, freeList);

            if (temp != null)
            {
                sv.Seek(0, SeekOrigin.Begin);
                sv.Write(temp, 0, streamSize);
            }

            sv.Seek(streamSize, SeekOrigin.Begin);
            sv.Write(buffer, 0, buffer.Length);

            switch (_st)
            {
                case SectorType.Normal:
                    SetNormalSectorChain(sv.BaseSectorChain);
                    break;

                case SectorType.Mini:
                    SetMiniSectorChain(sv.BaseSectorChain);
                    break;
            }


            if (sv.BaseSectorChain.Count > 0)
            {
                directoryEntry.StartSetc = sv.BaseSectorChain[0].Id;
                directoryEntry.Size = buffer.Length + streamSize;
            }
            else
            {
                directoryEntry.StartSetc = Sector.ENDOFCHAIN;
                directoryEntry.Size = 0;
            }
        }

        private void SetStreamData(IDirectoryEntry directoryEntry, Byte[] buffer)
        {
            //CheckFileLength();

            if (buffer == null)
                throw new CFException("Parameter [buffer] cannot be null");

            //Quick and dirty :-)
            if (buffer.Length == 0) return;

            SectorType _st = SectorType.Normal;
            int _sectorSize = GetSectorSize();

            if (buffer.Length < header.MinSizeStandardStream)
            {
                _st = SectorType.Mini;
                _sectorSize = Sector.MINISECTOR_SIZE;
            }

            // Check for transition ministream -> stream:
            // Only in this case we need to free old sectors,
            // otherwise they will be overwritten.

            if (directoryEntry.StartSetc != Sector.ENDOFCHAIN)
            {
                if (
                    (buffer.Length < header.MinSizeStandardStream && directoryEntry.Size > header.MinSizeStandardStream)
                    || (buffer.Length > header.MinSizeStandardStream && directoryEntry.Size < header.MinSizeStandardStream)
                   )
                {

                    if (directoryEntry.Size < header.MinSizeStandardStream)
                    {
                        FreeMiniChain(GetMiniSectorChain(directoryEntry.StartSetc), this.eraseFreeSectors);
                    }
                    else
                    {
                        FreeChain(GetNormalSectorChain(directoryEntry.StartSetc), this.eraseFreeSectors);
                    }

                    directoryEntry.Size = 0;
                    directoryEntry.StartSetc = Sector.ENDOFCHAIN;
                }
            }

            List<Sector> sectorChain
                = GetSectorChain(directoryEntry.StartSetc, _st);

            Stack<Sector> freeList = null;

            if (this.sectorRecycle)
                freeList = FindFreeSectors(_st); // Collect available free sectors

            StreamView sv = new StreamView(sectorChain, _sectorSize, buffer.Length, freeList);

            sv.Write(buffer, 0, buffer.Length);

            switch (_st)
            {
                case SectorType.Normal:
                    SetNormalSectorChain(sv.BaseSectorChain);
                    break;

                case SectorType.Mini:
                    SetMiniSectorChain(sv.BaseSectorChain);
                    break;
            }


            if (sv.BaseSectorChain.Count > 0)
            {
                directoryEntry.StartSetc = sv.BaseSectorChain[0].Id;
                directoryEntry.Size = buffer.Length;
            }
            else
            {
                directoryEntry.StartSetc = Sector.ENDOFCHAIN;
                directoryEntry.Size = 0;
            }
        }

        /// <summary>
        /// Check file size limit ( 2GB for version 3 )
        /// </summary>
        private void CheckFileLength()
        {
            throw new NotImplementedException();
        }


        internal byte[] GetData(CFStream cFStream, long offset, ref int count)
        {

            byte[] result = null;
            IDirectoryEntry de = cFStream as IDirectoryEntry;

            count = (int)Math.Min((long)(de.Size - offset), (long)count);

            StreamView sView = null;


            if (de.Size < header.MinSizeStandardStream)
            {
                sView
                    = new StreamView(GetSectorChain(de.StartSetc, SectorType.Mini), Sector.MINISECTOR_SIZE, de.Size);
            }
            else
            {

                sView = new StreamView(GetSectorChain(de.StartSetc, SectorType.Normal), GetSectorSize(), de.Size);
            }

            result = new byte[(int)(count)];


            sView.Seek(offset, SeekOrigin.Begin);
            sView.Read(result, 0, result.Length);


            return result;
        }


        internal byte[] GetData(CFStream cFStream)
        {

            if (_disposed)
                throw new CFDisposedException("Compound File closed: cannot access data");

            byte[] result = null;

            IDirectoryEntry de = cFStream as IDirectoryEntry;

            //IDirectoryEntry root = directoryEntries[0];

            if (de.Size < header.MinSizeStandardStream)
            {

                StreamView miniView
                    = new StreamView(GetSectorChain(de.StartSetc, SectorType.Mini), Sector.MINISECTOR_SIZE, de.Size);

                BinaryReader br = new BinaryReader(miniView);

                result = br.ReadBytes((int)de.Size);
                br.Close();

            }
            else
            {
                StreamView sView
                    = new StreamView(GetSectorChain(de.StartSetc, SectorType.Normal), GetSectorSize(), de.Size);

                result = new byte[(int)de.Size];

                sView.Read(result, 0, result.Length);

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


        internal void RemoveDirectoryEntry(int sid)
        {
            if (sid >= directoryEntries.Count)
                throw new CFException("Invalid SID of the directory entry to remove");

            if (directoryEntries[sid].StgType == StgType.StgStream)
            {
                // Clear the associated stream (or ministream)
                if (directoryEntries[sid].Size < header.MinSizeStandardStream)
                {
                    List<Sector> miniChain
                        = GetSectorChain(directoryEntries[sid].StartSetc, SectorType.Mini);
                    FreeMiniChain(miniChain, this.eraseFreeSectors);
                }
                else
                {
                    List<Sector> chain
                        = GetSectorChain(directoryEntries[sid].StartSetc, SectorType.Normal);
                    FreeChain(chain, this.eraseFreeSectors);
                }
            }


            Random r = new Random();
            directoryEntries[sid].SetEntryName("_DELETED_NAME_" + r.Next(short.MaxValue).ToString());
            directoryEntries[sid].StgType = StgType.StgInvalid;
        }

        /// <summary>
        /// Close the Compound File object <see cref="T:OLECompoundFileStorage.CompoundFile">CompoundFile</see> and
        /// free all associated resources (e.g. open file handle and allocated memory).
        /// <remarks>
        /// When the <see cref="T:OLECompoundFileStorage.CompoundFile.Close()">Close</see> method is called,
        /// all the associated stream and storage objects are invalidated:
        /// any operation invoked on them will produce a <see cref="T:OLECompoundFileStorage.CFDisposedException">CFDisposedException</see>.
        /// </remarks>
        /// </summary>
        /// <example>
        /// <code>
        ///    const String FILENAME = "CompoundFile.cfs";
        ///    CompoundFile cf = new CompoundFile(FILENAME);
        ///
        ///    CFStorage st = cf.RootStorage.GetStorage("MyStorage");
        ///    cf.Close();
        ///
        ///    try
        ///    {
        ///        byte[] temp = st.GetStream("MyStream").GetData();
        ///        
        ///        // The following line will fail because back-end object has been closed
        ///        Assert.Fail("Stream without media");
        ///    }
        ///    catch (Exception ex)
        ///    {
        ///        Assert.IsTrue(ex is CFDisposedException);
        ///    }
        /// </code>
        /// </example>
        public void Close()
        {
            ((IDisposable)this).Dispose();
        }

        #region IDisposable Members

        private bool _disposed;//false

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private object lockObject = new Object();

        /// <summary>
        /// When called from user code, release all resources, otherwise, in the case runtime called it,
        /// only unmanagd resources are released.
        /// </summary>
        /// <param name="disposing">If true, method has been called from User code, if false it's been called from .net runtime</param>
        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (!_disposed)
                {
                    lock (lockObject)
                    {
                        if (disposing)
                        {
                            // Call from user code...

                            if (sectors != null)
                            {
                                sectors.Clear();
                                sectors = null;
                            }

                            this.rootStorage = null; // Some problem releasing resources...
                            this.header = null;
                            this.directoryEntries.Clear();
                            this.directoryEntries = null;
                            this.fileName = null;
                            this.lockObject = null;
                        }

                        if (this.streamReader != null)
                        {
                            streamReader.Close();
                            streamReader = null;
                        }

                        if (this.streamWriter != null)
                        {
                            streamWriter.Close();
                            streamWriter = null;
                        }



                    }
                }
            }
            finally
            {
                _disposed = true;
            }

        }

        internal bool IsClosed
        {
            get
            {
                return _disposed;
            }
        }

        private List<IDirectoryEntry> directoryEntries
            = new List<IDirectoryEntry>();

        //internal List<IDirectoryEntry> DirectoryEntries
        //{
        //    get { return directoryEntries; }
        //}


        internal IDirectoryEntry RootEntry
        {
            get
            {
                return directoryEntries[0];
            }
        }

        internal int FindSID(String entryName)
        {
            int result = -1;

            int count = 0;

            foreach (DirectoryEntry d in directoryEntries)
            {
                if (d.GetEntryName() == entryName)
                    return count;

                count++;
            }

            return result;
        }

        /// <summary>
        /// Compress free space removing unallocated sectors from compound file
        /// effectively reducing its size. 
        /// This method is meant to be called after multiple structures
        /// removal in order to avoid space wasting.
        /// </summary>
        /// <remarks>
        /// This method will cause a full load and cloning
        /// of sectors introducing a slightly performance penalty.
        /// </remarks>
        public void CompressFreeSpace()
        {
            using (CompoundFile tempCF = new CompoundFile((CFSVersion)this.header.MajorVersion, this.sectorRecycle, this.eraseFreeSectors))
            {
                DoCompression(this.RootStorage, tempCF.RootStorage);

                MemoryStream tmpMS = new MemoryStream();

                tempCF.Save(tmpMS);
                tempCF.Close();

                tmpMS.Seek(0, SeekOrigin.Begin);

                this.LoadStream(tmpMS);
            }
        }

        /// <summary>
        /// Recursively clones valid structures, avoiding to copy free sectors.
        /// </summary>
        /// <param name="currSrcStorage">Current source storage to clone</param>
        /// <param name="currDstStorage">Current cloned destination storage</param>
        private void DoCompression(CFStorage currSrcStorage, CFStorage currDstStorage)
        {
            VisitedEntryAction va =
                delegate(CFItem item)
                {
                    if (item.IsStream)
                    {
                        CFStream itemAsStream = item as CFStream;
                        CFStream st = ((CFStorage)currDstStorage).AddStream(itemAsStream.Name);
                        st.SetData(itemAsStream.GetData());
                    }
                    else if (item.IsStorage)
                    {
                        CFStorage itemAsStorage = item as CFStorage;
                        CFStorage strg = ((CFStorage)currDstStorage).AddStorage(itemAsStorage.Name);
                        strg.CLSID = itemAsStorage.CLSID;
                        DoCompression(itemAsStorage, strg); // recursion, one level deeper
                    }
                };

            currSrcStorage.VisitEntries(va, false);
        }
    }
}