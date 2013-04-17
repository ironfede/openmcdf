using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenMcdf;
using RedBlackTree;

namespace OpenMcdfTest
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

        [TestMethod]
        public void Test_RBTREE_INSERT()
        {
            RBTree<CFItem> rbTree = new RBTree<CFItem>();

            List<CFItem> itemsToInsert = new List<CFItem>();

            for (int i = 0; i < 25; i++)
            {
                itemsToInsert.Add(new CFMock(i.ToString(), StgType.StgStream));
            }

            foreach (var item in itemsToInsert)
            {
                rbTree.Insert(item);
            }

            for (int i = 0; i < itemsToInsert.Count; i++)
            {
                CFItem c;
                rbTree.TryLookup(new CFMock(i.ToString(), StgType.StgStream), out c);
                Assert.IsTrue(c is CFItem);
                Assert.IsTrue(c.Name == i.ToString());
                Assert.IsTrue(c.IsStream);
            }
        }


        [TestMethod]
        public void Test_RBTREE_DELETE()
        {
            RBTree<CFItem> rbTree = new RBTree<CFItem>();

            List<CFItem> itemsToInsert = new List<CFItem>();

            for (int i = 0; i < 25; i++)
            {
                itemsToInsert.Add(new CFMock(i.ToString(), StgType.StgStream));
            }

            foreach (var item in itemsToInsert)
            {
                rbTree.Insert(item);
            }

            try
            {
                rbTree.Delete(new CFMock("17", StgType.StgStream));
                rbTree.Delete(new CFMock("24", StgType.StgStream));
                rbTree.Delete(new CFMock("7", StgType.StgStream));
            }
            catch (Exception ex)
            {
                Assert.Fail("Item removal failed: " + ex.Message);
            }



            CFItem c;
            bool s = rbTree.TryLookup(new CFMock("7", StgType.StgStream), out c);


            Assert.IsFalse(s);

            c = null;

            Assert.IsTrue(rbTree.TryLookup(new CFMock("6", StgType.StgStream), out c));
            Assert.IsTrue(c.IsStream);
            Assert.IsTrue(rbTree.TryLookup(new CFMock("12", StgType.StgStream), out c));
            Assert.IsTrue(c.Name == "12");


        }

        private static void VerifyProperties(RBTree<CFItem> t)
        {

            VerifyProperty1<CFItem>(t.Root);
            VerifyProperty2<CFItem>(t.Root);
            // Property 3 is implicit
            VerifyProperty4<CFItem>(t.Root);
            VerifyProperty5<CFItem>(t.Root);
        }

        private static Color NodeColor<K>(RBNode<K> n) where K : IComparable<K>
        {
            return n == null ? Color.BLACK : n.Color;
        }

        private static void VerifyProperty1<K>(RBNode<K> n) where K : IComparable<K>
        {

            Assert.IsTrue(NodeColor(n) == Color.RED || NodeColor(n) == Color.BLACK);

            if (n == null) return;
            VerifyProperty1(n.Left);
            VerifyProperty1(n.Right);
        }

        private static void VerifyProperty2<K>(RBNode<K> root) where K : IComparable<K>
        {
            Assert.IsTrue(NodeColor(root) == Color.BLACK);
        }

        private static void VerifyProperty4<K>(RBNode<K> n) where K : IComparable<K>
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

        private static void VerifyProperty5<K>(RBNode<K> root) where K : IComparable<K>
        {
            VerifyProperty5Helper(root, 0, -1);
        }

        private static int VerifyProperty5Helper<K>(RBNode<K> n, int blackCount, int pathBlackCount) where K : IComparable<K>
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
            RBTree<CFItem> rbTree = new RBTree<CFItem>();

            List<CFItem> itemsToInsert = new List<CFItem>();

            for (int i = 0; i < 25; i++)
            {
                itemsToInsert.Add(new CFMock(i.ToString(), StgType.StgStream));
            }

            foreach (var item in itemsToInsert)
            {
                rbTree.Insert(item);
            }

            VerifyProperties(rbTree);
            //rbTree.Print();
        }
    }
}
