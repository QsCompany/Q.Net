//using Common.Binding;
using System.Collections.Generic;

namespace Common.Data.Shemas
{
    using DataRow = System.Data.DataRow;

    public class SQLTable : SQLUnit<SQLColumn>
    {
        public SQLSchema Schema { get; }
        public Dictionary<string, SQLColumn> Columns => Childrens;
        public SQLTable(SQLSchema schema, DataRow data)
        {
            Schema = schema;
            Name = data["TABLE_NAME"] as string;
        }
        public SQLTable()
        {

        }
    }
}
