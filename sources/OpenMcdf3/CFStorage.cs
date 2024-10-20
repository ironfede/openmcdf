/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 *
 * The Original Code is OpenMCDF - Compound Document Format library.
 *
 * The Initial Developer of the Original Code is Federico Blaseotto.*/

using RedBlackTree;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenMcdf3
{
    /// <summary>
    /// Action to apply to  visited items in the OLE structured storage
    /// </summary>
    /// <param name="item">Currently visited <see cref="T:OpenMcdf.CFItem">item</see></param>
    /// <example>
    /// <code>
    ///
    /// //We assume that xls file should be a valid OLE compound file
    /// const String STORAGE_NAME = "report.xls";
    /// CompoundFile cf = new CompoundFile(STORAGE_NAME);
    ///
    /// FileStream output = new FileStream("LogEntries.txt", FileMode.Create);
    /// TextWriter tw = new StreamWriter(output);
    ///
    /// VisitedEntryAction va = delegate(CFItem item)
    /// {
    ///     tw.WriteLine(item.Name);
    /// };
    ///
    /// cf.RootStorage.VisitEntries(va, true);
    ///
    /// tw.Close();
    ///
    /// </code>
    /// </example>
    public delegate void VisitedEntryAction(CFItem item);

    /// <summary>
    /// Storage entity that acts like a logic container for streams
    /// or substorages in a compound file.
    /// </summary>
    public class CFStorage : CFItem
    {
        private RBTree children;

        internal RBTree Children
        {
            get
            {
                // Lazy loading of children tree.
                children ??= LoadChildren(DirEntry);
                return children;
            }
        }

        /// <summary>
        /// Create a CFStorage using an existing directory (previously loaded).
        /// </summary>
        /// <param name="compFile">The Storage Owner - CompoundFile</param>
        /// <param name="dirEntry">An existing Directory Entry</param>
        internal CFStorage(CompoundFile compFile, IDirectoryEntry dirEntry)
            : base(compFile)
        {
            if (dirEntry == null || dirEntry.SID < 0)
                throw new CFException("Attempting to create a CFStorage using an uninitialized directory");

            DirEntry = dirEntry;
        }

        private RBTree LoadChildren(IDirectoryEntry directoryEntry)
        {
            RBTree childrenTree = CompoundFile.GetChildrenTree(directoryEntry);
            DirEntry.Child = childrenTree.Root == null ? DirectoryEntry.NOSTREAM : ((IDirectoryEntry)childrenTree.Root).SID;
            return childrenTree;
        }

        /// <summary>
        /// Create a new child stream inside the current <see cref="T:OpenMcdf.CFStorage">storage</see>
        /// </summary>
        /// <param name="streamName">The new stream name</param>
        /// <returns>The new <see cref="T:OpenMcdf.CFStream">stream</see> reference</returns>
        /// <exception cref="T:OpenMcdf.CFDuplicatedItemException">Raised when adding an item with the same name of an existing one</exception>
        /// <exception cref="T:OpenMcdf.CFDisposedException">Raised when adding a stream to a closed compound file</exception>
        /// <exception cref="T:OpenMcdf.CFException">Raised when adding a stream with null or empty name</exception>
        /// <example>
        /// <code>
        ///
        ///  String filename = "A_NEW_COMPOUND_FILE_YOU_CAN_WRITE_TO.cfs";
        ///
        ///  CompoundFile cf = new CompoundFile();
        ///
        ///  CFStorage st = cf.RootStorage.AddStorage("MyStorage");
        ///  CFStream sm = st.AddStream("MyStream");
        ///  byte[] b = Helpers.GetBuffer(220, 0x0A);
        ///  sm.SetData(b);
        ///
        ///  cf.Save(filename);
        ///
        /// </code>
        /// </example>
        public CFStream AddStream(string streamName)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(streamName))
                throw new CFException("Stream name cannot be null or empty");

            IDirectoryEntry dirEntry = DirectoryEntry.TryNew(streamName, StgType.StgStream, CompoundFile.Directories);

            // Add new Stream directory entry
            //cfo = new CFStream(this.CompoundFile, streamName);

            try
            {
                // Add object to Siblings tree
                Children.Insert(dirEntry);

                //... and set the root of the tree as new child of the current item directory entry
                DirEntry.Child = (Children.Root as IDirectoryEntry).SID;
            }
            catch (RBTreeException)
            {
                dirEntry.Reset();

                throw new CFDuplicatedItemException("An entry with name '" + streamName + "' is already present in storage '" + Name + "' ");
            }

            return new CFStream(CompoundFile, dirEntry);
        }

        bool Contains(string name, StgType type, out IDirectoryEntry directoryEntry)
        {
            IDirectoryEntry tmp = DirectoryEntry.Mock(name, type);
            if (!Children.TryLookup(tmp, out IRBNode node) || node is not IDirectoryEntry de || de.StgType != type)
            {
                directoryEntry = null;
                return false;
            }

            directoryEntry = de;
            return true;
        }

        public bool ContainsStream(string streamName) => Contains(streamName, StgType.StgStream, out _);

        public bool ContainsStorage(string storageName) => Contains(storageName, StgType.StgStorage, out _);

        /// <summary>
        /// Get a named <see cref="T:OpenMcdf.CFStream">stream</see> contained in the current storage if existing.
        /// </summary>
        /// <param name="streamName">Name of the stream to look for</param>
        /// <returns>A stream reference if existing</returns>
        /// <exception cref="T:OpenMcdf.CFDisposedException">Raised if trying to delete item from a closed compound file</exception>
        /// <exception cref="T:OpenMcdf.CFItemNotFound">Raised if item to delete is not found</exception>
        /// <example>
        /// <code>
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
        public CFStream GetStream(string streamName)
        {
            CheckDisposed();

            if (!Contains(streamName, StgType.StgStream, out IDirectoryEntry outDe))
                throw new CFItemNotFound($"Cannot find item [{streamName}] within the current storage");

            return new CFStream(CompoundFile, outDe);
        }

        /// <summary>
        /// Get a named <see cref="T:OpenMcdf.CFStream">stream</see> contained in the current storage if existing.
        /// </summary>
        /// <param name="streamName">Name of the stream to look for</param>
        /// <param name="cfStream">Found <see cref="T:OpenMcdf.CFStream"> if any</param>
        /// <returns><see cref="T:System.Boolean"> true if stream found, else false</returns>
        /// <example>
        /// <code>
        /// String filename = "report.xls";
        ///
        /// CompoundFile cf = new CompoundFile(filename);
        /// bool b = cf.RootStorage.TryGetStream("Workbook",out CFStream foundStream);
        ///
        /// byte[] temp = foundStream.GetData();
        ///
        /// Assert.IsNotNull(temp);
        /// Assert.IsTrue(b);
        ///
        /// cf.Close();
        /// </code>
        /// </example>
        public bool TryGetStream(string streamName, out CFStream cfStream)
        {
            if (CompoundFile.IsClosed || !Contains(streamName, StgType.StgStream, out IDirectoryEntry directoryEntry))
            {
                cfStream = null;
                return false;
            }

            cfStream = new CFStream(CompoundFile, directoryEntry);
            return true;
        }

        /// <summary>
        /// Get a named <see cref="T:OpenMcdf.CFStream">stream</see> contained in the current storage if existing.
        /// </summary>
        /// <param name="streamName">Name of the stream to look for</param>
        /// <returns>A stream reference if found, else null</returns>
        /// <exception cref="T:OpenMcdf.CFDisposedException">Raised if trying to delete item from a closed compound file</exception>
        /// <example>
        /// <code>
        /// String filename = "report.xls";
        ///
        /// CompoundFile cf = new CompoundFile(filename);
        /// CFStream foundStream = cf.RootStorage.TryGetStream("Workbook");
        ///
        /// byte[] temp = foundStream.GetData();
        ///
        /// Assert.IsNotNull(temp);
        ///
        /// cf.Close();
        /// </code>
        /// </example>
        [Obsolete("Please use TryGetStream(string, out cfStream) instead.")]
        public CFStream TryGetStream(string streamName)
        {
            TryGetStream(streamName, out CFStream cfStream);
            return cfStream;
        }

        /// <summary>
        /// Get a named storage contained in the current one if existing.
        /// </summary>
        /// <param name="storageName">Name of the storage to look for</param>
        /// <returns>A storage reference if existing.</returns>
        /// <exception cref="T:OpenMcdf.CFDisposedException">Raised if trying to delete item from a closed compound file</exception>
        /// <exception cref="T:OpenMcdf.CFItemNotFound">Raised if item to delete is not found</exception>
        /// <example>
        /// <code>
        ///
        /// String FILENAME = "MultipleStorage2.cfs";
        /// CompoundFile cf = new CompoundFile(FILENAME, UpdateMode.ReadOnly, false, false);
        ///
        /// CFStorage st = cf.RootStorage.GetStorage("MyStorage");
        ///
        /// Assert.IsNotNull(st);
        /// cf.Close();
        /// </code>
        /// </example>
        public CFStorage GetStorage(string storageName)
        {
            CheckDisposed();

            if (!Contains(storageName, StgType.StgStorage, out IDirectoryEntry outDe))
                throw new CFItemNotFound($"Cannot find item [{storageName}] within the current storage");

            return new CFStorage(CompoundFile, outDe);
        }

        /// <summary>
        /// Get a named storage contained in the current one if existing.
        /// </summary>
        /// <param name="storageName">Name of the storage to look for</param>
        /// <returns>A storage reference if found else null</returns>
        /// <exception cref="T:OpenMcdf.CFDisposedException">Raised if trying to delete item from a closed compound file</exception>
        /// <example>
        /// <code>
        ///
        /// String FILENAME = "MultipleStorage2.cfs";
        /// CompoundFile cf = new CompoundFile(FILENAME, UpdateMode.ReadOnly, false, false);
        ///
        /// CFStorage st = cf.RootStorage.TryGetStorage("MyStorage");
        ///
        /// Assert.IsNotNull(st);
        /// cf.Close();
        /// </code>
        /// </example>
        [Obsolete("Please use TryGetStorage(string, out cfStorage) instead.")]
        public CFStorage TryGetStorage(string storageName)
        {
            TryGetStorage(storageName, out CFStorage cfStorage);
            return cfStorage;
        }

        /// <summary>
        /// Get a named storage contained in the current one if existing.
        /// </summary>
        /// <param name="storageName">Name of the storage to look for</param>
        /// <param name="cfStorage">A storage reference if found else null</param>
        /// <returns><see cref="T:System.Boolean"> true if storage found, else false</returns>
        /// <example>
        /// <code>
        ///
        /// String FILENAME = "MultipleStorage2.cfs";
        /// CompoundFile cf = new CompoundFile(FILENAME, UpdateMode.ReadOnly, false, false);
        ///
        /// bool b = cf.RootStorage.TryGetStorage("MyStorage",out CFStorage st);
        ///
        /// Assert.IsNotNull(st);
        /// Assert.IsTrue(b);
        ///
        /// cf.Close();
        /// </code>
        /// </example>
        public bool TryGetStorage(string storageName, out CFStorage cfStorage)
        {
            if (CompoundFile.IsClosed || !Contains(storageName, StgType.StgStorage, out IDirectoryEntry directoryEntry))
            {
                cfStorage = null;
                return false;
            }

            cfStorage = new CFStorage(CompoundFile, directoryEntry);
            return true;
        }

        /// <summary>
        /// Create new child storage directory inside the current storage.
        /// </summary>
        /// <param name="storageName">The new storage name</param>
        /// <returns>Reference to the new <see cref="T:OpenMcdf.CFStorage">storage</see></returns>
        /// <exception cref="T:OpenMcdf.CFDuplicatedItemException">Raised when adding an item with the same name of an existing one</exception>
        /// <exception cref="T:OpenMcdf.CFDisposedException">Raised when adding a storage to a closed compound file</exception>
        /// <exception cref="T:OpenMcdf.CFException">Raised when adding a storage with null or empty name</exception>
        /// <example>
        /// <code>
        ///
        ///  String filename = "A_NEW_COMPOUND_FILE_YOU_CAN_WRITE_TO.cfs";
        ///
        ///  CompoundFile cf = new CompoundFile();
        ///
        ///  CFStorage st = cf.RootStorage.AddStorage("MyStorage");
        ///  CFStream sm = st.AddStream("MyStream");
        ///  byte[] b = Helpers.GetBuffer(220, 0x0A);
        ///  sm.SetData(b);
        ///
        ///  cf.Save(filename);
        ///
        /// </code>
        /// </example>
        public CFStorage AddStorage(string storageName)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(storageName))
                throw new CFException("Stream name cannot be null or empty");

            // Add new Storage directory entry
            IDirectoryEntry cfo
                = DirectoryEntry.New(storageName, StgType.StgStorage, CompoundFile.Directories);

            //this.CompoundFile.InsertNewDirectoryEntry(cfo);

            try
            {
                // Add object to Siblings tree
                Children.Insert(cfo);
            }
            catch (RBTreeDuplicatedItemException)
            {
                cfo.Reset();
                throw new CFDuplicatedItemException("An entry with name '" + storageName + "' is already present in storage '" + Name + "' ");
            }

            IDirectoryEntry childrenRoot = Children.Root as IDirectoryEntry;
            DirEntry.Child = childrenRoot.SID;

            return new CFStorage(CompoundFile, cfo);
        }

        /// <summary>
        /// Visit all entities contained in the storage applying a user provided action
        /// </summary>
        /// <exception cref="T:OpenMcdf.CFDisposedException">Raised when visiting items of a closed compound file</exception>
        /// <param name="action">User <see cref="T:OpenMcdf.VisitedEntryAction">action</see> to apply to visited entities</param>
        /// <param name="recursive"> Visiting recursion level. True means substorages are visited recursively, false indicates that only the direct children of this storage are visited</param>
        /// <example>
        /// <code>
        /// const String STORAGE_NAME = "report.xls";
        /// CompoundFile cf = new CompoundFile(STORAGE_NAME);
        ///
        /// FileStream output = new FileStream("LogEntries.txt", FileMode.Create);
        /// TextWriter tw = new StreamWriter(output);
        ///
        /// VisitedEntryAction va = delegate(CFItem item)
        /// {
        ///     tw.WriteLine(item.Name);
        /// };
        ///
        /// cf.RootStorage.VisitEntries(va, true);
        ///
        /// tw.Close();
        /// </code>
        /// </example>
        public void VisitEntries(Action<CFItem> action, bool recursive)
        {
            CheckDisposed();

            if (action is null)
                return; // TODO: Reorder and throw ArgumentNullException in v3

            Stack<CFItem> stack = new();
            stack.Push(this);

            while (stack.Count > 0)
            {
                CFItem current = stack.Pop();
                if (current is CFStorage storage)
                {
                    foreach (IDirectoryEntry de in storage.Children.Cast<IDirectoryEntry>())
                    {
                        CFItem item = de.StgType == StgType.StgStream ? new CFStream(CompoundFile, de) : new CFStorage(CompoundFile, de);
                        action(item);
                        if (recursive)
                            stack.Push(item);
                    }
                }
            }
        }

        /// <summary>
        /// Remove an entry from the current storage and compound file.
        /// </summary>
        /// <param name="entryName">The name of the entry in the current storage to delete</param>
        /// <example>
        /// <code>
        /// cf = new CompoundFile("A_FILE_YOU_CAN_CHANGE.cfs", UpdateMode.Update, true, false);
        /// cf.RootStorage.Delete("AStream"); // AStream item is assumed to exist.
        /// cf.Commit(true);
        /// cf.Close();
        /// </code>
        /// </example>
        /// <exception cref="T:OpenMcdf.CFDisposedException">Raised if trying to delete item from a closed compound file</exception>
        /// <exception cref="T:OpenMcdf.CFItemNotFound">Raised if item to delete is not found</exception>
        /// <exception cref="T:OpenMcdf.CFException">Raised if trying to delete root storage</exception>
        public void Delete(string entryName) => DeleteCore(entryName, true);

        public void TryDelete(string entryName) => DeleteCore(entryName, false);

        bool DeleteCore(string entryName, bool throwOnError)
        {
            if (CompoundFile.IsClosed)
                return throwOnError ? throw new CFDisposedException("Owner Compound file has been closed and owned items have been invalidated") : false;

            // Find entry to delete
            IDirectoryEntry tmp = DirectoryEntry.Mock(entryName, StgType.StgInvalid);
            Children.TryLookup(tmp, out IRBNode foundObj);

            if (foundObj == null)
                return throwOnError ? throw new CFItemNotFound($"Entry named [{entryName}] was not found") : false;

            IDirectoryEntry directoryEntry = (IDirectoryEntry)foundObj;
            DeleteCore(directoryEntry);
            return true;
        }

        void DeleteCore(IDirectoryEntry directoryEntry)
        {
            IRBNode altDel;
            switch (directoryEntry.StgType)
            {
                case StgType.StgRoot:
                    throw new CFException("Root storage cannot be removed");

                case StgType.StgStorage:

                    CFStorage temp = new CFStorage(CompoundFile, directoryEntry);

                    // This is a storage. we have to remove children items first
                    foreach (IDirectoryEntry de in temp.Children.Cast<IDirectoryEntry>())
                    {
                        temp.DeleteCore(de);
                    }

                    // ...then we Remove storage item from children tree...
                    Children.Delete(directoryEntry, out altDel);

                    // ...after which we need to rethread the root of siblings tree...
                    DirEntry.Child = Children.Root == null ? DirectoryEntry.NOSTREAM : ((IDirectoryEntry)Children.Root).SID;

                    // ...and remove directory (storage) entry

                    if (altDel != null)
                    {
                        directoryEntry = (IDirectoryEntry)altDel;
                    }

                    directoryEntry.Reset();

                    break;

                case StgType.StgStream:

                    // Free directory associated data stream.
                    CompoundFile.FreeAssociatedData(directoryEntry.SID);

                    // Remove item from children tree
                    Children.Delete(directoryEntry, out altDel);

                    // Rethread the root of siblings tree...
                    DirEntry.Child = Children.Root == null ? DirectoryEntry.NOSTREAM : ((IDirectoryEntry)Children.Root).SID;

                    // Delete operation could possibly have cloned a directory, changing its SID.
                    // Invalidate the ACTUALLY deleted directory.
                    if (altDel != null)
                    {
                        directoryEntry = (IDirectoryEntry)altDel;
                    }

                    directoryEntry.Reset();

                    break;
            }
        }

        /// <summary>
        /// Rename a Stream or Storage item in the current storage
        /// </summary>
        /// <param name="oldItemName">The item old name to lookup</param>
        /// <param name="newItemName">The new name to assign</param>
        public void RenameItem(string oldItemName, string newItemName)
        {
            IDirectoryEntry template = DirectoryEntry.Mock(oldItemName, StgType.StgInvalid);
            if (Children.TryLookup(template, out IRBNode item))
            {
                ((DirectoryEntry)item).SetEntryName(newItemName);
            }
            else throw new CFItemNotFound("Item " + oldItemName + " not found in Storage");

            children = null;
            children = LoadChildren(DirEntry); // Rethread
        }
    }
}
