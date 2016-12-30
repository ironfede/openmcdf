#define ASSERT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if ASSERT
using System.Diagnostics;
#endif

// ------------------------------------------------------------- 
// This is a porting from java code, under MIT license of       |
// the beautiful Red-Black Tree implementation you can find at  |
// http://en.literateprograms.org/Red-black_tree_(Java)#chunk   |
// Many Thanks to original Implementors.                        |
// -------------------------------------------------------------

namespace RedBlackTree
{
    public class RBTreeException : Exception
    {
        public RBTreeException(String msg)
            : base(msg)
        {
        }
    }
    public class RBTreeDuplicatedItemException : RBTreeException
    {
        public RBTreeDuplicatedItemException(String msg)
            : base(msg)
        {
        }
    }

    public enum Color { RED = 0, BLACK = 1 }

    /// <summary>
    /// Red Black Node interface
    /// </summary>
    public interface IRBNode : IComparable
    {

        IRBNode Left
        {
            get;
            set;
        }

        IRBNode Right
        {
            get;
            set;
        }


        Color Color

        { get; set; }



        IRBNode Parent { get; set; }


        IRBNode Grandparent();


        IRBNode Sibling();
        //        {
        //#if ASSERT
        //            Debug.Assert(Parent != null); // Root node has no sibling
        //#endif
        //            if (this == Parent.Left)
        //                return Parent.Right;
        //            else
        //                return Parent.Left;
        //        }

        IRBNode Uncle();
        //        {
        //#if ASSERT
        //            Debug.Assert(Parent != null); // Root node has no uncle
        //            Debug.Assert(Parent.Parent != null); // Children of root have no uncle
        //#endif
        //            return Parent.Sibling();
        //        }
        //    }

        void AssignValueTo(IRBNode other);
    }

    public class RBTree
    {
        public IRBNode Root { get; set; }

        private static Color NodeColor(IRBNode n)
        {
            return n == null ? Color.BLACK : n.Color;
        }

        public RBTree()
        {

        }

        public RBTree(IRBNode root)
        {
            this.Root = root;
        }


        private IRBNode LookupNode(IRBNode template)
        {
            IRBNode n = Root;

            while (n != null)
            {
                int compResult = template.CompareTo(n);

                if (compResult == 0)
                {
                    return n;
                }
                else if (compResult < 0)
                {
                    n = n.Left;
                }
                else
                {
                    //assert compResult > 0;
                    n = n.Right;
                }
            }

            return n;
        }

        public bool TryLookup(IRBNode template, out IRBNode val)
        {
            IRBNode n = LookupNode(template);

            if (n == null)
            {
                val = null;
                return false;
            }
            else
            {
                val = n;
                return true;
            }
        }

        private void ReplaceNode(IRBNode oldn, IRBNode newn)
        {
            if (oldn.Parent == null)
            {
                Root = newn;
            }
            else
            {
                if (oldn == oldn.Parent.Left)
                    oldn.Parent.Left = newn;
                else
                    oldn.Parent.Right = newn;
            }
            if (newn != null)
            {
                newn.Parent = oldn.Parent;
            }
        }

        private void RotateLeft(IRBNode n)
        {
            IRBNode r = n.Right;
            ReplaceNode(n, r);
            n.Right = r.Left;
            if (r.Left != null)
            {
                r.Left.Parent = n;
            }
            r.Left = n;
            n.Parent = r;
        }

        private void RotateRight(IRBNode n)
        {
            IRBNode l = n.Left;
            ReplaceNode(n, l);
            n.Left = l.Right;

            if (l.Right != null)
            {
                l.Right.Parent = n;
            }

            l.Right = n;
            n.Parent = l;
        }



        public void Insert(IRBNode newNode)
        {
            newNode.Color = Color.RED;
            IRBNode insertedNode = newNode;

            if (Root == null)
            {
                Root = insertedNode;
            }
            else
            {
                IRBNode n = Root;
                while (true)
                {
                    int compResult = newNode.CompareTo(n);
                    if (compResult == 0)
                    {
                        throw new RBTreeDuplicatedItemException("RBNode " + newNode.ToString() + " already present in tree");
                        //n.Value = value;
                        //return;
                    }
                    else if (compResult < 0)
                    {
                        if (n.Left == null)
                        {
                            n.Left = insertedNode;

                            break;
                        }
                        else
                        {
                            n = n.Left;
                        }
                    }
                    else
                    {
                        //assert compResult > 0;
                        if (n.Right == null)
                        {
                            n.Right = insertedNode;

                            break;
                        }
                        else
                        {
                            n = n.Right;
                        }
                    }
                }
                insertedNode.Parent = n;
            }

            InsertCase1(insertedNode);

            if (NodeInserted != null)
            {
                NodeInserted(insertedNode);
            }

            //Trace.WriteLine(" ");
            //Print();
        }

