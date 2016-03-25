namespace PetaPoco.DatabaseTypes
{
    using PetaPoco;
    using PetaPoco.Internal;
    using System;
    using System.Data;
    using System.Runtime.InteropServices;

    internal class OracleDatabaseType : DatabaseType
    {
        public override string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args, string primaryKey = null)
        {
            if (parts.sqlSelectRemoved.StartsWith("*"))
            {
                throw new Exception("Query must alias '*' when performing a paged query.\neg. select t.* from table t order by t.id");
            }
            return Singleton<SqlServerDatabaseType>.Instance.BuildPageQuery(skip, take, parts, ref args, null);
        }

        public override string EscapeSqlIdentifier(string str)
        {
            return string.Format("\"{0}\"", str.ToUpperInvariant());
        }

        public override object ExecuteInsert(Database db, IDbCommand cmd, string PrimaryKeyName)
        {
            if (PrimaryKeyName != null)
            {
                cmd.CommandText = cmd.CommandText + string.Format(" returning {0} into :newid", this.EscapeSqlIdentifier(PrimaryKeyName));
                IDbDataParameter parameter = cmd.CreateParameter();
                parameter.ParameterName = ":newid";
                parameter.Value = DBNull.Value;
                parameter.Direction = ParameterDirection.ReturnValue;
                parameter.DbType = DbType.Int64;
                cmd.Parameters.Add(parameter);
                db.ExecuteNonQueryHelper(cmd);
                return parameter.Value;
            }
            db.ExecuteNonQueryHelper(cmd);
            return -1;
        }

        public override string GetAutoIncrementExpression(TableInfo ti)
        {
            if (!string.IsNullOrEmpty(ti.SequenceName))
            {
                return string.Format("{0}.nextval", ti.SequenceName);
            }
            return null;
        }

        public override string GetParameterPrefix(string ConnectionString)
        {
            return ":";
        }

        public override void PreExecute(IDbCommand cmd)
        {
            cmd.GetType().GetProperty("BindByName").SetValue(cmd, true, null);
        }
    }
}

