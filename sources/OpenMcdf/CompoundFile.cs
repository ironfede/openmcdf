﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 *
 * The Original Code is OpenMCDF - Compound Document Format library.
 *
 * The Initial Developer of the Original Code is Federico Blaseotto.*/

#define FLAT_WRITE // No optimization on the number of write operations

using RedBlackTree;
using System;
using System.Collections.Generic;
using System.IO;

namespace OpenMcdf
{
    internal sealed class CFItemComparer : IComparer<CFItem>
    {
        public int Compare(CFItem x, CFItem y)
        {
            // X CompareTo Y : X > Y --> 1 ; X < Y  --> -1
            return x.DirEntry.CompareTo(y.DirEntry);

            //Compare X < Y --> -1
        }
    }

    /// <summary>
    /// Configuration parameters for the compound files.
    /// They can be OR-combined to configure
    /// <see cref="T:OpenMcdf.CompoundFile">Compound file</see> behavior.
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
    /// efficient storage of multiple kinds of documents in a single file.
    /// Version 3 and 4 of specifications are supported.
    /// </summary>
    public class CompoundFile : IDisposable
    {
        /// <summary>
        /// Get the configuration parameters of the CompoundFile object.
        /// </summary>
        public CFSConfiguration Configuration { get; private set; }

        /// <summary>
        /// Returns the size of standard sectors switching on CFS version (3 or 4)
        /// </summary>
        /// <returns>Standard sector size</returns>
        internal int SectorSize => 2 << (header.SectorShift - 1);

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
        private bool sectorRecycle;

        /// <summary>
        /// Flag for unallocated sector zeroing out.
        /// </summary>
        private bool eraseFreeSectors;

        public bool ValidationExceptionEnabled { get; private set; } = true;

        private readonly CFSUpdateMode updateMode = CFSUpdateMode.ReadOnly;

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

        private SectorCollection sectors = new();

        /// <summary>
        /// CompoundFile header
        /// </summary>
        private Header header;

        /// <summary>
        /// Compound underlying stream. Null when new CF has been created.
        /// </summary>
        internal Stream sourceStream;

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
        public CompoundFile() : this(CFSVersion.Ver_3, CFSConfiguration.Default) { }

