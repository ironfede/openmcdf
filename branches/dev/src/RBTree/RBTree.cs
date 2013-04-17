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


    public interface IRBTreeDeserializer<V> where V : IComparable<V>
    {
        RBNode<V> Deseriazlize();
    }

    public enum Color { RED = 0, BLACK = 1 }

    /// <summary>
    /// Red Black Node class
    /// </summary>
    /// <typeparam name="V"></typeparam>
    public class RBNode<V>
        where V : IComparable<V>
    {

        private RBTree<V> owner;
        public RBTree<V> Owner
        {
            get { return owner; }
            set { owner = value; }
        }

        private V value;
        public V Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
                owner.FireNodeOperation(this, NodeOperation.ValueAssigned);
            }
        }

        private RBNode<V> left;
        public RBNode<V> Left
        {
            get { return left; }
            set
            {
                left = value;

                owner.FireNodeOperation(this, NodeOperation.LeftAssigned);
            }
        }

        private RBNode<V> right;
        public RBNode<V> Right
        {
            get { return right; }
            set
            {
                right = value;
                owner.FireNodeOperation(this, NodeOperation.RightAssigned);
            }
        }

        private Color color;

        public Color Color
        {
            get
            {
                return color;
            }
            set
            {
                color = value;
                owner.FireNodeOperation(this, NodeOperation.ColorAssigned);

            }
        }

        public RBNode<V> Parent { get; set; }
        
        public override string ToString()
        {
            if (value != null)
                return value.ToString();
            else
                return String.Empty;

        }

        public RBNode(V value, Color nodeColor, RBNode<V> left, RBNode<V> right, RBTree<V> owner)
        {
            this.owner = owner;
            this.Value = value;
            this.Left = left;
            this.Right = right;

            this.Color = nodeColor;
            if (left != null) left.Parent = this;
            if (right != null) right.Parent = this;
            this.Parent = null;

        }

        public RBNode<V> Grandparent()
        {

#if ASSERT
            Debug.Assert(Parent != null); // Not the root node
            Debug.Assert(Parent.Parent != null); // Not child of root
#endif
            return Parent.Parent;
        }

        public RBNode<V> Sibling()
        {
#if ASSERT
            Debug.Assert(Parent != null); // Root node has no sibling
#endif
            if (this == Parent.Left)
                return Parent.Right;
            else
                return Parent.Left;
        }

        public RBNode<V> Uncle()
        {
#if ASSERT
            Debug.Assert(Parent != null); // Root node has no uncle
            Debug.Assert(Parent.Parent != null); // Children of root have no uncle
#endif
            return Parent.Sibling();
        }
    }

    public class RBTree<V>
        where V : IComparable<V>
    {
        public RBNode<V> Root { get; set; }

        private static Color NodeColor(RBNode<V> n)
        {
            return n == null ? Color.BLACK : n.Color;
        }

        public RBTree()
        {

        }

        public RBTree(IRBTreeDeserializer<V> deserializer)
        {
            Root = deserializer.Deseriazlize();
        }

        private RBNode<V> LookupNode(V val)
        {
            RBNode<V> n = Root;

            while (n != null)
            {
                int compResult = val.CompareTo(n.Value);
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

        public bool TryLookup(V template, out V val)
        {
            RBNode<V> n = LookupNode(template);

            if (n == null)
            {
                val = default(V);
                return false;
            }
            else
            {
                val = n.Value;
                return true;
            }
        }

        private void ReplaceNode(RBNode<V> oldn, RBNode<V> newn)
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

        private void RotateLeft(RBNode<V> n)
        {
            RBNode<V> r = n.Right;
            ReplaceNode(n, r);
            n.Right = r.Left;
            if (r.Left != null)
            {
                r.Left.Parent = n;
            }
            r.Left = n;
            n.Parent = r;
        }

        private void RotateRight(RBNode<V> n)
        {
            RBNode<V> l = n.Left;
            ReplaceNode(n, l);
            n.Left = l.Right;

            if (l.Right != null)
            {
                l.Right.Parent = n;
            }

            l.Right = n;
            n.Parent = l;
        }

        private void DeserializeFromValues(IList<V> values)
        {

        }

        public void Insert(V value)
        {
            RBNode<V> insertedNode = new RBNode<V>(value, Color.RED, null, null, this);

            if (Root == null)
            {
                Root = insertedNode;
            }
            else
            {
                RBNode<V> n = Root;
                while (true)
                {
                    int compResult = value.CompareTo(n.Value);
                    if (compResult == 0)
                    {
                        throw new RBTreeDuplicatedItemException("OhiOhi Duplicated Item");
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
        private void InsertCase1(RBNode<V> n)
        {
            if (n.Parent == null)
                n.Color = Color.BLACK;
            else
                InsertCase2(n);
        }

        //-----------------------------------
        private void InsertCase2(RBNode<V> n)
        {
            if (NodeColor(n.Parent) == Color.BLACK)
                return; // Tree is still valid
            else
                InsertCase3(n);
        }

        //----------------------------
        private void InsertCase3(RBNode<V> n)
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
        private void InsertCase4(RBNode<V> n)
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
        private void InsertCase5(RBNode<V> n)
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

        private static RBNode<V> MaximumNode(RBNode<V> n)
        {
            //assert n != null;
            while (n.Right != null)
            {
                n = n.Right;
            }

            return n;
        }


        public void Delete(V value)
        {
            RBNode<V> n = LookupNode(value);
            if (n == null)
                return;  // Key not found, do nothing
            if (n.Left != null && n.Right != null)
            {
                // Copy key/value from predecessor and then delete it instead
                RBNode<V> pred = MaximumNode(n.Left);
                n.Value = pred.Value;
                n = pred;
            }

            //assert n.left == null || n.right == null;
            RBNode<V> child = (n.Right == null) ? n.Left : n.Right;
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

            //Trace.WriteLine(" ");

            //Print();



        }

        private void DeleteCase1(RBNode<V> n)
        {
            if (n.Parent == null)
                return;
            else
                DeleteCase2(n);
        }


        private void DeleteCase2(RBNode<V> n)
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

        private void DeleteCase3(RBNode<V> n)
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

        private void DeleteCase4(RBNode<V> n)
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

        private void DeleteCase5(RBNode<V> n)
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

        private void DeleteCase6(RBNode<V> n)
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

        public void VisitTree(Action<V> action)
        {
            //IN Order visit
            RBNode<V> walker = Root;

            if (walker != null)
                DoVisitTree(action, walker);
        }

        private void DoVisitTree(Action<V> action, RBNode<V> walker)
        {
            if (walker.Left != null)
            {
                DoVisitTree(action, walker.Left);
            }

            if (action != null)
                action(walker.Value);

            if (walker.Right != null)
            {
                DoVisitTree(action, walker.Right);
            }

        }

        internal void VisitTreeNodes(Action<RBNode<V>> action)
        {
            //IN Order visit
            RBNode<V> walker = Root;

            if (walker != null)
                DoVisitTreeNodes(action, walker);
        }

        private void DoVisitTreeNodes(Action<RBNode<V>> action, RBNode<V> walker)
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

        public class RBTreeEnumerator : IEnumerator<RBNode<V>>
        {
            int position = -1;
            private Queue<RBNode<V>> heap = new Queue<RBNode<V>>();

            internal RBTreeEnumerator(RBTree<V> tree)
            {
                tree.VisitTreeNodes(item => heap.Enqueue(item));
            }

            public RBNode<V> Current
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

        private static void PrintHelper(RBNode<V> n, int indent)
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
                Trace.WriteLine(" " + n.Value + " ");
            else
                Trace.WriteLine("<" + n.Value + ">");

            if (n.Right != null)
            {
                PrintHelper(n.Right, indent + INDENT_STEP);
            }
        }

        internal void FireNodeOperation(RBNode<V> node, NodeOperation operation)
        {
            if (NodeOperation != null)
                NodeOperation(node, operation);
        }

        //internal void FireValueAssigned(RBNode<V> node, V value)
        //{
        //    if (ValueAssignedAction != null)
        //        ValueAssignedAction(node, value);
        //}

        internal event Action<RBNode<V>> NodeInserted;
        //internal event Action<RBNode<V>> NodeDeleted;
        internal event Action<RBNode<V>, NodeOperation> NodeOperation;


    }

    internal enum NodeOperation
    {
        LeftAssigned, RightAssigned, ColorAssigned, ParentAssigned,
        ValueAssigned
    }


}
