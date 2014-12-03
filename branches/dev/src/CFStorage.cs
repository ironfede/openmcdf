using System;
using System.Collections.Generic;
using System.Text;
using RedBlackTree;
using System.Diagnostics;

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

namespace OpenMcdf
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
                if (children == null)
                {
                    if (this.CompoundFile.HasSourceStream)
                    {
                        children = LoadChildren(this.DirEntry.SID);
                    }
                    else
                    {
                        children = this.CompoundFile.CreateNewTree();
                    }
                }

                return children;
            }
        }

        /// <summary>
        /// Create a new CFStorage
        /// </summary>
        /// <param name="compFile">The Storage Owner - CompoundFile</param>
        //internal CFStorage(CompoundFile compFile, String name)
        //    : base(compFile)
        //{
        //    this.DirEntry = new DirectoryEntry(name, StgType.StgStorage, compFile.GetDirectories());
        //    this.DirEntry.StgColor = StgColor.Black;
        //    compFile.InsertNewDirectoryEntry(this.DirEntry);
        //}

        /// <summary>
        /// Create a CFStorage using an existing directory (previously loaded).
        /// </summary>
        /// <param name="compFile">The Storage Owner - CompoundFile</param>
        /// <param name="dirEntry">An existing Directory Entry</param>
        internal CFStorage(CompoundFile compFile, IDirectoryEntry dirEntry)
            : base(compFile)
        {
            if (dirEntry == null || dirEntry.SID < 0)
                throw new CFException("Attempting to create a CFStorage using an unitialized directory");

            this.DirEntry = dirEntry;
        }

        private RBTree LoadChildren(int SID)
        {
            RBTree childrenTree = this.CompoundFile.GetChildrenTree(SID);

            if (childrenTree.Root != null)
                this.DirEntry.Child = (childrenTree.Root as IDirectoryEntry).SID;
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
        public CFStream AddStream(String streamName)
        {
            CheckDisposed();

            if (String.IsNullOrEmpty(streamName))
                throw new CFException("Stream name cannot be null or empty");

            CFStream cfo = null;

            DirectoryEntry dirEntry = new DirectoryEntry(streamName, StgType.StgStream, this.CompoundFile.GetDirectories());

            // Add new Stream directory entry
            //cfo = new CFStream(this.CompoundFile, streamName);

            try
            {
                // Add object to Siblings tree
                this.Children.Insert(dirEntry);
                //Trace.WriteLine("**** INSERT STREAM " + cfo.Name + "******");
                //this.Children.Print();
                //Rethread children tree...
                // CompoundFile.RefreshIterative(Children.Root);

                //... and set the root of the tree as new child of the current item directory entry
                this.DirEntry.Child = (Children.Root as IDirectoryEntry).SID;
            }
            catch (RBTreeException)
            {
                CompoundFile.ResetDirectoryEntry(cfo.DirEntry.SID);
                cfo = null;
                throw new CFDuplicatedItemException("An entry with name '" + streamName + "' is already present in storage '" + this.Name + "' ");
            }

            return new CFStream(this.CompoundFile, dirEntry);
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
        public CFStream GetStream(String streamName)
        {
            CheckDisposed();

            DirectoryEntry tmp = new DirectoryEntry(streamName, StgType.StgStream, null);

            //if (children == null)
            //{
            //    children = compoundFile.GetChildrenTree(SID);
            //}

            IRBNode outDe = null;

            if (Children.TryLookup(tmp, out outDe) && (((IDirectoryEntry)outDe).StgType == StgType.StgStream))
            {
                return new CFStream(this.CompoundFile, (IDirectoryEntry)outDe);
            }
            else
            {
                throw new CFItemNotFound("Cannot find item [" + streamName + "] within the current storage");
            }
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
        public CFStorage GetStorage(String storageName)
        {
            CheckDisposed();

            IDirectoryEntry template = new DirectoryEntry(storageName, StgType.StgInvalid, null);
            IRBNode outDe = null;

            if (Children.TryLookup(template, out outDe) && ((IDirectoryEntry)outDe).StgType == StgType.StgStorage)
            {
                return outDe as CFStorage;
            }
            else
            {
                throw new CFItemNotFound("Cannot find item [" + storageName + "] within the current storage");
            }
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
        public CFStorage AddStorage(String storageName)
        {
            CheckDisposed();

            if (String.IsNullOrEmpty(storageName))
                throw new CFException("Stream name cannot be null or empty");

            // Add new Storage directory entry
            IDirectoryEntry cfo
                = new DirectoryEntry(storageName, StgType.StgStorage, this.CompoundFile.GetDirectories());

            //this.CompoundFile.InsertNewDirectoryEntry(cfo);

            try
            {
                // Add object to Siblings tree
                //Trace.WriteLine("**** INSERT STORAGE " + cfo.Name + "******");
                Children.Insert(cfo);
                //Children.Print();
            }
            catch (RBTreeDuplicatedItemException)
            {
                CompoundFile.ResetDirectoryEntry(cfo.SID);
                cfo = null;
                throw new CFDuplicatedItemException("An entry with name '" + storageName + "' is already present in storage '" + this.Name + "' ");
            }

            IDirectoryEntry childrenRoot = Children.Root as IDirectoryEntry;
            this.DirEntry.Child = childrenRoot.SID;

            return new CFStorage(this.CompoundFile, cfo);
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

            if (action != null)
            {
                List<IRBNode> subStorages
                    = new List<IRBNode>();

                Action<IRBNode> internalAction =
                    delegate(IRBNode targetNode)
                    {
                        IDirectoryEntry d = targetNode as IDirectoryEntry;
                        if (d.StgType == StgType.StgStream)
                            action(new CFStream(this.CompoundFile, d));
                        else
                            action(new CFStorage(this.CompoundFile, d));

                        if (d.Child != DirectoryEntry.NOSTREAM)
                            subStorages.Add(targetNode);

                        return;
                    };

                this.Children.VisitTreeNodes(internalAction);

                if (recursive && subStorages.Count > 0)
                    foreach (IRBNode n in subStorages)
                    {
                        IDirectoryEntry d = n as IDirectoryEntry;
                        (new CFStorage(this.CompoundFile, d)).VisitEntries(action, recursive);
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
        public void Delete(String entryName)
        {
            CheckDisposed();

            // Find entry to delete
            IDirectoryEntry tmp = new DirectoryEntry(entryName,StgType.StgInvalid, null);

            IRBNode foundObj = null;

            this.Children.TryLookup(tmp, out foundObj );

            if (foundObj == null)
                throw new CFItemNotFound("Entry named [" + entryName + "] was not found");

            //if (foundObj.GetType() != typeCheck)
            //    throw new CFException("Entry named [" + entryName + "] has not the correct type");

            if (((IDirectoryEntry)foundObj).StgType == StgType.StgRoot)
                throw new CFException("Root storage cannot be removed");

            switch (((IDirectoryEntry)foundObj).StgType)
            {
                case StgType.StgStorage:

                    CFStorage temp = new CFStorage(this.CompoundFile, ((IDirectoryEntry)foundObj));

                    // This is a storage. we have to remove children items first
                    foreach (IRBNode de in temp.Children)
                    {
                        IDirectoryEntry ded = de as IDirectoryEntry;
                        temp.Delete(ded.Name);
                    }

                    // ...then we need to rethread the root of siblings tree...
                    if (this.Children.Root != null)
                        this.DirEntry.Child = (this.Children.Root as IDirectoryEntry).SID;
                    else
                        this.DirEntry.Child = DirectoryEntry.NOSTREAM;

                    // ...and finally Remove storage item from children tree...
                    this.Children.Delete(foundObj);

                    // ...and remove directory (storage) entry
                    this.CompoundFile.RemoveDirectoryEntry(((IDirectoryEntry)foundObj).SID);

                    //Trace.WriteLine("**** DELETED STORAGE " + entryName + "******");

                    // Synchronize tree with directory entries
                    //this.CompoundFile.RefreshIterative(this.Children.Root);

                    break;

                case StgType.StgStream:

                    // Remove item from children tree
                    this.Children.Delete(foundObj);
                    //Trace.WriteLine("**** DELETED STREAM " + entryName + "******");

                    // Synchronize tree with directory entries
                    //this.CompoundFile.RefreshIterative(this.Children.Root);

                    // Rethread the root of siblings tree...
                    if (this.Children.Root != null)
                        this.DirEntry.Child = (this.Children.Root as IDirectoryEntry).SID;
                    else
                        this.DirEntry.Child = DirectoryEntry.NOSTREAM;


                    // Remove directory entry
                    this.CompoundFile.RemoveDirectoryEntry(((IDirectoryEntry)foundObj).SID);


                    break;
            }

            //// Refresh recursively all SIDs (invariant for tree sorting)
            //VisitedEntryAction action = delegate(CFSItem target)
            //{
            //    if( ((IDirectoryEntry)target).SID>foundObj.SID )
            //    {
            //        ((IDirectoryEntry)target).SID--;
            //    }                   


            //    ((IDirectoryEntry)target).LeftSibling--;
            //};
        }
    }
}
