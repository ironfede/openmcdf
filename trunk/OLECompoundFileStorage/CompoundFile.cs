using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using BinaryTrees;
using System.Collections;

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

namespace OLECompoundFileStorage
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
    /// Standard Microsoft&#169; Compound File implementation.
    /// It is also known as OLE/COM structured storage, version 3.
    /// Contains a hierarchy of storage and stream objects allowing
    /// efficent storage of multiple kinds of documents in a single file.
    /// </summary>
    public class CompoundFile : IDisposable
    {
        /// <summary>
        /// Number of DIFAT entries in the header
        /// </summary>
        private const int HEADER_DIFAT_ENTRIES_COUNT = 109;

        /// <summary>
        /// Number of FAT entries in a DIFAT Sector
        /// </summary>
        private const int DIFAT_SECTOR_FAT_ENTRIES_COUNT = 127;

        /// <summary>
        /// Sectors ID entries in a FAT Sector
        /// </summary>
        private const int FAT_SECTOR_ENTRIES_COUNT = 128;

        /// <summary>
        /// Sector ID Size (int)
        /// </summary>
        private const int SIZE_OF_SID = 4;

        private BinaryReader fileReader = null;

        private ArrayList sectors = new ArrayList();

        private Header header;

        private int nDirSectors = 0;


        /// <summary>
        /// Create a new, blank, compound file.
        /// </summary>
        public CompoundFile()
        {
            this.header = new Header();
            //this.directory = new Directory();

            //Root -- 
            rootStorage = new CFStorage(this);

            rootStorage.SetEntryName("Root Entry");
            rootStorage.StgType = StgType.STGTY_ROOT;
            rootStorage.StgColor = StgColor.BLACK;

            this.AddDirectoryEntry(rootStorage);
        }

        /// <summary>
        /// Load an existing compound file
        /// </summary>
        /// <param name="fileName">Compound file to read from</param>
        public CompoundFile(String fileName)
        {
            LoadFile(fileName);
        }

        private void LoadFile(String fileName)
        {
            this.header = new Header();
            //this.directory = new Directory();

            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            fileReader = new BinaryReader(fs);

            header.Read(fileReader);

            int n_sector = Ceiling((double)((fs.Length - Sector.SECTOR_SIZE) / Sector.SECTOR_SIZE));

            sectors = new ArrayList(n_sector);

            for (int i = 0; i < n_sector; i++)
            {
                sectors.Add(null);
            }

            LoadDirectories();

            this.rootStorage
                = new CFStorage(this, directoryEntries[0]);
        }

        public bool IsFileMapped
        {
            get { return fileReader != null; }
        }

        //internal void SetSectorChain(List<Sector> sectorChain, SectorType chainType)
        //{
        //    List<Sector> result
        //        = new List<Sector>();

        //    switch (chainType)
        //    {
        //        case SectorType.DIFAT:

        //            SetDIFATSectorChain(sectorChain);

        //            break;

        //        case SectorType.FAT:

        //            SetFATSectorChain(sectorChain);

        //            break;

        //        case SectorType.Normal:

        //            SetNormalSectorChain(sectorChain);

        //            break;

        //        case SectorType.Mini:

        //            SetMiniSectorChain(sectorChain);

        //            break;
        //    }

        //}

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
                = new StreamView(miniFAT, Sector.SECTOR_SIZE, header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE);

            StreamView miniStreamView
                = new StreamView(miniStream, Sector.SECTOR_SIZE, this.rootStorage.Size);

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

        private void FreeMiniChain(List<Sector> sectorChain)
        {

            byte[] ZEROED_MINI_SECTOR = new byte[Sector.MINISECTOR_SIZE];

            List<Sector> miniFAT
                = GetSectorChain(header.FirstMiniFATSectorID, SectorType.Normal);

            List<Sector> miniStream
                = GetSectorChain(RootEntry.StartSetc, SectorType.Normal);

            StreamView miniFATView
                = new StreamView(miniFAT, Sector.SECTOR_SIZE, header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE);

            StreamView miniStreamView
                = new StreamView(miniStream, Sector.SECTOR_SIZE, this.rootStorage.Size);

            // Set updated/new sectors within the ministream
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
                    sectors.Add(s);
                    s.Id = sectors.Count - 1;
                }
            }

            SetFATSectorChain(sectorChain);
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
                    Sector.SECTOR_SIZE,
                    header.FATSectorsNumber * Sector.SECTOR_SIZE
                    );

            AssureLength(fatStream, (sectorChain.Count * 4));

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

            SetDIFATSectorChain(fatStream.BaseSectorChain);

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
                Sector extraFATSector = new Sector();
                sectors.Add(extraFATSector);

                extraFATSector.Id = sectors.Count - 1;
                extraFATSector.Type = SectorType.FAT;

                FATsectorChain.Add(extraFATSector);

                header.FATSectorsNumber++;
                nCurrentSectors++;

                //... now, adding a FAT sector may induce DIFAT sectors to increase by one
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
                = new StreamView(difatSectors, Sector.SECTOR_SIZE);

            AssureLength(difatStream, nDIFATSectors * Sector.SECTOR_SIZE);

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
                    // room for DIFAT chaining at the end of any DIFAT sector (4 bytes
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
                        Sector.SECTOR_SIZE - sizeof(int),
                        4);
                }

                Buffer.BlockCopy(
                    BitConverter.GetBytes(Sector.ENDOFCHAIN),
                    0,
                    difatStream.BaseSectorChain[difatStream.BaseSectorChain.Count - 1].Data,
                    Sector.SECTOR_SIZE - sizeof(int),
                    sizeof(int)
                    );
            }
            else
                header.FirstDIFATSectorID = Sector.ENDOFCHAIN;

            // Mark DIFAT Sectors in FAT
            StreamView fatSv =
                new StreamView(FATsectorChain, Sector.SECTOR_SIZE, header.FATSectorsNumber * Sector.SECTOR_SIZE);


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
        private void AssureLength(StreamView streamView, int length)
        {
            if (streamView != null && streamView.Length < length)
            {
                streamView.SetLength(length);
            }
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
            List<Sector> result
                = new List<Sector>();

            int nextSecID
                = Sector.ENDOFCHAIN;

            nextSecID
                = secID;

            switch (chainType)
            {
                case SectorType.DIFAT:

                    if (header.DIFATSectorsNumber != 0)
                    {
                        Sector s = sectors[header.FirstDIFATSectorID] as Sector;

                        if (s == null)
                        {
                            s = Sector.LoadSector(header.FirstDIFATSectorID, fileReader, Sector.SECTOR_SIZE);
                            s.Type = SectorType.DIFAT;
                            s.Id = header.FirstDIFATSectorID;
                            header.FirstDIFATSectorID = s.Id;
                            sectors[header.FirstDIFATSectorID] = s;
                        }

                        result.Add(s);

                        while (true)
                        {
                            nextSecID = BitConverter.ToInt32(s.Data, 508);

                            // Strictly the following condition is not correct:
                            // only ENDOFCHAIN should break DIFAT chain but 
                            // a lot of compound files use FREESECT as DIFAT chain termination
                            if (nextSecID == Sector.FREESECT || nextSecID == Sector.ENDOFCHAIN) break;

                            if (sectors[nextSecID] == null)
                            {
                                sectors[nextSecID] = Sector.LoadSector(
                                    nextSecID,
                                    fileReader,
                                   Sector.SECTOR_SIZE
                                );
                            }

                            s = sectors[nextSecID] as Sector;

                            result.Add(s);
                        }
                    }

                    return result;

                    break;

                case SectorType.FAT:

                    List<Sector> difatSectors = GetSectorChain(-1, SectorType.DIFAT);

                    int c = 0;

                    while (c < 109 && header.DIFAT[c] != Sector.FREESECT)
                    {
                        nextSecID = header.DIFAT[c];

                        if (sectors[nextSecID] == null && fileReader != null)
                        {
                            sectors[nextSecID] = Sector.LoadSector(
                                   nextSecID,
                                   fileReader,
                                   Sector.SECTOR_SIZE);
                        }

                        result.Add(sectors[nextSecID] as Sector);

                        c++;
                    }

                    if (difatSectors.Count > 0)
                    {
                        StreamView difatStream
                            = new StreamView
                                (
                                difatSectors,
                                Sector.SECTOR_SIZE,
                                header.FATSectorsNumber > 109 ? (header.FATSectorsNumber - 109) * 4 : 0
                                );

                        byte[] buffer = new byte[4];

                        difatStream.Read(buffer, 0, 4);
                        nextSecID = BitConverter.ToInt32(buffer, 0);

                        int i = 0;
                        int nFat = 109;

                        while (nFat < header.FATSectorsNumber)
                        {
                            if (difatStream.Position == (508 + i * Sector.SECTOR_SIZE))
                            {
                                difatStream.Seek(4, SeekOrigin.Current);
                                i++;
                                continue;
                            }


                            if (sectors[nextSecID] == null && fileReader != null)
                            {
                                sectors[nextSecID] = Sector.LoadSector(
                                       nextSecID,
                                       fileReader,
                                       Sector.SECTOR_SIZE);
                            }

                            result.Add(sectors[nextSecID] as Sector);

                            difatStream.Read(buffer, 0, 4);
                            nextSecID = BitConverter.ToInt32(buffer, 0);
                            nFat++;
                        }
                    }


                    break;

                case SectorType.Normal:

                    List<Sector> fatSectors
                        = GetSectorChain(-1, SectorType.FAT);

                    StreamView fatStream = new StreamView(fatSectors, Sector.SECTOR_SIZE, fatSectors.Count * Sector.SECTOR_SIZE);
                    BinaryReader fatReader = new BinaryReader(fatStream);

                    while (true)
                    {
                        if (nextSecID == Sector.ENDOFCHAIN) break;

                        if (sectors[nextSecID] == null && fileReader != null)
                        {
                            sectors[nextSecID] = Sector.LoadSector(nextSecID, fileReader, Sector.SECTOR_SIZE);
                        }

                        result.Add(sectors[nextSecID] as Sector);

                        fatReader.BaseStream.Position = nextSecID * 4;
                        nextSecID = fatReader.ReadInt32();
                    }

                    fatReader.Close();

                    break;

                case SectorType.Mini:

                    List<Sector> miniFAT = GetSectorChain(header.FirstMiniFATSectorID, SectorType.Normal);
                    List<Sector> miniStream = GetSectorChain(RootEntry.StartSetc, SectorType.Normal);

                    StreamView miniFATView = new StreamView(miniFAT, Sector.SECTOR_SIZE, header.MiniFATSectorsNumber * Sector.MINISECTOR_SIZE);
                    StreamView miniStreamView = new StreamView(miniStream, Sector.SECTOR_SIZE, rootStorage.Size);

                    List<Sector> miniSectorChain = new List<Sector>();

                    //int secOffset = secID / (Sector.SECTOR_SIZE / Sector.MINISECTOR_SIZE);

                    ////if (secOffset > miniFAT.Count)
                    ////{
                    ////    return result;
                    ////}

                    //BinaryReader miniReader = new BinaryReader(miniStreamView);
                    BinaryReader miniFATReader = new BinaryReader(miniFATView);

                    nextSecID = secID;

                    while (true)
                    {
                        if (nextSecID == Sector.ENDOFCHAIN)
                            break;

                        Sector ms = new Sector();
                        byte[] temp = new byte[Sector.MINISECTOR_SIZE];

                        ms.Id = nextSecID;
                        miniStreamView.Seek(nextSecID * Sector.MINISECTOR_SIZE, SeekOrigin.Begin);

                        miniStreamView.Read(temp, 0, Sector.MINISECTOR_SIZE);
                        ms.Data = temp;

                        result.Add(ms);

                        miniFATView.Seek(nextSecID * 4, SeekOrigin.Begin);
                        nextSecID = miniFATReader.ReadInt32();
                    }

                    break;
            }

            return result;
        }

        private IDirectoryEntry rootStorage;

        public CFStorage RootStorage
        {
            get
            {
                return rootStorage as CFStorage;
            }
        }

        internal void AddDirectoryEntry(IDirectoryEntry de)
        {
            directoryEntries.Add(de);
            de.SID = directoryEntries.Count - 1;

        }

        internal void RefreshSIDs(BinaryTreeNode<IDirectoryEntry> Node)
        {
            if (Node.Value != null)
            {
                if (Node.Left != null)
                {
                    Node.Value.LeftSibling = Node.Left.Value.SID;
                }
                else
                {
                    Node.Value.LeftSibling = DirectoryEntry.NOSTREAM;
                }

                if (Node.Right != null)
                {
                    Node.Value.RightSibling = Node.Right.Value.SID;
                }
                else
                {
                    Node.Value.RightSibling = DirectoryEntry.NOSTREAM;
                }
            }
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
                if (directoryEntries[de.Child].StgType == StgType.STGTY_STREAM)
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

            if (directoryEntries[de.SID].StgType == StgType.STGTY_STREAM)
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
                = new BinaryReader(new StreamView(directoryChain, Sector.SECTOR_SIZE, directoryChain.Count * Sector.SECTOR_SIZE));

            DirectoryEntry de
                = new DirectoryEntry(StgType.STGTY_INVALID);

            de.Read(dirReader);

            while (de.StgType != StgType.STGTY_INVALID)
            {
                this.AddDirectoryEntry(de);

                de = new DirectoryEntry(StgType.STGTY_INVALID);
                de.Read(dirReader);
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
            List<Sector> directorySectors
                = GetSectorChain(header.FirstDirectorySectorID, SectorType.Normal);

            StreamView sv = new StreamView(directorySectors, 512, 0);
            BinaryWriter bw = new BinaryWriter(sv);

            ((CFStorage)rootStorage).Children.VisitTreeInOrder(new NodeAction<IDirectoryEntry>(RefreshIterative));

            foreach (IDirectoryEntry di in directoryEntries)
            {
                di.Write(bw);
            }

            int delta = directoryEntries.Count;

            while (delta % 4 != 0)
            {
                DirectoryEntry dummy = new DirectoryEntry(StgType.STGTY_INVALID);
                dummy.Write(bw);
                delta++;
            }

            SetNormalSectorChain(directorySectors);

            header.FirstDirectorySectorID = directorySectors[0].Id;
            bw.Flush();
            bw.Close();
        }

        public void Save(String fileName)
        {
            if (_disposed)
                throw new CFSException("Compound File closed: cannot save data");


            FileStream fs = new FileStream(fileName, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);

            bw.Write((byte[])Array.CreateInstance(typeof(byte), 512));
            SaveDirectory();

            if (fileReader != null)
                fileReader.BaseStream.Seek(0, SeekOrigin.Begin);

            for (int i = 0; i < sectors.Count; i++)
            {
                Sector s = sectors[i] as Sector;

                if (fileReader != null && s == null)
                {
                    s = Sector.LoadSector(i, fileReader, Sector.SECTOR_SIZE);
                    sectors[i] = s;
                }

                bw.Write(s.Data);
            }

            bw.BaseStream.Seek(0, SeekOrigin.Begin);
            header.Write(bw);

            fs.Flush();
            bw.Flush();

            fs.Close();
            bw.Close();
        }

        public void Save(Stream stream)
        {
            if (_disposed)
                throw new CFSException("Compound File closed: cannot save data");

            Stream tempStream = null;

            if (!stream.CanSeek)
                tempStream = new MemoryStream();
            else
                tempStream = stream;

            BinaryWriter bw = new BinaryWriter(tempStream);

            bw.Write((byte[])Array.CreateInstance(typeof(byte), 512));
            SaveDirectory();

            if (fileReader != null)
                fileReader.BaseStream.Seek(0, SeekOrigin.Begin);

            for (int i = 0; i < sectors.Count; i++)
            {
                Sector s = sectors[i] as Sector;


                if (fileReader != null && s == null)
                {

                    s = Sector.LoadSector(i, fileReader, Sector.SECTOR_SIZE);
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

        internal void SetData(IDirectoryEntry directoryEntry, Byte[] buffer)
        {
            SetStreamData(directoryEntry, buffer);
        }

        private void SetStreamData(IDirectoryEntry directoryEntry, Byte[] buffer)
        {
            SectorType _st = SectorType.Normal;
            int _sectorSize = Sector.SECTOR_SIZE;

            if (buffer.Length < header.MinSizeStandardStream)
            {
                _st = SectorType.Mini;
                _sectorSize = Sector.MINISECTOR_SIZE;
            }

            ///Check for transition ministream -> stream
            if (directoryEntry.StartSetc != Sector.ENDOFCHAIN)
            {
                if (
                    (buffer.Length < header.MinSizeStandardStream && directoryEntry.Size > header.MinSizeStandardStream)
                    || (buffer.Length > header.MinSizeStandardStream && directoryEntry.Size < header.MinSizeStandardStream)
                   )
                {
                    // TODO: Add sector reuse here; fututre release must provide cleanup of stream resources and avoid wasting space...
                    directoryEntry.Size = 0;
                    directoryEntry.StartSetc = Sector.ENDOFCHAIN;
                }
            }

            List<Sector> sectorChain
                = GetSectorChain(directoryEntry.StartSetc, _st);

            StreamView sv = new StreamView(sectorChain, _sectorSize, buffer.Length);
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


        internal byte[] GetData(CFStream cFStream)
        {

            if (_disposed)
                throw new CFSException("Compound File closed: cannot access data");

            byte[] result = null;

            IDirectoryEntry de = cFStream as IDirectoryEntry;

            IDirectoryEntry root = directoryEntries[0];

            if (de.Size <= header.MinSizeStandardStream)
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
                    = new StreamView(GetSectorChain(de.StartSetc, SectorType.Normal), Sector.SECTOR_SIZE, de.Size);

                result = new byte[(int)de.Size];

                sView.Read(result, 0, result.Length);

            }

            return result;
        }

        private int Ceiling(double d)
        {
            return (int)Math.Ceiling(d);
        }

        private int LowSaturation(int i)
        {
            return i > 0 ? i : 0;
        }

        private int FindMaxID(List<Sector> sectorChain)
        {
            int temp = Sector.ENDOFCHAIN;

            foreach (Sector s in sectorChain)
            {
                if (s.Id > temp)
                    temp = s.Id;
            }

            return temp;
        }


        public void Close()
        {
            ((IDisposable)this).Dispose();
        }

        internal void RemoveDirectoryEntry(int sid)
        {
            if (sid >= directoryEntries.Count)
                throw new CFSException("Invalid SID of the directory entry to remove");

            // Clear the associated stream (or ministream)
            if (directoryEntries[sid].Size < 4096)
            {
                List<Sector> miniChain
                    = GetSectorChain(directoryEntries[sid].StartSetc, SectorType.Mini);
                FreeMiniChain(miniChain);
            }

            // Update the SIDs of the entries following the (tobe)removed one.
            // This update will NOT invalidate sorting 
            for (int i = 0; i < directoryEntries.Count; i++)
            {
                if (directoryEntries[i].SID > sid)
                    directoryEntries[i].SID--;

                if (directoryEntries[i].Child > sid)
                    directoryEntries[i].Child--;

                if (directoryEntries[i].LeftSibling > sid)
                    directoryEntries[i].LeftSibling--;

                if (directoryEntries[i].RightSibling > sid)
                    directoryEntries[i].RightSibling--;
            }

            // Remove phisically the entry
            this.directoryEntries.RemoveAt(sid);
        }

        #region IDisposable Members

        private bool _disposed = false;

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
        protected void Dispose(bool disposing)
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

                        }

                        if (this.fileReader != null)
                        {
                            this.fileReader.BaseStream.Flush();
                            fileReader.Close();
                            fileReader = null;
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


        //internal void RemoveDirectoryEntry(int index)
        //{
        //    if (index < directoryEntries.Count)
        //    {
        //        for (int i = index + 1; i < directoryEntries.Count; i++)
        //        {
        //            directoryEntries[i].SID--;
        //        }

        //        this.directoryEntries.RemoveAt(index);
        //    }
        //}

        public void Write(System.IO.BinaryWriter bw)
        {
            int dirSectNumber = 0;

            foreach (DirectoryEntry dirEntry in directoryEntries)
            {
                dirEntry.Write(bw);
                dirSectNumber++;
            }

            // Padding with FREESECT
            while (dirSectNumber++ % 512 != 0)
            {
                for (int i = 0; i < 31; i++)
                    bw.Write(Sector.FREESECT);

                dirSectNumber++;
            }
        }
    }
}