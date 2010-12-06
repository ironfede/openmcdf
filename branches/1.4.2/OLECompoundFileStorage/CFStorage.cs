using System;
using System.Collections.Generic;
using System.Text;
using BinaryTrees;

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
    /// <summary>
    /// Action to apply to  visited items in the OLE structured storage
    /// </summary>
    /// <param name="item">Currently visited item</param>
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
        private BinarySearchTree<IDirectoryEntry> children;

        internal BinarySearchTree<IDirectoryEntry> Children
        {
            get
            {
                // Lazy loading of children tree.
                if (children == null)
                {
                    if (this.CompoundFile.IsFileMapped)
                    {
                        children = LoadChildren(this.dirEntry.SID);
                    }
                    else
                    {
                        children = new BinarySearchTree<IDirectoryEntry>();
                    }
                }

                return children;
            }
        }

        internal void SetChildrenTree(BinarySearchTree<IDirectoryEntry> bst)
        {
            children = bst;
        }


        /// <summary>
        /// Create a new CFStorage
        /// </summary>
        /// <param name="compFile">The Storage Owner - CompoundFile</param>
        internal CFStorage(CompoundFile compFile)
            : base(compFile)
        {
            this.dirEntry = new DirectoryEntry(StgType.StgStorage);
            this.dirEntry.StgColor = StgColor.Black;
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

            this.dirEntry = dirEntry;
        }


        private BinarySearchTree<IDirectoryEntry> LoadChildren(int SID)
        {
            return this.CompoundFile.GetChildrenTree(SID);
        }

        /// <summary>
        /// Create a new child stream inside the current <see cref="T:OLECompoundFileStorage.CFStorage">storage</see>
        /// </summary>
        /// <param name="streamName">The new stream name</param>
        /// <returns>The new <see cref="T:OLECompoundFileStorage.CFStream">stream</see> reference</returns>
        public CFStream AddStream(String streamName)
        {
            CheckDisposed();

            if (String.IsNullOrEmpty(streamName))
                throw new CFException("Stream name cannot be null or empty");

            // Add new Stream directory entry
            IDirectoryEntry cfo = new CFStream(this.CompoundFile);

            cfo.SetEntryName(streamName);
            //cfo.SID = compoundFile.AddDirectoryEntry(cfo);

            // Add object to Siblings tree
            this.Children.Add(cfo);
            CompoundFile.RefreshIterative(Children.Root);
            this.dirEntry.Child = Children.Root.Value.SID;

            return cfo as CFStream;
        }


        /// <summary>
        /// Get a named <see cref="T:OLECompoundFileStorage.CFStream">stream</see> contained in the current storage if existing.
        /// </summary>
        /// <param name="streamName">Name of the stream to look for</param>
        /// <returns>A stream reference if existing</returns>
        /// <exception cref="T:OLECompoundFileStorage.CFItemNotFound">
        /// If no entry with the given name can be found in direct children of the current
        /// storage, a CFItemNotFound exception is raised.
        /// </exception>
        public CFStream GetStream(String streamName)
        {
            CheckDisposed();

            DirectoryEntry de = new DirectoryEntry(StgType.StgStream);
            de.SetEntryName(streamName);

            //if (children == null)
            //{
            //    children = compoundFile.GetChildrenTree(SID);
            //}

            IDirectoryEntry outDe = null;
            if (Children.TryFind(de, out outDe) && outDe.StgType == StgType.StgStream)
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
        /// <exception cref="T:OLECompoundFileStorage.CFItemNotFound">
        /// If no entry with the given name can be found in direct children of the current
        /// storage, a CFItemNotFound exception is raised.
        /// </exception>
        public CFStorage GetStorage(String storageName)
        {
            CheckDisposed();

            DirectoryEntry de = new DirectoryEntry(StgType.StgStorage);
            de.SetEntryName(storageName);

            IDirectoryEntry outDe = null;
            if (Children.TryFind(de, out outDe) && outDe.StgType == StgType.StgStorage)
            {
                return outDe as CFStorage;
            }
            else
            {
                throw new CFItemNotFound("Cannot find item [" + storageName + "] within the current storage");
            }
        }


        /// <summary>
        /// Create new child storage "directory" inside the current storage.
        /// </summary>
        /// <param name="storageName">The new storage name</param>
        /// <returns>Reference to the new <see cref="T:OLECompoundFileStorage.CFStorage">storage</see> </returns>
        /// <example>
        /// <code>
        /// 
        /// CompoundFile cf = new CompoundFile();
        ///
        /// CFStorage st = cf.RootStorage.AddStorage("NameOfStorage");
        /// cf.Save("NewFileName.cfs");
        /// 
        /// cf.Close();
        ///
        /// </code>
        /// </example>
        public CFStorage AddStorage(String storageName)
        {
            CheckDisposed();

            if (String.IsNullOrEmpty(storageName))
                throw new CFException("Stream name cannot be null or empty");

            // Add new Storage directory entry
            CFStorage cfo = new CFStorage(this.CompoundFile);
            ((IDirectoryEntry)cfo).SetEntryName(storageName);

            CompoundFile.AddDirectoryEntry(cfo);

            // Add object to Siblings tree
            Children.Add(cfo);
            CompoundFile.RefreshIterative(Children.Root);
            this.dirEntry.Child = Children.Root.Value.SID;

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


        private NodeAction<IDirectoryEntry> internalAction;

        /// <summary>
        /// Visit all entities contained in the storage applying a user provided action
        /// </summary>
        /// <param name="action">User action to apply to visited entities</param>
        /// <param name="recursive"> Visiting recursion level. True means substorages are visited recursively, false indicates that only the direct children of this storage are visited</param>
        public void VisitEntries(VisitedEntryAction action, bool recursive)
        {
            CheckDisposed();

            if (action != null)
            {
                List<BinaryTreeNode<IDirectoryEntry>> subStorages
                    = new List<BinaryTreeNode<IDirectoryEntry>>();

                internalAction =
                    delegate(BinaryTreeNode<IDirectoryEntry> targetNode)
                    {
                        action(targetNode.Value as CFItem);

                        if (targetNode.Value.Child != DirectoryEntry.NOSTREAM)
                            subStorages.Add(targetNode);

                        return;
                    };

                this.Children.VisitTreeInOrder(internalAction);

                if (recursive && subStorages.Count > 0)
                    foreach (BinaryTreeNode<IDirectoryEntry> n in subStorages)
                    {
                        ((CFStorage)n.Value).VisitEntries(action, recursive);
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
        /// Remove (logically) an entry from the current storage and compound file.
        /// </summary>
        /// <param name="entryName">Entry to delete</param>
        /// <remarks>
        /// Entry name is overwritten with a '_DELETED_NAME_[random]'
        /// string and associated contents are overwritten with zeros.
        /// Data is NOT recoverable.
        /// If phisical deletion is required it's suggested to create a new
        /// compound file and inject in it the old structure using items traversal.
        /// </remarks>
        public void Delete(String entryName)
        {
            CheckDisposed();

            // Find entry to delete
            IDirectoryEntry dto = new DirectoryEntry(StgType.StgInvalid);
            dto.SetEntryName(entryName);

            IDirectoryEntry foundObj = null;

            this.Children.TryFind(dto, out foundObj);

            if (foundObj == null)
                throw new CFException("Entry named [" + entryName + "] not found");

            //if (foundObj.GetType() != typeCheck)
            //    throw new CFException("Entry named [" + entryName + "] has not the correct type");

            if (foundObj.StgType == StgType.StgRoot)
                throw new CFException("Root storage cannot be removed");

            switch (foundObj.StgType)
            {
                case StgType.StgStorage:

                    CFStorage temp = new CFStorage(this.CompoundFile, foundObj);

                    foreach (IDirectoryEntry de in temp.Children)
                    {
                        temp.Delete(de.Name);
                    }

                    // Remove item from children tree
                    this.Children.Remove(foundObj);

                    // Synchronize tree with directory entries
                    this.CompoundFile.RefreshIterative(this.Children.Root);

                    // Rethread the root of siblings tree...
                    if (this.Children.Root != null)
                        this.dirEntry.Child = this.Children.Root.Value.SID;
                    else
                        this.dirEntry.Child = DirectoryEntry.NOSTREAM;

                    // ...and now remove directory (storage) entry
                    this.CompoundFile.RemoveDirectoryEntry(foundObj.SID);

                    break;

                case StgType.StgStream:

                    // Remove item from children tree
                    this.Children.Remove(foundObj);

                    // Synchronize tree with directory entries
                    this.CompoundFile.RefreshIterative(this.Children.Root);

                    // Rethread the root of siblings tree...
                    if (this.Children.Root != null)
                        this.dirEntry.Child = this.Children.Root.Value.SID;
                    else
                        this.dirEntry.Child = DirectoryEntry.NOSTREAM;

                    // Remove directory entry
                    this.CompoundFile.RemoveDirectoryEntry(foundObj.SID);

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
