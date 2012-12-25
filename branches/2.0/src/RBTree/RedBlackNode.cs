// Original source code from CodeProject, CPOL license, by Roy Clem
// http://www.codeproject.com/Articles/8287/Red-Black-Trees-in-C
// Modified from Federico Blaseotto,2012.

using System;
using System.Text;
using OpenMcdf;

namespace RBTree
{
    ///<summary>
    /// The RedBlackNode class encapsulates a node in the tree
    ///</summary>
    public class RedBlackNode
    {
        // the data or value associated with the key
        private CFItem data;

        // left node 
        private RedBlackNode rbnLeft;
        // right node 
        private RedBlackNode rbnRight;
        // parent node 
        private RedBlackNode rbnParent;

        ///<summary>
        ///Data
        ///</summary>
        public CFItem Data
        {
            get
            {
                return data;
            }

            set
            {
                data = value;
            }
        }

        public bool IsSentinel
        {
            get { return this == RedBlackTree.sentinelNode; }
        }

        ///<summary>
        ///Color
        ///</summary>
        public StgColor Color
        {
            get
            {
                return data != null ? data.DirEntry.StgColor : StgColor.Black;
            }

            set
            {
                if (data != null)
                    data.DirEntry.StgColor = value;
            }
        }
        ///<summary>
        ///Left
        ///</summary>
        public RedBlackNode Left
        {
            get
            {
                return rbnLeft;
            }

            set
            {
                rbnLeft = value;
            }
        }
        ///<summary>
        /// Right
        ///</summary>
        public RedBlackNode Right
        {
            get
            {
                return rbnRight;
            }

            set
            {
                rbnRight = value;
            }
        }
        public RedBlackNode Parent
        {
            get
            {
                return rbnParent;
            }

            set
            {
                rbnParent = value;
            }
        }

        public RedBlackNode(CFItem item)
        {
            this.data = item;
            if (item != null)
                Color = (int)StgColor.Red;
        }
    }
}
