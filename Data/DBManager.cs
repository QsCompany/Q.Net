using Common.Binding;
using System;
using System.Data.Common;
namespace Common.Data
{
    public abstract class DBManager:DObject, IDisposable
    {
        public abstract DbConnection SQL { get; }

        public abstract bool Backup(string path, out Exception e);
        public abstract bool Restore(string path, out Exception e);


        public abstract bool CreateBackup(string fileName, out Exception e);

        public abstract void ResetConnection();

        public abstract bool IsDatabaseExist();

        public abstract bool Initialize();

        public abstract bool CheckTables(DProperty[] dProperty);

        public override abstract void Dispose();

        public abstract bool Execute(string v);
        public abstract bool StrictSave(DataRow art, bool update);
        public abstract bool? Save(IDatabaseOperation x);
        public abstract bool Save(DataRow art, bool update);
    }
}
