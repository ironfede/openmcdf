//using OpenMcdf.Extensions.OLEProperties.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.IO;
//using System.Linq;
//using System.Text;

//namespace OpenMcdf.Extensions.OLEProperties.Api
//{
//    public class OLEPropertyManager
//    {
//        private PropertyContext context = new PropertyContext();
//        private PropertySetStream pss;
//        private List<OLEProperty> properties = new List<OLEProperty>();
//        public IEnumerable<OLEProperty> OLEProperties { get { return new ReadOnlyCollection<OLEProperty>(properties); } }

//        internal PropertyContext GetPropertyContext()
//        {
//            context = new PropertyContext();

//            var b = (int)properties.Where(q => q.PropertyIdentifier == 0x80000003).FirstOrDefault().Value;
//            context.Behavior = b == 1 ? Behavior.CaseSensitive : Behavior.CaseInsensitive;
//            context.CodePage = (int)properties.Where(q => q.PropertyIdentifier == 1).FirstOrDefault().Value;
//            context.Locale = (uint)properties.Where(q => q.PropertyIdentifier == 0x80000000).FirstOrDefault().Value;

//            return context;
//        }

//        internal OLEPropertyManager(int codePage, uint locale, bool caseSensitive)
//        {

//        }

//        internal OLEPropertyManager(Stream propertyStream)
//        {
//            pss = new PropertySetStream(propertyStream);
            
//            for(int i=0;i<pss.N)
//        }

//        public void SetCodePage(int codePage)
//        {
//            var p = properties.Where(q => q.PropertyIdentifier == 1).FirstOrDefault();
//            p._property.PropertyValue = codePage;
//        }

//        public void AddProperty(uint propertyIdentifier, string value)
//        {
//            ITypedPropertyValue tv = null;
//            value += '\0';

//            var codePageProperty = properties.Where(q => q.PropertyIdentifier == 1).FirstOrDefault();
//            var codePagValue = (int)codePageProperty._property.PropertyValue;

//            if (codePagValue == 0x04B0)
//            {
//                tv = new VT(VTPropertyType.VT_LPWSTR);
//            }
//            else
//            {
//                tv = new TypedPropertyValue(VTPropertyType.VT_LPSTR);
//            }

//            tv.PropertyValue = Encoding.GetEncoding(codePagValue).GetBytes(value);
//        }


//        public void AddProperty(uint propertyIdentifier, VTPropertyType vtType, object value)
//        {

//        }

//    }
//}
