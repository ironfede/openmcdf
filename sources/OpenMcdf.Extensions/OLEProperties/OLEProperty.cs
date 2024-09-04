
using System;

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
            return PropertyIdentifier.GetDescription(this.container.ContainerType, this.container.PropertyNames);
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
                        if (value is string str && !String.IsNullOrEmpty(str))
                            return str.Trim('\0');
                        break;
                    default:
                        return this.value;
                }

                return this.value;
            }
            set
            {
                this.value = value;
            }
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