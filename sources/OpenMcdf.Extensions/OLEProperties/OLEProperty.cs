
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties
{
    public class OLEProperty
    {
        private OLEPropertiesContainer container;

        internal OLEProperty(OLEPropertiesContainer container)
        {
            this.container = container;
        }

        public string PropertyName
        {
            get { return DecodePropertyIdentifier(); }
        }

        private string DecodePropertyIdentifier()
        {
            switch (container.ContainerType)
            {
                case ContainerType.SummaryInfo:
                    PropertyIdentifiersSummaryInfo id1 = (PropertyIdentifiersSummaryInfo)PropertyIdentifier;
                    return id1.ToString();
                case ContainerType.DocumentSummaryInfo:
                    PropertyIdentifiersDocumentSummaryInfo id2 = (PropertyIdentifiersDocumentSummaryInfo)PropertyIdentifier;
                    return id2.ToString();
                default:
                    return PropertyIdentifier.ToString();
            }
        }


        //public string Description { get { return description; }
        public uint PropertyIdentifier { get; internal set; }

        public VTPropertyType VTType
        {
            get;
            internal set;
        }

        public object Value
        {
            get;
            set;
        }

        public override bool Equals(object obj)
        {
            var other = obj as OLEProperty;
            if (other == null) return false;

            return other.PropertyIdentifier == this.PropertyIdentifier;
        }

        public override int GetHashCode()
        {
            return (int)this.PropertyIdentifier;
        }

    }
}