        void OnSizeLimitReached()
        {
            Sector rangeLockSector = new Sector(SectorSize, sourceStream);
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
            SetConfigurationOptions(configFlags);

            header = new Header((ushort)cfsVersion);

            if (cfsVersion == CFSVersion.Ver_4)
                sectors.OnVer3SizeLimitReached += new Ver3SizeLimitReached(OnSizeLimitReached);

            DIFAT_SECTOR_FAT_ENTRIES_COUNT = (SectorSize / 4) - 1;
            FAT_SECTOR_ENTRIES_COUNT = SectorSize / 4;

            //Root --
            IDirectoryEntry rootDir = DirectoryEntry.New("Root Entry", StgType.StgRoot, directoryEntries);
            rootDir.StgColor = StgColor.Black;
            //this.InsertNewDirectoryEntry(rootDir);

            RootStorage = new CFStorage(this, rootDir);
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
        public CompoundFile(string fileName) : this(fileName, CFSUpdateMode.ReadOnly, CFSConfiguration.Default) { }

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
        public CompoundFile(string fileName, CFSUpdateMode updateMode, CFSConfiguration configParameters)
        {
            SetConfigurationOptions(configParameters);
            this.updateMode = updateMode;

            LoadFile(fileName);

            DIFAT_SECTOR_FAT_ENTRIES_COUNT = (SectorSize / 4) - 1;
            FAT_SECTOR_ENTRIES_COUNT = SectorSize / 4;
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
            SetConfigurationOptions(configParameters);
            this.updateMode = updateMode;

            LoadStream(stream);

            DIFAT_SECTOR_FAT_ENTRIES_COUNT = (SectorSize / 4) - 1;
            FAT_SECTOR_ENTRIES_COUNT = SectorSize / 4;
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
        public CompoundFile(Stream stream) : this(stream, CFSUpdateMode.ReadOnly, CFSConfiguration.Default) { }

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
            if (IsClosed)
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
            int sSize = SectorSize;

            if (header.MajorVersion != (ushort)CFSVersion.Ver_3)
                CheckForLockSector();

            sourceStream.Seek(0, SeekOrigin.Begin);
            sourceStream.Write(new byte[sSize], 0, sSize);

            CommitDirectory();

            bool gap = true;

            for (int i = 0; i < sectors.Count; i++)
            {
#if FLAT_WRITE

                //Note:
                //Here sectors should not be loaded dynamically because
                //if they are null it means that no change has involved them;

                Sector s = sectors[i];

                if (s != null && s.DirtyFlag)
                {
                    if (gap)
                        sourceStream.Seek(sSize + i * (long)sSize, SeekOrigin.Begin);

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
        /// Set configuration parameters
        /// </summary>
        private void SetConfigurationOptions(CFSConfiguration configParameters)
        {
            Configuration = configParameters;
            ValidationExceptionEnabled = !configParameters.HasFlag(CFSConfiguration.NoValidationException);
            sectorRecycle = configParameters.HasFlag(CFSConfiguration.SectorRecycle);
            eraseFreeSectors = configParameters.HasFlag(CFSConfiguration.EraseFreeSectors);
            closeStream = !configParameters.HasFlag(CFSConfiguration.LeaveOpen);
        }

        /// <summary>
        /// Load compound file from an existing stream.
        /// </summary>
        /// <param name="stream">Stream to load compound file from</param>
        private void Load(Stream stream)
        {
            try
            {
                header = new Header();
                directoryEntries = new List<IDirectoryEntry>();

                sourceStream = stream;

                header.Read(stream);

                if (!Configuration.HasFlag(CFSConfiguration.NoValidationException))
                {
                    header.ThrowIfInvalid();
                }

                int n_sector = Ceiling((stream.Length - SectorSize) / (double)SectorSize);

                if (stream.Length > 0x7FFFFF0)
                    _transactionLockAllocated = true;

                sectors = new SectorCollection();
                //sectors = new ArrayList();
                for (int i = 0; i < n_sector; i++)
                {
                    sectors.Add(null);
                }

                LoadDirectories();

                RootStorage
                    = new CFStorage(this, directoryEntries[0]);
            }
            catch (Exception)
            {
                if (stream != null && closeStream)
                    stream.Close();

                throw;
            }
        }

        private void LoadFile(string fileName)
        {
            FileAccess access = updateMode == CFSUpdateMode.ReadOnly ? FileAccess.Read : FileAccess.ReadWrite;
            FileShare share = updateMode == CFSUpdateMode.ReadOnly ? FileShare.ReadWrite : FileShare.Read;
            FileStream fs = new(fileName, FileMode.Open, access, share);
            Load(fs);
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
        public bool HasSourceStream => sourceStream != null;

        private void PersistMiniStreamToStream(List<Sector> miniSectorChain)
        {
            List<Sector> miniStream
                = GetSectorChain(RootEntry.StartSect, SectorType.Normal);

            using StreamView miniStreamView
                = new StreamView(
                    miniStream,
                    SectorSize,
                    RootStorage.Size,
                    null,
                    sourceStream);

            for (int i = 0; i < miniSectorChain.Count; i++)
            {
                Sector s = miniSectorChain[i];

                if (s.Id == -1)
                    throw new CFException("Invalid minisector index");

                // MiniStream sectors already allocated
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
                = GetSectorChain(RootEntry.StartSect, SectorType.Normal);

            using StreamView miniFATView
                = new StreamView(
                    miniFAT,
                    SectorSize,
                    header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE,
                    null,
                    sourceStream,
                    true);

            using StreamView miniStreamView
                = new StreamView(
                    miniStream,
                    SectorSize,
                    RootStorage.Size,
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

                    miniStreamView.Seek(RootStorage.Size + Sector.MINISECTOR_SIZE, SeekOrigin.Begin);
                    //miniStreamView.Write(s.GetData(), 0, Sector.MINISECTOR_SIZE);
                    s.Id = (int)(miniStreamView.Position - Sector.MINISECTOR_SIZE) / Sector.MINISECTOR_SIZE;

                    RootStorage.DirEntry.Size = miniStreamView.Length;


                }
            }



            // Update miniFAT
            StreamRW miniFATStreamRW = new(miniFATView);
            for (int i = 0; i < sectorChain.Count - 1; i++)
            {
                int currentId = sectorChain[i].Id;
                int nextId = sectorChain[i + 1].Id;
                miniFATStreamRW.Seek(currentId * 4, SeekOrigin.Begin);
                miniFATStreamRW.Write(nextId);

            }

            // Write End of Chain in MiniFAT
            miniFATStreamRW.Seek(sectorChain[sectorChain.Count - 1].Id * SIZE_OF_SID, SeekOrigin.Begin);
            miniFATStreamRW.Write(Sector.ENDOFCHAIN);


            // Update sector chains
            AllocateSectorChain(miniStreamView.BaseSectorChain);
            AllocateSectorChain(miniFATView.BaseSectorChain);

            //Update HEADER and root storage when ministream changes
            if (miniFAT.Count > 0)
            {
                RootStorage.DirEntry.StartSect = miniStream[0].Id;
                header.MiniFATSectorsNumber = (uint)miniFAT.Count;
                header.FirstMiniFATSectorID = miniFAT[0].Id;
            }
        }

        internal void FreeData(CFStream stream)
        {
            if (stream.Size == 0)
                return;

            List<Sector> sectorChain;
            if (stream.Size < header.MinSizeStandardStream)
            {
                sectorChain = GetSectorChain(stream.DirEntry.StartSect, SectorType.Mini);
                FreeMiniChain(sectorChain);
            }
            else
            {
                sectorChain = GetSectorChain(stream.DirEntry.StartSect, SectorType.Normal);
                FreeChain(sectorChain);
            }

            stream.DirEntry.StartSect = Sector.ENDOFCHAIN;
            stream.DirEntry.Size = 0;
        }

        private void FreeChain(List<Sector> sectorChain)
        {
            FreeChain(sectorChain, 0);
        }

        private void FreeChain(List<Sector> sectorChain, int nth_sector_to_remove)
        {
            List<Sector> FAT
                = GetSectorChain(-1, SectorType.FAT);

            using StreamView FATView
                = new StreamView(FAT, SectorSize, FAT.Count * SectorSize, null, sourceStream);

            // Zeroes out sector data (if required)-------------
            if (eraseFreeSectors)
            {
                for (int i = nth_sector_to_remove; i < sectorChain.Count; i++)
                {
                    Sector s = sectorChain[i];
                    s.ZeroData();
                }
            }

            // Update FAT marking unallocated sectors ----------
            StreamRW streamRW = new StreamRW(FATView);
            for (int i = nth_sector_to_remove; i < sectorChain.Count; i++)
            {
                int position = sectorChain[i].Id * 4;
                streamRW.Seek(position, SeekOrigin.Begin);
                streamRW.Write(Sector.FREESECT);
            }

            // Write new end of chain if partial free ----------
            if (nth_sector_to_remove > 0 && sectorChain.Count > 0)
            {
                int position = sectorChain[nth_sector_to_remove - 1].Id * 4;
                streamRW.Seek(position, SeekOrigin.Begin);
                streamRW.Write(Sector.ENDOFCHAIN);
            }
        }

        private void FreeMiniChain(List<Sector> sectorChain)
        {
            FreeMiniChain(sectorChain, 0);
        }

        private void FreeMiniChain(List<Sector> sectorChain, int nth_sector_to_remove)
        {
            List<Sector> miniStream
                = GetSectorChain(RootEntry.StartSect, SectorType.Normal);

            using StreamView miniStreamView
                = new StreamView(miniStream, SectorSize, RootStorage.Size, null, sourceStream);

            // Set updated/new sectors within the ministream ----------
            if (eraseFreeSectors)
            {
                byte[] ZEROED_MINI_SECTOR = new byte[Sector.MINISECTOR_SIZE];
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
            List<Sector> miniFAT
                = GetSectorChain(header.FirstMiniFATSectorID, SectorType.Normal);

            using StreamView miniFATView
                = new StreamView(miniFAT, SectorSize, header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE, null, sourceStream);

            StreamRW miniFATStreamRW = new StreamRW(miniFATView);
            for (int i = nth_sector_to_remove; i < sectorChain.Count; i++)
            {
                int position = sectorChain[i].Id * 4;
                miniFATStreamRW.Seek(position, SeekOrigin.Begin);
                miniFATStreamRW.Write(Sector.FREESECT);
            }

            // Write End of Chain in MiniFAT ---------------------------------------
            //miniFATView.Seek(sectorChain[(sectorChain.Count - 1) - nth_sector_to_remove].Id * SIZE_OF_SID, SeekOrigin.Begin);
            //miniFATView.Write(BitConverter.GetBytes(Sector.ENDOFCHAIN), 0, 4);

            // Write End of Chain in MiniFAT ---------------------------------------
            if (nth_sector_to_remove > 0 && sectorChain.Count > 0)
            {
                miniFATStreamRW.Seek(sectorChain[nth_sector_to_remove - 1].Id * 4, SeekOrigin.Begin);
                miniFATStreamRW.Write(Sector.ENDOFCHAIN);
            }

            // Update sector chains           ---------------------------------------
            AllocateSectorChain(miniStreamView.BaseSectorChain);
            AllocateSectorChain(miniFATView.BaseSectorChain);

            // Update HEADER and root storage when ministream changes
            if (miniFAT.Count > 0)
            {
                RootStorage.DirEntry.StartSect = miniStream[0].Id;
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

        internal bool _transactionLockAdded;
        internal int _lockSectorId = -1;
        internal bool _transactionLockAllocated;

        /// <summary>
        /// Check for transaction lock sector addition and mark it in the FAT.
        /// </summary>
        private void CheckForLockSector()
        {
            //If transaction lock has been added and not yet allocated in the FAT...
            if (_transactionLockAdded && !_transactionLockAllocated)
            {
                using StreamView fatStream = new StreamView(GetFatSectorChain(), SectorSize, sourceStream);

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

            using StreamView fatStream =
                new StreamView(
                    fatSectors,
                    SectorSize,
                    header.FATSectorsNumber * SectorSize,
                    null,
                    sourceStream,
                    true);

            // Write FAT chain values --
            StreamRW fatStreamRW = new StreamRW(fatStream);
            for (int i = 0; i < sectorChain.Count - 1; i++)
            {
                Sector sN = sectorChain[i + 1];
                Sector sC = sectorChain[i];

                fatStreamRW.Seek(sC.Id * 4, SeekOrigin.Begin);
                fatStreamRW.Write(sN.Id);
            }

            fatStreamRW.Seek(sectorChain[sectorChain.Count - 1].Id * 4, SeekOrigin.Begin);
            fatStreamRW.Write(Sector.ENDOFCHAIN);

            // Merge chain to CFS
            AllocateDIFATSectorChain(fatStream.BaseSectorChain);
        }

        /// <summary>
        /// Setup the DIFAT sector chain
        /// </summary>
        /// <param name="FATsectorChain">A FAT sector chain</param>
        private void AllocateDIFATSectorChain(List<Sector> FATsectorChain)
        {
            //Get initial DIFAT chain
            List<Sector> difatSectors =
                        GetSectorChain(-1, SectorType.DIFAT);

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
            //int nCurrentSectors = sectors.Count;

            // Temp DIFAT count
            //int nDIFATSectors = (int)header.DIFATSectorsNumber;
            int nDIFATSectors = 0;

            if (FATsectorChain.Count > HEADER_DIFAT_ENTRIES_COUNT)
            {
                nDIFATSectors = Ceiling((double)(FATsectorChain.Count - HEADER_DIFAT_ENTRIES_COUNT) / DIFAT_SECTOR_FAT_ENTRIES_COUNT);
                nDIFATSectors = LowSaturation(nDIFATSectors - (int)header.DIFATSectorsNumber); //required DIFAT
            }

            //for (int i = 0; i < (nDIFATSectors - difatSectors.Count); i++)
            
            for (int i = 0; i < nDIFATSectors; i++)
            {
                Sector s = new Sector(SectorSize, sourceStream);
                sectors.Add(s);
                s.Id = sectors.Count - 1;
                s.Type = SectorType.DIFAT;
                difatSectors.Add(s);
            }

            // ...sum with new required DIFAT sectors count
            //nCurrentSectors += nDIFATSectors;
            //header.FATSectorsNumber += nDIFATSectors;

            // ReCheck FAT bias
            while (FATsectorChain.Count * FAT_SECTOR_ENTRIES_COUNT < sectors.Count)
            {
                Sector extraFATSector = new Sector(SectorSize, sourceStream);
                sectors.Add(extraFATSector);

                extraFATSector.Id = sectors.Count - 1;
                extraFATSector.Type = SectorType.FAT;

                FATsectorChain.Add(extraFATSector);

                //header.FATSectorsNumber++;
                //nCurrentSectors++;

                //... so, adding a FAT sector may induce DIFAT sectors to increase by one
                // and consequently this may induce ANOTHER FAT sector (TO-THINK: May this condition occur ?)
                if (difatSectors.Count * DIFAT_SECTOR_FAT_ENTRIES_COUNT < (FATsectorChain.Count - HEADER_DIFAT_ENTRIES_COUNT))
                {

                    Sector s = new Sector(SectorSize, sourceStream);
                    sectors.Add(s);
                    s.Type = SectorType.DIFAT;
                    s.Id = sectors.Count - 1;
                    difatSectors.Add(s);

                }
            }

            using StreamView difatStream
                = new StreamView(difatSectors, SectorSize, difatSectors.Count * SectorSize, null, sourceStream);

            StreamRW difatStreamRW = new(difatStream);

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
                        difatStreamRW.Write(0);
                    }

                    difatStreamRW.Write(FATsectorChain[i].Id);
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

            header.DIFATSectorsNumber = (uint)difatStream.BaseSectorChain.Count;

            // Chain first sector
            if (difatStream.BaseSectorChain != null && difatStream.BaseSectorChain.Count > 0)
            {
                header.FirstDIFATSectorID = difatStream.BaseSectorChain[0].Id;

                // Update header information
                header.DIFATSectorsNumber = (uint)difatStream.BaseSectorChain.Count;

                // Write chaining information at the end of DIFAT Sectors
                for (int i = 0; i < difatStream.BaseSectorChain.Count - 1; i++)
                {
                    BinaryPrimitives.WriteInt32LittleEndian(
                        difatStream.BaseSectorChain[i].GetData(),
                        SectorSize - sizeof(int),
                        difatStream.BaseSectorChain[i + 1].Id);
                }

                BinaryPrimitives.WriteInt32LittleEndian(
                    difatStream.BaseSectorChain[difatStream.BaseSectorChain.Count - 1].GetData(),
                    SectorSize - sizeof(int),
                    Sector.ENDOFCHAIN);
            }
            else
                header.FirstDIFATSectorID = Sector.ENDOFCHAIN;

            // Mark DIFAT Sectors in FAT
            using StreamView fatSv =
                new StreamView(FATsectorChain, SectorSize, FATsectorChain.Count * SectorSize, null, sourceStream);

            StreamRW streamRW = new(fatSv);

            for (int i = 0; i < difatStream.BaseSectorChain.Count; i++)
            {
                streamRW.Seek(difatStream.BaseSectorChain[i].Id * 4, SeekOrigin.Begin);
                streamRW.Write(Sector.DIFSECT);
            }

            for (int i = 0; i < fatSv.BaseSectorChain.Count; i++)
            {
                streamRW.Seek(fatSv.BaseSectorChain[i].Id * 4, SeekOrigin.Begin);
                streamRW.Write(Sector.FATSECT);
            }

            //fatSv.Seek(fatSv.BaseSectorChain[fatSv.BaseSectorChain.Count - 1].Id * 4, SeekOrigin.Begin);
            //fatSv.Write(BitConverter.GetBytes(Sector.ENDOFCHAIN), 0, 4);

            header.FATSectorsNumber = FATsectorChain.Count;
            header.DIFATSectorsNumber = (uint)difatSectors.Count;
        }

        /// <summary>
        /// Get the DIFAT Sector chain
        /// </summary>
        /// <returns>A list of DIFAT sectors</returns>
        private List<Sector> GetDifatSectorChain()
        {
            List<Sector> result
                = new List<Sector>();
            HashSet<int> processedSectors = new HashSet<int>();

            if (header.DIFATSectorsNumber != 0)
            {
                int validationCount = (int)header.DIFATSectorsNumber;
                Sector s = sectors[header.FirstDIFATSectorID];

                if (s == null) //Lazy loading
                {
                    s = new Sector(SectorSize, sourceStream)
                    {
                        Type = SectorType.DIFAT,
                        Id = header.FirstDIFATSectorID
                    };
                    sectors[header.FirstDIFATSectorID] = s;
                }

                result.Add(s);

                while (true && validationCount >= 0)
                {
                    int nextSecID = BitConverter.ToInt32(s.GetData(), SectorSize - 4);
                    EnsureUniqueSectorIndex(nextSecID, processedSectors);

                    // Strictly speaking, the following condition is not correct from
                    // a specification point of view:
                    // only ENDOFCHAIN should break DIFAT chain but
                    // a lot of existing compound files use FREESECT as DIFAT chain termination
                    if (nextSecID is Sector.FREESECT or Sector.ENDOFCHAIN) break;

                    validationCount--;

                    if (validationCount < 0)
                    {
                        if (closeStream)
                            Close();

                        if (ValidationExceptionEnabled)
                            throw new CFCorruptedFileException("DIFAT sectors count mismatched. Corrupted compound file");
                    }

                    s = sectors[nextSecID];

                    if (s == null)
                    {
                        s = new Sector(SectorSize, sourceStream)
                        {
                            Id = nextSecID
                        };
                        sectors[nextSecID] = s;
                    }

                    result.Add(s);
                }
            }

            return result;
        }

        private void EnsureUniqueSectorIndex(int nextSecID, HashSet<int> processedSectors)
        {
            if (!ValidationExceptionEnabled)
            {
                return;
            }

            if (!processedSectors.Add(nextSecID))
            {
                throw new CFCorruptedFileException("The file is corrupted.");
            }
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
            List<Sector> difatSectors = GetDifatSectorChain();

            int idx = 0;
            int nextSecID;

            // Read FAT entries from the header Fat entry array (max 109 entries)
            while (idx < header.FATSectorsNumber && idx < N_HEADER_FAT_ENTRY)
            {
                nextSecID = header.DIFAT[idx];
                Sector s = sectors[nextSecID];

                if (s == null)
                {
                    s = new Sector(SectorSize, sourceStream)
                    {
                        Id = nextSecID,
                        Type = SectorType.FAT
                    };
                    sectors[nextSecID] = s;
                }

                result.Add(s);

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

                while (result.Count < header.FATSectorsNumber)
                {
                    nextSecID = difatStreamRW.ReadInt32();

                    EnsureUniqueSectorIndex(nextSecID, processedSectors);

                    Sector s = sectors[nextSecID];

                    if (s == null)
                    {
                        s = new Sector(SectorSize, sourceStream)
                        {
                            Type = SectorType.FAT,
                            Id = nextSecID
                        };
                        sectors[nextSecID] = s; //UUU
                    }

                    result.Add(s);

                    if (difatStream.Position == (SectorSize - 4 + i * SectorSize))
                    {
                        // Skip DIFAT chain fields considering the possibility that the last FAT entry has been already read
                        if (difatStreamRW.ReadInt32() == Sector.ENDOFCHAIN)
                            break;

                        i++;
                        continue;
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
                List<Sector> miniFAT = GetNormalSectorChain(header.FirstMiniFATSectorID);
                List<Sector> miniStream = GetNormalSectorChain(RootEntry.StartSect);

                using StreamView miniFATView
                    = new StreamView(miniFAT, SectorSize, header.MiniFATSectorsNumber * SectorSize, null, sourceStream);

                using StreamView miniStreamView =
                    new StreamView(miniStream, SectorSize, RootStorage.Size, null, sourceStream);

                StreamRW miniFATReader = new StreamRW(miniFATView);

                int nextSecID = secID;
                HashSet<int> processedSectors = new HashSet<int>();

                while (true)
                {
                    if (nextSecID == Sector.ENDOFCHAIN)
                        break;

                    Sector ms = new Sector(Sector.MINISECTOR_SIZE, sourceStream)
                    {
                        Id = nextSecID,
                        Type = SectorType.Mini
                    };

                    miniStreamView.Seek(nextSecID * Sector.MINISECTOR_SIZE, SeekOrigin.Begin);
                    miniStreamView.ReadExactly(ms.GetData(), 0, Sector.MINISECTOR_SIZE);

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
            return chainType switch
            {
                SectorType.DIFAT => GetDifatSectorChain(),
                SectorType.FAT => GetFatSectorChain(),
                SectorType.Normal => GetNormalSectorChain(secID),
                SectorType.Mini => GetMiniSectorChain(secID),
                _ => throw new CFException("Unsupported chain type"),
            };
        }

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
        public CFStorage RootStorage { get; private set; }

        public CFSVersion Version => (CFSVersion)header.MajorVersion;

        //internal class NodeFactory : IRBTreeDeserializer<CFItem>
        //{

        //    public RBNode<CFItem> DeserizlizeFromValues()
        //    {
        //           RBNode<CFItem> node = new RBNode<CFItem>(value,(Color)value.DirEntry.StgColor,
        //    }
        //}

        //void OnValueAssigned(RBNode<CFItem> node, CFItem from)
        //{
        //    if (from.DirEntry != null && from.DirEntry.LeftSibling != DirectoryEntry.NOSTREAM)

        //    if (from.DirEntry != null && from.DirEntry.LeftSibling != DirectoryEntry.NOSTREAM)
        //        node.Value.DirEntry.LeftSibling = from.DirEntry.LeftSibling;

        //    if (from.DirEntry != null && from.DirEntry.RightSibling != DirectoryEntry.NOSTREAM)
        //        node.Value.DirEntry.RightSibling = from.DirEntry.RightSibling;
        //}

        internal RBTree GetChildrenTree(IDirectoryEntry entry)
        {
            RBTree bst = new();
            List<int> levelSIDs = new List<int>();
            LoadChildren(bst, entry.Child, levelSIDs);
            return bst;
        }

        private static void NullifyChildNodes(IDirectoryEntry de)
        {
            de.Parent = null;
            de.Left = null;
            de.Right = null;
        }

        private void LoadChildren(RBTree bst, IDirectoryEntry de, List<int> levelSIDs)
        {
            levelSIDs.Add(de.SID);

            if (de.StgType == StgType.StgInvalid)
            {
                if (ValidationExceptionEnabled)
                    throw new CFCorruptedFileException($"A Directory Entry has a valid reference to an Invalid Storage Type directory [{de.SID}]");
                return;
            }

            if (!Enum.IsDefined(typeof(StgType), de.StgType))
            {
                if (ValidationExceptionEnabled)
                    throw new CFCorruptedFileException("A Directory Entry has an invalid Storage Type");
                return;
            }

            LoadChildren(bst, de.LeftSibling, levelSIDs);
            LoadChildren(bst, de.RightSibling, levelSIDs);
            NullifyChildNodes(de);
            bst.Insert(de);
        }

        private void LoadChildren(RBTree bst, int sid, List<int> levelSIDs)
        {
            if (sid == DirectoryEntry.NOSTREAM)
                return;

            // if this siblings id does not overflow current list
            if (sid >= directoryEntries.Count)
            {
                if (ValidationExceptionEnabled)
                    throw new CFCorruptedFileException($"A Directory Entry references the non-existent sid number {sid}");
                return;
            }

            if (levelSIDs.Contains(sid))
                throw new CFCorruptedFileException("Cyclic reference of directory item");

            IDirectoryEntry de = directoryEntries[sid];
            LoadChildren(bst, de, levelSIDs);
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

            using StreamView dirReader
                = new StreamView(directoryChain, SectorSize, directoryChain.Count * SectorSize, null, sourceStream);

            StreamRW dirReaderRW = new(dirReader);

            while (dirReader.Position < directoryChain.Count * SectorSize)
            {
                IDirectoryEntry de = DirectoryEntry.New(string.Empty, StgType.StgInvalid, directoryEntries);

                // We are not inserting dirs. Do not use 'InsertNewDirectoryEntry'
                de.Read(dirReaderRW, Version);
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

            using StreamView sv = new StreamView(directorySectors, SectorSize, 0, null, sourceStream);

            StreamRW svRW = new(sv);

            foreach (IDirectoryEntry di in directoryEntries)
            {
                di.Write(svRW);
            }

            int delta = directoryEntries.Count;

            while (delta % (SectorSize / DIRECTORY_SIZE) != 0)
            {
                IDirectoryEntry dummy = DirectoryEntry.New(string.Empty, StgType.StgInvalid, directoryEntries);
                dummy.Write(svRW);
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
        /// Saves the in-memory image of Compound File opened in ReadOnly mode to a file.
        /// </summary>
        /// <param name="fileName">File name to write the compound file to</param>
        /// <exception cref="T:OpenMcdf.CFException">Raised if destination file is not seekable</exception>
        /// <exception cref="T:OpenMcdf.CFInvalidOperation">Raised if destination file is the current file</exception>
        public void SaveAs(string fileName)
        {
            if (IsClosed)
                throw new CFException("Compound File closed: cannot save data");

            try
            {
                bool raiseSaveFileEx = false;

                if (this.HasSourceStream && this.sourceStream != null && this.sourceStream is FileStream stream)
                {
                    if (Path.IsPathRooted(fileName))
                    {
                        //Debug.WriteLine("Path is rooted");
                        //Debug.WriteLine("Filename:"+ fileName);
                        //Debug.WriteLine("Stream name:"+ stream.Name);
                        //Debug.WriteLine("Stream name equals filename? :" + (stream.Name == fileName));

                        if (stream.Name == fileName)
                        {
                            //Debug.WriteLine("-> Filename equals stream name");

                            raiseSaveFileEx = true;
                        }
                    }
                    else
                    {
                        //Debug.WriteLine("Path is NOT rooted");
                        //Debug.WriteLine("Filename:"+ fileName);
                        //Debug.WriteLine("Filename modified:"+ (Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + fileName));
                        //Debug.WriteLine("Directory name:"+ Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
                        //Debug.WriteLine("Stream name:"+ stream.Name);
                        //Debug.WriteLine("Stream name equals filename? :" + (stream.Name ==  (Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + fileName)));

                        if (stream.Name == (Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + fileName))
                        {
                            //Debug.WriteLine("-> Filename equals stream name:");

                            raiseSaveFileEx = true;
                        }
                    }
                }

                if (raiseSaveFileEx)
                {
                    throw new CFInvalidOperation("Cannot overwrite current backing file. Compound File should be opened in UpdateMode and Commit() method should be called to persist changes");
                }

                using FileStream fs = new(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                Save(fs);
            }
            catch (Exception ex)
            {
                throw new CFException("Error saving file [" + fileName + "]", ex);
            }
            finally
            {
                sourceStream?.Close();
            }
        }

        /// <summary>
        /// Saves the in-memory image of Compound File to a file.
        /// </summary>
        /// <param name="fileName">File name to write the compound file to</param>
        /// <exception cref="T:OpenMcdf.CFException">Raised if destination file is not seekable</exception>
        /// <exception cref="T:OpenMcdf.CFInvalidOperation">Raised if destination file is the current file</exception>
        [Obsolete("Use SaveAs method")]
        public void Save(string fileName)
        {
            SaveAs(fileName);
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
            if (IsClosed)
                throw new CFDisposedException("Compound File closed: cannot save data");

            if (!stream.CanSeek)
                throw new CFException("Cannot save on a non-seekable stream");

            CheckForLockSector();
            int sSize = SectorSize;

            try
            {
                if (this.HasSourceStream && this.sourceStream != null && this.sourceStream is FileStream && stream is FileStream otherStream)
                {
                    if (((FileStream)this.sourceStream).Name == otherStream.Name)
                    {
                        throw new CFInvalidOperation("Cannot overwrite current backing file. Compound File should be opened in UpdateMode and Commit() method should be called to persist changes");
                    }
                }

                stream.Write(new byte[sSize], 0, sSize);

                CommitDirectory();

                for (int i = 0; i < sectors.Count; i++)
                {
                    Sector s = sectors[i];

                    // Load source (unmodified) sectors
                    // Here we have to ignore "Dirty flag" of
                    // sectors because we are NOT modifying the source
                    // in a differential way but ALL sectors need to be
                    // persisted on the destination stream
                    s ??= new Sector(sSize, sourceStream)
                    {
                        Id = i
                    };

                    stream.Write(s.GetData(), 0, sSize);

                    //s.ReleaseData();
                }

                stream.Seek(0, SeekOrigin.Begin);
                header.Write(stream);
            }
            catch (Exception ex)
            {
                sourceStream?.Close();
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
                using StreamView fatStream = new StreamView(FatChain, SectorSize, header.FATSectorsNumber * SectorSize, null, sourceStream);
                StreamRW fatStreamRW = new(fatStream);

                int idx = 0;

                while (idx < sectors.Count)
                {
                    int id = fatStreamRW.ReadInt32();

                    if (id == Sector.FREESECT)
                    {
                        if (sectors[idx] == null)
                        {
                            Sector s = new Sector(SectorSize, sourceStream)
                            {
                                Id = idx
                            };
                            sectors[idx] = s;
                        }

                        freeList.Enqueue(sectors[idx]);
                    }

                    idx++;
                }
            }
            else
            {
                List<Sector> miniFAT
                    = GetSectorChain(header.FirstMiniFATSectorID, SectorType.Normal);

                using StreamView miniFATView
                    = new StreamView(miniFAT, SectorSize, header.MiniFATSectorsNumber * SectorSize, null, sourceStream);

                StreamRW miniFATStreamRW = new(miniFATView);

                List<Sector> miniStream
                    = GetSectorChain(RootEntry.StartSect, SectorType.Normal);

                using StreamView miniStreamView
                    = new StreamView(miniStream, SectorSize, RootStorage.Size, null, sourceStream);

                int idx = 0;

                int nMinisectors = (int)(miniStreamView.Length / Sector.MINISECTOR_SIZE);

                while (idx < nMinisectors)
                {
                    //AssureLength(miniStreamView, (int)miniFATView.Length);

                    int nextId = miniFATStreamRW.ReadInt32();

                    if (nextId == Sector.FREESECT)
                    {
                        Sector ms = new Sector(Sector.MINISECTOR_SIZE, sourceStream)
                        {
                            Id = idx,
                            Type = SectorType.Mini
                        };

                        miniStreamView.Seek(ms.Id * Sector.MINISECTOR_SIZE, SeekOrigin.Begin);
                        miniStreamView.ReadExactly(ms.GetData(), 0, Sector.MINISECTOR_SIZE);

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
        internal void AppendData(CFItem cfItem, byte[] buffer)
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
            int newSectorSize = SectorSize;

            if (length < header.MinSizeStandardStream)
            {
                newSectorType = SectorType.Mini;
                newSectorSize = Sector.MINISECTOR_SIZE;
            }

            SectorType oldSectorType = SectorType.Normal;
            int oldSectorSize = SectorSize;

            if (cfItem.Size < header.MinSizeStandardStream)
            {
                oldSectorType = SectorType.Mini;
                oldSectorSize = Sector.MINISECTOR_SIZE;
            }

            long oldSize = cfItem.Size;

            // Get Sector chain and delta size induced by client
            List<Sector> sectorChain = GetSectorChain(cfItem.DirEntry.StartSect, oldSectorType);
            long delta = length - cfItem.Size;

            // Check for transition ministream -> stream:
            // Only in this case we need to free old sectors,
            // otherwise they will be overwritten.

            bool transitionToMini = false;
            bool transitionToNormal = false;
            List<Sector> oldChain = null;

            if (cfItem.DirEntry.StartSect != Sector.ENDOFCHAIN)
            {
                if (
                    (length < header.MinSizeStandardStream && cfItem.DirEntry.Size >= header.MinSizeStandardStream)
                    || (length >= header.MinSizeStandardStream && cfItem.DirEntry.Size < header.MinSizeStandardStream))
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
            if (!transitionToMini && !transitionToNormal) //############  NO TRANSITION
            {
                if (delta > 0) // Enlarging stream...
                {
                    if (sectorRecycle)
                        freeList = FindFreeSectors(newSectorType); // Collect available free sectors

                    using StreamView sv = new(sectorChain, newSectorSize, length, freeList, sourceStream);

                    //Set up  destination chain
                    SetSectorChain(sectorChain);
                }
                else if (delta < 0) // Reducing size...
                {
                    int nSec = (int)Math.Floor((double)Math.Abs(delta) / newSectorSize); //number of sectors to mark as free

                    int startFreeSector = sectorChain.Count - nSec; // start sector to free

                    if (newSectorSize == Sector.MINISECTOR_SIZE)
                        FreeMiniChain(sectorChain, startFreeSector);
                    else
                        FreeChain(sectorChain, startFreeSector);
                }

                if (sectorChain.Count > 0)
                {
                    cfItem.DirEntry.StartSect = sectorChain[0].Id;
                    cfItem.DirEntry.Size = length;
                }
                else
                {
                    cfItem.DirEntry.StartSect = Sector.ENDOFCHAIN;
                    cfItem.DirEntry.Size = 0;
                }
            }
            else if (transitionToMini) //############## TRANSITION TO MINISTREAM
            {
                // Transition Normal chain -> Mini chain

                // Collect available MINI free sectors

                if (sectorRecycle)
                    freeList = FindFreeSectors(SectorType.Mini);

                using StreamView sv = new(oldChain, oldSectorSize, oldSize, null, sourceStream);

                // Reset start sector and size of dir entry
                cfItem.DirEntry.StartSect = Sector.ENDOFCHAIN;
                cfItem.DirEntry.Size = 0;

                List<Sector> newChain = GetMiniSectorChain(Sector.ENDOFCHAIN);
                using StreamView destSv = new(newChain, Sector.MINISECTOR_SIZE, length, freeList, sourceStream);

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

                sv.ReadExactly(buf, 0, (int)toRead);
                destSv.Write(buf, 0, (int)toRead);

                //Free old chain
                FreeChain(oldChain);

                //Set up destination chain
                AllocateMiniSectorChain(destSv.BaseSectorChain);

                // Persist to normal stream
                PersistMiniStreamToStream(destSv.BaseSectorChain);

                //Update dir item
                if (destSv.BaseSectorChain.Count > 0)
                {
                    cfItem.DirEntry.StartSect = destSv.BaseSectorChain[0].Id;
                    cfItem.DirEntry.Size = length;
                }
                else
                {
                    cfItem.DirEntry.StartSect = Sector.ENDOFCHAIN;
                    cfItem.DirEntry.Size = 0;
                }
            }
            else if (transitionToNormal) //############## TRANSITION TO NORMAL STREAM
            {
                // Transition Mini chain -> Normal chain

                if (sectorRecycle)
                    freeList = FindFreeSectors(SectorType.Normal); // Collect available Normal free sectors

                using StreamView sv = new(oldChain, oldSectorSize, oldSize, null, sourceStream);

                List<Sector> newChain = GetNormalSectorChain(Sector.ENDOFCHAIN);
                using StreamView destSv = new StreamView(newChain, SectorSize, length, freeList, sourceStream);

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

                sv.ReadExactly(buf, 0, (int)toRead);
                destSv.Write(buf, 0, (int)toRead);

                //Free old mini chain
                int oldChainCount = oldChain.Count;
                FreeMiniChain(oldChain);

                //Set up normal destination chain
                AllocateSectorChain(destSv.BaseSectorChain);

                //Update dir item
                if (destSv.BaseSectorChain.Count > 0)
                {
                    cfItem.DirEntry.StartSect = destSv.BaseSectorChain[0].Id;
                    cfItem.DirEntry.Size = length;
                }
                else
                {
                    cfItem.DirEntry.StartSect = Sector.ENDOFCHAIN;
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
            long delta = position + count - cfItem.Size < 0 ? 0 : position + count - cfItem.Size;
            long newLength = cfItem.Size + delta;

            SetStreamLength(cfItem, newLength);

            // Calculate NEW sectors SIZE
            SectorType _st = SectorType.Normal;
            int _sectorSize = SectorSize;

            if (cfItem.Size < header.MinSizeStandardStream)
            {
                _st = SectorType.Mini;
                _sectorSize = Sector.MINISECTOR_SIZE;
            }

            List<Sector> sectorChain = GetSectorChain(cfItem.DirEntry.StartSect, _st);
            using StreamView sv = new StreamView(sectorChain, _sectorSize, newLength, null, sourceStream);

            sv.Seek(position, SeekOrigin.Begin);
            sv.Write(buffer, offset, count);

            if (cfItem.Size < header.MinSizeStandardStream)
            {
                PersistMiniStreamToStream(sv.BaseSectorChain);
                //SetSectorChain(sv.BaseSectorChain);
            }
        }

        internal void WriteData(CFItem cfItem, byte[] buffer)
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

        internal int ReadData(IDirectoryEntry de, long position, byte[] buffer, int offset, int count)
        {
            count = (int)Math.Min(buffer.Length - offset, (long)count);

            SectorType sectorType = de.Size < header.MinSizeStandardStream ? SectorType.Mini : SectorType.Normal;
            List<Sector> chain = GetSectorChain(de.StartSect, sectorType);
            int sectorSize = sectorType == SectorType.Mini ? Sector.MINISECTOR_SIZE : SectorSize;
            using StreamView sView = new(chain, sectorSize, de.Size, null, sourceStream);
            sView.Seek(position, SeekOrigin.Begin);
            int result = sView.Read(buffer, offset, count);
            return result;
        }

        internal byte[] GetData(IDirectoryEntry de)
        {
            byte[] result = new byte[(int)de.Size];
            ReadData(de, 0, result, 0, result.Length);
            return result;
        }

        public byte[] GetDataBySID(int sid)
        {
            if (sid < 0)
                return null;

            if (IsClosed)
                throw new CFDisposedException("Compound File closed: cannot access data");

            try
            {
                IDirectoryEntry de = directoryEntries[sid];
                return GetData(de);
            }
            catch
            {
                throw new CFException("Cannot get data for SID");
            }
        }

        public Guid getGuidBySID(int sid)
        {
            if (IsClosed)
                throw new CFDisposedException("Compound File closed: cannot access data");
            if (sid < 0)
                throw new CFException("Invalid SID");
            IDirectoryEntry de = directoryEntries[sid];
            return de.StorageCLSID;
        }

        public Guid getGuidForStream(int sid)
        {
            if (IsClosed)
                throw new CFDisposedException("Compound File closed: cannot access data");
            if (sid < 0)
                throw new CFException("Invalid SID");
            Guid g = Guid.Empty;
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


        internal void FreeAssociatedData(int sid)
        {
            // Clear the associated stream (or ministream) if required
            if (directoryEntries[sid].Size > 0) //thanks to Mark Bosold for this !
            {
                if (directoryEntries[sid].Size < header.MinSizeStandardStream)
                {
                    List<Sector> miniChain
                        = GetSectorChain(directoryEntries[sid].StartSect, SectorType.Mini);
                    FreeMiniChain(miniChain);
                }
                else
                {
                    List<Sector> chain
                        = GetSectorChain(directoryEntries[sid].StartSect, SectorType.Normal);
                    FreeChain(chain);
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
        public void Close() => CloseCore(true);

        private bool closeStream = true;

        [Obsolete("Use flag LeaveOpen in CompoundFile constructor")]
        public void Close(bool closeStream) => CloseCore(closeStream);

        private void CloseCore(bool closeStream)
        {
            this.closeStream = closeStream;
            ((IDisposable)this).Dispose();
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private readonly object lockObject = new();

        /// <summary>
        /// When called from user code, release all resources, otherwise, in the case runtime called it,
        /// only unmanaged resources are released.
        /// </summary>
        /// <param name="disposing">If true, method has been called from User code, if false it's been called from .net runtime</param>
        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (!IsClosed)
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

                            RootStorage = null; // Some problem releasing resources...
                            header = null;
                            directoryEntries.Clear();
                            directoryEntries = null;
                            //this.lockObject = null;
#if !FLAT_WRITE
                            this.buffer = null;
#endif
                        }

                        if (sourceStream != null && closeStream && !Configuration.HasFlag(CFSConfiguration.LeaveOpen))
                            sourceStream.Close();
                    }
                }
            }
            finally
            {
                IsClosed = true;
            }
        }

        internal bool IsClosed { get; private set; }

        private List<IDirectoryEntry> directoryEntries
            = new();

        internal IList<IDirectoryEntry> Directories => directoryEntries;

        //internal List<IDirectoryEntry> DirectoryEntries
        //{
        //    get { return directoryEntries; }
        //}

        internal IDirectoryEntry RootEntry => directoryEntries[0];

        private List<IDirectoryEntry> FindDirectoryEntries(string entryName)
        {
            List<IDirectoryEntry> result = new List<IDirectoryEntry>();

            foreach (IDirectoryEntry d in directoryEntries)
            {
                if (d.StgType != StgType.StgInvalid && d.GetEntryName() == entryName)
                    result.Add(d);
            }

            return result;
        }

        /// <summary>
        /// Get a list of all entries with a given name contained in the document.
        /// </summary>
        /// <param name="entryName">Name of entries to retrieve</param>
        /// <returns>A list of name-matching entries</returns>
        /// <remarks>This function is aimed to speed up entity lookup in
        /// flat-structure files (only one or little more known entries)
        /// without the performance penalty related to entities hierarchy constraints.
        /// There is no implied hierarchy in the returned list.
        /// </remarks>
        public IList<CFItem> GetAllNamedEntries(string entryName)
        {
            IList<IDirectoryEntry> r = FindDirectoryEntries(entryName);
            List<CFItem> result = new List<CFItem>();

            foreach (IDirectoryEntry id in r)
            {
                if (id.StgType != StgType.StgInvalid && id.GetEntryName() == entryName)
                {
                    CFItem i = id.StgType == StgType.StgStorage ? new CFStorage(this, id) : new CFStream(this, id);
                    result.Add(i);
                }
            }

            return result;
        }

        public int GetNumDirectories()
        {
            if (IsClosed)
                throw new CFDisposedException("Compound File closed: cannot access data");
            return directoryEntries.Count;
        }

        public string GetNameDirEntry(int id)
        {
            if (IsClosed)
                throw new CFDisposedException("Compound File closed: cannot access data");
            if (id < 0)
                throw new CFException("Invalid Storage ID");
            return directoryEntries[id].Name;
        }

        public StgType GetStorageType(int id)
        {
            if (IsClosed)
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
            using CompoundFile cf = new(s, CFSUpdateMode.ReadOnly, CFSConfiguration.LeaveOpen);
            if (cf.header.MajorVersion != (ushort)CFSVersion.Ver_3)
                throw new CFException("Current implementation of free space compression does not support version 4 of Compound File Format");

            using MemoryStream tmpMS = new((int)cf.sourceStream.Length); // This could be a problem for v4

            using (CompoundFile tempCF = new((CFSVersion)cf.header.MajorVersion, cf.Configuration))
            {
                tempCF.RootStorage.CLSID = cf.RootStorage.CLSID;
                DoCompression(cf.RootStorage, tempCF.RootStorage);
                tempCF.Save(tmpMS);
            }

            // If we were based on a writable stream, we update
            // the stream and do reload from the compressed one...
            s.Seek(0, SeekOrigin.Begin);
            tmpMS.WriteTo(s);

            s.Seek(0, SeekOrigin.Begin);
            s.SetLength(tmpMS.Length);
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
        public static void ShrinkCompoundFile(string fileName)
        {
            using FileStream fs = new(fileName, FileMode.Open, FileAccess.ReadWrite);
            ShrinkCompoundFile(fs);
        }

        /// <summary>
        /// Recursively clones valid structures, avoiding to copy free sectors.
        /// </summary>
        /// <param name="currSrcStorage">Current source storage to clone</param>
        /// <param name="currDstStorage">Current cloned destination storage</param>
        private static void DoCompression(CFStorage currSrcStorage, CFStorage currDstStorage)
        {
            void va(CFItem item)
            {
                if (item.IsStream)
                {
                    CFStream itemAsStream = item as CFStream;
                    CFStream st = currDstStorage.AddStream(itemAsStream.Name);
                    st.SetData(itemAsStream.GetData());
                }
                else if (item.IsStorage)
                {
                    CFStorage itemAsStorage = item as CFStorage;
                    CFStorage strg = currDstStorage.AddStorage(itemAsStorage.Name);
                    strg.CLSID = itemAsStorage.CLSID;
                    DoCompression(itemAsStorage, strg); // recursion, one level deeper
                }
            }

            currSrcStorage.VisitEntries(va, false);
        }
    }
}
