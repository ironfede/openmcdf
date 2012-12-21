using System.Collections;
using System.Text;
using System;
using System.Reflection;
using OpenMcdf;

namespace RBTree
{
    ///<summary>
    ///A red-black tree must satisfy these properties:
    ///
    ///1. The root is black. 
    ///2. All leaves are black. 
    ///3. Red nodes can only have black children. 
    ///4. All paths from a node to its leaves contain the same number of black nodes.
    ///</summary>
    public class RedBlack
    {
        // the number of nodes contained in the tree
        private int intCount;
        // a simple randomized hash code. The hash code could be used as a key
        // if it is "unique" enough. Note: The IComparable interface would need to 
        // be replaced with int.
        private int intHashCode;
        // identifies the owner of the tree
        private string strIdentifier;
        // the tree
        private RedBlackNode rbTree;
        //  sentinelNode is convenient way of indicating a leaf node.
        public static RedBlackNode sentinelNode;
        // the node that was last found; used to optimize searches
        private RedBlackNode lastNodeFound;
        private Random rand = new Random();

        public RedBlack()
        {
            strIdentifier = base.ToString() + rand.Next();
            intHashCode = rand.Next();

            // set up the sentinel node. the sentinel node is the key to a successfull
            // implementation and for understanding the red-black tree properties.
            sentinelNode = new RedBlackNode(null);
            sentinelNode.Left = null;
            sentinelNode.Right = null;
            sentinelNode.Parent = null;
            sentinelNode.Color = StgColor.Black;

            rbTree = sentinelNode;
            lastNodeFound = sentinelNode;
        }

        public RedBlack(string strIdentifier)
        {
            intHashCode = rand.Next();
            this.strIdentifier = strIdentifier;
        }

        ///<summary>
        /// Add
        /// args: ByVal key As IComparable, ByVal data As Object
        /// key is object that implements IComparable interface
        /// performance tip: change to use use int type (such as the hashcode)
        ///</summary>
        public void Add(CFItem data)
        {
            if (data == null)
                throw (new RedBlackException("RedBlackNode data must not be null"));

            // traverse tree - find where node belongs
            int result = 0;
            // create new node
            RedBlackNode node = new RedBlackNode(data);
            RedBlackNode temp = rbTree;				// grab the rbTree node of the tree

            while (temp != sentinelNode)
            {	// find Parent
                node.Parent = temp;
                result = node.Data.CompareTo(temp.Data);
                if (result == 0)
                    throw (new RedBlackException("A Node with the same key already exists"));
                if (result > 0)
                    temp = temp.Right;
                else
                    temp = temp.Left;
            }

            // setup node
            node.Left = sentinelNode;
            node.Right = sentinelNode;

            // insert node into tree starting at parent's location
            if (node.Parent != null)
            {
                result = node.Data.CompareTo(node.Parent.Data);
                if (result > 0)
                    node.Parent.Right = node;
                else
                    node.Parent.Left = node;
            }
            else
                rbTree = node;					// first node added

            RestoreAfterInsert(node);           // restore red-black properities

            lastNodeFound = node;

            intCount = intCount + 1;
        }
        ///<summary>
        /// RestoreAfterInsert
        /// Additions to red-black trees usually destroy the red-black 
        /// properties. Examine the tree and restore. Rotations are normally 
        /// required to restore it
        ///</summary>
        private void RestoreAfterInsert(RedBlackNode x)
        {
            // x and y are used as variable names for brevity, in a more formal
            // implementation, you should probably change the names

            RedBlackNode y;

            // maintain red-black tree properties after adding x
            while (x != rbTree && x.Parent.Color == StgColor.Red)
            {
                // Parent node is .Colored red; 
                if (x.Parent == x.Parent.Parent.Left)	// determine traversal path			
                {										// is it on the Left or Right subtree?
                    y = x.Parent.Parent.Right;			// get uncle
                    if (y != null && y.Color == StgColor.Red)
                    {	// uncle is red; change x's Parent and uncle to black
                        x.Parent.Color = StgColor.Black;
                        y.Color = StgColor.Black;
                        // grandparent must be red. Why? Every red node that is not 
                        // a leaf has only black children 
                        x.Parent.Parent.Color = StgColor.Red;
                        x = x.Parent.Parent;	// continue loop with grandparent
                    }
                    else
                    {
                        // uncle is black; determine if x is greater than Parent
                        if (x == x.Parent.Right)
                        {	// yes, x is greater than Parent; rotate Left
                            // make x a Left child
                            x = x.Parent;
                            RotateLeft(x);
                        }
                        // no, x is less than Parent
                        x.Parent.Color = StgColor.Black;	// make Parent black
                        x.Parent.Parent.Color = StgColor.Red;		// make grandparent black
                        RotateRight(x.Parent.Parent);					// rotate right
                    }
                }
                else
                {	// x's Parent is on the Right subtree
                    // this code is the same as above with "Left" and "Right" swapped
                    y = x.Parent.Parent.Left;
                    if (y != null && y.Color == StgColor.Red)
                    {
                        x.Parent.Color = StgColor.Black;
                        y.Color = StgColor.Black;
                        x.Parent.Parent.Color = StgColor.Red;
                        x = x.Parent.Parent;
                    }
                    else
                    {
                        if (x == x.Parent.Left)
                        {
                            x = x.Parent;
                            RotateRight(x);
                        }
                        x.Parent.Color = StgColor.Black;
                        x.Parent.Parent.Color = StgColor.Red;
                        RotateLeft(x.Parent.Parent);
                    }
                }
            }
            rbTree.Color = StgColor.Black;		// rbTree should always be black
        }

