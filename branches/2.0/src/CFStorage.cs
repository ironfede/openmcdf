using System;
using System.Collections.Generic;
using System.Text;
using BinaryTrees;
using RBTree;

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
        private RedBlackTree children;

        internal RedBlackTree Children
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
                        children = new RedBlackTree();
                    }
                }

                return children;
            }
        }

        /// <summary>
        /// Create a new CFStorage
        /// </summary>
        /// <param name="compFile">The Storage Owner - CompoundFile</param>
        internal CFStorage(CompoundFile compFile)
            : base(compFile)
        {
            this.DirEntry = new DirectoryEntry(StgType.StgStorage);
            this.DirEntry.StgColor = StgColor.Black;
            compFile.InsertNewDirectoryEntry(this.DirEntry);
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
                throw new CFException("Attempting to create a CFStorage using an unitialized directory");

            this.DirEntry = dirEntry;
        }

        private RedBlackTree LoadChildren(int SID)
        {
            RedBlackTree childrenTree = this.CompoundFile.GetChildrenTree(SID);

            if (childrenTree.Root != RedBlackTree.sentinelNode)
                this.DirEntry.Child = childrenTree.Root.Data.DirEntry.SID;
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


            // Add new Stream directory entry
            cfo = new CFStream(this.CompoundFile);
            cfo.DirEntry.SetEntryName(streamName);


            try
            {
                // Add object to Siblings tree
                this.Children.Add(cfo);

                //Rethread children tree...
                CompoundFile.RefreshIterative(Children.Root);

                //... and set the root of the tree as new child of the current item directory entry
                this.DirEntry.Child = Children.Root.Data.DirEntry.SID;
            }
            catch (BSTDuplicatedException)
            {
                CompoundFile.ResetDirectoryEntry(cfo.DirEntry.SID);
                cfo = null;
                throw new CFDuplicatedItemException("An entry with name '" + streamName + "' is already present in storage '" + this.Name + "' ");
            }

            return cfo as CFStream;
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

            CFMock tmp = new CFMock(streamName, StgType.StgStream);

            //if (children == null)
            //{
            //    children = compoundFile.GetChildrenTree(SID);
            //}

            CFItem outDe = null;

            if (Children.TryFind(tmp, out outDe) && outDe.DirEntry.StgType == StgType.StgStream)
            {
                return outDe as CFStream;
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

            CFMock tmp = new CFMock(storageName, StgType.StgStorage);

            CFItem outDe = null;
            if (Children.TryFind(tmp, out outDe) && outDe.DirEntry.StgType == StgType.StgStorage)
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
            CFStorage cfo = null;

            cfo = new CFStorage(this.CompoundFile);
            cfo.DirEntry.SetEntryName(storageName);

            try
            {
                // Add object to Siblings tree
                Children.Add(cfo);
            }
            catch (BSTDuplicatedException)
            {

                CompoundFile.ResetDirectoryEntry(cfo.DirEntry.SID);
                cfo = null;
                throw new CFDuplicatedItemException("An entry with name '" + storageName + "' is already present in storage '" + this.Name + "' ");
            }


            CompoundFile.RefreshIterative(Children.Root);
            this.DirEntry.Child = Children.Root.Data.DirEntry.SID;
            return cfo;
        }

        //public List<ICFObject> GetSubTreeObjects()
        //{
        //    List<ICFObject> result = new List<ICFObject>();

        //    children.VisitTree(TraversalMethod.Inorder,
        //         delegate(BinaryTreeNode<ICFObject> node)
        //         {
        //             result.Add(node.Value);
        //         });

        //    return result;
        //}


        private VisitedRBNodeAction internalAction;

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
        public void VisitEntries(VisitedEntryAction action, bool recursive)
        {
            CheckDisposed();

            if (action != null)
            {
                List<RedBlackNode> subStorages
                    = new List<RedBlackNode>();

                internalAction =
                    delegate(RedBlackNode targetNode)
                    {
                        action(targetNode.Data as CFItem);

                        if (targetNode.Data.DirEntry.Child != DirectoryEntry.NOSTREAM)
                            subStorages.Add(targetNode);

                        return;
                    };

                this.Children.VisitTreeInOrder(internalAction);

                if (recursive && subStorages.Count > 0)
                    foreach (RedBlackNode n in subStorages)
                    {
                        ((CFStorage)n.Data).VisitEntries(action, recursive);
                    }
            }
        }


        //public void DeleteStream(String name)
        //{
        //    Delete(name, typeof(CFStream));
        //}

        //public void DeleteStorage(String name)
        //{
        //    Delete(name, typeof(CFStorage));
        //}



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
            CFMock tmp = new CFMock(entryName, StgType.StgInvalid);

            CFItem foundObj = null;

            this.Children.TryFind(tmp, out foundObj);

            if (foundObj == null)
                throw new CFItemNotFound("Entry named [" + entryName + "] was not found");

            //if (foundObj.GetType() != typeCheck)
            //    throw new CFException("Entry named [" + entryName + "] has not the correct type");

            if (foundObj.DirEntry.StgType == StgType.StgRoot)
                throw new CFException("Root storage cannot be removed");

            switch (foundObj.DirEntry.StgType)
            {
                case StgType.StgStorage:

                    CFStorage temp = (CFStorage)foundObj;

                    foreach (RedBlackNode de in temp.Children)
                    {
                        temp.Delete(de.Data.Name);
                    }

                    // Remove item from children tree
                    this.Children.Remove(foundObj);

                    // Synchronize tree with directory entries
                    this.CompoundFile.RefreshIterative(this.Children.Root);

                    // Rethread the root of siblings tree...
                    if (!this.Children.Root.IsSentinel)
                        this.DirEntry.Child = this.Children.Root.Data.DirEntry.SID;
                    else
                        this.DirEntry.Child = DirectoryEntry.NOSTREAM;

                    // ...and now remove directory (storage) entry
                    this.CompoundFile.RemoveDirectoryEntry(foundObj.DirEntry.SID);

                    break;

                case StgType.StgStream:

                    // Remove item from children tree
                    this.Children.Remove(foundObj);

                    // Synchronize tree with directory entries
                    this.CompoundFile.RefreshIterative(this.Children.Root);

                    // Rethread the root of siblings tree...
                    if (!this.Children.Root.IsSentinel)
                        this.DirEntry.Child = this.Children.Root.Data.DirEntry.SID;
                    else
                        this.DirEntry.Child = DirectoryEntry.NOSTREAM;

                    // Remove directory entry
                    this.CompoundFile.RemoveDirectoryEntry(foundObj.DirEntry.SID);

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
