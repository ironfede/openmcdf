
using System;
using System.Collections;
using System.Collections.Generic;

namespace RBTree
{
    ///<summary>
    /// The RedBlackEnumerator class returns the keys or data objects of the treap in
    /// sorted order. 
    ///</summary>
    public class RedBlackEnumerator : IEnumerator<RedBlackNode>
    {
        // the treap uses the stack to order the nodes
        private Stack<RedBlackNode> stack;

        // return in ascending order (true) or descending (false)
        private bool ascending;


        public RedBlackEnumerator()
        {
            
        }

        public delegate void NodeAction(RedBlackNode node);

        internal void DoVisitInOrder(RedBlackNode node, NodeAction nodeCallback)
        {
            if (node.Left != null) DoVisitInOrder(node.Left, nodeCallback);

            if (nodeCallback != null)
            {
                nodeCallback(node);
            }

            if (node.Right != null) DoVisitInOrder(node.Right, nodeCallback);

        }



        ///<summary>
        /// Determine order, walk the tree and push the nodes onto the stack
        ///</summary>
        public RedBlackEnumerator(RedBlackNode tnode, bool ascending)
        {
            stack = new Stack<RedBlackNode>();
            this.ascending = ascending;

            NodeAction na = delegate(RedBlackNode node)
            {
                stack.Push(node);
            };

            DoVisitInOrder(tnode, na);
        }
        ///<summary>
        /// HasMoreElements
        ///</summary>
        public bool HasMoreElements()
        {
            return (stack.Count > 0);
        }

        /////<summary>
        ///// NextElement
        /////</summary>
        //public T NextElement()
        //{
        //    if (stack.Count == 0)
        //        throw (new RedBlackException("Element not found"));

        //    // the top of stack will always have the next item
        //    // get top of stack but don't remove it as the next nodes in sequence
        //    // may be pushed onto the top
        //    // the stack will be popped after all the nodes have been returned
        //    RedBlackNode<T> node = (RedBlackNode<T>)stack.Peek();	//next node in sequence

        //    if (ascending)
        //    {
        //        if (node.Right == RedBlack<T>.sentinelNode)
        //        {
        //            // yes, top node is lowest node in subtree - pop node off stack 
        //            RedBlackNode<T> tn = (RedBlackNode<T>)stack.Pop();
        //            // peek at right node's parent 
        //            // get rid of it if it has already been used
        //            while (HasMoreElements() && ((RedBlackNode<T>)stack.Peek()).Right == tn)
        //                tn = (RedBlackNode<T>)stack.Pop();
        //        }
        //        else
        //        {
        //            // find the next items in the sequence
        //            // traverse to left; find lowest and push onto stack
        //            RedBlackNode<T> tn = node.Right;
        //            while (tn != RedBlack<T>.sentinelNode)
        //            {
        //                stack.Push(tn);
        //                tn = tn.Left;
        //            }
        //        }
        //    }
        //    else            // descending, same comments as above apply
        //    {
        //        if (node.Left == RedBlack<T>.sentinelNode)
        //        {
        //            // walk the tree
        //            RedBlackNode<T> tn = (RedBlackNode<T>)stack.Pop();
        //            while (HasMoreElements() && ((RedBlackNode<T>)stack.Peek()).Left == tn)
        //                tn = (RedBlackNode<T>)stack.Pop();
        //        }
        //        else
        //        {
        //            // determine next node in sequence
        //            // traverse to left subtree and find greatest node - push onto stack
        //            RedBlackNode<T> tn = node.Left;
        //            while (tn != RedBlack<T>.sentinelNode)
        //            {
        //                stack.Push(tn);
        //                tn = tn.Right;
        //            }
        //        }
        //    }

        //    // the following is for .NET compatibility (see MoveNext())

        //    return node.Data;
        //}

        ///<summary>
        /// MoveNext
        /// For .NET compatibility
        ///</summary>
        public bool MoveNext()
        {
            if (HasMoreElements())
            {
                //NextElement();
                this.stack.Pop();
                return true;
            }

            return false;
        }

        #region IEnumerator<RedBlackNode<T>> Members

        public RedBlackNode Current
        {
            get { return this.stack.Peek(); }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            this.stack.Clear();
        }

        #endregion

        #region IEnumerator Members

        object IEnumerator.Current
        {
            get { return this.stack.Peek(); }
        }

        public void Reset()
        {
            this.stack.Clear();
        }

        #endregion
    }
}