        ///<summary>
        /// RotateLeft
        /// Rebalance the tree by rotating the nodes to the left
        ///</summary>
        public void RotateLeft(RedBlackNode x)
        {
            // pushing node x down and to the Left to balance the tree. x's Right child (y)
            // replaces x (since y > x), and y's Left child becomes x's Right child 
            // (since it's < y but > x).

            RedBlackNode y = x.Right;			// get x's Right node, this becomes y

            // set x's Right link
            x.Right = y.Left;					// y's Left child's becomes x's Right child

            // modify parents
            if (y.Left != sentinelNode)
                y.Left.Parent = x;				// sets y's Left Parent to x

            if (y != sentinelNode)
                y.Parent = x.Parent;			// set y's Parent to x's Parent

            if (x.Parent != null)
            {	// determine which side of it's Parent x was on
                if (x == x.Parent.Left)
                    x.Parent.Left = y;			// set Left Parent to y
                else
                    x.Parent.Right = y;			// set Right Parent to y
            }
            else
                rbTree = y;						// at rbTree, set it to y

            // link x and y 
            y.Left = x;							// put x on y's Left 
            if (x != sentinelNode)						// set y as x's Parent
                x.Parent = y;
        }
        ///<summary>
        /// RotateRight
        /// Rebalance the tree by rotating the nodes to the right
        ///</summary>
        public void RotateRight(RedBlackNode x)
        {
            // pushing node x down and to the Right to balance the tree. x's Left child (y)
            // replaces x (since x < y), and y's Right child becomes x's Left child 
            // (since it's < x but > y).

            RedBlackNode y = x.Left;			// get x's Left node, this becomes y

            // set x's Right link
            x.Left = y.Right;					// y's Right child becomes x's Left child

            // modify parents
            if (y.Right != sentinelNode)
                y.Right.Parent = x;				// sets y's Right Parent to x

            if (y != sentinelNode)
                y.Parent = x.Parent;			// set y's Parent to x's Parent

            if (x.Parent != null)				// null=rbTree, could also have used rbTree
            {	// determine which side of it's Parent x was on
                if (x == x.Parent.Right)
                    x.Parent.Right = y;			// set Right Parent to y
                else
                    x.Parent.Left = y;			// set Left Parent to y
            }
            else
                rbTree = y;						// at rbTree, set it to y

            // link x and y 
            y.Right = x;						// put x on y's Right
            if (x != sentinelNode)				// set y as x's Parent
                x.Parent = y;
        }
        ///<summary>
        /// GetData
        /// Gets the data object associated with the specified key
        ///<summary>
        public CFItem GetData(CFItem template)
        {
            int result;

            RedBlackNode treeNode = rbTree;     // begin at root

            // traverse tree until node is found
            while (treeNode != sentinelNode)
            {
                result = template.CompareTo(treeNode.Data);
                if (result == 0)
                {
                    lastNodeFound = treeNode;
                    return treeNode.Data;
                }
                if (result < 0)
                    treeNode = treeNode.Left;
                else
                    treeNode = treeNode.Right;
            }

            throw (new RedBlackException("RedBlackNode key was not found"));
        }

        
       
