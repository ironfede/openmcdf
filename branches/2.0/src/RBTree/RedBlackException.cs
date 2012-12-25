// Original source code from CodeProject, CPOL license, by Roy Clem
// http://www.codeproject.com/Articles/8287/Red-Black-Trees-in-C
// Modified from Federico Blaseotto,2012.

using System;

namespace RBTree
{
    ///<summary>
    /// The RedBlackException class distinguishes read black tree exceptions from .NET
    /// exceptions. 
    ///</summary>
    public class RedBlackException : Exception
    {
        public RedBlackException()
        {
        }

        public RedBlackException(string msg)
            : base(msg)
        {
        }
    }
}
