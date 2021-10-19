/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. 
 * 
 * The Original Code is OpenMCDF - Compound Document Format library.
 * 
 * The Initial Developer of the Original Code is Federico Blaseotto.*/


using System;
using System.Collections.Generic;
using RedBlackTree;


namespace OpenMcdf
{
    /// <summary>
    /// Storage entity that acts like a logic container for streams
    /// or substorages in a compound file.
    /// </summary>
    public class ReadonlyCompoundFileStorage : ReadonlyCompoundFileItem
    {
        private RBTree children;

        internal RBTree Children
        {
            get
            {
                // Lazy loading of children tree.
                if (children == null)
                {
                    //if (this.CompoundFile.HasSourceStream)
                    //{
                    children = LoadChildren(this.DirEntry.SID);
                    //}
                    //else
                    if (children == null)
                    {
                        children = this.CompoundFile.CreateNewTree();
                    }
                }

                return children;
            }
        }


        /// <summary>
        /// Create a CFStorage using an existing directory (previously loaded).
        /// </summary>
        /// <param name="compFile">The Storage Owner - CompoundFile</param>
        /// <param name="dirEntry">An existing Directory Entry</param>
        internal ReadonlyCompoundFileStorage(ReadonlyCompoundFile compFile, IDirectoryEntry dirEntry)
            : base(compFile)
        {
            if (dirEntry == null || dirEntry.SID < 0)
                throw new CFException("Attempting to create a CFStorage using an unitialized directory");

            this.DirEntry = dirEntry;
        }

        private RBTree LoadChildren(int sId)
        {
            RBTree childrenTree = this.CompoundFile.GetChildrenTree(sId);

            if (childrenTree.Root != null)
                this.DirEntry.Child = (childrenTree.Root as IDirectoryEntry)?.SID ?? 0;
            else
                this.DirEntry.Child = DirectoryEntry.NOSTREAM;

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
        public ReadonlyCompoundFileStream AddStream(String streamName)
        {
            CheckDisposed();

            if (String.IsNullOrEmpty(streamName))
                throw new CFException("Stream name cannot be null or empty");



            IDirectoryEntry dirEntry = DirectoryEntry.TryNew(streamName, StgType.StgStream, this.CompoundFile.GetDirectories());

            // Add new Stream directory entry
            //cfo = new CFStream(this.CompoundFile, streamName);

            try
            {
                // Add object to Siblings tree
                this.Children.Insert(dirEntry);

                //... and set the root of the tree as new child of the current item directory entry
                this.DirEntry.Child = (Children.Root as IDirectoryEntry)?.SID ?? 0;
            }
            catch (RBTreeException)
            {
                CompoundFile.ResetDirectoryEntry(dirEntry.SID);

                throw new CFDuplicatedItemException("An entry with name '" + streamName + "' is already present in storage '" + this.Name + "' ");
            }

            return new ReadonlyCompoundFileStream(this.CompoundFile, dirEntry);
        }


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
        public ReadonlyCompoundFileStream GetStream(String streamName)
        {
            CheckDisposed();

            IDirectoryEntry tmp = DirectoryEntry.Mock(streamName, StgType.StgStream);

            if (Children.TryLookup(tmp, out var outDe) && (((IDirectoryEntry)outDe).StgType == StgType.StgStream))
            {
                return new ReadonlyCompoundFileStream(this.CompoundFile, (IDirectoryEntry)outDe);
            }
            else
            {
                throw new CFItemNotFound("Cannot find item [" + streamName + "] within the current storage");
            }
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
        public bool TryGetStream(String streamName, out ReadonlyCompoundFileStream cfStream)
        {
            bool result = false;
            cfStream = null;

            try
            {
                CheckDisposed();

                IDirectoryEntry tmp = DirectoryEntry.Mock(streamName, StgType.StgStream);

                if (Children.TryLookup(tmp, out var outDe) && (((IDirectoryEntry)outDe).StgType == StgType.StgStream))
                {
                    cfStream = new ReadonlyCompoundFileStream(this.CompoundFile, (IDirectoryEntry)outDe);
                    result = true;
                }
            }
            catch (CFDisposedException)
            {
                result = false;

            }

            return result;
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
        public ReadonlyCompoundFileStorage GetStorage(String storageName)
        {
            CheckDisposed();

            IDirectoryEntry template = DirectoryEntry.Mock(storageName, StgType.StgInvalid);
            IRBNode outDe = null;

            if (Children.TryLookup(template, out outDe) && ((IDirectoryEntry)outDe).StgType == StgType.StgStorage)
            {
                return new ReadonlyCompoundFileStorage(this.CompoundFile, outDe as IDirectoryEntry);
            }
            else
            {
                throw new CFItemNotFound("Cannot find item [" + storageName + "] within the current storage");
            }
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
        public bool TryGetStorage(String storageName, out ReadonlyCompoundFileStorage cfStorage)
        {
            bool result = false;
            cfStorage = null;

            try
            {
                CheckDisposed();

                IDirectoryEntry template = DirectoryEntry.Mock(storageName, StgType.StgInvalid);
                IRBNode outDe = null;

                if (Children.TryLookup(template, out outDe) && ((IDirectoryEntry)outDe).StgType == StgType.StgStorage)
                {
                    cfStorage = new ReadonlyCompoundFileStorage(this.CompoundFile, outDe as IDirectoryEntry);
                    result = true;
                }

            }
            catch (CFDisposedException)
            {
                result = false;
            }

            return result;
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
        public ReadonlyCompoundFileStorage AddStorage(String storageName)
        {
            CheckDisposed();

            if (String.IsNullOrEmpty(storageName))
                throw new CFException("Stream name cannot be null or empty");

            // Add new Storage directory entry
            IDirectoryEntry cfo
                = DirectoryEntry.New(storageName, StgType.StgStorage, this.CompoundFile.GetDirectories());

            //this.CompoundFile.InsertNewDirectoryEntry(cfo);

            try
            {
                // Add object to Siblings tree
                Children.Insert(cfo);
            }
            catch (RBTreeDuplicatedItemException)
            {
                CompoundFile.ResetDirectoryEntry(cfo.SID);
                cfo = null;
                throw new CFDuplicatedItemException("An entry with name '" + storageName + "' is already present in storage '" + this.Name + "' ");
            }

            IDirectoryEntry childrenRoot = Children.Root as IDirectoryEntry;
            this.DirEntry.Child = childrenRoot.SID;

            return new ReadonlyCompoundFileStorage(this.CompoundFile, cfo);
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
        public void VisitEntries(Action<ReadonlyCompoundFileItem> action, bool recursive)
        {
            CheckDisposed();

            if (action == null)
            {
                return;
            }

            List<IRBNode> subStorages
                = new List<IRBNode>();

            void InternalAction(IRBNode targetNode)
            {
                IDirectoryEntry d = targetNode as IDirectoryEntry;
                if (d.StgType == StgType.StgStream)
                    action(new ReadonlyCompoundFileStream(this.CompoundFile, d));
                else
                    action(new ReadonlyCompoundFileStorage(this.CompoundFile, d));

                if (d.Child != DirectoryEntry.NOSTREAM) subStorages.Add(targetNode);

                return;
            }

            this.Children.VisitTreeNodes(InternalAction);

            if (recursive && subStorages.Count > 0)
                foreach (IRBNode n in subStorages)
                {
                    IDirectoryEntry d = n as IDirectoryEntry;
                    (new ReadonlyCompoundFileStorage(this.CompoundFile, d)).VisitEntries(action, recursive);
                }
        }
    }
}