        ///<summary>
        /// GetEnumerator
        /// return an enumerator that returns the tree nodes in order
        ///<summary>
        public RedBlackEnumerator GetEnumerator()
        {
            // elements is simply a generic name to refer to the 
            // data objects the nodes contain
            return new RedBlackEnumerator(rbTree);
        }
 
              
        ///<summary>
        /// IsEmpty
        /// Is the tree empty?
        ///<summary>
        public bool IsEmpty()
        {
            return (rbTree == null);
        }
        ///<summary>
        /// Remove
        /// removes the key and data object (delete)
        ///<summary>
        public void Remove(CFItem template)
        {
            if (template == null)
                throw (new RedBlackException("RedBlackNode is null"));

            // find node
            int result;
            RedBlackNode node;

            // see if node to be deleted was the last one found
            result = template.CompareTo(lastNodeFound.Data);
            if (result == 0)
                node = lastNodeFound;
            else
            {	// not found, must search		
                node = rbTree;
                while (node != sentinelNode)
                {
                    result = template.CompareTo(node.Data);

                    if (result == 0)
                        break;
                    if (result < 0)
                        node = node.Left;
                    else
                        node = node.Right;
                }

                if (node == sentinelNode)
                    return;				// key not found
            }

            Delete(node);

            intCount = intCount - 1;
        }
        ///<summary>
        /// Delete
        /// Delete a node from the tree and restore red black properties
        ///<summary>
        private void Delete(RedBlackNode z)
        {
            // A node to be deleted will be: 
            //		1. a leaf with no children
            //		2. have one child
            //		3. have two children
            // If the deleted node is red, the red black properties still hold.
            // If the deleted node is black, the tree needs rebalancing

            RedBlackNode x = null;	// work node to contain the replacement node
            RedBlackNode y;			// work node 

            // find the replacement node (the successor to x) - the node one with 
            // at *most* one child. 
            if (z.Left == sentinelNode || z.Right == sentinelNode)
                y = z;						// node has sentinel as a child
            else
            {
                // z has two children, find replacement node which will 
                // be the leftmost node greater than z
                y = z.Right;				        // traverse right subtree	
                while (y.Left != sentinelNode)		// to find next node in sequence
                    y = y.Left;
            }

            // at this point, y contains the replacement node. it's content will be copied 
            // to the valules in the node to be deleted

            // x (y's only child) is the node that will be linked to y's old parent. 
            if (y.Left != sentinelNode)
                x = y.Left;
            else
                x = y.Right;

            // replace x's parent with y's parent and
            // link x to proper subtree in parent
            // this removes y from the chain
            x.Parent = y.Parent;
            if (y.Parent != null)
                if (y == y.Parent.Left)
                    y.Parent.Left = x;
                else
                    y.Parent.Right = x;
            else
                rbTree = x;			// make x the root node

            // copy the values from y (the replacement node) to the node being deleted.
            // note: this effectively deletes the node. 
            if (y != z)
            {
                z.Data = y.Data;
            }

            if (y.Color == StgColor.Black)
                RestoreAfterDelete(x);

            lastNodeFound = sentinelNode;
        }

        ///<summary>
        /// RestoreAfterDelete
        /// Deletions from red-black trees may destroy the red-black 
        /// properties. Examine the tree and restore. Rotations are normally 
        /// required to restore it
        ///</summary>
        private void RestoreAfterDelete(RedBlackNode x)
        {
            // maintain Red-Black tree balance after deleting node 			

            RedBlackNode y;

            while (x != rbTree && x.Color == StgColor.Black)
            {
                if (x == x.Parent.Left)			// determine sub tree from parent
                {
                    y = x.Parent.Right;			// y is x's sibling 
                    if (y.Color == StgColor.Red)
                    {	// x is black, y is red - make both black and rotate
                        y.Color = StgColor.Black;
                        x.Parent.Color = StgColor.Red;
                        RotateLeft(x.Parent);
                        y = x.Parent.Right;
                    }
                    if (y.Left.Color == StgColor.Black &&
                        y.Right.Color == StgColor.Black)
                    {	// children are both black
                        y.Color = StgColor.Red;		// change parent to red
                        x = x.Parent;					// move up the tree
                    }
                    else
                    {
                        if (y.Right.Color == StgColor.Black)
                        {
                            y.Left.Color = StgColor.Black;
                            y.Color = StgColor.Red;
                            RotateRight(y);
                            y = x.Parent.Right;
                        }
                        y.Color = x.Parent.Color;
                        x.Parent.Color = StgColor.Black;
                        y.Right.Color = StgColor.Black;
                        RotateLeft(x.Parent);
                        x = rbTree;
                    }
                }
                else
                {	// right subtree - same as code above with right and left swapped
                    y = x.Parent.Left;
                    if (y.Color == StgColor.Red)
                    {
                        y.Color = StgColor.Black;
                        x.Parent.Color = StgColor.Red;
                        RotateRight(x.Parent);
                        y = x.Parent.Left;
                    }
                    if (y.Right.Color == StgColor.Black &&
                        y.Left.Color == StgColor.Black)
                    {
                        y.Color = StgColor.Red;
                        x = x.Parent;
                    }
                    else
                    {
                        if (y.Left.Color == StgColor.Black)
                        {
                            y.Right.Color = StgColor.Black;
                            y.Color = StgColor.Red;
                            RotateLeft(y);
                            y = x.Parent.Left;
                        }
                        y.Color = x.Parent.Color;
                        x.Parent.Color = StgColor.Black;
                        y.Left.Color = StgColor.Black;
                        RotateRight(x.Parent);
                        x = rbTree;
                    }
                }
            }
            x.Color = StgColor.Black;
        }

        
        ///<summary>
        /// Clear
        /// Empties or clears the tree
        ///<summary>
        public void Clear()
        {
            rbTree = sentinelNode;
            intCount = 0;
        }

        ///<summary>
        /// Size
        /// returns the size (number of nodes) in the tree
        ///<summary>
        public int Size()
        {
            // number of keys
            return intCount;
        }

        ///<summary>
        /// Equals
        ///<summary>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (!(obj is RedBlackNode))
                return false;

            if (this == obj)
                return true;

            return (ToString().Equals(((RedBlackNode)(obj)).ToString()));

        }

        ///<summary>
        /// HashCode
        ///<summary>
        public override int GetHashCode()
        {
            return intHashCode;
        }

        ///<summary>
        /// ToString
        ///<summary>
        public override string ToString()
        {
            return strIdentifier.ToString();
        }
    }
}
