using Common.Binding;
using Common.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Parsers
{
    public delegate PropertyAttribute AttributeOf(DProperty dp);
    public class CSV<T> where T : DataRow, new()
    {
        private DProperty[] IdProps;
        private DProperty[] FlProps;
        private Context context;
        private StringBuilder s;
        public CSV(AttributeOf selector = null)
        {
            DProperty[] Props;
            Props = DObject.GetProperties<T>();
            var t = new List<DProperty>();
            var x = new List<DProperty>();
            foreach (var p in Props)
            {
                var attr = selector == null ? p.Attribute : selector(p);
                if ((attr & PropertyAttribute.NonSerializable) == PropertyAttribute.NonSerializable)
                    continue;
                if ((attr & PropertyAttribute.SerializeAsId) == PropertyAttribute.SerializeAsId)
                    x.Add(p);
                else
                    t.Add(p);
            }
            FlProps = t.ToArray();
            IdProps = x.ToArray();
            s = new StringBuilder();
        }
        private void BuildHeader(StringBuilder s)
        {
            s.Clear();
            for (int i = 0; i < FlProps.Length; i++)
            {
                if (i != 0) s.Append(';');
                s.Append(FlProps[i].Name);
            }
            var x = FlProps.Length > 0;
            for (int i = 0; i < IdProps.Length; i++)
            {
                if (x) s.Append(';');
                else x = true;
                s.Append(IdProps[i].Name);
            }
            s.AppendLine();
        }
        public StringBuilder Stringify(Context context, DataTable<T> table, Func<T, bool> selctor = null)
        {
            this.s = context.GetBuilder();
            this.context = context;
            var t = table.AsList();
            s.Length = t.Length * 100;
            this.BuildHeader(s);
            for (int i = 0; i < t.Length; i++)
            {
                var v = (T)t[i].Value;
                if (selctor?.Invoke(v) == false)
                    continue;
                BuildRow(v);
            }
            return s;
        }
        public StringBuilder Stringify<P>(Context context, IEnumerable<P> source, Func<P, T> getter, Func<T, bool> selctor = null)
        {
            s = context.GetBuilder();
            s.Clear();
            this.context = context;
            this.BuildHeader(s);
            foreach (var item in source)
            {
                var v = getter(item);
                if (v == null) continue;
                if (selctor?.Invoke(v) == false)
                    continue;
                BuildRow(v);
            }
            return s;
        }

        private void BuildRow(T row)
        {
            var x = false;
            var s = context.GetBuilder();
            for (int i = 0; i < FlProps.Length; i++)
            {
                var p = FlProps[i];
                if (x) s.Append(';');
                else x = true;
                var v = row.Get(p.Index);
                context.Stringify(v);
            }
            for (int i = 0; i < IdProps.Length; i++)
            {
                var p = IdProps[i];
                if (x) s.Append(';');
                else x = true;
                if (!(row.Get(p.Index) is DataRow v)) context.GetBuilder().Append("null");
                else context.GetBuilder().Append(v.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            s.AppendLine();
        }
    }
}