        //------------------------------------
        private void InsertCase1(IRBNode n)
        {
            if (n.Parent == null)
                n.Color = Color.BLACK;
            else
                InsertCase2(n);
        }

        //-----------------------------------
        private void InsertCase2(IRBNode n)
        {
            if (NodeColor(n.Parent) == Color.BLACK)
                return; // Tree is still valid
            else
                InsertCase3(n);
        }

        //----------------------------
        private void InsertCase3(IRBNode n)
        {
            if (NodeColor(n.Uncle()) == Color.RED)
            {
                n.Parent.Color = Color.BLACK;
                n.Uncle().Color = Color.BLACK;
                n.Grandparent().Color = Color.RED;
                InsertCase1(n.Grandparent());
            }
            else
            {
                InsertCase4(n);
            }
        }

        //----------------------------
        private void InsertCase4(IRBNode n)
        {
            if (n == n.Parent.Right && n.Parent == n.Grandparent().Left)
            {
                RotateLeft(n.Parent);
                n = n.Left;
            }
            else if (n == n.Parent.Left && n.Parent == n.Grandparent().Right)
            {
                RotateRight(n.Parent);
                n = n.Right;
            }

            InsertCase5(n);
        }

        //----------------------------
        private void InsertCase5(IRBNode n)
        {
            n.Parent.Color = Color.BLACK;
            n.Grandparent().Color = Color.RED;
            if (n == n.Parent.Left && n.Parent == n.Grandparent().Left)
            {
                RotateRight(n.Grandparent());
            }
            else
            {
                //assert n == n.parent.right && n.parent == n.grandparent().right;
                RotateLeft(n.Grandparent());
            }
        }

        private static IRBNode MaximumNode(IRBNode n)
        {
            //assert n != null;
            while (n.Right != null)
            {
                n = n.Right;
            }

            return n;
        }


        public void Delete(IRBNode template, out IRBNode deletedAlt)
        {
            deletedAlt = null;
            IRBNode n = LookupNode(template);
            template = n;
            if (n == null)
                return;  // Key not found, do nothing
            if (n.Left != null && n.Right != null)
            {
                // Copy key/value from predecessor and then delete it instead
                IRBNode pred = MaximumNode(n.Left);
                pred.AssignValueTo(n);
                n = pred;
                deletedAlt = pred;
            }

            //assert n.left == null || n.right == null;
            IRBNode child = (n.Right == null) ? n.Left : n.Right;
            if (NodeColor(n) == Color.BLACK)
            {
                n.Color = NodeColor(child);
                DeleteCase1(n);
            }

            ReplaceNode(n, child);

            if (NodeColor(Root) == Color.RED)
            {
                Root.Color = Color.BLACK;
            }


            return;
        }

        private void DeleteCase1(IRBNode n)
        {
            if (n.Parent == null)
                return;
            else
                DeleteCase2(n);
        }


        private void DeleteCase2(IRBNode n)
        {
            if (NodeColor(n.Sibling()) == Color.RED)
            {
                n.Parent.Color = Color.RED;
                n.Sibling().Color = Color.BLACK;
                if (n == n.Parent.Left)
                    RotateLeft(n.Parent);
                else
                    RotateRight(n.Parent);
            }

            DeleteCase3(n);
        }

        private void DeleteCase3(IRBNode n)
        {
            if (NodeColor(n.Parent) == Color.BLACK &&
                NodeColor(n.Sibling()) == Color.BLACK &&
                NodeColor(n.Sibling().Left) == Color.BLACK &&
                NodeColor(n.Sibling().Right) == Color.BLACK)
            {
                n.Sibling().Color = Color.RED;
                DeleteCase1(n.Parent);
            }
            else
                DeleteCase4(n);
        }

        private void DeleteCase4(IRBNode n)
        {
            if (NodeColor(n.Parent) == Color.RED &&
                NodeColor(n.Sibling()) == Color.BLACK &&
                NodeColor(n.Sibling().Left) == Color.BLACK &&
                NodeColor(n.Sibling().Right) == Color.BLACK)
            {
                n.Sibling().Color = Color.RED;
                n.Parent.Color = Color.BLACK;
            }
            else
                DeleteCase5(n);
        }

