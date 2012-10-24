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

#define FLAT_WRITE // No optimization on the number of write operations

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using BinaryTrees;
using System.Collections;
using System.Security.AccessControl;
using System.Threading;

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
    public enum UpdateMode
    {
        /// <summary>
        /// ReadOnly update mode prevents overwriting
        /// of the opened file. 
        /// Data changes are allowed but they have to be 
        /// persisted on a different file when required.
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
        //private ArrayList sectors = new ArrayList();

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
            rootStorage = new CFStorage(this);

            rootStorage.DirEntry.SetEntryName("Root Entry");
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
        ///     CompoundFile cf = new CompoundFile(CFSVersion.Ver_4, true, true);
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
        /// Sector recycling reduces data writing performances but avoids space wasting in scenarios with frequently
        /// data manipulation of the same streams. The new compound file is open in Update mode.
        /// </remarks>
        public CompoundFile(CFSVersion cfsVersion, bool sectorRecycle, bool eraseFreeSectors)
        {
            this.header = new Header((ushort)cfsVersion);
            this.sectorRecycle = sectorRecycle;

            
            DIFAT_SECTOR_FAT_ENTRIES_COUNT = (GetSectorSize() / 4) - 1;
            FAT_SECTOR_ENTRIES_COUNT = (GetSectorSize() / 4);

            //Root -- 
            rootStorage = new CFStorage(this);

            rootStorage.DirEntry.SetEntryName("Root Entry");
            rootStorage.DirEntry.StgType = StgType.StgRoot;
            rootStorage.DirEntry.StgColor = StgColor.Black;

            //this.InsertNewDirectoryEntry(rootStorage.DirEntry);
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
        /// Load an existing compound file.
        /// </summary>
        /// <param name="fileName">Compound file to read from</param>
        /// <param name="sectorRecycle">If true, recycle unused sectors</param>
        /// <param name="updateMode">Select the update mode of the underlying data file</param>
        /// <param name="eraseFreeSectors">If true, overwrite with zeros unallocated sectors</param>
        /// <param name="noValidationException">If true, no <see cref="T:OpenMcdf.CFCorruptedFileException">CFCorruptedFileException</see>
        ///  will be thrown even if corrupted file is loaded. Please note that this option is could pose a potential security threat</param>
        /// <example>
        /// <code>
        /// String srcFilename = "data_YOU_CAN_CHANGE.xls";
        /// 
        /// CompoundFile cf = new CompoundFile(srcFilename, UpdateMode.Update, true, true, true);
        ///
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
        public CompoundFile(String fileName, UpdateMode updateMode, bool sectorRecycle, bool eraseFreeSectors, bool noValidationException)
        {
            this.validationExceptionEnabled = !noValidationException;

            this.sectorRecycle = sectorRecycle;
            this.updateMode = updateMode;
            this.eraseFreeSectors = eraseFreeSectors;

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
        public CompoundFile(Stream stream, UpdateMode updateMode, bool sectorRecycle, bool eraseFreeSectors)
        {
            this.sectorRecycle = sectorRecycle;
            this.updateMode = updateMode;
            this.eraseFreeSectors = eraseFreeSectors;

            LoadStream(stream);

            DIFAT_SECTOR_FAT_ENTRIES_COUNT = (GetSectorSize() / 4) - 1;
            FAT_SECTOR_ENTRIES_COUNT = (GetSectorSize() / 4);
        }

        /// <summary>
        /// Load an existing compound file.
        /// </summary>
        /// <param name="stream">A stream containing a compound file to read</param>
        /// <param name="sectorRecycle">If true, recycle unused sectors</param>
        /// <param name="updateMode">Select the update mode of the underlying data file</param>
        /// <param name="eraseFreeSectors">If true, overwrite with zeros unallocated sectors</param>
        /// <param name="noValidationException">If true, openMcdf will try to ignore invalid references or format in order to load a possibly corrupted file anyway </param>
        /// <remarks>The 'noValidationEcxception' parameter could possibly lead to security issues so it's recommanded to use it only on trusted sources</remarks>
        /// <example>
        /// <code>
        /// 
        /// String filename = "reportREAD.xls";
        ///   
        /// FileStream fs = new FileStream(filename, FileMode.Open);
        /// CompoundFile cf = new CompoundFile(fs, UpdateMode.ReadOnly, false, false, false, true);
        /// CFStream foundStream = cf.RootStorage.GetStream("Workbook");
        ///
        /// byte[] temp = foundStream.GetData(); //if 'reportRead.xls' is corrupted, openMcdf will try to lad it anyway [noValidationException set true]
        ///
        /// Assert.IsNotNull(temp);
        ///
        /// cf.Close();
        ///
        /// </code>
        /// </example>
        /// <exception cref="T:OpenMcdf.CFException">Raised when trying to open a non-seekable stream</exception>
        /// <exception cref="T:OpenMcdf.CFException">Raised stream is null</exception>
        public CompoundFile(Stream stream, UpdateMode updateMode, bool sectorRecycle, bool eraseFreeSectors, bool noValidationException)
        {
            this.validationExceptionEnabled = !noValidationException;

            this.sectorRecycle = sectorRecycle;
            this.updateMode = updateMode;
            this.eraseFreeSectors = eraseFreeSectors;

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

        private UpdateMode updateMode = UpdateMode.ReadOnly;
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

            if (updateMode != UpdateMode.Update)
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
                    continue;
                }
                if (releaseMemory)
                {
                    s.ReleaseData();
                    s = null;
                    sectors[i] = null;

                    //GC.Collect();
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

            sourceStream.Flush();


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
                if (stream != null)
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
                if (this.updateMode == UpdateMode.ReadOnly)
                {
                    fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                }
                else
                {
                    fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                }

                Load(fs);

            }
            catch (Exception ex)
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
                = new StreamView(
                    miniFAT,
                    GetSectorSize(),
                    header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE,
                    this.sourceStream
                    );

            StreamView miniStreamView
                = new StreamView(
                    miniStream,
                    GetSectorSize(),
                    this.rootStorage.Size,
                    sourceStream);

            // Set updated/new sectors within the ministream
            for (int i = 0; i < sectorChain.Count; i++)
            {
                Sector s = sectorChain[i];

                if (s.Id != -1)
                {
                    // Overwrite

                    miniStreamView.Seek(Sector.MINISECTOR_SIZE * s.Id, SeekOrigin.Begin);
                    miniStreamView.Write(s.GetData(), 0, Sector.MINISECTOR_SIZE);
                }
                else
                {
                    // Allocate, position ministream at the end of already allocated
                    // ministream's sectors

                    miniStreamView.Seek(this.rootStorage.Size, SeekOrigin.Begin);
                    miniStreamView.Write(s.GetData(), 0, Sector.MINISECTOR_SIZE);
                    s.Id = (int)(miniStreamView.Position - Sector.MINISECTOR_SIZE) / Sector.MINISECTOR_SIZE;

                    this.rootStorage.DirEntry.Size = miniStreamView.Length;
                }
            }

            // Update miniFAT
            for (int i = 0; i < sectorChain.Count - 1; i++)
            {
                Int32 currentId = sectorChain[i].Id;
                Int32 nextId = sectorChain[i + 1].Id;

                //AssureLength(miniFATView, Math.Max(currentId * SIZE_OF_SID, nextId * SIZE_OF_SID));

                miniFATView.Seek(currentId * 4, SeekOrigin.Begin);
                miniFATView.Write(BitConverter.GetBytes(nextId), 0, 4);
            }

            //AssureLength(miniFATView, sectorChain[sectorChain.Count - 1].Id * SIZE_OF_SID);

            // Write End of Chain in MiniFAT
            miniFATView.Seek(sectorChain[sectorChain.Count - 1].Id * SIZE_OF_SID, SeekOrigin.Begin);
            miniFATView.Write(BitConverter.GetBytes(Sector.ENDOFCHAIN), 0, 4);

            // Update sector chains
            SetNormalSectorChain(miniStreamView.BaseSectorChain);
            SetNormalSectorChain(miniFATView.BaseSectorChain);

            //Update HEADER and root storage when ministream changes
            if (miniFAT.Count > 0)
            {
                this.rootStorage.DirEntry.StartSetc = miniStream[0].Id;
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
                = new StreamView(FAT, GetSectorSize(), FAT.Count * GetSectorSize(), sourceStream);

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

                //AssureLength(FATView, Math.Max(currentId * SIZE_OF_SID, nextId * SIZE_OF_SID));

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
                = new StreamView(miniFAT, GetSectorSize(), header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE, sourceStream);

            StreamView miniStreamView
                = new StreamView(miniStream, GetSectorSize(), this.rootStorage.Size, sourceStream);

            // Set updated/new sectors within the ministream
            if (zeroSector)
            {
                for (int i = 0; i < sectorChain.Count; i++)
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

            // Update miniFAT
            for (int i = 0; i < sectorChain.Count - 1; i++)
            {
                Int32 currentId = sectorChain[i].Id;
                Int32 nextId = sectorChain[i + 1].Id;

                // AssureLength(miniFATView, Math.Max(currentId * SIZE_OF_SID, nextId * SIZE_OF_SID));

                miniFATView.Seek(currentId * 4, SeekOrigin.Begin);
                miniFATView.Write(BitConverter.GetBytes(Sector.FREESECT), 0, 4);
            }

            //AssureLength(miniFATView, sectorChain[sectorChain.Count - 1].Id * SIZE_OF_SID);

            // Write End of Chain in MiniFAT
            miniFATView.Seek(sectorChain[sectorChain.Count - 1].Id * SIZE_OF_SID, SeekOrigin.Begin);
            miniFATView.Write(BitConverter.GetBytes(Sector.FREESECT), 0, 4);

            // Update sector chains
            SetNormalSectorChain(miniStreamView.BaseSectorChain);
            SetNormalSectorChain(miniFATView.BaseSectorChain);

            //Update HEADER and root storage when ministream changes
            if (miniFAT.Count > 0)
            {
                this.rootStorage.DirEntry.StartSetc = miniStream[0].Id;
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
                if (s.Id == -1)
                {
                    sectors.Add(s);
                    s.Id = sectors.Count - 1;
                }
            }

            SetFATSectorChain(sectorChain);
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
        private void SetFATSectorChain(List<Sector> sectorChain)
        {
            List<Sector> fatSectors = GetSectorChain(-1, SectorType.FAT);
            StreamView fatStream =
                new StreamView(
                    fatSectors,
                    GetSectorSize(),
                    header.FATSectorsNumber * GetSectorSize(), sourceStream
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
                new StreamView(FATsectorChain, GetSectorSize(), header.FATSectorsNumber * GetSectorSize(), sourceStream);


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

                    // Strictly speaking, the following condition is not correct from
                    // a specification point of view:
                    // only ENDOFCHAIN should break DIFAT chain but 
                    // a lot of existing compound files use FREESECT as DIFAT chain termination
                    if (nextSecID == Sector.FREESECT || nextSecID == Sector.ENDOFCHAIN) break;

                    validationCount--;

                    if (validationCount < 0)
                    {
                        this.Close();
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
                StreamView difatStream
                    = new StreamView
                        (
                        difatSectors,
                        GetSectorSize(),
                        header.FATSectorsNumber > N_HEADER_FAT_ENTRY ?
                            (header.FATSectorsNumber - N_HEADER_FAT_ENTRY) * 4 :
                            0,
                            sourceStream
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

                    Sector s = sectors[nextSecID] as Sector;

                    if (s == null)
                    {
                        s = new Sector(GetSectorSize(), sourceStream);
                        s.Type = SectorType.FAT;
                        s.Id = nextSecID;
                        sectors[nextSecID] = s;//UUU
                    }

                    result.Add(s);

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

            StreamView fatStream
                = new StreamView(fatSectors, GetSectorSize(), fatSectors.Count * GetSectorSize(), sourceStream);

            while (true)
            {
                if (nextSecID == Sector.ENDOFCHAIN) break;
                
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

                if (next != nextSecID)
                    nextSecID = next;
                else
                    throw new CFCorruptedFileException("Cyclic sector chain found. File is corrupted");
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
                    = new StreamView(miniFAT, GetSectorSize(), header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE, sourceStream);

                StreamView miniStreamView =
                    new StreamView(miniStream, GetSectorSize(), rootStorage.Size, sourceStream);

                BinaryReader miniFATReader = new BinaryReader(miniFATView);

                nextSecID = secID;

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
                    nextSecID = miniFATReader.ReadInt32();
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

        internal void InsertNewDirectoryEntry(IDirectoryEntry de)
        {
            // If we are not adding an invalid dirEntry as
            // in a normal loading from file (invalid dirs MAY pad a sector)
            if (de != null)
            {
                // Find first available invalid slot (if any) to reuse it
                for (int i = 0; i < directoryEntries.Count; i++)
                {
                    if (directoryEntries[i].StgType == StgType.StgInvalid)
                    {
                        directoryEntries[i] = de;
                        de.SID = i;
                        return;
                    }
                }
            }

            // No invalid directory entry found
            directoryEntries.Add(de);
            de.SID = directoryEntries.Count - 1;
        }

        /// <summary>
        /// Reset a directory entry setting it to StgInvalid in the Directory.
        /// </summary>
        /// <param name="sid">Sid of the directory to invalidate</param>
        internal void ResetDirectoryEntry(int sid)
        {
            directoryEntries[sid] = new DirectoryEntry(StgType.StgInvalid);
        }


        internal BinarySearchTree<CFItem> GetChildrenTree(int sid)
        {
            BinarySearchTree<CFItem> bst
                = new BinarySearchTree<CFItem>(new CFItemComparer());

            // Load children from their original tree.
            DoLoadChildren(bst, directoryEntries[sid]);

            // Rebuild of (Red)-Black tree of entry children.
            bst.VisitTreeInOrder(RefreshSIDs);

            return bst;
        }

        private void DoLoadChildren(BinarySearchTree<CFItem> bst, IDirectoryEntry de)
        {
            if (de.Child != DirectoryEntry.NOSTREAM )
            {
                if (directoryEntries[de.Child].StgType == StgType.StgInvalid) return;

                if (directoryEntries[de.Child].StgType == StgType.StgStream)
                    bst.Add(new CFStream(this, directoryEntries[de.Child]));
                else
                    bst.Add(new CFStorage(this, directoryEntries[de.Child]));

                LoadSiblings(bst, directoryEntries[de.Child]);
            }
        }

        // Doubling methods allows iterative behavior while avoiding
        // to insert duplicate items
        private void LoadSiblings(BinarySearchTree<CFItem> bst, IDirectoryEntry de)
        {
            if (de.LeftSibling != DirectoryEntry.NOSTREAM)
            {
                // If there're more left siblings load them...
                DoLoadSiblings(bst, directoryEntries[de.LeftSibling]);
            }

            if (de.RightSibling != DirectoryEntry.NOSTREAM)
            {
                // If there're more right siblings load them...
                DoLoadSiblings(bst, directoryEntries[de.RightSibling]);
            }
        }

        private void DoLoadSiblings(BinarySearchTree<CFItem> bst, IDirectoryEntry de)
        {
            if (ValidateSibling(de.LeftSibling))
            {
                // If there're more left siblings load them...
                DoLoadSiblings(bst, directoryEntries[de.LeftSibling]);
            }

            if (directoryEntries[de.SID].StgType == StgType.StgStream)
                bst.Add(new CFStream(this, directoryEntries[de.SID]));
            else if (directoryEntries[de.SID].StgType == StgType.StgStorage)
                bst.Add(new CFStorage(this, directoryEntries[de.SID]));


            if (ValidateSibling(de.RightSibling))
            {
                // If there're more right siblings load them...
                DoLoadSiblings(bst, directoryEntries[de.RightSibling]);
            }
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
                        this.Close();
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
                        this.Close();
                        throw new CFCorruptedFileException("A Directory Entry has a valid reference to an Invalid Storage Type directory");
                    }
                    else
                        return false;
                }

                if (!Enum.IsDefined(typeof(StgType), directoryEntries[sid].StgType))
                {

                    if (this.validationExceptionEnabled)
                    {
                        this.Close();
                        throw new CFCorruptedFileException("A Directory Entry has an invalid Storage Type");
                    }
                    else
                        return false;
                }

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

            if (header.FirstDirectorySectorID == Sector.ENDOFCHAIN)
                header.FirstDirectorySectorID = directoryChain[0].Id;

            StreamView dirReader
                = new StreamView(directoryChain, GetSectorSize(), directoryChain.Count * GetSectorSize(), sourceStream);


            while (dirReader.Position < directoryChain.Count * GetSectorSize())
            {
                DirectoryEntry de
                = new DirectoryEntry(StgType.StgInvalid);

                //We are not inserting dirs. Do not use 'InsertNewDirectoryEntry'
                de.Read(dirReader);
                directoryEntries.Add(de);
                de.SID = directoryEntries.Count - 1;
            }
        }

        internal void RefreshSIDs(BinaryTreeNode<CFItem> Node)
        {
            if (Node.Value != null)
            {
                if (Node.Left != null && (Node.Left.Value.DirEntry.StgType != StgType.StgInvalid))
                {
                    Node.Value.DirEntry.LeftSibling = Node.Left.Value.DirEntry.SID;
                }
                else
                {
                    Node.Value.DirEntry.LeftSibling = DirectoryEntry.NOSTREAM;
                }

                if (Node.Right != null && (Node.Right.Value.DirEntry.StgType != StgType.StgInvalid))
                {
                    Node.Value.DirEntry.RightSibling = Node.Right.Value.DirEntry.SID;
                }
                else
                {
                    Node.Value.DirEntry.RightSibling = DirectoryEntry.NOSTREAM;
                }
            }
        }

        internal void RefreshIterative(BinaryTreeNode<CFItem> node)
        {
            if (node == null) return;
            RefreshSIDs(node);
            RefreshIterative(node.Left);
            RefreshIterative(node.Right);
        }

        /// <summary>
        ///  Commit directory entries change on the Current Source stream
        /// </summary>
        private void CommitDirectory()
        {
            const int DIRECTORY_SIZE = 128;

            List<Sector> directorySectors
                = GetSectorChain(header.FirstDirectorySectorID, SectorType.Normal);

            StreamView sv = new StreamView(directorySectors, GetSectorSize(), 0, sourceStream);

            foreach (IDirectoryEntry di in directoryEntries)
            {
                di.Write(sv);
            }

            int delta = directoryEntries.Count;

            while (delta % (GetSectorSize() / DIRECTORY_SIZE) != 0)
            {
                DirectoryEntry dummy = new DirectoryEntry(StgType.StgInvalid);
                dummy.Write(sv);
                delta++;
            }

            foreach (Sector s in directorySectors)
            {
                s.Type = SectorType.Directory;
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
        /// Destination Stream must be seekable.
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
        /// <returns>A stack of available sectors or minisectors already allocated</returns>
        internal Queue<Sector> FindFreeSectors(SectorType sType)
        {
            Queue<Sector> freeList = new Queue<Sector>();

            if (sType == SectorType.Normal)
            {

                List<Sector> FatChain = GetSectorChain(-1, SectorType.FAT);
                StreamView fatStream = new StreamView(FatChain, GetSectorSize(), header.FATSectorsNumber * GetSectorSize(), sourceStream);

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
                    = new StreamView(miniFAT, GetSectorSize(), header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE, sourceStream);

                List<Sector> miniStream
                    = GetSectorChain(RootEntry.StartSetc, SectorType.Normal);

                StreamView miniStreamView
                    = new StreamView(miniStream, GetSectorSize(), rootStorage.Size, sourceStream);

                long ptr = 0;

                int nMinisectors = (int)(miniStreamView.Length / Sector.MINISECTOR_SIZE);

                while (ptr < nMinisectors)
                {
                    //AssureLength(miniStreamView, (int)miniFATView.Length);

                    int id = miniFATView.ReadInt32();
                    ptr += 4;

                    if (id == Sector.FREESECT)
                    {
                        Sector ms = new Sector(Sector.MINISECTOR_SIZE, sourceStream);
                        byte[] temp = new byte[Sector.MINISECTOR_SIZE];

                        ms.Id = (int)((ptr - 4) / 4);
                        ms.Type = SectorType.Mini;

                        miniStreamView.Seek(ms.Id * Sector.MINISECTOR_SIZE, SeekOrigin.Begin);
                        miniStreamView.Read(ms.GetData(), 0, Sector.MINISECTOR_SIZE);

                        freeList.Enqueue(ms);
                    }
                }
            }

            return freeList;
        }

        internal void SetData(CFItem cfItem, Byte[] buffer)
        {
            SetStreamData(cfItem, buffer);
        }

        /// <summary>
        /// INTERNAL DEVELOPMENT. DO NOT CALL.
        /// <param name="directoryEntry"></param>
        /// <param name="buffer"></param>
        internal void AppendData(CFItem cfItem, Byte[] buffer)
        {
            //CheckFileLength();

            if (buffer == null)
                throw new CFException("Parameter [buffer] cannot be null");

            //Quick and dirty :-)
            if (buffer.Length == 0) return;

            IDirectoryEntry directoryEntry = cfItem.DirEntry;

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


            byte[] tempMini = null;

            if (directoryEntry.StartSetc != Sector.ENDOFCHAIN)
            {
                if ((directoryEntry.Size + buffer.Length) > header.MinSizeStandardStream && directoryEntry.Size < header.MinSizeStandardStream)
                {
                    tempMini = new byte[directoryEntry.Size];

                    StreamView miniData
                        = new StreamView(GetMiniSectorChain(directoryEntry.StartSetc), Sector.MINISECTOR_SIZE, sourceStream);

                    miniData.Read(tempMini, 0, (int)directoryEntry.Size);
                    FreeMiniChain(GetMiniSectorChain(directoryEntry.StartSetc), this.eraseFreeSectors);

                    directoryEntry.StartSetc = Sector.ENDOFCHAIN;
                    directoryEntry.Size = 0;
                }
            }

            List<Sector> sectorChain
                = GetSectorChain(directoryEntry.StartSetc, _st);

            Queue<Sector> freeList = FindFreeSectors(_st); // Collect available free sectors

            StreamView sv = new StreamView(sectorChain, _sectorSize, tempMini != null ? buffer.Length + tempMini.Length : buffer.Length, freeList, sourceStream);

            // If stream was a ministream, copy ministream data
            // in the new stream
            if (tempMini != null)
            {
                sv.Seek(0, SeekOrigin.Begin);
                sv.Write(tempMini, 0, tempMini.Length);
            }
            else
            {
                sv.Seek(directoryEntry.Size, SeekOrigin.Begin);
            }

            // Write appended data
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
                directoryEntry.Size = (long)(buffer.Length) + directoryEntry.Size;

                if (tempMini != null)
                    directoryEntry.Size += tempMini.Length;
            }
            else
            {
                directoryEntry.StartSetc = Sector.ENDOFCHAIN;
                directoryEntry.Size = 0;
            }
        }

        private void SetStreamData(CFItem cfItem, Byte[] buffer)
        {
            //CheckFileLength();

            if (buffer == null)
                throw new CFException("Parameter [buffer] cannot be null");

            //CheckFileLength();

            if (buffer == null)
                throw new CFException("Parameter [buffer] cannot be null");

            //Quick and dirty :-)
            if (buffer.Length == 0) return;

            IDirectoryEntry directoryEntry = cfItem.DirEntry;

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

            Queue<Sector> freeList = null;

            if (this.sectorRecycle)
                freeList = FindFreeSectors(_st); // Collect available free sectors

            StreamView sv = new StreamView(sectorChain, _sectorSize, buffer.Length, freeList, sourceStream);

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
            IDirectoryEntry de = cFStream.DirEntry;

            count = (int)Math.Min((long)(de.Size - offset), (long)count);

            StreamView sView = null;


            if (de.Size < header.MinSizeStandardStream)
            {
                sView
                    = new StreamView(GetSectorChain(de.StartSetc, SectorType.Mini), Sector.MINISECTOR_SIZE, de.Size, sourceStream);
            }
            else
            {

                sView = new StreamView(GetSectorChain(de.StartSetc, SectorType.Normal), GetSectorSize(), de.Size, sourceStream);
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

            IDirectoryEntry de = cFStream.DirEntry;

            //IDirectoryEntry root = directoryEntries[0];

            if (de.Size < header.MinSizeStandardStream)
            {

                StreamView miniView
                    = new StreamView(GetSectorChain(de.StartSetc, SectorType.Mini), Sector.MINISECTOR_SIZE, de.Size, sourceStream);

                BinaryReader br = new BinaryReader(miniView);

                result = br.ReadBytes((int)de.Size);
                br.Close();

            }
            else
            {
                StreamView sView
                    = new StreamView(GetSectorChain(de.StartSetc, SectorType.Normal), GetSectorSize(), de.Size, sourceStream);

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


            Random r = new Random();
            directoryEntries[sid].SetEntryName("_DELETED_NAME_" + r.Next(short.MaxValue).ToString());
            directoryEntries[sid].StgType = StgType.StgInvalid;
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
            ((IDisposable)this).Dispose();
        }

        private bool closeStream = true;

        internal void Close(bool closeStream)
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
#if !FLAT_WRITE
                            this.buffer = null;
#endif
                        }

                        if (this.sourceStream != null && closeStream)
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

            using (CompoundFile tempCF = new CompoundFile((CFSVersion)cf.header.MajorVersion, cf.sectorRecycle, cf.eraseFreeSectors))
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
                        strg.CLSID = new Guid(itemAsStorage.CLSID.ToByteArray());
                        DoCompression(itemAsStorage, strg); // recursion, one level deeper
                    }
                };

            currSrcStorage.VisitEntries(va, false);
        }
    }
}