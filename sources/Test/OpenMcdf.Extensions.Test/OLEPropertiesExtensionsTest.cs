using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using OpenMcdf.Extensions.OLEProperties;

namespace OpenMcdf.Extensions.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class OLEPropertiesExtensionsTest
    {
        public OLEPropertiesExtensionsTest()
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
        public void Test_SUMMARY_INFO_READ()
        {
            using (CompoundFile cf = new CompoundFile("_Test.ppt"))
            {
                var co = cf.RootStorage.GetStream("\u0005SummaryInformation").AsOLEPropertiesContainer();

                foreach (OLEProperties.OLEProperty p in co.Properties)
                {
                    Debug.Write(p.PropertyName);
                    Debug.Write(" - ");
                    Debug.Write(p.VTType);
                    Debug.Write(" - ");
                    Debug.WriteLine(p.Value);
                }

                Assert.IsNotNull(co.Properties);

                cf.Close();
            }
        }

        [TestMethod]
        public void Test_DOCUMENT_SUMMARY_INFO_READ()
        {
            using (CompoundFile cf = new CompoundFile("_Test.ppt"))
            {
                var co = cf.RootStorage.GetStream("\u0005DocumentSummaryInformation").AsOLEPropertiesContainer();

                foreach (OLEProperties.OLEProperty p in co.Properties)
                {
                    Debug.Write(p.PropertyName);
                    Debug.Write(" - ");
                    Debug.Write(p.VTType);
                    Debug.Write(" - ");
                    Debug.WriteLine(p.Value);
                }

                Assert.IsNotNull(co.Properties);

                cf.Close();
            }

        }

        [TestMethod]
        public void Test_DOCUMENT_SUMMARY_INFO_ROUND_TRIP()
        {
            if (File.Exists("test1.cfs"))
                File.Delete("test1.cfs");

            using (CompoundFile cf = new CompoundFile("_Test.ppt"))
            {
                var co = cf.RootStorage.GetStream("\u0005DocumentSummaryInformation").AsOLEPropertiesContainer();
                using (CompoundFile cf2 = new CompoundFile())
                {
                    cf2.RootStorage.AddStream("\u0005DocumentSummaryInformation");

                    co.Save(cf2.RootStorage.GetStream("\u0005DocumentSummaryInformation"));

                    cf2.Save("test1.cfs");
                    cf2.Close();
                }

                cf.Close();
            }

        }

        // Modify some document summary information properties, save to a file, and then validate the expected results
        [TestMethod]
        public void Test_DOCUMENT_SUMMARY_INFO_MODIFY()
        {
            if (File.Exists("test_modify_summary.ppt"))
                File.Delete("test_modify_summary.ppt");

            // Verify initial properties, and then create a modified document
            using (CompoundFile cf = new CompoundFile("_Test.ppt"))
            {
                var dsiStream = cf.RootStorage.GetStream("\u0005DocumentSummaryInformation");
                var co = dsiStream.AsOLEPropertiesContainer();

                // The company property should exist but be empty
                var companyProperty = co.Properties.First(prop => prop.PropertyName == "PIDDSI_COMPANY");
                Assert.AreEqual("\0\0\0\0", companyProperty.Value);

                // As a sanity check, check that the value of a property that we don't change remains the same
                var formatProperty = co.Properties.First(prop => prop.PropertyName == "PIDDSI_PRESFORMAT");
                Assert.AreEqual("A4 Paper (210x297 mm)\0\0\0", formatProperty.Value);

                // The manager property shouldn't exist, and we'll add it
                Assert.IsFalse(co.Properties.Any(prop => prop.PropertyName == "PIDDSI_MANAGER"));
                var managerProp = co.NewProperty(VTPropertyType.VT_LPSTR, 0x0000000E, "PIDDSI_MANAGER");
                co.AddProperty(managerProp);

                companyProperty.Value = "My Company";
                managerProp.Value = "The Boss";

                co.Save(dsiStream);
                cf.SaveAs(@"test_modify_summary.ppt");
            }

            using (CompoundFile cf = new CompoundFile("test_modify_summary.ppt"))
            {
                var co = cf.RootStorage.GetStream("\u0005DocumentSummaryInformation").AsOLEPropertiesContainer();

                var companyProperty = co.Properties.First(prop => prop.PropertyName == "PIDDSI_COMPANY");
                Assert.AreEqual("My Company\0", companyProperty.Value);

                var formatProperty = co.Properties.First(prop => prop.PropertyName == "PIDDSI_PRESFORMAT");
                Assert.AreEqual("A4 Paper (210x297 mm)\0\0\0", formatProperty.Value);

                var managerProperty = co.Properties.First(prop => prop.PropertyName == "PIDDSI_MANAGER");
                Assert.AreEqual("The Boss\0", managerProperty.Value);
            }
        }

        [TestMethod]
        public void Test_SUMMARY_INFO_READ_UTF8_ISSUE_33()
        {
            try
            {
                using (CompoundFile cf = new CompoundFile("wstr_presets.doc"))
                {
                    var co = cf.RootStorage.GetStream("\u0005SummaryInformation").AsOLEPropertiesContainer();

                    foreach (OLEProperties.OLEProperty p in co.Properties)
                    {
                        Debug.Write(p.PropertyName);
                        Debug.Write(" - ");
                        Debug.Write(p.VTType);
                        Debug.Write(" - ");
                        Debug.WriteLine(p.Value);
                    }

                    Assert.IsNotNull(co.Properties);

                    var co2 = cf.RootStorage.GetStream("\u0005DocumentSummaryInformation").AsOLEPropertiesContainer();

                    foreach (OLEProperties.OLEProperty p in co2.Properties)
                    {
                        Debug.Write(p.PropertyName);
                        Debug.Write(" - ");
                        Debug.Write(p.VTType);
                        Debug.Write(" - ");
                        Debug.WriteLine(p.Value);
                    }

                    Assert.IsNotNull(co2.Properties);
                }
            }catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Assert.Fail();
            }

        }

        [TestMethod]
        public void Test_SUMMARY_INFO_READ_UTF8_ISSUE_34()
        {
            try
            {
                using (CompoundFile cf = new CompoundFile("2custom.doc"))
                {
                    var co = cf.RootStorage.GetStream("\u0005SummaryInformation").AsOLEPropertiesContainer();

                    foreach (OLEProperties.OLEProperty p in co.Properties)
                    {
                        Debug.Write(p.PropertyName);
                        Debug.Write(" - ");
                        Debug.Write(p.VTType);
                        Debug.Write(" - ");
                        Debug.WriteLine(p.Value);
                    }

                    Assert.IsNotNull(co.Properties);

                    var co2 = cf.RootStorage.GetStream("\u0005DocumentSummaryInformation").AsOLEPropertiesContainer();

                    Assert.IsNotNull(co2.Properties);
                    foreach (OLEProperties.OLEProperty p in co2.Properties)
                    {
                        Debug.Write(p.PropertyName);
                        Debug.Write(" - ");
                        Debug.Write(p.VTType);
                        Debug.Write(" - ");
                        Debug.WriteLine(p.Value);
                    }

                    
                    Assert.IsNotNull(co2.UserDefinedProperties.Properties);
                    foreach (OLEProperties.OLEProperty p in co2.UserDefinedProperties.Properties)
                    {
                        Debug.Write(p.PropertyName);
                        Debug.Write(" - ");
                        Debug.Write(p.VTType);
                        Debug.Write(" - ");
                        Debug.WriteLine(p.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Assert.Fail();
            }

        }
        [TestMethod]
        public void Test_SUMMARY_INFO_READ_LPWSTRING()
        {
            using (CompoundFile cf = new CompoundFile("english.presets.doc"))
            {
                var co = cf.RootStorage.GetStream("\u0005SummaryInformation").AsOLEPropertiesContainer();

                foreach (OLEProperties.OLEProperty p in co.Properties)
                {
                    Debug.Write(p.PropertyName);
                    Debug.Write(" - ");
                    Debug.Write(p.VTType);
                    Debug.Write(" - ");
                    Debug.WriteLine(p.Value);
                }

                Assert.IsNotNull(co.Properties);

                cf.Close();
            }

        }

        // Test that we can modify an LPWSTR property, and the value is null terminated as required
        [TestMethod]
        public void Test_SUMMARY_INFO_MODIFY_LPWSTRING()
        {
            if (File.Exists("test_write_lpwstr.doc"))
                File.Delete("test_write_lpwstr.doc");

            // Modify some LPWSTR properties, and save to a new file
            using (CompoundFile cf = new CompoundFile("wstr_presets.doc"))
            {
                var dsiStream = cf.RootStorage.GetStream("\u0005SummaryInformation");
                var co = dsiStream.AsOLEPropertiesContainer();

                var authorProperty = co.Properties.First(prop => prop.PropertyName == "PIDSI_AUTHOR");
                Assert.AreEqual(VTPropertyType.VT_LPWSTR, authorProperty.VTType);
                Assert.AreEqual("zkyiqpqoroxnbdwhnjfqroxlgylpbgcwuhjfifpkvycugvuecoputqgknnbs\0", authorProperty.Value);

                var keyWordsProperty = co.Properties.First(prop => prop.PropertyName == "PIDSI_KEYWORDS");
                Assert.AreEqual(VTPropertyType.VT_LPWSTR, keyWordsProperty.VTType);
                Assert.AreEqual("abcdefghijk\0", keyWordsProperty.Value);

                authorProperty.Value = "ABC";
                keyWordsProperty.Value = "";
                co.Save(dsiStream);
                cf.SaveAs("test_write_lpwstr.doc");
            }

            // Open the new file and check for the expected values
            using (CompoundFile cf = new CompoundFile("test_write_lpwstr.doc"))
            {
                var co = cf.RootStorage.GetStream("\u0005SummaryInformation").AsOLEPropertiesContainer();

                var authorProperty = co.Properties.First(prop => prop.PropertyName == "PIDSI_AUTHOR");
                Assert.AreEqual(VTPropertyType.VT_LPWSTR, authorProperty.VTType);
                Assert.AreEqual("ABC\0", authorProperty.Value);

                var keyWordsProperty = co.Properties.First(prop => prop.PropertyName == "PIDSI_KEYWORDS");
                Assert.AreEqual(VTPropertyType.VT_LPWSTR, keyWordsProperty.VTType);
                Assert.AreEqual("\0", keyWordsProperty.Value);
            }
        }

    }
}
