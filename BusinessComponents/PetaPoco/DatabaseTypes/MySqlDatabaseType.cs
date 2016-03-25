namespace PetaPoco.DatabaseTypes
{
    using PetaPoco.Internal;
    using System;

    internal class MySqlDatabaseType : DatabaseType
    {
        public override string EscapeSqlIdentifier(string str)
        {
            return string.Format("`{0}`", str);
        }

        public override string GetExistsSql()
        {
            return "SELECT EXISTS (SELECT 1 FROM {0} WHERE {1})";
        }

        public override string GetParameterPrefix(string ConnectionString)
        {
            if ((ConnectionString != null) && (ConnectionString.IndexOf("Allow User Variables=true") >= 0))
            {
                return "?";
            }
            return "@";
        }
    }
}

