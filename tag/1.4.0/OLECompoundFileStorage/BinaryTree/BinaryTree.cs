#region Using directives

using System;
using System.Collections.Generic;
using System.Text;

#endregion

namespace BinaryTrees
{
    /// <summary>
    /// Represents a binary tree.  This class provides access to the Root of the tree.  The developer
    /// must manually create the binary tree by adding descendents to the root.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the binary tree's nodes.</typeparam>
    public class BinaryTree<T>
    {
        #region Private Member Variables
        private BinaryTreeNode<T> root = null;
        #endregion

        #region Public Methods
        /// <summary>
        /// Clears out the contents of the binary tree.
        /// </summary>
        public void Clear()
        {
            root = null;
        }
        #endregion

        #region Public Properties
        public BinaryTreeNode<T> Root
        {
            get
            {
                return root;
            }
            set
            {
                root = value;
            }
        }
        #endregion
    }
}
