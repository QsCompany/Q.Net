//using Common.Binding;
using System.Collections.Generic;
using System.Data.Common;

namespace Common.Data.Shemas
{
    public class SQLDatabase : SQLUnit<SQLSchema>
    {
        public SQLDatabase(DbConnection sql)
        {
            GrabbeSchemas(sql);
        }

        public string ConnectionString { get; }
        public Dictionary<string, SQLSchema> Schemas => Childrens;

        public void GrabbeSchemas(DbConnection db)
        {
            var df = db.GetSchema("databases");
            foreach (System.Data.DataRow r in df.Rows)
            {
                var v = r["database_name"] as string;
                switch (v)
                {
                    case "mysql":
                    case "information_schema":
                    case "":
                    case null:
                        continue;
                    default:
                        Add(new SQLSchema(this, r));
                        continue;
                }
            }
            GrabbeTables(db);
        }
        public void GrabbeTables(DbConnection db)
        {
            var df = db.GetSchema("tables");
            List<string> tables = new List<string>();
            foreach (System.Data.DataRow r in df.Rows)
            {
                var schemaName = r["table_schema"] as string;
                var schema = this[schemaName];
                if (schema == null) continue;
                schema.Add(new SQLTable(schema, r));
            }
            this.GrabbeColumns(db);
        }

        public void GrabbeColumns(DbConnection db)
        {
            var df = db.GetSchema("columns");
            List<string> tables = new List<string>();
            foreach (System.Data.DataRow r in df.Rows)
            {
                var schemaName = r["table_schema"] as string;
                var tableName = r["table_name"] as string;

                var schema = this[schemaName];
                if (schema == null) continue;
                var table = schema[tableName];
                if (table == null) continue;
                table.Add(new SQLColumn(table, r));
            }
        }
    }
}
