using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenMcdf;
using RedBlackTree;

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

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

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

        internal IList<IDirectoryEntry> GetDirectoryRepository(int count)
        {
            List<IDirectoryEntry> repo = new List<IDirectoryEntry>();
            for (int i = 0; i < count; i++)
            {
                IDirectoryEntry de =  DirectoryEntry.New(i.ToString(), StgType.StgInvalid, repo);
            }

            return repo;
        }

        [TestMethod]
        public void Test_RBTREE_INSERT()
        {
            RBTree rbTree = new RBTree();
            System.Collections.Generic.IList<IDirectoryEntry> repo = GetDirectoryRepository(25);

            foreach (var item in repo)
            {
                rbTree.Insert(item);
            }

            for (int i = 0; i < repo.Count; i++)
            {
                IRBNode c;
                rbTree.TryLookup(DirectoryEntry.Mock(i.ToString(), StgType.StgInvalid), out c);
                Assert.IsTrue(c is IDirectoryEntry);
                Assert.IsTrue(((IDirectoryEntry)c).Name == i.ToString());
                //Assert.IsTrue(c.IsStream);
            }
        }


        [TestMethod]
        public void Test_RBTREE_DELETE()
        {
            RBTree rbTree = new RBTree();
            System.Collections.Generic.IList<IDirectoryEntry> repo = GetDirectoryRepository(25);


            foreach (var item in repo)
            {
                rbTree.Insert(item);
            }

            try
            {
                IRBNode n;
                rbTree.Delete(DirectoryEntry.Mock("5", StgType.StgInvalid),out n);
                rbTree.Delete(DirectoryEntry.Mock("24", StgType.StgInvalid), out n);
                rbTree.Delete(DirectoryEntry.Mock("7", StgType.StgInvalid), out n);
            }
            catch (Exception ex)
            {
                Assert.Fail("Item removal failed: " + ex.Message);
            }



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

            Assert.IsTrue(NodeColor(n) == Color.RED || NodeColor(n) == Color.BLACK);

            if (n == null) return;
            VerifyProperty1(n.Left);
            VerifyProperty1(n.Right);
        }

        private static void VerifyProperty2(IRBNode root)
        {
            Assert.IsTrue(NodeColor(root) == Color.BLACK);
        }

        private static void VerifyProperty4(IRBNode n) 
        {

            if (NodeColor(n) == Color.RED)
            {
                Assert.IsTrue((NodeColor(n.Left) == Color.BLACK));
                Assert.IsTrue((NodeColor(n.Right) == Color.BLACK));
                Assert.IsTrue((NodeColor(n.Parent) == Color.BLACK));
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

                    Assert.IsTrue(blackCount == pathBlackCount);

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
            System.Collections.Generic.IList<IDirectoryEntry> repo = GetDirectoryRepository(10000);

            foreach (var item in repo)
            {
                rbTree.Insert(item);
            }

            VerifyProperties(rbTree);
            //rbTree.Print();
        }
    }
}
