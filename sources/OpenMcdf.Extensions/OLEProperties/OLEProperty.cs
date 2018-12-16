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

        //public string Description { get { return description; }
        public uint PropertyIdentifier { get; internal set; }

        public VTPropertyType VTType { get; internal set; }

        public object Value { get; set; }

        private string DecodePropertyIdentifier()
        {
            return PropertyIdentifier.GetDescription(container.ContainerType, container.PropertyNames);
        }

        public override bool Equals(object obj)
        {
            var other = obj as OLEProperty;
            if (other == null) return false;

            return other.PropertyIdentifier == PropertyIdentifier;
        }

        public override int GetHashCode()
        {
            return (int) PropertyIdentifier;
        }
    }
}