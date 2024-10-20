using System.Collections;
using System.Collections.Generic;

namespace RedBlackTree
{
    public partial class RBTree
    {
        // TODO: Make internal in v3 (can seal in v2 since constructor is internal)
        public sealed class RBTreeEnumerator : IEnumerator<IRBNode>
        {
            private readonly IRBNode root;
            private readonly Stack<IRBNode> stack = new();

            internal RBTreeEnumerator(RBTree tree)
            {
                root = tree.Root;
                PushLeft(root);
            }

            public void Dispose()
            {
            }

            public IRBNode Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (stack.Count == 0)
                    return false;

                Current = stack.Pop();
                PushLeft(Current.Right);
                return true;
            }

            public void Reset()
            {
                Current = null;
                stack.Clear();
                PushLeft(root);
            }

            private void PushLeft(IRBNode node)
            {
                while (node is not null)
                {
                    stack.Push(node);
                    node = node.Left;
                }
            }
        }
    }
}
