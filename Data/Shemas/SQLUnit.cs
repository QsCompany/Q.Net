//using Common.Binding;
using System;
using System.Collections.Generic;

namespace Common.Data.Shemas
{
    public interface ISQLUnit
    {
        string Name { get; }
    }
    public abstract class SQLUnit<T> : ISQLUnit where T : class, ISQLUnit
    {
        public string Name { get; protected set; }
        public override string ToString()
        {
            return Name;
        }
        public static object GetValue(object value, object _default)
        {
            if (value == DBNull.Value) return _default;
            return value;
        }

        public Dictionary<string, T> Childrens { get; } = new Dictionary<string, T>();
        public T this[string name] => Childrens.TryGetValue(name.ToLowerInvariant(), out var child) ? child : null;
        public void Add(T table) => this.Childrens.Add(table.Name.ToLowerInvariant(), table);
    }
}
