using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenMcdf;

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
            //
            // TODO: Add constructor logic here
            //
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
            RBTree.RedBlack rbTree = new RBTree.RedBlack();

            List<CFItem> itemsToInsert = new List<CFItem>();

            for (int i = 0; i < 25; i++)
            {
                itemsToInsert.Add(new CFMock(i.ToString(), StgType.StgStream));

            }

            foreach (var item in itemsToInsert)
            {
                rbTree.Add(item);
            }

            //foreach (var c in rbTree)
            //{
            var S = rbTree.GetData(new CFMock("7", StgType.StgStream));
            //}

            Console.WriteLine(S.Name);
        }

        [TestMethod]
        public void Test_RBTREE_LOGN_DEEP()
        {
            RBTree.RedBlack rbTree = new RBTree.RedBlack();

            List<CFItem> itemsToInsert = new List<CFItem>();

            for (int i = 0; i < 25; i++)
            {
                itemsToInsert.Add(new CFMock(i.ToString(), StgType.StgStream));
            }

            foreach (var item in itemsToInsert)
            {
                rbTree.Add(item);
            }

            
        }

        [TestMethod]
        public void Test_RBTREE_ENUMERATE()
        {
            RBTree.RedBlack rbTree = new RBTree.RedBlack();

            List<CFItem> itemsToInsert = new List<CFItem>();

            for (int i = 0; i < 25; i++)
            {
                itemsToInsert.Add(new CFMock(i.ToString(), StgType.StgStream));
            }

            foreach (var item in itemsToInsert)
            {
                rbTree.Add(item);
            }

            try
            {
                foreach (var c in rbTree)
                {
                    Console.WriteLine(c.Data.Name);
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
        }
    }
}
