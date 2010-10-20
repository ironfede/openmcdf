using System;
using System.Collections.Generic;
using System.Text;

namespace OLECompoundFileStorage
{
    public enum TraversalType
    {
        InOrder
    }

    public class BSTree<T> : ICollection<T>, IEnumerable<T> where T : BSTreeNode<T>
    {
        BSTreeNode<T> _root = null;

        private bool Scan(T templateItem, out T item)
        {
            bool result = false;
            item = null;

            T ptr = _root as T;

            while (ptr != null)
            {
                if (templateItem.CompareTo(ptr) < 0)
                {
                    ptr = ptr.Left;
                }
                else if (templateItem.CompareTo(ptr) > 0)
                {
                    ptr = ptr.Right;
                }
                else
                {
                    result = true;
                    item = ptr;
                    break;
                }

            }

            return result;
        }

        public void Add(T item)
        {
            if (_root == null)
            {
                _root = item;
            }
            else
            {
                T ptr = _root as T;

                while (true)
                {
                    if (item.CompareTo(ptr) < 0)
                    {
                        if (ptr.Left != null)
                        {
                            ptr = ptr.Left;
                            continue;
                        }
                        else
                        {
                            ptr.Left = item;
                            item.Parent = ptr;
                            break;
                        }
                    }
                    else if (item.CompareTo(ptr) > 0)
                    {
                        if (ptr.Right != null)
                        {
                            ptr = ptr.Right;
                            continue;
                        }
                        else
                        {
                            ptr.Right = item;
                            item.Parent = ptr;
                            break;
                        }
                    }
                    else
                        throw new Exception();
                }
            }
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            if (item.Left == null && item.Right == null) //no children ***
            {
                if (item.CompareTo(item.Parent) < 0)
                {
                    // if lesser than parent, it's a left node
                    item.Parent.Left = null;
                }
                else
                {
                    item.Parent.Right = null;
                }

                return true;
            }
            else if ((item.Left != null && item.Right == null)
                || (item.Left == null && item.Right != null)) // one child ***
            {
                if (item.CompareTo(item.Parent) < 0)
                {
                    // if lesser than parent, it's a left node
                    if (item.Left != null)
                        item.Parent.Left = item.Left;
                    else
                        item.Parent.Left = item.Right;
                }
                else
                {
                    // else item is a Right child
                    if (item.Left != null)
                        item.Parent.Right = item.Left;
                    else
                        item.Parent.Right = item.Right;
                }

                return true;
            }
            else // two children ***
            {
                List<T> result = GetSubTree(item.Right);

                if (result.Count > 0) //lesser one of right subtree
                {
                    result[0].CopyTo(item);
                    Remove(result[0]);
                }
                else
                {
                    result[0].CopyTo(item);
                    Remove(result[0]);
                }

                return true;
            }
        }

        public static List<T> GetSubTree(BSTreeNode<T> ptr)
        {
            List<T> result = new List<T>();

            DoGetSubtree(ptr, result);

            return result;
        }

        private static void DoGetSubtree(BSTreeNode<T> ptr, List<T> result)
        {
            if (ptr.Left != null)
                DoGetSubtree(ptr.Left, result);

            result.Add(ptr as T);

            if (ptr.Right != null)
                DoGetSubtree(ptr.Right, result);

        }

        public IEnumerator<T> GetEnumerator()
        {
            BSTreeNode<T> ptr = _root;


            return GetSubTree(ptr).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }


    }
}
