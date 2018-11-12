using Common.Binding;
using Common.Data;
using System;

namespace Common.Parsers
{
    public interface ISerializeParametre
    {
        Type FromType { get; set; }
        Type ToType { get; set; }
        bool StringifyType { get; }
        bool StringifyRef { get; }
    }

    public class DObjectParameter : ISerializeParametre
    {
        public DObjectParameter Super { get; set; }

        private PropertyAttribute?[] attributes;


        public PropertyAttribute? this[int property]
        {
            get => property < attributes.Length ? attributes[property] : null;
            set => attributes[property] = value;
        }

        public bool? DIsFrozen;
        public virtual bool? IsFrozen(object p)
        {
            return DIsFrozen;
        }

        public Type FromType { get; set; }
        public Type ToType { get; set; }

        public Type BaseType { get; private set; }

        public bool StringifyType => true;

        public bool StringifyRef => true;

        public bool FullyStringify { get; set; }

        public DObjectParameter(Type baseType, bool fullyStringify = false)
        {
            BaseType = baseType;
            FullyStringify = fullyStringify;
            FromType = typeof(DObject);
            attributes = new PropertyAttribute?[DObject.GetPropertyCount(baseType)];
        }
    }

    public class DataRowParameter : DObjectParameter
    {
        public bool SerializeAsId { get; set; }
        public DataRowParameter(Type baseType, bool fullyStringify = false, bool SerializeAsId = false) : base(baseType, fullyStringify)
        {
            this.SerializeAsId = SerializeAsId;
        }

    }

    public class DataTableParameter : DObjectParameter
    {
        public Type ForType => typeof(DataTable);
        public bool SerializeItemsAsId { get; set; }


        public new bool StringifyType => true;

        public new bool StringifyRef => true;

        public DataTableParameter(Type baseType) : base(baseType)
        {

        }
    }

}
