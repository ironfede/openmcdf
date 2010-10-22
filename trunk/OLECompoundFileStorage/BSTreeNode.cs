using System;
using System.Collections.Generic;
using System.Text;

namespace OLECompoundFileStorage
{
    public abstract class BSTreeNode<T> : IComparable<T> where T : BSTreeNode<T>
    {
        private T parent;

        public T Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        private T left;

        public T Left
        {
            get { return left; }
            set { left = value; }
        }

        private T right;

        public T Right
        {
            get { return right; }
            set { right = value; }
        }

        public virtual int CompareTo(T other)
        {
            throw new NotImplementedException("You must provide a concrete implementation in subclasses");
        }

        public abstract void CopyTo(T dstObj);
    }
}
