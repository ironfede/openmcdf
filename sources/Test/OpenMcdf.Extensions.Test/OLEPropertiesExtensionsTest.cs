using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Collections.Generic;
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

        // winUnicodeDictionary.doc contains a UserProperties section with the CP_WINUNICODE codepage, and LPWSTR string properties
        [TestMethod]
        public void Test_Read_Unicode_User_Properties_Dictionary()
        {
            using (CompoundFile cf = new CompoundFile("winUnicodeDictionary.doc"))
            {
                var dsiStream = cf.RootStorage.GetStream("\u0005DocumentSummaryInformation");
                var co = dsiStream.AsOLEPropertiesContainer();
                var userProps = co.UserDefinedProperties;

                // CodePage should be CP_WINUNICODE (1200) 
                Assert.AreEqual(1200, userProps.Context.CodePage);

                // There should be 5 property names present, and 6 properties (the properties include the code page)
                Assert.AreEqual(5, userProps.PropertyNames.Count);
                Assert.AreEqual(6, userProps.Properties.Count());
                
                // Check for expected names and values
                var propArray = userProps.Properties.ToArray();
                
                // CodePage prop
                Assert.AreEqual(1u, propArray[0].PropertyIdentifier);
                Assert.AreEqual("0x00000001", propArray[0].PropertyName);
                Assert.AreEqual((short)1200, propArray[0].Value);

                // String properties
                Assert.AreEqual("A\0", propArray[1].PropertyName);
                Assert.AreEqual("\0", propArray[1].Value);
                Assert.AreEqual("AB\0", propArray[2].PropertyName);
                Assert.AreEqual("X\0", propArray[2].Value);
                Assert.AreEqual("ABC\0", propArray[3].PropertyName);
                Assert.AreEqual("XY\0", propArray[3].Value);
                Assert.AreEqual("ABCD\0", propArray[4].PropertyName);
                Assert.AreEqual("XYZ\0", propArray[4].Value);
                Assert.AreEqual("ABCDE\0", propArray[5].PropertyName);
                Assert.AreEqual("XYZ!\0", propArray[5].Value);
            }
        }

        // Test that we can add user properties of various types and then read them back
        [TestMethod]
        public void Test_DOCUMENT_SUMMARY_INFO_ADD_CUSTOM()
        {
            if (File.Exists("test_add_user_defined_property.doc"))
                File.Delete("test_add_user_defined_property.doc");

            // Test value for a VT_FILETIME property
            DateTime testNow = DateTime.Now;

            // english.presets.doc has a user defined property section, but no properties other than the codepage
            using (CompoundFile cf = new CompoundFile("english.presets.doc"))
            {
                var dsiStream = cf.RootStorage.GetStream("\u0005DocumentSummaryInformation");
                var co = dsiStream.AsOLEPropertiesContainer();
                var userProperties = co.UserDefinedProperties;

                userProperties.PropertyNames[2] = "StringProperty";
                userProperties.PropertyNames[3] = "BooleanProperty";
                userProperties.PropertyNames[4] = "IntegerProperty";
                userProperties.PropertyNames[5] = "DateProperty";

                var stringProperty = co.NewProperty(VTPropertyType.VT_LPSTR, 2);
                stringProperty.Value = "Hello";
                userProperties.AddProperty(stringProperty);

                var booleanProperty = co.NewProperty(VTPropertyType.VT_BOOL, 3);
                booleanProperty.Value = true;
                userProperties.AddProperty(booleanProperty);

                var integerProperty = co.NewProperty(VTPropertyType.VT_I4, 4);
                integerProperty.Value = 3456;
                userProperties.AddProperty(integerProperty);

                var timeProperty = co.NewProperty(VTPropertyType.VT_FILETIME, 5);
                timeProperty.Value = testNow;
                userProperties.AddProperty(timeProperty);

                co.Save(dsiStream);
                cf.SaveAs(@"test_add_user_defined_property.doc");
            }

            using (CompoundFile cf = new CompoundFile("test_add_user_defined_property.doc"))
            {
                var co = cf.RootStorage.GetStream("\u0005DocumentSummaryInformation").AsOLEPropertiesContainer();
                var propArray = co.UserDefinedProperties.Properties.ToArray();
                Assert.AreEqual(propArray.Length, 5);

                // CodePage prop
                Assert.AreEqual(1u, propArray[0].PropertyIdentifier);
                Assert.AreEqual("0x00000001", propArray[0].PropertyName);
                Assert.AreEqual((short)-535, propArray[0].Value);

                // User properties
                Assert.AreEqual("StringProperty\0", propArray[1].PropertyName);
                Assert.AreEqual("Hello\0", propArray[1].Value);
                Assert.AreEqual(VTPropertyType.VT_LPSTR, propArray[1].VTType);
                Assert.AreEqual("BooleanProperty\0", propArray[2].PropertyName);
                Assert.AreEqual(true, propArray[2].Value);
                Assert.AreEqual(VTPropertyType.VT_BOOL, propArray[2].VTType);
                Assert.AreEqual("IntegerProperty\0", propArray[3].PropertyName);
                Assert.AreEqual(3456, propArray[3].Value);
                Assert.AreEqual(VTPropertyType.VT_I4, propArray[3].VTType);
                Assert.AreEqual("DateProperty\0", propArray[4].PropertyName);
                Assert.AreEqual(testNow, propArray[4].Value);
                Assert.AreEqual(VTPropertyType.VT_FILETIME, propArray[4].VTType);
            }
        }

        // Try to read a document which contains Vector/String properties
        // refs https://github.com/ironfede/openmcdf/issues/98
        [TestMethod]
        public void Test_SUMMARY_INFO_READ_LPWSTRING_VECTOR()
        {
            using (CompoundFile cf = new CompoundFile("SampleWorkBook_bug98.xls"))
            {
                var co = cf.RootStorage.GetStream("\u0005DocumentSummaryInformation").AsOLEPropertiesContainer();

                var docPartsProperty = co.Properties.FirstOrDefault(property => property.PropertyIdentifier == 13); //13 == PIDDSI_DOCPARTS

                Assert.IsNotNull(docPartsProperty);
                
                var docPartsValues = docPartsProperty.Value as IEnumerable<string>;
                Assert.AreEqual(3, docPartsValues.Count());
                Assert.AreEqual("Sheet1\0", docPartsValues.ElementAt(0));
                Assert.AreEqual("Sheet2\0", docPartsValues.ElementAt(1));
                Assert.AreEqual("Sheet3\0", docPartsValues.ElementAt(2));
            }
        }
    }
}
