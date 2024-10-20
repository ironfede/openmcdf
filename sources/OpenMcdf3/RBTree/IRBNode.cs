using System;

namespace RedBlackTree
{
    public enum Color
    {
        RED = 0,
        BLACK = 1
    }

    /// <summary>
    /// Red Black Node interface
    /// </summary>
    // TODO: Make concrete class in v3
    public interface IRBNode : IComparable
    {
        IRBNode Left { get; set; }

        IRBNode Right { get; set; }

        Color Color { get; set; }

        IRBNode Parent { get; set; }

        IRBNode Grandparent();

        IRBNode Sibling();

        IRBNode Uncle();

        void AssignValueTo(IRBNode other);
    }
}
