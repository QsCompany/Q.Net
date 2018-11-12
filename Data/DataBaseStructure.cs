using Common.Binding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Common.Data
{
    public enum SqlOperation
    {
        Insert, Update, Delete
    }

    public interface IDatabaseOperation
    {
        string BuildSqlCommand(DataBaseStructure db, System.Data.Common.DbCommand c, ref int i);
        string BuildSqlCommand(DataBaseStructure db, System.Data.Common.DbCommand c);
        bool CanBuild { get; }
    }

    public class DatabaseOperation : IDatabaseOperation
    {
        public SqlOperation Operation;
        public DataRow Data;
        public DatabaseOperation(SqlOperation operation, DataRow data)
        {
            Operation = operation;
            Data = data;
        }

        public bool CanBuild => Data != null;

        public string BuildSqlCommand(DataBaseStructure db, System.Data.Common.DbCommand c, ref int i)
        {
            return Operation == SqlOperation.Update ? Data.GetUpdate(c, ref i) : Operation == SqlOperation.Insert ? Data.GetInsert(c, ref i) : Data.GetDelete(c);
        }
        public string BuildSqlCommand(DataBaseStructure db, System.Data.Common.DbCommand c)
        {
            var i = 0;
            return BuildSqlCommand(db, c, ref i);
        }
    }

    public class OperationBuilder : IDatabaseOperation
    {
        public readonly StringBuilder FormatCommand;
        public readonly List<object> Params;
        public OperationBuilder(string formatCommand, params object[] @params)
        {
            FormatCommand = new StringBuilder(formatCommand);
            Params = new List<object>(@params);
        }

        public bool CanBuild => true;

        public OperationBuilder Append(string s, params object[] values)
        {
            var strs = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                strs[i] = "{" + Params.Count + "}";
                Params.Add(values[i]);
            }
            FormatCommand.AppendFormat(s, strs);
            return this;
        }

        public OperationBuilder Append(bool condition, string s, params object[] values)
        {
            if (!condition) return this;
            var strs = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                strs[i] = "{" + Params.Count + "}";
                Params.Add(values[i]);
            }
            FormatCommand.AppendFormat(s, strs);
            return this;
        }
        public string BuildSqlCommand(DataBaseStructure db, DbCommand c, ref int counter)
        {
            var t = new object[Params.Count];
            for (int i = 0; i < t.Length; i++)
            {
                var val = Params[i];
                var p = c.CreateParameter();
                p.ParameterName = (++counter).ToString();
                p.Value = val;
                t[i] = "@" + p.ParameterName;
                c.Parameters.Add(p);
            }
            return string.Format(FormatCommand.ToString(), t);
        }

        public string BuildSqlCommand(DataBaseStructure db, DbCommand c)
        {
            var i = 0;
            return BuildSqlCommand(db, c, ref i);
        }
    }

    public class DatabaseOperationGroup : IDatabaseOperation, IEnumerable<IDatabaseOperation>
    {
        private List<IDatabaseOperation> operations = new List<IDatabaseOperation>();
        public int Length => operations.Count;

        public bool CanBuild => operations.Count > 0;

        public IDatabaseOperation this[int index]
        {
            get => operations[index];
        }
        public DatabaseOperationGroup Add(SqlOperation operation, DataRow data)
        {
            operations.Add(new DatabaseOperation(operation, data));
            return this;
        }
        public DatabaseOperationGroup Add(IDatabaseOperation operation)
        {
            operations.Add(operation);
            return this;
        }

        public DatabaseOperationGroup Add(string formatCommand, params object[] @params)
        {
            operations.Add(new OperationBuilder(formatCommand, @params));
            return this;
        }
        public DatabaseOperationGroup()
        {

        }
        public DatabaseOperationGroup(SqlOperation operation, DataRow data) => operations.Add(new DatabaseOperation(operation, data));
        public DatabaseOperationGroup(string formatCommand, params object[] @params) => operations.Add(new OperationBuilder(formatCommand, @params));

        public DatabaseOperationGroup(IDatabaseOperation operation) => operations.Add(operation);

        public string BuildSqlCommand(DataBaseStructure db, DbCommand c, ref int i)
        {
            var s = new StringBuilder();
            for (int j = 0, l = operations.Count; j < l; j++)
                s.Append(operations[j].BuildSqlCommand(db, c, ref i)).AppendLine(";");
            return s.ToString();
        }

        public string BuildSqlCommand(DataBaseStructure db, DbCommand c)
        {
            var i = 0;
            return BuildSqlCommand(db, c, ref i);
        }

        public IEnumerator<IDatabaseOperation> GetEnumerator() => operations.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => operations.GetEnumerator();
    }

    public class DataBaseStructure : DataRow
    {
        static int @const = 1;
        protected DatabaseUpdator _updater;
        public DBManager DB;
        protected Dictionary<Type, DProperty> tables = new Dictionary<Type, DProperty>();
        public List<Path> ItemConstraints = new List<Path>();
        public List<Path> ListConstraints = new List<Path>();
        public DatabaseUpdator Updator => (_updater ?? (_updater = new DatabaseUpdator(this)));
        public DataTable this[Type type] => tables.TryGetValue(type, out var tbl) ? get<DataTable>(tbl.Index) : null;

        protected static void Empty(DataBaseStructure d, Path c) { }
        public new static int __LOAD__(int dp) => DataRow.__LOAD__(@const);
        public static string GetPlur(string s) => s.EndsWith("y") ? s.Substring(0, s.Length - 1) + "ies" : s + "s";

        public DatabaseOperationGroup CreateOperations(SqlOperation operation, DataRow data) => new DatabaseOperationGroup(operation, data);
        public DatabaseOperationGroup CreateOperations(string formatCommand, params object[] @params) => new DatabaseOperationGroup(formatCommand, @params);
        public DatabaseOperationGroup CreateOperations() => new DatabaseOperationGroup();
        public DatabaseOperationGroup CreateOperations(IDatabaseOperation operation) => new DatabaseOperationGroup(operation);

        public object GetValue(DProperty dp) => get(dp.Index);

        public bool? Execute(IDatabaseOperation x, Action<DataBaseStructure, DbDataReader, object[]> callback, params object[] @params)
        {

            using (var sqlTran = DB.SQL.BeginTransaction())
            using (var c = DB.SQL.CreateCommand())
            {
                try
                {
                    c.CommandText = x.BuildSqlCommand(this, c);
                    var aff = c.ExecuteReader();
                    try
                    {
                        callback?.Invoke(this, aff, @params);
                    }
                    catch (Exception)
                    {
                    }
                    if (!aff.IsClosed)
                        aff.Close();
                    sqlTran.Commit();
                    return true;
                }
                catch (Exception)
                {
                    try
                    {
                        sqlTran.Rollback();
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                    return false;
                }
            }
        }


        public void SaveAllTables()
        {
            foreach (var property in GetProperties())
                if (typeof(DataTable).IsAssignableFrom(property.Type))
                {
                    var t = get<DataTable>(property.Index);
                    t.Save(DB.SQL);
                }
        }
        public override void Dispose()
        {
            Utils.Ionsole.WriteLine("Database Is Disposing");
            DB.Dispose();

            for (int i = 0; i < _values.Length; i++)
            {
                if (_values[i] is IDisposable)
                    (_values[i] as IDisposable).Dispose();
            }
            base.Dispose();
            Utils.Ionsole.WriteLine("Database Is Disposed");
        }

        public bool Delete(DataRow art)
        {
            if (DB.SQL.State == ConnectionState.Closed)
                DB.SQL.Open();
            using (var c = DB.SQL.CreateCommand())
            {
                try
                {
                    c.CommandText = art.GetDelete(c);
                    var ii = c.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Utils.Ionsole.WriteLine(e.Message);
                    return false;
                }
            }
            return true;
        }
        public bool DropAllTables()
        {
            var t = true;
            foreach (var dp in GetProperties(GetType()))
            {
                if (Get(dp.Index) is DataTable)
                    if (!Exec("DROP TABLE " + dp.Name))
                    {
                        t = false;
                    }
            }
            return t;
        }
        public bool Save(DataRow art, bool update)
        {
            return DB.Save(art, update);
            //DateTime lx = default;
            //if (art is IHistory x) { lx = x.LastModified; x.LastModified = DateTime.Now; }
            //else x = null;

            //int i = update ? 0 : 5;
            //using (var c = DB.SQL.CreateCommand())
            //{
            //deb:

            //    c.Parameters.Clear();
            //    int j = 0;
            //    switch (i)
            //    {
            //        case 0:
            //        case 6:
            //            c.CommandText = art.GetUpdate(c, ref j);
            //            break;
            //        case 1:
            //        case 5:
            //            c.CommandText = art.GetInsert(c, ref j);
            //            break;
            //        default:
            //            if (x != null)
            //                x.LastModified = lx;
            //            return false;
            //    }
            //    // fatal error:-2147467259
            //    // etablision fail:-2147467259
            //    //MySqlExecption
            //    //Socket Exception
            //    //System.InvalidOperationException
            //    try
            //    {
            //        var aff = c.ExecuteNonQuery();
            //        if (i == 0 && aff == 0) { i++; goto deb; }
            //        else if (i == 6 && aff == 0) { i++; goto deb; }
            //        return true;
            //    }
            //    catch (MySql.Data.MySqlClient.MySqlException e)
            //    {
            //        if (e.ErrorCode == 1046)
            //        {
            //            c.CommandText = "use " + Envirenement.Variables.DatabasePath + ";" + c.CommandText + ";";
            //            goto deb;
            //        }
            //        i++;
            //        goto deb;
            //    }
            //    catch (Exception)
            //    {
            //        i++;
            //        goto deb;
            //    }
            //}
        }

        public  virtual bool StrictSave(DataRow art, bool update)
        {
            return DB.StrictSave(art, update);
            //DateTime lx = default;
            //if (art is IHistory x) { lx = x.LastModified; x.LastModified = DateTime.Now; }
            //else x = null;


            //using (var c = DB.SQL.CreateCommand())
            //{
            //    c.Parameters.Clear();
            //    int j = 0;
            //    switch (update)
            //    {
            //        case true:
            //            c.CommandText = art.GetUpdate(c, ref j);
            //            break;
            //        default:
            //            c.CommandText = art.GetInsert(c, ref j);
            //            break;
            //    }
            //deb:
            //    try
            //    {
            //        var aff = c.ExecuteNonQuery();
            //        return true;
            //    }

            //    catch (MySql.Data.MySqlClient.MySqlException e)
            //    {
            //        if (e.ErrorCode == 1046)
            //        {
            //            c.CommandText = "use " + Envirenement.Variables.DatabasePath + ";" + c.CommandText + ";";
            //            goto deb;
            //        }
            //        goto deb;
            //    }
            //    catch
            //    {
            //        if (x != null)
            //            x.LastModified = lx;
            //        return false;
            //    }
            //}
        }

        public bool? Save(IDatabaseOperation x)
        {
            return DB.Save(x);
            //if (!x.CanBuild) return true;
            //using (var sqlTran = DB.SQL.BeginTransaction())
            //using (var c = DB.SQL.CreateCommand())
            //{
            //deb:
            //    try
            //    {
            //        c.CommandText = x.BuildSqlCommand(this, c);
            //        var aff = c.ExecuteNonQuery();
            //        sqlTran.Commit();
            //        return true;
            //    }

            //    catch (MySql.Data.MySqlClient.MySqlException e) when (e.ErrorCode == 1046)
            //    {

            //        c.CommandText = "use " + Envirenement.Variables.DatabasePath + ";" + c.CommandText + ";";
            //        goto deb;
            //    }
            //    catch (Exception)
            //    {
            //        try
            //        {
            //            sqlTran.Rollback();
            //        }
            //        catch (Exception)
            //        {
            //            return null;
            //        }
            //        return false;
            //    }

            //}
        }
        public bool Exec(string cmd)
        {
            if (DB.SQL.State == ConnectionState.Closed)
                DB.SQL.Open();
            using (var c = DB.SQL.CreateCommand())
            {
                try
                {
                    c.CommandText = cmd;
                    c.ExecuteNonQuery();
                    return true;
                }
                catch (Exception e) { Utils.Ionsole.WriteLine(e.Message); return false; }
            }
        }


        public void CreateTableIfNotExist(string tableName, Type datarowType)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("message", nameof(tableName));
            if (!typeof(DataRow).IsAssignableFrom(datarowType)) throw new ArgumentNullException(nameof(datarowType));

            var x = DataTable.CreateTable(tableName, GetProperties(datarowType));
            x = x.Replace("CREATE TABLE", "CREATE TABLE IF NOT EXISTS ");
            using (var c = DB.SQL.CreateCommand())
                try
                {
                    c.CommandText = x;
                    c.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Utils.Ionsole.WriteLine(e.Message);
                }
        }

        public void CreateTable(DataRow t)
        {
            var _tbls = new List<string>();

            {
                _tbls.Add("DROP TABLE [dbo].[" + t.TableName + "]");
                _tbls.Add(DataTable.CreateTable(t.TableName, GetProperties(t.GetType())));
            }
            foreach (var x in _tbls)
                using (var c = DB.SQL.CreateCommand())
                    try
                    {
                        c.CommandText = x;
                        c.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        Utils.Ionsole.WriteLine(e.Message);
                    }
        }
        private void ResetTable(int index)
        {
            var property = GetProperties()[index];
            var _tbls = new List<string>(2);
            if (typeof(DataTable).IsAssignableFrom(property.Type))
            {
                _tbls.Add("DROP TABLE [dbo].[" + property.Name + "]");
                _tbls.Add(DataTable.CreateTable(property.Name, GetProperties(property.Type.BaseType.GetGenericArguments()[0])));
            }
            if (DB.SQL.State == ConnectionState.Closed)
                DB.SQL.Open();
            foreach (var x in _tbls)
                using (var c = DB.SQL.CreateCommand())
                    try
                    {
                        c.CommandText = x;
                        c.ExecuteNonQuery();
                    }
                    catch (Exception ee)
                    {
                        Utils.Ionsole.WriteLine(ee.Message);
                    }
        }
        public void CreateEntity()
        {
            var _tbls = new List<string>();
            foreach (var property in GetProperties())
                if (typeof(DataTable).IsAssignableFrom(property.Type))
                {
                    _tbls.Add("DROP TABLE " + property.Name + "");
                    try
                    {
                        _tbls.Add(DataTable.CreateTable(property.Name, GetProperties(property.Type.BaseType.GetGenericArguments()[0])));
                    }
                    catch (Exception e)
                    {
                        Utils.Ionsole.WriteLine(e.Message);
                    }

                }

            foreach (var x in _tbls)
                using (var c = DB.SQL.CreateCommand())
                    try
                    {
                        c.CommandText = x;
                        c.ExecuteNonQuery();
                    }
                    catch (Exception ee)
                    {
                        Utils.Ionsole.WriteLine(ee.Message);
                    }
        }

        public  bool IsUploading;
        public virtual void Load() => new DatabaseUpdator(this).Update();

        protected virtual void InitTables()
        {
            foreach (var p in GetProperties(GetType()))
                if (typeof(DataTable).IsAssignableFrom(p.Type))
                {

                    tables[p.Type] = p;
                    var t = p.Type;
                    do
                    {
                        if (t.GetGenericArguments().Length != 0)
                            tables[t.GetGenericArguments()[0]] = p;
                        t = t.BaseType;
                    } while (t != typeof(DataTable));
                }
        }
        public DataBaseStructure() => InitTables();
    }
}
