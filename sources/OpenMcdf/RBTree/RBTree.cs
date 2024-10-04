using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

// -------------------------------------------------------------
// This is a porting from java code, under MIT license of       |
// the beautiful Red-Black Tree implementation you can find at  |
// http://en.literateprograms.org/Red-black_tree_(Java)#chunk   |
// Many Thanks to original Implementors.                        |
// -------------------------------------------------------------

namespace RedBlackTree
{
    // TODO: Remove in v3
    public class RBTreeException : Exception
    {
        public RBTreeException(string msg)
            : base(msg)
        {
        }
    }

    // TODO: Use ArgumentException in v3
    public class RBTreeDuplicatedItemException : RBTreeException
    {
        public RBTreeDuplicatedItemException(string msg)
            : base(msg)
        {
        }
    }

    // TODO: Make internal and seal in v3
    public partial class RBTree : IEnumerable<IRBNode>
    {
        private static Color NodeColor(IRBNode n) => n == null ? Color.BLACK : n.Color;

        public IRBNode Root { get; set; }

        public RBTree()
        {
        }

        public RBTree(IRBNode root)
        {
            Root = root;
        }

        private IRBNode LookupNode(IRBNode template)
        {
            IRBNode n = Root;

            while (n != null)
            {
                int compResult = template.CompareTo(n);
                if (compResult == 0)
                    return n;
                n = compResult < 0 ? n.Left : n.Right;
            }

            return n;
        }

        public bool TryLookup(IRBNode template, out IRBNode val)
        {
            IRBNode n = LookupNode(template);

            switch (n)
            {
                case null:
                    val = null;
                    return false;
                default:
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
        }

        private void InsertCase1(IRBNode n)
        {
            if (n.Parent == null)
                n.Color = Color.BLACK;
            else
                InsertCase2(n);
        }

        private void InsertCase2(IRBNode n)
        {
            if (NodeColor(n.Parent) == Color.BLACK)
                return; // Tree is still valid

            InsertCase3(n);
        }

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
            IRBNode child = n.Right ?? n.Left;
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
        }

        private void DeleteCase1(IRBNode n)
        {
            if (n.Parent == null)
                return;

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
            {
                DeleteCase4(n);
            }
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
            {
                DeleteCase5(n);
            }
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
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            // IN Order visit
            if (Root != null)
                VisitNode(action, Root);
        }

        private static void VisitNode(Action<IRBNode> action, IRBNode node)
        {
            if (node.Left != null)
                VisitNode(action, node.Left);

            action.Invoke(node);

            if (node.Right != null)
                VisitNode(action, node.Right);
        }

        public IEnumerator<IRBNode> GetEnumerator() => new RBTreeEnumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Print()
        {
            if (Root is null)
                Trace.WriteLine("<empty tree>");
            else
                Print(Root);
        }

        private static void Print(IRBNode n)
        {
            if (n.Left != null)
            {
                Trace.Indent();
                Print(n.Left);
                Trace.Unindent();
            }

            if (n.Color == Color.BLACK)
                Trace.WriteLine($" {n} ");
            else
                Trace.WriteLine($"<{n}>");

            if (n.Right != null)
            {
                Trace.Indent();
                Print(n.Right);
                Trace.Unindent();
            }
        }
    }
}
