using OpenMcdf.Extensions.OLEProperties.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties
{
    public class PropertySet
    {
        public uint Size { get; set; }
        public uint NumProperties { get; set; }

        List<PropertyIdentifierAndOffset> propertyIdentifierAndOffsets
            = new List<PropertyIdentifierAndOffset>();

        public List<PropertyIdentifierAndOffset> PropertyIdentifierAndOffsets
        {
            get { return propertyIdentifierAndOffsets; }
            set { propertyIdentifierAndOffsets = value; }
        }

        List<ITypedPropertyValue> properties = new List<ITypedPropertyValue>();
        public List<ITypedPropertyValue> Properties
        {
            get
            {
                return properties;
            }
            set
            {
                properties = value;
            }
        }

    }
}
