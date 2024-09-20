using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenMcdf.Extensions.OLEProperties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

                    cf2.SaveAs("test1.cfs");
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
                Assert.AreEqual("", companyProperty.Value);

                // As a sanity check, check that the value of a property that we don't change remains the same
                var formatProperty = co.Properties.First(prop => prop.PropertyName == "PIDDSI_PRESFORMAT");
                Assert.AreEqual("A4 Paper (210x297 mm)", formatProperty.Value);

                // The manager property shouldn't exist, and we'll add it
                Assert.IsFalse(co.Properties.Any(prop => prop.PropertyName == "PIDDSI_MANAGER"));

                var managerProp = co.NewProperty(VTPropertyType.VT_LPSTR, 0x0000000E, "PIDDSI_MANAGER");
                co.AddProperty(managerProp);

                companyProperty.Value = "My Company";
                managerProp.Value = "The Boss";

                co.Save(dsiStream);
                cf.SaveAs(@"test_modify_summary.ppt");
                cf.Close();
            }

            using (CompoundFile cf = new CompoundFile("test_modify_summary.ppt"))
            {
                var co = cf.RootStorage.GetStream("\u0005DocumentSummaryInformation").AsOLEPropertiesContainer();

                var companyProperty = co.Properties.First(prop => prop.PropertyName == "PIDDSI_COMPANY");
                Assert.AreEqual("My Company", companyProperty.Value);

                var formatProperty = co.Properties.First(prop => prop.PropertyName == "PIDDSI_PRESFORMAT");
                Assert.AreEqual("A4 Paper (210x297 mm)", formatProperty.Value);

                var managerProperty = co.Properties.First(prop => prop.PropertyName == "PIDDSI_MANAGER");
                Assert.AreEqual("The Boss", managerProperty.Value);
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
            }
            catch (Exception ex)
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
                Assert.AreEqual("zkyiqpqoroxnbdwhnjfqroxlgylpbgcwuhjfifpkvycugvuecoputqgknnbs", authorProperty.Value);

                var keyWordsProperty = co.Properties.First(prop => prop.PropertyName == "PIDSI_KEYWORDS");
                Assert.AreEqual(VTPropertyType.VT_LPWSTR, keyWordsProperty.VTType);
                Assert.AreEqual("abcdefghijk", keyWordsProperty.Value);

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
                Assert.AreEqual("ABC", authorProperty.Value);

                var keyWordsProperty = co.Properties.First(prop => prop.PropertyName == "PIDSI_KEYWORDS");
                Assert.AreEqual(VTPropertyType.VT_LPWSTR, keyWordsProperty.VTType);
                Assert.AreEqual("", keyWordsProperty.Value);
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
                Assert.AreEqual("A", propArray[1].PropertyName);
                Assert.AreEqual("", propArray[1].Value);
                Assert.AreEqual("AB", propArray[2].PropertyName);
                Assert.AreEqual("X", propArray[2].Value);
                Assert.AreEqual("ABC", propArray[3].PropertyName);
                Assert.AreEqual("XY", propArray[3].Value);
                Assert.AreEqual("ABCD", propArray[4].PropertyName);
                Assert.AreEqual("XYZ", propArray[4].Value);
                Assert.AreEqual("ABCDE", propArray[5].PropertyName);
                Assert.AreEqual("XYZ!", propArray[5].Value);
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
                userProperties.PropertyNames[6] = "DoubleProperty";

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

                var doubleProperty = co.NewProperty(VTPropertyType.VT_R8, 6);
                doubleProperty.Value = 1.234567d;
                userProperties.AddProperty(doubleProperty);

                co.Save(dsiStream);
                cf.SaveAs(@"test_add_user_defined_property.doc");
            }

            using (CompoundFile cf = new CompoundFile("test_add_user_defined_property.doc"))
            {
                var co = cf.RootStorage.GetStream("\u0005DocumentSummaryInformation").AsOLEPropertiesContainer();
                var propArray = co.UserDefinedProperties.Properties.ToArray();
                Assert.AreEqual(propArray.Length, 6);

                // CodePage prop
                Assert.AreEqual(1u, propArray[0].PropertyIdentifier);
                Assert.AreEqual("0x00000001", propArray[0].PropertyName);
                Assert.AreEqual((short)-535, propArray[0].Value);

                // User properties
                Assert.AreEqual("StringProperty", propArray[1].PropertyName);
                Assert.AreEqual("Hello", propArray[1].Value);
                Assert.AreEqual(VTPropertyType.VT_LPSTR, propArray[1].VTType);
                Assert.AreEqual("BooleanProperty", propArray[2].PropertyName);
                Assert.AreEqual(true, propArray[2].Value);
                Assert.AreEqual(VTPropertyType.VT_BOOL, propArray[2].VTType);
                Assert.AreEqual("IntegerProperty", propArray[3].PropertyName);
                Assert.AreEqual(3456, propArray[3].Value);
                Assert.AreEqual(VTPropertyType.VT_I4, propArray[3].VTType);
                Assert.AreEqual("DateProperty", propArray[4].PropertyName);
                Assert.AreEqual(testNow, propArray[4].Value);
                Assert.AreEqual(VTPropertyType.VT_FILETIME, propArray[4].VTType);
                Assert.AreEqual("DoubleProperty", propArray[5].PropertyName);
                Assert.AreEqual(1.234567d, propArray[5].Value);
                Assert.AreEqual(VTPropertyType.VT_R8, propArray[5].VTType);
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
                Assert.AreEqual("Sheet1", docPartsValues.ElementAt(0));
                Assert.AreEqual("Sheet2", docPartsValues.ElementAt(1));
                Assert.AreEqual("Sheet3", docPartsValues.ElementAt(2));
            }
        }

        [TestMethod]
        public void Test_CLSID_PROPERTY()
        {
            var guid = new Guid("15891a95-bf6e-4409-b7d0-3a31c391fa31");
            using (CompoundFile cf = new CompoundFile("CLSIDPropertyTest.file"))
            {
                var co = cf.RootStorage.GetStream("\u0005C3teagxwOttdbfkuIaamtae3Ie").AsOLEPropertiesContainer();
                var clsidProp = co.Properties.First(x => x.PropertyName == "DocumentID");
                Assert.AreEqual(guid, clsidProp.Value);
            }
        }

        // The test file 'report.xls' contains a DocumentSummaryInfo section, but no user defined properties.
        //    This tests adding a new user defined properties section to the existing DocumentSummaryInfo.
        [TestMethod]
        public void Test_ADD_USER_DEFINED_PROPERTIES_SECTION()
        {
            if (File.Exists("test_add_user_defined_properties.xls"))
                File.Delete("test_add_user_defined_properties.xls");

            using (CompoundFile cf = new CompoundFile("report.xls"))
            {
                var dsiStream = cf.RootStorage.GetStream("\u0005DocumentSummaryInformation");
                var co = dsiStream.AsOLEPropertiesContainer();

                Assert.IsFalse(co.HasUserDefinedProperties);
                Assert.IsNull(co.UserDefinedProperties);

                var newUserDefinedProperties = co.CreateUserDefinedProperties(65001); // 65001 - UTF-8

                newUserDefinedProperties.PropertyNames[2] = "MyCustomProperty";

                var newProperty = co.NewProperty(VTPropertyType.VT_LPSTR, 2);
                newProperty.Value = "Testing";
                newUserDefinedProperties.AddProperty(newProperty);

                co.Save(dsiStream);
                cf.SaveAs("test_add_user_defined_properties.xls");
            }

            using (CompoundFile cf = new CompoundFile("test_add_user_defined_properties.xls"))
            {
                var co = cf.RootStorage.GetStream("\u0005DocumentSummaryInformation").AsOLEPropertiesContainer();

                // User defined properties should be present now
                Assert.IsTrue(co.HasUserDefinedProperties);
                Assert.IsNotNull(co.UserDefinedProperties);
                Assert.AreEqual(65001, co.UserDefinedProperties.Context.CodePage);

                // And the expected properties should the there
                var propArray = co.UserDefinedProperties.Properties.ToArray();
                Assert.AreEqual(propArray.Length, 2);

                // CodePage prop
                Assert.AreEqual(1u, propArray[0].PropertyIdentifier);
                Assert.AreEqual("0x00000001", propArray[0].PropertyName);
                Assert.AreEqual((short)-535, propArray[0].Value);

                // User properties
                Assert.AreEqual("MyCustomProperty", propArray[1].PropertyName);
                Assert.AreEqual("Testing", propArray[1].Value);
                Assert.AreEqual(VTPropertyType.VT_LPSTR, propArray[1].VTType);
            }
        }

        // A test for the issue described in https://github.com/ironfede/openmcdf/issues/134 where modifying an AppSpecific stream
        // removes any already-existing Dictionary property
        [TestMethod]
        public void Test_Retain_Dictionary_Property_In_AppSpecific_Streams()
        {
            File.Delete("Issue134RoundTrip.cfs");

            using (CompoundFile cf = new CompoundFile("Issue134.cfs"))
            {
                var testStream = cf.RootStorage.GetStream("Issue134");
                var co = testStream.AsOLEPropertiesContainer();

                // Test initial contents are as expected
                AssertExpectedProperties(co.PropertyNames);

                // Write test file
                co.Save(testStream);
                cf.SaveAs("Issue134RoundTrip.cfs");
            }

            // Open test file, and check that the property names are still as expected.
            using (CompoundFile cf = new CompoundFile("Issue134RoundTrip.cfs", CFSUpdateMode.ReadOnly, CFSConfiguration.Default))
            {
                var co = cf.RootStorage.GetStream("Issue134").AsOLEPropertiesContainer();
                AssertExpectedProperties(co.PropertyNames);
            }

            void AssertExpectedProperties(Dictionary<uint, string> actual)
            {
                Assert.IsNotNull(actual);

                var expected = new Dictionary<uint, string>()
                {
                    [2] = "Document Number",
                    [3] = "Revision",
                    [4] = "Project Name"
                };

                CollectionAssert.AreEqual(expected, actual);
            }
        }
    }
}
