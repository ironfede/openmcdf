using OleCompoundFileStorage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;

namespace OleCfsTest
{


    /// <summary>
    ///This is a test class for SectorCollectionTest and is intended
    ///to contain all SectorCollectionTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SectorCollectionTest
    {


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
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for Count
        ///</summary>
        [TestMethod()]
        public void CountTest()
        {
            
            int count = 0;

            SectorCollection target = new SectorCollection(); // TODO: Initialize to an appropriate value
            int actual;
            actual = target.Count;

            Assert.IsTrue(actual == count);
            Sector s = new Sector(4096);

            target.Add(s);
            Assert.IsTrue(target.Count == actual + 1);


            for (int i = 0; i < 5000; i++)
                target.Add(s);

            Assert.IsTrue(target.Count == actual + 1 + 5000);
        }

        /// <summary>
        ///A test for Item
        ///</summary>
        [TestMethod()]
        public void ItemTest()
        {
            int count = 37;

            SectorCollection target = new SectorCollection();
            int index = 0;

            Sector expected = new Sector(4096);
            target.Add(null);

            Sector actual;
            target[index] = expected;
            actual = target[index];

            Assert.AreEqual(expected, actual);
            Assert.IsNotNull(actual);
            Assert.IsTrue(actual.Id == expected.Id);

            actual = null;

            try
            {
                actual = target[count + 100];
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is ArgumentOutOfRangeException);
            }

            try
            {
                actual = target[-1];
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is ArgumentOutOfRangeException);
            }
        }

        /// <summary>
        ///A test for SectorCollection Constructor
        ///</summary>
        [TestMethod()]
        public void SectorCollectionConstructorTest()
        {

            SectorCollection target = new SectorCollection();

            Assert.IsNotNull(target);
            Assert.IsTrue(target.Count == 0);

            Sector s = new Sector(4096);
            target.Add(s);
            Assert.IsTrue(target.Count == 1);
        }

        /// <summary>
        ///A test for Add
        ///</summary>
        [TestMethod()]
        public void AddTest()
        {
            SectorCollection target = new SectorCollection();
            for (int i = 0; i < 579; i++)
            {
                target.Add(null);
            }


            Sector item = new Sector(4096);
            target.Add(item);
            Assert.IsTrue(target.Count == 580);
        }

        /// <summary>
        ///A test for GetEnumerator
        ///</summary>
        [TestMethod()]
        public void GetEnumeratorTest()
        {
            SectorCollection target = new SectorCollection();
            for (int i = 0; i < 579; i++)
            {
                target.Add(null);
            }

            
            Sector item = new Sector(4096);
            target.Add(item);
            Assert.IsTrue(target.Count == 580);

            int cnt = 0;
            foreach (Sector s in target)
            {
                cnt++;
            }

            Assert.IsTrue(cnt == target.Count);
        }
    }
}
