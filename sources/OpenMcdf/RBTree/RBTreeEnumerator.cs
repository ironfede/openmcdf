using System.Collections;
using System.Collections.Generic;

namespace RedBlackTree
{
    public partial class RBTree
    {
        // TODO: Make internal in v3 (can seal in v2 since constructor is internal)
        public sealed class RBTreeEnumerator : IEnumerator<IRBNode>
        {
            private readonly List<IRBNode> list = new();
            int position = -1;

            internal RBTreeEnumerator(RBTree tree)
            {
                tree.VisitTree(item => list.Add(item));
            }

            public void Dispose()
            {
            }

            public IRBNode Current => list[position];

            object IEnumerator.Current => list[position];

            public bool MoveNext()
            {
                position++;
                return position < list.Count;
            }

            public void Reset()
            {
                position = -1;
            }
        }
    }
}
