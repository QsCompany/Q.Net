//using Common.Binding;
using System.Collections.Generic;

namespace Common.Data.Shemas
{
    using DataRow = System.Data.DataRow;

    public class SQLSchema : SQLUnit<SQLTable>
    {
        public SQLDatabase Database { get; }

        public Dictionary<string, SQLTable> Tables => Childrens;

        public SQLSchema(SQLDatabase database, DataRow data)
        {
            Database = database;
            Name = data["database_name"] as string;
        }
    }
}