        private void DeleteCase5(IRBNode n)
        {
            if (n == n.Parent.Left &&
                NodeColor(n.Sibling()) == Color.BLACK &&
                NodeColor(n.Sibling().Left) == Color.RED &&
                NodeColor(n.Sibling().Right) == Color.BLACK)
            {
                n.Sibling().Color = Color.RED;
                n.Sibling().Left.Color = Color.BLACK;
                RotateRight(n.Sibling());
            }
            else if (n == n.Parent.Right &&
                     NodeColor(n.Sibling()) == Color.BLACK &&
                     NodeColor(n.Sibling().Right) == Color.RED &&
                     NodeColor(n.Sibling().Left) == Color.BLACK)
            {
                n.Sibling().Color = Color.RED;
                n.Sibling().Right.Color = Color.BLACK;
                RotateLeft(n.Sibling());
            }

            DeleteCase6(n);
        }

        private void DeleteCase6(IRBNode n)
        {
            n.Sibling().Color = NodeColor(n.Parent);
            n.Parent.Color = Color.BLACK;
            if (n == n.Parent.Left)
            {
                //assert nodeColor(n.sibling().right) == Color.RED;
                n.Sibling().Right.Color = Color.BLACK;
                RotateLeft(n.Parent);
            }
            else
            {
                //assert nodeColor(n.sibling().left) == Color.RED;
                n.Sibling().Left.Color = Color.BLACK;
                RotateRight(n.Parent);
            }
        }

        public void VisitTree(Action<IRBNode> action)
        {
            //IN Order visit
            IRBNode walker = Root;

            if (walker != null)
                DoVisitTree(action, walker);
        }

        private void DoVisitTree(Action<IRBNode> action, IRBNode walker)
        {
            if (walker.Left != null)
            {
                DoVisitTree(action, walker.Left);
            }

            if (action != null)
                action(walker);

            if (walker.Right != null)
            {
                DoVisitTree(action, walker.Right);
            }

        }

        internal void VisitTreeNodes(Action<IRBNode> action)
        {
            //IN Order visit
            IRBNode walker = Root;

            if (walker != null)
                DoVisitTreeNodes(action, walker);
        }

        private void DoVisitTreeNodes(Action<IRBNode> action, IRBNode walker)
        {
            if (walker.Left != null)
            {
                DoVisitTreeNodes(action, walker.Left);
            }

            if (action != null)
                action(walker);

            if (walker.Right != null)
            {

                DoVisitTreeNodes(action, walker.Right);
            }

        }

        public class RBTreeEnumerator : IEnumerator<IRBNode>
        {
            int position = -1;
            private Queue<IRBNode> heap = new Queue<IRBNode>();

            internal RBTreeEnumerator(RBTree tree)
            {
                tree.VisitTreeNodes(item => heap.Enqueue(item));
            }

            public IRBNode Current
            {
                get
                {
                    return heap.ElementAt(position);
                }
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get
                {
                    return heap.ElementAt(position);
                }
            }

            public bool MoveNext()
            {
                position++;
                return (position < heap.Count);
            }

            public void Reset()
            {
                position = -1;
            }
        }

        public RBTreeEnumerator GetEnumerator()
        {
            return new RBTreeEnumerator(this);
        }

        private static int INDENT_STEP = 15;

        public void Print()
        {
            PrintHelper(Root, 0);
        }

        private static void PrintHelper(IRBNode n, int indent)
        {
            if (n == null)
            {
                Trace.WriteLine("<empty tree>");
                return;
            }

            if (n.Left != null)
            {
                PrintHelper(n.Left, indent + INDENT_STEP);
            }

            for (int i = 0; i < indent; i++)
                Trace.Write(" ");
            if (n.Color == Color.BLACK)
                Trace.WriteLine(" " + n.ToString() + " ");
            else
                Trace.WriteLine("<" + n.ToString() + ">");

            if (n.Right != null)
            {
                PrintHelper(n.Right, indent + INDENT_STEP);
            }
        }

        internal void FireNodeOperation(IRBNode node, NodeOperation operation)
        {
            if (NodeOperation != null)
                NodeOperation(node, operation);
        }

        //internal void FireValueAssigned(RBNode<V> node, V value)
        //{
        //    if (ValueAssignedAction != null)
        //        ValueAssignedAction(node, value);
        //}

        internal event Action<IRBNode> NodeInserted;
        //internal event Action<RBNode<V>> NodeDeleted;
        internal event Action<IRBNode, NodeOperation> NodeOperation;


    }

    internal enum NodeOperation
    {
        LeftAssigned, RightAssigned, ColorAssigned, ParentAssigned,
        ValueAssigned
    }


}
