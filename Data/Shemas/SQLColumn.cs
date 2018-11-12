//using Common.Binding;
using System;

namespace Common.Data.Shemas
{
    using DataRow = System.Data.DataRow;

    public class SQLColumn : ISQLUnit
    {
        public string DBType { get; }
        public SQLTable Table { get; }
        public Type Type => null;
        public int Ordinal { get; }
        public object Default { get; }
        public bool IsNullable { get; }
        public int CharMaxLength { get; }
        public bool IsKey { get; }

        public string Name { get; }

        public SQLColumn(SQLTable table, DataRow data)
        {
            Table = table;
            Name = data["COLUMN_NAME"] as string;
            DBType = data["COLUMN_TYPE"] as string;
            IsKey = (data["COLUMN_KEY"] as string == "PRI");
            Ordinal = (int)(long)SQLUnit<SQLColumn>.GetValue(data["ORDINAL_POSITION"], -1L);
            Default = SQLUnit<SQLColumn>.GetValue(data["COLUMN_DEFAULT"], null);
            IsNullable = (data["IS_NULLABLE"] as string) == "YES";
            CharMaxLength = (int)(long)SQLUnit<SQLColumn>.GetValue(data["CHARACTER_MAXIMUM_LENGTH"], -1L);
        }
        public override string ToString()
        {
            return $"{Name} {DBType} {(IsKey ? "PRIMARY KEY" : "")} ORDINAL {Ordinal} {(Default != null ? $"DEFAULT({Default})" : string.Empty)} " + (IsNullable ? " NULLABLE" : "");
        }
        public bool IsSameType(Binding.DProperty property)
        {
            return true;
        }
    }
}
