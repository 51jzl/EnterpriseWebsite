namespace PetaPoco.Internal
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;

    internal static class AutoSelectHelper
    {
        private static Regex rxFrom = new Regex(@"\A\s*FROM\s", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static Regex rxSelect = new Regex(@"\A\s*(SELECT|EXECUTE|CALL)\s", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        public static string AddSelectClause<T>(DatabaseType DatabaseType, string sql, string primaryKey = null)
        {
            if (sql.StartsWith(";"))
            {
                return sql.Substring(1);
            }
            if (!rxSelect.IsMatch(sql))
            {
                PocoData data = PocoData.ForType(typeof(T));
                string tableName = DatabaseType.EscapeTableName(data.TableInfo.TableName);
                string str = string.Empty;
                if (string.IsNullOrEmpty(primaryKey))
                {
                    str = (data.Columns.Count != 0) ? string.Join(", ", (from c in data.QueryColumns select tableName + "." + DatabaseType.EscapeSqlIdentifier(c)).ToArray<string>()) : "NULL";
                }
                else
                {
                    str = primaryKey;
                }
                if (!rxFrom.IsMatch(sql))
                {
                    sql = string.Format("SELECT {0} FROM {1} {2}", str, tableName, sql);
                    return sql;
                }
                sql = string.Format("SELECT {0} {1}", str, sql);
            }
            return sql;
        }
    }
}

