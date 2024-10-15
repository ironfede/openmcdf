
using System;

namespace OpenMcdf.Extensions.OLEProperties
{
    public class OLEProperty
    {
        private readonly OLEPropertiesContainer container;

        internal OLEProperty(OLEPropertiesContainer container)
        {
            this.container = container;
        }

        public string PropertyName => DecodePropertyIdentifier();

        private string DecodePropertyIdentifier()
        {
            return PropertyIdentifier.GetDescription(container.ContainerType, container.PropertyNames);
        }

        //public string Description { get { return description; }
        public uint PropertyIdentifier { get; internal set; }

        public VTPropertyType VTType
        {
            get;
            internal set;
        }

        object value;

        public object Value
        {
            get
            {
                switch (VTType)
                {
                    case VTPropertyType.VT_LPSTR:
                    case VTPropertyType.VT_LPWSTR:
                        if (value is string str && !string.IsNullOrEmpty(str))
                            return str.Trim('\0');
                        break;
                    default:
                        return value;
                }

                return value;
            }
            set => this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is not OLEProperty other)
                return false;

            return other.PropertyIdentifier == PropertyIdentifier;
        }

        public override int GetHashCode()
        {
            return (int)PropertyIdentifier;
        }

        public override string ToString() => $"{PropertyName} - {VTType} - {Value}";
    }
}