namespace PetaPoco.Internal
{
    using PetaPoco;
    using PetaPoco.DatabaseTypes;
    using System;
    using System.Data;
    using System.Linq;
    using System.Runtime.InteropServices;

    internal abstract class DatabaseType
    {
        protected DatabaseType()
        {
        }

        public virtual string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args, string primaryKey = null)
        {
            string str = string.Format("{0}\nLIMIT @{1} OFFSET @{2}", parts.sql, args.Length, args.Length + 1);
            args = args.Concat<object>(new object[] { take, skip }).ToArray<object>();
            return str;
        }

        public virtual string EscapeSqlIdentifier(string str)
        {
            return string.Format("[{0}]", str);
        }

        public virtual string EscapeTableName(string tableName)
        {
            if (tableName.IndexOf('.') < 0)
            {
                return this.EscapeSqlIdentifier(tableName);
            }
            return tableName;
        }

        public virtual object ExecuteInsert(Database db, IDbCommand cmd, string PrimaryKeyName)
        {
            cmd.CommandText = cmd.CommandText + ";\nSELECT @@IDENTITY AS NewID;";
            return db.ExecuteScalarHelper(cmd);
        }

        public virtual string GetAutoIncrementExpression(TableInfo ti)
        {
            return null;
        }

        public virtual string GetExistsSql()
        {
            return "SELECT COUNT(*) FROM {0} WHERE {1}";
        }

        public virtual string GetInsertOutputClause(string primaryKeyName)
        {
            return string.Empty;
        }

        public virtual string GetParameterPrefix(string ConnectionString)
        {
            return "@";
        }

        public virtual object MapParameterValue(object value)
        {
            if (value.GetType() == typeof(bool))
            {
                return (((bool) value) ? 1 : 0);
            }
            return value;
        }

        public virtual void PreExecute(IDbCommand cmd)
        {
        }

        public static DatabaseType Resolve(string TypeName, string ProviderName)
        {
            if (TypeName.StartsWith("MySql"))
            {
                return Singleton<MySqlDatabaseType>.Instance;
            }
            if (TypeName.StartsWith("SqlCe"))
            {
                return Singleton<SqlServerCEDatabaseType>.Instance;
            }
            if (TypeName.StartsWith("Npgsql") || TypeName.StartsWith("PgSql"))
            {
                return Singleton<PostgreSQLDatabaseType>.Instance;
            }
            if (TypeName.StartsWith("Oracle"))
            {
                return Singleton<OracleDatabaseType>.Instance;
            }
            if (TypeName.StartsWith("SQLite"))
            {
                return Singleton<SQLiteDatabaseType>.Instance;
            }
            if (!TypeName.StartsWith("System.Data.SqlClient."))
            {
                if (ProviderName.IndexOf("MySql", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    return Singleton<MySqlDatabaseType>.Instance;
                }
                if (ProviderName.IndexOf("SqlServerCe", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    return Singleton<SqlServerCEDatabaseType>.Instance;
                }
                if (ProviderName.IndexOf("pgsql", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    return Singleton<PostgreSQLDatabaseType>.Instance;
                }
                if (ProviderName.IndexOf("Oracle", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    return Singleton<OracleDatabaseType>.Instance;
                }
                if (ProviderName.IndexOf("SQLite", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    return Singleton<SQLiteDatabaseType>.Instance;
                }
            }
            return Singleton<SqlServerDatabaseType>.Instance;
        }
    }
}

