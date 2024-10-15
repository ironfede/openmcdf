using Microsoft.VisualStudio.TestTools.UnitTesting;
using RedBlackTree;
using System.Collections.Generic;

namespace OpenMcdf.Test
{
    /// <summary>
    /// Summary description for RBTreeTest
    /// </summary>
    [TestClass]
    public class RBTreeTest
    {
        public RBTreeTest()
        {
        }

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        internal static IList<IDirectoryEntry> GetDirectoryRepository(int count)
        {
            List<IDirectoryEntry> repo = new List<IDirectoryEntry>();
            for (int i = 0; i < count; i++)
            {
                _ = DirectoryEntry.New(i.ToString(), StgType.StgInvalid, repo);
            }

            return repo;
        }

        [TestMethod]
        public void Test_RBTREE_INSERT()
        {
            RBTree rbTree = new RBTree();
            IList<IDirectoryEntry> repo = GetDirectoryRepository(1000000);

            foreach (var item in repo)
            {
                rbTree.Insert(item);
            }

            for (int i = 0; i < repo.Count; i++)
            {
                rbTree.TryLookup(DirectoryEntry.Mock(i.ToString(), StgType.StgInvalid), out IRBNode c);
                Assert.IsTrue(c is IDirectoryEntry);
                Assert.AreEqual(i.ToString(), ((IDirectoryEntry)c).Name);
                //Assert.IsTrue(c.IsStream);
            }
        }

        [TestMethod]
        public void Test_RBTREE_DELETE()
        {
            RBTree rbTree = new RBTree();
            IList<IDirectoryEntry> repo = GetDirectoryRepository(25);

            foreach (var item in repo)
            {
                rbTree.Insert(item);
            }

            rbTree.Delete(DirectoryEntry.Mock("5", StgType.StgInvalid), out _);
            rbTree.Delete(DirectoryEntry.Mock("24", StgType.StgInvalid), out _);
            rbTree.Delete(DirectoryEntry.Mock("7", StgType.StgInvalid), out _);

            //    CFItem c;
            //    bool s = rbTree.TryLookup(new CFMock("7", StgType.StgStream), out c);

            //    Assert.IsFalse(s);

            //    c = null;

            //    Assert.IsTrue(rbTree.TryLookup(new CFMock("6", StgType.StgStream), out c));
            //    Assert.IsTrue(c.IsStream);
            //    Assert.IsTrue(rbTree.TryLookup(new CFMock("12", StgType.StgStream), out c));
            //    Assert.IsTrue(c.Name == "12");

            //}
        }

        private static void VerifyProperties(RBTree t)
        {
            VerifyProperty1(t.Root);
            VerifyProperty2(t.Root);
            // Property 3 is implicit
            VerifyProperty4(t.Root);
            VerifyProperty5(t.Root);
        }

        private static Color NodeColor(IRBNode n)
        {
            return n == null ? Color.BLACK : n.Color;
        }

        private static void VerifyProperty1(IRBNode n)
        {
            Assert.IsTrue(NodeColor(n) is Color.RED or Color.BLACK);

            if (n == null) return;
            VerifyProperty1(n.Left);
            VerifyProperty1(n.Right);
        }

        private static void VerifyProperty2(IRBNode root)
        {
            Assert.AreEqual(Color.BLACK, NodeColor(root));
        }

        private static void VerifyProperty4(IRBNode n)
        {
            if (NodeColor(n) == Color.RED)
            {
                Assert.AreEqual(Color.BLACK, NodeColor(n.Left));
                Assert.AreEqual(Color.BLACK, NodeColor(n.Right));
                Assert.AreEqual(Color.BLACK, NodeColor(n.Parent));
            }

            if (n == null) return;
            VerifyProperty4(n.Left);
            VerifyProperty4(n.Right);
        }

        private static void VerifyProperty5(IRBNode root)
        {
            VerifyProperty5Helper(root, 0, -1);
        }

        private static int VerifyProperty5Helper(IRBNode n, int blackCount, int pathBlackCount)
        {
            if (NodeColor(n) == Color.BLACK)
            {
                blackCount++;
            }

            if (n == null)
            {
                if (pathBlackCount == -1)
                {
                    pathBlackCount = blackCount;
                }
                else
                {
                    Assert.AreEqual(blackCount, pathBlackCount);
                }

                return pathBlackCount;
            }

            pathBlackCount = VerifyProperty5Helper(n.Left, blackCount, pathBlackCount);
            pathBlackCount = VerifyProperty5Helper(n.Right, blackCount, pathBlackCount);

            return pathBlackCount;
        }

        [TestMethod]
        public void Test_RBTREE_ENUMERATE()
        {
            RBTree rbTree = new RBTree();
            IList<IDirectoryEntry> repo = GetDirectoryRepository(10000);

            foreach (var item in repo)
            {
                rbTree.Insert(item);
            }

            VerifyProperties(rbTree);
            //rbTree.Print();
        }
    }
}
