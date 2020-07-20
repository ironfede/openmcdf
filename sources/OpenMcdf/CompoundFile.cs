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
    internal class CFItemComparer : IComparer<CFItem>
    {
        public int Compare(CFItem x, CFItem y)
        {
            // X CompareTo Y : X > Y --> 1 ; X < Y  --> -1
            return (x.DirEntry.CompareTo(y.DirEntry));

            //Compare X < Y --> -1
        }
    }

    /// <summary>
    /// Configuration parameters for the compund files.
    /// They can be OR-combined to configure 
    /// <see cref="T:OpenMcdf.CompoundFile">Compound file</see> behaviour.
    /// All flags are NOT set by Default.
    /// </summary>
    [Flags]
    public enum CFSConfiguration
    {
        /// <summary>
        /// Sector Recycling turn off, 
        /// free sectors erasing off, 
        /// format validation exception raised
        /// </summary>
        Default = 1,

        /// <summary>
        /// Sector recycling reduces data writing performances 
        /// but avoids space wasting in scenarios with frequently
        /// data manipulation of the same streams.
        /// </summary>
        SectorRecycle = 2,

        /// <summary>
        /// Free sectors are erased to avoid information leakage
        /// </summary>
        EraseFreeSectors = 4,

        /// <summary>
        /// No exception is raised when a validation error occurs.
        /// This can possibly lead to a security issue but gives 
        /// a chance to corrupted files to load.
        /// </summary>
        NoValidationException = 8,

        /// <summary>
        /// If this flag is set true,
        /// backing stream is kept open after CompoundFile disposal
        /// </summary>
        LeaveOpen = 16,
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
        /// Compound file version 4 - Sector size is 4096 bytes. Using this version could bring some compatibility problem with existing applications.
        /// </summary>
        Ver_4 = 4
    }

    /// <summary>
    /// Update mode of the compound file.
    /// Default is ReadOnly.
    /// </summary>
    public enum CFSUpdateMode
    {
        /// <summary>
        /// ReadOnly update mode prevents overwriting
        /// of the opened file. 
        /// Data changes are allowed but they have to be 
        /// persisted on a different file when required 
        /// using <see cref="M:OpenMcdf.CompoundFile.Save">method</see>
        /// </summary>
        ReadOnly,

        /// <summary>
        /// Update mode allows subsequent data changing operations
        /// to be persisted directly on the opened file or stream
        /// using the <see cref="M:OpenMcdf.CompoundFile.Commit">Commit</see>
        /// method when required. Warning: this option may cause existing data loss if misused.
        /// </summary>
        Update
    }

    /// <summary>
    /// Standard Microsoft&#169; Compound File implementation.
    /// It is also known as OLE/COM structured storage 
    /// and contains a hierarchy of storage and stream objects providing
    /// efficent storage of multiple kinds of documents in a single file.
    /// Version 3 and 4 of specifications are supported.
    /// </summary>
    public class CompoundFile : IDisposable
    {
        private CFSConfiguration configuration
            = CFSConfiguration.Default;

        /// <summary>
        /// Get the configuration parameters of the CompoundFile object.
        /// </summary>
        public CFSConfiguration Configuration
        {
            get
            {
                return configuration;
            }
        }

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

        /// <summary>
        /// Initial capacity of the flushing queue used
        /// to optimize commit writing operations
        /// </summary>
        private const int FLUSHING_QUEUE_SIZE = 6000;

        /// <summary>
        /// Maximum size of the flushing buffer used
        /// to optimize commit writing operations
        /// </summary>
        private const int FLUSHING_BUFFER_MAX_SIZE = 1024 * 1024 * 16;


        private SectorCollection sectors = new SectorCollection();


        /// <summary>
        /// CompoundFile header
        /// </summary>
        private Header header;

        /// <summary>
        /// Compound underlying stream. Null when new CF has been created.
        /// </summary>
        internal Stream sourceStream = null;


        /// <summary>
        /// Create a blank, version 3 compound file.
        /// Sector recycle is turned off to achieve the best reading/writing 
        /// performance in most common scenarios.
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

            this.sectors.OnVer3SizeLimitReached += new Ver3SizeLimitReached(OnSizeLimitReached);

            DIFAT_SECTOR_FAT_ENTRIES_COUNT = (GetSectorSize() / 4) - 1;
            FAT_SECTOR_ENTRIES_COUNT = (GetSectorSize() / 4);

            //Root -- 
            IDirectoryEntry de = DirectoryEntry.New("Root Entry", StgType.StgRoot, directoryEntries);
            rootStorage = new CFStorage(this, de);
            rootStorage.DirEntry.StgType = StgType.StgRoot;
            rootStorage.DirEntry.StgColor = StgColor.Black;

            //this.InsertNewDirectoryEntry(rootStorage.DirEntry);
        }

        void OnSizeLimitReached()
        {

            Sector rangeLockSector = new Sector(GetSectorSize(), sourceStream);
            sectors.Add(rangeLockSector);

            rangeLockSector.Type = SectorType.RangeLockSector;

            _transactionLockAdded = true;
            _lockSectorId = rangeLockSector.Id;
        }


        /// <summary>
        /// Create a new, blank, compound file.
        /// </summary>
        /// <param name="cfsVersion">Use a specific Compound File Version to set 512 or 4096 bytes sectors</param>
        /// <param name="configFlags">Set <see cref="T:OpenMcdf.CFSConfiguration">configuration</see> parameters for the new compound file</param>
        /// <example>
        /// <code>
        /// 
        ///     byte[] b = new byte[10000];
        ///     for (int i = 0; i &lt; 10000; i++)
        ///     {
        ///         b[i % 120] = (byte)i;
        ///     }
        ///
        ///     CompoundFile cf = new CompoundFile(CFSVersion.Ver_4, CFSConfiguration.Default);
        ///     CFStream myStream = cf.RootStorage.AddStream("MyStream");
        ///
        ///     Assert.IsNotNull(myStream);
        ///     myStream.SetData(b);
        ///     cf.Save("MyCompoundFile.cfs");
        ///     cf.Close();
        ///     
        /// </code>
        /// </example>
        public CompoundFile(CFSVersion cfsVersion, CFSConfiguration configFlags)
        {
            this.configuration = configFlags;

            bool sectorRecycle = configFlags.HasFlag(CFSConfiguration.SectorRecycle);
            bool eraseFreeSectors = configFlags.HasFlag(CFSConfiguration.EraseFreeSectors);

            this.header = new Header((ushort)cfsVersion);
            this.sectorRecycle = sectorRecycle;


            DIFAT_SECTOR_FAT_ENTRIES_COUNT = (GetSectorSize() / 4) - 1;
            FAT_SECTOR_ENTRIES_COUNT = (GetSectorSize() / 4);

            //Root -- 
            IDirectoryEntry rootDir = DirectoryEntry.New("Root Entry", StgType.StgRoot, directoryEntries);
            rootDir.StgColor = StgColor.Black;
            //this.InsertNewDirectoryEntry(rootDir);

            rootStorage = new CFStorage(this, rootDir);


            //
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
        /// with a different filename. A wrapping implementation has to be provided 
        /// in order to remove/substitute an existing file. Version will be
        /// automatically recognized from the file. Sector recycle is turned off
        /// to achieve the best reading/writing performance in most common scenarios.
        /// </remarks>
        public CompoundFile(String fileName)
        {
            this.sectorRecycle = false;
            this.updateMode = CFSUpdateMode.ReadOnly;
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
        /// <param name="eraseFreeSectors">If true, overwrite with zeros unallocated sectors</param>
        /// <example>
        /// <code>
        /// String srcFilename = "data_YOU_CAN_CHANGE.xls";
        /// 
        /// CompoundFile cf = new CompoundFile(srcFilename, UpdateMode.Update, true, true);
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
        public CompoundFile(String fileName, CFSUpdateMode updateMode, CFSConfiguration configParameters)
        {
            this.configuration = configParameters;
            this.validationExceptionEnabled = !configParameters.HasFlag(CFSConfiguration.NoValidationException);
            this.sectorRecycle = configParameters.HasFlag(CFSConfiguration.SectorRecycle);
            this.updateMode = updateMode;
            this.eraseFreeSectors = configParameters.HasFlag(CFSConfiguration.EraseFreeSectors);

            LoadFile(fileName);

            DIFAT_SECTOR_FAT_ENTRIES_COUNT = (GetSectorSize() / 4) - 1;
            FAT_SECTOR_ENTRIES_COUNT = (GetSectorSize() / 4);
        }

        private bool validationExceptionEnabled = true;

        public bool ValidationExceptionEnabled
        {
            get { return validationExceptionEnabled; }
        }


        /// <summary>
        /// Load an existing compound file.
        /// </summary>
        /// <param name="stream">A stream containing a compound file to read</param>
        /// <param name="sectorRecycle">If true, recycle unused sectors</param>
        /// <param name="updateMode">Select the update mode of the underlying data file</param>
        /// <param name="eraseFreeSectors">If true, overwrite with zeros unallocated sectors</param>
        /// <example>
        /// <code>
        /// 
        /// String filename = "reportREAD.xls";
        ///   
        /// FileStream fs = new FileStream(filename, FileMode.Open);
        /// CompoundFile cf = new CompoundFile(fs, UpdateMode.ReadOnly, false, false);
        /// CFStream foundStream = cf.RootStorage.GetStream("Workbook");
        ///
        /// byte[] temp = foundStream.GetData();
        ///
        /// Assert.IsNotNull(temp);
        ///
        /// cf.Close();
        ///
        /// </code>
        /// </example>
        /// <exception cref="T:OpenMcdf.CFException">Raised when trying to open a non-seekable stream</exception>
        /// <exception cref="T:OpenMcdf.CFException">Raised stream is null</exception>
        public CompoundFile(Stream stream, CFSUpdateMode updateMode, CFSConfiguration configParameters)
        {
            this.configuration = configParameters;
            this.validationExceptionEnabled = !configParameters.HasFlag(CFSConfiguration.NoValidationException);
            this.sectorRecycle = configParameters.HasFlag(CFSConfiguration.SectorRecycle);
            this.eraseFreeSectors = configParameters.HasFlag(CFSConfiguration.EraseFreeSectors);
            this.closeStream = !configParameters.HasFlag(CFSConfiguration.LeaveOpen);

            this.updateMode = updateMode;
            LoadStream(stream);

            DIFAT_SECTOR_FAT_ENTRIES_COUNT = (GetSectorSize() / 4) - 1;
            FAT_SECTOR_ENTRIES_COUNT = (GetSectorSize() / 4);
        }


        /// <summary>
        /// Load an existing compound file from a stream.
        /// </summary>
        /// <param name="stream">Streamed compound file</param>
        /// <example>
        /// <code>
        /// 
        /// String filename = "reportREAD.xls";
        ///   
        /// FileStream fs = new FileStream(filename, FileMode.Open);
        /// CompoundFile cf = new CompoundFile(fs);
        /// CFStream foundStream = cf.RootStorage.GetStream("Workbook");
        ///
        /// byte[] temp = foundStream.GetData();
        ///
        /// Assert.IsNotNull(temp);
        ///
        /// cf.Close();
        ///
        /// </code>
        /// </example>
        /// <exception cref="T:OpenMcdf.CFException">Raised when trying to open a non-seekable stream</exception>
        /// <exception cref="T:OpenMcdf.CFException">Raised stream is null</exception>
        public CompoundFile(Stream stream)
        {
            LoadStream(stream);

            DIFAT_SECTOR_FAT_ENTRIES_COUNT = (GetSectorSize() / 4) - 1;
            FAT_SECTOR_ENTRIES_COUNT = (GetSectorSize() / 4);
        }

        private CFSUpdateMode updateMode = CFSUpdateMode.ReadOnly;
        private String fileName = String.Empty;



        /// <summary>
        /// Commit data changes since the previously commit operation
        /// to the underlying supporting stream or file on the disk.
        /// </summary>
        /// <remarks>
        /// This method can be used
        /// only if the supporting stream has been opened in 
        /// <see cref="T:OpenMcdf.UpdateMode">Update mode</see>.
        /// </remarks>
        public void Commit()
        {
            Commit(false);
        }

#if !FLAT_WRITE
        private byte[] buffer = new byte[FLUSHING_BUFFER_MAX_SIZE];
        private Queue<Sector> flushingQueue = new Queue<Sector>(FLUSHING_QUEUE_SIZE);
#endif


        /// <summary>
        /// Commit data changes since the previously commit operation
        /// to the underlying supporting stream or file on the disk.
        /// </summary>
        /// <param name="releaseMemory">If true, release loaded sectors to limit memory usage but reduces following read operations performance</param>
        /// <remarks>
        /// This method can be used only if 
        /// the supporting stream has been opened in 
        /// <see cref="T:OpenMcdf.UpdateMode">Update mode</see>.
        /// </remarks>
        public void Commit(bool releaseMemory)
        {
            if (_disposed)
                throw new CFDisposedException("Compound File closed: cannot commit data");

            if (updateMode != CFSUpdateMode.Update)
                throw new CFInvalidOperation("Cannot commit data in Read-Only update mode");

            //try
            //{
#if !FLAT_WRITE

            int sId = -1;
            int sCount = 0;
            int bufOffset = 0;
#endif
            int sSize = GetSectorSize();

            if (header.MajorVersion != (ushort)CFSVersion.Ver_3)
                CheckForLockSector();

            sourceStream.Seek(0, SeekOrigin.Begin);
            sourceStream.Write((byte[])Array.CreateInstance(typeof(byte), GetSectorSize()), 0, sSize);

            CommitDirectory();

            bool gap = true;


            for (int i = 0; i < sectors.Count; i++)
            {
#if FLAT_WRITE

                //Note:
                //Here sectors should not be loaded dynamically because
                //if they are null it means that no change has involved them;

                Sector s = (Sector)sectors[i];

                if (s != null && s.DirtyFlag)
                {
                    if (gap)
                        sourceStream.Seek((long)((long)(sSize) + (long)i * (long)sSize), SeekOrigin.Begin);

                    sourceStream.Write(s.GetData(), 0, sSize);
                    sourceStream.Flush();
                    s.DirtyFlag = false;
                    gap = false;

                }
                else
                {
                    gap = true;
                }

                if (s != null && releaseMemory)
                {

                    s.ReleaseData();
                    s = null;
                    sectors[i] = null;
                }



#else
               

                Sector s = sectors[i] as Sector;


                if (s != null && s.DirtyFlag && flushingQueue.Count < (int)(buffer.Length / sSize))
                {
                    //First of a block of contiguous sectors, mark id, start enqueuing

                    if (gap)
                    {
                        sId = s.Id;
                        gap = false;
                    }

                    flushingQueue.Enqueue(s);


                }
                else
                {
                    //Found a gap, stop enqueuing, flush a write operation

                    gap = true;
                    sCount = flushingQueue.Count;

                    if (sCount == 0) continue;

                    bufOffset = 0;
                    while (flushingQueue.Count > 0)
                    {
                        Sector r = flushingQueue.Dequeue();
                        Buffer.BlockCopy(r.GetData(), 0, buffer, bufOffset, sSize);
                        r.DirtyFlag = false;

                        if (releaseMemory)
                        {
                            r.ReleaseData();
                        }

                        bufOffset += sSize;
                    }

                    sourceStream.Seek(((long)sSize + (long)sId * (long)sSize), SeekOrigin.Begin);
                    sourceStream.Write(buffer, 0, sCount * sSize);

               

                    //Console.WriteLine("W - " + (int)(sCount * sSize ));

                }
#endif
            }

#if !FLAT_WRITE
            sCount = flushingQueue.Count;
            bufOffset = 0;

            while (flushingQueue.Count > 0)
            {
                Sector r = flushingQueue.Dequeue();
                Buffer.BlockCopy(r.GetData(), 0, buffer, bufOffset, sSize);
                r.DirtyFlag = false;

                if (releaseMemory)
                {
                    r.ReleaseData();
                    r = null;
                }

                bufOffset += sSize;
            }

            if (sCount != 0)
            {
                sourceStream.Seek((long)sSize + (long)sId * (long)sSize, SeekOrigin.Begin);
                sourceStream.Write(buffer, 0, sCount * sSize);
                //Console.WriteLine("W - " + (int)(sCount * sSize));
            }

#endif

            // Seek to beginning position and save header (first 512 or 4096 bytes)
            sourceStream.Seek(0, SeekOrigin.Begin);
            header.Write(sourceStream);

            sourceStream.SetLength((long)(sectors.Count + 1) * sSize);
            sourceStream.Flush();

            if (releaseMemory)
                GC.Collect();

            //}
            //catch (Exception ex)
            //{
            //    throw new CFException("Internal error while committing data", ex);
            //}
        }

        /// <summary>
        /// Load compound file from an existing stream.
        /// </summary>
        /// <param name="stream">Stream to load compound file from</param>
        private void Load(Stream stream)
        {
            try
            {
                this.header = new Header();
                this.directoryEntries = new List<IDirectoryEntry>();

                this.sourceStream = stream;

                header.Read(stream);

                int n_sector = Ceiling(((double)(stream.Length - GetSectorSize()) / (double)GetSectorSize()));

                if (stream.Length > 0x7FFFFF0)
                    this._transactionLockAllocated = true;


                sectors = new SectorCollection();
                //sectors = new ArrayList();
                for (int i = 0; i < n_sector; i++)
                {
                    sectors.Add(null);
                }

                LoadDirectories();

                this.rootStorage
                    = new CFStorage(this, directoryEntries[0]);
            }
            catch (Exception)
            {
                if (stream != null && closeStream)
                    stream.Close();

                throw;
            }
        }

        private void LoadFile(String fileName)
        {

            this.fileName = fileName;

            FileStream fs = null;

            try
            {
                if (this.updateMode == CFSUpdateMode.ReadOnly)
                {
                    fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                }
                else
                {
                    fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                }

                Load(fs);

            }
            catch
            {
                if (fs != null)
                    fs.Close();

                throw;
            }
        }

        private void LoadStream(Stream stream)
        {
            if (stream == null)
                throw new CFException("Stream parameter cannot be null");

            if (!stream.CanSeek)
                throw new CFException("Cannot load a non-seekable Stream");


            stream.Seek(0, SeekOrigin.Begin);

            Load(stream);
        }

        /// <summary>
        /// Return true if this compound file has been 
        /// loaded from an existing file or stream
        /// </summary>
        public bool HasSourceStream
        {
            get { return sourceStream != null; }
        }


        private void PersistMiniStreamToStream(List<Sector> miniSectorChain)
        {
            List<Sector> miniStream
                = GetSectorChain(RootEntry.StartSetc, SectorType.Normal);

            StreamView miniStreamView
                = new StreamView(
                    miniStream,
                    GetSectorSize(),
                    this.rootStorage.Size,
                    null,
                    sourceStream);

            for (int i = 0; i < miniSectorChain.Count; i++)
            {
                Sector s = miniSectorChain[i];

                if (s.Id == -1)
                    throw new CFException("Invalid minisector index");

                // Ministream sectors already allocated
                miniStreamView.Seek(Sector.MINISECTOR_SIZE * s.Id, SeekOrigin.Begin);
                miniStreamView.Write(s.GetData(), 0, Sector.MINISECTOR_SIZE);
            }
        }

        /// <summary>
        /// Allocate space, setup sectors id and refresh header
        /// for the new or updated mini sector chain.
        /// </summary>
        /// <param name="sectorChain">The new MINI sector chain</param>
        private void AllocateMiniSectorChain(List<Sector> sectorChain)
        {
            List<Sector> miniFAT
                = GetSectorChain(header.FirstMiniFATSectorID, SectorType.Normal);

            List<Sector> miniStream
                = GetSectorChain(RootEntry.StartSetc, SectorType.Normal);

            StreamView miniFATView
                = new StreamView(
                    miniFAT,
                    GetSectorSize(),
                    header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE,
                    null,
                    this.sourceStream,
                    true
                    );

            StreamView miniStreamView
                = new StreamView(
                    miniStream,
                    GetSectorSize(),
                    this.rootStorage.Size,
                    null,
                    sourceStream);


            // Set updated/new sectors within the ministream
            // We are writing data in a NORMAL Sector chain.
            for (int i = 0; i < sectorChain.Count; i++)
            {
                Sector s = sectorChain[i];

                if (s.Id == -1)
                {
                    // Allocate, position ministream at the end of already allocated
                    // ministream's sectors

                    miniStreamView.Seek(this.rootStorage.Size + Sector.MINISECTOR_SIZE, SeekOrigin.Begin);
                    //miniStreamView.Write(s.GetData(), 0, Sector.MINISECTOR_SIZE);
                    s.Id = (int)(miniStreamView.Position - Sector.MINISECTOR_SIZE) / Sector.MINISECTOR_SIZE;

                    this.rootStorage.DirEntry.Size = miniStreamView.Length;
                }
            }

            // Update miniFAT
            for (int i = 0; i < sectorChain.Count - 1; i++)
            {
                Int32 currentId = sectorChain[i].Id;
                Int32 nextId = sectorChain[i + 1].Id;

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
                this.rootStorage.DirEntry.StartSetc = miniStream[0].Id;
                header.MiniFATSectorsNumber = (uint)miniFAT.Count;
                header.FirstMiniFATSectorID = miniFAT[0].Id;
            }
        }

        internal void FreeData(CFStream stream)
        {
            if (stream.Size == 0)
                return;

            List<Sector> sectorChain = null;

            if (stream.Size < header.MinSizeStandardStream)
            {
                sectorChain = GetSectorChain(stream.DirEntry.StartSetc, SectorType.Mini);
                FreeMiniChain(sectorChain, this.eraseFreeSectors);
            }
            else
            {
                sectorChain = GetSectorChain(stream.DirEntry.StartSetc, SectorType.Normal);
                FreeChain(sectorChain, this.eraseFreeSectors);
            }

            stream.DirEntry.StartSetc = Sector.ENDOFCHAIN;
            stream.DirEntry.Size = 0;
        }

        private void FreeChain(List<Sector> sectorChain, bool zeroSector)
        {
            FreeChain(sectorChain, 0, zeroSector);
        }

        private void FreeChain(List<Sector> sectorChain, int nth_sector_to_remove, bool zeroSector)
        {
            // Dummy zero buffer
            byte[] ZEROED_SECTOR = new byte[GetSectorSize()];

            List<Sector> FAT
                = GetSectorChain(-1, SectorType.FAT);

            StreamView FATView
                = new StreamView(FAT, GetSectorSize(), FAT.Count * GetSectorSize(), null, sourceStream);

            // Zeroes out sector data (if required)-------------
            if (zeroSector)
            {
                for (int i = nth_sector_to_remove; i < sectorChain.Count; i++)
                {
                    Sector s = sectorChain[i];
                    s.ZeroData();
                }
            }

            // Update FAT marking unallocated sectors ----------
            for (int i = nth_sector_to_remove; i < sectorChain.Count; i++)
            {
                Int32 currentId = sectorChain[i].Id;

                FATView.Seek(currentId * 4, SeekOrigin.Begin);
                FATView.Write(BitConverter.GetBytes(Sector.FREESECT), 0, 4);
            }

            // Write new end of chain if partial free ----------
            if (nth_sector_to_remove > 0 && sectorChain.Count > 0)
            {
                FATView.Seek(sectorChain[nth_sector_to_remove - 1].Id * 4, SeekOrigin.Begin);
                FATView.Write(BitConverter.GetBytes(Sector.ENDOFCHAIN), 0, 4);
            }
        }

        private void FreeMiniChain(List<Sector> sectorChain, bool zeroSector)
        {
            FreeMiniChain(sectorChain, 0, zeroSector);
        }

        private void FreeMiniChain(List<Sector> sectorChain, int nth_sector_to_remove, bool zeroSector)
        {
            byte[] ZEROED_MINI_SECTOR = new byte[Sector.MINISECTOR_SIZE];

            List<Sector> miniFAT
                = GetSectorChain(header.FirstMiniFATSectorID, SectorType.Normal);

            List<Sector> miniStream
                = GetSectorChain(RootEntry.StartSetc, SectorType.Normal);

            StreamView miniFATView
                = new StreamView(miniFAT, GetSectorSize(), header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE, null, sourceStream);

            StreamView miniStreamView
                = new StreamView(miniStream, GetSectorSize(), this.rootStorage.Size, null, sourceStream);

            // Set updated/new sectors within the ministream ----------
            if (zeroSector)
            {
                for (int i = nth_sector_to_remove; i < sectorChain.Count; i++)
                {
                    Sector s = sectorChain[i];

                    if (s.Id != -1)
                    {
                        // Overwrite
                        miniStreamView.Seek(Sector.MINISECTOR_SIZE * s.Id, SeekOrigin.Begin);
                        miniStreamView.Write(ZEROED_MINI_SECTOR, 0, Sector.MINISECTOR_SIZE);
                    }
                }
            }

            // Update miniFAT                ---------------------------------------
            for (int i = nth_sector_to_remove; i < sectorChain.Count; i++)
            {
                Int32 currentId = sectorChain[i].Id;

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
                this.rootStorage.DirEntry.StartSetc = miniStream[0].Id;
                header.MiniFATSectorsNumber = (uint)miniFAT.Count;
                header.FirstMiniFATSectorID = miniFAT[0].Id;
            }
        }

        /// <summary>
        /// Allocate space, setup sectors id in the FAT and refresh header
        /// for the new or updated sector chain (Normal or Mini sectors)
        /// </summary>
        /// <param name="sectorChain">The new or updated normal or mini sector chain</param>
        private void SetSectorChain(List<Sector> sectorChain)
        {
            if (sectorChain == null || sectorChain.Count == 0)
                return;

            SectorType _st = sectorChain[0].Type;

            if (_st == SectorType.Normal)
            {
                AllocateSectorChain(sectorChain);
            }
            else if (_st == SectorType.Mini)
            {
                AllocateMiniSectorChain(sectorChain);
            }
        }

        /// <summary>
        /// Allocate space, setup sectors id and refresh header
        /// for the new or updated sector chain.
        /// </summary>
        /// <param name="sectorChain">The new or updated generic sector chain</param>
        private void AllocateSectorChain(List<Sector> sectorChain)
        {

            foreach (Sector s in sectorChain)
            {
                if (s.Id == -1)
                {
                    sectors.Add(s);
                    s.Id = sectors.Count - 1;

                }
            }

            AllocateFATSectorChain(sectorChain);
        }

        internal bool _transactionLockAdded = false;
        internal int _lockSectorId = -1;
        internal bool _transactionLockAllocated = false;

        /// <summary>
        /// Check for transaction lock sector addition and mark it in the FAT.
        /// </summary>
        private void CheckForLockSector()
        {
            //If transaction lock has been added and not yet allocated in the FAT...
            if (_transactionLockAdded && !_transactionLockAllocated)
            {
                StreamView fatStream = new StreamView(GetFatSectorChain(), GetSectorSize(), sourceStream);

                fatStream.Seek(_lockSectorId * 4, SeekOrigin.Begin);
                fatStream.Write(BitConverter.GetBytes(Sector.ENDOFCHAIN), 0, 4);

                _transactionLockAllocated = true;
            }

        }
        /// <summary>
        /// Allocate space, setup sectors id and refresh header
        /// for the new or updated FAT sector chain.
        /// </summary>
        /// <param name="sectorChain">The new or updated generic sector chain</param>
        private void AllocateFATSectorChain(List<Sector> sectorChain)
        {
            List<Sector> fatSectors = GetSectorChain(-1, SectorType.FAT);

            StreamView fatStream =
                new StreamView(
                    fatSectors,
                    GetSectorSize(),
                    header.FATSectorsNumber * GetSectorSize(),
                    null,
                    sourceStream,
                    true
                    );

            // Write FAT chain values --

            for (int i = 0; i < sectorChain.Count - 1; i++)
            {

                Sector sN = sectorChain[i + 1];
                Sector sC = sectorChain[i];

                fatStream.Seek(sC.Id * 4, SeekOrigin.Begin);
                fatStream.Write(BitConverter.GetBytes(sN.Id), 0, 4);
            }

            fatStream.Seek(sectorChain[sectorChain.Count - 1].Id * 4, SeekOrigin.Begin);
            fatStream.Write(BitConverter.GetBytes(Sector.ENDOFCHAIN), 0, 4);

            // Merge chain to CFS
            AllocateDIFATSectorChain(fatStream.BaseSectorChain);
        }

        /// <summary>
        /// Setup the DIFAT sector chain
        /// </summary>
        /// <param name="FATsectorChain">A FAT sector chain</param>
        private void AllocateDIFATSectorChain(List<Sector> FATsectorChain)
        {
            // Get initial sector's count
            header.FATSectorsNumber = FATsectorChain.Count;

            // Allocate Sectors
            foreach (Sector s in FATsectorChain)
            {
                if (s.Id == -1)
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
                Sector extraFATSector = new Sector(GetSectorSize(), sourceStream);
                sectors.Add(extraFATSector);

                extraFATSector.Id = sectors.Count - 1;
                extraFATSector.Type = SectorType.FAT;

                FATsectorChain.Add(extraFATSector);

                header.FATSectorsNumber++;
                nCurrentSectors++;

                //... so, adding a FAT sector may induce DIFAT sectors to increase by one
                // and consequently this may induce ANOTHER FAT sector (TO-THINK: May this condition occure ?)
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
                = new StreamView(difatSectors, GetSectorSize(), sourceStream);

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
                if (difatStream.BaseSectorChain[i].Id == -1)
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
                header.FirstDIFATSectorID = Sector.ENDOFCHAIN;

            // Mark DIFAT Sectors in FAT
            StreamView fatSv =
                new StreamView(FATsectorChain, GetSectorSize(), header.FATSectorsNumber * GetSectorSize(), null, sourceStream);

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
        /// Get the DIFAT Sector chain
        /// </summary>
        /// <returns>A list of DIFAT sectors</returns>
        private List<Sector> GetDifatSectorChain()
        {
            int validationCount = 0;

            List<Sector> result
                = new List<Sector>();

            int nextSecID
               = Sector.ENDOFCHAIN;

            HashSet<int> processedSectors = new HashSet<int>();

            if (header.DIFATSectorsNumber != 0)
            {
                validationCount = (int)header.DIFATSectorsNumber;

                Sector s = sectors[header.FirstDIFATSectorID] as Sector;

                if (s == null) //Lazy loading
                {
                    s = new Sector(GetSectorSize(), sourceStream);
                    s.Type = SectorType.DIFAT;
                    s.Id = header.FirstDIFATSectorID;
                    sectors[header.FirstDIFATSectorID] = s;
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
                    if (nextSecID == Sector.FREESECT || nextSecID == Sector.ENDOFCHAIN) break;

                    validationCount--;

                    if (validationCount < 0)
                    {
                        if (this.closeStream)
                            this.Close();

                        if (this.validationExceptionEnabled)
                            throw new CFCorruptedFileException("DIFAT sectors count mismatched. Corrupted compound file");
                    }

                    s = sectors[nextSecID] as Sector;

                    if (s == null)
                    {
                        s = new Sector(GetSectorSize(), sourceStream);
                        s.Id = nextSecID;
                        sectors[nextSecID] = s;
                    }

                    result.Add(s);
                }
            }

            return result;
        }

        private void EnsureUniqueSectorIndex(int nextSecID, HashSet<int> processedSectors)
        {
            if (processedSectors.Contains(nextSecID) && this.validationExceptionEnabled)
            {
                throw new CFCorruptedFileException("The file is corrupted.");
            }

            processedSectors.Add(nextSecID);
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
                Sector s = sectors[nextSecID] as Sector;

                if (s == null)
                {
                    s = new Sector(GetSectorSize(), sourceStream);
                    s.Id = nextSecID;
                    s.Type = SectorType.FAT;
                    sectors[nextSecID] = s;
                }

                result.Add(s);

                idx++;
            }

            //Is there any DIFAT sector containing other FAT entries ?
            if (difatSectors.Count > 0)
            {
                HashSet<int> processedSectors = new HashSet<int>();
                StreamView difatStream
                    = new StreamView
                        (
                        difatSectors,
                        GetSectorSize(),
                        header.FATSectorsNumber > N_HEADER_FAT_ENTRY ?
                            (header.FATSectorsNumber - N_HEADER_FAT_ENTRY) * 4 :
                            0,
                        null,
                            sourceStream
                        );

                byte[] nextDIFATSectorBuffer = new byte[4];

               

                int i = 0;
                
                while (result.Count < header.FATSectorsNumber)
                {
                    difatStream.Read(nextDIFATSectorBuffer, 0, 4);
                    nextSecID = BitConverter.ToInt32(nextDIFATSectorBuffer, 0);
                    
                    EnsureUniqueSectorIndex(nextSecID, processedSectors);

                    Sector s = sectors[nextSecID] as Sector;

                    if (s == null)
                    {
                        s = new Sector(GetSectorSize(), sourceStream);
                        s.Type = SectorType.FAT;
                        s.Id = nextSecID;
                        sectors[nextSecID] = s;//UUU
                    }

                    result.Add(s);

                    //difatStream.Read(nextDIFATSectorBuffer, 0, 4);
                    //nextSecID = BitConverter.ToInt32(nextDIFATSectorBuffer, 0);
                    

                    if (difatStream.Position == ((GetSectorSize() - 4) + i * GetSectorSize()))
                    {
                        // Skip DIFAT chain fields considering the possibility that the last FAT entry has been already read
                        difatStream.Read(nextDIFATSectorBuffer, 0, 4);
                        if (BitConverter.ToInt32(nextDIFATSectorBuffer, 0) == Sector.ENDOFCHAIN)
                            break;
                        else
                        {
                            i++;
                            continue;
                        }
                    }
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
            HashSet<int> processedSectors = new HashSet<int>();

            StreamView fatStream
                = new StreamView(fatSectors, GetSectorSize(), fatSectors.Count * GetSectorSize(), null, sourceStream);

            while (true)
            {
                if (nextSecID == Sector.ENDOFCHAIN) break;

                if (nextSecID < 0)
                    throw new CFCorruptedFileException(String.Format("Next Sector ID reference is below zero. NextID : {0}", nextSecID));

                if (nextSecID >= sectors.Count)
                    throw new CFCorruptedFileException(String.Format("Next Sector ID reference an out of range sector. NextID : {0} while sector count {1}", nextSecID, sectors.Count));

                Sector s = sectors[nextSecID] as Sector;
                if (s == null)
                {
                    s = new Sector(GetSectorSize(), sourceStream);
                    s.Id = nextSecID;
                    s.Type = SectorType.Normal;
                    sectors[nextSecID] = s;
                }

                result.Add(s);

                fatStream.Seek(nextSecID * 4, SeekOrigin.Begin);
                int next = fatStream.ReadInt32();

                EnsureUniqueSectorIndex(next, processedSectors);
                nextSecID = next;

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

            if (secID != Sector.ENDOFCHAIN)
            {
                int nextSecID = secID;

                List<Sector> miniFAT = GetNormalSectorChain(header.FirstMiniFATSectorID);
                List<Sector> miniStream = GetNormalSectorChain(RootEntry.StartSetc);

                StreamView miniFATView
                    = new StreamView(miniFAT, GetSectorSize(), header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE, null, sourceStream);

                StreamView miniStreamView =
                    new StreamView(miniStream, GetSectorSize(), rootStorage.Size, null, sourceStream);

                BinaryReader miniFATReader = new BinaryReader(miniFATView);

                nextSecID = secID;

                HashSet<int> processedSectors = new HashSet<int>();

                while (true)
                {
                    if (nextSecID == Sector.ENDOFCHAIN)
                        break;

                    Sector ms = new Sector(Sector.MINISECTOR_SIZE, sourceStream);
                    byte[] temp = new byte[Sector.MINISECTOR_SIZE];

                    ms.Id = nextSecID;
                    ms.Type = SectorType.Mini;

                    miniStreamView.Seek(nextSecID * Sector.MINISECTOR_SIZE, SeekOrigin.Begin);
                    miniStreamView.Read(ms.GetData(), 0, Sector.MINISECTOR_SIZE);

                    result.Add(ms);

                    miniFATView.Seek(nextSecID * 4, SeekOrigin.Begin);
                    int next = miniFATReader.ReadInt32();

                    nextSecID = next;
                    EnsureUniqueSectorIndex(nextSecID, processedSectors);
                }
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

        private CFStorage rootStorage;

        /// <summary>
        /// The entry point object that represents the 
        /// root of the structures tree to get or set storage or
        /// stream data.
        /// </summary>
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
        public CFStorage RootStorage
        {
            get
            {
                return rootStorage as CFStorage;
            }
        }

        public CFSVersion Version
        {
            get
            {
                return (CFSVersion)this.header.MajorVersion;
            }
        }



        /// <summary>
        /// Reset a directory entry setting it to StgInvalid in the Directory.
        /// </summary>
        /// <param name="sid">Sid of the directory to invalidate</param>
        internal void ResetDirectoryEntry(int sid)
        {
            directoryEntries[sid].SetEntryName(String.Empty);
            directoryEntries[sid].Left = null;
            directoryEntries[sid].Right = null;
            directoryEntries[sid].Parent = null;
            directoryEntries[sid].StgType = StgType.StgInvalid;
        }



        //internal class NodeFactory : IRBTreeDeserializer<CFItem>
        //{

        //    public RBNode<CFItem> DeserizlizeFromValues()
        //    {
        //           RBNode<CFItem> node = new RBNode<CFItem>(value,(Color)value.DirEntry.StgColor,
        //    }
        //}

        internal RBTree CreateNewTree()
        {
            RBTree bst = new RBTree();
            //bst.NodeInserted += OnNodeInsert;
            //bst.NodeOperation += OnNodeOperation;
            //bst.NodeDeleted += new Action<RBNode<CFItem>>(OnNodeDeleted);
            //  bst.ValueAssignedAction += new Action<RBNode<CFItem>, CFItem>(OnValueAssigned);
            return bst;
        }

        //void OnValueAssigned(RBNode<CFItem> node, CFItem from)
        //{
        //    if (from.DirEntry != null && from.DirEntry.LeftSibling != DirectoryEntry.NOSTREAM)

        //    if (from.DirEntry != null && from.DirEntry.LeftSibling != DirectoryEntry.NOSTREAM)
        //        node.Value.DirEntry.LeftSibling = from.DirEntry.LeftSibling;

        //    if (from.DirEntry != null && from.DirEntry.RightSibling != DirectoryEntry.NOSTREAM)
        //        node.Value.DirEntry.RightSibling = from.DirEntry.RightSibling;
        //}


        internal RBTree GetChildrenTree(int sid)
        {
            RBTree bst = new RBTree();


            // Load children from their original tree.
            DoLoadChildren(bst, directoryEntries[sid]);
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
                bst = new RBTree(directoryEntries[de.Child]);
            }

            return bst;
        }


        private void DoLoadChildren(RBTree bst, IDirectoryEntry de)
        {

            if (de.Child != DirectoryEntry.NOSTREAM)
            {
                if (directoryEntries[de.Child].StgType == StgType.StgInvalid) return;

                LoadSiblings(bst, directoryEntries[de.Child]);
                NullifyChildNodes(directoryEntries[de.Child]);
                bst.Insert(directoryEntries[de.Child]);
            }
        }

        private void NullifyChildNodes(IDirectoryEntry de)
        {
            de.Parent = null;
            de.Left = null;
            de.Right = null;
        }

        private List<int> levelSIDs = new List<int>();

        // Doubling methods allows iterative behavior while avoiding
        // to insert duplicate items
        private void LoadSiblings(RBTree bst, IDirectoryEntry de)
        {
            levelSIDs.Clear();

            if (de.LeftSibling != DirectoryEntry.NOSTREAM)
            {


                // If there're more left siblings load them...
                DoLoadSiblings(bst, directoryEntries[de.LeftSibling]);
                //NullifyChildNodes(directoryEntries[de.LeftSibling]);
            }

            if (de.RightSibling != DirectoryEntry.NOSTREAM)
            {
                levelSIDs.Add(de.RightSibling);

                // If there're more right siblings load them...
                DoLoadSiblings(bst, directoryEntries[de.RightSibling]);
                //NullifyChildNodes(directoryEntries[de.RightSibling]);
            }
        }

        private void DoLoadSiblings(RBTree bst, IDirectoryEntry de)
        {
            if (ValidateSibling(de.LeftSibling))
            {
                levelSIDs.Add(de.LeftSibling);

                // If there're more left siblings load them...
                DoLoadSiblings(bst, directoryEntries[de.LeftSibling]);
            }

            if (ValidateSibling(de.RightSibling))
            {
                levelSIDs.Add(de.RightSibling);

                // If there're more right siblings load them...
                DoLoadSiblings(bst, directoryEntries[de.RightSibling]);
            }

            NullifyChildNodes(de);
            bst.Insert(de);
        }

        private bool ValidateSibling(int sid)
        {
            if (sid != DirectoryEntry.NOSTREAM)
            {
                // if this siblings id does not overflow current list
                if (sid >= directoryEntries.Count)
                {
                    if (this.validationExceptionEnabled)
                    {
                        //this.Close();
                        throw new CFCorruptedFileException("A Directory Entry references the non-existent sid number " + sid.ToString());
                    }
                    else
                        return false;
                }

                //if this sibling is valid...
                if (directoryEntries[sid].StgType == StgType.StgInvalid)
                {
                    if (this.validationExceptionEnabled)
                    {
                        //this.Close();
                        throw new CFCorruptedFileException("A Directory Entry has a valid reference to an Invalid Storage Type directory [" + sid + "]");
                    }
                    else
                        return false;
                }

                if (!Enum.IsDefined(typeof(StgType), directoryEntries[sid].StgType))
                {

                    if (this.validationExceptionEnabled)
                    {
                        //this.Close();
                        throw new CFCorruptedFileException("A Directory Entry has an invalid Storage Type");
                    }
                    else
                        return false;
                }

                if (levelSIDs.Contains(sid))
                    throw new CFCorruptedFileException("Cyclic reference of directory item");

                return true; //No fault condition encountered for sid being validated
            }

            return false;
        }


        /// <summary>
        /// Load directory entries from compound file. Header and FAT MUST be already loaded.
        /// </summary>
        private void LoadDirectories()
        {
            List<Sector> directoryChain
                = GetSectorChain(header.FirstDirectorySectorID, SectorType.Normal);

            if (!(directoryChain.Count > 0))
                throw new CFCorruptedFileException("Directory sector chain MUST contain at least 1 sector");

            if (header.FirstDirectorySectorID == Sector.ENDOFCHAIN)
                header.FirstDirectorySectorID = directoryChain[0].Id;

            StreamView dirReader
                = new StreamView(directoryChain, GetSectorSize(), directoryChain.Count * GetSectorSize(), null, sourceStream);


            while (dirReader.Position < directoryChain.Count * GetSectorSize())
            {
                IDirectoryEntry de
                = DirectoryEntry.New(String.Empty, StgType.StgInvalid, directoryEntries);

                //We are not inserting dirs. Do not use 'InsertNewDirectoryEntry'
                de.Read(dirReader, this.Version);

            }
        }



        /// <summary>
        ///  Commit directory entries change on the Current Source stream
        /// </summary>
        private void CommitDirectory()
        {
            const int DIRECTORY_SIZE = 128;

            List<Sector> directorySectors
                = GetSectorChain(header.FirstDirectorySectorID, SectorType.Normal);

            StreamView sv = new StreamView(directorySectors, GetSectorSize(), 0, null, sourceStream);

            foreach (IDirectoryEntry di in directoryEntries)
            {
                di.Write(sv);
            }

            int delta = directoryEntries.Count;

            while (delta % (GetSectorSize() / DIRECTORY_SIZE) != 0)
            {
                IDirectoryEntry dummy = DirectoryEntry.New(String.Empty, StgType.StgInvalid, directoryEntries);
                dummy.Write(sv);
                delta++;
            }

            foreach (Sector s in directorySectors)
            {
                s.Type = SectorType.Directory;
            }

            AllocateSectorChain(directorySectors);

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
        }


        /// <summary>
        /// Saves the in-memory image of Compound File to a file.
        /// </summary>
        /// <param name="fileName">File name to write the compound file to</param>
        /// <exception cref="T:OpenMcdf.CFException">Raised if destination file is not seekable</exception>

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
        /// </summary>        
        /// <remarks>
        /// Destination Stream must be seekable. Uncommitted data will be persisted to the destination stream.
        /// </remarks>
        /// <param name="stream">The stream to save compound File to</param>
        /// <exception cref="T:OpenMcdf.CFException">Raised if destination stream is not seekable</exception>
        /// <exception cref="T:OpenMcdf.CFDisposedException">Raised if Compound File Storage has been already disposed</exception>
        /// <example>
        /// <code>
        ///    MemoryStream ms = new MemoryStream(size);
        ///
        ///    CompoundFile cf = new CompoundFile();
        ///    CFStorage st = cf.RootStorage.AddStorage("MyStorage");
        ///    CFStream sm = st.AddStream("MyStream");
        ///
        ///    byte[] b = new byte[]{0x00,0x01,0x02,0x03};
        ///
        ///    sm.SetData(b);
        ///    cf.Save(ms);
        ///    cf.Close();
        /// </code>
        /// </example>
        public void Save(Stream stream)
        {
            if (_disposed)
                throw new CFDisposedException("Compound File closed: cannot save data");

            if (!stream.CanSeek)
                throw new CFException("Cannot save on a non-seekable stream");

            CheckForLockSector();
            int sSize = GetSectorSize();

            try
            {
                stream.Write((byte[])Array.CreateInstance(typeof(byte), sSize), 0, sSize);

                CommitDirectory();

                for (int i = 0; i < sectors.Count; i++)
                {
                    Sector s = sectors[i] as Sector;

                    if (s == null)
                    {
                        // Load source (unmodified) sectors
                        // Here we have to ignore "Dirty flag" of 
                        // sectors because we are NOT modifying the source
                        // in a differential way but ALL sectors need to be 
                        // persisted on the destination stream
                        s = new Sector(sSize, sourceStream);
                        s.Id = i;

                        //sectors[i] = s;
                    }


                    stream.Write(s.GetData(), 0, sSize);

                    //s.ReleaseData();

                }

                stream.Seek(0, SeekOrigin.Begin);
                header.Write(stream);
            }
            catch (Exception ex)
            {
                throw new CFException("Internal error while saving compound file to stream ", ex);
            }
        }


        /// <summary>
        /// Scan FAT o miniFAT for free sectors to reuse.
        /// </summary>
        /// <param name="sType">Type of sector to look for</param>
        /// <returns>A Queue of available sectors or minisectors already allocated</returns>
        internal Queue<Sector> FindFreeSectors(SectorType sType)
        {
            Queue<Sector> freeList = new Queue<Sector>();

            if (sType == SectorType.Normal)
            {

                List<Sector> FatChain = GetSectorChain(-1, SectorType.FAT);
                StreamView fatStream = new StreamView(FatChain, GetSectorSize(), header.FATSectorsNumber * GetSectorSize(), null, sourceStream);

                int idx = 0;

                while (idx < sectors.Count)
                {
                    int id = fatStream.ReadInt32();

                    if (id == Sector.FREESECT)
                    {
                        if (sectors[idx] == null)
                        {
                            Sector s = new Sector(GetSectorSize(), sourceStream);
                            s.Id = idx;
                            sectors[idx] = s;

                        }

                        freeList.Enqueue(sectors[idx] as Sector);
                    }

                    idx++;
                }
            }
            else
            {
                List<Sector> miniFAT
                    = GetSectorChain(header.FirstMiniFATSectorID, SectorType.Normal);

                StreamView miniFATView
                    = new StreamView(miniFAT, GetSectorSize(), header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE, null, sourceStream);

                List<Sector> miniStream
                    = GetSectorChain(RootEntry.StartSetc, SectorType.Normal);

                StreamView miniStreamView
                    = new StreamView(miniStream, GetSectorSize(), rootStorage.Size, null, sourceStream);

                int idx = 0;

                int nMinisectors = (int)(miniStreamView.Length / Sector.MINISECTOR_SIZE);

                while (idx < nMinisectors)
                {
                    //AssureLength(miniStreamView, (int)miniFATView.Length);

                    int nextId = miniFATView.ReadInt32();

                    if (nextId == Sector.FREESECT)
                    {
                        Sector ms = new Sector(Sector.MINISECTOR_SIZE, sourceStream);
                        byte[] temp = new byte[Sector.MINISECTOR_SIZE];

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

        /// <summary>
        /// INTERNAL DEVELOPMENT. DO NOT CALL.
        /// <param name="directoryEntry"></param>
        /// <param name="buffer"></param>
        internal void AppendData(CFItem cfItem, Byte[] buffer)
        {
            WriteData(cfItem, cfItem.Size, buffer);
        }

        /// <summary>
        /// Resize stream length
        /// </summary>
        /// <param name="cfItem"></param>
        /// <param name="length"></param>
        internal void SetStreamLength(CFItem cfItem, long length)
        {
            if (cfItem.Size == length)
                return;

            SectorType newSectorType = SectorType.Normal;
            int newSectorSize = GetSectorSize();

            if (length < header.MinSizeStandardStream)
            {
                newSectorType = SectorType.Mini;
                newSectorSize = Sector.MINISECTOR_SIZE;
            }

            SectorType oldSectorType = SectorType.Normal;
            int oldSectorSize = GetSectorSize();

            if (cfItem.Size < header.MinSizeStandardStream)
            {
                oldSectorType = SectorType.Mini;
                oldSectorSize = Sector.MINISECTOR_SIZE;
            }

            long oldSize = cfItem.Size;


            // Get Sector chain and delta size induced by client
            List<Sector> sectorChain = GetSectorChain(cfItem.DirEntry.StartSetc, oldSectorType);
            long delta = length - cfItem.Size;

            // Check for transition ministream -> stream:
            // Only in this case we need to free old sectors,
            // otherwise they will be overwritten.

            bool transitionToMini = false;
            bool transitionToNormal = false;
            List<Sector> oldChain = null;

            if (cfItem.DirEntry.StartSetc != Sector.ENDOFCHAIN)
            {
                if (
                    (length < header.MinSizeStandardStream && cfItem.DirEntry.Size >= header.MinSizeStandardStream)
                    || (length >= header.MinSizeStandardStream && cfItem.DirEntry.Size < header.MinSizeStandardStream)
                   )
                {
                    if (cfItem.DirEntry.Size < header.MinSizeStandardStream)
                    {
                        transitionToNormal = true;
                        oldChain = sectorChain;
                    }
                    else
                    {
                        transitionToMini = true;
                        oldChain = sectorChain;
                    }

                    // No transition caused by size change

                }
            }


            Queue<Sector> freeList = null;
            StreamView sv = null;

            if (!transitionToMini && !transitionToNormal)   //############  NO TRANSITION
            {
                if (delta > 0) // Enlarging stream...
                {
                    if (this.sectorRecycle)
                        freeList = FindFreeSectors(newSectorType); // Collect available free sectors

                    sv = new StreamView(sectorChain, newSectorSize, length, freeList, sourceStream);

                    //Set up  destination chain
                    SetSectorChain(sectorChain);
                }
                else if (delta < 0)  // Reducing size...
                {

                    int nSec = (int)Math.Floor(((double)(Math.Abs(delta)) / newSectorSize)); //number of sectors to mark as free

                    if (newSectorSize == Sector.MINISECTOR_SIZE)
                        FreeMiniChain(sectorChain, nSec, this.eraseFreeSectors);
                    else
                        FreeChain(sectorChain, nSec, this.eraseFreeSectors);
                }

                if (sectorChain.Count > 0)
                {
                    cfItem.DirEntry.StartSetc = sectorChain[0].Id;
                    cfItem.DirEntry.Size = length;
                }
                else
                {
                    cfItem.DirEntry.StartSetc = Sector.ENDOFCHAIN;
                    cfItem.DirEntry.Size = 0;
                }

            }
            else if (transitionToMini)                          //############## TRANSITION TO MINISTREAM
            {
                // Transition Normal chain -> Mini chain

                // Collect available MINI free sectors

                if (this.sectorRecycle)
                    freeList = FindFreeSectors(SectorType.Mini);

                sv = new StreamView(oldChain, oldSectorSize, oldSize, null, sourceStream);

                // Reset start sector and size of dir entry
                cfItem.DirEntry.StartSetc = Sector.ENDOFCHAIN;
                cfItem.DirEntry.Size = 0;

                List<Sector> newChain = GetMiniSectorChain(Sector.ENDOFCHAIN);
                StreamView destSv = new StreamView(newChain, Sector.MINISECTOR_SIZE, length, freeList, sourceStream);

                // Buffered trimmed copy from old (larger) to new (smaller)
                int cnt = 4096 < length ? 4096 : (int)length;

                byte[] buf = new byte[4096];
                long toRead = length;

                //Copy old to new chain
                while (toRead > cnt)
                {
                    cnt = sv.Read(buf, 0, cnt);
                    toRead -= cnt;
                    destSv.Write(buf, 0, cnt);
                }

                sv.Read(buf, 0, (int)toRead);
                destSv.Write(buf, 0, (int)toRead);

                //Free old chain
                FreeChain(oldChain, this.eraseFreeSectors);

                //Set up destination chain
                AllocateMiniSectorChain(destSv.BaseSectorChain);

                // Persist to normal strea
                PersistMiniStreamToStream(destSv.BaseSectorChain);

                //Update dir item
                if (destSv.BaseSectorChain.Count > 0)
                {
                    cfItem.DirEntry.StartSetc = destSv.BaseSectorChain[0].Id;
                    cfItem.DirEntry.Size = length;
                }
                else
                {
                    cfItem.DirEntry.StartSetc = Sector.ENDOFCHAIN;
                    cfItem.DirEntry.Size = 0;
                }
            }
            else if (transitionToNormal)                        //############## TRANSITION TO NORMAL STREAM
            {
                // Transition Mini chain -> Normal chain

                if (this.sectorRecycle)
                    freeList = FindFreeSectors(SectorType.Normal); // Collect available Normal free sectors

                sv = new StreamView(oldChain, oldSectorSize, oldSize, null, sourceStream);

                List<Sector> newChain = GetNormalSectorChain(Sector.ENDOFCHAIN);
                StreamView destSv = new StreamView(newChain, GetSectorSize(), length, freeList, sourceStream);

                int cnt = 256 < length ? 256 : (int)length;

                byte[] buf = new byte[256];
                long toRead = Math.Min(length, cfItem.Size);

                //Copy old to new chain
                while (toRead > cnt)
                {
                    cnt = sv.Read(buf, 0, cnt);
                    toRead -= cnt;
                    destSv.Write(buf, 0, cnt);
                }

                sv.Read(buf, 0, (int)toRead);
                destSv.Write(buf, 0, (int)toRead);

                //Free old mini chain
                int oldChainCount = oldChain.Count;
                FreeMiniChain(oldChain, this.eraseFreeSectors);

                //Set up normal destination chain
                AllocateSectorChain(destSv.BaseSectorChain);

                //Update dir item
                if (destSv.BaseSectorChain.Count > 0)
                {
                    cfItem.DirEntry.StartSetc = destSv.BaseSectorChain[0].Id;
                    cfItem.DirEntry.Size = length;
                }
                else
                {
                    cfItem.DirEntry.StartSetc = Sector.ENDOFCHAIN;
                    cfItem.DirEntry.Size = 0;
                }
            }
        }

        internal void WriteData(CFItem cfItem, long position, byte[] buffer)
        {
            WriteData(cfItem, buffer, position, 0, buffer.Length);
        }

        internal void WriteData(CFItem cfItem, byte[] buffer, long position, int offset, int count)
        {

            if (buffer == null)
                throw new CFInvalidOperation("Parameter [buffer] cannot be null");

            if (cfItem.DirEntry == null)
                throw new CFException("Internal error [cfItem.DirEntry] cannot be null");

            if (buffer.Length == 0) return;

            // Get delta size induced by client
            long delta = (position + count) - cfItem.Size < 0 ? 0 : (position + count) - cfItem.Size;
            long newLength = cfItem.Size + delta;

            SetStreamLength(cfItem, newLength);

            // Calculate NEW sectors SIZE
            SectorType _st = SectorType.Normal;
            int _sectorSize = GetSectorSize();

            if (cfItem.Size < header.MinSizeStandardStream)
            {
                _st = SectorType.Mini;
                _sectorSize = Sector.MINISECTOR_SIZE;
            }

            List<Sector> sectorChain = GetSectorChain(cfItem.DirEntry.StartSetc, _st);
            StreamView sv = new StreamView(sectorChain, _sectorSize, newLength, null, sourceStream);

            sv.Seek(position, SeekOrigin.Begin);
            sv.Write(buffer, offset, count);

            if (cfItem.Size < header.MinSizeStandardStream)
            {
                PersistMiniStreamToStream(sv.BaseSectorChain);
                //SetSectorChain(sv.BaseSectorChain);
            }
        }

        internal void WriteData(CFItem cfItem, Byte[] buffer)
        {
            WriteData(cfItem, 0, buffer);
        }

        /// <summary>
        /// Check file size limit ( 2GB for version 3 )
        /// </summary>
        private void CheckFileLength()
        {
            throw new NotImplementedException();
        }


        internal int ReadData(CFStream cFStream, long position, byte[] buffer, int count)
        {
            if (count > buffer.Length)
                throw new ArgumentException("count parameter exceeds buffer size");

            IDirectoryEntry de = cFStream.DirEntry;

            count = (int)Math.Min((long)(de.Size - position), (long)count);

            StreamView sView = null;


            if (de.Size < header.MinSizeStandardStream)
            {
                sView
                    = new StreamView(GetSectorChain(de.StartSetc, SectorType.Mini), Sector.MINISECTOR_SIZE, de.Size, null, sourceStream);
            }
            else
            {

                sView = new StreamView(GetSectorChain(de.StartSetc, SectorType.Normal), GetSectorSize(), de.Size, null, sourceStream);
            }


            sView.Seek(position, SeekOrigin.Begin);
            int result = sView.Read(buffer, 0, count);

            return result;
        }

        internal int ReadData(CFStream cFStream, long position, byte[] buffer, int offset, int count)
        {

            IDirectoryEntry de = cFStream.DirEntry;

            count = (int)Math.Min((long)(de.Size - offset), (long)count);

            StreamView sView = null;


            if (de.Size < header.MinSizeStandardStream)
            {
                sView
                    = new StreamView(GetSectorChain(de.StartSetc, SectorType.Mini), Sector.MINISECTOR_SIZE, de.Size, null, sourceStream);
            }
            else
            {

                sView = new StreamView(GetSectorChain(de.StartSetc, SectorType.Normal), GetSectorSize(), de.Size, null, sourceStream);
            }


            sView.Seek(position, SeekOrigin.Begin);
            int result = sView.Read(buffer, offset, count);

            return result;
        }


        internal byte[] GetData(CFStream cFStream)
        {

            if (_disposed)
                throw new CFDisposedException("Compound File closed: cannot access data");

            byte[] result = null;

            IDirectoryEntry de = cFStream.DirEntry;

            //IDirectoryEntry root = directoryEntries[0];

            if (de.Size < header.MinSizeStandardStream)
            {

                StreamView miniView
                    = new StreamView(GetSectorChain(de.StartSetc, SectorType.Mini), Sector.MINISECTOR_SIZE, de.Size, null, sourceStream);

                BinaryReader br = new BinaryReader(miniView);

                result = br.ReadBytes((int)de.Size);
                br.Close();

            }
            else
            {
                StreamView sView
                    = new StreamView(GetSectorChain(de.StartSetc, SectorType.Normal), GetSectorSize(), de.Size, null, sourceStream);

                result = new byte[(int)de.Size];

                sView.Read(result, 0, result.Length);

            }

            return result;
        }
        public byte[] GetDataBySID(int sid)
        {
            if (_disposed)
                throw new CFDisposedException("Compound File closed: cannot access data");
            if (sid < 0)
                return null;
            byte[] result = null;
            try
            {
                IDirectoryEntry de = directoryEntries[sid];
                if (de.Size < header.MinSizeStandardStream)
                {
                    StreamView miniView
                        = new StreamView(GetSectorChain(de.StartSetc, SectorType.Mini), Sector.MINISECTOR_SIZE, de.Size, null, sourceStream);
                    BinaryReader br = new BinaryReader(miniView);
                    result = br.ReadBytes((int)de.Size);
                    br.Close();
                }
                else
                {
                    StreamView sView
                        = new StreamView(GetSectorChain(de.StartSetc, SectorType.Normal), GetSectorSize(), de.Size, null, sourceStream);
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
        public Guid getGuidBySID(int sid)
        {
            if (_disposed)
                throw new CFDisposedException("Compound File closed: cannot access data");
            if (sid < 0)
                throw new CFException("Invalid SID");
            IDirectoryEntry de = directoryEntries[sid];
            return de.StorageCLSID;
        }
        public Guid getGuidForStream(int sid)
        {
            if (_disposed)
                throw new CFDisposedException("Compound File closed: cannot access data");
            if (sid < 0)
                throw new CFException("Invalid SID");
            Guid g = new Guid("00000000000000000000000000000000");
            //find first storage containing a non-zero CLSID before SID in directory structure
            for (int i = sid - 1; i >= 0; i--)
            {
                if (directoryEntries[i].StorageCLSID != g && directoryEntries[i].StgType == StgType.StgStorage)
                {
                    return directoryEntries[i].StorageCLSID;
                }
            }
            return g;
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
            if (sid >= directoryEntries.Count)
                throw new CFException("Invalid SID of the directory entry to remove");

            //Random r = new Random();
            directoryEntries[sid].SetEntryName("_DELETED_NAME_" + sid.ToString());
            directoryEntries[sid].StgType = StgType.StgInvalid;
        }

        internal void FreeAssociatedData(int sid)
        {
            // Clear the associated stream (or ministream) if required
            if (directoryEntries[sid].Size > 0) //thanks to Mark Bosold for this !
            {
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
        }

        /// <summary>
        /// Close the Compound File object <see cref="T:OpenMcdf.CompoundFile">CompoundFile</see> and
        /// free all associated resources (e.g. open file handle and allocated memory).
        /// <remarks>
        /// When the <see cref="T:OpenMcdf.CompoundFile.Close()">Close</see> method is called,
        /// all the associated stream and storage objects are invalidated:
        /// any operation invoked on them will produce a <see cref="T:OpenMcdf.CFDisposedException">CFDisposedException</see>.
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
            this.Close(true);
        }

        private bool closeStream = true;

        [Obsolete("Use flag LeaveOpen in CompoundFile constructor")]
        public void Close(bool closeStream)
        {
            this.closeStream = closeStream;
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
                            //this.lockObject = null;
#if !FLAT_WRITE
                            this.buffer = null;
#endif
                        }

                        if (this.sourceStream != null && closeStream && !configuration.HasFlag(CFSConfiguration.LeaveOpen))
                            this.sourceStream.Close();
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

        internal IList<IDirectoryEntry> GetDirectories()
        {
            return directoryEntries;
        }

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

        private IList<IDirectoryEntry> FindDirectoryEntries(String entryName)
        {
            List<IDirectoryEntry> result = new List<IDirectoryEntry>();

            foreach (IDirectoryEntry d in directoryEntries)
            {
                if (d.GetEntryName() == entryName && d.StgType != StgType.StgInvalid)
                    result.Add(d);
            }

            return result;
        }



        /// <summary>
        /// Get a list of all entries with a given name contained in the document.
        /// </summary>
        /// <param name="entryName">Name of entries to retrive</param>
        /// <returns>A list of name-matching entries</returns>
        /// <remarks>This function is aimed to speed up entity lookup in 
        /// flat-structure files (only one or little more known entries)
        /// without the performance penalty related to entities hierarchy constraints.
        /// There is no implied hierarchy in the returned list.
        /// </remarks>
        public IList<CFItem> GetAllNamedEntries(String entryName)
        {
            IList<IDirectoryEntry> r = FindDirectoryEntries(entryName);
            List<CFItem> result = new List<CFItem>();

            foreach (IDirectoryEntry id in r)
            {
                if (id.GetEntryName() == entryName && id.StgType != StgType.StgInvalid)
                {
                    CFItem i = id.StgType == StgType.StgStorage ? (CFItem)new CFStorage(this, id) : (CFItem)new CFStream(this, id);
                    result.Add(i);
                }
            }

            return result;
        }

        public int GetNumDirectories()
        {
            if (_disposed)
                throw new CFDisposedException("Compound File closed: cannot access data");
            return directoryEntries.Count;
        }

        public string GetNameDirEntry(int id)
        {
            if (_disposed)
                throw new CFDisposedException("Compound File closed: cannot access data");
            if (id < 0)
                throw new CFException("Invalid Storage ID");
            return directoryEntries[id].Name;
        }

        public StgType GetStorageType(int id)
        {
            if (_disposed)
                throw new CFDisposedException("Compound File closed: cannot access data");
            if (id < 0)
                throw new CFException("Invalid Storage ID");
            return directoryEntries[id].StgType;
        }


        /// <summary>
        /// Compress free space by removing unallocated sectors from compound file
        /// effectively reducing stream or file size.
        /// </summary>
        /// <remarks>
        /// Current implementation supports compression only for ver. 3 compound files.
        /// </remarks>
        /// <example>
        /// <code>
        /// 
        ///  //This code has been extracted from unit test
        ///  
        ///    String FILENAME = "MultipleStorage3.cfs";
        ///
        ///    FileInfo srcFile = new FileInfo(FILENAME);
        ///
        ///    File.Copy(FILENAME, "MultipleStorage_Deleted_Compress.cfs", true);
        ///
        ///    CompoundFile cf = new CompoundFile("MultipleStorage_Deleted_Compress.cfs", UpdateMode.Update, true, true);
        ///
        ///    CFStorage st = cf.RootStorage.GetStorage("MyStorage");
        ///    st = st.GetStorage("AnotherStorage");
        ///    
        ///    Assert.IsNotNull(st);
        ///    st.Delete("Another2Stream"); //17Kb
        ///    cf.Commit();
        ///    cf.Close();
        ///
        ///    CompoundFile.ShrinkCompoundFile("MultipleStorage_Deleted_Compress.cfs");
        ///
        ///    FileInfo dstFile = new FileInfo("MultipleStorage_Deleted_Compress.cfs");
        ///
        ///    Assert.IsTrue(srcFile.Length > dstFile.Length);
        ///
        /// </code>
        /// </example>
        public static void ShrinkCompoundFile(Stream s)
        {
            CompoundFile cf = new CompoundFile(s);

            if (cf.header.MajorVersion != (ushort)CFSVersion.Ver_3)
                throw new CFException("Current implementation of free space compression does not support version 4 of Compound File Format");

            using (CompoundFile tempCF = new CompoundFile((CFSVersion)cf.header.MajorVersion, cf.Configuration))
            {
                //Copy Root CLSID
                tempCF.RootStorage.CLSID = new Guid(cf.RootStorage.CLSID.ToByteArray());

                DoCompression(cf.RootStorage, tempCF.RootStorage);

                MemoryStream tmpMS = new MemoryStream((int)cf.sourceStream.Length); //This could be a problem for v4

                tempCF.Save(tmpMS);
                tempCF.Close();

                // If we were based on a writable stream, we update
                // the stream and do reload from the compressed one...

                s.Seek(0, SeekOrigin.Begin);
                tmpMS.WriteTo(s);

                s.Seek(0, SeekOrigin.Begin);
                s.SetLength(tmpMS.Length);

                tmpMS.Close();
                cf.Close(false);
            }
        }

        /// <summary>
        /// Remove unallocated sectors from compound file in order to reduce its size.
        /// </summary>
        /// <remarks>
        /// Current implementation supports compression only for ver. 3 compound files.
        /// </remarks>
        /// <example>
        /// <code>
        /// 
        ///  //This code has been extracted from unit test
        ///  
        ///    String FILENAME = "MultipleStorage3.cfs";
        ///
        ///    FileInfo srcFile = new FileInfo(FILENAME);
        ///
        ///    File.Copy(FILENAME, "MultipleStorage_Deleted_Compress.cfs", true);
        ///
        ///    CompoundFile cf = new CompoundFile("MultipleStorage_Deleted_Compress.cfs", UpdateMode.Update, true, true);
        ///
        ///    CFStorage st = cf.RootStorage.GetStorage("MyStorage");
        ///    st = st.GetStorage("AnotherStorage");
        ///    
        ///    Assert.IsNotNull(st);
        ///    st.Delete("Another2Stream"); //17Kb
        ///    cf.Commit();
        ///    cf.Close();
        ///
        ///    CompoundFile.ShrinkCompoundFile("MultipleStorage_Deleted_Compress.cfs");
        ///
        ///    FileInfo dstFile = new FileInfo("MultipleStorage_Deleted_Compress.cfs");
        ///
        ///    Assert.IsTrue(srcFile.Length > dstFile.Length);
        ///
        /// </code>
        /// </example>
        public static void ShrinkCompoundFile(String fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);
            ShrinkCompoundFile(fs);
            fs.Close();
        }

        /// <summary>
        /// Recursively clones valid structures, avoiding to copy free sectors.
        /// </summary>
        /// <param name="currSrcStorage">Current source storage to clone</param>
        /// <param name="currDstStorage">Current cloned destination storage</param>
        private static void DoCompression(CFStorage currSrcStorage, CFStorage currDstStorage)
        {
            Action<CFItem> va =
                delegate (CFItem item)
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
                        strg.CLSID = new Guid(itemAsStorage.CLSID.ToByteArray());
                        DoCompression(itemAsStorage, strg); // recursion, one level deeper
                    }
                };

            currSrcStorage.VisitEntries(va, false);
        }
    }
}
